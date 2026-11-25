using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>Texturas pequeñas generadas por código (no hay que importar imágenes).</summary>
    public static class TexturasProcedurales
    {
        static Texture2D radial, anillo, blanca;

        /// <summary>Degradado circular: opaco en el centro, transparente en el borde.</summary>
        public static Texture2D Radial()
        {
            if (radial != null) return radial;
            radial = Crear(128, (d) =>
            {
                float a = Mathf.Clamp01(1f - d);
                return a * a * (3f - 2f * a);
            });
            return radial;
        }

        /// <summary>Aro con borde suave (retícula y aura).</summary>
        public static Texture2D Anillo()
        {
            if (anillo != null) return anillo;
            anillo = Crear(256, (d) =>
            {
                float aro = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.82f) / 0.08f);
                float centro = d < 0.12f ? 1f - d / 0.12f : 0f;
                return Mathf.Max(aro, centro * 0.8f);
            });
            return anillo;
        }

        public static Texture2D Blanca()
        {
            if (blanca != null) return blanca;
            blanca = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color32[4];
            for (int i = 0; i < 4; i++) px[i] = new Color32(255, 255, 255, 255);
            blanca.SetPixels32(px);
            blanca.Apply();
            return blanca;
        }

        static Texture2D Crear(int tam, System.Func<float, float> alfa)
        {
            var t = new Texture2D(tam, tam, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[tam * tam];
            float c = (tam - 1) * 0.5f;
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    byte a = (byte)(Mathf.Clamp01(alfa(d)) * 255f);
                    px[y * tam + x] = new Color32(255, 255, 255, a);
                }
            t.SetPixels32(px);
            t.Apply(true);
            return t;
        }

        /// <summary>Quad plano (acostado sobre el piso) con el material dado.</summary>
        public static GameObject QuadPlano(string nombre, Transform padre, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = nombre;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(padre, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }
}
