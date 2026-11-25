using System;
using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Guarda qué criaturas se han capturado en AR (PlayerPrefs).
    /// Tu interfaz puede suscribirse a AlCapturar para desbloquear cosas en la Colección.
    /// </summary>
    public static class ColeccionAR
    {
        const string Prefijo = "ancestrales_ar_";

        public static event Action<string> AlCapturar;

        public static int Veces(string id) => PlayerPrefs.GetInt(Prefijo + id, 0);
        public static bool Capturada(string id) => Veces(id) > 0;

        public static void Capturar(string id)
        {
            PlayerPrefs.SetInt(Prefijo + id, Veces(id) + 1);
            PlayerPrefs.Save();
            AlCapturar?.Invoke(id);
        }

        public static void Borrar(string id)
        {
            PlayerPrefs.DeleteKey(Prefijo + id);
            PlayerPrefs.Save();
        }
    }
}
