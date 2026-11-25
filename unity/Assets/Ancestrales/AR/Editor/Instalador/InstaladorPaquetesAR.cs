using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Ancestrales.AR.Editor
{
    /// <summary>
    /// Va en su propio ensamblado para funcionar aunque AR Foundation aún no esté instalado.
    /// </summary>
    public static class InstaladorPaquetesAR
    {
        static readonly string[] Paquetes =
        {
            "com.unity.xr.arfoundation",
            "com.unity.xr.arcore",
            "com.unity.xr.arkit",
            "com.unity.cloud.gltfast",
        };

        static AddAndRemoveRequest pedido;

        [MenuItem("Ancestrales/AR/0. Instalar paquetes (AR Foundation, ARCore, ARKit, glTFast)", priority = 0)]
        public static void Instalar()
        {
            if (pedido != null && !pedido.IsCompleted) { Debug.Log("[Ancestrales AR] Ya se están instalando los paquetes…"); return; }
            pedido = Client.AddAndRemove(Paquetes, null);
            EditorApplication.update += Vigilar;
            Debug.Log("[Ancestrales AR] Instalando: " + string.Join(", ", Paquetes) + ". Si Unity pregunta por el nuevo Input System, luego pon Player > Active Input Handling en \"Both\" para no romper los controles del runner.");
        }

        static void Vigilar()
        {
            if (pedido == null || !pedido.IsCompleted)
            {
                EditorUtility.DisplayProgressBar("Ancestrales AR", "Instalando paquetes de realidad aumentada…", 0.5f);
                return;
            }
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= Vigilar;
            if (pedido.Status == StatusCode.Success)
                EditorUtility.DisplayDialog("Ancestrales AR",
                    "Paquetes instalados.\n\nAhora:\n1) Edit > Project Settings > XR Plug-in Management: marca ARCore (Android) y ARKit (iOS).\n2) Menú Ancestrales > AR > Configurar todo.", "Listo");
            else
                Debug.LogError("[Ancestrales AR] No se pudieron instalar los paquetes: " + pedido.Error?.message);
            pedido = null;
        }
    }
}
