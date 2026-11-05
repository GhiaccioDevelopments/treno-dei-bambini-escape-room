using System;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Mattoni di sintesi per il paesaggio sonoro originale del gioco: strumenti (violino, mandolino, pianoforte, campane),
    /// rumore filtrato, riverbero e "patina da grammofono" applicati ai buffer (offline: nessun DSP in tempo reale, quindi
    /// funziona identico in WebGL). Tutto mono a 22,05 kHz.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 22050;

        public struct Note
        {
            public float midi;     // altezza (69 = La4)
            public float start;    // secondi
            public float dur;      // secondi
            public float vel;      // 0..1
            public float glideFrom; // midi di partenza del portamento (0 = nessuno)
        }

        /// <summary>Generatore pseudo-casuale deterministico (stesso suono a ogni avvio).</summary>
        public sealed class Rng
        {
            uint s;
            public Rng(uint seed) { s = seed == 0 ? 1u : seed; }
            public float Next() // 0..1
            {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                return (s & 0xFFFFFF) / 16777216f;
            }
            public float Signed() { return Next() * 2f - 1f; }
            public float Range(float a, float b) { return a + (b - a) * Next(); }
        }

        public static float Hz(float midi) { return 440f * Mathf.Pow(2f, (midi - 69f) / 12f); }
        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        public static float[] Buffer(float seconds) { return new float[Mathf.CeilToInt(seconds * Rate)]; }

        public static AudioClip ToClip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        // ------------------------------------------------------------ strumenti
        /// <summary>Violino: armoniche con formanti del corpo, vibrato che sboccia, archetto leggermente rumoroso, portamento opzionale.</summary>
        public static void Violin(float[] buf, Note n, float gain, Rng rng)
        {
            int i0 = Mathf.RoundToInt(n.start * Rate);
            float rel = 0.22f;
            int len = Mathf.CeilToInt((n.dur + rel) * Rate);
            float phase = 0f;
            float vibPhase = rng.Next() * 6.28f;
            float baseMidi = n.midi;
            for (int k = 0; k < len; k++)
            {
                int idx = i0 + k;
                if (idx < 0) continue;
                if (idx >= buf.Length) break;
                float t = k / (float)Rate;
                float env;
                float att = 0.075f;
                if (t < att) env = Smooth(t / att);
                else if (t < n.dur) env = 1f - 0.12f * ((t - att) / Mathf.Max(0.01f, n.dur - att)) * 0f;
                else env = Mathf.Exp(-(t - n.dur) / (rel * 0.35f));
                // leggera "messa di voce" sulle note lunghe
                env *= 1f + 0.08f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / Mathf.Max(0.05f, n.dur)));

                float m = baseMidi;
                if (n.glideFrom > 0f) m = Mathf.Lerp(n.glideFrom, baseMidi, Smooth(t / 0.11f));
                float vibDepth = 0.16f * Smooth((t - 0.18f) / 0.35f); // semitoni
                m += Mathf.Sin(vibPhase + t * 2f * Mathf.PI * 5.4f) * vibDepth;
                float f = Hz(m);
                phase += 2f * Mathf.PI * f / Rate;
                if (phase > 6.2831853f) phase -= 6.2831853f;

                float s = 0f;
                for (int h = 1; h <= 12; h++)
                {
                    float fr = f * h;
                    if (fr > 9000f) break;
                    float formant = 1f + 1.5f * Mathf.Exp(-Sq((fr - 460f) / 260f)) + 1.1f * Mathf.Exp(-Sq((fr - 1450f) / 650f)) + 0.7f * Mathf.Exp(-Sq((fr - 3200f) / 900f));
                    s += Mathf.Sin(h * phase) * formant / Mathf.Pow(h, 0.95f);
                }
                s *= 0.22f;
                s += rng.Signed() * 0.012f; // fruscio d'archetto
                buf[idx] += s * env * n.vel * gain;
            }
        }

        static float Sq(float x) { return x * x; }

        /// <summary>Corda pizzicata (mandolino, pizzicato, chitarra): armoniche che decadono più in fretta le più acute. tremolo = ripetizioni (mandolino).</summary>
        public static void Pluck(float[] buf, Note n, float gain, float brightness, float tremoloRate, Rng rng)
        {
            float step = tremoloRate > 0f ? 1f / tremoloRate : n.dur + 1f;
            int count = tremoloRate > 0f ? Mathf.Max(1, Mathf.FloorToInt(n.dur / step)) : 1;
            for (int r = 0; r < count; r++)
            {
                float t0 = n.start + r * step;
                float g = gain * (r == 0 ? 1f : 0.55f);
                int i0 = Mathf.RoundToInt(t0 * Rate);
                float f = Hz(n.midi);
                float decay = tremoloRate > 0f ? step * 0.9f : Mathf.Max(0.35f, n.dur);
                int len = Mathf.CeilToInt(Mathf.Min(decay * 3f, 4f) * Rate);
                for (int k = 0; k < len; k++)
                {
                    int idx = i0 + k;
                    if (idx < 0) continue;
                    if (idx >= buf.Length) break;
                    float t = k / (float)Rate;
                    float s = 0f;
                    for (int h = 1; h <= 8; h++)
                    {
                        float fr = f * h;
                        if (fr > 9000f) break;
                        float a = Mathf.Pow(brightness, h - 1) / h;
                        s += Mathf.Sin(2f * Mathf.PI * fr * t) * a * Mathf.Exp(-t * (2.2f + 1.6f * h) / decay);
                    }
                    float click = k < 40 ? rng.Signed() * 0.25f * (1f - k / 40f) : 0f;
                    buf[idx] += (s * 0.5f + click) * Mathf.Min(1f, t / 0.002f) * n.vel * g;
                }
            }
        }

        /// <summary>Pianoforte semplice (anche per "ricordo"): attacco secco, armoniche che decadono, lieve scordatura.</summary>
        public static void Piano(float[] buf, Note n, float gain, Rng rng)
        {
            int i0 = Mathf.RoundToInt(n.start * Rate);
            float f = Hz(n.midi);
            float life = Mathf.Clamp(n.dur * 1.6f, 0.8f, 3.2f);
            int len = Mathf.CeilToInt(life * Rate);
            float detune = 1f + rng.Signed() * 0.0007f;
            for (int k = 0; k < len; k++)
            {
                int idx = i0 + k;
                if (idx < 0) continue;
                if (idx >= buf.Length) break;
                float t = k / (float)Rate;
                float s = 0f;
                for (int h = 1; h <= 6; h++)
                {
                    float fr = f * h * detune;
                    if (fr > 8000f) break;
                    s += Mathf.Sin(2f * Mathf.PI * fr * t) / (h * h * 0.7f + 0.3f) * Mathf.Exp(-t * (1.1f + 0.7f * h));
                }
                float hammer = k < 60 ? rng.Signed() * 0.15f * (1f - k / 60f) : 0f;
                float rel = t > n.dur ? Mathf.Exp(-(t - n.dur) / 0.12f) : 1f;
                buf[idx] += (s * 0.55f + hammer) * Mathf.Min(1f, t / 0.003f) * rel * n.vel * gain;
            }
        }

        /// <summary>Campanella/celesta: parziali non armoniche con decadimenti lunghi.</summary>
        public static void Bell(float[] buf, float start, float midi, float gain, float life)
        {
            int i0 = Mathf.RoundToInt(start * Rate);
            float f = Hz(midi);
            int len = Mathf.CeilToInt(life * Rate);
            float[] ratio = { 1f, 2.01f, 2.76f, 4.07f, 5.4f };
            float[] amp = { 1f, 0.5f, 0.35f, 0.18f, 0.1f };
            for (int k = 0; k < len; k++)
            {
                int idx = i0 + k;
                if (idx < 0) continue;
                if (idx >= buf.Length) break;
                float t = k / (float)Rate;
                float s = 0f;
                for (int p = 0; p < 5; p++)
                {
                    float fr = f * ratio[p];
                    if (fr > 9500f) break;
                    s += Mathf.Sin(2f * Mathf.PI * fr * t) * amp[p] * Mathf.Exp(-t * (1.6f + p * 1.3f) / life * 3f);
                }
                buf[idx] += s * 0.4f * Mathf.Min(1f, t / 0.0015f) * gain;
            }
        }

        // ------------------------------------------------------------ rumore e filtri
        public static float[] Noise(float seconds, Rng rng, float gain = 1f)
        {
            var b = Buffer(seconds);
            for (int i = 0; i < b.Length; i++) b[i] = rng.Signed() * gain;
            return b;
        }

        public static void LowPass(float[] x, float fc)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / Rate);
            float y = 0f;
            for (int i = 0; i < x.Length; i++) { y += a * (x[i] - y); x[i] = y; }
        }

        public static void HighPass(float[] x, float fc)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / Rate);
            float y = 0f;
            for (int i = 0; i < x.Length; i++) { y += a * (x[i] - y); x[i] -= y; }
        }

        public static void BandPass(float[] x, float lo, float hi) { HighPass(x, lo); LowPass(x, hi); }

        public static void Scale(float[] x, float g) { for (int i = 0; i < x.Length; i++) x[i] *= g; }

        public static void Add(float[] dst, float[] src, float atSeconds, float gain)
        {
            int o = Mathf.RoundToInt(atSeconds * Rate);
            for (int i = 0; i < src.Length; i++)
            {
                int j = o + i;
                if (j < 0) continue;
                if (j >= dst.Length) break;
                dst[j] += src[i] * gain;
            }
        }

        public static void Envelope(float[] x, Func<float, float> env)
        {
            for (int i = 0; i < x.Length; i++) x[i] *= env(i / (float)Rate);
        }

        public static void Normalize(float[] x, float peak)
        {
            float m = 0f;
            for (int i = 0; i < x.Length; i++) { float a = Mathf.Abs(x[i]); if (a > m) m = a; }
            if (m < 1e-5f) return;
            Scale(x, peak / m);
        }

        public static void FadeEdges(float[] x, float fadeIn, float fadeOut)
        {
            int a = Mathf.RoundToInt(fadeIn * Rate), b = Mathf.RoundToInt(fadeOut * Rate);
            for (int i = 0; i < a && i < x.Length; i++) x[i] *= i / (float)a;
            for (int i = 0; i < b && i < x.Length; i++) x[x.Length - 1 - i] *= i / (float)b;
        }

        /// <summary>Rende un buffer un loop senza clic: la coda si dissolve nell'inizio (equal power). Restituisce un buffer più corto di "fade".</summary>
        public static float[] MakeLoop(float[] x, float fadeSeconds)
        {
            int fade = Mathf.Min(Mathf.RoundToInt(fadeSeconds * Rate), x.Length / 3);
            var o = new float[x.Length - fade];
            Array.Copy(x, o, o.Length);
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                float head = Mathf.Sin(w * Mathf.PI * 0.5f), tail = Mathf.Cos(w * Mathf.PI * 0.5f);
                o[i] = x[i] * head + x[o.Length + i] * tail;
            }
            return o;
        }

        // ------------------------------------------------------------ effetti sui buffer
        /// <summary>Riverbero di Schroeder (4 comb + 2 allpass). Funziona in-place; wet 0..1; room scala le durate.</summary>
        public static void Reverb(float[] x, float wet, float room)
        {
            float[] times = { 0.0297f, 0.0371f, 0.0411f, 0.0437f };
            var combs = new float[4][];
            var pos = new int[4];
            for (int c = 0; c < 4; c++) combs[c] = new float[Mathf.Max(8, Mathf.RoundToInt(times[c] * room * Rate))];
            float fb = Mathf.Clamp(0.70f + 0.07f * room, 0.7f, 0.9f);
            var lp = new float[4];
            var ap1 = new float[Mathf.Max(4, Mathf.RoundToInt(0.005f * Rate))];
            var ap2 = new float[Mathf.Max(4, Mathf.RoundToInt(0.0017f * Rate))];
            int p1 = 0, p2 = 0;
            for (int i = 0; i < x.Length; i++)
            {
                float inp = x[i];
                float sum = 0f;
                for (int c = 0; c < 4; c++)
                {
                    float d = combs[c][pos[c]];
                    lp[c] += 0.4f * (d - lp[c]); // smorza le alte frequenze nella coda
                    combs[c][pos[c]] = inp * 0.3f + lp[c] * fb;
                    sum += d;
                    if (++pos[c] >= combs[c].Length) pos[c] = 0;
                }
                float o1 = AllPass(ap1, ref p1, sum * 0.5f);
                float o2 = AllPass(ap2, ref p2, o1);
                x[i] = inp * (1f - wet * 0.5f) + o2 * wet * 0.9f;
            }
        }

        static float AllPass(float[] buf, ref int p, float x)
        {
            float d = buf[p];
            float y = -x + d;
            buf[p] = x + d * 0.5f;
            if (++p >= buf.Length) p = 0;
            return y;
        }

        /// <summary>"Patina da grammofono" applicata al file: taglio di bassi e alti, fruscio e crepitii discreti, lieve saturazione.</summary>
        public static void Patina(float[] x, Rng rng, float amount)
        {
            HighPass(x, 170f);
            LowPass(x, 4200f);
            float hiss = 0.0045f * amount;
            float click = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                if (rng.Next() < 9f * amount / Rate * 1.0f) click = rng.Range(0.02f, 0.08f) * amount * (rng.Next() > 0.5f ? 1f : -1f);
                click *= 0.92f;
                float s = x[i] + rng.Signed() * hiss + click;
                x[i] = (float)Math.Tanh(s * 1.15f) * 0.9f;
            }
        }
    }
}
