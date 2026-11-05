using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.UI.Fx
{
    public enum Sfx
    {
        Hover, ClickBrass, ClickPaper, ClickSeal, Stamp, Punch, TypeKey, Bell,
        SuitcaseOpen, SuitcaseClose, Whoosh, Chime, SoftError, Tick, Projector, Sparkle, TagDrop
    }

    /// <summary>
    /// Suoni dell'interfaccia sintetizzati da codice (nessun file audio, nessun peso nella build, nessuna licenza).
    /// Clip generate alla prima richiesta e messe in cache; riproduzione su un piccolo pool di AudioSource.
    /// Il volume segue AudioListener.volume (slider globale) e, se presente, il canale effetti di R002 (vedi Volume).
    /// </summary>
    public class UISfx : MonoBehaviour
    {
        const int Rate = 22050;
        static UISfx inst;
        static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        AudioSource[] sources;
        int next;

        /// <summary>Moltiplicatore aggiuntivo (0..1) per i suoni UI. Impostato dal sistema audio a canali.</summary>
        public static float Volume = 1f;

        static void Ensure()
        {
            if (inst != null) return;
            var go = new GameObject("UISfx") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            inst = go.AddComponent<UISfx>();
            inst.sources = new AudioSource[6];
            for (int i = 0; i < inst.sources.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                inst.sources[i] = s;
            }
        }

        void OnDestroy() { if (inst == this) inst = null; }

        public static void Play(Sfx id, float vol = 1f, float pitchVar = 0.05f)
        {
            if (Application.isBatchMode) return;
            Ensure();
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip) || clip == null) { clip = Build(id); clips[id] = clip; }
            if (clip == null) return;
            var s = inst.sources[inst.next];
            inst.next = (inst.next + 1) % inst.sources.Length;
            s.pitch = 1f + UnityEngine.Random.Range(-pitchVar, pitchVar);
            s.volume = Mathf.Clamp01(vol * Volume);
            s.clip = clip;
            s.Play();
        }

        /// <summary>Clip di un effetto UI (sintetizzata alla prima richiesta). Usata da AudioLibrary per gli id ui_*.</summary>
        public static AudioClip GetClip(Sfx id)
        {
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip) || clip == null) { clip = Build(id); clips[id] = clip; }
            return clip;
        }

        // ------------------------------------------------------------ sintesi
        static AudioClip Gen(string name, float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var c = AudioClip.Create("ui_" + name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        // rumore bianco deterministico per campione
        static float Nz(float t, int seed)
        {
            unchecked
            {
                uint h = (uint)(t * Rate) * 2654435761u + (uint)seed * 97u;
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                return (h & 0xFFFF) / 32768f - 1f;
            }
        }

        static float Env(float t, float a, float d) { return t < a ? t / a : Mathf.Exp(-(t - a) / d); }
        static float Sin(float t, float f) { return Mathf.Sin(2f * Mathf.PI * f * t); }

        static AudioClip Build(Sfx id)
        {
            switch (id)
            {
                case Sfx.Hover:
                    return Gen("hover", 0.09f, t => Nz(t, 1) * 0.10f * Env(t, 0.01f, 0.025f) + Sin(t, 1900f) * 0.015f * Env(t, 0.004f, 0.03f));
                case Sfx.ClickBrass:
                    return Gen("brass", 0.32f, t => (Sin(t, 1320f) * 0.28f + Sin(t, 1980f) * 0.16f + Sin(t, 3150f) * 0.08f) * Env(t, 0.002f, 0.07f)
                        + Nz(t, 2) * 0.16f * Env(t, 0.001f, 0.01f));
                case Sfx.ClickPaper:
                    return Gen("paper", 0.16f, t => Nz(t, 3) * (0.22f * Env(t, 0.012f, 0.03f)) * (0.6f + 0.4f * Sin(t, 60f)));
                case Sfx.ClickSeal:
                    return Gen("seal", 0.26f, t => Sin(t, 95f - t * 120f) * 0.62f * Env(t, 0.002f, 0.07f) + Nz(t, 4) * 0.12f * Env(t, 0.001f, 0.012f));
                case Sfx.Stamp:
                    return Gen("stamp", 0.4f, t => Sin(t, 70f - t * 60f) * 0.7f * Env(t, 0.001f, 0.09f) + Nz(t, 5) * 0.35f * Env(t, 0.001f, 0.025f)
                        + Sin(t, 420f) * 0.07f * Env(t, 0.001f, 0.05f));
                case Sfx.Punch:
                    return Gen("punch", 0.22f, t => (Sin(t, 2400f) * 0.2f + Nz(t, 6) * 0.35f) * Env(t, 0.0005f, 0.012f) + Sin(t, 160f) * 0.3f * Env(t, 0.001f, 0.04f));
                case Sfx.TypeKey:
                    return Gen("key", 0.07f, t => Nz(t, 7) * 0.32f * Env(t, 0.0004f, 0.007f) + Sin(t, 780f) * 0.1f * Env(t, 0.0005f, 0.012f));
                case Sfx.Bell:
                    return Gen("bell", 0.9f, t => (Sin(t, 2093f) * 0.3f + Sin(t, 3136f) * 0.12f + Sin(t, 5230f) * 0.05f) * Env(t, 0.002f, 0.22f));
                case Sfx.SuitcaseOpen:
                    return Gen("valigia_apre", 0.5f, t => Nz(t, 8) * 0.16f * Env(t, 0.03f, 0.07f) + Sin(t, 210f - t * 90f) * 0.3f * Env(t, 0.004f, 0.06f)
                        + (t > 0.18f ? (Sin(t, 1560f) * 0.14f + Nz(t, 9) * 0.08f) * Env(t - 0.18f, 0.001f, 0.03f) : 0f));
                case Sfx.SuitcaseClose:
                    return Gen("valigia_chiude", 0.4f, t => Sin(t, 120f - t * 70f) * 0.55f * Env(t, 0.002f, 0.08f) + Nz(t, 10) * 0.18f * Env(t, 0.001f, 0.02f)
                        + (t > 0.1f ? Sin(t, 1700f) * 0.1f * Env(t - 0.1f, 0.001f, 0.025f) : 0f));
                case Sfx.Whoosh:
                    return Gen("whoosh", 0.35f, t => Nz(t, 11) * 0.26f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.35f)) * (0.5f + 0.5f * Sin(t, 22f)));
                case Sfx.Chime:
                    return Gen("chime", 1.0f, t => (Sin(t, 659f) * 0.22f * Env(t, 0.003f, 0.25f)
                        + (t > 0.09f ? Sin(t, 880f) * 0.22f * Env(t - 0.09f, 0.003f, 0.28f) : 0f)
                        + (t > 0.18f ? Sin(t, 1319f) * 0.2f * Env(t - 0.18f, 0.003f, 0.4f) : 0f)));
                case Sfx.SoftError:
                    // errore gentile: due note basse morbide, nessun suono duro
                    return Gen("errore_gentile", 0.5f, t => (Sin(t, 330f) * 0.16f * Env(t, 0.02f, 0.12f)
                        + (t > 0.11f ? Sin(t, 262f) * 0.16f * Env(t - 0.11f, 0.02f, 0.16f) : 0f)));
                case Sfx.Tick:
                    return Gen("tick", 0.06f, t => Nz(t, 12) * 0.2f * Env(t, 0.0003f, 0.005f) + Sin(t, 1100f) * 0.16f * Env(t, 0.0003f, 0.008f));
                case Sfx.Projector:
                    return Gen("proiettore", 1.2f, t => Nz(t, 13) * 0.08f * (0.5f + 0.5f * Mathf.Sign(Sin(t, 24f))) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.2f))
                        + Sin(t, 52f) * 0.1f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.2f)));
                case Sfx.Sparkle:
                    return Gen("scintille", 0.6f, t => (Sin(t, 2637f) * 0.1f * Env(t, 0.002f, 0.09f)
                        + (t > 0.07f ? Sin(t, 3520f) * 0.09f * Env(t - 0.07f, 0.002f, 0.1f) : 0f)
                        + (t > 0.15f ? Sin(t, 4186f) * 0.08f * Env(t - 0.15f, 0.002f, 0.12f) : 0f)));
                case Sfx.TagDrop:
                    return Gen("cartellino", 0.25f, t => Nz(t, 14) * 0.2f * Env(t, 0.005f, 0.035f) + Sin(t, 240f) * 0.18f * Env(t, 0.003f, 0.05f));
            }
            return null;
        }
    }
}
