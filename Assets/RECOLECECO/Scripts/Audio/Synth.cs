using UnityEngine;

namespace RecolecEco
{
    /// <summary>Síntesis de audio procedural (todo el sonido del juego es original).</summary>
    public static class Synth
    {
        const int SR = 44100;
        const int SRLow = 22050;
        static System.Random _rng = new System.Random(1234);

        static float Rnd() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] data, int sr)
        {
            var c = AudioClip.Create(name, data.Length, 1, sr, false);
            c.SetData(data, 0);
            return c;
        }

        static float Env(float t, float dur, float attack = 0.005f)
        {
            if (t < attack) return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / Mathf.Max(0.0001f, dur - attack));
        }

        public static AudioClip Blip(float freq, float dur, float vol)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                d[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Env(t, dur) * vol;
            }
            return Make("blip", d, SR);
        }

        public static AudioClip Arp(float[] notes, float step, float vol)
        {
            float dur = step * notes.Length + 0.35f;
            int n = (int)(SR * dur);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * step * SR);
                float nd = 0.4f;
                for (int i = 0; i < (int)(nd * SR) && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float e = Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.004f);
                    float s = Mathf.Sin(2 * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * notes[k] * t);
                    d[start + i] += s * e * vol * 0.7f;
                }
            }
            return Make("arp", d, SR);
        }

        public static AudioClip Sweep(float f0, float f1, float dur, float vol, bool mechanical)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(f0, f1, t / dur);
                ph += 2 * Mathf.PI * f / SR;
                float s = mechanical ? Mathf.Sign(Mathf.Sin(ph)) * 0.4f + Mathf.Sin(ph * 0.5f) * 0.6f : Mathf.Sin(ph);
                d[i] = s * Env(t, dur, 0.03f) * vol * 0.6f;
            }
            return Make("sweep", d, SR);
        }

        public static AudioClip Splash(float dur, float vol)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                lp = Mathf.Lerp(lp, Rnd(), 0.25f);
                d[i] = lp * Mathf.Exp(-t * 10f) * Mathf.Clamp01(t / 0.01f) * vol;
            }
            return Make("splash", d, SR);
        }

        public static AudioClip Thud(float dur, float vol)
        {
            int n = (int)(SR * dur);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                lp = Mathf.Lerp(lp, Rnd(), 0.05f);
                float tone = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(110, 60, t / dur) * t);
                d[i] = (tone * 0.8f + lp * 1.5f) * Mathf.Exp(-t * 12f) * vol;
            }
            return Make("thud", d, SR);
        }

        public static AudioClip Gull()
        {
            float dur = 1.0f;
            int n = (int)(SR * dur);
            var d = new float[n];
            float ph = 0;
            for (int call = 0; call < 3; call++)
            {
                int start = (int)(call * 0.28f * SR);
                float cd = 0.24f;
                for (int i = 0; i < (int)(cd * SR) && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float x = t / cd;
                    float f = 1500f + 900f * Mathf.Sin(x * Mathf.PI) - 500f * x;
                    ph += 2 * Mathf.PI * f / SR;
                    float s = Mathf.Sin(ph) + 0.35f * Mathf.Sin(ph * 2.01f) + 0.15f * Rnd();
                    d[start + i] += s * Mathf.Sin(x * Mathf.PI) * 0.25f;
                }
            }
            return Make("gull", d, SR);
        }

        /// <summary>Olas: ruido filtrado con oleaje lento. Bucle sin cortes.</summary>
        public static AudioClip Waves()
        {
            float dur = 12f;
            int n = (int)(SRLow * dur);
            var d = new float[n];
            float lp1 = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SRLow;
                float swell = 0.45f + 0.35f * Mathf.Sin(2 * Mathf.PI * t / 6f) + 0.2f * Mathf.Sin(2 * Mathf.PI * t / 4f + 1f);
                float r = Rnd();
                lp1 = Mathf.Lerp(lp1, r, 0.06f + 0.08f * swell);
                lp2 = Mathf.Lerp(lp2, lp1, 0.3f);
                d[i] = lp2 * swell * 1.6f;
            }
            d = Crossfade(d, (int)(SRLow * 1.0f));
            return Make("waves", d, SRLow);
        }

        public static AudioClip Motor()
        {
            float dur = 2f;
            int n = (int)(SR * dur);
            var d = new float[n];
            float f = 55f; // ciclos enteros en 2 s -> bucle perfecto
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float ph = 2 * Mathf.PI * f * t;
                float saw = 0;
                for (int h = 1; h <= 6; h++) saw += Mathf.Sin(ph * h) / h;
                float chug = 0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * 8f * t);
                lp = Mathf.Lerp(lp, Rnd(), 0.1f);
                d[i] = (saw * 0.35f * chug + lp * 0.25f) * 0.7f;
            }
            return Make("motor", d, SR);
        }

        static float[] Crossfade(float[] d, int fade)
        {
            int n = d.Length;
            for (int i = 0; i < fade; i++)
            {
                float a = i / (float)fade;
                d[i] = d[i] * a + d[n - fade + i] * (1 - a);
            }
            System.Array.Resize(ref d, n - fade);
            return d;
        }

        /// <summary>Música relajante original: acordes suaves + melodía pentatónica pulsada.</summary>
        public static AudioClip Music(int seed)
        {
            var rng = new System.Random(seed * 97);
            float bpm = seed == 1 ? 78f : 92f;
            float beat = 60f / bpm;
            int bars = 8;
            float dur = bars * 4 * beat;
            int n = (int)(SRLow * dur);
            var d = new float[n];

            // Progresión I - vi - IV - V en Re mayor (frecuencias de raíz)
            float[][] chords =
            {
                new[] { 146.83f, 185.00f, 220.00f },  // D
                new[] { 123.47f, 146.83f, 185.00f },  // Bm
                new[] { 196.00f, 246.94f, 293.66f },  // G
                new[] { 220.00f, 277.18f, 329.63f },  // A
            };
            float[] penta = { 587.33f, 659.25f, 739.99f, 880f, 987.77f, 1174.66f };

            for (int bar = 0; bar < bars; bar++)
            {
                var ch = chords[bar % 4];
                int s0 = (int)(bar * 4 * beat * SRLow);
                int len = (int)(4 * beat * SRLow);
                for (int i = 0; i < len && s0 + i < n; i++)
                {
                    float t = i / (float)SRLow;
                    float e = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((4 * beat - t) / 0.6f);
                    float s = 0;
                    foreach (var f in ch) s += Mathf.Sin(2 * Mathf.PI * f * t) + 0.2f * Mathf.Sin(4 * Mathf.PI * f * t);
                    float bass = Mathf.Sin(2 * Mathf.PI * ch[0] * 0.5f * t) * Mathf.Exp(-(t % beat) * 2.5f);
                    d[s0 + i] += (s * 0.05f + bass * 0.12f) * e;
                }
                // Melodía: notas en corcheas con silencios
                for (int k = 0; k < 8; k++)
                {
                    if (rng.NextDouble() < 0.35) continue;
                    float f = penta[rng.Next(penta.Length)];
                    int ns = s0 + (int)(k * beat * 0.5f * SRLow);
                    int nl = (int)(beat * 1.2f * SRLow);
                    for (int i = 0; i < nl && ns + i < n; i++)
                    {
                        float t = i / (float)SRLow;
                        float e = Mathf.Exp(-t * 4f) * Mathf.Clamp01(t / 0.005f);
                        d[ns + i] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.25f * Mathf.Sin(6 * Mathf.PI * f * t)) * e * 0.09f;
                    }
                }
            }
            // Suavizar inicio/fin para el bucle
            int fade = SRLow / 20;
            for (int i = 0; i < fade; i++) { float a = i / (float)fade; d[i] *= a; d[n - 1 - i] *= a; }
            return Make("music" + seed, d, SRLow);
        }
    }
}
