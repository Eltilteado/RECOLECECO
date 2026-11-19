using UnityEngine;

namespace RecolecEco
{
    /// <summary>Zona de la bahía (A, B, C) con sus cambios visuales al limpiarse.</summary>
    public class SectorZone : MonoBehaviour
    {
        public SectorId id;
        public string displayName;
        public float radius = 30f;
        [Tooltip("Fauna que aparece según la limpieza del sector")]
        public FaunaGroup[] fauna;
        [Tooltip("Manchas/espuma sucia que desaparecen al limpiar (umbral = limpieza a la que se ocultan)")]
        public FaunaGroup[] dirtProps;

        public float CurrentDirt { get; private set; } = 1f;

        static float DirtFromClean(float clean01) => Mathf.Clamp01(1f - clean01 * 1.15f);

        public void SnapVisuals(float clean01)
        {
            CurrentDirt = DirtFromClean(clean01);
            Apply(clean01, true);
        }

        public void Tick(float clean01)
        {
            CurrentDirt = Mathf.MoveTowards(CurrentDirt, DirtFromClean(clean01), Time.deltaTime * 0.25f);
            Apply(clean01, false);
        }

        void Apply(float clean01, bool instant)
        {
            if (fauna != null) foreach (var f in fauna) if (f != null) f.SetVisible(clean01 >= f.threshold, instant);
            if (dirtProps != null) foreach (var d in dirtProps) if (d != null) d.SetVisible(clean01 < d.threshold, instant);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.8f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
