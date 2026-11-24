using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RecolecEco
{
    public sealed partial class FieldJournal : MonoBehaviour
    {
        static readonly Vector2 TopLeft = new Vector2(0, 1);
        static readonly Vector2 Center = new Vector2(.5f, .5f);
        GameManager GM => GameManager.I;
        GameObject _overlay;
        Transform _tabs, _body;
        ReportMap _map;
        Action _refresh;
        int _tabCount;
        bool _open;

        partial void AddImpact();
        partial void AddContract();
        partial void AddQuiz();
        partial void AddAudio();

        public bool HandleKeys(Keyboard keys)
        {
            if (_open)
            {
                if (keys.mKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame) Close();
                return true;
            }
            if (!keys.mKey.wasPressedThisFrame || !GM.State.introShown || GM.ControlsLocked) return false;
            if (!_overlay) Build();
            _overlay.SetActive(true);
            _open = true;
            GM.PushModal();
            ShowMap();
            return true;
        }

        void OnDisable()
        {
            if (_open && GM) Close();
        }

        void LateUpdate()
        {
            if (_open) _refresh?.Invoke();
        }

        void Close()
        {
            _open = false;
            _overlay.SetActive(false);
            InteractionPrompt.UIConsumedFrame = Time.frameCount;
            GM.PopModal();
        }

        void Build()
        {
            var canvas = UIFactory.CreateCanvas("Bitácora", 20);
            canvas.transform.SetParent(transform, false);
            _overlay = canvas.gameObject;
            var backdrop = UIFactory.Panel(canvas.transform, "Fondo", new Color(0.02f, 0.06f, 0.07f, .67f));
            UIFactory.Stretch(backdrop.rectTransform);
            var frame = UIFactory.Panel(backdrop.transform, "Bitácora de Pisco", UIColors.Panel);
            UIFactory.Place(frame.rectTransform, Center, Center, Vector2.zero, new Vector2(1060, 764));
            var title = Line(frame.transform, "Bitácora de Pisco", 53, UIColors.Text, 38, 24, 760, 68);
            title.font = UIFactory.DisplayFont;
            ActionButton(frame.transform, "Cerrar   Esc", Close, 842, 34, 180, 52);
            _tabs = UIFactory.Rect(frame.transform, "Pestañas");
            UIFactory.Place((RectTransform)_tabs, TopLeft, TopLeft, new Vector2(38, -118), new Vector2(984, 58));
            _body = UIFactory.Rect(frame.transform, "Contenido");
            UIFactory.Place((RectTransform)_body, TopLeft, TopLeft, new Vector2(38, -208), new Vector2(984, 514));
            AddTab("Carta náutica", ShowMap);
            AddImpact();
            AddContract();
            AddQuiz();
            AddAudio();
        }

        void AddTab(string title, Action show)
        {
            ActionButton(_tabs, title, show, 198 * _tabCount, 0, 186, 54);
            _tabCount++;
        }

        void ClearPage()
        {
            _refresh = null;
            foreach (Transform child in _body)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        static Text Line(Transform parent, string label, int size, Color color, float x, float y, float w, float h)
        {
            var text = UIFactory.Label(parent, label, size, color, TextAnchor.UpperLeft);
            UIFactory.Place(text.rectTransform, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
            return text;
        }

        static Button ActionButton(Transform parent, string label, Action action, float x, float y, float w, float h)
        {
            var button = UIFactory.Button(parent, label, UIColors.PanelLight, UIColors.Text, () => action(), 23);
            UIFactory.Place((RectTransform)button.transform, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
            return button;
        }

        void ShowMap()
        {
            ClearPage();
            Line(_body, "Ubica tu chalana, el muelle y los sectores de limpieza.", 25, UIColors.TextDim, 0, 0, 960, 38);
            var panel = UIFactory.Panel(_body, "Mapa de la bahía", UIColors.Hex("#214C58"));
            UIFactory.Place(panel.rectTransform, TopLeft, TopLeft, new Vector2(0, -55), new Vector2(984, 396));
            _map = new ReportMap(panel.rectTransform, GM);
            _map.Update();
            _refresh = _map.Update;
            Line(_body, "Amarillo · chalana      Marfil · muelle      Turquesa · sectores      Coral · objetivo", 22,
                UIColors.Text, 0, 471, 984, 40);
        }

        sealed class ReportMap
        {
            readonly RectTransform _root;
            readonly GameManager _game;
            readonly RectTransform _boat, _goal, _dock;
            readonly SectorZone[] _zones;
            readonly Text[] _labels;
            readonly Vector2 _min, _max;

            public ReportMap(RectTransform root, GameManager game)
            {
                _root = root;
                _game = game;
                _zones = game.Environment.sectors;
                _labels = new Text[_zones.Length];
                Vector2 dock = XZ(game.dock.transform.position);
                _min = dock;
                _max = dock;
                foreach (var zone in _zones)
                {
                    if (!zone) continue;
                    var point = XZ(zone.transform.position);
                    _min = Vector2.Min(_min, point);
                    _max = Vector2.Max(_max, point);
                }
                _min -= new Vector2(37, 37);
                _max += new Vector2(37, 37);
                for (int i = 0; i < _zones.Length; i++)
                {
                    if (!_zones[i]) continue;
                    var zone = _zones[i];
                    var ring = Icon(UIFactory.Ring, UIColors.Turquoise, 42, zone.transform.position);
                    _labels[i] = Line(ring, "", 20, UIColors.Text, -44, 44, 178, 35);
                }
                _dock = Icon(UIFactory.Circle, UIColors.Text, 19, game.dock.transform.position);
                _goal = Icon(UIFactory.Ring, UIColors.Alert, 25, game.dock.transform.position);
                _boat = Icon(UIFactory.Triangle, UIColors.Yellow, 35, game.boat.transform.position);
            }

            RectTransform Icon(Sprite sprite, Color color, int size, Vector3 point)
            {
                var icon = UIFactory.Icon(_root, sprite, color, size);
                UIFactory.Place(icon.rectTransform, Center, Center, Plot(point), new Vector2(size, size));
                return icon.rectTransform;
            }

            public void Update()
            {
                _boat.anchoredPosition = Plot(_game.boat.transform.position);
                Vector3 direction = _game.boat.transform.forward;
                _boat.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
                _dock.anchoredPosition = Plot(_game.dock.transform.position);
                _goal.gameObject.SetActive(_game.Quests.HasTarget);
                if (_game.Quests.HasTarget) _goal.anchoredPosition = Plot(_game.Quests.TargetPosition);
                for (int i = 0; i < _zones.Length; i++)
                {
                    if (!_zones[i]) continue;
                    var id = _zones[i].id;
                    _labels[i].text = $"Sector {id} · {_game.Quests.SectorCollected(id)}/{_game.Quests.SectorTotal(id)}";
                }
            }

            static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

            Vector2 Plot(Vector3 position)
            {
                var p = XZ(position);
                var span = _max - _min;
                float scale = Mathf.Min(810f / span.x, 292f / span.y);
                var offset = (p - (_min + _max) * .5f) * scale;
                return new Vector2(Mathf.Clamp(offset.x, -450, 450), Mathf.Clamp(offset.y, -163, 150));
            }
        }
    }
}
