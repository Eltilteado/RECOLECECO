using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    public enum EffectType { CollectSparkle, Splash }

    /// <summary>Partículas de efecto reutilizables (brillo al recoger, salpicaduras).</summary>
    public class EffectsManager : MonoBehaviour
    {
        public ParticleSystem sparklePrefab;
        public ParticleSystem splashPrefab;

        static EffectsManager _i;
        readonly Dictionary<EffectType, ParticleSystem> _instances = new Dictionary<EffectType, ParticleSystem>();

        void Awake()
        {
            _i = this;
            if (sparklePrefab) _instances[EffectType.CollectSparkle] = Instantiate(sparklePrefab, transform);
            if (splashPrefab) _instances[EffectType.Splash] = Instantiate(splashPrefab, transform);
        }

        void OnDestroy() { if (_i == this) _i = null; }

        public static void Spawn(EffectType t, Vector3 pos)
        {
            if (_i == null || !_i._instances.TryGetValue(t, out var ps) || ps == null) return;
            var ep = new ParticleSystem.EmitParams { position = pos, applyShapeToPosition = true };
            ps.Emit(ep, t == EffectType.CollectSparkle ? 28 : 14);
        }
    }
}
