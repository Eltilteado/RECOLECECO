using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RecolecEco
{
    /// <summary>
    /// Toda la interfaz de la escena de juego (§7). Solo lee el estado y llama a los sistemas;
    /// no contiene reglas de juego.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        GameManager GM => GameManager.I;
        Canvas _canvas;
        FieldJournal _journal;
        Transform _root;

        // HUD
        Text _missionHeader, _missionTitle, _missionProgress, _credits, _hold, _clean, _speed, _prompt;
        Image _missionBar, _holdBar, _cleanBar;
        GameObject _promptPanel, _hudRoot;
        RectTransform _marker; Image _markerArrow; Text _markerText;
        RectTransform _toastRoot;
        readonly Queue<string> _toastQueue = new Queue<string>();
        float _toastCooldown;

        // Paneles
        GameObject _inventory, _dialogue, _port, _recycling, _workshop, _pause, _controls, _victory, _confirm;
        Text _invText; Image _invBar; Text[] _invCounts = new Text[5];
        Text _dlgSpeaker, _dlgText, _dlgInitials; Image _dlgPortrait;
        string _dlgFull; float _dlgChars; bool _dlgTyping;
        Text _portInfo; Button _portRosa, _portMateo;
        Image _fade;

        // Reciclaje
        Text _recItemName, _recRemaining, _recFeedback, _recSession; Image _recIcon; Button[] _binButtons = new Button[5];
        GameObject _recEmpty;
        float _recLock;

        // Taller
        readonly Text[] _wsStatus = new Text[3];
        readonly Button[] _wsBuy = new Button[3];
        Text _wsCredits;

        // Confirmación
        Text _confirmText; System.Action _confirmYes;

        // Victoria
        Text _victoryStats;

        bool _metRosa, _metMateo, _controlsModal;
        public bool ControlsOpen => _controls != null && _controls.activeSelf;
        public bool AnyPanelOpen => _port.activeSelf || _recycling.activeSelf || _workshop.activeSelf || _pause.activeSelf || _controls.activeSelf || _victory.activeSelf || _confirm.activeSelf;
        public bool VictoryOpen => _victory != null && _victory.activeSelf;
        public bool PortOpen => _port != null && _port.activeSelf;

        void Awake()
        {
            I = this;
            _canvas = UIFactory.CreateCanvas("Canvas_Juego", 10);
            _canvas.transform.SetParent(transform, false);
            _root = _canvas.transform;
            BuildHUD();
            _journal = gameObject.AddComponent<FieldJournal>();
            BuildInventory();
            BuildDialogue();
            BuildPort();
            BuildRecycling();
            BuildWorkshop();
            BuildControls();
            BuildPause();
            BuildVictory();
            BuildConfirm();
            _toastRoot.SetAsLastSibling();
            _fade = UIFactory.Panel(_root, "Fade", Color.black, UIFactory.Circle);
            _fade.sprite = null; _fade.type = Image.Type.Simple;
            UIFactory.Stretch(_fade.rectTransform);
            _fade.raycastTarget = false;
            StartCoroutine(FadeIn());
        }

        void OnEnable()
        {
            GameEvents.Toast += OnToast;
            GameEvents.Docked += OnDocked;
            GameEvents.StateChanged += RefreshAll;
        }

        void OnDisable()
        {
            GameEvents.Toast -= OnToast;
            GameEvents.Docked -= OnDocked;
            GameEvents.StateChanged -= RefreshAll;
            if (GM != null && GM.Dialogue != null) { GM.Dialogue.LineChanged -= OnDialogueLine; GM.Dialogue.Closed -= OnDialogueClosed; }
        }

        void Start()
        {
            GM.Dialogue.LineChanged += OnDialogueLine;
            GM.Dialogue.Closed += OnDialogueClosed;
            RefreshAll();
        }

        IEnumerator FadeIn()
        {
            float a = 1f;
            while (a > 0f)
            {
                a -= Time.unscaledDeltaTime * 1.2f;
                _fade.color = new Color(0, 0, 0, Mathf.Max(0, a));
                yield return null;
            }
            _fade.gameObject.SetActive(false);
        }

        // ======================= HUD =======================
        void BuildHUD()
        {
            _hudRoot = UIFactory.Rect(_root, "HUD").gameObject;
            UIFactory.Stretch((RectTransform)_hudRoot.transform);
            var hud = _hudRoot.transform;

            var topLeft = new Vector2(0, 1);
            var topRight = new Vector2(1, 1);

            // Misión activa (arriba izquierda)
            var card = UIFactory.Panel(hud, "Mision", UIColors.Panel);
            UIFactory.Place(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -32), new Vector2(502, 151));
            UIFactory.Rule(card.transform, "Acento de misión", UIColors.Turquoise, topLeft,
                topLeft, new Vector2(22, -18), new Vector2(34, 3));
            _missionHeader = UIFactory.Label(card.transform, "", 20, UIColors.Turquoise, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_missionHeader.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -10), new Vector2(410, 26));
            _missionTitle = UIFactory.Label(card.transform, "", 32, UIColors.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_missionTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -40), new Vector2(458, 40));
            _missionProgress = UIFactory.Label(card.transform, "", 22, UIColors.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(_missionProgress.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -82), new Vector2(458, 36));
            var mb = UIFactory.Bar(card.transform, UIColors.NavyDeep, UIColors.Green, out _missionBar);
            UIFactory.Place(mb.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -127), new Vector2(458, 10));

            // Estado (arriba derecha)
            var stats = UIFactory.Panel(hud, "Estado", UIColors.Panel);
            UIFactory.Place(stats.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -32), new Vector2(386, 200));
            UIFactory.Rule(stats.transform, "Acento de bodega", UIColors.Yellow, topRight,
                topRight, new Vector2(-20, -20), new Vector2(34, 3));
            var coin = UIFactory.Icon(stats.transform, IconFactory.Coin, Color.white, 44);
            UIFactory.Place(coin.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -14), new Vector2(44, 44));
            _credits = UIFactory.Label(stats.transform, "", 30, UIColors.Yellow, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_credits.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(74, -14), new Vector2(264, 44));

            _hold = UIFactory.Label(stats.transform, "", 22, UIColors.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_hold.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -68), new Vector2(360, 28));
            var hb = UIFactory.Bar(stats.transform, UIColors.NavyDeep, UIColors.Turquoise, out _holdBar);
            UIFactory.Place(hb.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -100), new Vector2(346, 10));

            _clean = UIFactory.Label(stats.transform, "", 22, UIColors.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_clean.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -122), new Vector2(360, 28));
            var cb = UIFactory.Bar(stats.transform, UIColors.NavyDeep, UIColors.Green, out _cleanBar);
            UIFactory.Place(cb.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -154), new Vector2(346, 10));
            for (int i = 1; i <= 3; i++)
            {
                var tick = UIFactory.Panel(cb.transform, "Hito", new Color(1, 1, 1, 0.5f), UIFactory.RoundedSmall);
                tick.type = Image.Type.Simple; tick.sprite = null;
                UIFactory.Place(tick.rectTransform, new Vector2(i * 0.25f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2, 10));
            }
            _speed = UIFactory.Label(stats.transform, "", 18, UIColors.TextDim, TextAnchor.MiddleLeft);
            UIFactory.Place(_speed.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -176), new Vector2(360, 22));

            // Ayuda de controles (abajo izquierda)
            var help = UIFactory.Panel(hud, "Atajos de navegación", UIColors.Panel);
            help.raycastTarget = false;
            UIFactory.Place(help.rectTransform, Vector2.zero, Vector2.zero, new Vector2(32, 24), new Vector2(405, 44));
            var keys = UIFactory.Label(help.transform, "<b>E</b> Interactuar     <b>I</b> Bodega     <b>M</b> Bitácora",
                21, UIColors.Text, TextAnchor.MiddleCenter);
            UIFactory.Stretch(keys.rectTransform);

            // Indicador de interacción (abajo centro)
            var pp = UIFactory.Panel(hud, "Prompt", new Color(0.04f, 0.12f, 0.2f, 0.88f));
            UIFactory.Place(pp.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(560, 70));
            _prompt = UIFactory.Label(pp.transform, "", 30, UIColors.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_prompt.rectTransform, 10, 0, 10, 0);
            var outline = pp.gameObject.AddComponent<Outline>();
            outline.effectColor = UIColors.Turquoise; outline.effectDistance = new Vector2(2, 2);
            _promptPanel = pp.gameObject;
            _promptPanel.SetActive(false);

            // Marcador de destino
            _marker = UIFactory.Rect(hud, "Marcador");
            _marker.sizeDelta = new Vector2(80, 80);
            _markerArrow = UIFactory.Icon(_marker, UIFactory.Triangle, UIColors.Yellow, 44);
            var sh = _markerArrow.gameObject.AddComponent<Outline>(); sh.effectColor = new Color(0, 0, 0, 0.6f);
            var markerPanel = UIFactory.Panel(_marker, "Distancia al objetivo", UIColors.Panel, UIFactory.RoundedSmall);
            markerPanel.raycastTarget = false;
            UIFactory.Place(markerPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0, -28), new Vector2(232, 58));
            _markerText = UIFactory.Label(markerPanel.transform, "", 18, UIColors.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_markerText.rectTransform, 5, 3, 5, 3);

            // Avisos
            _toastRoot = UIFactory.Rect(_root, "Avisos");
            UIFactory.Place(_toastRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(700, 300));
        }

        void UpdateHUD()
        {
            if (GM == null) return;
            var q = GM.Quests;
            bool show = !GM.InVictory && !VictoryOpen && !_recycling.activeSelf && !_workshop.activeSelf && !_pause.activeSelf;
            _hudRoot.SetActive(show);
            if (!show) return;

            if (q.AllDone)
            {
                _missionHeader.text = "CAMPAÑA COMPLETADA";
                _missionTitle.text = "Guardianes de la bahía";
                _missionProgress.text = q.ProgressText(5);
                UIFactory.SetBar(_missionBar, 1);
            }
            else
            {
                _missionHeader.text = $"MISIÓN {q.Index + 1}/5 · {QuestManager.Givers[q.Index]}";
                _missionTitle.text = QuestManager.Titles[q.Index];
                _missionProgress.text = q.ProgressText(q.Index);
                UIFactory.SetBar(_missionBar, q.ProgressFraction(q.Index));
            }

            _credits.text = $"{GM.State.credits} EcoCréditos";
            int used = GM.Inventory.Used, cap = GM.Inventory.Capacity;
            _hold.text = $"Bodega: {used}/{cap}" + (used >= cap ? "  ¡LLENA!" : "");
            _hold.color = used >= cap ? UIColors.Alert : UIColors.Text;
            UIFactory.SetBar(_holdBar, used / (float)Mathf.Max(1, cap));
            _clean.text = $"Limpieza de la bahía: {GM.Environment.CleanPercent} %";
            UIFactory.SetBar(_cleanBar, GM.Environment.Cleanliness01);
            _speed.text = $"{Mathf.Abs(GM.boat.CurrentSpeed):0.0} m/s · máx. {GM.MaxSpeed:0} m/s · red {GM.Reach:0} m";

            string p = InteractionPrompt.Text;
            bool showPrompt = !string.IsNullOrEmpty(p) && !GM.ControlsLocked;
            if (_promptPanel.activeSelf != showPrompt) _promptPanel.SetActive(showPrompt);
            if (showPrompt) _prompt.text = p;

            UpdateMarker();
        }

        void UpdateMarker()
        {
            var q = GM.Quests;
            var cam = Camera.main;
            bool show = q.HasTarget && cam != null && !GM.Cinematic && !GM.boat.IsDocked;
            if (show)
            {
                Vector3 d = q.TargetPosition - GM.boat.transform.position; d.y = 0;
                show = d.magnitude > 6f;
            }
            _marker.gameObject.SetActive(show);
            if (!show) return;

            Vector3 world = q.TargetPosition + Vector3.up * 3f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            var canvasRt = (RectTransform)_root;
            float scale = canvasRt.rect.width / Screen.width;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            bool behind = sp.z < 0;
            Vector2 p = new Vector2(sp.x, sp.y);
            if (behind) p = screen - p;
            Vector2 center = screen * 0.5f;
            float margin = 126f / Mathf.Max(0.01f, scale);
            bool onScreen = !behind && p.x > margin && p.x < screen.x - margin && p.y > margin && p.y < screen.y - margin;
            if (!onScreen)
            {
                Vector2 dir = (p - center);
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
                dir.Normalize();
                float kx = (center.x - margin) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
                float ky = (center.y - margin) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
                p = center + dir * Mathf.Min(kx, ky);
                _markerArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            }
            else _markerArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 180f);

            _marker.anchorMin = _marker.anchorMax = Vector2.zero;
            _marker.anchoredPosition = p * scale + (onScreen ? new Vector2(0, Mathf.Sin(Time.unscaledTime * 4f) * 6f) : Vector2.zero);
            float dist = Vector3.Distance(GM.boat.transform.position, q.TargetPosition);
            _markerText.text = $"{q.TargetLabel}\n{dist:0} m";
        }

        void OnToast(string msg) { _toastQueue.Enqueue(msg); }

        void UpdateToasts()
        {
            _toastCooldown -= Time.unscaledDeltaTime;
            if (_toastQueue.Count == 0 || _toastCooldown > 0f) return;
            _toastCooldown = 0.6f;
            StartCoroutine(ToastRoutine(_toastQueue.Dequeue()));
        }

        IEnumerator ToastRoutine(string msg)
        {
            var panel = UIFactory.Panel(_toastRoot, "Aviso", UIColors.Teal);
            var rt = panel.rectTransform;
            UIFactory.Place(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(660, 58));
            var t = UIFactory.Label(panel.transform, msg, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform, 14, 0, 14, 0);
            var cg = panel.gameObject.AddComponent<CanvasGroup>();
            // Desplaza los avisos anteriores hacia abajo
            foreach (RectTransform child in _toastRoot)
                if (child != rt) child.anchoredPosition += new Vector2(0, -66);
            float time = 0;
            while (time < 3.4f)
            {
                time += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(time * 4f) * Mathf.Clamp01((3.4f - time) * 2.5f);
                yield return null;
            }
            Destroy(panel.gameObject);
        }

        // ======================= Inventario =======================
        void BuildInventory()
        {
            var p = UIFactory.Panel(_root, "Inventario", UIColors.Panel);
            UIFactory.Place(p.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, -20), new Vector2(460, 520));
            _inventory = p.gameObject;
            var title = UIFactory.Label(p.transform, "Bodega de la chalana", 32, UIColors.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -18), new Vector2(420, 40));
            var cfg = GameConfig.Load();
            for (int i = 0; i < 5; i++)
            {
                var row = UIFactory.Panel(p.transform, "Fila", UIColors.PanelLight);
                UIFactory.Place(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -72 - i * 70), new Vector2(420, 62));
                var ic = UIFactory.Icon(row.transform, IconFactory.Waste((WasteType)i), Color.white, 50);
                UIFactory.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(50, 50));
                var name = UIFactory.Label(row.transform, cfg.GetWaste((WasteType)i).displayName, 22, UIColors.Text);
                UIFactory.Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(280, 40));
                _invCounts[i] = UIFactory.Label(row.transform, "0", 30, UIColors.Yellow, TextAnchor.MiddleRight, FontStyle.Bold);
                UIFactory.Place(_invCounts[i].rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(80, 40));
            }
            var bar = UIFactory.Bar(p.transform, UIColors.NavyDeep, UIColors.Turquoise, out _invBar);
            UIFactory.Place(bar.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 76), new Vector2(412, 18));
            _invText = UIFactory.Label(p.transform, "", 22, UIColors.TextDim, TextAnchor.MiddleLeft);
            UIFactory.Place(_invText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 34), new Vector2(412, 34));
            var hint = UIFactory.Label(p.transform, "[I] cerrar", 18, UIColors.TextDim, TextAnchor.UpperRight);
            UIFactory.Place(hint.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -26), new Vector2(140, 30));
            _inventory.SetActive(false);
        }

        void RefreshInventory()
        {
            for (int i = 0; i < 5; i++) _invCounts[i].text = GM.Inventory.Get((WasteType)i).ToString();
            int used = GM.Inventory.Used, cap = GM.Inventory.Capacity;
            UIFactory.SetBar(_invBar, used / (float)Mathf.Max(1, cap));
            _invText.text = $"Usado: {used}   ·   Libre: {Mathf.Max(0, cap - used)}   ·   Capacidad: {cap}";
        }

        public void ToggleInventory() { _inventory.SetActive(!_inventory.activeSelf); if (_inventory.activeSelf) RefreshInventory(); AudioManager.I.PlaySfx(Sfx.Click); }

        // ======================= Diálogo =======================
        void BuildDialogue()
        {
            var p = UIFactory.Panel(_root, "Dialogo", new Color(0.04f, 0.13f, 0.22f, 0.96f));
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1300, 230));
            _dialogue = p.gameObject;
            var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = UIColors.Teal; ol.effectDistance = new Vector2(3, 3);
            var btn = p.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(AdvanceDialogue);

            _dlgPortrait = UIFactory.Icon(p.transform, UIFactory.Circle, UIColors.Teal, 150);
            UIFactory.Place(_dlgPortrait.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(150, 150));
            _dlgInitials = UIFactory.Label(_dlgPortrait.transform, "", 56, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_dlgInitials.rectTransform);
            _dlgSpeaker = UIFactory.Label(p.transform, "", 30, UIColors.Turquoise, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_dlgSpeaker.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(210, -22), new Vector2(1000, 40));
            _dlgText = UIFactory.Label(p.transform, "", 30, UIColors.Text, TextAnchor.UpperLeft);
            UIFactory.Place(_dlgText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(210, -70), new Vector2(1050, 120));
            var hint = UIFactory.Label(p.transform, "E / Espacio / clic para continuar", 18, UIColors.TextDim, TextAnchor.LowerRight);
            UIFactory.Place(hint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 14), new Vector2(500, 26));
            _dialogue.SetActive(false);
        }

        void OnDialogueLine()
        {
            _dialogue.SetActive(true);
            _dialogue.transform.SetAsLastSibling();
            var line = GM.Dialogue.Current;
            _dlgSpeaker.text = line.speaker;
            bool rosa = line.speaker == DialogueScripts.Rosa;
            bool mateo = line.speaker == DialogueScripts.Mateo;
            _dlgPortrait.color = rosa ? UIColors.Hex("#D9577A") : mateo ? UIColors.Hex("#2F78C4") : UIColors.Teal;
            _dlgInitials.text = rosa ? "DR" : mateo ? "DM" : "?";
            _dlgFull = line.text;
            _dlgChars = 0;
            _dlgTyping = true;
            _dlgText.text = "";
        }

        void OnDialogueClosed() { _dialogue.SetActive(false); }

        void AdvanceDialogue()
        {
            if (!GM.Dialogue.IsOpen) return;
            InteractionPrompt.UIConsumedFrame = Time.frameCount;
            if (_dlgTyping) { _dlgTyping = false; _dlgText.text = _dlgFull; return; }
            GM.Dialogue.Next();
        }

        void UpdateDialogue()
        {
            if (!_dialogue.activeSelf) return;
            if (_dlgTyping)
            {
                _dlgChars += Time.unscaledDeltaTime * 55f;
                int n = Mathf.Min(_dlgFull.Length, (int)_dlgChars);
                _dlgText.text = _dlgFull.Substring(0, n);
                if (n >= _dlgFull.Length) _dlgTyping = false;
            }
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                AdvanceDialogue();
        }

        // ======================= Puerto =======================
        void BuildPort()
        {
            var p = UIFactory.Panel(_root, "Puerto", UIColors.Panel);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(760, 600));
            _port = p.gameObject;
            UIFactory.Rule(p.transform, "Línea del puerto", UIColors.Yellow, new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -153), new Vector2(68, 3));
            var t = UIFactory.Label(p.transform, "Muelle de Pisco", 46, UIColors.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            t.font = UIFactory.DisplayFont;
            t.fontStyle = FontStyle.Normal;
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(700, 60));
            _portInfo = UIFactory.Label(p.transform, "", 24, UIColors.TextDim, TextAnchor.UpperCenter);
            UIFactory.Place(_portInfo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(680, 70));

            _portRosa = UIFactory.Button(p.transform, "Doña Rosa · Centro de reciclaje", UIColors.Teal, Color.white, OpenRecycling, 30);
            UIFactory.Place((RectTransform)_portRosa.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(620, 92));
            _portMateo = UIFactory.Button(p.transform, "Don Mateo · Taller de mejoras", UIColors.Hex("#2F78C4"), Color.white, OpenWorkshop, 30);
            UIFactory.Place((RectTransform)_portMateo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(620, 92));
            var sail = UIFactory.Button(p.transform, "Zarpar", UIColors.Green, UIColors.NavyDeep, Sail, 32);
            UIFactory.Place((RectTransform)sail.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(620, 92));
            var hint = UIFactory.Label(p.transform, "Teclas: 1 reciclaje · 2 taller · 3 zarpar", 18, UIColors.TextDim, TextAnchor.LowerCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(680, 30));
            _port.SetActive(false);
        }

        void OnDocked()
        {
            if (GM.InVictory) return;
            OpenPort();
        }

        public void OpenPort()
        {
            if (!_port.activeSelf) { _port.SetActive(true); GM.PushModal(); }
            RefreshPort();
        }

        void ClosePort()
        {
            if (_port.activeSelf) { _port.SetActive(false); GM.PopModal(); }
        }

        void RefreshPort()
        {
            _portInfo.text = $"Chalana atracada · Bodega {GM.Inventory.Used}/{GM.Inventory.Capacity} · {GM.State.credits} EcoCréditos\n<size=20>Partida guardada automáticamente.</size>";
        }

        public void Sail()
        {
            ClosePort();
            GM.dock.Undock();
        }

        // ======================= Reciclaje =======================
        void BuildRecycling()
        {
            var p = UIFactory.Panel(_root, "Reciclaje", UIColors.Panel);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1400, 800));
            _recycling = p.gameObject;
            var t = UIFactory.Label(p.transform, "Centro de reciclaje · Doña Rosa", 42, UIColors.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -26), new Vector2(900, 56));
            var st = UIFactory.Label(p.transform, "Elige el contenedor correcto para cada residuo (clic o teclas 1-5).", 22, UIColors.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(st.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -84), new Vector2(1000, 30));
            _recSession = UIFactory.Label(p.transform, "", 26, UIColors.Yellow, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Place(_recSession.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -34), new Vector2(460, 80));

            // Tarjeta del residuo actual
            var card = UIFactory.Panel(p.transform, "Residuo", UIColors.PanelLight);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(1320, 250));
            _recIcon = UIFactory.Icon(card.transform, IconFactory.Waste(WasteType.Botella), Color.white, 190);
            UIFactory.Place(_recIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(190, 190));
            _recItemName = UIFactory.Label(card.transform, "", 40, UIColors.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_recItemName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(260, -30), new Vector2(1020, 50));
            _recRemaining = UIFactory.Label(card.transform, "", 22, UIColors.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(_recRemaining.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(260, -84), new Vector2(1020, 30));
            _recFeedback = UIFactory.Label(card.transform, "", 24, UIColors.Text, TextAnchor.UpperLeft);
            UIFactory.Place(_recFeedback.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(260, -124), new Vector2(1020, 110));

            _recEmpty = UIFactory.Label(card.transform, "Tu bodega está vacía. ¡Sal a recoger residuos y vuelve!", 32, UIColors.Text, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject;
            UIFactory.Stretch((RectTransform)_recEmpty.transform, 260, 0, 30, 0);

            // Contenedores
            var cfg = GameConfig.Load();
            for (int i = 0; i < 5; i++)
            {
                var info = cfg.GetBin((BinType)i);
                int idx = i;
                var b = UIFactory.Button(p.transform, "", UIColors.PanelLight, UIColors.Text, () => SortInto((BinType)idx), 26);
                var rt = (RectTransform)b.transform;
                UIFactory.Place(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40 + i * 266, 130), new Vector2(250, 250));
                var lbl = b.GetComponentInChildren<Text>();
                lbl.text = $"{info.displayName}\n<size=19>{info.subtitle}</size>";
                lbl.alignment = TextAnchor.UpperCenter;
                UIFactory.Stretch(lbl.rectTransform, 14, 12, 14, 122);
                var lid = UIFactory.Panel(b.transform, "Color del contenedor", info.color, UIFactory.RoundedSmall);
                UIFactory.Place(lid.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -18), new Vector2(200, 8));
                lid.raycastTarget = false;
                var key = UIFactory.Panel(b.transform, "Tecla", info.color, UIFactory.Circle);
                UIFactory.Place(key.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -46), new Vector2(54, 54));
                key.raycastTarget = false;
                var digit = UIFactory.Label(key.transform, (i + 1).ToString(), 25, info.textColor,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(digit.rectTransform);
                var ol = b.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.4f); ol.effectDistance = new Vector2(3, -3);
                _binButtons[i] = b;
            }

            var close = UIFactory.Button(p.transform, "Terminar", UIColors.Coral, Color.white, CloseRecycling, 28);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 30), new Vector2(240, 70));
            var hint = UIFactory.Label(p.transform, "Un error no quita EcoCréditos: Doña Rosa te muestra el contenedor correcto y puedes reintentar.", 20, UIColors.TextDim, TextAnchor.MiddleLeft);
            UIFactory.Place(hint.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 46), new Vector2(1060, 40));
            _recycling.SetActive(false);
        }

        public void OpenRecycling()
        {
            if (!_metRosa)
            {
                _metRosa = true;
                GM.Dialogue.Show(DialogueScripts.RosaGreeting, OpenRecycling);
                return;
            }
            _port.SetActive(false);
            _recycling.SetActive(true);
            GM.Recycling.StartSession();
            _recFeedback.text = "";
            RefreshRecycling();
        }

        public void CloseRecycling()
        {
            _recycling.SetActive(false);
            if (GM.boat.IsDocked) { _port.SetActive(true); RefreshPort(); }
            else GM.PopModal();
        }

        void SortInto(BinType bin)
        {
            if (Time.unscaledTime < _recLock || !GM.Recycling.HasCurrent) return;
            var r = GM.Recycling.TrySort(bin);
            var cfg = GM.Config;
            var info = cfg.GetWaste(r.waste);
            if (r.correct)
            {
                string tip = r.firstTimeForType ? "\n<color=#A9C3D6>Doña Rosa: " + info.tip + "</color>" : "";
                _recFeedback.text = $"<color=#5BC17F><b>¡Correcto!</b></color> {info.displayName} → {cfg.GetBin(bin).displayName}.  <color=#F6C445>+{r.credits} EC</color>{tip}";
                StartCoroutine(Punch(_binButtons[(int)bin].transform));
                _recLock = Time.unscaledTime + 0.25f;
            }
            else
            {
                _recFeedback.text = $"<color=#EF6F5E><b>Ese no es su contenedor.</b></color> Sin recompensa. Va en <b>{cfg.GetBin(r.correctBin).displayName}</b>. ¡Inténtalo de nuevo!" +
                                    (r.waste == WasteType.CajaTecnopor ? "\n<color=#A9C3D6>El tecnopor del mar no es reciclable aquí.</color>" : "");
                StartCoroutine(Shake(_binButtons[(int)bin].transform));
                StartCoroutine(Flash(_binButtons[(int)r.correctBin].transform));
                _recLock = Time.unscaledTime + 0.35f;
            }
            RefreshRecycling();
        }

        void RefreshRecycling()
        {
            var rec = GM.Recycling;
            bool has = rec.HasCurrent;
            _recEmpty.SetActive(!has);
            _recIcon.gameObject.SetActive(has);
            _recItemName.gameObject.SetActive(has);
            _recRemaining.gameObject.SetActive(has);
            foreach (var b in _binButtons) b.interactable = has;
            if (has)
            {
                var info = GM.Config.GetWaste(rec.Current);
                _recIcon.sprite = IconFactory.Waste(rec.Current);
                _recItemName.text = info.displayName;
                _recRemaining.text = $"Quedan por clasificar: {rec.Remaining}";
            }
            else if (rec.SessionCorrect > 0)
            {
                _recEmpty.GetComponent<Text>().text = $"¡Bodega vacía! Clasificaste {rec.SessionCorrect} residuos y ganaste {rec.SessionCredits} EcoCréditos.";
            }
            else _recEmpty.GetComponent<Text>().text = "Tu bodega está vacía. ¡Sal a recoger residuos y vuelve!";
            _recSession.text = $"Ganado ahora: +{rec.SessionCredits} EC\nTotal: {GM.State.credits} EC";
        }

        IEnumerator Punch(Transform t)
        {
            float time = 0;
            while (time < 0.25f) { time += Time.unscaledDeltaTime; t.localScale = Vector3.one * (1f + Mathf.Sin(time / 0.25f * Mathf.PI) * 0.12f); yield return null; }
            t.localScale = Vector3.one;
        }

        IEnumerator Shake(Transform t)
        {
            var rt = (RectTransform)t; Vector2 basePos = rt.anchoredPosition; float time = 0;
            while (time < 0.3f) { time += Time.unscaledDeltaTime; rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(time * 70f) * 10f * (1 - time / 0.3f), 0); yield return null; }
            rt.anchoredPosition = basePos;
        }

        IEnumerator Flash(Transform t)
        {
            var img = t.GetComponent<Image>(); var ol = t.GetComponent<Outline>();
            Color c0 = ol.effectColor; Vector2 d0 = ol.effectDistance; float time = 0;
            while (time < 1.6f)
            {
                time += Time.unscaledDeltaTime;
                bool on = Mathf.Repeat(time, 0.4f) < 0.25f;
                ol.effectColor = on ? UIColors.Yellow : c0; ol.effectDistance = on ? new Vector2(8, 8) : d0;
                yield return null;
            }
            ol.effectColor = c0; ol.effectDistance = d0;
        }

        // ======================= Taller =======================
        void BuildWorkshop()
        {
            var p = UIFactory.Panel(_root, "Taller", UIColors.Panel);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1400, 720));
            _workshop = p.gameObject;
            var t = UIFactory.Label(p.transform, "Taller de Don Mateo", 42, UIColors.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -26), new Vector2(900, 56));
            var st = UIFactory.Label(p.transform, "Cada mejora se compra una vez y se instala en tu misma chalana.", 22, UIColors.TextDim, TextAnchor.UpperLeft);
            UIFactory.Place(st.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -84), new Vector2(1000, 30));
            _wsCredits = UIFactory.Label(p.transform, "", 30, UIColors.Yellow, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Place(_wsCredits.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -34), new Vector2(500, 44));

            var cfg = GameConfig.Load();
            for (int i = 0; i < 3; i++)
            {
                var u = cfg.GetUpgrade((UpgradeType)i);
                int idx = i;
                var card = UIFactory.Panel(p.transform, "Mejora", UIColors.PanelLight);
                UIFactory.Place(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36 + i * 448, -140), new Vector2(424, 450));
                UIFactory.Rule(card.transform, "Acento", UIColors.Yellow, new Vector2(0.5f, 1),
                    new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(56, 3));
                var ic = UIFactory.Icon(card.transform, IconFactory.Upgrade((UpgradeType)i), Color.white, 110);
                UIFactory.Place(ic.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(110, 110));
                var name = UIFactory.Label(card.transform, u.displayName, 32, UIColors.Text, TextAnchor.UpperCenter, FontStyle.Bold);
                UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(400, 40));
                var eff = UIFactory.Label(card.transform, u.effectText, 24, UIColors.Turquoise, TextAnchor.UpperCenter, FontStyle.Bold);
                UIFactory.Place(eff.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -186), new Vector2(400, 34));
                var desc = UIFactory.Label(card.transform, u.description, 21, UIColors.TextDim, TextAnchor.UpperCenter);
                UIFactory.Place(desc.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -228), new Vector2(380, 80));
                _wsStatus[i] = UIFactory.Label(card.transform, "", 26, UIColors.Yellow, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Place(_wsStatus[i].rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(400, 36));
                _wsBuy[i] = UIFactory.Button(card.transform, "Comprar", UIColors.Green, UIColors.NavyDeep, () => AskBuy((UpgradeType)idx), 28);
                UIFactory.Place((RectTransform)_wsBuy[i].transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(320, 74));
            }
            var close = UIFactory.Button(p.transform, "Volver al muelle", UIColors.Coral, Color.white, CloseWorkshop, 28);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 30), new Vector2(300, 70));
            _workshop.SetActive(false);
        }

        public void OpenWorkshop()
        {
            if (!_metMateo)
            {
                _metMateo = true;
                GM.Dialogue.Show(DialogueScripts.MateoGreeting, OpenWorkshop);
                return;
            }
            _port.SetActive(false);
            _workshop.SetActive(true);
            RefreshWorkshop();
        }

        public void CloseWorkshop()
        {
            _workshop.SetActive(false);
            if (GM.boat.IsDocked) { _port.SetActive(true); RefreshPort(); }
            else GM.PopModal();
        }

        void RefreshWorkshop()
        {
            _wsCredits.text = $"Tienes {GM.State.credits} EcoCréditos";
            for (int i = 0; i < 3; i++)
            {
                var t = (UpgradeType)i;
                bool owned = GM.Upgrades.IsOwned(t);
                int cost = GM.Upgrades.Cost(t);
                _wsStatus[i].text = owned ? "INSTALADA" : $"Costo: {cost} EC";
                _wsStatus[i].color = owned ? UIColors.Green : (GM.Upgrades.CanAfford(t) ? UIColors.Yellow : UIColors.Alert);
                _wsBuy[i].interactable = !owned && GM.Upgrades.CanAfford(t);
                _wsBuy[i].GetComponentInChildren<Text>().text = owned ? "Comprada" : (GM.Upgrades.CanAfford(t) ? "Comprar" : $"Faltan {cost - GM.State.credits} EC");
            }
        }

        void AskBuy(UpgradeType t)
        {
            var u = GM.Config.GetUpgrade(t);
            ShowConfirm($"¿Comprar <b>{u.displayName}</b> por <color=#F6C445>{u.cost} EcoCréditos</color>?\n<size=24>{u.effectText}</size>", () =>
            {
                GM.Upgrades.Buy(t);
                RefreshWorkshop();
            });
        }

        // ======================= Confirmación =======================
        void BuildConfirm()
        {
            var shade = UIFactory.Panel(_root, "Confirmar", new Color(0, 0, 0, 0.55f));
            shade.sprite = null; shade.type = Image.Type.Simple;
            UIFactory.Stretch(shade.rectTransform);
            _confirm = shade.gameObject;
            var p = UIFactory.Panel(shade.transform, "Caja", UIColors.Navy);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 340));
            var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = UIColors.Turquoise; ol.effectDistance = new Vector2(3, 3);
            _confirmText = UIFactory.Label(p.transform, "", 32, UIColors.Text, TextAnchor.MiddleCenter);
            UIFactory.Place(_confirmText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(680, 160));
            var yes = UIFactory.Button(p.transform, "Sí", UIColors.Green, UIColors.NavyDeep, () => { _confirm.SetActive(false); var a = _confirmYes; _confirmYes = null; a?.Invoke(); }, 30);
            UIFactory.Place((RectTransform)yes.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-150, 34), new Vector2(260, 80));
            var no = UIFactory.Button(p.transform, "No", UIColors.Coral, Color.white, () => { _confirm.SetActive(false); _confirmYes = null; }, 30);
            UIFactory.Place((RectTransform)no.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(150, 34), new Vector2(260, 80));
            _confirm.SetActive(false);
        }

        void ShowConfirm(string text, System.Action yes)
        {
            _confirmText.text = text;
            _confirmYes = yes;
            _confirm.SetActive(true);
            _confirm.transform.SetAsLastSibling();
        }

        // ======================= Controles =======================
        void BuildControls()
        {
            var shade = UIFactory.Panel(_root, "Controles", new Color(0, 0, 0, 0.5f));
            shade.sprite = null; shade.type = Image.Type.Simple;
            UIFactory.Stretch(shade.rectTransform);
            _controls = shade.gameObject;
            BuildControlsContent(shade.transform, () => ShowControls(false));
            _controls.SetActive(false);
        }

        public static void BuildControlsContent(Transform parent, UnityEngine.Events.UnityAction onClose)
        {
            var p = UIFactory.Panel(parent, "Caja", UIColors.Navy);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 720));
            var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = UIColors.Teal; ol.effectDistance = new Vector2(3, 3);
            var t = UIFactory.Label(p.transform, "Controles", 46, UIColors.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(800, 60));
            string[,] rows =
            {
                { "W / S", "Avanzar / frenar y retroceder" },
                { "A / D", "Girar la chalana" },
                { "Mouse (clic der.)", "Orientar la cámara · rueda: zoom" },
                { "E", "Recoger residuo / atracar / continuar diálogo" },
                { "I", "Abrir o cerrar la bodega (inventario)" },
                { "Esc", "Pausa" },
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                var key = UIFactory.Panel(p.transform, "Tecla", UIColors.Teal, UIFactory.RoundedSmall);
                UIFactory.Place(key.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -110 - i * 72), new Vector2(260, 56));
                var kt = UIFactory.Label(key.transform, rows[i, 0], 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(kt.rectTransform);
                var dt = UIFactory.Label(p.transform, rows[i, 1], 26, UIColors.Text, TextAnchor.MiddleLeft);
                UIFactory.Place(dt.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(345, -110 - i * 72), new Vector2(520, 56));
            }
            var goal = UIFactory.Label(p.transform, "Objetivo: recoge residuos desde tu chalana, vuelve al muelle, recicla con Doña Rosa y mejora tu barco con Don Mateo. Sigue la flecha amarilla.", 22, UIColors.TextDim, TextAnchor.UpperCenter);
            UIFactory.Place(goal.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(800, 70));
            var ok = UIFactory.Button(p.transform, "¡Entendido!", UIColors.Green, UIColors.NavyDeep, onClose, 30);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(320, 76));
        }

        public void ShowControls(bool show)
        {
            if (show == _controls.activeSelf) return;
            _controls.SetActive(show);
            if (show) { _controls.transform.SetAsLastSibling(); GM.PushModal(); _controlsModal = true; }
            else if (_controlsModal) { _controlsModal = false; GM.PopModal(); }
        }

        // ======================= Pausa =======================
        void BuildPause()
        {
            var shade = UIFactory.Panel(_root, "Pausa", new Color(0.02f, 0.08f, 0.14f, 0.7f));
            shade.sprite = null; shade.type = Image.Type.Simple;
            UIFactory.Stretch(shade.rectTransform);
            _pause = shade.gameObject;
            var p = UIFactory.Panel(shade.transform, "Caja", UIColors.Navy);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 600));
            var t = UIFactory.Label(p.transform, "Pausa", 54, UIColors.Text, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(560, 70));
            string[] labels = { "Continuar", "Controles", "Reiniciar partida", "Menú principal" };
            UnityEngine.Events.UnityAction[] acts =
            {
                () => SetPause(false),
                () => { _pause.SetActive(false); ShowControlsFromPause(); },
                () => ShowConfirm("¿Reiniciar desde el principio?\n<size=24>Se borrará el progreso guardado.</size>", () => GM.RestartNewGame()),
                () => GM.GoToMenu(true),
            };
            Color[] cols = { UIColors.Green, UIColors.Teal, UIColors.Hex("#2F78C4"), UIColors.Coral };
            for (int i = 0; i < 4; i++)
            {
                var b = UIFactory.Button(p.transform, labels[i], cols[i], i == 0 ? UIColors.NavyDeep : Color.white, acts[i], 30);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130 - i * 108), new Vector2(460, 88));
            }
            _pause.SetActive(false);
        }

        void ShowControlsFromPause()
        {
            _controls.SetActive(true);
            _controls.transform.SetAsLastSibling();
            StartCoroutine(ReturnToPauseWhenControlsClosed());
        }

        IEnumerator ReturnToPauseWhenControlsClosed()
        {
            while (_controls.activeSelf) yield return null;
            if (GM.Paused) { _pause.SetActive(true); }
        }

        public void SetPause(bool p)
        {
            if (p == GM.Paused) return;
            GM.SetPaused(p);
            _pause.SetActive(p);
            if (p) _pause.transform.SetAsLastSibling();
            else { _confirm.SetActive(false); }
        }

        // ======================= Victoria =======================
        void BuildVictory()
        {
            var shade = UIFactory.Panel(_root, "Victoria", new Color(0.02f, 0.08f, 0.14f, 0.55f));
            shade.sprite = null; shade.type = Image.Type.Simple;
            UIFactory.Stretch(shade.rectTransform);
            _victory = shade.gameObject;
            var p = UIFactory.Panel(shade.transform, "Caja", UIColors.Navy);
            UIFactory.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 860));
            var ol = p.gameObject.AddComponent<Outline>(); ol.effectColor = UIColors.Yellow; ol.effectDistance = new Vector2(4, 4);
            var t = UIFactory.Label(p.transform, "Guardianes de la bahía", 56, UIColors.Yellow, TextAnchor.UpperCenter);
            t.font = UIFactory.DisplayFont;
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1000, 70));
            var sub = UIFactory.Label(p.transform, "La bahía de Pisco vuelve a tener vida. ODS 12 · ODS 13 · ODS 14", 26, UIColors.Turquoise, TextAnchor.UpperCenter);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(1000, 36));
            var report = UIFactory.Panel(p.transform, "Resumen de la bahía", UIColors.PanelLight);
            UIFactory.Place(report.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -165), new Vector2(968, 510));
            _victoryStats = UIFactory.Label(report.transform, "", 28, UIColors.Text, TextAnchor.UpperLeft);
            _victoryStats.lineSpacing = 1.12f;
            UIFactory.Stretch(_victoryStats.rectTransform, 35, 22, 35, 26);
            var keep = UIFactory.Button(p.transform, "Seguir navegando", UIColors.Green, UIColors.NavyDeep, () =>
            {
                _victory.SetActive(false); GM.PopModal(); GM.ContinueSailingAfterVictory();
            }, 30);
            UIFactory.Place((RectTransform)keep.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-220, 36), new Vector2(400, 86));
            var menu = UIFactory.Button(p.transform, "Volver al menú", UIColors.Hex("#2F78C4"), Color.white, () => GM.GoToMenu(true), 30);
            UIFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(220, 36), new Vector2(400, 86));
            _victory.SetActive(false);
        }

        public void ShowVictory()
        {
            var s = GM.State;
            var cfg = GM.Config;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>Residuos recogidos por tipo</b>");
            int total = 0;
            for (int i = 0; i < 5; i++)
            {
                total += s.collectedByType[i];
                sb.AppendLine($"   · {cfg.GetWaste((WasteType)i).displayName}: <color=#F6C445>{s.collectedByType[i]}</color>");
            }
            sb.AppendLine($"   Total retirado del mar: <color=#F6C445>{total}</color> de {GM.Environment.TotalWaste}  (limpieza {GM.Environment.CleanPercent} %)");
            sb.AppendLine();
            sb.AppendLine($"<b>EcoCréditos</b> ganados: <color=#F6C445>{s.creditsEarned}</color>   ·   disponibles: <color=#F6C445>{s.credits}</color>");
            var ups = new List<string>();
            for (int i = 0; i < 3; i++) if (s.HasUpgrade((UpgradeType)i)) ups.Add(cfg.GetUpgrade((UpgradeType)i).displayName);
            sb.AppendLine($"<b>Mejoras</b> ({ups.Count}/3): {(ups.Count > 0 ? string.Join(", ", ups) : "ninguna")}");
            sb.AppendLine($"<b>Misiones</b> completadas: <color=#F6C445>{Mathf.Min(5, s.missionIndex)}/5</color>");
            int min = Mathf.FloorToInt(s.playTime / 60f);
            sb.AppendLine($"<b>Tiempo de juego</b>: {min} min {Mathf.FloorToInt(s.playTime % 60):00} s");
            _victoryStats.text = sb.ToString();
            _victory.SetActive(true);
            _victory.transform.SetAsLastSibling();
            GM.PushModal();
        }

        // ======================= Entrada y refresco =======================
        void RefreshAll()
        {
            if (GM == null) return;
            if (_inventory.activeSelf) RefreshInventory();
            if (_port.activeSelf) RefreshPort();
            if (_workshop.activeSelf) RefreshWorkshop();
        }

        void Update()
        {
            if (GM == null) return;
            UpdateHUD();
            UpdateToasts();
            UpdateDialogue();
            HandleKeys();
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (_journal.HandleKeys(kb)) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                InteractionPrompt.UIConsumedFrame = Time.frameCount;
                if (_confirm.activeSelf) { _confirm.SetActive(false); _confirmYes = null; return; }
                if (_controls.activeSelf) { ShowControls(false); return; }
                if (GM.Paused) { SetPause(false); return; }
                if (_victory.activeSelf) return;
                if (_recycling.activeSelf) { CloseRecycling(); return; }
                if (_workshop.activeSelf) { CloseWorkshop(); return; }
                if (_inventory.activeSelf) { _inventory.SetActive(false); return; }
                SetPause(true);
                return;
            }
            if (GM.Paused || _confirm.activeSelf || _controls.activeSelf || GM.Dialogue.IsOpen) return;

            if (kb.iKey.wasPressedThisFrame && !_victory.activeSelf) ToggleInventory();

            if (_port.activeSelf)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) OpenRecycling();
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) OpenWorkshop();
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Sail();
            }
            else if (_recycling.activeSelf)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
                Key[] pad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };
                for (int i = 0; i < 5; i++)
                    if (kb[keys[i]].wasPressedThisFrame || kb[pad[i]].wasPressedThisFrame) SortInto((BinType)i);
            }
        }

        // ======================= API para la prueba automatizada =======================
        public void Test_SortInto(BinType b) => SortInto(b);
        public void Test_CloseAllPanels()
        {
            if (_recycling.activeSelf) CloseRecycling();
            if (_workshop.activeSelf) CloseWorkshop();
        }
        public void Test_ConfirmYes() { if (_confirm.activeSelf) { _confirm.SetActive(false); var a = _confirmYes; _confirmYes = null; a?.Invoke(); } }
        public void Test_AskBuy(UpgradeType t) => AskBuy(t);
        public void Test_ContinueSailing() { if (_victory.activeSelf) { _victory.SetActive(false); GM.PopModal(); GM.ContinueSailingAfterVictory(); } }
        public bool RecyclingOpen => _recycling.activeSelf;
        public bool WorkshopOpen => _workshop.activeSelf;
        public bool PauseOpen => _pause.activeSelf;
        public bool InventoryOpen => _inventory.activeSelf;
        public string RecyclingFeedback => _recFeedback.text;
    }
}
