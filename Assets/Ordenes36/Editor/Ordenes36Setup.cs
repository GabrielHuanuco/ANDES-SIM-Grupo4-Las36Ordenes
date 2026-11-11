#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

namespace AndesSim.Ordenes36.EditorTools
{
    /// <summary>
    /// Configura de forma reproducible la prueba funcional del escenario 4 «Las 36 órdenes»:
    /// - Tablero de tarjetas 3D flotantes SIEMPRE DERECHAS frente a la cámara (sin inclinación chueca).
    /// - Tarjeta Central (Orden 01), 3 Tarjetas Izquierdas (Análisis técnico), 3 Tarjetas Derechas (Defensa),
    ///   3 Pestañas 3D superiores y Tarjeta Inferior con la animación en vivo del flujo SWIFT y las 36 órdenes.
    /// - Todas las tarjetas 3D tienen Collider + PhysicsRaycaster para hacer clic en Unity o tocar en el celular.
    /// </summary>
    public static class Ordenes36Setup
    {
        const string Root = "Assets/Ordenes36";
        const string ScenePath = "Assets/Scenes/Ordenes_AR.unity";
        const string MarkerPath = Root + "/Markers/Orden_01.png";
        const string DataPath = Root + "/Data/ScenarioData_Ordenes36.asset";
        const string PrefabPath = Root + "/Prefabs/Ficha_Orden.prefab";
        const string ApkPath = "Builds/ANDES-SIM_Ordenes36_prueba1.apk";
        const float MarkerWidthMeters = 0.20f;   // ancho impreso del marcador (20 cm)

        // ------------------------------------------------------------------ menús
        [MenuItem("ANDES-SIM/0. Importar TMP Essential Resources")]
        public static void ImportTmpEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) { Debug.Log("[Setup] TMP ya importado."); return; }
            string pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
            Debug.Log("[Setup] TMP Essential Resources importado.");
        }

        [MenuItem("ANDES-SIM/1. Configurar prueba Orden 01 (escena Ordenes_AR)")]
        public static void SetupAll()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                ImportTmpEssentials();
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("ANDES-SIM",
                        "Se importaron los recursos de TextMesh Pro.\nEspera a que termine la importación y vuelve a ejecutar «ANDES-SIM › 1».", "OK");
                    return;
                }
            }
            ConfigureMarkerTexture();
            ConfigureVuforia();
            ConfigurePlayer();
            var data = CreateScenarioData();
            var prefab = CreateCardPrefab();
            BuildScene(data, prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Tablero de Tarjetas 3D Flotantes (Ataque · Análisis · Defensa) configurado. Escena: " + ScenePath);
        }

        [MenuItem("ANDES-SIM/2. Generar APK (Android)")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[Setup] Build: {report.summary.result} · {report.summary.totalSize / (1024f * 1024f):0.0} MB · errores {report.summary.totalErrors} → {ApkPath}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        // Entradas para modo batch
        public static void BatchImportTmp() { ImportTmpEssentials(); }
        public static void BatchSetup() { SetupAll(); }
        public static void BatchBuild() { BuildApk(); }

        // ------------------------------------------------------------------ pasos
        static void ConfigureMarkerTexture()
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(MarkerPath);
            if (imp == null) { Debug.LogError("[Setup] Falta el marcador " + MarkerPath); return; }
            imp.textureType = TextureImporterType.Default;
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }

        static void ConfigureVuforia()
        {
            var cfg = VuforiaConfiguration.Instance;
            if (cfg == null) { Debug.LogError("[Setup] No se encontró VuforiaConfiguration."); return; }
            cfg.DeviceTracker.ARCoreRequirementSetting =
                VuforiaConfiguration.DeviceTrackerConfiguration.ARCoreRequirement.DONT_USE;
            if (string.IsNullOrEmpty(cfg.Vuforia.LicenseKey))
                Debug.LogWarning("[Setup] Falta la licencia Basic de Vuforia (Assets/Resources/VuforiaConfiguration).");
            EditorUtility.SetDirty(cfg);
        }

        static void ConfigurePlayer()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.companyName = "Grupo4";
            PlayerSettings.productName = "ANDES-SIM 36 Ordenes";
            PlayerSettings.SetApplicationIdentifier(android, "com.grupo4.andessim.ordenes36");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        static ScenarioData CreateScenarioData()
        {
            EnsureFolder(Root + "/Data");
            var data = AssetDatabase.LoadAssetAtPath<ScenarioData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ScenarioData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            data.limiteMontoMaximoUSD = 5000000;
            data.horaInicioLaboral = 8;
            data.horaFinLaboral = 18;
            data.ordenes.Clear();
            data.ordenes.Add(new OrderData
            {
                id = "ORD-01",
                numero = 1,
                referenciaSwift = "BAND160204001",
                fechaHora = "04 feb 2016 · 20:58",
                hora24 = 20,
                montoUSD = 30000000,
                beneficiario = "R. VILLANUEVA",
                beneficiarioRealEsperado = "PROVEEDOR REGISTRADO BANCO ANDINO",
                cuentaBeneficiario = "1180-0042-7731",
                destino = "Agencia Las Palmeras · Calle Jupiter",
                pais = "Makati, Manila (Filipinas)",
                beneficiarioEnListaBlanca = false,
                tieneDobleFirma = false,
                tieneTypo = false,
                estadoReal = OrderStatus.Pagada,
                notaEstado = "ATAQUE 2016: Liberada y pagada en Manila.\nSin doble firma ni límite nocturno."
            });
            EditorUtility.SetDirty(data);
            return data;
        }

        // ------------------------------------------------------------------ Tablero de Tarjetas 3D Flotantes
        static GameObject CreateCardPrefab()
        {
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Materials");

            var matEstado   = GetUnlitMat("Ficha_Estado",   Color.white);
            var matFondo    = GetUnlitMat("Ficha_Fondo",    new Color(0.035f, 0.082f, 0.114f));
            var matSubpanel = GetUnlitMat("Ficha_Subpanel", new Color(0.055f, 0.125f, 0.168f));
            var matCabBanco = GetUnlitMat("Ficha_Cabecera", new Color(0.047f, 0.196f, 0.365f));
            var matCabVuln  = GetUnlitMat("Ficha_CabVuln",  new Color(0.290f, 0.175f, 0.035f));
            var matCabDef   = GetUnlitMat("Ficha_CabDef",   new Color(0.035f, 0.235f, 0.145f));
            var matLinea    = GetUnlitMat("Ficha_Linea",    new Color(0.140f, 0.250f, 0.320f));

            var root = new GameObject("Ficha_Orden");
            var card = root.AddComponent<OrderCard>();
            card.mantenerDerechoFrenteACamara = true;
            card.BuildAestheticRig(matEstado, matFondo, matSubpanel, matCabBanco, matCabVuln, matCabDef, matLinea);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Renderer Create3DTabButton(string name, Transform parent, Vector3 pos, Vector3 size,
                                          Material matMarco, Material matFondo, TMP_FontAsset font, string label,
                                          OrderCard stage, Interactive3DCard.CardAction action)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;

            var marco = Box("Marco", root, new Vector3(0, 0, 1.0f), size + new Vector3(4, 4, 0), matMarco).GetComponent<Renderer>();
            var fondo = Box("Fondo", root, Vector3.zero, size, matFondo, true);
            var hit = fondo.AddComponent<Interactive3DCard>();
            hit.parentStage = stage;
            hit.action = action;

            Text("Label", root, font, Vector2.zero, new Vector2(size.x - 8, size.y - 4), 44,
                TextAlignmentOptions.Center, Color.white, true).text = label;
            return marco;
        }

        static void CreateSide3DCard(string name, Transform parent, Vector3 pos, Vector3 size,
                                     Material matMarco, Material matFondo, TMP_FontAsset font,
                                     OrderCard stage, Interactive3DCard.CardAction action,
                                     out Renderer marcoRenderer, out TMP_Text bodyText)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;

            marcoRenderer = Box("Marco", root, new Vector3(0, 0, 1.2f), size + new Vector3(4, 4, 0), matMarco).GetComponent<Renderer>();
            var fondo = Box("Fondo", root, Vector3.zero, size, matFondo, true);
            var hit = fondo.AddComponent<Interactive3DCard>();
            hit.parentStage = stage;
            hit.action = action;

            bodyText = Text("Texto", root, font, Vector2.zero, new Vector2(size.x - 10, size.y - 6), 36,
                TextAlignmentOptions.MidlineLeft, Color.white, false);
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, Vector2 pos, Vector2 size,
                             float fontSize, TextAlignmentOptions align, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            if (font) t.font = font;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.localPosition = new Vector3(pos.x, pos.y, -2.2f);
            t.fontSize = fontSize;
            t.enableAutoSizing = true;
            t.fontSizeMin = fontSize * 0.45f;
            t.fontSizeMax = fontSize;
            t.alignment = align;
            t.color = color;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.text = name;
            return t;
        }

        static Material GetUnlitMat(string name, Color c)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ escena
        static void BuildScene(ScenarioData data, GameObject cardPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // GameManager
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.data = data;
            gm.mode = SimMode.Ataque;
            gm.phase = SimPhase.Ataque;

            // ARCamera de Vuforia + PhysicsRaycaster para hacer clic o tocar las tarjetas 3D
            var camGo = new GameObject("ARCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 1f);
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PhysicsRaycaster>();
            camGo.AddComponent<VuforiaBehaviour>();
            camGo.AddComponent<DefaultInitializationErrorHandler>();

            // ImageTarget ORD-01
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
            var itGo = new GameObject("ImageTarget_Orden_01");
            var itb = itGo.AddComponent<ImageTargetBehaviour>();
            var so = new SerializedObject(itb);
            float aspect = tex ? (float)tex.height / tex.width : 0.707f;
            SetProp(so, "mTrackableName", p => p.stringValue = "ORD-01");
            SetProp(so, "mInitializedInEditor", p => p.boolValue = true);
            SetProp(so, "mImageTargetType", p => p.intValue = 3);
            SetProp(so, "mAspectRatio", p => p.floatValue = aspect);
            SetProp(so, "mWidth", p => p.floatValue = MarkerWidthMeters);
            SetProp(so, "mHeight", p => p.floatValue = MarkerWidthMeters * aspect);
            SetProp(so, "mRuntimeTexture", p => p.objectReferenceValue = tex);
            SetProp(so, "mMotionHint", p => p.intValue = 1);
            so.ApplyModifiedPropertiesWithoutUndo();

            var handler = itGo.AddComponent<DefaultObserverEventHandler>();
            handler.StatusFilter = DefaultObserverEventHandler.TrackingStatusFilter.Tracked_ExtendedTracked;
            var target = itGo.AddComponent<OrderTarget>();
            target.orderId = "ORD-01";
            UnityEventTools.AddPersistentListener(handler.OnTargetFound, target.HandleTargetFound);
            UnityEventTools.AddPersistentListener(handler.OnTargetLost, target.HandleTargetLost);

            // Tablero de Tarjetas 3D: completamente DERECHO (Quaternion.identity), sin inclinación que lo deforme
            var ficha = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, itGo.transform);
            ficha.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            ficha.transform.localRotation = Quaternion.identity;
            target.ficha = ficha.GetComponent<OrderCard>();

            // Luz
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            BuildHud();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ------------------------------------------------------------------ Sin paneles 2D que tapen la cámara (solo botón discreto para probar sin cámara)
        static void BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<ScanHUD>();

            hud.btnSimularEscaneo = CreateButton("Btn_Simular_Escaneo", canvasGo.transform,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-24, -20), new Vector2(310, 56), new Color(0.10f, 0.28f, 0.46f, 0.88f),
                out hud.txtBtnSimular, out _, "Probar sin cámara (Tarjetas 3D)", 20);
        }

        static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                         Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            if (bgColor.a <= 0.001f) img.raycastTarget = false;
            return rt;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 anchoredPos, Vector2 sizeDelta, float size, FontStyles style,
                                          TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset) t.font = TMP_Settings.defaultFontAsset;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.55f;
            t.fontSizeMax = size;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.raycastTarget = false;
            t.text = name;
            return t;
        }

        static Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                   Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor,
                                   out TMP_Text label, out Image bgImage, string text, float fontSize,
                                   TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            bgImage = go.GetComponent<Image>();
            bgImage.color = bgColor;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bgImage;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)txtGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 4);
            trt.offsetMax = new Vector2(-10, -4);

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = fontSize * 0.55f;
            tmp.fontSizeMax = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            label = tmp;
            return btn;
        }

        // ------------------------------------------------------------------ util
        static void SetProp(SerializedObject so, string name, System.Action<SerializedProperty> set)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogError($"[Setup] Propiedad Vuforia no encontrada: {name}"); return; }
            set(p);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
