using System;
using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>El amuleto que se lanza para capturar (el equivalente a la Pokébola).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Amuleto : MonoBehaviour
    {
        public event Action<Amuleto, CriaturaViva> AlGolpear;
        public Rigidbody Cuerpo { get; private set; }
        public bool Usado { get; private set; }
        TrailRenderer estela;

        public static Amuleto Crear(Material matAmuleto, Material matEstela, float radio)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Amuleto";
            go.transform.localScale = Vector3.one * radio * 2f;
            var r = go.GetComponent<MeshRenderer>();
            if (matAmuleto != null) r.sharedMaterial = matAmuleto;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var a = go.AddComponent<Amuleto>();
            a.Cuerpo = rb;
            if (matEstela != null)
            {
                a.estela = go.AddComponent<TrailRenderer>();
                a.estela.sharedMaterial = new Material(matEstela) { mainTexture = TexturasProcedurales.Blanca() };
                a.estela.time = 0.25f;
                a.estela.widthMultiplier = radio * 1.4f;
                a.estela.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                a.estela.startColor = new Color(1f, 0.85f, 0.4f, 0.8f);
                a.estela.endColor = new Color(1f, 0.6f, 0.2f, 0f);
                a.estela.emitting = false;
                a.estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return a;
        }

        public void Lanzar(Vector3 velocidad, Vector3 giro)
        {
            transform.SetParent(null, true);
            Cuerpo.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
            Cuerpo.linearVelocity = velocidad;
#else
            Cuerpo.velocity = velocidad;
#endif
            Cuerpo.angularVelocity = giro;
            if (estela != null) { estela.Clear(); estela.emitting = true; }
        }

        public void Congelar()
        {
            Cuerpo.isKinematic = true;
            if (estela != null) estela.emitting = false;
        }

        void OnTriggerEnter(Collider otro)
        {
            if (Usado || Cuerpo.isKinematic) return;
            var c = otro.GetComponentInParent<CriaturaViva>();
            if (c == null) return;
            Usado = true;
            Congelar();
            AlGolpear?.Invoke(this, c);
        }
    }
}
