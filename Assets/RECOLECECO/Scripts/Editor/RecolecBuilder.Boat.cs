#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RecolecEco.EditorTools
{
    public static partial class RecolecBuilder
    {
        static GameObject _boatPrefab, _boatModelPrefab;
        static Material _waterMat, _foamMat, _beaconMat, _ringMat, _sparkMat, _trailMat;
        static ParticleSystem _sparklePrefab, _splashPrefab;

        struct BoatScheme
        {
            public Color main, stripe, white, bottom, rim, inside, cabin, roof;
            public string name;
        }

        static readonly BoatScheme Hero = new BoatScheme
        {
            main = Hex("#1E6FC0"), stripe = Hex("#F6C445"), white = Hex("#F5F5F0"), bottom = Hex("#C8423A"),
            rim = Hex("#F5F5F0"), inside = Hex("#7FB7E0"), cabin = Hex("#F5F5F0"), roof = Hex("#E2453C"), name = "LA ESPERANZA"
        };

        static void CreateSharedAssets()
        {
            _waterMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Water.mat");
            if (_waterMat == null) { _waterMat = new Material(_water); AssetDatabase.CreateAsset(_waterMat, MatDir + "/M_Water.mat"); }
            _waterMat.shader = _water;

            var soft = SoftCircle();
            _foamMat = TransparentMat("Foam", new Color(1, 1, 1, 0.75f), true, soft);
            _sparkMat = TransparentMat("Sparkle", new Color(0.6f, 1f, 0.9f, 1f), true, soft);
            _trailMat = TransparentMat("Wake", new Color(1, 1, 1, 0.55f), true, soft);
            _beaconMat = TransparentMat("Beacon", new Color(1f, 0.85f, 0.3f, 0.35f), false);
            _ringMat = TransparentMat("HighlightRing", new Color(0.35f, 1f, 0.9f, 0.8f), false);

            _sparklePrefab = MakeParticlePrefab("FX_Sparkle", _sparkMat, new Color(0.6f, 1f, 0.9f), 0.25f, 4f, 0.7f, -0.6f, 0.6f);
            _splashPrefab = MakeParticlePrefab("FX_Splash", _foamMat, Color.white, 0.35f, 3f, 0.6f, 1.5f, 0.4f);
        }

        static ParticleSystem MakeParticlePrefab(string name, Material mat, Color color, float size, float speed, float life, float gravity, float radius)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = radius;
            sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var prefab = SavePrefab(go, PrefabDir + "/Environment/" + name + ".prefab");
            return prefab.GetComponent<ParticleSystem>();
        }

        struct BoatRefs
        {
            public Transform root, model;
            public CollectorArm arm;
            public Transform toolOrigin, propeller;
            public GameObject motorSmall, motorBig, holdExtra;
            public ParticleSystem wake, spray;
            public TrailRenderer[] trails;
        }

        /// <summary>Modelo 3D original de chalana pesquera peruana: casco pintado, cabina, bodega, mástil y red de proa.</summary>
        static BoatRefs BuildBoatModel(Transform parent, BoatScheme sc, bool withArm, bool withCrew, string meshKey)
        {
            var r = new BoatRefs();
            var model = Empty("Modelo", parent, Vector3.zero).transform;
            r.model = model;

            var hullMesh = CachedMesh("Hull_" + meshKey, () => MeshGen.ChalanaHull(sc.bottom, sc.white, sc.main, sc.stripe, sc.rim, sc.inside));
            MeshObj("Casco", model, hullMesh, VertexMat("BoatVertex"), Vector3.zero);

            var wood = Mat("Wood", Hex("#9C6B3F"));
            var woodDark = Mat("WoodDark", Hex("#6E4A2A"));
            var white = Mat("BoatWhite_" + meshKey, sc.cabin);
            var roof = Mat("BoatRoof_" + meshKey, sc.roof);
            var glass = Mat("Glass", Hex("#1C3A52"), 0.85f);
            var metal = Mat("Metal", Hex("#8A96A3"), 0.6f, 0.6f);
            var black = Mat("Rubber", Hex("#222628"), 0.1f);
            var orange = Mat("Orange", Hex("#F28C28"), 0.35f);

            // Regala (borde superior de madera)
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < MeshGen.ChalanaSections.Length - 2; i++)
                {
                    var a = MeshGen.ChalanaSections[i]; var b = MeshGen.ChalanaSections[i + 1];
                    Vector3 pa = new Vector3(side * a.halfWidth, a.top + 0.03f, a.z), pb = new Vector3(side * b.halfWidth, b.top + 0.03f, b.z);
                    var seg = Prim(PrimitiveType.Cube, model, (pa + pb) * 0.5f, new Vector3(0.09f, 0.07f, (pb - pa).magnitude + 0.05f), wood);
                    seg.transform.localRotation = Quaternion.LookRotation(pb - pa);
                }
            }

            float deckY = MeshGen.HullDeckY(-1.8f);
            // Cabina (popa)
            var cabin = Empty("Cabina", model, new Vector3(0, deckY, -2.05f)).transform;
            Prim(PrimitiveType.Cube, cabin, new Vector3(0, 0.62f, 0), new Vector3(1.55f, 1.24f, 1.35f), white);
            Prim(PrimitiveType.Cube, cabin, new Vector3(0, 1.32f, 0.05f), new Vector3(1.85f, 0.12f, 1.75f), roof);
            Prim(PrimitiveType.Cube, cabin, new Vector3(0, 0.85f, 0.68f), new Vector3(1.2f, 0.42f, 0.04f), glass);
            Prim(PrimitiveType.Cube, cabin, new Vector3(0.78f, 0.85f, 0.1f), new Vector3(0.04f, 0.38f, 0.7f), glass);
            Prim(PrimitiveType.Cube, cabin, new Vector3(-0.78f, 0.85f, 0.1f), new Vector3(0.04f, 0.38f, 0.7f), glass);
            Prim(PrimitiveType.Cube, cabin, new Vector3(0, 0.25f, 0.69f), new Vector3(1.56f, 0.1f, 0.03f), Mat("Stripe_" + meshKey, sc.stripe));
            // Salvavidas
            var ring = MeshObj("Salvavidas", cabin, CachedMesh("LifeRing", () => MeshGen.Torus(0.26f, 0.07f, 16, 6)), orange, new Vector3(0.79f, 0.6f, -0.25f), null, new Vector3(0, 0, 90));
            // Mástil y bandera
            Prim(PrimitiveType.Cylinder, cabin, new Vector3(0.5f, 2.0f, -0.4f), new Vector3(0.06f, 0.7f, 0.06f), wood);
            var flag = Empty("Bandera", cabin, new Vector3(0.5f, 2.6f, -0.4f)).transform;
            var red = Mat("FlagRed", Hex("#D52B1E"));
            var whiteF = Mat("FlagWhite", Color.white);
            Prim(PrimitiveType.Cube, flag, new Vector3(0, 0, -0.15f), new Vector3(0.02f, 0.3f, 0.15f), red);
            Prim(PrimitiveType.Cube, flag, new Vector3(0, 0, -0.3f), new Vector3(0.02f, 0.3f, 0.15f), whiteF);
            Prim(PrimitiveType.Cube, flag, new Vector3(0, 0, -0.45f), new Vector3(0.02f, 0.3f, 0.15f), red);
            flag.gameObject.AddComponent<FlagWave>().amplitude = 18f;

            // Bodega (escotilla central)
            float holdY = MeshGen.HullDeckY(0.2f);
            var hold = Empty("Bodega", model, new Vector3(0, holdY, 0.15f)).transform;
            Prim(PrimitiveType.Cube, hold, new Vector3(0, 0.18f, 0), new Vector3(1.3f, 0.36f, 1.5f), woodDark);
            Prim(PrimitiveType.Cube, hold, new Vector3(0, 0.38f, 0), new Vector3(1.15f, 0.06f, 1.35f), wood);
            Prim(PrimitiveType.Cube, hold, new Vector3(0, 0.42f, 0), new Vector3(0.08f, 0.03f, 1.35f), woodDark);
            var holdPoint = Empty("PuntoBodega", hold, new Vector3(0, 0.55f, 0)).transform;

            // Llantas (defensas) a los costados
            for (int i = 0; i < 3; i++)
                foreach (int side in new[] { -1, 1 })
                {
                    float z = -1.6f + i * 1.4f;
                    MeshObj("Defensa", model, CachedMesh("Tyre", () => MeshGen.Torus(0.2f, 0.08f, 12, 6)), black, new Vector3(side * 1.25f, 0.3f, z), null, new Vector3(0, 0, 90));
                }

            // Motores fuera de borda (normal y mejorado)
            var motorSmall = Empty("MotorNormal", model, new Vector3(0, 0.55f, -3.62f)).transform;
            Prim(PrimitiveType.Cube, motorSmall, new Vector3(0, 0.2f, -0.12f), new Vector3(0.36f, 0.42f, 0.42f), Mat("MotorGrey", Hex("#55626E")));
            Prim(PrimitiveType.Cylinder, motorSmall, new Vector3(0, -0.45f, -0.12f), new Vector3(0.1f, 0.5f, 0.1f), metal);
            var prop1 = Empty("Helice", motorSmall, new Vector3(0, -0.9f, -0.2f)).transform;
            Prim(PrimitiveType.Cube, prop1, Vector3.zero, new Vector3(0.36f, 0.06f, 0.03f), metal);
            r.motorSmall = motorSmall.gameObject;

            var motorBig = Empty("MotorMejorado", model, new Vector3(0, 0.55f, -3.7f)).transform;
            Prim(PrimitiveType.Cube, motorBig, new Vector3(0, 0.32f, -0.2f), new Vector3(0.55f, 0.68f, 0.62f), orange);
            Prim(PrimitiveType.Cube, motorBig, new Vector3(0, 0.7f, -0.2f), new Vector3(0.5f, 0.1f, 0.56f), Mat("MotorBlack", Hex("#2A2F35"), 0.5f));
            Prim(PrimitiveType.Cube, motorBig, new Vector3(0.28f, 0.35f, -0.2f), new Vector3(0.02f, 0.12f, 0.4f), whiteF);
            Prim(PrimitiveType.Cylinder, motorBig, new Vector3(0, -0.45f, -0.2f), new Vector3(0.14f, 0.55f, 0.14f), metal);
            var prop2 = Empty("Helice", motorBig, new Vector3(0, -0.95f, -0.3f)).transform;
            Prim(PrimitiveType.Cube, prop2, Vector3.zero, new Vector3(0.5f, 0.08f, 0.04f), metal);
            Prim(PrimitiveType.Cube, prop2, Vector3.zero, new Vector3(0.08f, 0.5f, 0.04f), metal);
            motorBig.gameObject.SetActive(false);
            r.motorBig = motorBig.gameObject;
            r.propeller = prop1;

            // Bodega ampliada (cajas extra en cubierta)
            var extra = Empty("BodegaAmpliada", model, new Vector3(0, MeshGen.HullDeckY(1.3f), 1.3f)).transform;
            var blue = Mat("CrateBlue", Hex("#2F78C4"));
            Prim(PrimitiveType.Cube, extra, new Vector3(-0.42f, 0.26f, 0), new Vector3(0.62f, 0.52f, 0.7f), blue);
            Prim(PrimitiveType.Cube, extra, new Vector3(0.42f, 0.26f, 0), new Vector3(0.62f, 0.52f, 0.7f), blue);
            Prim(PrimitiveType.Cube, extra, new Vector3(-0.42f, 0.54f, 0), new Vector3(0.66f, 0.06f, 0.74f), Mat("CrateLid", Hex("#5FA3E8")));
            Prim(PrimitiveType.Cube, extra, new Vector3(0.42f, 0.54f, 0), new Vector3(0.66f, 0.06f, 0.74f), Mat("CrateLid", Hex("#5FA3E8")));
            extra.gameObject.SetActive(false);
            r.holdExtra = extra.gameObject;

            // Tripulante (El Recolector) a bordo
            if (withCrew) BuildPerson(model, new Vector3(-0.55f, deckY, -0.95f), Quaternion.identity, Hex("#F28C28"), Hex("#2D4A6B"), Hex("#C68B59"), Hex("#2B2B2B"), "cap", out _, out _, out _, out _, 0.92f);

            // Brazo recolector con red (proa)
            if (withArm)
            {
                float bowDeck = MeshGen.HullDeckY(2.4f);
                var armBase = Empty("BrazoRecolector", model, new Vector3(0, bowDeck, 2.4f)).transform;
                var yellow = Mat("ArmYellow", Hex("#F6C445"), 0.4f);
                Prim(PrimitiveType.Cylinder, armBase, new Vector3(0, 0.12f, 0), new Vector3(0.5f, 0.12f, 0.5f), Mat("MotorGrey", Hex("#55626E")));
                var yaw = Empty("Giro", armBase, new Vector3(0, 0.24f, 0)).transform;
                Prim(PrimitiveType.Cylinder, yaw, new Vector3(0, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), yellow);
                var boomPivot = Empty("Pluma", yaw, new Vector3(0, 0.9f, 0)).transform;
                Prim(PrimitiveType.Sphere, boomPivot, Vector3.zero, Vector3.one * 0.26f, Mat("MotorGrey", Hex("#55626E")));
                var boom = Empty("Brazo", boomPivot, Vector3.zero).transform;
                Prim(PrimitiveType.Cube, boom, new Vector3(0, 0, 1.1f), new Vector3(0.14f, 0.14f, 2.2f), yellow);
                var tip = Empty("PuntaRed", boomPivot, new Vector3(0, -0.35f, 2.2f)).transform;
                var net = Empty("Red", boomPivot, new Vector3(0, 0, 2.2f)).transform;
                MeshObj("Aro", net, CachedMesh("NetHoop", () => MeshGen.Torus(0.42f, 0.04f, 18, 5)), yellow, new Vector3(0, -0.1f, 0), null, null);
                MeshObj("Malla", net, CachedMesh("NetBagMesh", () => MeshGen.NetBag(0.42f, 0.75f, 10)), Mat("NetGreen", Hex("#2E7D63"), 0.1f), new Vector3(0, -0.1f, 0), null, null, false);
                var arm = armBase.gameObject.AddComponent<CollectorArm>();
                arm.yawPivot = yaw; arm.boomPivot = boomPivot; arm.boom = boom; arm.netTip = tip; arm.net = net; arm.holdPoint = holdPoint;
                arm.restPitch = -32f; arm.scoopPitch = 30f; arm.holdYaw = 175f;
                r.arm = arm;
                r.toolOrigin = Empty("OrigenHerramienta", parent, new Vector3(0, 0.5f, 3.0f)).transform;
            }
            return r;
        }

        static void AddWakeFX(ref BoatRefs r, Transform parent)
        {
            // Espuma de popa
            var wakeGo = new GameObject("EstelaEspuma");
            wakeGo.transform.SetParent(parent, false);
            wakeGo.transform.localPosition = new Vector3(0, 0.1f, -3.7f);
            var ps = wakeGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startColor = new Color(1, 1, 1, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.playOnAwake = true;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 0.1f, 0.3f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            var pr = wakeGo.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = _foamMat;
            pr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            r.wake = ps;

            // Salpicadura de proa
            var sprayGo = Object.Instantiate(wakeGo, parent);
            sprayGo.name = "SalpicaduraProa";
            sprayGo.transform.localPosition = new Vector3(0, 0.3f, 3.4f);
            var sp = sprayGo.GetComponent<ParticleSystem>();
            var m2 = sp.main;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m2.gravityModifier = 1f;
            var sh2 = sp.shape; sh2.shapeType = ParticleSystemShapeType.Cone; sh2.angle = 35; sh2.radius = 0.3f; sh2.rotation = new Vector3(-30, 0, 0);
            sprayGo.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            r.spray = sp;

            // Estelas (líneas planas sobre el agua)
            var trails = new TrailRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var tgo = new GameObject("Estela" + i);
                tgo.transform.SetParent(parent, false);
                tgo.transform.localPosition = new Vector3(i == 0 ? -0.9f : 0.9f, 0.12f, -3.2f);
                tgo.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var tr = tgo.AddComponent<TrailRenderer>();
                tr.time = 2.6f;
                tr.minVertexDistance = 0.4f;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2.6f));
                var tg = new Gradient();
                tg.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.7f, 0), new GradientAlphaKey(0, 1) });
                tr.colorGradient = tg;
                tr.alignment = LineAlignment.TransformZ;
                tr.sharedMaterial = _trailMat;
                tr.shadowCastingMode = ShadowCastingMode.Off;
                tr.emitting = false;
                trails[i] = tr;
            }
            r.trails = trails;
        }

        static void BuildBoatPrefabs()
        {
            // --- Chalana jugable ---
            var root = new GameObject("Chalana");
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.25f, 0.1f);
            col.size = new Vector3(2.5f, 1.7f, 7.3f);
            var pm = new PhysicsMaterial("BoatBounce") { bounciness = 0.3f, dynamicFriction = 0.1f, staticFriction = 0.1f, frictionCombine = PhysicsMaterialCombine.Minimum };
            string pmPath = MatDir + "/PM_Boat.physicMaterial";
            var pmAsset = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
            if (pmAsset == null) { AssetDatabase.CreateAsset(pm, pmPath); pmAsset = pm; }
            col.sharedMaterial = pmAsset;

            var refs = BuildBoatModel(root.transform, Hero, true, true, "Hero");
            AddWakeFX(ref refs, root.transform);

            var ctrl = root.AddComponent<BoatController>();
            ctrl.model = refs.model;
            var vis = root.AddComponent<BoatVisuals>();
            vis.controller = ctrl; vis.model = refs.model; vis.wakeFoam = refs.wake; vis.bowSpray = refs.spray; vis.wakeTrails = refs.trails;
            vis.propeller = refs.propeller; vis.motorSmall = refs.motorSmall; vis.motorBig = refs.motorBig; vis.holdExtra = refs.holdExtra; vis.arm = refs.arm;
            var colSys = root.AddComponent<CollectionSystem>();
            colSys.boat = ctrl; colSys.arm = refs.arm; colSys.toolOrigin = refs.toolOrigin;
            _boatPrefab = SavePrefab(root, PrefabDir + "/Boat/Chalana.prefab");

            // --- Modelo decorativo (menú) ---
            var deco = new GameObject("ChalanaModelo");
            var drefs = BuildBoatModel(deco.transform, Hero, true, true, "Hero");
            Object.DestroyImmediate(drefs.arm);
            deco.AddComponent<Floater>().tilt = 4f;
            _boatModelPrefab = SavePrefab(deco, PrefabDir + "/Boat/ChalanaModelo.prefab");
        }

        static readonly BoatScheme[] DecoSchemes =
        {
            new BoatScheme { main = Hex("#2BB3A3"), stripe = Hex("#E2453C"), white = Hex("#F5F5F0"), bottom = Hex("#B03A32"), rim = Hex("#F6C445"), inside = Hex("#9FD9CF"), cabin = Hex("#F5F5F0"), roof = Hex("#2F78C4"), name = "SAN PEDRO" },
            new BoatScheme { main = Hex("#E2453C"), stripe = Hex("#F5F5F0"), white = Hex("#F6C445"), bottom = Hex("#2A3D52"), rim = Hex("#F5F5F0"), inside = Hex("#F0A39C"), cabin = Hex("#F6C445"), roof = Hex("#1E6FC0"), name = "VIRGEN DEL CARMEN" },
            new BoatScheme { main = Hex("#F6C445"), stripe = Hex("#1E6FC0"), white = Hex("#F5F5F0"), bottom = Hex("#C8423A"), rim = Hex("#1E6FC0"), inside = Hex("#F7DD92"), cabin = Hex("#F5F5F0"), roof = Hex("#2BB3A3"), name = "MI CHOLITA" },
        };

        static GameObject DecoBoat(Transform parent, Vector3 pos, float yaw, int scheme)
        {
            var go = new GameObject("BarcoFondo_" + scheme);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            BuildBoatModel(go.transform, DecoSchemes[scheme % DecoSchemes.Length], false, false, "Deco" + scheme);
            go.AddComponent<Floater>().tilt = 5f;
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.25f, 0.1f);
            col.size = new Vector3(2.5f, 1.7f, 7.3f);
            return go;
        }
    }
}
#endif
