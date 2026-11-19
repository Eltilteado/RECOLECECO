using UnityEngine;

namespace RecolecEco
{
    /// <summary>
    /// Indicador «Limpieza de la bahía» (§4.7): residuos retirados / total, hitos 25-50-75-100 %
    /// y cambios visibles por sector (agua, manchas, fauna).
    /// </summary>
    public class EnvironmentProgress : MonoBehaviour
    {
        public SectorZone[] sectors;
        [Tooltip("Fauna que aparece con la limpieza global (ej. delfines al 75 %)")]
        public FaunaGroup[] globalFauna;

        GameManager GM => GameManager.I;
        bool _started;

        public int TotalWaste => CollectibleWaste.All.Count;
        public int Retired => GM.State.collectedIds.Count;
        public float Cleanliness01 => TotalWaste == 0 ? 1f : Mathf.Clamp01(Retired / (float)TotalWaste);
        public int CleanPercent => Mathf.FloorToInt(Cleanliness01 * 100f + 0.0001f);

        void OnEnable() { GameEvents.WasteCollected += OnCollected; }
        void OnDisable() { GameEvents.WasteCollected -= OnCollected; }

        void Start()
        {
            _started = true;
            foreach (var s in sectors) if (s != null) s.SnapVisuals(SectorClean01(s.id));
            UpdateGlobalFauna(true);
        }

        public float SectorClean01(SectorId id)
        {
            int total = 0, got = 0;
            foreach (var w in CollectibleWaste.All)
            {
                if (w.Sector != id) continue;
                total++;
                if (GM.State.collectedIds.Contains(w.Id)) got++;
            }
            return total == 0 ? 1f : got / (float)total;
        }

        void Update()
        {
            if (!_started) return;
            for (int i = 0; i < sectors.Length && i < 3; i++)
            {
                var s = sectors[i];
                if (s == null) continue;
                s.Tick(SectorClean01(s.id));
                Shader.SetGlobalVector("_RecSector" + i, new Vector4(s.transform.position.x, s.transform.position.z, s.radius, s.CurrentDirt));
            }
        }

        void OnCollected(CollectibleWaste w)
        {
            int pct = CleanPercent;
            int reached = GM.State.milestoneReached;
            foreach (var m in GM.Config.milestones)
            {
                if (pct >= m && reached < m)
                {
                    reached = m;
                    GM.State.milestoneReached = m;
                    AudioManager.I.PlaySfx(Sfx.Milestone);
                    GameEvents.RaiseToast($"¡Hito de limpieza: {m} % de la bahía!");
                    GameEvents.RaiseMilestone(m);
                    GM.Dialogue.Show(DialogueScripts.Milestone(m));
                }
            }
            UpdateGlobalFauna(false);
        }

        void UpdateGlobalFauna(bool instant)
        {
            if (globalFauna == null) return;
            foreach (var f in globalFauna) if (f != null) f.SetVisible(Cleanliness01 >= f.threshold, instant);
        }
    }
}
