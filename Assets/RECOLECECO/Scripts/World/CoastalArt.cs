using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecolecEco
{
    /// <summary>Acabados visuales compartidos por la bahía y el menú.</summary>
    static class CoastalArt
    {
        const string ResourcePath = "ArteCosta/";
        static Mesh _heroHull;
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, string> SurfaceColors = new Dictionary<string, string>
        {
            { "Ivory", "#E7DFCC" }, { "Almond", "#D9AA73" },
            { "Oak", "#985E36" }, { "DarkWood", "#5C3830" },
            { "Navy", "#183D4B" }, { "Glass", "#25566C" }, { "GlassLight", "#609DAD" },
            { "Teal", "#338780" }, { "Brass", "#DEA951" }, { "Rust", "#A54F39" },
            { "Coral", "#CF694C" }, { "Rubber", "#293738" }, { "Steel", "#849A9F" },
            { "Canvas", "#DAD1AE" }, { "Foam", "#F8EAAD" }, { "Ochre", "#C77936" }
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Subscribe()
        {
            SceneManager.sceneLoaded -= DressScene;
            SceneManager.sceneLoaded += DressScene;
        }

        static void DressScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameManager.GameScene && scene.name != GameManager.MenuScene) return;

            var cabinArt = Resources.Load<GameObject>(ResourcePath + "Cabina");
            var equipment = Resources.Load<GameObject>(ResourcePath + "BodegaPesca");
            var motor = Resources.Load<GameObject>(ResourcePath + "Motor");
            var net = Resources.Load<GameObject>(ResourcePath + "RedTrenzada");
            var dockProps = Resources.Load<GameObject>(ResourcePath + "AparejosMuelle");
            var shutters = Resources.Load<GameObject>(ResourcePath + "Postigos");

            foreach (var root in scene.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "Modelo" && item.Find("Casco"))
                    DressBoat(item, cabinArt, equipment, motor, net);
                else if (item.name == "PuertoDePisco")
                    DressPort(item, dockProps, shutters);
            }

            if (RenderSettings.sun)
            {
                RenderSettings.sun.transform.rotation = Quaternion.Euler(24f, -38f, 0);
                RenderSettings.sun.color = UIColors.Hex("#FFE0B1");
                RenderSettings.sun.intensity = 1.2f;
                RenderSettings.sun.shadowStrength = 0.8f;
            }
            RenderSettings.ambientSkyColor = UIColors.Hex("#B0CFE1");
            RenderSettings.ambientEquatorColor = UIColors.Hex("#B1A49E");
            RenderSettings.ambientGroundColor = UIColors.Hex("#6B8090");
            RenderSettings.fogColor = UIColors.Hex("#A8C4D2");
            RenderSettings.fogStartDistance = 88f;
            RenderSettings.fogEndDistance = 485f;
        }

        static void DressBoat(Transform boat, GameObject cabinArt, GameObject equipment,
            GameObject motor, GameObject net)
        {
            var cabin = boat.Find("Cabina");
            if (cabin && cabinArt)
            {
                // La bandera y los anclajes de la animación siguen en sus objetos originales.
                foreach (Transform part in cabin)
                {
                    if (part.name == "Bandera") continue;
                    if (part.TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
                }
                Mount(cabinArt, cabin, Vector3.zero);
            }

            var armRoot = boat.Find("BrazoRecolector");
            if (armRoot)
            {
                var hull = boat.Find("Casco");
                if (hull.TryGetComponent<MeshFilter>(out var filter))
                {
                    if (!_heroHull)
                        _heroHull = MeshGen.ChalanaHull(UIColors.Hex("#9A4E3D"), UIColors.Hex("#DFD3B9"),
                            UIColors.Hex("#286C69"), UIColors.Hex("#D7AC66"), UIColors.Hex("#986840"),
                            UIColors.Hex("#DBCEB1"), detailed: true);
                    filter.sharedMesh = _heroHull;
                }

                if (net && armRoot.TryGetComponent<CollectorArm>(out var arm) && arm.net)
                {
                    foreach (Transform part in arm.net)
                        if ((part.name == "Aro" || part.name == "Malla")
                            && part.TryGetComponent<MeshRenderer>(out var renderer))
                            renderer.enabled = false;
                    Mount(net, arm.net, new Vector3(0, -0.1f, 0));
                }
            }

            if (armRoot && equipment)
            {
                var hold = boat.Find("Bodega");
                if (hold) Mount(equipment, hold, new Vector3(0.29f, 0.43f, -0.27f), 25f, 0.72f);
                if (cabin && cabinArt) Label(cabin, "LA ESPERANZA", new Vector3(0, 0.31f, -0.763f), 0);
            }

            if (motor)
            {
                DressMotor(boat.Find("MotorNormal"), motor, false);
                DressMotor(boat.Find("MotorMejorado"), motor, true);
            }
        }

        static void DressMotor(Transform mount, GameObject model, bool upgraded)
        {
            if (!mount) return;
            foreach (Transform part in mount)
            {
                if (part.localPosition.y <= 0f) continue;
                if (part.TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
            }
            Mount(model, mount, new Vector3(0, 0, upgraded ? -0.20f : -0.12f),
                0, upgraded ? 1.35f : 0.9f, upgraded);
        }

        static void DressPort(Transform port, GameObject dockProps, GameObject shutters)
        {
            if (dockProps)
            {
                Mount(dockProps, port, new Vector3(-0.8f, 1.45f, -60), 82, 0.67f);
                Mount(dockProps, port, new Vector3(-0.63f, 1.45f, -47), -88, 0.62f);
                Mount(dockProps, port, new Vector3(-9f, 1.25f, -74f), 16, 1.2f);
                Mount(dockProps, port, new Vector3(8.2f, 1.25f, -76f), 153, 1.1f);
            }

            if (!shutters) return;
            foreach (Transform building in port)
            {
                if (building.name != "Edificio") continue;
                // Las molduras nuevas se colocan sobre los marcos de ventana existentes.
                int children = building.childCount;
                for (int i = 0; i < children; i++)
                {
                    var part = building.GetChild(i);
                    if (!part.TryGetComponent<MeshRenderer>(out var renderer)
                        || !renderer.sharedMaterial || renderer.sharedMaterial.name != "M_WindowFrame"
                        || part.localScale.x < 1f || part.localScale.x > 1.2f
                        || part.localScale.y < 1f || part.localScale.y > 1.2f) continue;
                    Mount(shutters, building, part.localPosition + Vector3.forward * 0.045f);
                }
            }
        }

        static void Mount(GameObject source, Transform parent, Vector3 offset, float yaw = 0,
            float scale = 1f, bool upgraded = false)
        {
            var model = UnityEngine.Object.Instantiate(source, parent, false);
            model.name = source.name;
            model.transform.SetLocalPositionAndRotation(offset, Quaternion.Euler(0, yaw, 0));
            model.transform.localScale = Vector3.one * scale;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.gameObject.name;
                if (!name.StartsWith("Surf_", StringComparison.Ordinal)) continue;
                int suffix = name.IndexOf('.', 5);
                string key = suffix < 0 ? name.Substring(5) : name.Substring(5, suffix - 5);
                if (upgraded && key == "Teal") key = "Ochre";
                var material = GetMaterial(key);
                if (material) renderer.sharedMaterial = material;
            }
        }

        static Material GetMaterial(string key)
        {
            if (Materials.TryGetValue(key, out var material) && material) return material;
            if (!SurfaceColors.TryGetValue(key, out var hex)) return null;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) return null;

            material = new Material(shader) { name = "Costa · " + key };
            material.SetColor("_BaseColor", UIColors.Hex(hex));
            material.SetFloat("_Smoothness", key.StartsWith("Glass", StringComparison.Ordinal) ? 0.82f : 0.32f);
            material.SetFloat("_Metallic", key == "Steel" || key == "Brass" ? 0.35f : 0f);
            Materials[key] = material;
            return material;
        }

        static void Label(Transform parent, string text, Vector3 position, float yaw)
        {
            var sign = new GameObject("Nombre de la chalana");
            sign.transform.SetParent(parent, false);
            sign.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var label = sign.AddComponent<TextMesh>();
            label.font = UIFactory.Font;
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.027f;
            label.color = UIColors.Hex("#183D4B");
            sign.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }
    }
}
