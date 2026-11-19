using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    public struct SortResult
    {
        public bool correct;
        public int credits;
        public WasteType waste;
        public BinType correctBin;
        public bool firstTimeForType;
    }

    /// <summary>Centro de reciclaje de Doña Rosa (§4.4). Lógica pura: la vista llama a TrySort.</summary>
    public class RecyclingSystem : MonoBehaviour
    {
        readonly List<WasteType> _queue = new List<WasteType>();
        readonly HashSet<WasteType> _tipsShown = new HashSet<WasteType>();

        public int SessionCredits { get; private set; }
        public int SessionCorrect { get; private set; }
        public int SessionWrong { get; private set; }
        public bool HasCurrent => _queue.Count > 0;
        public WasteType Current => _queue[0];
        public int Remaining => _queue.Count;

        GameManager GM => GameManager.I;

        /// <summary>Prepara la mesa de clasificación con lo que hay en la bodega, mezclado.</summary>
        public void StartSession()
        {
            _queue.Clear();
            SessionCredits = SessionCorrect = SessionWrong = 0;
            for (int t = 0; t < 5; t++)
                for (int k = 0; k < GM.State.inventory[t]; k++) _queue.Add((WasteType)t);
            // Mezcla (Fisher-Yates)
            for (int i = _queue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_queue[i], _queue[j]) = (_queue[j], _queue[i]);
            }
        }

        public SortResult TrySort(BinType bin)
        {
            var r = new SortResult();
            if (!HasCurrent) return r;
            var waste = Current;
            var info = GM.Config.GetWaste(waste);
            r.waste = waste;
            r.correctBin = info.correctBin;
            r.correct = info.correctBin == bin;
            if (r.correct)
            {
                _queue.RemoveAt(0);
                GM.Inventory.Remove(waste);
                r.credits = info.value;
                r.firstTimeForType = _tipsShown.Add(waste);
                GM.State.sortedByType[(int)waste]++;
                GM.State.totalSortedCorrect++;
                SessionCredits += info.value;
                SessionCorrect++;
                GM.AddCredits(info.value);
                AudioManager.I.PlaySfx(Sfx.Correct);
            }
            else
            {
                SessionWrong++;
                AudioManager.I.PlaySfx(Sfx.Wrong);
            }
            GameEvents.RaiseStateChanged();
            return r;
        }
    }
}
