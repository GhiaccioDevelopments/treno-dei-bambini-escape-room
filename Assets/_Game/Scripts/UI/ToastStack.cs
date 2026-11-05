using System.Collections.Generic;
using TMPro;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Messaggi brevi a "telegramma": strisce di carta che scivolano dal lato con il testo battuto a macchina.
    /// Fino a 3 telegrammi impilati (invece di sovrascriversi); un messaggio uguale ad uno già visibile ne rinnova solo il tempo.
    /// </summary>
    public class ToastStack : MonoBehaviour
    {
        const int Max = 3;
        const float ItemH = 92f;
        const float Top = -86f;

        class Item
        {
            public RectTransform rt;
            public TextMeshProUGUI body;
            public string text;
            public float timer;
            public bool leaving;
            public float targetY;
        }

        RectTransform layer;
        readonly List<Item> items = new List<Item>();

        public static ToastStack Create(RectTransform parent)
        {
            var s = parent.gameObject.AddComponent<ToastStack>();
            s.layer = parent;
            return s;
        }

        public void Push(string text, float secs)
        {
            for (int i = 0; i < items.Count; i++)
                if (!items[i].leaving && items[i].text == text) { items[i].timer = Mathf.Max(items[i].timer, secs); return; }

            // troppi telegrammi: il più vecchio se ne va subito
            int active = 0;
            for (int i = 0; i < items.Count; i++) if (!items[i].leaving) active++;
            if (active >= Max)
                for (int i = 0; i < items.Count; i++) if (!items[i].leaving) { Leave(items[i]); break; }

            var it = new Item { text = text, timer = Mathf.Max(0.8f, secs) };
            var img = UIFactory.MakePaper(layer, "Telegramma", new Color(0.97f, 0.9f, 0.7f, 1f), false);
            it.rt = img.rectTransform;
            UIFactory.Place(it.rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, Top), new Vector2(860f, 84f));

            var head = UIFactory.MakeText(it.rt, "TELEGRAMMA", 14, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Stretch(head.rectTransform, 30f, 0f, 20f, 8f);
            head.characterSpacing = 6f;
            var line = UIFactory.MakePanel(it.rt, "Filetto", new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.45f), false);
            line.sprite = UIArt.White();
            UIFactory.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(790f, 2f));

            it.body = UIFactory.MakeText(it.rt, "", 24, UITheme.Inchiostro, TextAlignmentOptions.Left);
            UIFactory.Stretch(it.body.rectTransform, 30f, 8f, 24f, 30f);
            it.body.enableAutoSizing = true; it.body.fontSizeMin = 14; it.body.fontSizeMax = 24;

            // gli altri scendono, il nuovo entra dal lato
            for (int i = 0; i < items.Count; i++) if (!items[i].leaving) items[i].targetY -= ItemH;
            it.targetY = Top;
            Reposition();
            items.Add(it);

            UISfx.Play(Sfx.Whoosh, 0.35f);
            var cg = it.rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            UITween.Alpha(cg, 1f, 0.15f);
            it.rt.anchoredPosition = new Vector2(UITheme.ReduceEffects ? 0f : 900f, it.targetY);
            UITween.Move(it.rt, new Vector2(0f, it.targetY), 0.4f, Ease.OutCubic);
            Typewriter.Play(it.body, text, 90f, true);
        }

        void Reposition()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.leaving || it.rt == null) continue;
                Vector2 p = it.rt.anchoredPosition;
                // gli elementi già presenti scivolano alla nuova quota (il nuovo ha già la sua posizione impostata dopo)
                UITween.Cancel(it.rt);
                UITween.Move(it.rt, new Vector2(p.x > 400f ? p.x : 0f, it.targetY), 0.25f, Ease.OutCubic);
            }
        }

        void Leave(Item it)
        {
            if (it.leaving) return;
            it.leaving = true;
            UITween.Cancel(it.rt);
            var cg = it.rt.GetComponent<CanvasGroup>();
            if (!UITheme.ReduceEffects)
            {
                UITween.Scale(it.rt, new Vector3(0.92f, 0.8f, 1f), 0.25f, Ease.InCubic);
                UITween.Rotate(it.rt, Random.Range(-3f, 3f), 0.25f, Ease.InCubic);
            }
            UITween.Alpha(cg, 0f, 0.25f, Ease.InCubic, () =>
            {
                items.Remove(it);
                if (it.rt != null) Destroy(it.rt.gameObject);
            });
            // chi rimane risale
            float y = Top;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].leaving) continue;
                items[i].targetY = y;
                y -= ItemH;
            }
            Reposition();
        }

        public void Clear()
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (items[i].rt != null) { UITween.Cancel(items[i].rt); Destroy(items[i].rt.gameObject); }
            items.Clear();
        }

        void Update()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var it = items[i];
                if (it.leaving) continue;
                it.timer -= Time.unscaledDeltaTime;
                if (it.timer <= 0f) Leave(it);
            }
        }
    }
}
