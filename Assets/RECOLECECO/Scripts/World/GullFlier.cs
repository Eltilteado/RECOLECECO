using UnityEngine;

namespace RecolecEco
{

    /// <summary>Gaviota que vuela en círculos aleteando.</summary>
    public class GullFlier : MonoBehaviour
    {
        public Vector3 center;
        public float radius = 12f;
        public float height = 9f;
        public float speed = 0.35f;
        public Transform wingL, wingR;
        float _angle, _seed;

        void Start()
        {
            if (center == Vector3.zero) center = transform.position;
            _seed = Random.value * 10f;
            _angle = Random.value * Mathf.PI * 2f;
            if (Random.value < 0.5f) speed = -speed;
        }

        void Update()
        {
            _angle += speed * Time.deltaTime;
            float y = height + Mathf.Sin(Time.time * 0.6f + _seed) * 1.5f;
            Vector3 p = center + new Vector3(Mathf.Cos(_angle) * radius, y, Mathf.Sin(_angle) * radius);
            Vector3 tangent = new Vector3(-Mathf.Sin(_angle), 0, Mathf.Cos(_angle)) * Mathf.Sign(speed);
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0, 0, -15f * Mathf.Sign(speed));
            float flap = Mathf.Sin(Time.time * 7f + _seed) * 35f;
            if (Mathf.Sin(Time.time * 0.5f + _seed) > 0.3f) flap = 8f; // planear
            if (wingL) wingL.localRotation = Quaternion.Euler(0, 0, flap);
            if (wingR) wingR.localRotation = Quaternion.Euler(0, 0, -flap);
        }
    }
}
