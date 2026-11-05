using System;
using TMPro;
using UnityEngine;

namespace TrenoBambini.UI.Fx
{
    /// <summary>
    /// Testo che compare lettera per lettera (macchina da scrivere). Usa maxVisibleCharacters: i tag rich text sono rispettati.
    /// Click/tasto = completa subito (vedi Complete). Tempo "unscaled".
    /// </summary>
    public class Typewriter : MonoBehaviour
    {
        TMP_Text target;
        float shown;
        int total;
        float cps;
        bool playSound;
        bool bellAtEnd;
        float nextTick;
        Action onDone;

        public bool IsTyping { get; private set; }

        /// <summary>Avvia la scrittura su "t" (aggiunge il componente se manca).</summary>
        public static Typewriter Play(TMP_Text t, string text, float charsPerSecond = 55f, bool sound = true, Action done = null)
        {
            var tw = t.GetComponent<Typewriter>();
            if (tw == null) tw = t.gameObject.AddComponent<Typewriter>();
            tw.Begin(t, text, charsPerSecond, sound, done);
            return tw;
        }

        void Begin(TMP_Text t, string text, float charsPerSecond, bool sound, Action done)
        {
            target = t;
            t.text = text ?? "";
            t.maxVisibleCharacters = 0;
            t.ForceMeshUpdate();
            total = t.textInfo.characterCount;
            shown = 0f;
            cps = Mathf.Max(5f, charsPerSecond);
            playSound = sound;
            bellAtEnd = sound && total > 90;
            onDone = done;
            nextTick = 0f;
            IsTyping = total > 0;
            if (!IsTyping)
            {
                t.maxVisibleCharacters = 99999;
                if (done != null) done();
            }
            enabled = true;
        }

        /// <summary>Mostra subito tutto il testo.</summary>
        public void Complete()
        {
            if (!IsTyping) return;
            Finish();
        }

        void Finish()
        {
            IsTyping = false;
            if (target != null) target.maxVisibleCharacters = 99999;
            if (bellAtEnd) UISfx.Play(Sfx.Bell, 0.35f, 0.02f);
            var cb = onDone;
            onDone = null;
            if (cb != null) cb();
        }

        void Update()
        {
            if (!IsTyping || target == null) return;
            shown += cps * Time.unscaledDeltaTime;
            int n = Mathf.Min(total, Mathf.FloorToInt(shown));
            if (n != target.maxVisibleCharacters)
            {
                target.maxVisibleCharacters = n;
                if (playSound && Time.unscaledTime >= nextTick)
                {
                    UISfx.Play(Sfx.TypeKey, 0.22f, 0.12f);
                    nextTick = Time.unscaledTime + 0.07f;
                }
            }
            if (n >= total) Finish();
        }
    }
}
