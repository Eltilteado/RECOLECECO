using UnityEngine;

namespace RecolecEco
{
    /// <summary>Orienta un texto/objeto hacia la cámara (nombres de NPC, carteles).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
