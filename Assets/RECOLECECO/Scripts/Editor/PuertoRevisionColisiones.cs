using System.Text;
using UnityEditor;
using UnityEngine;

namespace RecolecEco.EditorTools
{
    internal static class PuertoRevisionColisiones
    {
        [MenuItem("RECOLECECO/Puerto/Revisar colisiones seleccionadas")]
        private static void InspectCollisions()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Selecciona las rocas o el rompeolas en la escena.");
                return;
            }

            foreach (GameObject root in Selection.gameObjects)
            {
                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                int solid = 0;
                var report = new StringBuilder();
                report.AppendLine("Objeto: " + root.name);
                foreach (Collider collider in colliders)
                {
                    bool usable = collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger;
                    if (usable) solid++;
                    report.AppendLine(collider.name + " | " + collider.GetType().Name
                        + " | activo: " + usable + " | trigger: " + collider.isTrigger);
                    if (collider is MeshCollider mesh && mesh.sharedMesh == null)
                        report.AppendLine("  Aviso: MeshCollider sin malla asignada.");
                }
                report.AppendLine("Colliders solidos activos: " + solid);
                report.AppendLine("Revisar el contorno en Scene y probar el recorrido en Play Mode.");
                if (solid == 0) Debug.LogWarning(report.ToString(), root);
                else Debug.Log(report.ToString(), root);
            }
        }
    }
}
