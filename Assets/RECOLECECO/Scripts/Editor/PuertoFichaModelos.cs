using System.Text;
using UnityEditor;
using UnityEngine;

namespace RecolecEco.EditorTools
{
    internal static class PuertoFichaModelos
    {
        [MenuItem("RECOLECECO/Puerto/Inspeccionar modelos seleccionados")]
        private static void InspectModels()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Selecciona un modelo o un objeto del muelle.");
                return;
            }

            foreach (GameObject root in Selection.gameObjects)
            {
                var report = new StringBuilder();
                report.AppendLine("Modelo: " + root.name);
                report.AppendLine("Escala local: " + root.transform.localScale);
                report.AppendLine("Escala global: " + root.transform.lossyScale);
                string path = AssetDatabase.GetAssetPath(root);
                if (string.IsNullOrEmpty(path))
                    path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                report.AppendLine("Recurso: " + (string.IsNullOrEmpty(path) ? "Objeto de escena" : path));

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                report.AppendLine("Renderizadores: " + renderers.Length);
                foreach (Renderer renderer in renderers)
                {
                    report.AppendLine("  " + renderer.name + " | Tamano global: " + renderer.bounds.size);
                    foreach (Material material in renderer.sharedMaterials)
                        report.AppendLine("    Material: " + (material != null ? material.name : "Sin asignar"));
                }
                Debug.Log(report.ToString(), root);
            }
        }
    }
}
