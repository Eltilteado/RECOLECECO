using UnityEngine;

namespace RecolecEco
{
    /// <summary>Hace flotar y mecerse un objeto decorativo (boyas, barcos de fondo).</summary>
    public class Floater : MonoBehaviour
    {
        public float offset = 0f;
        public float tilt = 6f;
        public float sampleSize = 1.5f;
        float _seed;
        float _baseYaw;

        void Start()
        {
            _seed = Random.value * 10f;
            _baseYaw = transform.eulerAngles.y;
        }

        void Update()
        {
            Vector3 p = transform.position;
            float h = Waves.Height(p.x, p.z);
            float hx = Waves.Height(p.x + sampleSize, p.z) - Waves.Height(p.x - sampleSize, p.z);
            float hz = Waves.Height(p.x, p.z + sampleSize) - Waves.Height(p.x, p.z - sampleSize);
            transform.position = new Vector3(p.x, h + offset, p.z);
            float k = tilt / 0.3f;
            transform.rotation = Quaternion.Euler(Mathf.Clamp(-hz * k, -tilt, tilt), _baseYaw + Mathf.Sin(Time.time * 0.2f + _seed) * 4f, Mathf.Clamp(hx * k, -tilt, tilt));
        }
    }
}
