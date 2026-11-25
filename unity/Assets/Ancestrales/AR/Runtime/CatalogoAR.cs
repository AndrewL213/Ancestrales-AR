using System.Collections.Generic;
using UnityEngine;

namespace Ancestrales.AR
{
    [CreateAssetMenu(menuName = "Ancestrales/AR/Catálogo", fileName = "CatalogoAR")]
    public class CatalogoAR : ScriptableObject
    {
        public List<CriaturaAR> criaturas = new List<CriaturaAR>();

        public CriaturaAR Buscar(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var c in criaturas)
                if (c != null && c.id == id) return c;
            return null;
        }

        /// <summary>Elige al azar, prefiriendo las que aún no se han capturado.</summary>
        public CriaturaAR Aleatoria(CriaturaAR evitar = null)
        {
            var validas = new List<CriaturaAR>();
            var nuevas = new List<CriaturaAR>();
            foreach (var c in criaturas)
            {
                if (c == null || c.modelo == null || c == evitar) continue;
                validas.Add(c);
                if (!ColeccionAR.Capturada(c.id)) nuevas.Add(c);
            }
            var lista = nuevas.Count > 0 ? nuevas : validas;
            if (lista.Count == 0) return evitar;
            return lista[Random.Range(0, lista.Count)];
        }

        public CriaturaAR Siguiente(CriaturaAR actual)
        {
            if (criaturas.Count == 0) return null;
            int i = Mathf.Max(0, criaturas.IndexOf(actual));
            for (int k = 1; k <= criaturas.Count; k++)
            {
                var c = criaturas[(i + k) % criaturas.Count];
                if (c != null && c.modelo != null) return c;
            }
            return actual;
        }

        public int CantidadCapturadas()
        {
            int n = 0;
            foreach (var c in criaturas)
                if (c != null && ColeccionAR.Capturada(c.id)) n++;
            return n;
        }
    }
}
