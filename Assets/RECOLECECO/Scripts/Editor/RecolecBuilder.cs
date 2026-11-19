#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RecolecEco.EditorTools
{
    /// <summary>
    /// Construye todo el contenido del proyecto de forma reproducible: materiales, mallas, prefabs,
    /// configuración, escenas MainMenu y BahiaPuerto, y la build de Windows.
    /// Menú: RECOLECECO → Construir todo / Build Windows.
    /// Línea de comandos: -executeMethod RecolecEco.EditorTools.RecolecBuilder.BatchBuildAll
    /// </summary>
    public static partial class RecolecBuilder
    {
        public const string Root = "Assets/RECOLECECO";
        const string MatDir = Root + "/Materials";
        const string ModelDir = Root + "/Models";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string TexDir = Root + "/Textures";

        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        static Shader _lit, _vertexLit, _water, _unlit, _particles;

        [MenuItem("RECOLECECO/Construir todo (contenido + escenas)")]
        public static void BuildAll()
        {
            _mats.Clear();
            _meshCache.Clear();
            EnsureFolders();
            LoadShaders();
            ConfigureProject();
            CreateConfig();
            CreateSharedAssets();
            BuildBoatPrefabs();
            BuildWastePrefabs();
            BuildCharacterPrefabs();
            BuildMainMenuScene();
            BuildGameScene();
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RECOLECECO] Contenido construido.");
        }

        [MenuItem("RECOLECECO/Build Windows (x64)")]
        public static bool BuildWindows()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build/RECOLECECO_Windows"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { SceneDir + "/MainMenu.unity", SceneDir + "/BahiaPuerto.unity" },
                locationPathName = Path.Combine(outDir, "RECOLECECO.exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[RECOLECECO] Build: {report.summary.result} · {report.summary.totalSize / (1024 * 1024)} MB · errores {report.summary.totalErrors}");
            return report.summary.result == BuildResult.Succeeded;
        }

        public static void BatchBuildAll()
        {
            try
            {
                BuildAll();
                bool ok = BuildWindows();
                EditorApplication.Exit(ok ? 0 : 2);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(3);
            }
        }

        public static void BatchContentOnly()
        {
            try { BuildAll(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(3); }
        }

        static void EnsureFolders()
        {
            foreach (var d in new[] { MatDir, ModelDir, PrefabDir, PrefabDir + "/Boat", PrefabDir + "/Waste", PrefabDir + "/Characters", PrefabDir + "/Environment", PrefabDir + "/UI", SceneDir, TexDir, Root + "/Resources", Root + "/Data" })
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(d));
                }
            }
        }

        static void LoadShaders()
        {
            _lit = Shader.Find("Universal Render Pipeline/Lit");
            _unlit = Shader.Find("Universal Render Pipeline/Unlit");
            _particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _vertexLit = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/RecolecVertexLit.shader");
            _water = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/RecolecWater.shader");
            if (_lit == null || _vertexLit == null || _water == null) throw new System.Exception("Faltan shaders (URP Lit / RECOLECECO).");
        }

        // ------------------------------------------------------------ Configuración del proyecto
        static void ConfigureProject()
        {
            PlayerSettings.companyName = "RECOLECECO";
            PlayerSettings.productName = "RECOLEC(ECO) Mareas de Esperanza";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.recolececo.mareasdeesperanza");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, "../Build/RECOLECECO_Windows/RECOLECECO.exe");

            // Sombras más lejanas para la bahía.
            foreach (var path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
            {
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (rp == null) continue;
                rp.shadowDistance = 140f;
                rp.shadowCascadeCount = 2;
                EditorUtility.SetDirty(rp);
            }

            // Ícono de la aplicación (generado).
            var icon = MakeAppIcon();
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        static Texture2D MakeAppIcon()
        {
            string path = TexDir + "/AppIcon.png";
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                    Color sea = Color.Lerp(UIColors.Hex("#0E6E8C"), UIColors.Hex("#3FD0D4"), v);
                    Color c = sea;
                    float wave = 0.36f + Mathf.Sin(u * 12f) * 0.03f;
                    if (v < wave) c = Color.Lerp(UIColors.Hex("#0A3E5C"), UIColors.Hex("#1FB5A8"), v / wave);
                    // casco
                    float hullTop = 0.42f, hullBot = 0.32f;
                    float hw = Mathf.Lerp(0.30f, 0.22f, (v - hullBot) / (hullTop - hullBot));
                    if (v > hullBot && v < hullTop && Mathf.Abs(u - 0.5f + (v - hullBot) * 0.3f) < hw) c = v > 0.395f ? UIColors.Hex("#F6C445") : UIColors.Hex("#2F78C4");
                    if (u > 0.40f && u < 0.56f && v > 0.42f && v < 0.56f) c = Color.white;
                    if (u > 0.43f && u < 0.53f && v > 0.47f && v < 0.53f) c = UIColors.Hex("#2F78C4");
                    // sol
                    if (Vector2.Distance(new Vector2(u, v), new Vector2(0.75f, 0.78f)) < 0.09f) c = UIColors.Hex("#FFE08A");
                    float a = Mathf.Clamp01((0.5f - d) * S * 0.5f);
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void CreateConfig()
        {
            string path = Root + "/Resources/GameConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                cfg.ResetToDefaults();
                AssetDatabase.CreateAsset(cfg, path);
            }
            else if (cfg.wastes == null || cfg.wastes.Length == 0) { cfg.ResetToDefaults(); EditorUtility.SetDirty(cfg); }
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/BahiaPuerto.unity", true),
            };
        }

        // ------------------------------------------------------------ Materiales
        public static Color Hex(string h) => UIColors.Hex(h);

        static Material Mat(string name, Color c, float smooth = 0.15f, float metal = 0f, bool emission = false)
        {
            if (_mats.TryGetValue(name, out var m) && m != null) return m;
            string path = MatDir + "/M_" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(_lit);
            m.shader = _lit;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (emission)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            m.enableInstancing = true;
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            _mats[name] = m;
            return m;
        }

        static Material EmissiveMat(string name, Color c, float intensity)
        {
            var m = Mat(name, c, 0.3f, 0, true);
            m.SetColor("_EmissionColor", c * intensity);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material TransparentMat(string name, Color c, bool particle, Texture tex = null)
        {
            if (_mats.TryGetValue(name, out var m) && m != null) return m;
            string path = MatDir + "/M_" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            var sh = particle ? _particles : _unlit;
            if (isNew) m = new Material(sh);
            m.shader = sh;
            m.SetColor("_BaseColor", c);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            _mats[name] = m;
            return m;
        }

        static Material VertexMat(string name)
        {
            if (_mats.TryGetValue(name, out var m) && m != null) return m;
            string path = MatDir + "/M_" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(_vertexLit); AssetDatabase.CreateAsset(m, path); }
            m.shader = _vertexLit;
            _mats[name] = m;
            return m;
        }

        static Texture2D _softCircle;
        static Texture2D SoftCircle()
        {
            if (_softCircle != null) return _softCircle;
            string path = TexDir + "/T_SoftCircle.png";
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            _softCircle = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return _softCircle;
        }

        // ------------------------------------------------------------ Mallas
        static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();

        static Mesh SaveMesh(Mesh m, string name)
        {
            string path = ModelDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(m, existing);
                existing.name = name;
                EditorUtility.SetDirty(existing);
                _meshCache[name] = existing;
                return existing;
            }
            m.name = name;
            AssetDatabase.CreateAsset(m, path);
            _meshCache[name] = m;
            return m;
        }

        static Mesh CachedMesh(string name, System.Func<Mesh> make)
        {
            if (_meshCache.TryGetValue(name, out var m) && m != null) return m;
            return SaveMesh(make(), name);
        }

        // ------------------------------------------------------------ Objetos
        static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, Vector3? euler = null, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3? scale = null, Vector3? euler = null, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale ?? Vector3.one;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static GameObject Empty(string name, Transform parent, Vector3 localPos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            return go;
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene s, string path)
        {
            EditorSceneManager.SaveScene(s, path);
        }
    }
}
#endif
