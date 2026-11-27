using UnityEngine;

namespace RecolecEco
{
    /// <summary>
    /// Balanceo visual con el oleaje, inclinación al acelerar/girar, estela y espuma,
    /// sonido de motor y cambios visibles de las mejoras.
    /// </summary>
    public class BoatVisuals : MonoBehaviour
    {
        public BoatController controller;
        public Transform model;
        public ParticleSystem wakeFoam;
        public ParticleSystem bowSpray;
        public TrailRenderer[] wakeTrails;
        public Transform propeller;

        [Header("Mejoras visibles")]
        public GameObject motorSmall;
        public GameObject motorBig;
        public GameObject holdExtra;
        public CollectorArm arm;

        float _pitch, _roll, _lastSpeed, _heave;
        Transform _upgradedPropeller;
        Transform _activePropeller;

        void OnEnable() { GameEvents.StateChanged += RefreshUpgrades; }
        void OnDisable() { GameEvents.StateChanged -= RefreshUpgrades; }
        void Awake()
        {
            _upgradedPropeller = motorBig != null ? motorBig.transform.Find("Helice") : null;
            _activePropeller = propeller;
        }

        void Start() { RefreshUpgrades(); }

        public void RefreshUpgrades()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            bool motor = gm.State.HasUpgrade(UpgradeType.Motor);
            if (motorSmall) motorSmall.SetActive(!motor);
            if (motorBig) motorBig.SetActive(motor);
            _activePropeller = motor && _upgradedPropeller != null ? _upgradedPropeller : propeller;
            if (holdExtra) holdExtra.SetActive(gm.State.HasUpgrade(UpgradeType.Bodega));
            if (arm) arm.SetUpgraded(gm.State.HasUpgrade(UpgradeType.Herramienta));
        }

        void Update()
        {
            if (controller == null || model == null) return;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Transform t = controller.transform;
            Vector3 p = t.position;
            Vector3 f = t.forward * 2.4f;
            Vector3 r = t.right * 1.0f;

            float hC = Waves.Height(p);
            float hF = Waves.Height(p + f), hB = Waves.Height(p - f);
            float hR = Waves.Height(p + r), hL = Waves.Height(p - r);

            float accel = (controller.CurrentSpeed - _lastSpeed) / dt;
            _lastSpeed = controller.CurrentSpeed;

            float targetPitch = Mathf.Atan2(hB - hF, 4.8f) * Mathf.Rad2Deg * 0.8f - Mathf.Clamp(accel, -3, 3) * 0.9f
                                - Mathf.Clamp01(controller.CurrentSpeed / 8f) * 2.5f;
            float targetRoll = Mathf.Atan2(hL - hR, 2.0f) * Mathf.Rad2Deg * 0.6f
                               - controller.Steer * Mathf.Clamp01(Mathf.Abs(controller.CurrentSpeed) / 6f) * 5f;

            _pitch = Mathf.Lerp(_pitch, targetPitch, dt * 3f);
            _roll = Mathf.Lerp(_roll, targetRoll, dt * 2.5f);
            _heave = Mathf.Lerp(_heave, hC, dt * 6f);

            model.localPosition = new Vector3(0, _heave, 0);
            model.localRotation = Quaternion.Euler(_pitch, 0, _roll);

            bool navigating = !controller.IsDocking && !controller.IsDocked;
            float s01 = controller.Speed01;
            if (wakeFoam)
            {
                var em = wakeFoam.emission;
                em.rateOverTime = navigating ? Mathf.Lerp(0, 45, s01) : 0f;
            }
            if (bowSpray)
            {
                var em = bowSpray.emission;
                float forwardSpray = Mathf.Clamp01((controller.CurrentSpeed - 2.5f) / 5f);
                em.rateOverTime = navigating ? 30f * forwardSpray : 0f;
            }
            if (wakeTrails != null)
                foreach (var tr in wakeTrails) if (tr) tr.emitting = navigating && Mathf.Abs(controller.CurrentSpeed) > 0.6f;
            if (_activePropeller) _activePropeller.Rotate(0, 0, controller.CurrentSpeed * 360f * dt, Space.Self);

            AudioManager.I.SetMotor(s01, controller.Throttle != 0);
        }
    }
}
