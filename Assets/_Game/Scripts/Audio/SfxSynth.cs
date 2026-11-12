using System;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Effetti, ambienti e suoni sparsi originali, sintetizzati da codice (nessun file esterno, nessuna licenza da gestire).
    /// Ogni id può essere sostituito da una registrazione vera in Assets/Audio/{Effetti,Ambienti}: la AudioLibrary ha la precedenza.
    /// Gli id con varianti terminano con _1, _2, ... (come i file passo_pietra_01, passo_pietra_02 della libreria).
    /// </summary>
    public static class SfxSynth
    {
        const int R = Synth.Rate;

        public static readonly string[] AllIds =
        {
            "passo_pietra_1", "passo_pietra_2", "passo_pietra_3", "passo_pietra_4",
            "passo_legno_1", "passo_legno_2", "passo_legno_3", "passo_legno_4",
            "passo_ovattato_1", "passo_ovattato_2", "passo_ovattato_3", "passo_ovattato_4",
            "porta_apri", "porta_chiudi", "porta_chiusa", "raccolta", "trovato", "ricordo_brilla",
            "proiettore_avvio", "proiettore_stop", "violino_sale", "treno_fischio",
            "campane", "gabbiani", "carretto", "venditore", "bambini", "pendolo", "sedia", "pentola", "panni", "finestra",
            "vicolo_brusio", "vento_mare", "interno_silenzio", "ricordo_tono", "stazione_vapore", "pioggia", "pioggia_ovattata"
        };

        public static bool Has(string id) { return Array.IndexOf(AllIds, id) >= 0; }

        /// <summary>Quante varianti sintetiche esistono per un id base (passo_pietra -> 4). 1 se non ha varianti.</summary>
        public static int VariantCount(string baseId)
        {
            int n = 0;
            while (Has(baseId + "_" + (n + 1))) n++;
            return n > 0 ? n : 1;
        }

        static float[] Fill(float seconds, Func<float, float> f)
        {
            var b = Synth.Buffer(seconds);
            for (int i = 0; i < b.Length; i++) b[i] = f(i / (float)R);
            return b;
        }

        static float Env(float t, float a, float d) { return t < a ? t / a : Mathf.Exp(-(t - a) / d); }
        static float Sin(float t, float f) { return Mathf.Sin(2f * Mathf.PI * f * t); }

        public static AudioClip Make(string id)
        {
            var b = Build(id);
            return b == null ? null : Synth.ToClip("sfx_" + id, b);
        }

        /// <summary>Seme stabile (FNV-1a): string.GetHashCode non è deterministico tra piattaforme/versioni, e i file pre-generati devono coincidere con la sintesi di ripiego.</summary>
        static uint SeedOf(string id)
        {
            uint h = 2166136261u;
            for (int i = 0; i < id.Length; i++) { h ^= id[i]; h *= 16777619u; }
            return h | 1u;
        }

        /// <summary>Campioni dell'effetto (mono, Synth.Rate), senza AudioClip: usata anche dallo strumento che pre-genera i file.</summary>
        public static float[] BuildSamples(string id) { return Build(id); }

        static float[] Build(string id)
        {
            var rng = new Synth.Rng(SeedOf(id));
            float[] x;
            switch (id)
            {
                // ----------------------------------------------------- passi (4 varianti per superficie)
                case "passo_pietra_1": case "passo_pietra_2": case "passo_pietra_3": case "passo_pietra_4":
                    {
                        int v = id[id.Length - 1] - '0';
                        x = Synth.Noise(0.22f, rng, 1f);
                        Synth.LowPass(x, 1500f + v * 260f);
                        Synth.Envelope(x, t => Mathf.Exp(-t / (0.03f + v * 0.004f)));
                        var thump = Fill(0.22f, t => Sin(t, 110f + v * 9f) * Mathf.Exp(-t / 0.05f) * 0.55f);
                        Synth.Add(x, thump, 0f, 1f);
                        var tick = Fill(0.02f, t => rng.Signed() * Mathf.Exp(-t / 0.002f) * 0.35f);
                        Synth.Add(x, tick, 0f, 1f);
                        Synth.Scale(x, 0.7f);
                        return x;
                    }
                case "passo_legno_1": case "passo_legno_2": case "passo_legno_3": case "passo_legno_4":
                    {
                        int v = id[id.Length - 1] - '0';
                        float f0 = 150f + v * 14f;
                        x = Fill(0.3f, t => Sin(t, f0) * Mathf.Exp(-t / 0.07f) * 0.6f + Sin(t, f0 * 2.1f) * Mathf.Exp(-t / 0.1f) * 0.22f);
                        var n = Synth.Noise(0.3f, rng, 0.5f);
                        Synth.LowPass(n, 900f);
                        Synth.Envelope(n, t => Mathf.Exp(-t / 0.02f));
                        Synth.Add(x, n, 0f, 0.8f);
                        if (v == 3) Synth.Add(x, Fill(0.2f, t => Sin(t, 420f + t * 300f) * Env(t, 0.02f, 0.05f) * 0.12f), 0.05f, 1f); // scricchiolio
                        return x;
                    }
                case "passo_ovattato_1": case "passo_ovattato_2": case "passo_ovattato_3": case "passo_ovattato_4":
                    {
                        int v = id[id.Length - 1] - '0';
                        x = Synth.Noise(0.3f, rng, 1f);
                        Synth.LowPass(x, 420f + v * 40f);
                        Synth.Envelope(x, t => Env(t, 0.012f, 0.06f));
                        Synth.Add(x, Fill(0.3f, t => Sin(t, 80f) * Env(t, 0.01f, 0.07f) * 0.4f), 0f, 1f);
                        Synth.Scale(x, 0.6f);
                        return x;
                    }

                // ----------------------------------------------------- porte e oggetti
                case "porta_apri":
                    {
                        x = Fill(1.0f, t =>
                        {
                            float f = 170f + 150f * Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI * 0.8f) + 8f * Mathf.Sin(t * 40f);
                            float s = 0f;
                            for (int h = 1; h <= 6; h++) s += Sin(t, f * h) / h * (h == 2 || h == 3 ? 1.6f : 1f);
                            float amp = Mathf.Clamp01(t / 0.06f) * Mathf.Clamp01((0.8f - t) / 0.15f) * (0.65f + 0.35f * Mathf.Sin(t * 23f));
                            return s * amp * 0.16f;
                        });
                        Synth.LowPass(x, 2400f);
                        Synth.Add(x, Fill(0.05f, t => rng.Signed() * Mathf.Exp(-t / 0.004f) * 0.5f), 0f, 1f); // scatto del chiavistello
                        return x;
                    }
                case "porta_chiudi":
                    {
                        x = Fill(0.55f, t => Sin(t, 78f) * Mathf.Exp(-t / 0.12f) * 0.8f);
                        var n = Synth.Noise(0.55f, rng, 0.8f);
                        Synth.LowPass(n, 700f);
                        Synth.Envelope(n, t => Mathf.Exp(-t / 0.05f));
                        Synth.Add(x, n, 0f, 1f);
                        Synth.Add(x, Fill(0.04f, t => rng.Signed() * Mathf.Exp(-t / 0.003f) * 0.45f), 0.05f, 1f);
                        return x;
                    }
                case "porta_chiusa":
                    {
                        x = Synth.Buffer(0.6f);
                        float[] at = { 0f, 0.11f, 0.2f };
                        for (int i = 0; i < at.Length; i++)
                        {
                            var c = Synth.Noise(0.05f, rng, 1f);
                            Synth.BandPass(c, 900f, 2600f);
                            Synth.Envelope(c, t => Mathf.Exp(-t / 0.008f));
                            Synth.Add(x, c, at[i], 0.8f);
                        }
                        Synth.Add(x, Fill(0.3f, t => Sin(t, 95f) * Mathf.Exp(-t / 0.07f) * 0.5f), 0.22f, 1f);
                        return x;
                    }
                case "raccolta":
                    {
                        x = Synth.Noise(0.5f, rng, 0.7f);
                        Synth.BandPass(x, 1200f, 5000f);
                        Synth.Envelope(x, t => Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-t / 0.07f) * (0.6f + 0.4f * Sin(t, 28f)));
                        Synth.Add(x, Fill(0.5f, t => (Sin(t, 2637f) * 0.12f + Sin(t, 3520f) * 0.1f) * Env(t, 0.002f, 0.12f)), 0.06f, 1f);
                        return x;
                    }
                case "trovato":
                    {
                        x = Synth.Buffer(1.4f);
                        Synth.Bell(x, 0f, 81f, 0.8f, 1.3f);
                        Synth.Bell(x, 0.12f, 88f, 0.5f, 1.2f);
                        return x;
                    }
                case "ricordo_brilla":
                    {
                        x = Fill(3f, t => (Sin(t, 1760f) * 0.5f + Sin(t, 2349f) * 0.35f + Sin(t, 1174.7f) * 0.4f) * (0.5f + 0.5f * Sin(t, 0.33f)) * 0.06f);
                        return Synth.MakeLoop(Fill(3.5f, t => (Sin(t, 1760f) * 0.5f + Sin(t, 2349f) * 0.35f + Sin(t, 1174.7f) * 0.4f) * (0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t * 0.5714f)) * 0.06f), 0.5f);
                    }
                case "proiettore_avvio":
                    {
                        x = Fill(1.4f, t =>
                        {
                            float rate = Mathf.Lerp(3f, 24f, Mathf.Clamp01(t / 1.1f));
                            float tick = Mathf.Exp(-((t * rate) % 1f) * 14f);
                            return (rng.Signed() * 0.18f * tick + Sin(t, 48f * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(t / 1.1f))) * 0.07f) * Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1.4f - t) / 0.3f);
                        });
                        Synth.BandPass(x, 120f, 4500f);
                        return x;
                    }
                case "proiettore_stop":
                    {
                        x = Fill(1.0f, t =>
                        {
                            float p = Mathf.Clamp01(t / 0.8f);
                            float rate = Mathf.Lerp(24f, 2f, p * p);
                            float tick = Mathf.Exp(-((t * rate) % 1f) * 14f);
                            return (rng.Signed() * 0.18f * tick + Sin(t, 48f * Mathf.Lerp(1f, 0.3f, p)) * 0.07f) * (1f - p);
                        });
                        Synth.BandPass(x, 120f, 4500f);
                        return x;
                    }
                case "violino_sale":
                    {
                        // accordo di violino che sale, per l'entrata nel ricordo
                        x = Synth.Buffer(3.4f);
                        int[] ch = { 57, 64, 69 };
                        for (int i = 0; i < ch.Length; i++)
                            Synth.Violin(x, new Synth.Note { midi = ch[i] + 12, start = 0.1f + i * 0.18f, dur = 2.2f, vel = 0.6f, glideFrom = ch[i] + 7 }, 1f, rng);
                        Synth.Reverb(x, 0.5f, 2.2f);
                        Synth.Normalize(x, 0.6f);
                        Synth.FadeEdges(x, 0.01f, 0.6f);
                        return x;
                    }
                case "treno_fischio":
                    {
                        x = Fill(3.2f, t =>
                        {
                            float s = (Sin(t, 440f) + Sin(t, 554f)) * 0.5f + Sin(t, 880f) * 0.1f;
                            float amp = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((3.0f - t) / 0.9f);
                            return s * amp * 0.28f * (1f + 0.04f * Sin(t, 6f));
                        });
                        var br = Synth.Noise(3.2f, rng, 0.04f);
                        Synth.BandPass(br, 600f, 2500f);
                        Synth.Add(x, br, 0f, 1f);
                        Synth.LowPass(x, 2200f); // lontano
                        Synth.Reverb(x, 0.5f, 2.6f);
                        Synth.Scale(x, 0.7f);
                        return x;
                    }

                // ----------------------------------------------------- suoni sparsi
                case "campane":
                    {
                        x = Synth.Buffer(8f);
                        float[] at = { 0.1f, 1.9f, 3.7f, 5.5f };
                        float[] pitch = { 50f, 47f, 50f, 45f };
                        for (int i = 0; i < at.Length; i++) Synth.Bell(x, at[i], pitch[i], 0.9f, 3.2f);
                        Synth.LowPass(x, 3000f);
                        Synth.Reverb(x, 0.45f, 2.8f);
                        Synth.Normalize(x, 0.5f);
                        Synth.FadeEdges(x, 0.01f, 0.8f);
                        return x;
                    }
                case "gabbiani":
                    {
                        x = Synth.Buffer(3.2f);
                        float[] at = { 0.1f, 1.0f, 1.6f };
                        for (int i = 0; i < at.Length; i++)
                        {
                            float start = at[i]; float len = 0.55f + i * 0.1f;
                            var c = Fill(len, t =>
                            {
                                float p = t / len;
                                float f = 1500f + 900f * Mathf.Sin(p * Mathf.PI) + 250f * Sin(t, 38f);
                                float s = Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(6f * Mathf.PI * f * t);
                                return s * Mathf.Sin(p * Mathf.PI) * 0.18f;
                            });
                            Synth.Add(x, c, start, 1f);
                        }
                        Synth.LowPass(x, 5200f);
                        Synth.Reverb(x, 0.3f, 1.8f);
                        Synth.Scale(x, 0.7f);
                        return x;
                    }
                case "carretto":
                    {
                        x = Synth.Noise(4f, rng, 0.5f);
                        Synth.LowPass(x, 260f);
                        Synth.Envelope(x, t => (0.55f + 0.45f * Mathf.Abs(Sin(t, 3.4f))) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 4f)));
                        for (int i = 0; i < 12; i++)
                        {
                            var hoof = Fill(0.09f, t => Sin(t, 540f) * Mathf.Exp(-t / 0.012f) * 0.4f + rng.Signed() * Mathf.Exp(-t / 0.004f) * 0.25f);
                            Synth.Add(x, hoof, 0.3f + i * 0.31f + (i % 2) * 0.05f, 0.55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((0.3f + i * 0.31f) / 4f)));
                        }
                        Synth.Normalize(x, 0.45f);
                        return x;
                    }
                case "venditore":
                    {
                        // vocalizzo senza parole (richiamo): vocale che scorre da "a" a "o", altezza che sale e scende
                        float len = 2.6f;
                        float ph = 0f;
                        x = Fill(len, t =>
                        {
                            float p = t / len;
                            float f0 = 150f + 70f * Mathf.Sin(p * Mathf.PI) + 12f * Sin(t, 5.5f);
                            ph += 2f * Mathf.PI * f0 / R;
                            float f1 = Mathf.Lerp(780f, 520f, p), f2 = Mathf.Lerp(1200f, 880f, p);
                            float s = 0f;
                            for (int h = 1; h <= 22; h++)
                            {
                                float fr = f0 * h;
                                float g = Mathf.Exp(-Sq((fr - f1) / 160f)) + 0.6f * Mathf.Exp(-Sq((fr - f2) / 220f)) + 0.05f;
                                s += Mathf.Sin(h * ph) * g / h;
                            }
                            float amp = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((len - t) / 0.3f) * (0.7f + 0.3f * Mathf.Sin(p * Mathf.PI * 3f));
                            return s * amp * 0.5f;
                        });
                        Synth.LowPass(x, 3200f);
                        Synth.Reverb(x, 0.35f, 2f);
                        Synth.Normalize(x, 0.35f); // lontano e discreto
                        return x;
                    }
                case "bambini":
                    {
                        x = Synth.Buffer(3.4f);
                        for (int i = 0; i < 9; i++)
                        {
                            float st = rng.Range(0f, 2.8f); float len = rng.Range(0.12f, 0.3f); float f0 = rng.Range(420f, 760f);
                            float f1 = rng.Range(700f, 1100f);
                            float ph = 0f;
                            var c = Fill(len, t =>
                            {
                                float p = t / len;
                                float f = f0 * (1f + 0.25f * Mathf.Sin(p * Mathf.PI));
                                ph += 2f * Mathf.PI * f / R;
                                float s = 0f;
                                for (int h = 1; h <= 10; h++)
                                    s += Mathf.Sin(h * ph) / h * (Mathf.Exp(-Sq((f * h - f1) / 300f)) + 0.1f);
                                return s * Mathf.Sin(p * Mathf.PI) * 0.35f;
                            });
                            Synth.Add(x, c, st, 1f);
                        }
                        Synth.LowPass(x, 3800f);
                        Synth.Reverb(x, 0.45f, 2.4f);
                        Synth.Normalize(x, 0.3f); // molto lontano
                        return x;
                    }
                case "pendolo":
                    {
                        x = Synth.Buffer(4.2f);
                        for (int i = 0; i < 8; i++)
                        {
                            float f = (i % 2 == 0) ? 1750f : 1350f;
                            Synth.Add(x, Fill(0.06f, t => (Sin(t, f) * 0.4f + rng.Signed() * 0.25f) * Mathf.Exp(-t / 0.008f)), 0.1f + i * 0.5f, 0.5f);
                        }
                        Synth.BandPass(x, 600f, 3500f);
                        Synth.Reverb(x, 0.25f, 1.2f);
                        return x;
                    }
                case "sedia":
                    {
                        x = Fill(0.7f, t =>
                        {
                            float f = 260f + 140f * Mathf.Sin(t * 5f) + 120f * t;
                            float s = Sin(t, f) + 0.6f * Sin(t, f * 2f) + 0.3f * Sin(t, f * 3f);
                            return s * Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((0.7f - t) / 0.2f) * (0.5f + 0.5f * Sin(t, 17f)) * 0.14f;
                        });
                        Synth.LowPass(x, 1800f);
                        return x;
                    }
                case "pentola":
                    {
                        x = Synth.Noise(2.6f, rng, 0.5f);
                        Synth.BandPass(x, 400f, 2500f);
                        Synth.Envelope(x, t => 0.08f * (0.5f + 0.5f * Mathf.Abs(Sin(t, 9.3f))));
                        for (int i = 0; i < 7; i++)
                            Synth.Add(x, Fill(0.1f, t => Sin(t, rng.Range(280f, 520f) + 900f * t) * Mathf.Exp(-t / 0.015f) * 0.35f), rng.Range(0.1f, 2.4f), 0.5f);
                        Synth.Add(x, Fill(0.6f, t => (Sin(t, 1850f) * 0.3f + Sin(t, 2480f) * 0.2f) * Mathf.Exp(-t / 0.12f)), 0.4f, 0.8f);
                        return x;
                    }
                case "panni":
                    {
                        x = Synth.Noise(3.2f, rng, 1f);
                        Synth.BandPass(x, 300f, 2200f);
                        Synth.Envelope(x, t => 0.16f * Mathf.Abs(Sin(t, 1.3f)) * (0.5f + 0.5f * Mathf.Abs(Sin(t, 6.1f))) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 3.2f)));
                        return x;
                    }
                case "finestra":
                    {
                        x = Fill(0.4f, t => Sin(t, 210f) * Mathf.Exp(-t / 0.05f) * 0.5f);
                        Synth.Add(x, Fill(0.05f, t => rng.Signed() * Mathf.Exp(-t / 0.004f) * 0.5f), 0f, 1f);
                        Synth.Add(x, Fill(0.2f, t => Sin(t, 1600f) * Mathf.Exp(-t / 0.03f) * 0.2f), 0.08f, 1f);
                        Synth.Reverb(x, 0.3f, 1.5f);
                        return x;
                    }

                // ----------------------------------------------------- ambienti (loop senza clic)
                case "vicolo_brusio":
                    {
                        x = Synth.Noise(14f, rng, 1f);
                        Synth.BandPass(x, 180f, 1400f);
                        Synth.Envelope(x, t => 0.12f * (0.7f + 0.3f * Sin(t, 0.21f)));
                        var mur = Synth.Noise(14f, rng, 1f);
                        Synth.BandPass(mur, 500f, 1100f);
                        Synth.Envelope(mur, t => 0.1f * Mathf.Clamp01(Sin(t, 0.5f) * Sin(t, 0.77f) * 2f + 0.2f) * (0.5f + 0.5f * Mathf.Abs(Sin(t, 3.7f))));
                        Synth.Add(x, mur, 0f, 1f);
                        return Synth.MakeLoop(x, 2f);
                    }
                case "vento_mare":
                    {
                        x = Synth.Noise(16f, rng, 1f);
                        Synth.LowPass(x, 700f);
                        Synth.Envelope(x, t => 0.16f * (0.55f + 0.45f * Sin(t, 0.11f)));
                        var wave = Synth.Noise(16f, rng, 1f);
                        Synth.BandPass(wave, 250f, 1800f);
                        Synth.Envelope(wave, t => 0.1f * Mathf.Pow(0.5f + 0.5f * Sin(t, 0.14f), 2f));
                        Synth.Add(x, wave, 0f, 1f);
                        return Synth.MakeLoop(x, 2.5f);
                    }
                case "interno_silenzio":
                    {
                        x = Synth.Noise(10f, rng, 1f);
                        Synth.LowPass(x, 260f);
                        Synth.Envelope(x, t => 0.06f * (0.8f + 0.2f * Sin(t, 0.17f)));
                        var street = Synth.Noise(10f, rng, 1f);
                        Synth.BandPass(street, 200f, 900f);
                        Synth.Scale(street, 0.02f);
                        Synth.Add(x, street, 0f, 1f);
                        return Synth.MakeLoop(x, 1.5f);
                    }
                case "pioggia":
                    {   // R007 t4: pioggia costante: sibilo diffuso + scroscio piu' basso, con lenta variazione di intensita'
                        x = Synth.Noise(14f, rng, 1f);
                        Synth.BandPass(x, 1800f, 9000f);
                        Synth.Envelope(x, t => 0.15f * (0.85f + 0.15f * Sin(t, 0.13f)));
                        var scroscio = Synth.Noise(14f, rng, 1f);
                        Synth.BandPass(scroscio, 400f, 1600f);
                        Synth.Envelope(scroscio, t => 0.07f * (0.7f + 0.3f * Sin(t, 0.31f)));
                        Synth.Add(x, scroscio, 0f, 1f);
                        for (int i = 0; i < 60; i++)
                            Synth.Add(x, Fill(0.03f, t => rng.Signed() * Mathf.Exp(-t / 0.004f) * 0.18f), rng.Range(0f, 13.5f), 1f);
                        return Synth.MakeLoop(x, 2f);
                    }
                case "pioggia_ovattata":
                    {   // la pioggia sentita da dentro: tutto sotto i 900 Hz, piu' piano, con qualche goccia sul tetto
                        x = Synth.Noise(14f, rng, 1f);
                        Synth.LowPass(x, 900f);
                        Synth.Envelope(x, t => 0.11f * (0.85f + 0.15f * Sin(t, 0.11f)));
                        for (int i = 0; i < 18; i++)
                            Synth.Add(x, Fill(0.05f, t => Sin(t, 180f) * Mathf.Exp(-t / 0.012f) * 0.12f), rng.Range(0f, 13.5f), 1f);
                        return Synth.MakeLoop(x, 2f);
                    }
                case "ricordo_tono":
                    {
                        x = Fill(14f, t => (Sin(t, 110f) * 0.5f + Sin(t, 110.7f) * 0.5f + Sin(t, 164.8f) * 0.28f + Sin(t, 220.3f) * 0.18f) * (0.7f + 0.3f * Sin(t, 0.09f)) * 0.07f);
                        var air = Synth.Noise(14f, rng, 1f);
                        Synth.LowPass(air, 500f);
                        Synth.Scale(air, 0.025f);
                        Synth.Add(x, air, 0f, 1f);
                        return Synth.MakeLoop(x, 2f);
                    }
                case "stazione_vapore":
                    {
                        x = Synth.Noise(14f, rng, 1f);
                        Synth.BandPass(x, 1500f, 6000f);
                        Synth.Envelope(x, t => 0.05f * Mathf.Pow(0.5f + 0.5f * Sin(t, 0.18f), 1.5f) + 0.012f);
                        var rumble = Synth.Noise(14f, rng, 1f);
                        Synth.LowPass(rumble, 180f);
                        Synth.Scale(rumble, 0.05f);
                        Synth.Add(x, rumble, 0f, 1f);
                        return Synth.MakeLoop(x, 2f);
                    }
            }
            return null;
        }

        static float Sq(float x) { return x * x; }
    }
}
