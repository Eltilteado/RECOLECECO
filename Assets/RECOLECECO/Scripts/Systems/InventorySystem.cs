using UnityEngine;

namespace RecolecEco
{
    /// <summary>Bodega de la chalana (§4.3).</summary>
    public class InventorySystem : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        public int Capacity => GM.Capacity;

        public int Used
        {
            get
            {
                int n = 0;
                foreach (var c in GM.State.inventory) n += c;
                return n;
            }
        }

        public int Free => Mathf.Max(0, Capacity - Used);
        public bool IsFull => Used >= Capacity;
        public bool IsEmpty => Used == 0;

        public int Get(WasteType t) => GM.State.inventory[(int)t];

        public bool TryAdd(WasteType t)
        {
            if (IsFull) return false;
            GM.State.inventory[(int)t]++;
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool Remove(WasteType t)
        {
            if (GM.State.inventory[(int)t] <= 0) return false;
            GM.State.inventory[(int)t]--;
            GameEvents.RaiseStateChanged();
            return true;
        }
    }
}
