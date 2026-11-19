using UnityEngine;

namespace RecolecEco
{
    /// <summary>Hace girar un objeto (aspas, faro).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 30, 0);
        void Update() { transform.Rotate(speed * Time.deltaTime, Space.Self); }
    }
}
