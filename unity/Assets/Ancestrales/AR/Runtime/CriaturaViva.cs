using System.Collections;
using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Va en el "pivote" de la criatura (hijo del ancla AR). Le da vida sin necesitar animaciones:
    /// aparición, respiración, balanceo, mirar a la cámara, saltitos, absorción y huida.
    /// Si el modelo trae un Animator con controlador, se respeta y solo se hace el giro hacia la cámara.
    /// </summary>
    public class CriaturaViva : MonoBehaviour
    {
        public CriaturaAR Datos { get; private set; }
        public float Altura { get; private set; }
        public TipoSuperficie Superficie { get; private set; }
        public BoxCollider Colision { get; private set; }

        /// <summary>Centro del cuerpo en el mundo (para apuntar el amuleto).</summary>
        public Vector3 Centro => transform.TransformPoint(Colision != null ? Colision.center : Vector3.up * Altura * 0.5f);

        Transform modelo;
        Vector3 escalaBase;
        Quaternion rotacionBase;
        Transform cam;
        Animator animador;
        bool tieneAnimacion;
        bool viva;            // idle activo
        float fase;
        float proximoSalto;
        float saltoT = -1f;
        GameObject aura;
        Material matAura;
        SombraContacto sombra;

        public void Iniciar(CriaturaAR datos, Transform modelo, float altura, TipoSuperficie superficie,
                            Material materialBrillo, Material materialSombra, Camera camara)
        {
            Datos = datos;
            this.modelo = modelo;
            Altura = altura;
            Superficie = superficie;
            cam = camara != null ? camara.transform : null;
            escalaBase = modelo.localScale;
            rotacionBase = modelo.localRotation;
            fase = Random.value * 10f;
            proximoSalto = Time.time + Random.Range(4f, 8f);

            animador = modelo.GetComponentInChildren<Animator>();
            tieneAnimacion = animador != null && animador.runtimeAnimatorController != null;

            CrearColision();
            sombra = SombraContacto.Crear(transform, Colision.size.x, Colision.size.z, materialSombra);

            if (materialBrillo != null)
            {
                matAura = new Material(materialBrillo);
                matAura.mainTexture = TexturasProcedurales.Anillo();
                aura = TexturasProcedurales.QuadPlano("Aura", transform, matAura);
                aura.transform.localPosition = Vector3.up * 0.003f;
            }

            StartCoroutine(Aparecer());
        }

        void CrearColision()
        {
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool primero = true;
            foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            {
                var lb = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var esquina = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var local = transform.InverseTransformPoint(r.transform.TransformPoint(esquina));
                    if (primero) { b = new Bounds(local, Vector3.zero); primero = false; }
                    else b.Encapsulate(local);
                }
            }
            if (primero) b = new Bounds(Vector3.up * Altura * 0.5f, new Vector3(Altura * 0.5f, Altura, Altura * 0.3f));

            Colision = gameObject.AddComponent<BoxCollider>();
            Colision.isTrigger = true;
            // Un poco más grande que el cuerpo para que acertar no sea frustrante.
            Colision.center = b.center;
            Colision.size = new Vector3(Mathf.Max(b.size.x * 0.75f, Altura * 0.35f), b.size.y, Mathf.Max(b.size.z, Altura * 0.3f));
        }

        IEnumerator Aparecer()
        {
            modelo.localScale = Vector3.zero;
            float dur = 0.9f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float s = ElasticoSalida(k);
                modelo.localScale = escalaBase * s;
                if (aura != null)
                {
                    float r = Mathf.Lerp(0.2f, 1.6f, k) * Mathf.Max(Colision.size.x, Colision.size.z);
                    aura.transform.localScale = new Vector3(r, r, 1f);
                    var c = Datos.colorAura; c.a = 1f - k;
                    matAura.color = c;
                }
                yield return null;
            }
            modelo.localScale = escalaBase;
            if (aura != null) aura.SetActive(false);
            viva = true;
        }

        void Update()
        {
            MirarCamara();
            if (!viva || modelo == null) return;

            fase += Time.deltaTime;
            if (!tieneAnimacion)
            {
                // Respiración: estira un poco en Y y compensa en XZ.
                float resp = Mathf.Sin(fase * 1.7f) * 0.012f;
                modelo.localScale = new Vector3(escalaBase.x * (1f - resp * 0.5f), escalaBase.y * (1f + resp), escalaBase.z * (1f - resp * 0.5f));
                // Balanceo suave.
                modelo.localRotation = rotacionBase * Quaternion.Euler(Mathf.Sin(fase * 0.6f) * 1.2f, 0f, Mathf.Sin(fase * 0.9f) * 1.8f);
            }

            // Saltito o sacudida cada tanto.
            if (Time.time > proximoSalto && saltoT < 0f)
            {
                saltoT = 0f;
                proximoSalto = Time.time + Random.Range(5f, 10f);
            }
            if (saltoT >= 0f)
            {
                saltoT += Time.deltaTime / 0.45f;
                float y = Mathf.Sin(Mathf.Clamp01(saltoT) * Mathf.PI) * Altura * 0.07f;
                modelo.localPosition = new Vector3(0f, y, 0f);
                if (sombra != null) sombra.Intensidad = 1f - Mathf.Clamp01(y / (Altura * 0.1f)) * 0.4f;
                if (saltoT >= 1f) { saltoT = -1f; modelo.localPosition = Vector3.zero; }
            }
        }

        void MirarCamara()
        {
            if (cam == null) return;
            var dir = cam.position - transform.position;
            dir = Vector3.ProjectOnPlane(dir, transform.parent != null ? transform.parent.up : Vector3.up);
            if (dir.sqrMagnitude < 1e-4f) return;
            var objetivo = Quaternion.LookRotation(dir, transform.parent != null ? transform.parent.up : Vector3.up);
            // Gira despacio, como si se diera cuenta de que la miras.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, 60f * Time.deltaTime);
        }

        /// <summary>Se encoge y vuela hacia el amuleto.</summary>
        public IEnumerator Absorber(Vector3 punto, float dur = 0.55f)
        {
            viva = false;
            Colision.enabled = false;
            var inicio = modelo.position;
            var escala = modelo.localScale;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float e = k * k;
                modelo.localScale = escala * (1f - e);
                modelo.position = Vector3.Lerp(inicio, punto, e);
                modelo.localRotation = rotacionBase * Quaternion.Euler(0f, 720f * e, 0f);
                if (sombra != null) sombra.Intensidad = 1f - k;
                yield return null;
            }
            modelo.localScale = Vector3.zero;
            if (sombra != null) sombra.Intensidad = 0f;
        }

        /// <summary>Sale del amuleto y vuelve a su sitio.</summary>
        public IEnumerator Liberar(Vector3 desde, float dur = 0.5f)
        {
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = ElasticoSalida(t / dur);
                modelo.localScale = escalaBase * k;
                modelo.position = Vector3.Lerp(desde, transform.position, Mathf.Clamp01(t / dur));
                if (sombra != null) sombra.Intensidad = Mathf.Clamp01(t / dur);
                yield return null;
            }
            modelo.localPosition = Vector3.zero;
            modelo.localRotation = rotacionBase;
            modelo.localScale = escalaBase;
            if (sombra != null) sombra.Intensidad = 1f;
            Colision.enabled = true;
            viva = true;
        }

        /// <summary>Se va (huye o se despide) y se destruye junto con su ancla.</summary>
        public IEnumerator Irse(float dur = 0.6f)
        {
            viva = false;
            if (Colision != null) Colision.enabled = false;
            var escala = modelo.localScale;
            var pos = modelo.localPosition;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                modelo.localScale = escala * (1f - k * k);
                modelo.localPosition = pos + Vector3.up * Altura * 0.4f * k;
                if (sombra != null) sombra.Intensidad = 1f - k;
                yield return null;
            }
            Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
        }

        /// <summary>Pequeño susto cuando el amuleto pasa cerca.</summary>
        public void Sobresalto()
        {
            if (viva && saltoT < 0f) saltoT = 0f;
        }

        static float ElasticoSalida(float k)
        {
            k = Mathf.Clamp01(k);
            if (k == 0f || k == 1f) return k;
            return Mathf.Pow(2f, -10f * k) * Mathf.Sin((k * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }
}
