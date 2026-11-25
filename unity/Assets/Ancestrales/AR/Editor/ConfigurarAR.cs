#if ANCESTRALES_ARF
using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

namespace Ancestrales.AR.Editor
{
    /// <summary>Menú Ancestrales > AR: crea los datos, los materiales y la escena del encuentro AR.</summary>
    public static class ConfigurarAR
    {
        struct Def
        {
            public string id, nombre, descripcion;
            public float suelo, mesa, palma, facilidad, huir;
            public Color aura;
        }

        static readonly Def[] Criaturas =
        {
            new Def { id = "silbon", nombre = "El Silbón", suelo = 1.9f, mesa = 0.3f, palma = 0.13f, facilidad = 0.3f, huir = 0.2f, aura = new Color(0.6f, 0.9f, 1f),
                descripcion = "Alma en pena de los Llanos que carga un costal de huesos. Si su silbido suena cerca, está lejos; si suena lejos, está cerca." },
            new Def { id = "patasola", nombre = "La Patasola", suelo = 1.6f, mesa = 0.28f, palma = 0.12f, facilidad = 0.35f, huir = 0.2f, aura = new Color(1f, 0.45f, 0.45f),
                descripcion = "Mujer de una sola pierna que habita la selva y persigue a cazadores y leñadores que se internan solos en el monte." },
            new Def { id = "mohan", nombre = "El Mohán", suelo = 1.5f, mesa = 0.26f, palma = 0.12f, facilidad = 0.45f, huir = 0.15f, aura = new Color(0.4f, 0.75f, 1f),
                descripcion = "Brujo de los ríos, de barba y cabellos largos. Hechiza a los pescadores, enreda sus atarrayas y hace crecer las aguas." },
            new Def { id = "madremonte", nombre = "La Madremonte", suelo = 1.8f, mesa = 0.3f, palma = 0.13f, facilidad = 0.35f, huir = 0.15f, aura = new Color(0.5f, 1f, 0.5f),
                descripcion = "Guardiana del bosque cubierta de musgo y hojas. Castiga a quienes dañan la naturaleza o invaden tierras ajenas." },
            new Def { id = "hojarasquin", nombre = "El Hojarasquín del Monte", suelo = 1.8f, mesa = 0.3f, palma = 0.13f, facilidad = 0.4f, huir = 0.15f, aura = new Color(0.8f, 0.95f, 0.4f),
                descripcion = "Mitad hombre, mitad árbol. Protege el monte y desorienta a los cazadores para salvar a los animales." },
            new Def { id = "hombre_caiman", nombre = "El Hombre Caimán", suelo = 1.7f, mesa = 0.28f, palma = 0.12f, facilidad = 0.5f, huir = 0.1f, aura = new Color(0.5f, 1f, 0.75f),
                descripcion = "Hombre de Plato, Magdalena, que por una pócima mal usada quedó con cuerpo de caimán y vaga por el río." },
        };

        // ------------------------------------------------------------------ rutas

        static string Raiz
        {
            get
            {
                var guid = AssetDatabase.FindAssets("HudAR t:StyleSheet").FirstOrDefault();
                if (guid == null) return "Assets/Ancestrales/AR";
                var uss = AssetDatabase.GUIDToAssetPath(guid);          // .../AR/Resources/HudAR.uss
                return Path.GetDirectoryName(Path.GetDirectoryName(uss)).Replace('\\', '/');
            }
        }

        static string Carpeta(string sub)
        {
            var ruta = Raiz + "/" + sub;
            if (!AssetDatabase.IsValidFolder(ruta)) AssetDatabase.CreateFolder(Raiz, sub);
            return ruta;
        }

        // ------------------------------------------------------------------ menú

        [MenuItem("Ancestrales/AR/Configurar todo (1 + 2 + 3)", priority = 10)]
        public static void Todo()
        {
            CrearDatos();
            AjustarPlayerSettings();
            CrearEscena();
        }

        [MenuItem("Ancestrales/AR/1. Crear datos de criaturas", priority = 21)]
        public static CatalogoAR CrearDatos()
        {
            var datos = Carpeta("Datos");
            var modelos = Raiz + "/Modelos";

            var rutaCat = datos + "/CatalogoAR.asset";
            var cat = AssetDatabase.LoadAssetAtPath<CatalogoAR>(rutaCat);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<CatalogoAR>();
                AssetDatabase.CreateAsset(cat, rutaCat);
            }

            int sinModelo = 0;
            foreach (var d in Criaturas)
            {
                var ruta = datos + "/Criatura_" + d.id + ".asset";
                var c = AssetDatabase.LoadAssetAtPath<CriaturaAR>(ruta);
                if (c == null)
                {
                    c = ScriptableObject.CreateInstance<CriaturaAR>();
                    c.id = d.id; c.nombre = d.nombre; c.descripcion = d.descripcion;
                    c.alturaSuelo = d.suelo; c.alturaMesa = d.mesa; c.alturaPalma = d.palma;
                    c.facilidadCaptura = d.facilidad; c.probabilidadHuir = d.huir; c.colorAura = d.aura;
                    AssetDatabase.CreateAsset(c, ruta);
                }
                if (c.modelo == null)
                    c.modelo = AssetDatabase.LoadAssetAtPath<GameObject>(modelos + "/" + d.id + ".glb");
                if (c.modelo == null) sinModelo++;
                if (!cat.criaturas.Contains(c)) cat.criaturas.Add(c);
                EditorUtility.SetDirty(c);
            }
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();

            if (sinModelo > 0)
                Debug.LogWarning($"[Ancestrales AR] {sinModelo} criatura(s) sin modelo. Instala glTFast (menú Ancestrales > AR > 0) para que Unity importe los .glb de {modelos} y vuelve a usar este menú.");
            else
                Debug.Log("[Ancestrales AR] Datos listos en " + datos);
            return cat;
        }

        [MenuItem("Ancestrales/AR/3. Ajustar Player Settings (Android e iOS)", priority = 23)]
        public static void AjustarPlayerSettings()
        {
            if ((int)PlayerSettings.Android.minSdkVersion < 24)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                PlayerSettings.iOS.cameraUsageDescription = "Ancestrales usa la cámara para mostrar las criaturas en tu entorno.";
            Debug.Log("[Ancestrales AR] Player Settings: Android API 24+, IL2CPP, ARM64, OpenGLES3; iOS con permiso de cámara.");
        }

        [MenuItem("Ancestrales/AR/2. Crear escena AR", priority = 22)]
        public static void CrearEscena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var cat = CrearDatos();
            var mats = CrearMateriales();
            var prefabPlano = CrearPrefabPlano(mats.plano);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Sesión AR
            var goSesion = new GameObject("AR Session");
            var sesion = goSesion.AddComponent<ARSession>();
            goSesion.AddComponent<ARInputManager>();

            // XR Origin con cámara AR
            var origen = CrearOrigen(escena);
            if (origen == null)
            {
                EditorUtility.DisplayDialog("Ancestrales AR",
                    "No se pudo crear el XR Origin automáticamente.\nAgrega uno con GameObject > XR > XR Origin (Mobile AR) y vuelve a usar este menú.", "OK");
                return;
            }
            var cam = origen.Camera;
            cam.gameObject.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;

            var camManager = Obtener<ARCameraManager>(cam.gameObject);
            Obtener<ARCameraBackground>(cam.gameObject);
            camManager.requestedLightEstimation = LightEstimation.AmbientIntensity | LightEstimation.AmbientColor |
                                                  LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity |
                                                  LightEstimation.AmbientSphericalHarmonics;

            var oclusion = Obtener<AROcclusionManager>(cam.gameObject);
            // Por reflexión: los nombres cambian un poco entre AR Foundation 5 y 6.
            Poner(oclusion, "requestedEnvironmentDepthMode", "Fastest");
            Poner(oclusion, "requestedHumanDepthMode", "Fastest");
            Poner(oclusion, "requestedHumanStencilMode", "Fastest");
            Poner(oclusion, "requestedOcclusionPreferenceMode", "PreferEnvironmentOcclusion");

            var planos = Obtener<ARPlaneManager>(origen.gameObject);
            planos.planePrefab = prefabPlano;
            planos.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            var raycaster = Obtener<ARRaycastManager>(origen.gameObject);
            Obtener<ARAnchorManager>(origen.gameObject);

            // Luz que imita la luz real
            var goLuz = new GameObject("Luz estimada");
            var luz = goLuz.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1f;
            luz.shadows = LightShadows.None;
            goLuz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            goLuz.AddComponent<LuzEstimadaAR>().camara = camManager;

            CrearEventSystem();

            // Encuentro + interfaz
            var goJuego = new GameObject("Encuentro AR");
            var doc = goJuego.AddComponent<UIDocument>();
            doc.panelSettings = CrearPanelSettings();
            var hud = goJuego.AddComponent<HudAR>();
            var lanzador = goJuego.AddComponent<LanzadorAmuleto>();
            lanzador.materialAmuleto = mats.amuleto;
            lanzador.materialEstela = mats.brillo;
            var gestor = goJuego.AddComponent<GestorEncuentroAR>();
            gestor.sesion = sesion;
            gestor.raycaster = raycaster;
            gestor.planos = planos;
            gestor.oclusion = oclusion;
            gestor.camara = cam;
            gestor.catalogo = cat;
            gestor.hud = hud;
            gestor.lanzador = lanzador;
            gestor.materialBrillo = mats.brillo;
            gestor.materialSombra = mats.sombra;

            var rutaEscena = Carpeta("Escenas") + "/EncuentroAR.unity";
            EditorSceneManager.SaveScene(escena, rutaEscena);
            AgregarABuild(rutaEscena);
            string urp = AgregarFondoARaURP();
            Selection.activeGameObject = goJuego;

            EditorUtility.DisplayDialog("Ancestrales AR",
                "Escena creada: " + rutaEscena + "\n\n" +
                (string.IsNullOrEmpty(urp) ? "" : urp + "\n\n") +
                "Falta un paso manual:\nEdit > Project Settings > XR Plug-in Management → marca ARCore en Android y ARKit en iOS.\n\n" +
                "Luego File > Build Settings, conecta el celular y Build And Run.", "Listo");
        }

        // ------------------------------------------------------------------ piezas

        static T Obtener<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void Poner(object objeto, string propiedad, string valorEnum)
        {
            var p = objeto.GetType().GetProperty(propiedad);
            if (p == null || !p.CanWrite || !p.PropertyType.IsEnum) return;
            try { p.SetValue(objeto, Enum.Parse(p.PropertyType, valorEnum)); }
            catch (Exception e) { Debug.LogWarning($"[Ancestrales AR] No se pudo ajustar {propiedad}: {e.Message}"); }
        }

        static XROrigin BuscarOrigen(Scene escena)
        {
            foreach (var r in escena.GetRootGameObjects())
            {
                var o = r.GetComponentInChildren<XROrigin>(true);
                if (o != null) return o;
            }
            return null;
        }

        static XROrigin CrearOrigen(Scene escena)
        {
            string[] menus = { "GameObject/XR/XR Origin (Mobile AR)", "GameObject/XR/XR Origin (AR)" };
            foreach (var m in menus)
            {
                try { EditorApplication.ExecuteMenuItem(m); } catch { /* sigue con el siguiente */ }
                var o = BuscarOrigen(escena);
                if (o != null)
                {
                    o.transform.SetParent(null);
                    return o;
                }
            }

            // Plan B: armado a mano con el TrackedPoseDriver clásico.
            var tpd = Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
            if (tpd == null) return null;
            var go = new GameObject("XR Origin");
            var origen = go.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset").transform;
            offset.SetParent(go.transform, false);
            var goCam = new GameObject("Main Camera");
            goCam.transform.SetParent(offset, false);
            var cam = goCam.AddComponent<Camera>();
            var comp = goCam.AddComponent(tpd);
            var metodo = tpd.GetMethod("SetPoseSource");
            if (metodo != null)
            {
                var tipoDisp = Enum.Parse(tpd.GetNestedType("DeviceType"), "GenericXRDevice");
                var pose = Enum.Parse(tpd.GetNestedType("TrackedPose"), "ColorCamera");
                metodo.Invoke(comp, new[] { tipoDisp, pose });
            }
            origen.Camera = cam;
            origen.CameraFloorOffsetObject = offset.gameObject;
            return origen;
        }

        static void CrearEventSystem()
        {
            var tipoES = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            if (tipoES == null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent(tipoES);
            Type modulo = null;
#if ENABLE_INPUT_SYSTEM
            modulo = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
#endif
            if (modulo == null) modulo = Type.GetType("UnityEngine.EventSystems.StandaloneInputModule, UnityEngine.UI");
            if (modulo != null) go.AddComponent(modulo);
        }

        struct Materiales { public Material brillo, sombra, plano, amuleto; }

        static Material CrearMaterial(string carpeta, string nombre, string shader, Color color)
        {
            var ruta = carpeta + "/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            var s = Shader.Find(shader);
            if (s == null) { Debug.LogError("[Ancestrales AR] No se encontró el shader " + shader); return null; }
            m = new Material(s) { color = color };
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        static Materiales CrearMateriales()
        {
            var c = Carpeta("Materiales");
            var r = new Materiales
            {
                brillo = CrearMaterial(c, "M_AR_Brillo", "Ancestrales/AR/Brillo", Color.white),
                sombra = CrearMaterial(c, "M_AR_Sombra", "Ancestrales/AR/Sombra", new Color(0f, 0f, 0f, 0.55f)),
                plano = CrearMaterial(c, "M_AR_PlanoPuntos", "Ancestrales/AR/PlanoPuntos", new Color(0.94f, 0.89f, 0.78f, 0.7f)),
                amuleto = CrearMaterial(c, "M_AR_Amuleto", "Ancestrales/AR/Amuleto", new Color(0.95f, 0.72f, 0.25f, 1f)),
            };
            AssetDatabase.SaveAssets();
            return r;
        }

        static GameObject CrearPrefabPlano(Material mat)
        {
            var ruta = Carpeta("Prefabs") + "/PlanoAR.prefab";
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (existente != null) return existente;

            var go = new GameObject("PlanoAR");
            go.AddComponent<ARPlane>();
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>();   // para que el amuleto rebote en el piso
            go.AddComponent<ARPlaneMeshVisualizer>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static PanelSettings CrearPanelSettings()
        {
            var ruta = Carpeta("UI") + "/PanelAR.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(ruta);
            if (ps != null) return ps;
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(390, 844);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            var tema = AssetDatabase.FindAssets("t:ThemeStyleSheet").FirstOrDefault();
            if (tema != null) ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(tema));
            AssetDatabase.CreateAsset(ps, ruta);
            return ps;
        }

        static void AgregarABuild(string ruta)
        {
            var escenas = EditorBuildSettings.scenes.ToList();
            if (escenas.Any(e => e.path == ruta)) return;
            escenas.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }

        /// <summary>En URP el video de la cámara necesita el "AR Background Renderer Feature".</summary>
        static string AgregarFondoARaURP()
        {
            if (GraphicsSettings.currentRenderPipeline == null) return null; // Built-in: no hace falta
            var tipoFeature = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .FirstOrDefault(t => t.Name == "ARBackgroundRendererFeature");
            if (tipoFeature == null)
                return "Usas URP: agrega \"AR Background Renderer Feature\" a tu Universal Renderer Data.";

            int agregados = 0;
            var guids = AssetDatabase.FindAssets("t:UniversalRendererData").Concat(AssetDatabase.FindAssets("t:ForwardRendererData")).Distinct();
            foreach (var guid in guids)
            {
                var ruta = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadMainAssetAtPath(ruta) as ScriptableObject;
                if (data == null || !(data.GetType().Name == "UniversalRendererData" || data.GetType().Name == "ForwardRendererData")) continue;

                var so = new SerializedObject(data);
                var lista = so.FindProperty("m_RendererFeatures");
                var mapa = so.FindProperty("m_RendererFeatureMap");
                if (lista == null || mapa == null) continue;
                bool ya = false;
                for (int i = 0; i < lista.arraySize; i++)
                {
                    var r = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (r != null && r.GetType() == tipoFeature) ya = true;
                }
                if (ya) continue;

                var feature = ScriptableObject.CreateInstance(tipoFeature);
                feature.name = "ARBackgroundRendererFeature";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long idLocal);
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = feature;
                mapa.arraySize++;
                mapa.GetArrayElementAtIndex(mapa.arraySize - 1).longValue = idLocal;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                agregados++;
            }
            AssetDatabase.SaveAssets();
            return agregados > 0 ? $"URP: se agregó el AR Background Renderer Feature a {agregados} renderer(s)." : null;
        }
    }
}
#endif
