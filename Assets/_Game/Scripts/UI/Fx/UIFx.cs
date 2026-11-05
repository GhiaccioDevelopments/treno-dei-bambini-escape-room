using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI.Fx
{
    /// <summary>Effetti riutilizzabili: scossa (Shake), oscillazione gentile d'errore (Wobble), salto (Pop), linee di spago.</summary>
    public static class UIFx
    {
        /// <summary>Piccola scossa dello schermo/pannello. Non fa nulla con "Effetti ridotti".</summary>
        public static void Shake(RectTransform rt, float amp = 8f, float dur = 0.3f)
        {
            if (rt == null || UITheme.ReduceEffects) return;
            var key = rt.gameObject;
            UITween.Cancel(key);
            Vector2 origin = rt.anchoredPosition;
            UITween.Run(key, dur, Ease.Linear, v =>
            {
                if (rt == null) return;
                float f = 1f - v;
                rt.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * amp * f;
            }, () => { if (rt != null) rt.anchoredPosition = origin; });
        }

        /// <summary>Oscillazione gentile (errore non punitivo): ondeggia in rotazione e torna ferma.</summary>
        public static void Wobble(RectTransform rt, float degrees = 3f, float dur = 0.5f)
        {
            if (rt == null) return;
            var key = rt.gameObject;
            UITween.Cancel(key);
            if (UITheme.ReduceEffects) return;
            UITween.Run(key, dur, Ease.Linear, v =>
            {
                if (rt != null) rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(v * Mathf.PI * 5f) * degrees * (1f - v));
            }, () => { if (rt != null) rt.localRotation = Quaternion.identity; });
        }

        /// <summary>Salto: ingrandisce un attimo e torna a scala 1 con rimbalzo.</summary>
        public static void Pop(RectTransform rt, float peak = 1.18f, float dur = 0.35f)
        {
            if (rt == null) return;
            UITween.Cancel(rt);
            if (UITheme.ReduceEffects) { rt.localScale = Vector3.one; return; }
            UITween.Run(rt, dur, Ease.Linear, v =>
            {
                if (rt == null) return;
                float s = 1f + (peak - 1f) * Mathf.Sin(Mathf.Clamp01(v * 1.4f) * Mathf.PI) * (1f - v * 0.3f);
                rt.localScale = new Vector3(s, s, 1f);
            }, () => { if (rt != null) rt.localScale = Vector3.one; });
        }

        /// <summary>Ruota il pannello "sul suo asse verticale" fingendo la rotazione Y (scala X 1 → 0, cambio, 0 → 1).</summary>
        public static void FlipX(RectTransform rt, Action atMiddle, float dur = 0.28f)
        {
            if (rt == null) { if (atMiddle != null) atMiddle(); return; }
            UITween.Cancel(rt);
            if (UITheme.ReduceEffects) { if (atMiddle != null) atMiddle(); rt.localScale = Vector3.one; return; }
            bool swapped = false;
            UITween.Run(rt, dur, Ease.Linear, v =>
            {
                if (rt == null) return;
                float sx = Mathf.Abs(1f - 2f * v);
                if (v >= 0.5f && !swapped) { swapped = true; if (atMiddle != null) atMiddle(); }
                rt.localScale = new Vector3(Mathf.Max(0.02f, sx), 1f, 1f);
            }, () => { if (rt != null) rt.localScale = Vector3.one; });
        }

        /// <summary>Spago/linea tra due punti locali di "parent", con leggero cedimento (sag in px). Restituisce il contenitore.</summary>
        public static RectTransform Line(RectTransform parent, Vector2 a, Vector2 b, Color color, float thickness, float sag, int segments = 10)
        {
            var root = UIFactory.NewRect("Spago", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            Vector2 prev = a;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 p = Vector2.Lerp(a, b, t) + Vector2.down * Mathf.Sin(t * Mathf.PI) * sag;
                var seg = UIFactory.MakePanel(root, "s", color, false);
                seg.sprite = UIArt.White();
                var r = seg.rectTransform;
                Vector2 d = p - prev;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = prev;
                r.sizeDelta = new Vector2(d.magnitude + 1f, thickness);
                r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                prev = p;
            }
            return root;
        }

        /// <summary>Mostra una linea che si "tende": scala il contenitore da 0 a 1 in orizzontale lungo la direzione.</summary>
        public static void FadeIn(RectTransform rt, float dur = 0.25f)
        {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            UITween.Alpha(g, 1f, dur);
        }
    }
}
