using UnityEngine;

namespace RecolecEco
{
    /// <summary>Pez que salta fuera del agua cada cierto tiempo dentro de un radio.</summary>
    public class FishJumper : MonoBehaviour
    {
        public float radius = 15f;
        public Transform fish;
        public float arcHeight = 1.6f;
        public float arcLength = 3f;
        public float jumpTime = 0.9f;
        Vector3 _center, _a, _b;
        float _t = 1f, _wait;

        void Start()
        {
            _center = transform.position;
            _wait = Random.Range(0.5f, 4f);
            if (fish) fish.gameObject.SetActive(false);
        }

        void Update()
        {
            if (fish == null) return;
            if (_t >= 1f)
            {
                _wait -= Time.deltaTime;
                if (_wait > 0) return;
                Vector2 r = Random.insideUnitCircle * radius;
                _a = _center + new Vector3(r.x, 0, r.y);
                Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(arcLength * 0.7f, arcLength * 1.2f);
                _b = _a + new Vector3(dir.x, 0, dir.y);
                _t = 0f;
                fish.gameObject.SetActive(true);
                EffectsManager.Spawn(EffectType.Splash, _a);
            }
            _t += Time.deltaTime / jumpTime;
            float e = Mathf.Clamp01(_t);
            Vector3 p = Vector3.Lerp(_a, _b, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * arcHeight - 0.2f);
            Vector3 vel = (_b - _a) + Vector3.up * Mathf.Cos(e * Mathf.PI) * arcHeight * 3f;
            fish.position = p;
            if (vel.sqrMagnitude > 0.001f) fish.rotation = Quaternion.LookRotation(vel);
            if (_t >= 1f)
            {
                fish.gameObject.SetActive(false);
                EffectsManager.Spawn(EffectType.Splash, _b);
                _wait = Random.Range(2f, 6f);
            }
        }
    }
}
