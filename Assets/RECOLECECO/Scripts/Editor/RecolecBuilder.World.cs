#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RecolecEco.EditorTools
{
    /// <summary>Mapa único: Puerto de Pisco + bahía con sectores A (muelle), B (playa rocosa) y C (islote de aves).</summary>
    public static partial class RecolecBuilder
    {
        // ---------------- Distribución del mapa (metros, agua en y = 0)
        public static readonly Vector3 SectorA = new Vector3(4, 0, -10);
        public static readonly Vector3 SectorB = new Vector3(-118, 0, 52);
        public static readonly Vector3 SectorC = new Vector3(112, 0, 128);
        public static readonly Vector3 IsletC = new Vector3(122, 0, 142);
        const float RadiusA = 24f, RadiusB = 32f, RadiusC = 36f, IsletR = 17f;
        const float QuayZ = -68f, PortY = 1.2f;
        static readonly Vector3 DockPoint = new Vector3(5.6f, 0, -54f);
        static readonly Vector3 BoatSpawn = new Vector3(8f, 0, -34f);
        static readonly Vector3 Mission1Point = new Vector3(-4f, 0, -16f);

        struct RockInfo { public Vector3 pos; public float radius; }
        static readonly List<RockInfo> _rocks = new List<RockInfo>();

        static float Noise(float x, float z, float f) => Mathf.PerlinNoise(x * f + 31.7f, z * f + 11.3f);

        /// <summary>Altura del terreno (fondo marino y tierra).</summary>
        public static float TerrainHeight(float x, float z)
        {
            float coastW = -160f + 9f * Mathf.Sin(z * 0.035f) + (z > 10 && z < 100 ? 6f : 0f);
            float coastE = 172f + 10f * Mathf.Sin(z * 0.045f + 1f) - (z > 120 && z < 200 ? 6f : 0f);
            float dSouth = QuayZ - z;
            float dWest = coastW - x;
            float dEast = x - coastE;
            float dIslet = IsletR - Vector2.Distance(new Vector2(x, z), new Vector2(IsletC.x, IsletC.z));
            float land = Mathf.Max(Mathf.Max(dSouth, dWest), Mathf.Max(dEast, dIslet));

            float n = Noise(x, z, 0.05f);
            if (land < 0)
            {
                float depth = Mathf.Lerp(-1.2f, -7f, Mathf.Clamp01(-land / 18f));
                return depth + (n - 0.5f) * 0.8f;
            }
            float h;
            if (dIslet >= land - 0.01f && dIslet > 0)
                h = Mathf.Lerp(-0.5f, 7.5f, Mathf.Pow(Mathf.Clamp01(dIslet / IsletR), 0.6f)) + (n - 0.5f) * 2f;
            else if (dSouth >= land - 0.01f && Mathf.Abs(x) < 125f && z > -118f)
                h = PortY + Mathf.Clamp01((-118f - z + 30f) / 30f) * 0f; // puerto plano
            else
            {
                // Colinas y acantilados costeros (Paracas / Pisco: tonos arena y ocre)
                float hills = Mathf.Clamp01(land / 26f);
                h = Mathf.Lerp(0.2f, 1.6f, Mathf.Clamp01(land / 5f)) + hills * hills * (10f + n * 22f);
                // playa de arena suave en el sector B
                if (dWest >= land - 0.01f && z > 15 && z < 95) h = Mathf.Lerp(0.0f, 2.2f, Mathf.Clamp01(land / 22f)) + hills * hills * n * 8f;
            }
            // Transición costa
            if (land < 3f) h = Mathf.Lerp(-1.2f, h, land / 3f);
            return h;
        }

        static Color TerrainColor(float x, float y, float z, float steep)
        {
            float j = (Noise(x, z, 0.3f) - 0.5f) * 0.06f;
            Color sand = Hex("#E8CF9A"), wetSand = Hex("#C9A974"), ochre = Hex("#D6A15E"), rock = Hex("#9A7B5C"), green = Hex("#9BB86A"), seabed = Hex("#2E7E87"), paved = Hex("#CFC6B4");
            Color c;
            if (y < -0.6f) c = Color.Lerp(seabed, wetSand, Mathf.Clamp01((y + 4f) / 3.4f));
            else if (y < 0.6f) c = wetSand;
            else if (Mathf.Abs(y - PortY) < 0.05f && z < QuayZ + 1 && z > -120 && Mathf.Abs(x) < 125) c = paved;
            else if (steep > 0.45f) c = rock;
            else if (y > 12f) c = Color.Lerp(ochre, rock, 0.35f);
            else c = Color.Lerp(sand, ochre, Mathf.Clamp01((y - 1f) / 10f));
            float vx = x - IsletC.x, vz = z - IsletC.z;
            if (vx * vx + vz * vz < IsletR * IsletR && y > 2.5f) c = Color.Lerp(Hex("#EDE6D6"), green, Noise(x, z, 0.2f) * 0.5f); // guano y matorral
            return c + new Color(j, j, j, 0);
        }

        static bool IsFreeWater(Vector3 p, float margin)
        {
            if (TerrainHeight(p.x, p.z) > -1.6f) return false;
            // margen desde la costa
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                if (TerrainHeight(p.x + Mathf.Cos(a) * margin, p.z + Mathf.Sin(a) * margin) > -0.9f) return false;
            }
            foreach (var r in _rocks) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(r.pos.x, r.pos.z)) < r.radius + margin) return false;
            return true;
        }

        // ------------------------------------------------------------ Escena principal
        static void BuildWorld(Transform world, out SectorZone[] sectors, out FaunaGroup[] globalFauna, out DockZone dock, out Transform finaleCam, out Transform finaleLook)
        {
            _rocks.Clear();
            var rng = new System.Random(2024);

            // Agua
            var waterMesh = CachedMesh("Water", () => MeshGen.WaterGrid(760f, 300));
            var water = MeshObj("Mar", world, waterMesh, _waterMat, new Vector3(0, 0, 40f), null, null, false);
            water.GetComponent<MeshRenderer>().receiveShadows = true;

            // Terreno
            var terrainMesh = CachedMesh("TerrainBahia", () => MeshGen.Terrain(-300, 300, -200, 330, 4f, TerrainHeight, TerrainColor));
            var terrain = MeshObj("Terreno", world, terrainMesh, VertexMat("Terrain"), Vector3.zero);
            terrain.AddComponent<MeshCollider>().sharedMesh = terrainMesh;
            terrain.isStatic = true;

            // Límites invisibles de la bahía
            var bounds = Empty("LimitesBahia", world, Vector3.zero).transform;
            void Wall(Vector3 c, Vector3 s) { var w = Empty("Limite", bounds, c); w.AddComponent<BoxCollider>().size = s; }
            Wall(new Vector3(0, 0, 250), new Vector3(600, 20, 4));
            Wall(new Vector3(-200, 0, 60), new Vector3(4, 20, 400));
            Wall(new Vector3(205, 0, 60), new Vector3(4, 20, 400));

            BuildPort(world, out dock, out finaleCam, out finaleLook);

            // Rocas (antes de los residuos para respetar distancias)
            var rockMeshes = new Mesh[6];
            for (int i = 0; i < rockMeshes.Length; i++)
            {
                int seed = 11 + i * 7;
                rockMeshes[i] = CachedMesh("Rock" + i, () => MeshGen.Rock(seed, 0.28f, Hex("#7E6A58"), Hex("#B59C80"), 1));
            }
            var guanoRock = CachedMesh("RockGuano", () => MeshGen.Rock(99, 0.3f, Hex("#7A6A5A"), Hex("#F1ECE0"), 1));
            var rocks = Empty("Rocas", world, Vector3.zero).transform;
            GameObject Rock(Vector3 p, float s, int idx, bool guano = false, float sink = 0.35f)
            {
                var m = guano ? guanoRock : rockMeshes[idx % rockMeshes.Length];
                var go = MeshObj("Roca", rocks, m, VertexMat("RockVertex"), new Vector3(p.x, -s * sink, p.z), Vector3.one * s, new Vector3(0, (float)rng.NextDouble() * 360f, 0));
                var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.95f;
                go.isStatic = true;
                _rocks.Add(new RockInfo { pos = p, radius = s * 0.95f });
                if (TerrainHeight(p.x, p.z) < -0.5f)
                {
                    var foam = MeshObj("Espuma", go.transform.parent, CachedMesh("FoamRing", () => MeshGen.Ring(0.9f, 1.25f, 24, "FoamRingMesh")), _foamMat, new Vector3(p.x, 0.05f, p.z), Vector3.one * s, null, false);
                    foam.AddComponent<FoamPulse>();
                }
                return go;
            }
            // Sector B: playa rocosa
            for (int i = 0; i < 16; i++)
            {
                float z = 18 + i * 5.2f + (float)rng.NextDouble() * 3f;
                float x = -152f + (float)rng.NextDouble() * 10f;
                Rock(new Vector3(x, 0, z), 2f + (float)rng.NextDouble() * 3.5f, i);
            }
            Rock(new Vector3(-128, 0, 30), 3.2f, 1); Rock(new Vector3(-104, 0, 74), 2.8f, 3); Rock(new Vector3(-136, 0, 70), 4f, 2);
            Rock(new Vector3(-96, 0, 38), 2.2f, 4);
            // Sector C: islote
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad + (float)rng.NextDouble() * 0.3f;
                float rr = IsletR + 1.5f + (float)rng.NextDouble() * 3f;
                Rock(IsletC + new Vector3(Mathf.Cos(a) * rr, 0, Mathf.Sin(a) * rr), 2.2f + (float)rng.NextDouble() * 2.5f, i, i % 3 == 0);
            }
            Rock(IsletC + new Vector3(-30, 0, -26), 3.5f, 5, true); Rock(IsletC + new Vector3(24, 0, -30), 2.6f, 2, true);
            // Rompeolas del puerto y costas
            for (int i = 0; i < 9; i++) Rock(new Vector3(40 + i * 4.2f, 0, -66 + (i % 2) * 1.5f), 2.2f + (i % 3) * 0.6f, i);
            for (int i = 0; i < 9; i++) Rock(new Vector3(-44 - i * 4.2f, 0, -66 + (i % 2) * 1.5f), 2.2f + (i % 3) * 0.6f, i + 2);
            for (int i = 0; i < 14; i++) Rock(new Vector3(166 + (float)rng.NextDouble() * 8f, 0, -40 + i * 14f), 3f + (float)rng.NextDouble() * 3f, i);
            for (int i = 0; i < 6; i++) Rock(new Vector3(-158 + (float)rng.NextDouble() * 6f, 0, -50 + i * 11f), 3f + (float)rng.NextDouble() * 2f, i + 1);

            // Sectores
            sectors = new SectorZone[3];
            sectors[0] = BuildSector(world, SectorId.A, "Sector A · Cerca del muelle", SectorA, RadiusA, rng);
            sectors[1] = BuildSector(world, SectorId.B, "Sector B · Playa rocosa", SectorB, RadiusB, rng);
            sectors[2] = BuildSector(world, SectorId.C, "Sector C · Islote de aves", SectorC, RadiusC, rng);
            PlaceWastes(world, rng);

            // Fauna global: delfines al 75 % de limpieza
            var dolphins = Empty("Delfines_75", world, new Vector3(20, 0, 70)).AddComponent<FaunaGroup>();
            dolphins.threshold = 0.75f;
            for (int i = 0; i < 3; i++) AddJumper(dolphins.transform, new Vector3(i * 8 - 8, 0, i * 5), 30f, 2.6f, Hex("#6F8797"), Hex("#C9D6DE"), 2.2f, 7f, 1.4f);
            globalFauna = new[] { dolphins };

            // Faro
            var lh = Empty("Faro", world, new Vector3(186, TerrainHeight(186, 150), 150)).transform;
            var lw = Mat("LighthouseWhite", Hex("#F5F5F0")); var lr = Mat("LighthouseRed", Hex("#D63A30"));
            for (int i = 0; i < 5; i++) Prim(PrimitiveType.Cylinder, lh, new Vector3(0, 1.5f + i * 3f, 0), new Vector3(3.2f - i * 0.25f, 1.5f, 3.2f - i * 0.25f), i % 2 == 0 ? lw : lr);
            Prim(PrimitiveType.Cylinder, lh, new Vector3(0, 16f, 0), new Vector3(2.4f, 1f, 2.4f), Mat("Glass", Hex("#1C3A52"), 0.85f));
            var beam = Empty("Luz", lh, new Vector3(0, 16f, 0)).transform;
            Prim(PrimitiveType.Cube, beam, new Vector3(0, 0, 1.2f), new Vector3(0.6f, 0.6f, 0.6f), EmissiveMat("LighthouseLamp", Hex("#FFE08A"), 3f));
            beam.gameObject.AddComponent<Spinner>().speed = new Vector3(0, 60, 0);
        }

        static void AddJumper(Transform parent, Vector3 local, float radius, float scale, Color body, Color belly, float arcH, float arcL, float time)
        {
            var j = Empty("Saltador", parent, local);
            var fish = MeshObj("Pez", j.transform, CachedMesh("Fish_" + ColorUtility.ToHtmlStringRGB(body), () => MeshGen.Fish(body, belly)), VertexMat("FishVertex"), Vector3.zero, Vector3.one * scale, null, false);
            var fj = j.AddComponent<FishJumper>();
            fj.fish = fish.transform; fj.radius = radius; fj.arcHeight = arcH; fj.arcLength = arcL; fj.jumpTime = time;
        }

        static SectorZone BuildSector(Transform world, SectorId id, string display, Vector3 center, float radius, System.Random rng)
        {
            var go = Empty("Sector_" + id, world, center);
            var zone = go.AddComponent<SectorZone>();
            zone.id = id; zone.displayName = display; zone.radius = radius + 6f;

            var fauna = new List<FaunaGroup>();
            // Peces que saltan (más con la limpieza)
            float[] th = { 0.25f, 0.5f, 0.75f, 1f };
            for (int i = 0; i < th.Length; i++)
            {
                var g = Empty("Peces_" + (int)(th[i] * 100), go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                g.threshold = th[i];
                for (int k = 0; k < 2; k++) AddJumper(g.transform, Vector3.zero, radius * 0.8f, 1f, Hex("#4B8FB5"), Hex("#DDEBF2"), 1.5f, 3f, 0.9f);
                fauna.Add(g);
            }
            // Gaviotas
            for (int i = 0; i < 3; i++)
            {
                var g = Empty("Gaviotas_" + i, go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                g.threshold = 0.2f + i * 0.3f;
                for (int k = 0; k < 2; k++)
                {
                    var gull = Place(_gullPrefab, g.transform, center + Vector3.up * 10, Quaternion.identity);
                    var gf = gull.GetComponent<GullFlier>();
                    gf.center = center; gf.radius = radius * (0.4f + 0.25f * k + 0.1f * i); gf.height = 8 + i * 2;
                }
                fauna.Add(g);
            }
            if (id == SectorId.B)
            {
                // Lobos marinos en las rocas de la playa
                Vector3[] spots = { new Vector3(-150, 0, 30), new Vector3(-147, 0, 45), new Vector3(-151, 0, 60), new Vector3(-146, 0, 75), new Vector3(-136, 0, 70), new Vector3(-128, 0, 30) };
                for (int i = 0; i < spots.Length; i++)
                {
                    var g = Empty("LobosMarinos_" + i, go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                    g.threshold = 0.15f + i * 0.15f;
                    var p = spots[i];
                    float top = RockTop(p) + 0.1f;
                    Place(_seaLionPrefab, g.transform, new Vector3(p.x, top, p.z), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0));
                    fauna.Add(g);
                }
            }
            if (id == SectorId.C)
            {
                // Aves del islote (guaneras y pingüinos de Humboldt)
                for (int i = 0; i < 8; i++)
                {
                    var g = Empty("AvesIslote_" + i, go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                    g.threshold = 0.1f + i * 0.12f;
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                        float rr = (float)rng.NextDouble() * IsletR * 0.75f;
                        float x = IsletC.x + Mathf.Cos(a) * rr, z = IsletC.z + Mathf.Sin(a) * rr;
                        var prefab = (i % 3 == 2) ? _penguinPrefab : _boobyPrefab;
                        Place(prefab, g.transform, new Vector3(x, TerrainHeight(x, z) + 0.05f, z), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0)).transform.localScale = Vector3.one * 1.5f;
                    }
                    fauna.Add(g);
                }
                for (int i = 0; i < 2; i++)
                {
                    var g = Empty("GaviotasIslote_" + i, go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                    g.threshold = 0.3f + i * 0.4f;
                    for (int k = 0; k < 3; k++)
                    {
                        var gull = Place(_gullPrefab, g.transform, IsletC + Vector3.up * 14, Quaternion.identity);
                        var gf = gull.GetComponent<GullFlier>(); gf.center = IsletC; gf.radius = 8 + k * 5; gf.height = 12 + k * 2;
                    }
                    fauna.Add(g);
                }
            }
            zone.fauna = fauna.ToArray();

            // Basura decorativa pequeña (desaparece al limpiar)
            var dirt = new List<FaunaGroup>();
            var chipMat = Mat("DebrisChip", Hex("#D8D2C4"));
            var chipMat2 = Mat("DebrisChip2", Hex("#8E8A6E"));
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = Vector3.zero;
                for (int tries = 0; tries < 40; tries++)
                {
                    Vector2 r = Random2(rng) * radius;
                    p = center + new Vector3(r.x, 0, r.y);
                    if (IsFreeWater(p, 3f)) break;
                }
                var g = Empty("Desechos_" + i, go.transform, Vector3.zero).AddComponent<FaunaGroup>();
                g.threshold = 0.25f + (i % 4) * 0.2f;
                var cluster = Empty("Grupo", g.transform, p);
                cluster.AddComponent<Floater>().tilt = 8f;
                for (int k = 0; k < 7; k++)
                {
                    Vector2 o = Random2(rng) * 1.6f;
                    Prim(PrimitiveType.Cube, cluster.transform, new Vector3(o.x, 0.02f, o.y), new Vector3(0.18f, 0.06f, 0.14f) * (0.7f + (float)rng.NextDouble()), k % 2 == 0 ? chipMat : chipMat2, new Vector3(0, (float)rng.NextDouble() * 360, 0));
                }
                dirt.Add(g);
            }
            zone.dirtProps = dirt.ToArray();

            // Boyas de sector
            var buoyCol = id == SectorId.A ? Hex("#F6C445") : id == SectorId.B ? Hex("#F28C28") : Hex("#E2453C");
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + 4f);
                if (!IsFreeWater(p, 2f)) continue;
                Buoy(go.transform, p, buoyCol, id.ToString());
            }
            return zone;
        }

        static float RockTop(Vector3 p)
        {
            float best = 0.3f;
            foreach (var r in _rocks)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(r.pos.x, r.pos.z));
                if (d < r.radius) best = Mathf.Max(best, r.radius * 0.65f * Mathf.Sqrt(Mathf.Max(0, 1 - (d * d) / (r.radius * r.radius))));
            }
            return Mathf.Max(best, TerrainHeight(p.x, p.z));
        }

        static Vector2 Random2(System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble());
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        static GameObject Buoy(Transform parent, Vector3 p, Color c, string letter)
        {
            var b = Empty("Boya_" + letter, parent, p);
            var mat = Mat("Buoy_" + ColorUtility.ToHtmlStringRGB(c), c, 0.4f);
            Prim(PrimitiveType.Cylinder, b.transform, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.4f, 1.1f), mat);
            Prim(PrimitiveType.Cylinder, b.transform, new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.12f, 0.7f), Mat("BuoyWhite", Color.white));
            Prim(PrimitiveType.Cylinder, b.transform, new Vector3(0, 1.6f, 0), new Vector3(0.12f, 0.8f, 0.12f), Mat("Metal", Hex("#8A96A3"), 0.6f, 0.6f));
            Prim(PrimitiveType.Sphere, b.transform, new Vector3(0, 2.5f, 0), Vector3.one * 0.35f, EmissiveMat("BuoyLamp_" + ColorUtility.ToHtmlStringRGB(c), c, 2f));
            var label = new GameObject("Letra");
            label.transform.SetParent(b.transform, false);
            label.transform.localPosition = new Vector3(0, 3.3f, 0);
            var tm = label.AddComponent<TextMesh>();
            tm.text = letter; tm.fontSize = 64; tm.characterSize = 0.12f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white; tm.fontStyle = FontStyle.Bold;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.AddComponent<Billboard>();
            b.AddComponent<Floater>().tilt = 8f;
            var col = b.AddComponent<CapsuleCollider>(); col.radius = 0.6f; col.height = 3f; col.center = new Vector3(0, 0.5f, 0);
            var rb = b.AddComponent<Rigidbody>(); rb.isKinematic = true;
            return b;
        }

        static void PlaceWastes(Transform world, System.Random rng)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Resources/GameConfig.asset");
            var root = Empty("Residuos", world, Vector3.zero).transform;
            var placed = new List<Vector3>();
            var dockArea = new Vector3(3, 0, -50);
            foreach (var sc in cfg.sectors)
            {
                Vector3 center = sc.sector == SectorId.A ? SectorA : sc.sector == SectorId.B ? SectorB : SectorC;
                float radius = sc.sector == SectorId.A ? RadiusA : sc.sector == SectorId.B ? RadiusB : RadiusC;
                var list = new List<WasteType>();
                for (int i = 0; i < sc.botellas; i++) list.Add(WasteType.Botella);
                for (int i = 0; i < sc.latas; i++) list.Add(WasteType.Lata);
                for (int i = 0; i < sc.redes; i++) list.Add(WasteType.Red);
                for (int i = 0; i < sc.cajasCarton; i++) list.Add(WasteType.CajaCarton);
                for (int i = 0; i < sc.cajasTecnopor; i++) list.Add(WasteType.CajaTecnopor);
                var sroot = Empty("Residuos_" + sc.sector, root, Vector3.zero).transform;
                int idx = 0;
                foreach (var t in list)
                {
                    Vector3 p = center;
                    bool ok = false;
                    for (int tries = 0; tries < 400 && !ok; tries++)
                    {
                        Vector2 r = Random2(rng) * radius;
                        p = center + new Vector3(r.x, 0, r.y);
                        ok = IsFreeWater(p, 3.5f) && Vector3.Distance(p, dockArea) > 16f && Vector3.Distance(p, BoatSpawn) > 6f;
                        if (ok) foreach (var q in placed) if (Vector3.Distance(p, q) < 4.5f) { ok = false; break; }
                    }
                    if (!ok) Debug.LogWarning($"[RECOLECECO] Residuo {sc.sector}{idx} colocado sin cumplir todas las restricciones.");
                    placed.Add(p);
                    var go = Place(_wastePrefabs[(int)t], sroot, p, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                    go.name = $"Residuo_{sc.sector}{idx:00}_{t}";
                    go.GetComponent<CollectibleWaste>().Setup($"{sc.sector}{idx:00}", t, sc.sector);
                    EditorUtility.SetDirty(go.GetComponent<CollectibleWaste>());
                    idx++;
                }
            }
        }

        // ------------------------------------------------------------ Puerto de Pisco
        static void BuildPort(Transform world, out DockZone dockZone, out Transform finaleCam, out Transform finaleLook)
        {
            var port = Empty("PuertoDePisco", world, Vector3.zero).transform;
            var stone = Mat("QuayStone", Hex("#A79C8A"));
            var stoneDark = Mat("QuayStoneDark", Hex("#7C7366"));
            var wood = Mat("Wood", Hex("#9C6B3F"));
            var woodDark = Mat("WoodDark", Hex("#6E4A2A"));
            var white = Mat("WallWhite", Hex("#F3EFE6"));

            // Malecón (muro del puerto)
            var quay = Prim(PrimitiveType.Cube, port, new Vector3(0, -1.5f, QuayZ - 4f), new Vector3(250f, 5.4f, 8f), stone, null, true, "Malecon");
            quay.isStatic = true;
            Prim(PrimitiveType.Cube, port, new Vector3(0, PortY + 0.1f, QuayZ - 0.3f), new Vector3(250f, 0.25f, 0.6f), stoneDark);
            for (int i = -12; i <= 12; i++)
                if (Mathf.Abs(i) > 0) Prim(PrimitiveType.Cylinder, port, new Vector3(i * 10f, PortY + 0.45f, QuayZ - 0.4f), new Vector3(0.35f, 0.35f, 0.35f), Mat("Bollard", Hex("#3B3F44")));

            // Muelle de madera
            var pier = Empty("Muelle", port, Vector3.zero).transform;
            var deck = Prim(PrimitiveType.Cube, pier, new Vector3(0, PortY + 0.1f, -55f), new Vector3(4f, 0.25f, 26f), wood, null, false, "Tablero");
            for (int i = 0; i < 26; i++) Prim(PrimitiveType.Cube, pier, new Vector3(0, PortY + 0.24f, -67.5f + i), new Vector3(4.05f, 0.03f, 0.08f), woodDark);
            for (int i = 0; i < 7; i++)
                foreach (int s in new[] { -1, 1 })
                    Prim(PrimitiveType.Cylinder, pier, new Vector3(s * 1.85f, -0.6f, -67f + i * 4.2f), new Vector3(0.35f, 2.2f, 0.35f), woodDark);
            var pierCol = Empty("ColliderMuelle", pier, new Vector3(0, -0.3f, -55f));
            pierCol.AddComponent<BoxCollider>().size = new Vector3(4.2f, 4f, 26f);
            // Barandas y postes de amarre
            for (int i = 0; i < 6; i++) Prim(PrimitiveType.Cylinder, pier, new Vector3(2.0f, PortY + 0.5f, -66 + i * 5f), new Vector3(0.22f, 0.35f, 0.22f), Mat("Bollard", Hex("#3B3F44")));
            // Neumáticos colgados (defensas)
            for (int i = 0; i < 5; i++) MeshObj("Defensa", pier, CachedMesh("Tyre", () => MeshGen.Torus(0.2f, 0.08f, 12, 6)), Mat("Rubber", Hex("#222628"), 0.1f), new Vector3(2.1f, 0.6f, -64 + i * 5f), Vector3.one * 1.5f, new Vector3(0, 0, 90));

            // Zona de atraque
            var dockGo = Empty("ZonaAtraque", port, new Vector3(6f, 0, -52f));
            dockZone = dockGo.AddComponent<DockZone>();
            dockZone.radius = 11f;
            var dp = Empty("PuntoAtraque", dockGo.transform, Vector3.zero).transform;
            dp.position = DockPoint; dp.rotation = Quaternion.identity;
            dockZone.dockPoint = dp;
            var hl = MeshObj("IndicadorAtraque", dockGo.transform, CachedMesh("DockRing", () => MeshGen.Ring(4.2f, 4.7f, 40, "DockRingMesh")), _ringMat, Vector3.zero, null, null, false);
            hl.transform.position = new Vector3(DockPoint.x, 0.3f, DockPoint.z);
            hl.AddComponent<Spinner>().speed = new Vector3(0, 20, 0);
            dockZone.dockHighlight = hl;
            dockZone.cameraPoint = Empty("CamaraAtraque", dockGo.transform, Vector3.zero).transform;
            dockZone.cameraPoint.position = new Vector3(19f, 7.5f, -42f);
            dockZone.cameraLook = Empty("MiradaAtraque", dockGo.transform, Vector3.zero).transform;
            dockZone.cameraLook.position = new Vector3(3f, 2.2f, -66f);
            Buoy(port, new Vector3(-6, 0, -40), Hex("#3E9E5C"), "");
            Buoy(port, new Vector3(14, 0, -40), Hex("#E2453C"), "");

            // Arco "Puerto de Pisco"
            var arch = Empty("Arco", port, new Vector3(0, PortY, QuayZ - 2.5f)).transform;
            Prim(PrimitiveType.Cube, arch, new Vector3(-2.6f, 2.2f, 0), new Vector3(0.5f, 4.4f, 0.5f), Mat("ArchBlue", Hex("#1E6FC0")));
            Prim(PrimitiveType.Cube, arch, new Vector3(2.6f, 2.2f, 0), new Vector3(0.5f, 4.4f, 0.5f), Mat("ArchBlue", Hex("#1E6FC0")));
            Prim(PrimitiveType.Cube, arch, new Vector3(0, 4.6f, 0), new Vector3(6.4f, 1.1f, 0.4f), Mat("ArchYellow", Hex("#F6C445")));
            Sign(arch, new Vector3(0, 4.6f, 0.25f), "PUERTO DE PISCO", 0.09f, Hex("#0E2A47"), 0);
            Sign(arch, new Vector3(0, 4.6f, -0.25f), "PUERTO DE PISCO", 0.09f, Hex("#0E2A47"), 180);

            // Banderines entre postes
            var flags = Empty("Banderines", port, new Vector3(0, PortY, QuayZ - 6f)).transform;
            Color[] fc = { Hex("#E2453C"), Hex("#F6C445"), Hex("#2BB3A3"), Hex("#1E6FC0"), Hex("#F5F5F0") };
            var pennant = CachedMesh("Pennant", MeshGen.Pennant);
            for (int i = 0; i < 26; i++)
            {
                float x = -26 + i * 2f;
                var f = MeshObj("Banderin", flags, pennant, Mat("Pennant" + (i % 5), fc[i % 5]), new Vector3(x, 5.2f - Mathf.Sin(i / 25f * Mathf.PI) * 0.8f, 0), Vector3.one * 0.8f, new Vector3(0, 90, 0), false);
                var fw = f.AddComponent<FlagWave>(); fw.axis = Vector3.up; fw.amplitude = 25f; fw.frequency = 3f;
            }
            Prim(PrimitiveType.Cylinder, flags, new Vector3(-27, 2.8f, 0), new Vector3(0.18f, 2.8f, 0.18f), woodDark);
            Prim(PrimitiveType.Cylinder, flags, new Vector3(27, 2.8f, 0), new Vector3(0.18f, 2.8f, 0.18f), woodDark);

            // Taller de Don Mateo
            var shop = Building(port, new Vector3(-16, PortY, -82), new Vector3(10, 5, 8), Hex("#5FA3E8"), Hex("#C8423A"), "TALLER DON MATEO", 0);
            Prim(PrimitiveType.Cube, shop, new Vector3(0, 1.7f, 4.02f), new Vector3(4.2f, 3.4f, 0.1f), woodDark);
            // Casco en reparación junto al taller
            var repair = Empty("ChalanaEnReparacion", port, new Vector3(-30, PortY + 0.8f, -80), new Vector3(0, 30, 8)).transform;
            BuildBoatModel(repair, DecoSchemes[1], false, false, "Deco1");
            Prim(PrimitiveType.Cube, port, new Vector3(-30, PortY + 0.35f, -79), new Vector3(1, 0.7f, 1.4f), woodDark);
            Prim(PrimitiveType.Cube, port, new Vector3(-31, PortY + 0.35f, -83), new Vector3(1, 0.7f, 1.4f), woodDark);

            // Centro de reciclaje de Doña Rosa
            var rec = Building(port, new Vector3(18, PortY, -82), new Vector3(12, 4.5f, 8), Hex("#5BC17F"), Hex("#2BB3A3"), "CENTRO DE RECICLAJE", 0);
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Resources/GameConfig.asset");
            for (int i = 0; i < cfg.bins.Length; i++)
            {
                var bin = cfg.bins[i];
                var b = Empty("Contenedor_" + bin.type, port, new Vector3(10 + i * 2.4f, PortY, -75f)).transform;
                var bm = Mat("Bin_" + bin.type, bin.color, 0.3f);
                Prim(PrimitiveType.Cube, b, new Vector3(0, 0.75f, 0), new Vector3(1.6f, 1.5f, 1.4f), bm);
                Prim(PrimitiveType.Cube, b, new Vector3(0, 1.56f, -0.05f), new Vector3(1.7f, 0.12f, 1.5f), Mat("BinLid_" + bin.type, Color.Lerp(bin.color, Color.black, 0.25f)));
                Sign(b, new Vector3(0, 0.95f, 0.71f), bin.displayName.ToUpper(), 0.035f, bin.textColor, 0);
            }

            // Casas coloridas
            Color[] wallCols = { Hex("#F6C445"), Hex("#E2453C"), Hex("#2BB3A3"), Hex("#F28C28"), Hex("#D9577A"), Hex("#5FA3E8"), Hex("#F3EFE6"), Hex("#9BB86A") };
            Color[] roofCols = { Hex("#C8423A"), Hex("#8A5A3C"), Hex("#2F78C4") };
            var rng = new System.Random(7);
            for (int i = 0; i < 8; i++)
            {
                float x = -66 + i * 17f + (float)rng.NextDouble() * 3f;
                if (Mathf.Abs(x + 16) < 8 || Mathf.Abs(x - 18) < 9) x += 9f;
                float z = -98 - (float)rng.NextDouble() * 6f;
                Building(port, new Vector3(x, PortY, z), new Vector3(7 + (float)rng.NextDouble() * 3, 4 + (float)rng.NextDouble() * 3, 6), wallCols[i % wallCols.Length], roofCols[i % roofCols.Length], null, 0);
            }
            // Detalles: cajas, barriles, redes colgadas, palmeras, faroles
            var crate = Mat("Crate", Hex("#B07C46"));
            for (int i = 0; i < 10; i++)
                Prim(PrimitiveType.Cube, port, new Vector3(-10 + (i % 5) * 1.1f + (i / 5) * 30f, PortY + 0.45f + (i % 2) * 0.9f, -72 - (i % 3)), Vector3.one * 0.9f, crate, new Vector3(0, i * 17, 0), true);
            var barrel = Mat("Barrel", Hex("#2F78C4"));
            for (int i = 0; i < 5; i++) Prim(PrimitiveType.Cylinder, port, new Vector3(30 + i * 1.2f, PortY + 0.6f, -74f), new Vector3(0.8f, 0.6f, 0.8f), barrel, null, true);
            for (int i = 0; i < 3; i++)
            {
                var rack = Empty("RedColgada", port, new Vector3(-44 + i * 6f, PortY, -76f)).transform;
                Prim(PrimitiveType.Cylinder, rack, new Vector3(-1.5f, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), woodDark);
                Prim(PrimitiveType.Cylinder, rack, new Vector3(1.5f, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), woodDark);
                Prim(PrimitiveType.Cylinder, rack, new Vector3(0, 2.55f, 0), new Vector3(0.08f, 1.6f, 0.08f), woodDark, new Vector3(0, 0, 90));
                var net = Empty("Red", rack, new Vector3(0, 2.5f, 0)).transform;
                Prim(PrimitiveType.Cube, net, new Vector3(0, -0.8f, 0), new Vector3(2.8f, 1.6f, 0.05f), Mat("HangingNet", Hex("#2E7D63"), 0.05f));
                var fw = net.gameObject.AddComponent<FlagWave>(); fw.axis = Vector3.right; fw.amplitude = 6f; fw.frequency = 1.3f;
            }
            for (int i = 0; i < 10; i++)
            {
                float x = -100 + i * 22f;
                if (Mathf.Abs(x) < 6) continue;
                Palm(port, new Vector3(x, PortY, -90f + (i % 2) * 4f), rng);
            }
            for (int i = -5; i <= 5; i++)
            {
                var lamp = Empty("Farol", port, new Vector3(i * 12f + 6f, PortY, QuayZ - 1.5f)).transform;
                Prim(PrimitiveType.Cylinder, lamp, new Vector3(0, 1.8f, 0), new Vector3(0.12f, 1.8f, 0.12f), Mat("LampPost", Hex("#2A2F35")));
                Prim(PrimitiveType.Sphere, lamp, new Vector3(0, 3.7f, 0), Vector3.one * 0.4f, EmissiveMat("LampGlow", Hex("#FFE7A8"), 0.6f));
            }
            // Cartel educativo ODS
            var board = Empty("CartelODS", port, new Vector3(-6, PortY, -74)).transform;
            Prim(PrimitiveType.Cube, board, new Vector3(0, 1.8f, 0), new Vector3(3.6f, 1.6f, 0.12f), Mat("BoardBlue", Hex("#0E2A47")));
            Prim(PrimitiveType.Cylinder, board, new Vector3(-1.5f, 0.6f, 0), new Vector3(0.1f, 0.6f, 0.1f), woodDark);
            Prim(PrimitiveType.Cylinder, board, new Vector3(1.5f, 0.6f, 0), new Vector3(0.1f, 0.6f, 0.1f), woodDark);
            Sign(board, new Vector3(0, 1.8f, 0.07f), "¡CUIDEMOS EL MAR!\nODS 12 · 13 · 14", 0.05f, Color.white, 0);
            Sign(board, new Vector3(0, 1.8f, -0.07f), "¡CUIDEMOS EL MAR!\nODS 12 · 13 · 14", 0.05f, Color.white, 180);

            // NPC
            var mateo = Place(_mateoPrefab, port, new Vector3(-5f, PortY, -71f), Quaternion.Euler(0, 160, 0));
            var rosa = Place(_rosaPrefab, port, new Vector3(9f, PortY, -71.5f), Quaternion.Euler(0, 200, 0));
            var fisher = Place(_fisherPrefab, port, new Vector3(-1.2f, PortY + 0.2f, -43.2f), Quaternion.Euler(0, 0, 0));
            fisher.transform.localScale = Vector3.one;
            // Gaviotas del puerto
            for (int i = 0; i < 4; i++)
            {
                var gull = Place(_gullPrefab, port, new Vector3(0, 10, -60), Quaternion.identity);
                var gf = gull.GetComponent<GullFlier>(); gf.center = new Vector3(i * 6 - 10, 0, -62); gf.radius = 8 + i * 4; gf.height = 9 + i;
            }
            // Barcos de fondo
            DecoBoat(port, new Vector3(28, 0, -54), 15, 0);
            DecoBoat(port, new Vector3(-24, 0, -52), -20, 2);
            DecoBoat(port, new Vector3(-62, 0, -40), 70, 1);
            DecoBoat(port, new Vector3(70, 0, -30), -60, 2);
            DecoBoat(port, new Vector3(150, 0, 20), 100, 0);

            finaleCam = Empty("CamaraFinal", port, new Vector3(30, 16, -20)).transform;
            finaleLook = Empty("MiradaFinal", port, new Vector3(2, 2, -62)).transform;
        }

        static Transform Building(Transform parent, Vector3 pos, Vector3 size, Color wall, Color roof, string sign, float yaw)
        {
            var b = Empty("Edificio", parent, pos, new Vector3(0, yaw, 0)).transform;
            var wm = Mat("Wall_" + ColorUtility.ToHtmlStringRGB(wall), wall);
            var rm = Mat("Roof_" + ColorUtility.ToHtmlStringRGB(roof), roof);
            var body = Prim(PrimitiveType.Cube, b, new Vector3(0, size.y / 2f, 0), size, wm, null, true);
            body.isStatic = true;
            MeshObj("Techo", b, CachedMesh("GableRoof", MeshGen.GableRoof), rm, new Vector3(0, size.y, 0), new Vector3(size.x + 0.8f, 2.2f, size.z + 0.8f), new Vector3(0, 90, 0));
            var dark = Mat("WindowDark", Hex("#1C3A52"), 0.7f);
            var frame = Mat("WindowFrame", Hex("#F5F5F0"));
            int nw = Mathf.Max(1, Mathf.FloorToInt(size.x / 3.2f));
            for (int i = 0; i < nw; i++)
            {
                float x = -size.x / 2f + (i + 0.5f) * size.x / nw;
                Prim(PrimitiveType.Cube, b, new Vector3(x, size.y * 0.6f, size.z / 2f + 0.02f), new Vector3(1.1f, 1.1f, 0.05f), frame);
                Prim(PrimitiveType.Cube, b, new Vector3(x, size.y * 0.6f, size.z / 2f + 0.05f), new Vector3(0.9f, 0.9f, 0.05f), dark);
            }
            if (sign != null)
            {
                Prim(PrimitiveType.Cube, b, new Vector3(0, size.y + 0.1f, size.z / 2f + 0.5f), new Vector3(size.x * 0.8f, 1.1f, 0.15f), frame);
                Sign(b, new Vector3(0, size.y + 0.1f, size.z / 2f + 0.6f), sign, 0.07f, Hex("#0E2A47"), 0);
            }
            return b;
        }

        static void Sign(Transform parent, Vector3 local, string text, float size, Color color, float yaw)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0, 180 + yaw, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.fontSize = 64; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = color; tm.fontStyle = FontStyle.Bold;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        static void Palm(Transform parent, Vector3 pos, System.Random rng)
        {
            var p = Empty("Palmera", parent, pos).transform;
            var trunk = Mat("PalmTrunk", Hex("#8A6A48"));
            var leaf = Mat("PalmLeaf", Hex("#3E9E5C"));
            float lean = (float)rng.NextDouble() * 10f - 5f;
            Vector3 top = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                top = new Vector3(lean * 0.02f * i * i * 0.3f, 0.9f + i * 1.0f, 0);
                Prim(PrimitiveType.Cylinder, p, top, new Vector3(0.45f - i * 0.04f, 0.5f, 0.45f - i * 0.04f), trunk);
            }
            var crown = Empty("Copa", p, top + Vector3.up * 0.5f).transform;
            for (int i = 0; i < 7; i++)
            {
                var l = Empty("Hoja", crown, Vector3.zero, new Vector3(25, i * 51.4f, 0)).transform;
                Prim(PrimitiveType.Cube, l, new Vector3(0, 0, 1.4f), new Vector3(0.7f, 0.06f, 2.8f), leaf, new Vector3(12, 0, 0));
            }
            var fw = crown.gameObject.AddComponent<FlagWave>(); fw.axis = Vector3.forward; fw.amplitude = 3f; fw.frequency = 0.8f;
        }
    }
}
#endif
