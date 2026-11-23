using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RecolecEco
{
    /// <summary>Menú principal: Jugar / Controles / Salir (+ Continuar si hay partida guardada).</summary>
    public class MainMenuController : MonoBehaviour
    {
        public Transform cameraPivot;
        GameObject _controls;
        Image _fade;
        Quaternion _restOrbit;

        void Awake()
        {
            Time.timeScale = 1f;
            Waves.ResetGlobals();
            Waves.PushToShader(0);
        }

        void Start()
        {
            AudioManager.I.PlayMenuMusic();
            FrameChalana();
            BuildUI();

            // Modo de prueba automatizada (solo con argumentos de línea de comandos).
            string mode = AutoTestRunner.ModeFromArgs();
            if (!string.IsNullOrEmpty(mode))
            {
                GameManager.AutoTestMode = mode;
                if (mode == "load") Continue();
                else if (AutoTestRunner.MenuVisited) { /* volvió al menú desde la prueba: terminar */ AutoTestRunner.OnMenuReached(); }
                else { AutoTestRunner.MenuVisited = true; NewGame(); }
            }
        }

        void Update()
        {
            Waves.PushToShader(Time.timeSinceLevelLoad);
            if (cameraPivot)
                cameraPivot.localRotation = _restOrbit * Quaternion.Euler(0,
                    Mathf.Sin(Time.timeSinceLevelLoad * 0.22f) * 13f, 0);
        }

        void FrameChalana()
        {
            if (!cameraPivot) return;
            _restOrbit = cameraPivot.localRotation;
            var cam = cameraPivot.GetComponentInChildren<Camera>();
            if (!cam) return;
            cam.transform.localPosition = new Vector3(-10.2f, 4.6f, -9.7f);
            Vector3 focus = cameraPivot.position + Vector3.up * 1.25f;
            Vector3 right = Vector3.Cross(Vector3.up, focus - cam.transform.position).normalized;
            cam.transform.LookAt(focus - right * 5f);
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("Canvas_Menu", 10);
            var root = canvas.transform;

            var wash = UIFactory.Panel(root, "Degradado costero",
                new Color(0.035f, 0.09f, 0.12f, 0.98f), UIFactory.MenuGradient);
            UIFactory.Stretch(wash.rectTransform);
            wash.raycastTarget = false;

            var left = UIFactory.Rect(root, "Presentación");
            UIFactory.Place(left, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(100, 0), new Vector2(720, 860));

            var top = new Vector2(0, 1);
            var location = UIFactory.Label(left, "PUERTO DE PISCO  /  PERÚ", 22,
                UIColors.Turquoise, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(location.rectTransform, top, top, new Vector2(0, -40), new Vector2(600, 35));
            UIFactory.Rule(left, "Línea de cubierta", UIColors.Yellow, top, top,
                new Vector2(0, -95), new Vector2(78, 3));

            var title = UIFactory.Label(left, "Mareas de\nEsperanza", 98, UIColors.Text,
                TextAnchor.UpperLeft);
            title.font = UIFactory.DisplayFont;
            title.lineSpacing = 0.9f;
            UIFactory.Place(title.rectTransform, top, top, new Vector2(-5, -116), new Vector2(730, 240));

            var series = UIFactory.Label(left, "RECOLEC(ECO)", 25, UIColors.Yellow,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(series.rectTransform, top, top, new Vector2(0, -350), new Vector2(600, 38));

            var pitch = UIFactory.Label(left,
                "Recoge residuos del mar, llévalos a reciclar y mejora tu chalana.",
                27, UIColors.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(pitch.rectTransform, top, top, new Vector2(0, -413), new Vector2(505, 92));

            bool hasSave = SaveManager.Exists;
            var primary = UIFactory.Button(left, hasSave ? "Continuar partida" : "Zarpar",
                UIColors.Yellow, UIColors.NavyDeep, hasSave
                    ? (UnityEngine.Events.UnityAction)Continue : NewGame, 31);
            UIFactory.Place((RectTransform)primary.transform, top, top,
                new Vector2(0, -548), new Vector2(402, 76));

            float y = -640;
            if (hasSave)
            {
                var play = UIFactory.Button(left, "Nueva partida", UIColors.PanelLight, UIColors.Text, NewGame, 27);
                UIFactory.Place((RectTransform)play.transform, top, top, new Vector2(0, y), new Vector2(402, 64));
                y -= 80;
            }
            var controls = UIFactory.Button(left, "Controles", UIColors.PanelLight, UIColors.Text,
                () => _controls.SetActive(true), 24);
            UIFactory.Place((RectTransform)controls.transform, top, top, new Vector2(0, y), new Vector2(194, 58));
            var exit = UIFactory.Button(left, "Salir", UIColors.PanelLight, UIColors.Text, Quit, 24);
            UIFactory.Place((RectTransform)exit.transform, top, top, new Vector2(208, y), new Vector2(194, 58));

            var shade = UIFactory.Panel(root, "Controles", new Color(0, 0, 0, 0.55f));
            shade.sprite = null; shade.type = Image.Type.Simple;
            UIFactory.Stretch(shade.rectTransform);
            _controls = shade.gameObject;
            UIManager.BuildControlsContent(shade.transform, () => _controls.SetActive(false));
            _controls.SetActive(false);

            _fade = UIFactory.Panel(root, "Fade", new Color(0, 0, 0, 0));
            _fade.sprite = null; _fade.type = Image.Type.Simple;
            UIFactory.Stretch(_fade.rectTransform);
            _fade.raycastTarget = false;
        }

        void Continue()
        {
            GameManager.ContinueRequested = SaveManager.Exists;
            StartCoroutine(LoadGame());
        }

        void NewGame()
        {
            SaveManager.Delete();
            GameManager.ContinueRequested = false;
            StartCoroutine(LoadGame());
        }

        IEnumerator LoadGame()
        {
            _fade.raycastTarget = true;
            float a = 0;
            while (a < 1f)
            {
                a += Time.unscaledDeltaTime * 2.5f;
                _fade.color = new Color(0, 0, 0, a);
                yield return null;
            }
            SceneManager.LoadScene(GameManager.GameScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
