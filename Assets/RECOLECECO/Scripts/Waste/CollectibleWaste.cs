using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    /// <summary>Residuo flotante interactivo: flota y se mece con las olas, se resalta y se recoge.</summary>
    public class CollectibleWaste : MonoBehaviour
    {
        public static readonly List<CollectibleWaste> All = new List<CollectibleWaste>();

        [SerializeField] string id;
        [SerializeField] WasteType type;
        [SerializeField] SectorId sector;
        public Transform visual;
        public GameObject highlightRing;
        public float floatOffset = 0.05f;

        public string Id => id;
        public WasteType Type => type;
        public SectorId Sector => sector;
        public bool IsCollecting { get; private set; }
        public bool IsAvailable => gameObject.activeInHierarchy && !IsCollecting;

        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        bool _highlighted;
        float _seed;
        Vector3 _visualScale;
        Color[] _baseColors;

        public void Setup(string newId, WasteType t, SectorId s)
        {
            id = newId; type = t; sector = s;
        }

        void Awake()
        {
            All.Add(this);
            _seed = Random.value * 100f;
            if (visual == null) visual = transform;
            _visualScale = visual.localScale;
            _renderers = visual.GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var m = _renderers[i].sharedMaterial;
                _baseColors[i] = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            }
            if (highlightRing) highlightRing.SetActive(false);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (IsCollecting) return;
            Vector3 p = transform.position;
            float h = Waves.Height(p.x, p.z);
            transform.position = new Vector3(p.x, h + floatOffset, p.z);
            float tt = Time.time + _seed;
            visual.localRotation = Quaternion.Euler(Mathf.Sin(tt * 1.3f) * 8f, _seed * 3.6f + Mathf.Sin(tt * 0.3f) * 10f, Mathf.Cos(tt * 1.1f) * 8f);

            if (_highlighted)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _renderers[i].GetPropertyBlock(_mpb);
                    _mpb.SetColor("_BaseColor", Color.Lerp(_baseColors[i], Color.white, 0.25f + 0.3f * pulse));
                    _mpb.SetColor("_EmissionColor", new Color(0.35f, 0.9f, 0.8f) * (0.4f + 0.6f * pulse));
                    _renderers[i].SetPropertyBlock(_mpb);
                }
                if (highlightRing) highlightRing.transform.localScale = Vector3.one * (1f + 0.12f * pulse);
            }
        }

        public void SetHighlighted(bool on)
        {
            if (_highlighted == on) return;
            _highlighted = on;
            if (highlightRing) highlightRing.SetActive(on);
            if (!on) foreach (var r in _renderers) r.SetPropertyBlock(null);
        }

        public void BeginCollect()
        {
            IsCollecting = true;
            SetHighlighted(false);
        }

        /// <summary>Vuela hasta un punto (red o bodega), opcionalmente quedándose pegado a él.</summary>
        public IEnumerator FlyTo(Transform target, float duration, bool attach)
        {
            Vector3 start = transform.position;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duration);
                float e = t * t * (3 - 2 * t);
                Vector3 end = target.position;
                Vector3 pos = Vector3.Lerp(start, end, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.8f;
                transform.position = pos;
                visual.Rotate(0, 400f * Time.deltaTime, 200f * Time.deltaTime, Space.Self);
                if (!attach) visual.localScale = _visualScale * Mathf.Lerp(1f, 0.25f, e);
                yield return null;
            }
            if (attach)
            {
                transform.SetParent(target, true);
                transform.localPosition = Vector3.zero;
            }
        }

        public void FinishCollect()
        {
            EffectsManager.Spawn(EffectType.CollectSparkle, transform.position);
            transform.SetParent(null, true);
            visual.localScale = _visualScale;
            gameObject.SetActive(false);
        }
    }
}
