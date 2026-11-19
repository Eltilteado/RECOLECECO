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

        void Awake()
        {
            Time.timeScale = 1f;
            Waves.ResetGlobals();
            Waves.PushToShader(0);
        }

        void Start()
        {
            AudioManager.I.PlayMenuMusic();
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
            if (cameraPivot) cameraPivot.Rotate(0, 4f * Time.deltaTime, 0, Space.World);
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("Canvas_Menu", 10);
            var root = canvas.transform;

            var title = UIFactory.Label(root, "RECOLEC(ECO)", 120, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -110), new Vector2(1200, 150));
            var ol = title.gameObject.AddComponent<Outline>(); ol.effectColor = UIColors.Navy; ol.effectDistance = new Vector2(5, -5);
            var sub = UIFactory.Label(root, "Mareas de Esperanza", 52, UIColors.Yellow, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            UIFactory.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -250), new Vector2(1000, 70));
            var pitch = UIFactory.Label(root, "Navega por una bahía peruana, recoge residuos, recíclalos y mejora tu chalana\nmientras ayudas a devolverle la vida al mar.", 26, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true);
            UIFactory.Place(pitch.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(118, -330), new Vector2(1100, 90));

            var panel = UIFactory.Panel(root, "Botones", new Color(0.055f, 0.165f, 0.28f, 0.85f));
            bool hasSave = SaveManager.Exists;
            float h = hasSave ? 470 : 370;
            UIFactory.Place(panel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 110), new Vector2(460, h));
            float y = -30;
            if (hasSave)
            {
                var c = UIFactory.Button(panel.transform, "Continuar", UIColors.Turquoise, UIColors.NavyDeep, Continue, 34);
                UIFactory.Place((RectTransform)c.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(380, 84));
                y -= 100;
            }
            var play = UIFactory.Button(panel.transform, hasSave ? "Jugar (nueva partida)" : "Jugar", UIColors.Green, UIColors.NavyDeep, () =>
            {
                NewGame();
            }, 34);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(380, 84));
            y -= 100;
            var ctr = UIFactory.Button(panel.transform, "Controles", UIColors.Teal, Color.white, () => _controls.SetActive(true), 34);
            UIFactory.Place((RectTransform)ctr.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(380, 84));
            y -= 100;
            var quit = UIFactory.Button(panel.transform, "Salir", UIColors.Coral, Color.white, Quit, 34);
            UIFactory.Place((RectTransform)quit.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(380, 84));

            var credits = UIFactory.Label(root, "Proyecto final académico · Unity 6.3 LTS · ODS 12 · 13 · 14", 20, new Color(1, 1, 1, 0.8f), TextAnchor.LowerRight, FontStyle.Normal, true);
            UIFactory.Place(credits.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 20), new Vector2(900, 30));

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
