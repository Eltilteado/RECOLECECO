#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RecolecEco.EditorTools
{
    public static partial class RecolecBuilder
    {
        static Material _skyMat;
        static VolumeProfile _volume;

        static void SetupEnvironment(Transform root, out Light sun)
        {
            if (_skyMat == null)
            {
                string path = MatDir + "/M_Sky.mat";
                _skyMat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (_skyMat == null) { _skyMat = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(_skyMat, path); }
                _skyMat.SetFloat("_SunSize", 0.05f);
                _skyMat.SetFloat("_AtmosphereThickness", 0.8f);
                _skyMat.SetColor("_SkyTint", Hex("#8CC8F0"));
                _skyMat.SetColor("_GroundColor", Hex("#9FC6D4"));
                _skyMat.SetFloat("_Exposure", 1.25f);
                EditorUtility.SetDirty(_skyMat);
            }
            RenderSettings.skybox = _skyMat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#B8D8EE");
            RenderSettings.ambientEquatorColor = Hex("#E6D2B0");
            RenderSettings.ambientGroundColor = Hex("#6E8A8C");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("#CFE6F0");
            RenderSettings.fogStartDistance = 140f;
            RenderSettings.fogEndDistance = 520f;

            var sunGo = new GameObject("Sol (tarde soleada)");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#FFE9C9");
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            RenderSettings.sun = sun;

            if (_volume == null)
            {
                string vp = Root + "/Data/VP_RecolecEco.asset";
                _volume = AssetDatabase.LoadAssetAtPath<VolumeProfile>(vp);
                if (_volume == null)
                {
                    _volume = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(_volume, vp);
                    var bloom = _volume.Add<Bloom>(true);
                    bloom.intensity.Override(0.35f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.6f);
                    var tone = _volume.Add<Tonemapping>(true);
                    tone.mode.Override(TonemappingMode.Neutral);
                    var ca = _volume.Add<ColorAdjustments>(true);
                    ca.saturation.Override(14f); ca.contrast.Override(6f); ca.postExposure.Override(0.1f);
                    var vig = _volume.Add<Vignette>(true);
                    vig.intensity.Override(0.18f);
                    foreach (var c in _volume.components) AssetDatabase.AddObjectToAsset(c, _volume);
                    EditorUtility.SetDirty(_volume);
                }
            }
            var volGo = new GameObject("PostProceso");
            volGo.transform.SetParent(root, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = _volume;
        }

        static Camera MakeCamera(Transform root, Vector3 pos, Quaternion rot)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root, false);
            camGo.transform.SetPositionAndRotation(pos, rot);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 900f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return cam;
        }

        // ------------------------------------------------------------ MainMenu
        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MainMenu").transform;
            SetupEnvironment(root, out _);

            var water = MeshObj("Mar", root, CachedMesh("Water", () => MeshGen.WaterGrid(760f, 300)), _waterMat, Vector3.zero, null, null, false);
            var boat = Place(_boatModelPrefab, root, new Vector3(0, 0, 0), Quaternion.Euler(0, -30, 0));
            DecoBoat(root, new Vector3(-22, 0, 26), 40, 0);
            DecoBoat(root, new Vector3(30, 0, 40), -70, 2);
            // islote de fondo
            var rockMesh = CachedMesh("RockGuano", () => MeshGen.Rock(99, 0.3f, Hex("#7A6A5A"), Hex("#F1ECE0"), 1));
            MeshObj("Islote", root, rockMesh, VertexMat("RockVertex"), new Vector3(-60, -4, 120), new Vector3(30, 14, 22));
            MeshObj("Islote2", root, rockMesh, VertexMat("RockVertex"), new Vector3(-20, -3, 140), new Vector3(16, 9, 14));
            MeshObj("Cerro", root, CachedMesh("Rock2", () => MeshGen.Rock(25, 0.28f, Hex("#7E6A58"), Hex("#B59C80"), 1)), VertexMat("RockVertex"), new Vector3(90, -8, 160), new Vector3(60, 22, 40));
            for (int i = 0; i < 4; i++)
            {
                var gull = Place(_gullPrefab, root, new Vector3(0, 10, 0), Quaternion.identity);
                var gf = gull.GetComponent<GullFlier>(); gf.center = new Vector3(i * 4, 0, 10); gf.radius = 10 + i * 4; gf.height = 8 + i;
            }

            var pivot = new GameObject("PivoteCamara").transform;
            pivot.SetParent(root, false);
            pivot.position = new Vector3(0, 0, 0);
            var cam = MakeCamera(pivot, new Vector3(-9, 4.5f, -13), Quaternion.identity);
            cam.transform.LookAt(new Vector3(3, 1.5f, 0));

            var ctrl = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            ctrl.transform.SetParent(root, false);
            ctrl.cameraPivot = pivot;

            SaveScene(scene, SceneDir + "/MainMenu.unity");
        }

        // ------------------------------------------------------------ BahiaPuerto
        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BahiaPuerto").transform;
            SetupEnvironment(root, out _);

            var world = new GameObject("Mundo").transform;
            world.SetParent(root, false);
            BuildWorld(world, out var sectors, out var globalFauna, out var dock, out var finaleCam, out var finaleLook);

            // Chalana del jugador
            var boat = Place(_boatPrefab, root, BoatSpawn, Quaternion.identity);
            var spawn = Empty("InicioChalana", root, BoatSpawn).transform;

            // Cámara
            var cam = MakeCamera(root, BoatSpawn + new Vector3(0, 5, -10), Quaternion.identity);
            var bc = cam.gameObject.AddComponent<BoatCamera>();
            bc.target = boat.transform;

            // Marcador de destino (columna de luz)
            var beacon = Empty("MarcadorDestino", root, Mission1Point).transform;
            var beaconVis = MeshObj("Columna", beacon, CachedMesh("BeaconCylinder", () => {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var m = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(go);
                return m;
            }), _beaconMat, new Vector3(0, 12, 0), new Vector3(1.6f, 12f, 1.6f), null, false);
            var bring = MeshObj("Aro", beaconVis.transform, _ringMesh, _ringMat, new Vector3(0, -0.98f, 0), new Vector3(1.6f, 0.08f, 1.6f), null, false);
            beacon.gameObject.AddComponent<ObjectiveBeacon>().visual = beaconVis.transform;

            // Punto de la misión 1 (boya grande)
            var m1 = Buoy(root, Mission1Point, Hex("#F6C445"), "A");
            m1.name = "PuntoMision1";
            m1.transform.localScale = Vector3.one * 1.4f;

            // Sistemas
            var sys = new GameObject("Sistemas");
            sys.transform.SetParent(root, false);
            var gm = sys.AddComponent<GameManager>();
            sys.AddComponent<InventorySystem>();
            sys.AddComponent<RecyclingSystem>();
            sys.AddComponent<UpgradeSystem>();
            var qm = sys.AddComponent<QuestManager>();
            sys.AddComponent<DialogueSystem>();
            var env = sys.AddComponent<EnvironmentProgress>();
            gm.boat = boat.GetComponent<BoatController>();
            gm.boatCamera = bc;
            gm.boatSpawn = spawn;
            gm.dock = dock;
            gm.finaleCameraPoint = finaleCam;
            gm.finaleLookPoint = finaleLook;
            qm.mission1Point = m1.transform;
            env.sectors = sectors;
            env.globalFauna = globalFauna;

            var fx = new GameObject("Efectos").AddComponent<EffectsManager>();
            fx.transform.SetParent(root, false);
            fx.sparklePrefab = _sparklePrefab;
            fx.splashPrefab = _splashPrefab;

            var ui = new GameObject("UI").AddComponent<UIManager>();
            ui.transform.SetParent(root, false);

            SaveScene(scene, SceneDir + "/BahiaPuerto.unity");
        }
    }
}
#endif
