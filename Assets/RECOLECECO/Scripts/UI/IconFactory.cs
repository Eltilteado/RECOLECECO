using System.Collections.Generic;
using UnityEngine;

namespace RecolecEco
{
    /// <summary>Íconos dibujados por código para residuos y mejoras (sin recursos externos).</summary>
    public static class IconFactory
    {
        const int S = 96;
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Waste(WasteType t) => Get("w" + (int)t, px => DrawWaste(px, t));
        public static Sprite Upgrade(UpgradeType t) => Get("u" + (int)t, px => DrawUpgrade(px, t));
        public static Sprite Coin => Get("coin", DrawCoin);

        static Sprite Get(string key, System.Action<Color32[]> draw)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var px = new Color32[S * S];
            draw(px);
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100);
            _cache[key] = s;
            return s;
        }

        static void Blend(Color32[] px, int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= S || y >= S || a <= 0) return;
            int i = y * S + x;
            Color dst = px[i];
            float outA = a + dst.a * (1 - a);
            Color o = outA > 0 ? (c * a + dst * dst.a * (1 - a)) / outA : Color.clear;
            o.a = outA;
            px[i] = o;
        }

        delegate float Sdf(float x, float y);

        static void Fill(Color32[] px, Color c, Sdf sdf)
        {
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a > 0) Blend(px, x, y, c, a * c.a);
                }
        }

        static float Box(float x, float y, float cx, float cy, float hw, float hh, float r = 0)
        {
            float dx = Mathf.Abs(x - cx) - hw + r, dy = Mathf.Abs(y - cy) - hh + r;
            float ox = Mathf.Max(dx, 0), oy = Mathf.Max(dy, 0);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0) - r;
        }

        static float Circ(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        static float Seg(float x, float y, float ax, float ay, float bx, float by, float w)
        {
            Vector2 p = new Vector2(x, y), a = new Vector2(ax, ay), b = new Vector2(bx, by);
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - w;
        }

        static readonly Color Outline = new Color(0.04f, 0.12f, 0.2f, 1f);

        static void DrawWaste(Color32[] px, WasteType t)
        {
            switch (t)
            {
                case WasteType.Botella:
                    Fill(px, Outline, (x, y) => Mathf.Min(Box(x, y, 48, 38, 17, 28, 9), Box(x, y, 48, 74, 8, 12, 3)) - 3);
                    Fill(px, UIColors.Hex("#7FD3F0"), (x, y) => Mathf.Min(Box(x, y, 48, 38, 17, 28, 9), Box(x, y, 48, 72, 8, 10, 3)));
                    Fill(px, UIColors.Hex("#2F78C4"), (x, y) => Box(x, y, 48, 84, 9, 5, 2));
                    Fill(px, UIColors.Hex("#E8F7FD"), (x, y) => Box(x, y, 40, 40, 3, 18, 2));
                    Fill(px, UIColors.Hex("#4CB0D9"), (x, y) => Box(x, y, 48, 36, 17, 7, 0));
                    break;
                case WasteType.Lata:
                    Fill(px, Outline, (x, y) => Box(x, y, 48, 46, 22, 32, 8) - 3);
                    Fill(px, UIColors.Hex("#E35D5B"), (x, y) => Box(x, y, 48, 46, 22, 32, 8));
                    Fill(px, UIColors.Hex("#D9DEE3"), (x, y) => Box(x, y, 48, 74, 22, 5, 3));
                    Fill(px, UIColors.Hex("#D9DEE3"), (x, y) => Box(x, y, 48, 18, 22, 5, 3));
                    Fill(px, UIColors.Hex("#FFFFFF"), (x, y) => Box(x, y, 48, 46, 14, 6, 3));
                    break;
                case WasteType.Red:
                    Fill(px, Outline, (x, y) => Circ(x, y, 48, 48, 38));
                    Fill(px, UIColors.Hex("#2E7D63"), (x, y) => Circ(x, y, 48, 48, 35));
                    for (int k = -3; k <= 3; k++)
                    {
                        float o = k * 13;
                        Fill(px, UIColors.Hex("#8FE0BE"), (x, y) => Mathf.Max(Seg(x, y, 10 + o, 10, 86 + o, 86, 1.6f), Circ(x, y, 48, 48, 34)));
                        Fill(px, UIColors.Hex("#8FE0BE"), (x, y) => Mathf.Max(Seg(x, y, 86 + o, 10, 10 + o, 86, 1.6f), Circ(x, y, 48, 48, 34)));
                    }
                    Fill(px, UIColors.Hex("#F28C28"), (x, y) => Circ(x, y, 72, 72, 9));
                    Fill(px, UIColors.Hex("#F28C28"), (x, y) => Circ(x, y, 24, 28, 7));
                    break;
                case WasteType.CajaCarton:
                    Fill(px, Outline, (x, y) => Box(x, y, 48, 46, 34, 28, 4) - 3);
                    Fill(px, UIColors.Hex("#C9955A"), (x, y) => Box(x, y, 48, 46, 34, 28, 4));
                    Fill(px, UIColors.Hex("#A8743E"), (x, y) => Box(x, y, 48, 66, 34, 8, 2));
                    Fill(px, UIColors.Hex("#E8C48A"), (x, y) => Box(x, y, 48, 46, 6, 28, 0));
                    break;
                case WasteType.CajaTecnopor:
                    Fill(px, Outline, (x, y) => Box(x, y, 48, 46, 34, 28, 4) - 3);
                    Fill(px, UIColors.Hex("#F4F4EE"), (x, y) => Box(x, y, 48, 46, 34, 28, 4));
                    Fill(px, UIColors.Hex("#D4D8D2"), (x, y) => Box(x, y, 48, 66, 34, 4, 1));
                    for (int i = 0; i < 9; i++)
                    {
                        float cx = 26 + (i % 3) * 22, cy = 30 + (i / 3) * 12;
                        Fill(px, UIColors.Hex("#D4D8D2"), (x, y) => Circ(x, y, cx, cy, 2.5f));
                    }
                    Fill(px, UIColors.Hex("#E2453C"), (x, y) => Mathf.Max(Circ(x, y, 74, 24, 13), -Circ(x, y, 74, 24, 9)));
                    Fill(px, UIColors.Hex("#E2453C"), (x, y) => Seg(x, y, 66, 16, 82, 32, 2.2f));
                    break;
            }
        }

        static void DrawUpgrade(Color32[] px, UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Motor:
                    Fill(px, Outline, (x, y) => Mathf.Min(Box(x, y, 44, 60, 22, 20, 6), Box(x, y, 46, 28, 6, 18, 2)) - 3);
                    Fill(px, UIColors.Hex("#F28C28"), (x, y) => Box(x, y, 44, 60, 22, 20, 6));
                    Fill(px, UIColors.Hex("#55626E"), (x, y) => Box(x, y, 46, 28, 6, 18, 2));
                    Fill(px, UIColors.Hex("#D9DEE3"), (x, y) => Mathf.Min(Box(x, y, 56, 12, 14, 3, 1), Box(x, y, 36, 12, 4, 3, 1)));
                    Fill(px, UIColors.Hex("#FFD66B"), (x, y) => Seg(x, y, 74, 56, 88, 70, 3));
                    Fill(px, UIColors.Hex("#FFD66B"), (x, y) => Seg(x, y, 74, 48, 90, 48, 3));
                    break;
                case UpgradeType.Herramienta:
                    Fill(px, Outline, (x, y) => Seg(x, y, 16, 16, 62, 62, 8));
                    Fill(px, UIColors.Hex("#FFD66B"), (x, y) => Seg(x, y, 16, 16, 62, 62, 5));
                    Fill(px, Outline, (x, y) => Circ(x, y, 68, 68, 22));
                    Fill(px, UIColors.Hex("#2E7D63"), (x, y) => Circ(x, y, 68, 68, 19));
                    for (int k = -2; k <= 2; k++)
                    {
                        float o = k * 9;
                        Fill(px, UIColors.Hex("#8FE0BE"), (x, y) => Mathf.Max(Seg(x, y, 48 + o, 48, 88 + o, 88, 1.3f), Circ(x, y, 68, 68, 18)));
                        Fill(px, UIColors.Hex("#8FE0BE"), (x, y) => Mathf.Max(Seg(x, y, 88 + o, 48, 48 + o, 88, 1.3f), Circ(x, y, 68, 68, 18)));
                    }
                    break;
                case UpgradeType.Bodega:
                    Fill(px, Outline, (x, y) => Box(x, y, 48, 42, 38, 26, 5) - 3);
                    Fill(px, UIColors.Hex("#2F78C4"), (x, y) => Box(x, y, 48, 42, 38, 26, 5));
                    Fill(px, UIColors.Hex("#5FA3E8"), (x, y) => Box(x, y, 48, 60, 38, 8, 3));
                    Fill(px, UIColors.Hex("#FFFFFF"), (x, y) => Mathf.Min(Box(x, y, 48, 82, 4, 12, 1), Box(x, y, 48, 82, 12, 4, 1)));
                    break;
            }
        }

        static void DrawCoin(Color32[] px)
        {
            Fill(px, UIColors.Hex("#B8860B"), (x, y) => Circ(x, y, 48, 48, 42));
            Fill(px, UIColors.Hex("#F6C445"), (x, y) => Circ(x, y, 48, 48, 36));
            Fill(px, UIColors.Hex("#3E9E5C"), (x, y) =>
            {
                // Hoja estilizada (Eco)
                float a = Circ(x, y, 42, 52, 20), b = Circ(x, y, 58, 40, 20);
                return Mathf.Max(a, b);
            });
            Fill(px, UIColors.Hex("#2A7442"), (x, y) => Seg(x, y, 34, 32, 64, 62, 1.8f));
        }
    }
}
