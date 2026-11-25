using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>
    /// Datos de una criatura que se puede invocar en realidad aumentada.
    /// Los modelos vienen normalizados a 1 m de alto, con el origen en los pies,
    /// así que "altura" es directamente el alto final en metros.
    /// </summary>
    [CreateAssetMenu(menuName = "Ancestrales/AR/Criatura", fileName = "CriaturaAR")]
    public class CriaturaAR : ScriptableObject
    {
        [Tooltip("Mismo id que usa la interfaz del juego: silbon, patasola, mohan, hojarasquin, madremonte...")]
        public string id;
        public string nombre;
        [TextArea(2, 4)] public string descripcion;

        [Tooltip("Modelo (el .glb importado con glTFast, o un prefab con animaciones).")]
        public GameObject modelo;

        [Header("Tamaño según la superficie (metros)")]
        public float alturaSuelo = 1.6f;
        public float alturaMesa = 0.28f;
        public float alturaPalma = 0.12f;

        [Header("Ajustes")]
        [Tooltip("Si la criatura aparece de espaldas, pon 180.")]
        public float giroY = 0f;
        [Range(0.05f, 1f)]
        [Tooltip("1 = muy fácil de capturar, 0.1 = muy difícil.")]
        public float facilidadCaptura = 0.45f;
        [Range(0f, 0.6f)]
        [Tooltip("Probabilidad de huir cada vez que se libera del amuleto.")]
        public float probabilidadHuir = 0.15f;
        public Color colorAura = new Color(0.55f, 1f, 0.7f, 1f);

        public float AlturaPara(TipoSuperficie tipo)
        {
            switch (tipo)
            {
                case TipoSuperficie.Mesa: return alturaMesa;
                case TipoSuperficie.Palma: return alturaPalma;
                default: return alturaSuelo;
            }
        }
    }

    public enum TipoSuperficie { Suelo, Mesa, Palma }

    public enum ModoSuperficie { Auto, Suelo, Mesa, Palma }
}
