using UnityEngine;

namespace RecolecEco
{
    /// <summary>Anillo de espuma alrededor de rocas e islotes.</summary>
    public class FoamPulse : MonoBehaviour
    {
        public float amount = 0.06f;
        Vector3 _s;
        float _seed;
        void Start() { _s = transform.localScale; _seed = Random.value * 10f; }
        void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * 1.2f + _seed) * amount;
            transform.localScale = new Vector3(_s.x * k, _s.y, _s.z * k);
            var p = transform.position;
            transform.position = new Vector3(p.x, Waves.Height(p.x, p.z) * 0.5f + 0.05f, p.z);
        }
    }
}
