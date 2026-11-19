using UnityEngine;
using UnityEngine.InputSystem;

namespace RecolecEco
{
    /// <summary>
    /// Recolección manual con E (§0.3, §4.2): detecta el residuo más cercano dentro del alcance de la
    /// herramienta, lo resalta, muestra «E — Recoger» y acciona el brazo.
    /// </summary>
    public class CollectionSystem : MonoBehaviour
    {
        public BoatController boat;
        public CollectorArm arm;
        public Transform toolOrigin;

        public CollectibleWaste Target { get; private set; }
        public float LastFullWarningTime { get; private set; } = -100f;
        public int CollectedThisSession { get; private set; }

        GameManager GM => GameManager.I;

        void Update()
        {
            if (GM == null) return;
            bool canAct = !GM.ControlsLocked && !boat.IsDocked && !boat.IsDocking;
            var newTarget = canAct ? FindTarget() : null;
            if (newTarget != Target)
            {
                if (Target != null) Target.SetHighlighted(false);
                Target = newTarget;
                if (Target != null) Target.SetHighlighted(true);
            }

            if (Target != null && !arm.Busy)
            {
                if (GM.Inventory.IsFull) InteractionPrompt.Set(this, "Bodega llena, vuelve al puerto", 1);
                else InteractionPrompt.Set(this, "E — Recoger " + GM.Config.GetWaste(Target.Type).displayName.ToLower(), 1);
            }
            else InteractionPrompt.Clear(this);

            if (canAct && InteractionPrompt.Owner == (object)this && InteractionPrompt.KeyFree && !boat.AutopilotActive && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryCollectTarget();
        }

        public float DistanceToTool(CollectibleWaste w)
        {
            Vector3 a = toolOrigin != null ? toolOrigin.position : boat.transform.position;
            Vector3 d = w.transform.position - a; d.y = 0;
            return d.magnitude;
        }

        CollectibleWaste FindTarget()
        {
            float reach = GM.Reach;
            CollectibleWaste best = null;
            float bd = float.MaxValue;
            foreach (var w in CollectibleWaste.All)
            {
                if (!w.IsAvailable) continue;
                float d = DistanceToTool(w);
                if (d <= reach && d < bd) { bd = d; best = w; }
            }
            return best;
        }

        /// <summary>Intenta recoger el residuo resaltado. Devuelve true si empezó la recogida.</summary>
        public bool TryCollectTarget()
        {
            if (Target == null || arm.Busy) return false;
            if (GM.Inventory.IsFull)
            {
                LastFullWarningTime = Time.time;
                GameEvents.RaiseToast("Bodega llena, vuelve al puerto");
                AudioManager.I.PlaySfx(Sfx.Wrong);
                return false;
            }
            var w = Target;
            Target = null;
            w.BeginCollect();
            InteractionPrompt.Clear(this);
            arm.Collect(w, () => Store(w));
            return true;
        }

        void Store(CollectibleWaste w)
        {
            if (!GM.Inventory.TryAdd(w.Type))
            {
                // No debería ocurrir (se comprueba antes), pero nunca se pierde el residuo.
                GameEvents.RaiseToast("Bodega llena, vuelve al puerto");
            }
            if (!GM.State.collectedIds.Contains(w.Id)) GM.State.collectedIds.Add(w.Id);
            GM.State.collectedByType[(int)w.Type]++;
            CollectedThisSession++;
            AudioManager.I.PlaySfx(Sfx.Collect);
            GameEvents.RaiseWasteCollected(w);
            GameEvents.RaiseStateChanged();
        }
    }
}
