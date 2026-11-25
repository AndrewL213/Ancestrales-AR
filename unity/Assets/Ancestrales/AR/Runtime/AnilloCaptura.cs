using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Aro que se encoge una y otra vez alrededor de la criatura (como en Pokémon GO).
    /// Acertar cuando el aro está pequeño da más probabilidad de captura.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AnilloCaptura : MonoBehaviour
    {
        public float ciclo = 1.6f;
        const int Puntos = 48;

        LineRenderer linea;
        CriaturaViva criatura;
        Transform cam;
        float radioMax, radioMin, t0;

        /// <summary>0 = aro grande, 1 = aro en su punto más pequeño.</summary>
        public float Ajuste { get; private set; }

        public static AnilloCaptura Crear(CriaturaViva criatura, Material baseMat, Camera camara)
        {
            var go = new GameObject("AnilloCaptura");
            go.transform.SetParent(criatura.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = Puntos;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            if (baseMat != null) lr.sharedMaterial = new Material(baseMat) { mainTexture = TexturasProcedurales.Blanca() };
            var a = go.AddComponent<AnilloCaptura>();
            a.criatura = criatura;
            a.cam = camara != null ? camara.transform : null;
            a.linea = lr;
            a.radioMax = criatura.Altura * 0.42f;
            a.radioMin = criatura.Altura * 0.1f;
            a.t0 = Time.time;
            lr.widthMultiplier = criatura.Altura * 0.012f;
            // Color según dificultad: verde fácil, amarillo medio, rojo difícil.
            float f = criatura.Datos.facilidadCaptura;
            var col = f > 0.55f ? new Color(0.45f, 1f, 0.45f) : f > 0.3f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.3f);
            lr.startColor = lr.endColor = col;
            return a;
        }

        void LateUpdate()
        {
            if (criatura == null) { Destroy(gameObject); return; }
            transform.position = criatura.Centro;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.position);

            float k = ((Time.time - t0) % ciclo) / ciclo;
            Ajuste = k;
            float r = Mathf.Lerp(radioMax, radioMin, k);
            for (int i = 0; i < Puntos; i++)
            {
                float a = i * Mathf.PI * 2f / Puntos;
                linea.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
        }

        public void Mostrar(bool si) { if (linea != null) linea.enabled = si; }

        /// <summary>Texto de valoración según el tamaño del aro al acertar.</summary>
        public static string Valoracion(float ajuste)
        {
            if (ajuste > 0.8f) return "¡Excelente!";
            if (ajuste > 0.55f) return "¡Genial!";
            if (ajuste > 0.3f) return "¡Bien!";
            return null;
        }
    }
}
