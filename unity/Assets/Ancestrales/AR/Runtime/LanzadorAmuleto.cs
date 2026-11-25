using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Desliza el dedo hacia arriba para lanzar el amuleto.
    /// Tiene puntería asistida: un lanzamiento recto y con buena fuerza llega a la criatura;
    /// si el dedo se va de lado o con poca/mucha fuerza, el amuleto se desvía en proporción.
    /// </summary>
    public class LanzadorAmuleto : MonoBehaviour
    {
        [Header("Materiales (los asigna el menú de configuración)")]
        public Material materialAmuleto;
        public Material materialEstela;

        [Header("Ajustes")]
        public float radioAmuleto = 0.022f;
        [Tooltip("Velocidad del dedo (en alturas de pantalla por segundo) que se considera un lanzamiento perfecto.")]
        public float fuerzaIdeal = 2.4f;
        [Tooltip("Velocidad mínima del dedo para que cuente como lanzamiento.")]
        public float fuerzaMinima = 0.7f;
        [Range(0f, 1f)] public float ayudaPunteria = 0.6f;

        public bool Activo { get; set; }
        public Func<CriaturaViva> Objetivo;
        public Func<Vector2, bool> TocaInterfaz;
        public event Action<Amuleto> AlLanzar;
        public event Action<Amuleto, CriaturaViva> AlGolpear;
        public event Action AlFallar;

        Camera cam;
        Amuleto enMano;
        bool arrastrando;
        readonly List<(float t, Vector2 p)> muestras = new List<(float, Vector2)>();
        Vector3 posReposo => new Vector3(0f, -0.07f, 0.22f);
        Amuleto enVuelo;
        float tiempoVuelo;

        public void Configurar(Camera camara) { cam = camara; }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            bool mostrar = Activo && enVuelo == null;
            if (mostrar && enMano == null) PrepararNuevo();
            if (enMano != null) enMano.gameObject.SetActive(mostrar);

            VigilarVuelo();
            if (!mostrar || enMano == null) { arrastrando = false; return; }

            var e = EntradaTactil.Leer();
            if (e.empezo && e.posicion.y < Screen.height * 0.5f && (TocaInterfaz == null || !TocaInterfaz(e.posicion)))
            {
                arrastrando = true;
                muestras.Clear();
            }

            if (arrastrando)
            {
                muestras.Add((Time.time, e.posicion));
                while (muestras.Count > 2 && Time.time - muestras[0].t > 0.12f) muestras.RemoveAt(0);

                // El amuleto sigue al dedo, a 22 cm de la cámara.
                var mundo = cam.ScreenToWorldPoint(new Vector3(e.posicion.x, e.posicion.y, posReposo.z));
                enMano.transform.position = Vector3.Lerp(enMano.transform.position, mundo, 0.5f);

                if (e.termino || !e.presionado)
                {
                    arrastrando = false;
                    if (!IntentarLanzar()) VolverAReposo();
                }
            }
            else
            {
                // Flota suavemente en reposo.
                var lp = posReposo + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.004f;
                enMano.transform.localPosition = Vector3.Lerp(enMano.transform.localPosition, lp, 10f * Time.deltaTime);
                enMano.transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);
            }
        }

        void PrepararNuevo()
        {
            enMano = Amuleto.Crear(materialAmuleto, materialEstela, radioAmuleto);
            enMano.transform.SetParent(cam.transform, false);
            enMano.transform.localPosition = posReposo + Vector3.down * 0.1f;
        }

        void VolverAReposo()
        {
            if (enMano == null) return;
            enMano.transform.SetParent(cam.transform, true);
        }

        bool IntentarLanzar()
        {
            var objetivo = Objetivo != null ? Objetivo() : null;
            if (objetivo == null || muestras.Count < 2) return false;

            var a = muestras[0];
            var b = muestras[muestras.Count - 1];
            float dt = Mathf.Max(0.016f, b.t - a.t);
            Vector2 v = (b.p - a.p) / dt / Screen.height;   // alturas de pantalla por segundo
            if (v.y < fuerzaMinima) return false;

            float lateral = Mathf.Clamp(v.x / v.y, -1f, 1f);          // 0 = recto
            float fuerza = Mathf.Clamp(v.y / fuerzaIdeal, 0.4f, 1.8f); // 1 = perfecto

            var inicio = enMano.transform.position;
            var destino = objetivo.Centro;
            var hacia = destino - inicio;
            float dist = hacia.magnitude;

            // Error del jugador, reducido por la ayuda de puntería.
            var derecha = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            float tolerancia = 1f - ayudaPunteria;
            float errLateral = (Mathf.Abs(lateral) < 0.12f ? 0f : lateral) * dist * 0.9f * (0.35f + tolerancia);
            float errFuerza = (Mathf.Abs(fuerza - 1f) < 0.25f ? 0f : (fuerza - 1f)) * objetivo.Altura * 1.6f * (0.35f + tolerancia);
            destino += derecha * errLateral + Vector3.up * errFuerza;

            // Tiro parabólico que llega al destino en un tiempo que depende de la distancia.
            float T = Mathf.Clamp(0.28f + dist * 0.22f, 0.3f, 0.95f);
            var g = Physics.gravity;
            var vel = (destino - inicio - 0.5f * g * T * T) / T;
            if (fuerza > 1.25f) vel *= 1f + (fuerza - 1.25f) * 0.5f; // pasarse de fuerza: se va largo

            var lanzado = enMano;
            enMano = null;
            lanzado.AlGolpear += (am, cr) => { enVuelo = null; AlGolpear?.Invoke(am, cr); };
            lanzado.Lanzar(vel, cam.transform.right * -15f);
            enVuelo = lanzado;
            tiempoVuelo = 0f;
            AlLanzar?.Invoke(lanzado);
            return true;
        }

        void VigilarVuelo()
        {
            if (enVuelo == null) return;
            if (enVuelo.Usado) { enVuelo = null; return; }
            tiempoVuelo += Time.deltaTime;
            var obj = Objetivo != null ? Objetivo() : null;
            if (obj != null && Vector3.Distance(enVuelo.transform.position, obj.Centro) < obj.Altura * 0.6f)
                obj.Sobresalto();
            if (tiempoVuelo > 2.2f || enVuelo.transform.position.y < cam.transform.position.y - 4f)
            {
                Destroy(enVuelo.gameObject);
                enVuelo = null;
                AlFallar?.Invoke();
            }
        }

        public void Cancelar()
        {
            if (enVuelo != null) { Destroy(enVuelo.gameObject); enVuelo = null; }
            arrastrando = false;
        }
    }
}
