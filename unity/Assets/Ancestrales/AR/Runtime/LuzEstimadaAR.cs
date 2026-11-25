#if ANCESTRALES_ARF
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

namespace Ancestrales.AR
{
    /// <summary>
    /// Ajusta la luz de la escena a la luz real que ve la cámara,
    /// para que la criatura no se vea "pegada" encima del video.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LuzEstimadaAR : MonoBehaviour
    {
        public ARCameraManager camara;
        Light luz;
        float intensidadBase;

        void Awake()
        {
            luz = GetComponent<Light>();
            intensidadBase = luz.intensity;
        }

        void OnEnable() { if (camara != null) camara.frameReceived += AlRecibirFrame; }
        void OnDisable() { if (camara != null) camara.frameReceived -= AlRecibirFrame; }

        void AlRecibirFrame(ARCameraFrameEventArgs args)
        {
            var est = args.lightEstimation;

            if (est.averageBrightness.HasValue)
                luz.intensity = intensidadBase * Mathf.Clamp(est.averageBrightness.Value * 2f, 0.3f, 1.6f);
            else if (est.averageMainLightBrightness.HasValue)
                luz.intensity = intensidadBase * Mathf.Clamp(est.averageMainLightBrightness.Value * 2f, 0.3f, 1.6f);

            if (est.colorCorrection.HasValue)
                luz.color = est.colorCorrection.Value;
            else if (est.averageColorTemperature.HasValue)
            {
                luz.useColorTemperature = true;
                luz.colorTemperature = est.averageColorTemperature.Value;
            }

            if (est.mainLightDirection.HasValue)
                transform.rotation = Quaternion.LookRotation(est.mainLightDirection.Value);
            if (est.mainLightColor.HasValue)
                luz.color = est.mainLightColor.Value;

            if (est.ambientSphericalHarmonics.HasValue)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = est.ambientSphericalHarmonics.Value;
            }
        }
    }
}
#endif
