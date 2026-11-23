using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RecolecEco
{
    /// <summary>Paleta de la interfaz: azul marino, verde y turquesa (§6).</summary>
    public static class UIColors
    {
        public static readonly Color Navy = Hex("#102C3A");
        public static readonly Color NavyDeep = Hex("#0B202A");
        public static readonly Color Panel = new Color(0.047f, 0.121f, 0.151f, 0.96f);
        public static readonly Color PanelLight = Hex("#1B3945");
        public static readonly Color Teal = Hex("#236361");
        public static readonly Color Turquoise = Hex("#89D8CD");
        public static readonly Color Green = Hex("#95D5AC");
        public static readonly Color Yellow = Hex("#EAC683");
        public static readonly Color Coral = Hex("#AD564A");
        public static readonly Color Alert = Hex("#F3A496");
        public static readonly Color Text = Hex("#F5EFE3");
        public static readonly Color TextDim = Hex("#B0C5C8");
        public static readonly Color Disabled = Hex("#4A5D70");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }

    /// <summary>Construcción de interfaz por código (uGUI) con sprites generados.</summary>
    public static class UIFactory
    {
        static Font _font, _displayFont;
        static Sprite _menuGradient;
        static Sprite _rounded, _roundedSmall, _circle, _triangle, _ring;

        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = Resources.Load<Font>("Tipografia/NotoSansDisplay-Regular")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Font DisplayFont => _displayFont
            ? _displayFont
            : (_displayFont = Resources.Load<Font>("Tipografia/NotoSerifDisplay-Regular") ?? Font);

        public static Sprite MenuGradient
        {
            get
            {
                if (_menuGradient) return _menuGradient;
                const int width = 128;
                var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color[width];
                for (int i = 0; i < width; i++)
                {
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((i / 127f - 0.13f) / 0.61f));
                    pixels[i] = new Color(1, 1, 1, a);
                }
                tex.SetPixels(pixels);
                tex.Apply(false, true);
                _menuGradient = Sprite.Create(tex, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
                return _menuGradient;
            }
        }

        public static Image Rule(Transform parent, string name, Color color, Vector2 anchor,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var line = Panel(parent, name, color);
            line.sprite = null;
            line.type = Image.Type.Simple;
            line.raycastTarget = false;
            Place(line.rectTransform, anchor, pivot, position, size);
            return line;
        }

        public static Sprite Rounded => _rounded ? _rounded : (_rounded = MakeRounded(96, 15));
        public static Sprite RoundedSmall => _roundedSmall ? _roundedSmall : (_roundedSmall = MakeRounded(48, 8));
        public static Sprite Circle => _circle ? _circle : (_circle = MakeCircle(128, false));
        public static Sprite Ring => _ring ? _ring : (_ring = MakeCircle(128, true));
        public static Sprite Triangle => _triangle ? _triangle : (_triangle = MakeTriangle(64));

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        static Sprite MakeCircle(int size, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float a = Mathf.Clamp01(r - d);
                    if (ring) a *= Mathf.Clamp01(d - (r - 10f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite MakeTriangle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Triángulo apuntando hacia arriba
                    float fy = (y + 0.5f) / size;
                    float half = (1f - fy) * 0.5f;
                    float fx = Mathf.Abs((x + 0.5f) / size - 0.5f);
                    float a = Mathf.Clamp01((half - fx) * size);
                    if (fy < 0.05f) a = 0;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color c, Sprite sprite = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : Rounded;
            img.type = img.sprite == Rounded || img.sprite == RoundedSmall ? Image.Type.Sliced : Image.Type.Simple;
            img.color = c;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color c, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool shadow = false)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.6f);
                s.effectDistance = new Vector2(2, -2);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Color bg, Color fg, UnityAction onClick, int fontSize = 30)
        {
            var img = Panel(parent, "Btn_" + label, bg);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            colors.colorMultiplier = 1.15f;
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null)
            {
                btn.onClick.AddListener(() => AudioManager.I.PlaySfx(Sfx.Click));
                btn.onClick.AddListener(onClick);
            }
            var t = Label(img.transform, label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform, 12, 4, 12, 4);
            return btn;
        }

        public static Image Bar(Transform parent, Color back, Color fill, out Image fillImg)
        {
            var bg = Panel(parent, "Bar", back, RoundedSmall);
            fillImg = Panel(bg.transform, "Fill", fill, RoundedSmall);
            var rt = fillImg.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
            return bg;
        }

        public static void SetBar(Image fill, float v)
        {
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
            fill.enabled = v > 0.005f;
        }

        public static Image Icon(Transform parent, Sprite s, Color c, float size)
        {
            var rt = Rect(parent, "Icon");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.color = c; img.preserveAspect = true; img.raycastTarget = false;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }
    }
}
