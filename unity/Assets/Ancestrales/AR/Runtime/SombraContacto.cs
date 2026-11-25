using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Sombra suave bajo los pies. Es lo que más ayuda a que la criatura parezca
    /// "parada" sobre la mesa o el suelo y no flotando.
    /// </summary>
    public class SombraContacto : MonoBehaviour
    {
        public float opacidad = 0.55f;
        Material mat;
        float intensidad = 1f;

        public float Intensidad
        {
            get => intensidad;
            set
            {
                intensidad = Mathf.Clamp01(value);
                if (mat != null) mat.color = new Color(0f, 0f, 0f, opacidad * intensidad);
            }
        }

        public static SombraContacto Crear(Transform padre, float ancho, float fondo, Material baseMat)
        {
            if (baseMat == null) return null;
            var m = new Material(baseMat) { mainTexture = TexturasProcedurales.Radial() };
            var go = TexturasProcedurales.QuadPlano("Sombra", padre, m);
            go.transform.localPosition = Vector3.up * 0.002f;
            float d = Mathf.Max(ancho, fondo) * 1.1f;
            go.transform.localScale = new Vector3(d, d, 1f);
            var s = go.AddComponent<SombraContacto>();
            s.mat = m;
            s.Intensidad = 1f;
            return s;
        }
    }
}
