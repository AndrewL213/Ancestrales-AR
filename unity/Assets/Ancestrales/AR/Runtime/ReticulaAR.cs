using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>Aro sobre la superficie detectada que indica dónde aparecerá la criatura.</summary>
    public class ReticulaAR : MonoBehaviour
    {
        public static readonly Color ColorSuelo = new Color(0.55f, 1f, 0.65f, 0.9f);
        public static readonly Color ColorMesa = new Color(1f, 0.8f, 0.35f, 0.9f);

        Material mat;
        Transform quad;
        float tam = 0.3f;

        public static ReticulaAR Crear(Material baseMat)
        {
            var go = new GameObject("Reticula");
            var r = go.AddComponent<ReticulaAR>();
            r.mat = new Material(baseMat) { mainTexture = TexturasProcedurales.Anillo() };
            r.quad = TexturasProcedurales.QuadPlano("Aro", go.transform, r.mat).transform;
            r.quad.localPosition = Vector3.up * 0.004f;
            go.SetActive(false);
            return r;
        }

        public void Mostrar(Pose pose, TipoSuperficie tipo, Vector3 posCamara)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            var adelante = Vector3.ProjectOnPlane(pose.position - posCamara, Vector3.up);
            if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.forward;
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, pose.position, 0.4f),
                Quaternion.LookRotation(adelante, Vector3.up));
            tam = Mathf.Lerp(tam, tipo == TipoSuperficie.Mesa ? 0.14f : 0.32f, 0.2f);
            float pulso = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
            quad.localScale = new Vector3(tam * pulso, tam * pulso, 1f);
            mat.color = tipo == TipoSuperficie.Mesa ? ColorMesa : ColorSuelo;
        }

        public void Ocultar()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
