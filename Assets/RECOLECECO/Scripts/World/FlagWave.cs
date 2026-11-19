using UnityEngine;

namespace RecolecEco
{
    /// <summary>Movimiento simple de banderas, banderines y redes colgadas.</summary>
    public class FlagWave : MonoBehaviour
    {
        public Vector3 axis = new Vector3(0, 1, 0);
        public float amplitude = 12f;
        public float frequency = 2.2f;
        Quaternion _base;
        float _seed;

        void Start() { _base = transform.localRotation; _seed = Random.value * 10f; }

        void Update()
        {
            float a = Mathf.Sin(Time.time * frequency + _seed) * amplitude + Mathf.Sin(Time.time * frequency * 2.3f + _seed) * amplitude * 0.3f;
            transform.localRotation = _base * Quaternion.AngleAxis(a, axis);
        }
    }
}
