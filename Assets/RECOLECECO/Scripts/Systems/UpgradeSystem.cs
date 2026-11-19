using UnityEngine;

namespace RecolecEco
{
    /// <summary>Taller de Don Mateo: tres mejoras de la misma chalana (§4.5).</summary>
    public class UpgradeSystem : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        public bool IsOwned(UpgradeType t) => GM.State.HasUpgrade(t);
        public int Cost(UpgradeType t) => GM.Config.GetUpgrade(t).cost;
        public bool CanAfford(UpgradeType t) => GM.State.credits >= Cost(t);
        public bool CanBuy(UpgradeType t) => !IsOwned(t) && CanAfford(t);

        public int CheapestAvailableCost
        {
            get
            {
                int best = int.MaxValue;
                foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
                    if (!IsOwned(t)) best = Mathf.Min(best, Cost(t));
                return best;
            }
        }

        public bool Buy(UpgradeType t)
        {
            if (!CanBuy(t)) { AudioManager.I.PlaySfx(Sfx.Wrong); return false; }
            GM.State.credits -= Cost(t);
            GM.State.upgrades[(int)t] = true;
            AudioManager.I.PlaySfx(Sfx.Buy);
            GameEvents.RaiseToast("¡" + GM.Config.GetUpgrade(t).displayName + " instalado en tu chalana!");
            GameEvents.RaiseStateChanged();
            return true;
        }
    }
}
