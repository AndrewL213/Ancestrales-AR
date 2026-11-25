#if ANCESTRALES_ARF
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Ancestrales.AR
{
    /// <summary>
    /// Encuentro estilo Pokémon GO:
    /// 1) se escanea el espacio, 2) un aro marca la superficie (suelo o mesa),
    /// 3) al tocar aparece la criatura anclada al mundo real, 4) se lanza el amuleto para capturarla.
    /// En modo Palma la criatura aparece pequeña a ~35 cm de la cámara para posarla sobre la mano.
    /// </summary>
    public class GestorEncuentroAR : MonoBehaviour
    {
        public enum Fase { Iniciando, Escaneando, Apuntando, Apareciendo, Encuentro, Capturando, Resultado, NoCompatible }

        /// <summary>Asígnalo antes de cargar la escena para abrir con una criatura concreta.</summary>
        public static string CriaturaInicialId;

        [Header("AR (los asigna el menú de configuración)")]
        public ARSession sesion;
        public ARRaycastManager raycaster;
        public ARPlaneManager planos;
        public AROcclusionManager oclusion;
        public Camera camara;

        [Header("Contenido")]
        public CatalogoAR catalogo;
        public HudAR hud;
        public LanzadorAmuleto lanzador;

        [Header("Materiales")]
        public Material materialBrillo;
        public Material materialSombra;

        [Header("Ajustes")]
        public ModoSuperficie modo = ModoSuperficie.Auto;
        [Tooltip("Una superficie más alta que esto sobre el suelo se considera mesa.")]
        public float alturaMinimaMesa = 0.35f;
        [Tooltip("Si la superficie está a menos de esto por debajo del celular, se considera mesa.")]
        public float distanciaMaximaMesa = 0.85f;
        [Tooltip("Distancia a la que aparece la criatura en modo Palma.")]
        public float distanciaPalma = 0.33f;
        public bool ocultarPlanosAlInvocar = true;

        public Fase FaseActual { get; private set; } = Fase.Iniciando;
        public CriaturaAR Actual { get; private set; }
        public CriaturaViva Instancia { get; private set; }

        static readonly List<ARRaycastHit> impactos = new List<ARRaycastHit>();
        ReticulaAR reticula;
        AnilloCaptura anillo;
        bool hayObjetivo;
        Pose poseObjetivo;
        TipoSuperficie tipoObjetivo;
        int intentos;

        // ------------------------------------------------------------------ inicio

        IEnumerator Start()
        {
            if (camara == null) camara = Camera.main;
            reticula = ReticulaAR.Crear(materialBrillo);
            MostrarPlanos(true);

            if (hud != null)
            {
                hud.AlCambiarModo += CambiarModo;
                hud.AlPedirOtra += OtraCriatura;
                hud.MarcarModo(modo);
            }
            if (lanzador != null)
            {
                lanzador.Configurar(camara);
                lanzador.Objetivo = () => Instancia;
                lanzador.TocaInterfaz = p => hud != null && hud.TocaInterfaz(p);
                lanzador.AlGolpear += (am, cr) => StartCoroutine(SecuenciaCaptura(am, cr));
                lanzador.AlFallar += () => { if (FaseActual == Fase.Encuentro) hud?.Valoracion("¡Fallaste!"); };
            }

            Actual = catalogo != null ? (catalogo.Buscar(CriaturaInicialId) ?? catalogo.Aleatoria()) : null;
            CriaturaInicialId = null;
            RefrescarHud();

            if (Actual == null || Actual.modelo == null)
            {
                hud?.Mensaje("No hay criaturas con modelo en el catálogo. Usa el menú Ancestrales > AR > 1.");
                yield break;
            }

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();

            if (ARSession.state == ARSessionState.Unsupported)
            {
                FaseActual = Fase.NoCompatible;
                hud?.Mensaje("Este celular no es compatible con realidad aumentada (ARCore / ARKit).");
                yield break;
            }
            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                hud?.Mensaje("Instalando los servicios de realidad aumentada…");
                yield return ARSession.Install();
            }
            FaseActual = Fase.Escaneando;
        }

        // ------------------------------------------------------------------ bucle

        void Update()
        {
            switch (FaseActual)
            {
                case Fase.Escaneando:
                case Fase.Apuntando:
                    ActualizarApuntado();
                    break;
                case Fase.Encuentro:
                    hud?.Mensaje(intentos == 0 && toques == 0
                        ? "Tócala para que reaccione.\nDesliza el amuleto hacia arriba para capturarla."
                        : null);
                    DetectarToqueCriatura();
                    break;
            }
            if (lanzador != null) lanzador.Activo = FaseActual == Fase.Encuentro;
        }

        void ActualizarApuntado()
        {
            bool rastreando = ARSession.state == ARSessionState.SessionTracking;

            if (modo == ModoSuperficie.Palma)
            {
                reticula.Ocultar();
                FaseActual = rastreando ? Fase.Apuntando : Fase.Escaneando;
                hud?.Mensaje(rastreando
                    ? "Abre tu mano frente a la cámara y toca la pantalla: la criatura aparecerá sobre tu palma."
                    : "Mueve el celular despacio para empezar…");
                if (rastreando && TocoPantalla(out _))
                {
                    var cam = camara.transform;
                    var adelante = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                    if (adelante.sqrMagnitude < 0.01f) adelante = cam.up;
                    // Un poco por debajo del centro de la vista, donde suele quedar la palma.
                    var pos = cam.position + cam.forward * distanciaPalma - Vector3.up * 0.06f;
                    StartCoroutine(Invocar(new Pose(pos, Quaternion.LookRotation(-adelante)), TipoSuperficie.Palma));
                }
                return;
            }

            hayObjetivo = rastreando && BuscarSuperficie(out poseObjetivo, out tipoObjetivo);
            if (hayObjetivo)
            {
                FaseActual = Fase.Apuntando;
                reticula.Mostrar(poseObjetivo, tipoObjetivo, camara.transform.position);
                hud?.Mensaje(tipoObjetivo == TipoSuperficie.Mesa
                    ? "Mesa encontrada. Toca para invocar a " + Actual.nombre + "."
                    : "Suelo encontrado. Toca para invocar a " + Actual.nombre + ".");
                if (TocoPantalla(out _))
                    StartCoroutine(Invocar(poseObjetivo, tipoObjetivo));
            }
            else
            {
                FaseActual = Fase.Escaneando;
                reticula.Ocultar();
                string donde = modo == ModoSuperficie.Mesa ? "una mesa" : modo == ModoSuperficie.Suelo ? "el suelo" : "el suelo o una mesa";
                hud?.Mensaje(rastreando
                    ? "Apunta a " + donde + " y mueve el celular despacio…"
                    : "Mueve el celular despacio para reconocer el espacio…");
            }
        }

        // Un toque corto (sin arrastrar) sobre la criatura la hace reaccionar.
        Vector2 toqueInicio; float toqueT; bool toqueActivo; int toques;
        void DetectarToqueCriatura()
        {
            if (Instancia == null) return;
            var e = EntradaTactil.Leer();
            if (e.empezo && (hud == null || !hud.TocaInterfaz(e.posicion)))
            {
                toqueInicio = e.posicion; toqueT = Time.time; toqueActivo = true;
            }
            if (!toqueActivo || !e.termino) return;
            toqueActivo = false;
            if ((e.posicion - toqueInicio).magnitude > Screen.height * 0.02f || Time.time - toqueT > 0.4f) return;
            var rayo = camara.ScreenPointToRay(e.posicion);
            if (Instancia.Colision != null && Instancia.Colision.Raycast(rayo, out _, 50f))
            {
                var r = Instancia.Reaccionar();
                if (r != null) { toques++; hud?.Valoracion(r == "Saludar" ? "¡Hola!" : r == "Gritar" ? "¡Aaargh!" : "¡Buu!"); }
            }
        }

        bool TocoPantalla(out Vector2 pos)
        {
            var e = EntradaTactil.Leer();
            pos = e.posicion;
            return e.empezo && (hud == null || !hud.TocaInterfaz(e.posicion));
        }

        // ------------------------------------------------------------------ superficies

        bool BuscarSuperficie(out Pose pose, out TipoSuperficie tipo)
        {
            pose = default;
            tipo = TipoSuperficie.Suelo;
            var centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
            if (raycaster == null || !raycaster.Raycast(centro, impactos, TrackableType.PlaneWithinPolygon))
                return false;

            foreach (var h in impactos)
            {
                var plano = planos != null ? planos.GetPlane(h.trackableId) : null;
                if (plano == null || plano.alignment != PlaneAlignment.HorizontalUp) continue;
                var t = Clasificar(h.pose.position.y);
                if (modo == ModoSuperficie.Suelo && t != TipoSuperficie.Suelo) continue;
                if (modo == ModoSuperficie.Mesa && t != TipoSuperficie.Mesa) continue;
                pose = h.pose;
                tipo = t;
                return true;
            }
            return false;
        }

        TipoSuperficie Clasificar(float y)
        {
            float suelo = y;
            if (planos != null)
                foreach (var p in planos.trackables)
                    if (p.alignment == PlaneAlignment.HorizontalUp && p.trackingState == TrackingState.Tracking)
                        suelo = Mathf.Min(suelo, p.transform.position.y);

            bool masAltaQueSuelo = y - suelo > alturaMinimaMesa;
            bool cercaDelCelular = camara.transform.position.y - y < distanciaMaximaMesa;
            return masAltaQueSuelo || cercaDelCelular ? TipoSuperficie.Mesa : TipoSuperficie.Suelo;
        }

        // ------------------------------------------------------------------ invocar

        IEnumerator Invocar(Pose pose, TipoSuperficie tipo)
        {
            FaseActual = Fase.Apareciendo;
            reticula.Ocultar();
            hud?.Mensaje(null);
            hud?.PermitirOtra(false);
            intentos = 0;

            // En palma la oclusión taparía los pies con la mano: se apaga.
            if (oclusion != null)
                oclusion.enabled = tipo != TipoSuperficie.Palma;
            if (ocultarPlanosAlInvocar) MostrarPlanos(false);

            float altura = Actual.AlturaPara(tipo);

            // Raíz anclada al mundo (el ancla AR la mantiene en su sitio).
            var raiz = new GameObject("Criatura_" + Actual.id);
            var mirar = Vector3.ProjectOnPlane(camara.transform.position - pose.position, Vector3.up);
            raiz.transform.SetPositionAndRotation(pose.position,
                mirar.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(mirar) : Quaternion.identity);
            raiz.AddComponent<ARAnchor>();

            // Pivote: gira hacia la cámara sin pelear con el ancla.
            var pivote = new GameObject("Pivote").transform;
            pivote.SetParent(raiz.transform, false);

            var modelo = Instantiate(Actual.modelo, pivote);
            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.Euler(0f, Actual.giroY, 0f);
            modelo.transform.localScale = Vector3.one * altura;
            foreach (var c in modelo.GetComponentsInChildren<Collider>()) c.enabled = false;

            Instancia = pivote.gameObject.AddComponent<CriaturaViva>();
            Instancia.Iniciar(Actual, modelo.transform, altura, tipo, materialBrillo, materialSombra, camara);

            yield return new WaitForSeconds(0.9f);
            anillo = AnilloCaptura.Crear(Instancia, materialBrillo, camara);
            hud?.PermitirOtra(true);
            FaseActual = Fase.Encuentro;
        }

        // ------------------------------------------------------------------ captura

        IEnumerator SecuenciaCaptura(Amuleto amuleto, CriaturaViva criatura)
        {
            if (FaseActual != Fase.Encuentro || criatura != Instancia) { Destroy(amuleto.gameObject); yield break; }
            FaseActual = Fase.Capturando;
            intentos++;

            float ajuste = anillo != null ? anillo.Ajuste : 0f;
            string val = AnilloCaptura.Valoracion(ajuste);
            if (val != null) hud?.Valoracion(val);
            if (anillo != null) anillo.Mostrar(false);

            // Probabilidad: base de la criatura × bonificación del aro (hasta ×2).
            float prob = Mathf.Clamp01(Actual.facilidadCaptura * (1f + ajuste) * (1f + 0.08f * (intentos - 1)));
            float porSacudida = Mathf.Pow(prob, 1f / 3f);

            // El amuleto rebota hacia atrás y queda flotando.
            var am = amuleto.transform;
            var p0 = am.position;
            var p1 = p0 + (camara.transform.position - p0).normalized * criatura.Altura * 0.25f + Vector3.up * criatura.Altura * 0.15f;
            yield return Mover(am, p0, p1, 0.25f);

            yield return criatura.Absorber(am.position);

            // Cae hasta la base de la criatura.
            var suelo = new Vector3(am.position.x, criatura.transform.position.y + amuleto.transform.localScale.y * 0.5f, am.position.z);
            yield return Mover(am, am.position, suelo, 0.35f);

            // Tres sacudidas.
            bool escapo = false;
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.35f);
                yield return Sacudir(am, Vector3.ProjectOnPlane(camara.transform.forward, Vector3.up));
                if (Random.value > porSacudida) { escapo = true; break; }
            }
            yield return new WaitForSeconds(0.3f);

            if (escapo)
            {
                Destroy(amuleto.gameObject);
                yield return criatura.Liberar(am.position);
                if (Random.value < Actual.probabilidadHuir)
                {
                    hud?.Valoracion("¡Huyó!");
                    yield return criatura.Irse();
                    Instancia = null;
                    yield return new WaitForSeconds(0.6f);
                    hud?.MostrarResultado(Actual.nombre + " huyó",
                        "Se escabulló de vuelta al monte. Busca otra superficie para encontrar otra criatura.",
                        () => PrepararSiguiente(true));
                    FaseActual = Fase.Resultado;
                }
                else
                {
                    hud?.Valoracion("¡Se escapó!");
                    if (anillo != null) anillo.Mostrar(true);
                    FaseActual = Fase.Encuentro;
                }
                yield break;
            }

            // ¡Capturada!
            yield return Destello(am);
            bool nueva = !ColeccionAR.Capturada(Actual.id);
            ColeccionAR.Capturar(Actual.id);
            Destroy(amuleto.gameObject);
            if (Instancia != null) Destroy(Instancia.transform.parent.gameObject);
            Instancia = null;
            RefrescarHud();

            FaseActual = Fase.Resultado;
            hud?.MostrarResultado("¡Capturaste a " + Actual.nombre + "!",
                (nueva ? "Nueva leyenda en tu colección.\n\n" : "") + Actual.descripcion,
                () => PrepararSiguiente(true));
        }

        IEnumerator Mover(Transform t, Vector3 a, Vector3 b, float dur)
        {
            for (float k = 0; k < dur; k += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, k / dur);
                t.position = Vector3.Lerp(a, b, e);
                yield return null;
            }
            t.position = b;
        }

        IEnumerator Sacudir(Transform t, Vector3 adelante)
        {
            var eje = Vector3.Cross(Vector3.up, adelante.sqrMagnitude > 0 ? adelante.normalized : Vector3.forward);
            var r0 = t.rotation;
            float dur = 0.5f;
            for (float k = 0; k < dur; k += Time.deltaTime)
            {
                float ang = Mathf.Sin(k / dur * Mathf.PI * 2f) * 28f * (1f - k / dur * 0.3f);
                t.rotation = Quaternion.AngleAxis(ang, eje) * r0;
                yield return null;
            }
            t.rotation = r0;
        }

        IEnumerator Destello(Transform t)
        {
            var s0 = t.localScale;
            for (float k = 0; k < 0.45f; k += Time.deltaTime)
            {
                float e = Mathf.Sin(k / 0.45f * Mathf.PI);
                t.localScale = s0 * (1f + e * 0.6f);
                yield return null;
            }
            t.localScale = s0;
        }

        // ------------------------------------------------------------------ acciones

        void CambiarModo(ModoSuperficie nuevo)
        {
            modo = nuevo;
            // Cambiar de superficie con una criatura en pantalla: se despide y se vuelve a apuntar.
            if (FaseActual == Fase.Encuentro && Instancia != null)
                StartCoroutine(DespedirYApuntar(Actual));
        }

        void OtraCriatura()
        {
            if (FaseActual == Fase.Capturando || FaseActual == Fase.Apareciendo) return;
            var siguiente = catalogo != null ? catalogo.Aleatoria(Actual) : Actual;
            StartCoroutine(DespedirYApuntar(siguiente));
        }

        IEnumerator DespedirYApuntar(CriaturaAR siguiente)
        {
            lanzador?.Cancelar();
            if (Instancia != null)
            {
                FaseActual = Fase.Apareciendo; // bloquea entradas mientras se va
                var c = Instancia;
                Instancia = null;
                yield return c.Irse();
            }
            Actual = siguiente;
            RefrescarHud();
            MostrarPlanos(true);
            if (oclusion != null) oclusion.enabled = true;
            FaseActual = Fase.Escaneando;
        }

        void PrepararSiguiente(bool cambiarCriatura)
        {
            if (cambiarCriatura && catalogo != null) Actual = catalogo.Aleatoria(Actual);
            RefrescarHud();
            MostrarPlanos(true);
            if (oclusion != null) oclusion.enabled = true;
            FaseActual = Fase.Escaneando;
        }

        /// <summary>Muestra u oculta los puntos de los planos (el material compartido tiene _Visible).</summary>
        void MostrarPlanos(bool si)
        {
            if (planos == null || planos.planePrefab == null) return;
            var r = planos.planePrefab.GetComponent<MeshRenderer>();
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Visible"))
                r.sharedMaterial.SetFloat("_Visible", si ? 1f : 0f);
        }

        void OnDestroy() { MostrarPlanos(true); }

        void RefrescarHud()
        {
            if (hud == null) return;
            int total = catalogo != null ? catalogo.criaturas.Count : 0;
            int capturadas = catalogo != null ? catalogo.CantidadCapturadas() : 0;
            hud.MostrarCriatura(Actual, capturadas, total);
        }
    }
}
#endif
