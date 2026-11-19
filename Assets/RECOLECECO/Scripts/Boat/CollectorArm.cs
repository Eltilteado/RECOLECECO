using System;
using System.Collections;
using UnityEngine;

namespace RecolecEco
{
    /// <summary>
    /// Brazo con red montado en la proa de la chalana. Gira hacia el residuo, baja, lo atrapa
    /// y lo deposita en la bodega (§4.2: la herramienta se ve moverse en cada recogida).
    /// </summary>
    public class CollectorArm : MonoBehaviour
    {
        public Transform yawPivot;
        public Transform boomPivot;
        public Transform boom;          // se escala en Z con la mejora
        public Transform netTip;        // donde queda atrapado el residuo
        public Transform net;           // se agranda con la mejora
        public Transform holdPoint;     // bodega

        public float restPitch = -35f;
        public float scoopPitch = 18f;
        public float holdYaw = 180f;

        public bool Busy { get; private set; }

        float _yaw, _pitch;
        Vector3 _boomBase, _netBase;

        void Awake()
        {
            if (boom) _boomBase = boom.localScale;
            if (net) _netBase = net.localScale;
            _pitch = restPitch;
            Apply();
        }

        public void SetUpgraded(bool up)
        {
            if (boom) boom.localScale = new Vector3(_boomBase.x, _boomBase.y, _boomBase.z * (up ? 1.45f : 1f));
            if (net) net.localScale = _netBase * (up ? 1.45f : 1f);
            // El extremo del brazo sigue a la pluma escalada.
            if (boom && netTip)
            {
                var lp = netTip.localPosition;
                netTip.localPosition = new Vector3(lp.x, lp.y, BoomLength(up));
            }
            if (boom && net)
            {
                var lp = net.localPosition;
                net.localPosition = new Vector3(lp.x, lp.y, BoomLength(up));
            }
        }

        float BoomLength(bool up) => 2.2f * (up ? 1.45f : 1f);

        void Update()
        {
            if (Busy) return;
            // Pequeño vaivén en reposo.
            _yaw = Mathf.LerpAngle(_yaw, Mathf.Sin(Time.time * 0.7f) * 6f, Time.deltaTime * 2f);
            _pitch = Mathf.Lerp(_pitch, restPitch + Mathf.Sin(Time.time * 1.1f) * 2f, Time.deltaTime * 2f);
            Apply();
        }

        void Apply()
        {
            if (yawPivot) yawPivot.localRotation = Quaternion.Euler(0, _yaw, 0);
            if (boomPivot) boomPivot.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        public void Collect(CollectibleWaste waste, Action onStored)
        {
            if (Busy) return;
            StartCoroutine(CollectRoutine(waste, onStored));
        }

        IEnumerator Tween(float dur, float yawTo, float pitchTo)
        {
            float y0 = _yaw, p0 = _pitch, t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = t * t * (3 - 2 * t);
                _yaw = Mathf.LerpAngle(y0, yawTo, e);
                _pitch = Mathf.Lerp(p0, pitchTo, e);
                Apply();
                yield return null;
            }
        }

        IEnumerator CollectRoutine(CollectibleWaste waste, Action onStored)
        {
            Busy = true;
            // 1. Girar hacia el residuo y bajar la red.
            Vector3 local = yawPivot.parent.InverseTransformPoint(waste.transform.position);
            float yawTo = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -120f, 120f);
            AudioManager.I.PlaySfx(Sfx.Arm);
            yield return Tween(0.35f, yawTo, scoopPitch);

            // 2. El residuo vuela hasta la red.
            AudioManager.I.PlaySfx(Sfx.Splash);
            yield return waste.FlyTo(netTip, 0.35f, true);

            // 3. Subir y girar hacia la bodega.
            yield return Tween(0.5f, holdYaw * Mathf.Sign(yawTo == 0 ? 1 : yawTo), restPitch - 10f);

            // 4. Soltar en la bodega.
            if (holdPoint != null) yield return waste.FlyTo(holdPoint, 0.25f, false);
            waste.FinishCollect();
            onStored?.Invoke();

            // 5. Volver a la posición de reposo.
            yield return Tween(0.35f, 0f, restPitch);
            Busy = false;
        }
    }
}
