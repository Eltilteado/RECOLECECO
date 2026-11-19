using UnityEngine;

namespace RecolecEco
{
    /// <summary>Animación procedural de NPC: reposo, mirar a la chalana, saludar y gesticular al hablar.</summary>
    public class NPCAnimator : MonoBehaviour
    {
        public string speakerName;
        public Transform body;
        public Transform head;
        public Transform armRight;
        public Transform armLeft;
        public GameObject questMarker;
        public float waveDistance = 30f;

        float _seed, _waveTimer, _waveT = -1f;
        Quaternion _baseRot;

        void Start()
        {
            _seed = Random.value * 10f;
            _baseRot = transform.rotation;
            _waveTimer = Random.Range(2f, 5f);
        }

        void Update()
        {
            float t = Time.time + _seed;
            if (body) body.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.015f, 1f);

            var gm = GameManager.I;
            Transform boat = gm != null && gm.boat != null ? gm.boat.transform : null;
            bool talking = DialogueSystem.CurrentSpeaker == speakerName ||
                           (speakerName == DialogueScripts.Mateo && DialogueSystem.CurrentSpeaker != null && DialogueSystem.CurrentSpeaker.Contains("Mateo"));
            float boatDist = boat != null ? Vector3.Distance(boat.position, transform.position) : 999f;

            // Girar hacia la chalana si está cerca.
            if (boat != null && boatDist < 45f)
            {
                Vector3 d = boat.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.1f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 1.5f);
            }
            else transform.rotation = Quaternion.Slerp(transform.rotation, _baseRot, Time.deltaTime);

            if (head) head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 4f + (talking ? Mathf.Sin(t * 9f) * 4f : 0), Mathf.Sin(t * 0.4f) * 15f, 0);

            // Saludar periódicamente cuando la chalana está cerca.
            if (_waveT < 0f && boatDist < waveDistance && !talking)
            {
                _waveTimer -= Time.deltaTime;
                if (_waveTimer <= 0f) { _waveT = 0f; _waveTimer = Random.Range(6f, 10f); }
            }

            float armR = 8f + Mathf.Sin(t * 1.3f) * 3f;
            float armRz = 0f;
            float armL = 8f + Mathf.Sin(t * 1.1f + 1f) * 3f;
            if (_waveT >= 0f)
            {
                _waveT += Time.deltaTime;
                float k = Mathf.Clamp01(Mathf.Min(_waveT * 3f, (2.2f - _waveT) * 3f));
                armR = Mathf.Lerp(armR, 160f, k);
                armRz = Mathf.Sin(_waveT * 12f) * 20f * k;
                if (_waveT > 2.2f) _waveT = -1f;
            }
            else if (talking)
            {
                armR = 50f + Mathf.Sin(t * 5f) * 25f;
                armL = 35f + Mathf.Sin(t * 4f + 2f) * 20f;
            }
            if (armRight) armRight.localRotation = Quaternion.Euler(-armR, 0, armRz);
            if (armLeft) armLeft.localRotation = Quaternion.Euler(-armL, 0, 0);

            if (questMarker && gm != null && gm.Quests != null)
            {
                bool show = !gm.Quests.AllDone && QuestManager.Givers[gm.Quests.Index].Contains(speakerName.Replace("Don ", "").Replace("Doña ", ""));
                if (questMarker.activeSelf != show) questMarker.SetActive(show);
                if (show) questMarker.transform.localPosition = new Vector3(0, 2.6f + Mathf.Sin(Time.time * 3f) * 0.12f, 0);
                questMarker.transform.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            }
        }
    }
}
