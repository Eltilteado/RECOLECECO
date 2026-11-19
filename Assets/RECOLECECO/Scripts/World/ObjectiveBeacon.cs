using UnityEngine;

namespace RecolecEco
{
    /// <summary>Columna de luz en el mundo que marca el siguiente destino de la misión.</summary>
    public class ObjectiveBeacon : MonoBehaviour
    {
        public Transform visual;
        public float minBoatDistance = 6f;

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || visual == null) return;
            bool show = gm.Quests.HasTarget && !gm.Cinematic;
            if (show && gm.boat != null)
            {
                Vector3 d = gm.Quests.TargetPosition - gm.boat.transform.position; d.y = 0;
                show = d.magnitude > minBoatDistance;
            }
            if (visual.gameObject.activeSelf != show) visual.gameObject.SetActive(show);
            if (!show) return;
            Vector3 p = gm.Quests.TargetPosition;
            transform.position = Vector3.Lerp(transform.position, new Vector3(p.x, 0, p.z),
                (transform.position - p).sqrMagnitude > 400f ? 1f : Time.deltaTime * 8f);
            visual.localScale = new Vector3(1f + Mathf.Sin(Time.time * 4f) * 0.1f, 1f, 1f + Mathf.Sin(Time.time * 4f) * 0.1f);
            visual.Rotate(0, 40f * Time.deltaTime, 0, Space.Self);
        }
    }
}
