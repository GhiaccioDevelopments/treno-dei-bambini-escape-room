using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI.Fx
{
    public enum Ease { Linear, OutCubic, InCubic, InOutCubic, OutBack, OutElastic, OutBounce, InBack }

    /// <summary>
    /// Mini motore di tween interno (nessuna libreria esterna). Tempo sempre "unscaled" (funziona in pausa).
    /// Un solo runner nascosto; i tween sono in pool. Ogni tween ha un target (chiave) per essere annullato in blocco.
    /// </summary>
    public class UITween : MonoBehaviour
    {
        sealed class T
        {
            public object key;
            public float delay, dur, t;
            public Ease ease;
            public Action<float> onUpdate;
            public Action onDone;
            public bool loop, pingPong, active;
            public int gen; // cambia a ogni rilascio: distingue un tween riciclato dal pool
        }

        static UITween runner;
        static readonly List<T> live = new List<T>(64);
        static readonly Stack<T> pool = new Stack<T>(64);
        static readonly List<T> doneBuffer = new List<T>(16);
        static readonly List<int> doneGen = new List<int>(16);

        static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("UITween") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            runner = go.AddComponent<UITween>();
        }

        // ------------------------------------------------------------ API generica
        /// <summary>Avvia un tween 0..1. onUpdate riceve il valore già "easato". key = oggetto per Cancel(key).</summary>
        public static void Run(object key, float dur, Ease ease, Action<float> onUpdate, Action onDone = null,
            float delay = 0f, bool loop = false, bool pingPong = false)
        {
            EnsureRunner();
            T t = pool.Count > 0 ? pool.Pop() : new T();
            t.key = key; t.dur = Mathf.Max(0.0001f, dur); t.t = 0f; t.delay = delay; t.ease = ease;
            t.onUpdate = onUpdate; t.onDone = onDone; t.loop = loop; t.pingPong = pingPong; t.active = true;
            live.Add(t);
        }

        /// <summary>Annulla tutti i tween del target (senza chiamare onDone).</summary>
        public static void Cancel(object key)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i].key == key) { Release(live[i]); live.RemoveAt(i); }
            }
        }

        static void Release(T t)
        {
            t.active = false; t.onUpdate = null; t.onDone = null; t.key = null; t.gen++;
            pool.Push(t);
        }

        /// <summary>Dopo "delay" secondi chiama cb (utile per le cascate).</summary>
        public static void After(object key, float delay, Action cb)
        {
            Run(key, 0.0001f, Ease.Linear, null, cb, delay);
        }

        // ------------------------------------------------------------ scorciatoie
        public static void Float(object key, float from, float to, float dur, Ease ease, Action<float> set, Action done = null, float delay = 0f)
        {
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(key, dur, ease, v => set(Mathf.LerpUnclamped(from, to, v)), done, delay);
        }

        public static void Scale(RectTransform rt, Vector3 to, float dur, Ease ease = Ease.OutCubic, Action done = null, float delay = 0f)
        {
            Vector3 from = rt.localScale;
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(rt, dur, ease, v => { if (rt != null) rt.localScale = Vector3.LerpUnclamped(from, to, v); }, done, delay);
        }

        public static void Move(RectTransform rt, Vector2 to, float dur, Ease ease = Ease.OutCubic, Action done = null, float delay = 0f)
        {
            Vector2 from = rt.anchoredPosition;
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(rt, dur, ease, v => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, v); }, done, delay);
        }

        public static void Rotate(RectTransform rt, float toZ, float dur, Ease ease = Ease.OutCubic, Action done = null, float delay = 0f)
        {
            float from = NormalizeAngle(rt.localEulerAngles.z);
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(rt, dur, ease, v => { if (rt != null) rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(from, toZ, v)); }, done, delay);
        }

        public static void Alpha(CanvasGroup g, float to, float dur, Ease ease = Ease.OutCubic, Action done = null, float delay = 0f)
        {
            float from = g.alpha;
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(g, dur, ease, v => { if (g != null) g.alpha = Mathf.LerpUnclamped(from, to, v); }, done, delay);
        }

        public static void Fade(Graphic gr, float to, float dur, Ease ease = Ease.OutCubic, Action done = null, float delay = 0f)
        {
            float from = gr.color.a;
            dur = UITheme.Dur(dur); delay = UITheme.ReduceEffects ? 0f : delay;
            Run(gr, dur, ease, v => { if (gr != null) { var c = gr.color; c.a = Mathf.LerpUnclamped(from, to, v); gr.color = c; } }, done, delay);
        }

        public static void Tint(Graphic gr, Color to, float dur, Ease ease = Ease.OutCubic, Action done = null)
        {
            Color from = gr.color;
            dur = UITheme.Dur(dur);
            Run(gr, dur, ease, v => { if (gr != null) gr.color = Color.LerpUnclamped(from, to, v); }, done);
        }

        static float NormalizeAngle(float a) { return a > 180f ? a - 360f : a; }

        // ------------------------------------------------------------ easing
        public static float Evaluate(Ease e, float x)
        {
            x = Mathf.Clamp01(x);
            switch (e)
            {
                case Ease.OutCubic: { float k = 1f - x; return 1f - k * k * k; }
                case Ease.InCubic: return x * x * x;
                case Ease.InOutCubic: return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1f; float k = x - 1f; return 1f + c3 * k * k * k + c1 * k * k; }
                case Ease.InBack: { const float c1 = 1.70158f, c3 = c1 + 1f; return c3 * x * x * x - c1 * x * x; }
                case Ease.OutElastic:
                    if (x <= 0f) return 0f;
                    if (x >= 1f) return 1f;
                    return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.OutBounce:
                    {
                        const float n1 = 7.5625f, d1 = 2.75f;
                        if (x < 1f / d1) return n1 * x * x;
                        if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
                        if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
                        x -= 2.625f / d1; return n1 * x * x + 0.984375f;
                    }
                default: return x;
            }
        }

        // ------------------------------------------------------------ loop
        void OnDestroy() { if (runner == this) runner = null; }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); // evita salti dopo un hiccup/caricamento
            doneBuffer.Clear(); doneGen.Clear();
            // ciclo a indice: i callback possono aggiungere tween (finiscono in coda e partono al frame dopo)
            int n = live.Count;
            for (int i = 0; i < n && i < live.Count; i++)
            {
                T t = live[i];
                if (!t.active) continue;
                var uo = t.key as UnityEngine.Object;
                if (uo != null || !(t.key is UnityEngine.Object)) { /* bersaglio vivo (o chiave non Unity) */ }
                else { t.active = false; doneBuffer.Add(t); doneGen.Add(t.gen); t.onDone = null; continue; } // bersaglio distrutto: il tween (anche in loop) termina in silenzio
                if (t.delay > 0f) { t.delay -= dt; if (t.delay > 0f) continue; }
                t.t += dt;
                float x = t.t / t.dur;
                if (x >= 1f && !t.loop) { x = 1f; doneBuffer.Add(t); doneGen.Add(t.gen); }
                else if (x >= 1f) { t.t = 0f; x = 0f; }
                float u = t.pingPong ? 1f - Mathf.Abs(2f * x - 1f) : x;
                if (t.onUpdate != null) t.onUpdate(Evaluate(t.ease, u));
            }
            for (int i = 0; i < doneBuffer.Count; i++)
            {
                T t = doneBuffer[i];
                if (t.gen != doneGen[i]) continue; // annullato (e magari riciclato) da un callback: non chiuderlo
                int idx = live.IndexOf(t);
                if (idx < 0) continue; // annullato da un callback
                live.RemoveAt(idx);
                var cb = t.onDone;
                Release(t);
                if (cb != null) cb();
            }
        }
    }
}
