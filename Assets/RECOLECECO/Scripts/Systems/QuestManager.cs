using UnityEngine;

namespace RecolecEco
{
    /// <summary>Cinco misiones encadenadas (§4.6) y el marcador del siguiente destino.</summary>
    public class QuestManager : MonoBehaviour
    {
        public Transform mission1Point;

        public static readonly string[] Titles =
        {
            "Primer viaje", "Una bahía más limpia", "Nada se desperdicia", "Una chalana mejor", "Guardianes de la bahía"
        };
        public static readonly string[] Givers =
        {
            DialogueScripts.Mateo, DialogueScripts.Mateo, DialogueScripts.Rosa, DialogueScripts.Mateo, "Don Mateo y Doña Rosa"
        };

        GameManager GM => GameManager.I;
        float _checkTimer;

        public int Index => GM.State.missionIndex;
        public bool AllDone => Index >= 5;

        public Vector3 TargetPosition { get; private set; }
        public string TargetLabel { get; private set; }
        public bool HasTarget { get; private set; }

        void OnEnable() { GameEvents.StateChanged += OnState; }
        void OnDisable() { GameEvents.StateChanged -= OnState; }
        void OnState() { _checkTimer = 0f; }

        void Update()
        {
            if (GM == null || GM.boat == null) return;
            UpdateTarget();
            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;
            _checkTimer = 0.2f;

            if (Index == 0 && !GM.State.mission1Reached && mission1Point != null)
            {
                Vector3 d = GM.boat.transform.position - mission1Point.position; d.y = 0;
                if (d.magnitude <= GM.Config.mission1Radius) GM.State.mission1Reached = true;
            }
            if (!AllDone && GM.State.introShown && IsComplete(Index)) CompleteCurrent();
        }

        public int SectorTotal(SectorId s) { int n = 0; foreach (var w in CollectibleWaste.All) if (w.Sector == s) n++; return n; }

        public int SectorCollected(SectorId s)
        {
            int n = 0;
            foreach (var w in CollectibleWaste.All) if (w.Sector == s && GM.State.collectedIds.Contains(w.Id)) n++;
            return n;
        }

        public int SectorTarget(SectorId s) => Mathf.CeilToInt(SectorTotal(s) * GM.Config.mission5Fraction - 0.0001f);

        public bool IsComplete(int i)
        {
            switch (i)
            {
                case 0: return GM.State.mission1Reached;
                case 1: return SectorCollected(SectorId.A) >= GM.Config.mission2Target;
                case 2: return GM.State.totalSortedCorrect >= GM.Config.mission3Target;
                case 3: return GM.State.UpgradeCount >= 1;
                case 4: return SectorCollected(SectorId.B) >= SectorTarget(SectorId.B) && SectorCollected(SectorId.C) >= SectorTarget(SectorId.C);
            }
            return true;
        }

        public string ProgressText(int i)
        {
            switch (i)
            {
                case 0: return GM.State.mission1Reached ? "Punto alcanzado" : "Navega hasta la boya marcada del Sector A";
                case 1: return $"Residuos del Sector A: {Mathf.Min(SectorCollected(SectorId.A), GM.Config.mission2Target)}/{GM.Config.mission2Target}";
                case 2: return $"Atraca (E) y clasifica: {Mathf.Min(GM.State.totalSortedCorrect, GM.Config.mission3Target)}/{GM.Config.mission3Target}";
                case 3: return $"Compra una mejora en el taller ({GM.State.UpgradeCount}/1)";
                case 4:
                    return $"Sector B: {SectorCollected(SectorId.B)}/{SectorTarget(SectorId.B)}   ·   Sector C: {SectorCollected(SectorId.C)}/{SectorTarget(SectorId.C)}";
            }
            return "¡Campaña completada! Sigue navegando libremente.";
        }

        public float ProgressFraction(int i)
        {
            switch (i)
            {
                case 0: return GM.State.mission1Reached ? 1 : 0;
                case 1: return Mathf.Clamp01(SectorCollected(SectorId.A) / (float)GM.Config.mission2Target);
                case 2: return Mathf.Clamp01(GM.State.totalSortedCorrect / (float)GM.Config.mission3Target);
                case 3: return GM.State.UpgradeCount >= 1 ? 1 : 0;
                case 4:
                    float b = Mathf.Clamp01(SectorCollected(SectorId.B) / (float)Mathf.Max(1, SectorTarget(SectorId.B)));
                    float c = Mathf.Clamp01(SectorCollected(SectorId.C) / (float)Mathf.Max(1, SectorTarget(SectorId.C)));
                    return (b + c) * 0.5f;
            }
            return 1;
        }

        void CompleteCurrent()
        {
            int done = Index;
            GM.State.missionIndex++;
            AudioManager.I.PlaySfx(Sfx.Mission);
            GameEvents.RaiseToast($"¡Misión completada: {Titles[done]}!");
            GameEvents.RaiseMissionCompleted(done);
            GM.Autosave("misión completada");
            GameEvents.RaiseStateChanged();

            if (AllDone) { GM.BeginFinale(); return; }
            GameEvents.RaiseMissionStarted(Index);
            var lines = DialogueScripts.MissionStart(Index);
            // Si estamos en el puerto con un panel abierto, el diálogo espera a que se cierre.
            StartCoroutine(ShowWhenFree(lines));
        }

        System.Collections.IEnumerator ShowWhenFree(DialogueLine[] lines)
        {
            yield return new WaitForSeconds(0.8f);
            while (GM.ModalCount > 0) yield return null;
            GM.Dialogue.Show(lines);
        }

        public void AnnounceCurrent()
        {
            if (!AllDone) GameEvents.RaiseMissionStarted(Index);
        }

        // ---------- Marcador de destino ----------
        void UpdateTarget()
        {
            HasTarget = false;
            if (GM.InVictory) return;
            var inv = GM.Inventory;
            var boatPos = GM.boat.transform.position;
            bool docked = GM.boat.IsDocked;

            if (inv.IsFull && !docked) { SetTarget(GM.dock.transform.position, "Muelle: bodega llena"); return; }

            switch (Index)
            {
                case 0:
                    if (mission1Point != null) SetTarget(mission1Point.position, "Boya del Sector A");
                    return;
                case 1:
                    NearestWaste(boatPos, w => w.Sector == SectorId.A, "Residuo (Sector A)");
                    return;
                case 2:
                    if (!inv.IsEmpty) { if (!docked) SetTarget(GM.dock.transform.position, "Muelle: clasificar"); }
                    else NearestWaste(boatPos, w => true, "Residuo");
                    return;
                case 3:
                    if (GM.State.credits >= GM.Upgrades.CheapestAvailableCost || !inv.IsEmpty)
                    { if (!docked) SetTarget(GM.dock.transform.position, "Muelle: taller"); }
                    else NearestWaste(boatPos, w => true, "Residuo");
                    return;
                case 4:
                    bool needB = SectorCollected(SectorId.B) < SectorTarget(SectorId.B);
                    bool needC = SectorCollected(SectorId.C) < SectorTarget(SectorId.C);
                    if (!NearestWaste(boatPos, w => (needB && w.Sector == SectorId.B) || (needC && w.Sector == SectorId.C), "Residuo (Sector B/C)")
                        && !inv.IsEmpty && !docked)
                        SetTarget(GM.dock.transform.position, "Muelle");
                    return;
            }
        }

        void SetTarget(Vector3 p, string label) { TargetPosition = p; TargetLabel = label; HasTarget = true; }

        bool NearestWaste(Vector3 from, System.Predicate<CollectibleWaste> filter, string label)
        {
            CollectibleWaste best = null;
            float bd = float.MaxValue;
            foreach (var w in CollectibleWaste.All)
            {
                if (!w.IsAvailable || !filter(w)) continue;
                float d = (w.transform.position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = w; }
            }
            if (best == null) return false;
            SetTarget(best.transform.position, label);
            return true;
        }
    }
}
