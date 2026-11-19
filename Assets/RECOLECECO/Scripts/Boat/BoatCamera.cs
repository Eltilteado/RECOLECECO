using UnityEngine;
using UnityEngine.InputSystem;

namespace RecolecEco
{
    /// <summary>
    /// Cámara en tercera persona detrás de la chalana. Sigue suavemente, evita atravesar objetos
    /// y permite orbitar manteniendo el botón derecho del mouse (rueda = zoom).
    /// </summary>
    public class BoatCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 10f;
        public float height = 4.2f;
        public float lookHeight = 1.4f;
        public float followSmooth = 0.18f;
        public float minDistance = 6f, maxDistance = 15f;
        public float collisionRadius = 0.35f;

        float _orbitYaw, _orbitPitch;
        float _smoothYaw;
        float _lastOrbitInput = -10f;
        Vector3 _vel;
        Transform _cinePoint, _cineLook;
        float _cineAngle;
        readonly RaycastHit[] _hits = new RaycastHit[16];

        void Start()
        {
            if (target != null)
            {
                _smoothYaw = target.eulerAngles.y;
                transform.position = DesiredPosition(out _);
                transform.LookAt(target.position + Vector3.up * lookHeight);
            }
        }

        public void SetCinematic(Transform point, Transform look)
        {
            _cinePoint = point; _cineLook = look; _cineAngle = 0f;
        }

        public void ClearCinematic() { _cinePoint = null; _cineLook = null; }

        Vector3 DesiredPosition(out Vector3 pivot)
        {
            pivot = target.position + Vector3.up * lookHeight;
            float yaw = _smoothYaw + _orbitYaw;
            float pitch = Mathf.Atan2(height - lookHeight, distance) * Mathf.Rad2Deg + _orbitPitch;
            Quaternion q = Quaternion.Euler(pitch, yaw, 0);
            return pivot - q * Vector3.forward * distance;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.unscaledDeltaTime;

            if (_cinePoint != null)
            {
                _cineAngle += dt * 6f;
                Vector3 look = _cineLook != null ? _cineLook.position : target.position;
                Vector3 off = _cinePoint.position - look;
                Vector3 p = look + Quaternion.Euler(0, _cineAngle, 0) * off;
                transform.position = Vector3.SmoothDamp(transform.position, p, ref _vel, 0.8f, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), dt * 2.5f);
                return;
            }

            var gm = GameManager.I;
            if (gm != null && gm.boat != null && gm.dock != null && gm.dock.cameraPoint != null && (gm.boat.IsDocked || gm.boat.IsDocking))
            {
                Vector3 look = gm.dock.cameraLook != null ? gm.dock.cameraLook.position : target.position;
                transform.position = Vector3.SmoothDamp(transform.position, gm.dock.cameraPoint.position, ref _vel, 0.9f, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), dt * 3f);
                _smoothYaw = target.eulerAngles.y;
                return;
            }
            bool canOrbit = gm == null || !gm.Paused;
            var mouse = Mouse.current;
            if (canOrbit && mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    _orbitYaw += d.x * 0.18f;
                    _orbitPitch = Mathf.Clamp(_orbitPitch - d.y * 0.12f, -12f, 35f);
                    _lastOrbitInput = Time.unscaledTime;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && (gm == null || gm.ModalCount == 0))
                    distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 0.8f, minDistance, maxDistance);
            }
            // Vuelve detrás del barco si no se usa el mouse.
            if (Time.unscaledTime - _lastOrbitInput > 1.5f)
            {
                _orbitYaw = Mathf.LerpAngle(_orbitYaw, 0, dt * 1.5f);
                _orbitPitch = Mathf.Lerp(_orbitPitch, 0, dt * 1.5f);
            }

            _smoothYaw = Mathf.LerpAngle(_smoothYaw, target.eulerAngles.y, dt * 3f);
            Vector3 desired = DesiredPosition(out Vector3 pivot);

            // Evitar atravesar objetos (rocas, muelle, edificios).
            Vector3 dir = desired - pivot;
            float len = dir.magnitude;
            float allowed = len;
            int n = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir / Mathf.Max(0.001f, len), _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider.attachedRigidbody != null && h.collider.attachedRigidbody.transform == target) continue;
                if (h.distance > 0.01f) allowed = Mathf.Min(allowed, h.distance);
            }
            Vector3 finalPos = pivot + dir.normalized * Mathf.Max(1.5f, allowed - 0.2f);
            finalPos.y = Mathf.Max(finalPos.y, 0.9f);

            bool pulledIn = allowed < len - 0.01f;
            transform.position = pulledIn
                ? Vector3.Lerp(transform.position, finalPos, dt * 12f)
                : Vector3.SmoothDamp(transform.position, finalPos, ref _vel, followSmooth, Mathf.Infinity, dt);
            transform.rotation = Quaternion.LookRotation(pivot + target.forward * 2f - transform.position);
        }
    }
}
