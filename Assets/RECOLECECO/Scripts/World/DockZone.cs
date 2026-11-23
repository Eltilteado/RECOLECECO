using UnityEngine;
using UnityEngine.InputSystem;

namespace RecolecEco
{
    /// <summary>Zona de atraque del muelle: con E la chalana se fija y se abre el panel del puerto.</summary>
    public class DockZone : MonoBehaviour
    {
        public Transform dockPoint;
        public float radius = 11f;
        const float ManualDockSpeedLimit = 2f;
        public GameObject dockHighlight;
        [Tooltip("Vista de cámara mientras la chalana está atracada")]
        public Transform cameraPoint;
        public Transform cameraLook;

        GameManager GM => GameManager.I;
        public bool BoatInRange { get; private set; }

        void Update()
        {
            if (GM == null || GM.boat == null) return;
            var boat = GM.boat;
            Vector3 d = boat.transform.position - transform.position; d.y = 0;
            BoatInRange = d.magnitude <= radius && !boat.IsDocked && !boat.IsDocking;
            if (dockHighlight) dockHighlight.SetActive(BoatInRange || (GM.Quests != null && GM.Quests.HasTarget && (GM.Quests.TargetPosition - transform.position).sqrMagnitude < 1f));

            if (BoatInRange && !GM.ControlsLocked)
            {
                bool slowEnough = Mathf.Abs(boat.CurrentSpeed) <= ManualDockSpeedLimit;
                InteractionPrompt.Set(this, slowEnough ? "E — Atracar en el muelle" : "Espacio — Frena para atracar", 2);
                var keyboard = Keyboard.current;
                if (slowEnough && InteractionPrompt.Owner == (object)this && InteractionPrompt.KeyFree
                    && !boat.AutopilotActive && keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    DockNow();
                }
            }
            else InteractionPrompt.Clear(this);
        }

        public bool DockNow()
        {
            var boat = GM.boat;
            if (boat.IsDocked || boat.IsDocking) return false;
            InteractionPrompt.Clear(this);
            boat.Dock(dockPoint, () =>
            {
                GM.Autosave("atraque");
                GameEvents.RaiseDocked();
            });
            return true;
        }

        public void Undock()
        {
            GM.boat.Undock();
            GameEvents.RaiseUndocked();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
