using UnityEngine;

namespace RecolecEco
{
    /// <summary>Grupo de objetos que aparece/desaparece suavemente (escala).</summary>
    public class FaunaGroup : MonoBehaviour
    {
        [Range(0, 1)] public float threshold = 0.5f;
        bool _visible = true;
        bool _init;
        float _t = 1f;
        Vector3 _baseScale;

        void Awake() { _baseScale = transform.localScale; }

        public void SetVisible(bool v, bool instant)
        {
            if (!_init) { _init = true; _visible = !v; }
            if (_visible == v && !instant) return;
            _visible = v;
            if (instant)
            {
                _t = v ? 1f : 0f;
                transform.localScale = _baseScale * _t;
                gameObject.SetActive(v);
            }
            else if (v) gameObject.SetActive(true);
        }

        void Update()
        {
            float target = _visible ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, Time.deltaTime * 0.8f);
            float e = _t * _t * (3f - 2f * _t);
            transform.localScale = _baseScale * Mathf.Max(0.0001f, e);
            if (_t <= 0f) gameObject.SetActive(false);
        }
    }
}
