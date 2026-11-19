using UnityEngine;

namespace RecolecEco
{
    /// <summary>Balanceo/respiración genérica (lobos marinos, aves posadas).</summary>
    public class IdleBob : MonoBehaviour
    {
        public float amount = 0.04f;
        public float speed = 1.5f;
        public float turn = 0f;
        Vector3 _scale;
        Quaternion _rot;
        float _seed;

        void Start() { _scale = transform.localScale; _rot = transform.localRotation; _seed = Random.value * 10f; }

        void Update()
        {
            float s = Mathf.Sin(Time.time * speed + _seed);
            transform.localScale = new Vector3(_scale.x, _scale.y * (1f + s * amount), _scale.z);
            if (turn > 0) transform.localRotation = _rot * Quaternion.Euler(0, Mathf.Sin(Time.time * speed * 0.3f + _seed) * turn, 0);
        }
    }
}
