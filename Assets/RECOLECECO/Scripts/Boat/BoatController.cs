using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RecolecEco
{
    /// <summary>
    /// Navegación de la chalana (§4.1): física simplificada y estable sobre un plano (Y fija),
    /// aceleración suave, giro con inercia, retroceso al 40 % y rebote suave al chocar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
        public Transform model;

        bool _braking;
        Rigidbody _rb;
        GameConfig _cfg;

        public float CurrentSpeed { get; private set; }
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool IsDocked { get; private set; }
        public bool IsDocking { get; private set; }
        public float LastBumpTime { get; private set; } = -10f;

        // Piloto automático (solo para la prueba automatizada).
        public bool AutopilotActive { get; private set; }
        float _apThrottle, _apSteer;

        public float MaxSpeed => GameManager.I != null ? GameManager.I.MaxSpeed : 6f;
        public float Speed01 => Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / Mathf.Max(0.1f, _cfg != null ? _cfg.maxSpeedUpgraded : 8f));

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.mass = 800f;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 10f;
            _cfg = GameConfig.Load();
        }

        bool InputAllowed => GameManager.I != null && !GameManager.I.ControlsLocked && !IsDocked && !IsDocking;

        public void SetAutopilot(bool active, float throttle = 0, float steer = 0)
        {
            AutopilotActive = active;
            _apThrottle = Mathf.Clamp(throttle, -1, 1);
            _apSteer = Mathf.Clamp(steer, -1, 1);
        }

        void Update()
        {
            float th = 0, st = 0;
            _braking = false;
            if (AutopilotActive)
            {
                if (!IsDocked && !IsDocking && GameManager.I != null && !GameManager.I.Paused) { th = _apThrottle; st = _apSteer; }
            }
            else if (InputAllowed)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    _braking = kb.spaceKey.isPressed;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) th += 1;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) th -= 1;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) st += 1;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) st -= 1;
                }
            }
            Throttle = _braking ? 0f : th;
            Steer = st;
        }

        void FixedUpdate()
        {
            if (IsDocked || IsDocking) return;
            float dt = Time.fixedDeltaTime;
            float max = MaxSpeed;
            float accel = max / Mathf.Max(0.1f, _cfg.accelTime);

            float target = Throttle > 0 ? Throttle * max : Throttle * max * _cfg.reverseFactor;
            float rate;
            if (_braking) rate = accel * 2.4f;
            else if (Mathf.Approximately(Throttle, 0)) rate = accel * 0.45f;              // el agua frena suavemente
            else if (Mathf.Sign(target) != Mathf.Sign(CurrentSpeed) && Mathf.Abs(CurrentSpeed) > 0.05f) rate = accel * 1.6f; // frenar
            else rate = accel;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, target, rate * dt);

            // Giro: más lento a baja velocidad (inercia de barco); invertido en retroceso.
            float speedFactor = Mathf.Lerp(_cfg.minTurnFactor, 1f, Mathf.Clamp01(Mathf.Abs(CurrentSpeed) / _cfg.maxSpeed));
            float dir = CurrentSpeed < -0.1f ? -1f : 1f;
            float yaw = Steer * _cfg.turnSpeedDeg * speedFactor * dir * dt;
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0, yaw, 0));

            Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
            _rb.linearVelocity = fwd * CurrentSpeed;
            _rb.angularVelocity = Vector3.zero;
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.collider.isTrigger) return;
            if (Mathf.Abs(CurrentSpeed) > 0.5f)
            {
                CurrentSpeed = -CurrentSpeed * _cfg.bounceFactor; // rebote suave, sin daño
                LastBumpTime = Time.time;
                AudioManager.I.PlaySfx(Sfx.Bump);
            }
        }

        void OnCollisionStay(Collision c)
        {
            if (c.collider.isTrigger) return;
            // Empujar contra una pared no acumula velocidad.
            CurrentSpeed *= 0.92f;
        }

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            pos.y = 0;
            _rb.position = pos;
            _rb.rotation = rot;
            transform.SetPositionAndRotation(pos, rot);
            _rb.linearVelocity = Vector3.zero;
            CurrentSpeed = 0;
        }

        public void Dock(Transform dockPoint, System.Action onDocked)
        {
            if (IsDocked || IsDocking) return;
            StartCoroutine(DockRoutine(dockPoint, onDocked));
        }

        IEnumerator DockRoutine(Transform p, System.Action onDocked)
        {
            IsDocking = true;
            _rb.isKinematic = true;
            Vector3 a = transform.position;
            Quaternion ra = transform.rotation;
            Vector3 b = new Vector3(p.position.x, 0, p.position.z);
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.4f;
                float e = t * t * (3 - 2 * t);
                CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime * 4f);
                _rb.MovePosition(Vector3.Lerp(a, b, e));
                _rb.MoveRotation(Quaternion.Slerp(ra, p.rotation, e));
                yield return null;
            }
            transform.SetPositionAndRotation(b, p.rotation);
            CurrentSpeed = 0;
            IsDocking = false;
            IsDocked = true;
            AudioManager.I.PlaySfx(Sfx.Dock);
            onDocked?.Invoke();
        }

        public void Undock()
        {
            if (!IsDocked) return;
            IsDocked = false;
            _rb.isKinematic = false;
            CurrentSpeed = 0.8f;
        }
    }
}
