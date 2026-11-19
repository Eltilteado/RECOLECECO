#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;

namespace RecolecEco.EditorTools
{
    public static partial class RecolecBuilder
    {
        static readonly GameObject[] _wastePrefabs = new GameObject[5];
        static GameObject _mateoPrefab, _rosaPrefab, _fisherPrefab, _gullPrefab, _seaLionPrefab, _penguinPrefab, _boobyPrefab;
        static Mesh _ringMesh;

        // ------------------------------------------------------------ Residuos (4 tipos, 5 variantes)
        static void BuildWastePrefabs()
        {
            _ringMesh = CachedMesh("HighlightRingMesh", () => MeshGen.Ring(1.05f, 1.25f, 32));
            for (int i = 0; i < 5; i++)
            {
                var t = (WasteType)i;
                var root = new GameObject("Residuo_" + t);
                var visual = Empty("Visual", root.transform, Vector3.zero).transform;
                switch (t)
                {
                    case WasteType.Botella:
                    {
                        var body = Mat("W_BottleBody", Hex("#8FDDF5"), 0.85f, 0, true);
                        var label = Mat("W_BottleLabel", Hex("#2F78C4"), 0.3f, 0, true);
                        var cap = Mat("W_BottleCap", Hex("#1E8A4C"), 0.3f, 0, true);
                        var g = Empty("Botella", visual, Vector3.zero, new Vector3(0, 0, 90)).transform;
                        g.localScale = Vector3.one * 1.5f;
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, 0, 0), new Vector3(0.26f, 0.3f, 0.26f), body);
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, 0.04f, 0), new Vector3(0.27f, 0.1f, 0.27f), label);
                        Prim(PrimitiveType.Sphere, g, new Vector3(0, 0.3f, 0), new Vector3(0.26f, 0.2f, 0.26f), body);
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, 0.43f, 0), new Vector3(0.1f, 0.06f, 0.1f), body);
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, 0.5f, 0), new Vector3(0.12f, 0.03f, 0.12f), cap);
                        break;
                    }
                    case WasteType.Lata:
                    {
                        var red = Mat("W_CanRed", Hex("#E35D5B"), 0.6f, 0.3f, true);
                        var silver = Mat("W_CanSilver", Hex("#D9DEE3"), 0.8f, 0.8f, true);
                        var g = Empty("Lata", visual, Vector3.zero, new Vector3(80, 0, 20)).transform;
                        g.localScale = Vector3.one * 1.6f;
                        Prim(PrimitiveType.Cylinder, g, Vector3.zero, new Vector3(0.26f, 0.22f, 0.26f), red);
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, 0.225f, 0), new Vector3(0.24f, 0.012f, 0.24f), silver);
                        Prim(PrimitiveType.Cylinder, g, new Vector3(0, -0.225f, 0), new Vector3(0.24f, 0.012f, 0.24f), silver);
                        Prim(PrimitiveType.Cube, g, new Vector3(0, 0, 0.13f), new Vector3(0.12f, 0.2f, 0.01f), silver);
                        break;
                    }
                    case WasteType.Red:
                    {
                        var netMat = Mat("W_Net", Hex("#2E7D63"), 0.1f, 0, true);
                        var netLight = Mat("W_NetLight", Hex("#5FBF97"), 0.1f, 0, true);
                        var buoy = Mat("W_NetBuoy", Hex("#F28C28"), 0.4f, 0, true);
                        var g = Empty("Red", visual, Vector3.zero).transform;
                        var blob = MeshObj("Maraña", g, CachedMesh("NetBlob", () => MeshGen.Rock(77, 0.35f, Color.white, Color.white, 1, new Vector3(0.9f, 0.25f, 0.7f))), netMat, Vector3.zero);
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 60f;
                            Prim(PrimitiveType.Cylinder, g, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.3f, 0.12f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.3f), new Vector3(0.03f, 0.6f, 0.03f), netLight, new Vector3(90, a + 30, 0));
                        }
                        Prim(PrimitiveType.Sphere, g, new Vector3(0.6f, 0.15f, 0.2f), Vector3.one * 0.26f, buoy);
                        Prim(PrimitiveType.Sphere, g, new Vector3(-0.5f, 0.12f, -0.35f), Vector3.one * 0.22f, buoy);
                        break;
                    }
                    case WasteType.CajaCarton:
                    {
                        var card = Mat("W_Cardboard", Hex("#C9955A"), 0.1f, 0, true);
                        var tape = Mat("W_Tape", Hex("#E8C48A"), 0.3f, 0, true);
                        var g = Empty("Caja", visual, new Vector3(0, 0.1f, 0), new Vector3(0, 25, 8)).transform;
                        Prim(PrimitiveType.Cube, g, Vector3.zero, new Vector3(0.85f, 0.55f, 0.7f), card);
                        Prim(PrimitiveType.Cube, g, new Vector3(0, 0.28f, 0), new Vector3(0.18f, 0.02f, 0.72f), tape);
                        Prim(PrimitiveType.Cube, g, new Vector3(0.2f, 0.29f, 0.1f), new Vector3(0.4f, 0.03f, 0.3f), card, new Vector3(0, 0, 20));
                        break;
                    }
                    case WasteType.CajaTecnopor:
                    {
                        var foam = Mat("W_Foam", Hex("#F4F4EE"), 0.05f, 0, true);
                        var foamDark = Mat("W_FoamDark", Hex("#D4D8D2"), 0.05f, 0, true);
                        var g = Empty("Tecnopor", visual, new Vector3(0, 0.12f, 0), new Vector3(0, -15, -6)).transform;
                        Prim(PrimitiveType.Cube, g, Vector3.zero, new Vector3(0.95f, 0.5f, 0.65f), foam);
                        Prim(PrimitiveType.Cube, g, new Vector3(0, 0.27f, 0), new Vector3(1.0f, 0.06f, 0.7f), foamDark);
                        Prim(PrimitiveType.Cube, g, new Vector3(0.3f, 0.32f, -0.1f), new Vector3(0.25f, 0.06f, 0.2f), foam, new Vector3(0, 30, 0));
                        break;
                    }
                }
                var ring = MeshObj("Resaltado", root.transform, _ringMesh, _ringMat, new Vector3(0, 0.08f, 0), Vector3.one, null, false);
                var w = root.AddComponent<CollectibleWaste>();
                w.visual = visual;
                w.highlightRing = ring;
                _wastePrefabs[i] = SavePrefab(root, PrefabDir + "/Waste/Residuo_" + t + ".prefab");
            }
        }

        // ------------------------------------------------------------ Personas (low-poly con primitivas)
        static Transform BuildPerson(Transform parent, Vector3 pos, Quaternion rot, Color shirt, Color pants, Color skin, Color hair, string hat,
            out Transform head, out Transform armR, out Transform armL, out Transform body, float scale = 1f, Color? hatColor = null, bool apron = false, bool beard = false)
        {
            var root = new GameObject("Persona").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = rot;
            root.localScale = Vector3.one * scale;
            var mShirt = Mat("P_Shirt_" + ColorUtility.ToHtmlStringRGB(shirt), shirt);
            var mPants = Mat("P_Pants_" + ColorUtility.ToHtmlStringRGB(pants), pants);
            var mSkin = Mat("P_Skin_" + ColorUtility.ToHtmlStringRGB(skin), skin, 0.3f);
            var mHair = Mat("P_Hair_" + ColorUtility.ToHtmlStringRGB(hair), hair);
            var mEye = Mat("P_Eye", Hex("#1A1A1A"), 0.8f);

            Prim(PrimitiveType.Capsule, root, new Vector3(-0.12f, 0.42f, 0), new Vector3(0.18f, 0.42f, 0.18f), mPants);
            Prim(PrimitiveType.Capsule, root, new Vector3(0.12f, 0.42f, 0), new Vector3(0.18f, 0.42f, 0.18f), mPants);
            Prim(PrimitiveType.Cube, root, new Vector3(-0.12f, 0.04f, 0.05f), new Vector3(0.16f, 0.08f, 0.26f), Mat("P_Shoe", Hex("#3B2A1E")));
            Prim(PrimitiveType.Cube, root, new Vector3(0.12f, 0.04f, 0.05f), new Vector3(0.16f, 0.08f, 0.26f), Mat("P_Shoe", Hex("#3B2A1E")));
            body = Empty("Cuerpo", root, new Vector3(0, 0.8f, 0)).transform;
            Prim(PrimitiveType.Capsule, body, new Vector3(0, 0.32f, 0), new Vector3(0.5f, 0.38f, 0.32f), mShirt);
            if (apron) Prim(PrimitiveType.Cube, body, new Vector3(0, 0.2f, 0.15f), new Vector3(0.38f, 0.5f, 0.04f), Mat("P_Apron", Hex("#3E9E5C")));
            head = Empty("Cabeza", body, new Vector3(0, 0.78f, 0)).transform;
            Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.12f, 0), new Vector3(0.34f, 0.36f, 0.34f), mSkin);
            Prim(PrimitiveType.Sphere, head, new Vector3(-0.07f, 0.16f, 0.155f), Vector3.one * 0.045f, mEye);
            Prim(PrimitiveType.Sphere, head, new Vector3(0.07f, 0.16f, 0.155f), Vector3.one * 0.045f, mEye);
            Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.1f, 0.17f), new Vector3(0.06f, 0.07f, 0.06f), mSkin);
            Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.2f, -0.03f), new Vector3(0.36f, 0.28f, 0.36f), mHair);
            if (beard) Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.0f, 0.08f), new Vector3(0.28f, 0.18f, 0.24f), Mat("P_Beard", Hex("#E8E4DC")));
            var mHat = Mat("P_Hat_" + (hatColor.HasValue ? ColorUtility.ToHtmlStringRGB(hatColor.Value) : "def"), hatColor ?? Hex("#E9D8A6"));
            switch (hat)
            {
                case "straw":
                    Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.015f, 0.6f), mHat);
                    Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.37f, 0), new Vector3(0.3f, 0.07f, 0.3f), mHat);
                    break;
                case "cap":
                    Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.26f, 0), new Vector3(0.37f, 0.2f, 0.37f), mHat);
                    Prim(PrimitiveType.Cube, head, new Vector3(0, 0.25f, 0.2f), new Vector3(0.26f, 0.02f, 0.16f), mHat);
                    break;
                case "bun":
                    Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.3f, -0.14f), Vector3.one * 0.16f, mHair);
                    break;
            }
            armR = Empty("BrazoDer", body, new Vector3(0.3f, 0.55f, 0)).transform;
            Prim(PrimitiveType.Capsule, armR, new Vector3(0.02f, -0.27f, 0), new Vector3(0.13f, 0.28f, 0.13f), mShirt);
            Prim(PrimitiveType.Sphere, armR, new Vector3(0.02f, -0.56f, 0), Vector3.one * 0.12f, mSkin);
            armL = Empty("BrazoIzq", body, new Vector3(-0.3f, 0.55f, 0)).transform;
            Prim(PrimitiveType.Capsule, armL, new Vector3(-0.02f, -0.27f, 0), new Vector3(0.13f, 0.28f, 0.13f), mShirt);
            Prim(PrimitiveType.Sphere, armL, new Vector3(-0.02f, -0.56f, 0), Vector3.one * 0.12f, mSkin);
            return root;
        }

        static GameObject MakeNPC(string name, string speaker, Color shirt, Color pants, Color skin, Color hair, string hat, Color? hatColor, bool apron, bool beard, Color markerColor)
        {
            var root = new GameObject(name);
            var person = BuildPerson(root.transform, Vector3.zero, Quaternion.identity, shirt, pants, skin, hair, hat, out var head, out var armR, out var armL, out var body, 1f, hatColor, apron, beard);
            var anim = root.AddComponent<NPCAnimator>();
            anim.speakerName = speaker; anim.head = head; anim.armRight = armR; anim.armLeft = armL; anim.body = body;
            // Marcador de misión (signo ! flotante)
            var marker = Empty("MarcadorMision", root.transform, new Vector3(0, 2.6f, 0)).transform;
            var mm = EmissiveMat("Marker_" + name, markerColor, 1.5f);
            Prim(PrimitiveType.Cube, marker, new Vector3(0, 0.25f, 0), new Vector3(0.14f, 0.45f, 0.14f), mm);
            Prim(PrimitiveType.Cube, marker, new Vector3(0, -0.13f, 0), new Vector3(0.14f, 0.14f, 0.14f), mm);
            anim.questMarker = marker.gameObject;
            // Etiqueta con el nombre
            var label = new GameObject("Nombre");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0, 2.15f, 0);
            var tm = label.AddComponent<TextMesh>();
            tm.text = speaker; tm.fontSize = 48; tm.characterSize = 0.05f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white; tm.fontStyle = FontStyle.Bold;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.AddComponent<Billboard>();
            return SavePrefab(root, PrefabDir + "/Characters/" + name + ".prefab");
        }

        static void BuildCharacterPrefabs()
        {
            _mateoPrefab = MakeNPC("DonMateo", DialogueScripts.Mateo, Hex("#2F78C4"), Hex("#4A3B2C"), Hex("#B57B4E"), Hex("#E0DDD5"), "straw", Hex("#E9D8A6"), false, true, Hex("#F6C445"));
            _rosaPrefab = MakeNPC("DonaRosa", DialogueScripts.Rosa, Hex("#D9577A"), Hex("#2D3E5C"), Hex("#C68B59"), Hex("#2B2118"), "bun", null, true, false, Hex("#F6C445"));

            // Pescador local (decorativo, sentado en el muelle con caña)
            var fisher = new GameObject("PescadorLocal");
            var p = BuildPerson(fisher.transform, Vector3.zero, Quaternion.identity, Hex("#E9A23B"), Hex("#33475B"), Hex("#A8714A"), Hex("#1E1E1E"), "cap", out var fh, out var far, out var fal, out var fb, 1f, Hex("#C8423A"));
            far.localRotation = Quaternion.Euler(-60, 0, 0);
            fal.localRotation = Quaternion.Euler(-60, 0, 0);
            var rod = Prim(PrimitiveType.Cylinder, far, new Vector3(0, -0.55f, 1.0f), new Vector3(0.025f, 1.3f, 0.025f), Mat("Rod", Hex("#3B2A1E")), new Vector3(70, 0, 0));
            var a = fisher.AddComponent<NPCAnimator>(); a.speakerName = DialogueScripts.Fisher; a.head = fh; a.body = fb; a.waveDistance = 20f;
            _fisherPrefab = SavePrefab(fisher, PrefabDir + "/Characters/PescadorLocal.prefab");

            // Gaviota
            var gull = new GameObject("Gaviota");
            var gw = Mat("GullWhite", Hex("#F4F4F0"));
            var gg = Mat("GullGrey", Hex("#9AA5AE"));
            var gy = Mat("GullBeak", Hex("#F2B33D"));
            Prim(PrimitiveType.Capsule, gull.transform, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), gw, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Sphere, gull.transform, new Vector3(0, 0.08f, 0.28f), Vector3.one * 0.18f, gw);
            Prim(PrimitiveType.Cube, gull.transform, new Vector3(0, 0.06f, 0.42f), new Vector3(0.04f, 0.04f, 0.12f), gy);
            var wl = Empty("AlaIzq", gull.transform, new Vector3(-0.08f, 0.05f, 0)).transform;
            Prim(PrimitiveType.Cube, wl, new Vector3(-0.4f, 0, 0), new Vector3(0.8f, 0.03f, 0.25f), gg);
            var wr = Empty("AlaDer", gull.transform, new Vector3(0.08f, 0.05f, 0)).transform;
            Prim(PrimitiveType.Cube, wr, new Vector3(0.4f, 0, 0), new Vector3(0.8f, 0.03f, 0.25f), gg);
            var gf = gull.AddComponent<GullFlier>(); gf.wingL = wl; gf.wingR = wr;
            _gullPrefab = SavePrefab(gull, PrefabDir + "/Environment/Gaviota.prefab");

            // Lobo marino
            var sl = new GameObject("LoboMarino");
            var brown = Mat("SeaLion", Hex("#6B4F3A"), 0.6f);
            Prim(PrimitiveType.Capsule, sl.transform, new Vector3(0, 0.35f, 0), new Vector3(0.6f, 0.75f, 0.6f), brown, new Vector3(70, 0, 0));
            Prim(PrimitiveType.Sphere, sl.transform, new Vector3(0, 0.95f, 0.45f), new Vector3(0.38f, 0.36f, 0.42f), brown);
            Prim(PrimitiveType.Sphere, sl.transform, new Vector3(0, 0.92f, 0.66f), new Vector3(0.18f, 0.14f, 0.16f), Mat("SeaLionNose", Hex("#3A2A1E")));
            Prim(PrimitiveType.Cube, sl.transform, new Vector3(0.38f, 0.1f, 0.2f), new Vector3(0.4f, 0.06f, 0.2f), brown, new Vector3(0, 30, -20));
            Prim(PrimitiveType.Cube, sl.transform, new Vector3(-0.38f, 0.1f, 0.2f), new Vector3(0.4f, 0.06f, 0.2f), brown, new Vector3(0, -30, 20));
            sl.AddComponent<IdleBob>().turn = 25f;
            _seaLionPrefab = SavePrefab(sl, PrefabDir + "/Environment/LoboMarino.prefab");

            // Pingüino de Humboldt
            var pg = new GameObject("PinguinoHumboldt");
            Prim(PrimitiveType.Capsule, pg.transform, new Vector3(0, 0.3f, 0), new Vector3(0.3f, 0.32f, 0.28f), Mat("PenguinBlack", Hex("#22262A")));
            Prim(PrimitiveType.Capsule, pg.transform, new Vector3(0, 0.28f, 0.06f), new Vector3(0.24f, 0.26f, 0.2f), Mat("PenguinWhite", Hex("#F4F4F0")));
            Prim(PrimitiveType.Sphere, pg.transform, new Vector3(0, 0.62f, 0.02f), Vector3.one * 0.2f, Mat("PenguinBlack", Hex("#22262A")));
            Prim(PrimitiveType.Cube, pg.transform, new Vector3(0, 0.6f, 0.14f), new Vector3(0.05f, 0.04f, 0.1f), Mat("PenguinBeak", Hex("#2B2B2B")));
            pg.AddComponent<IdleBob>().turn = 40f;
            _penguinPrefab = SavePrefab(pg, PrefabDir + "/Environment/PinguinoHumboldt.prefab");

            // Ave guanera / piquero
            var bb = new GameObject("Piquero");
            Prim(PrimitiveType.Capsule, bb.transform, new Vector3(0, 0.25f, 0), new Vector3(0.24f, 0.26f, 0.24f), Mat("BoobyBody", Hex("#E9E4DA")));
            Prim(PrimitiveType.Sphere, bb.transform, new Vector3(0, 0.52f, 0.05f), Vector3.one * 0.16f, Mat("BoobyBody", Hex("#E9E4DA")));
            Prim(PrimitiveType.Cube, bb.transform, new Vector3(0, 0.5f, 0.17f), new Vector3(0.04f, 0.04f, 0.14f), Mat("BoobyBeak", Hex("#5C7A8C")));
            Prim(PrimitiveType.Cube, bb.transform, new Vector3(0, 0.27f, -0.1f), new Vector3(0.26f, 0.2f, 0.2f), Mat("BoobyWing", Hex("#7A6A58")));
            bb.AddComponent<IdleBob>().turn = 60f;
            _boobyPrefab = SavePrefab(bb, PrefabDir + "/Environment/Piquero.prefab");
        }
    }
}
#endif
