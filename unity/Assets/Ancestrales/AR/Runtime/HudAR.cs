using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ancestrales.AR
{
    /// <summary>
    /// Interfaz del modo AR (UI Toolkit, construida por código).
    /// Estilos en Resources/HudAR.uss, con la paleta de Ancestrales.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudAR : MonoBehaviour
    {
        public event Action<ModoSuperficie> AlCambiarModo;
        public event Action AlPedirOtra;
        public event Action AlVolver;

        [Tooltip("Escena a la que vuelve el botón ✕. Vacío = se oculta el botón.")]
        public string escenaMenu = "";

        VisualElement raiz, panelResultado;
        Label titulo, subtitulo, contador, mensaje, valoracion, resTitulo, resTexto;
        Button btnVolver, btnOtra;
        readonly Button[] btnModos = new Button[4];
        Action alSeguir;
        IVisualElementScheduledItem ocultarValoracion;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;
            root.Clear();
            var estilos = Resources.Load<StyleSheet>("HudAR");
            if (estilos != null) root.styleSheets.Add(estilos);

            raiz = Nuevo<VisualElement>("ar-raiz", root);
            raiz.pickingMode = PickingMode.Ignore;
            AplicarZonaSegura();

            // Arriba
            var barra = Nuevo<VisualElement>("ar-barra", raiz);
            var textos = Nuevo<VisualElement>("ar-textos", barra);
            titulo = Nuevo<Label>("ar-titulo", textos);
            subtitulo = Nuevo<Label>("ar-subtitulo", textos);
            contador = Nuevo<Label>("ar-contador", barra);
            btnVolver = Boton("✕", "ar-boton-redondo", barra, () =>
            {
                AlVolver?.Invoke();
                if (!string.IsNullOrEmpty(escenaMenu)) UnityEngine.SceneManagement.SceneManager.LoadScene(escenaMenu);
            });
            btnVolver.style.display = string.IsNullOrEmpty(escenaMenu) ? DisplayStyle.None : DisplayStyle.Flex;

            // Centro
            var centro = Nuevo<VisualElement>("ar-centro", raiz);
            valoracion = Nuevo<Label>("ar-valoracion", centro);
            valoracion.style.opacity = 0f;
            mensaje = Nuevo<Label>("ar-mensaje", centro);

            // Abajo
            var abajo = Nuevo<VisualElement>("ar-abajo", raiz);
            var acciones = Nuevo<VisualElement>("ar-acciones", abajo);
            btnOtra = Boton("Otra criatura", "ar-boton", acciones, () => AlPedirOtra?.Invoke());
            Boton("Foto", "ar-boton", acciones, () => StartCoroutine(TomarFoto()));
            var modos = Nuevo<VisualElement>("ar-modos", abajo);
            string[] nombres = { "Auto", "Suelo", "Mesa", "Palma" };
            for (int i = 0; i < 4; i++)
            {
                var m = (ModoSuperficie)i;
                btnModos[i] = Boton(nombres[i], "ar-modo", modos, () => { MarcarModo(m); AlCambiarModo?.Invoke(m); });
            }

            // Resultado (captura)
            panelResultado = Nuevo<VisualElement>("ar-resultado", root);
            var tarjeta = Nuevo<VisualElement>("ar-tarjeta", panelResultado);
            resTitulo = Nuevo<Label>("ar-res-titulo", tarjeta);
            resTexto = Nuevo<Label>("ar-res-texto", tarjeta);
            Boton("Seguir explorando", "ar-boton", tarjeta, () =>
            {
                panelResultado.style.display = DisplayStyle.None;
                var a = alSeguir; alSeguir = null; a?.Invoke();
            });
            panelResultado.pickingMode = PickingMode.Position;
            panelResultado.style.display = DisplayStyle.None;

            root.RegisterCallback<GeometryChangedEvent>(_ => AplicarZonaSegura());
        }

        void AplicarZonaSegura()
        {
            if (raiz == null) return;
            var z = Screen.safeArea;
            float esc = 1f;
            if (raiz.panel != null && raiz.panel.visualTree.layout.height > 0)
                esc = raiz.panel.visualTree.layout.height / Screen.height;
            raiz.style.paddingTop = (Screen.height - z.yMax) * esc + 8;
            raiz.style.paddingBottom = z.yMin * esc + 8;
            raiz.style.paddingLeft = z.xMin * esc + 8;
            raiz.style.paddingRight = (Screen.width - z.xMax) * esc + 8;
        }

        T Nuevo<T>(string clase, VisualElement padre) where T : VisualElement, new()
        {
            var e = new T();
            e.AddToClassList(clase);
            padre.Add(e);
            if (!(e is Button)) e.pickingMode = PickingMode.Ignore;
            return e;
        }

        Button Boton(string texto, string clase, VisualElement padre, Action accion)
        {
            var b = new Button(accion) { text = texto };
            b.AddToClassList(clase);
            padre.Add(b);
            return b;
        }

        // ---------- API para el gestor ----------

        public void MostrarCriatura(CriaturaAR c, int capturadas, int total)
        {
            if (titulo == null) return;
            titulo.text = c != null ? c.nombre : "";
            subtitulo.text = c != null && ColeccionAR.Capturada(c.id) ? "Ya en tu colección" : "Leyenda por descubrir";
            contador.text = $"{capturadas}/{total}";
        }

        public void Mensaje(string texto)
        {
            if (mensaje == null) return;
            mensaje.text = texto ?? "";
            mensaje.style.display = string.IsNullOrEmpty(texto) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Valoracion(string texto)
        {
            if (valoracion == null || string.IsNullOrEmpty(texto)) return;
            valoracion.text = texto;
            valoracion.style.opacity = 1f;
            ocultarValoracion?.Pause();
            ocultarValoracion = valoracion.schedule.Execute(() => valoracion.style.opacity = 0f).StartingIn(1100);
        }

        public void MarcarModo(ModoSuperficie modo)
        {
            for (int i = 0; i < btnModos.Length; i++)
                btnModos[i]?.EnableInClassList("ar-modo--activo", i == (int)modo);
        }

        public void MostrarResultado(string tituloTexto, string texto, Action alContinuar)
        {
            resTitulo.text = tituloTexto;
            resTexto.text = texto;
            alSeguir = alContinuar;
            panelResultado.style.display = DisplayStyle.Flex;
        }

        public void PermitirOtra(bool si) { btnOtra?.SetEnabled(si); }

        /// <summary>¿El punto de pantalla (coordenadas de Input) cae sobre un botón?</summary>
        public bool TocaInterfaz(Vector2 posPantalla)
        {
            if (raiz == null || raiz.panel == null) return false;
            if (panelResultado.resolvedStyle.display == DisplayStyle.Flex) return true;
            var p = RuntimePanelUtils.ScreenToPanel(raiz.panel, new Vector2(posPantalla.x, Screen.height - posPantalla.y));
            var elegido = raiz.panel.Pick(p);
            return elegido != null && elegido.pickingMode == PickingMode.Position && elegido != raiz && elegido != raiz.parent;
        }

        IEnumerator TomarFoto()
        {
            raiz.style.display = DisplayStyle.None;
            yield return new WaitForEndOfFrame();
            string nombre = "Ancestrales_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(nombre);
            yield return null;
            yield return new WaitForEndOfFrame();
            raiz.style.display = DisplayStyle.Flex;
            Valoracion("Foto guardada");
            Debug.Log("[Ancestrales AR] Foto guardada en " + System.IO.Path.Combine(Application.persistentDataPath, nombre));
        }
    }
}
