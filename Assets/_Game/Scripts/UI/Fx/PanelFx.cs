using System;
using UnityEngine;

namespace TrenoBambini.UI.Fx
{
    public enum PanelEntry { Pop, Letter, Suitcase, Notebook }

    /// <summary>
    /// Apertura/chiusura animata dei pannelli modali. Si aggiunge alla radice del pannello (con velo): "card" è la parte che si muove.
    /// Show() attiva e anima; Hide(cb) anima e disattiva. Mai bloccante per l'input: durate &lt; 0,5 s.
    /// </summary>
    public class PanelFx : MonoBehaviour
    {
        public RectTransform card;
        public PanelEntry entry = PanelEntry.Pop;

        CanvasGroup group;
        bool hiding;
        Vector2 restPos;
        bool restKnown;

        public bool IsHiding { get { return hiding; } }

        CanvasGroup Group()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }
            return group;
        }

        public void Show(Action done = null)
        {
            var g = Group();
            hiding = false;
            UITween.Cancel(g);
            if (card != null)
            {
                UITween.Cancel(card);
                if (!restKnown) { restPos = card.anchoredPosition; restKnown = true; }
            }
            gameObject.SetActive(true);
            g.alpha = 0f;
            g.blocksRaycasts = true;
            UITween.Alpha(g, 1f, 0.2f, Ease.OutCubic);
            UISfx.Play(entry == PanelEntry.Suitcase ? Sfx.SuitcaseOpen : (entry == PanelEntry.Notebook ? Sfx.ClickPaper : Sfx.Whoosh), 0.6f);

            if (card == null || UITheme.ReduceEffects)
            {
                if (card != null) { card.anchoredPosition = restPos; card.localScale = Vector3.one; card.localRotation = Quaternion.identity; }
                if (done != null) done();
                return;
            }

            float fromY = 0f, fromRot = 0f, fromScale = 0.9f;
            switch (entry)
            {
                case PanelEntry.Letter: fromY = -240f; fromRot = UnityEngine.Random.Range(-5f, 5f); fromScale = 0.96f; break;
                case PanelEntry.Notebook: fromY = -120f; fromRot = -4f; fromScale = 0.94f; break;
                case PanelEntry.Suitcase: fromY = 60f; fromScale = 0.82f; break;
            }
            var rt = card;
            Vector2 rest = restPos;
            UITween.Run(rt, UITheme.Apertura, Ease.Linear, v =>
            {
                if (rt == null) return;
                float k = UITween.Evaluate(Ease.OutBack, v);
                float c = UITween.Evaluate(Ease.OutCubic, v);
                rt.anchoredPosition = new Vector2(rest.x, Mathf.LerpUnclamped(rest.y + fromY, rest.y, k));
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(fromRot, 0f, c));
                float s = Mathf.LerpUnclamped(fromScale, 1f, k);
                rt.localScale = entry == PanelEntry.Suitcase ? new Vector3(1f, Mathf.Max(0.05f, Mathf.Lerp(0.2f, 1f, k)) , 1f) * Mathf.Lerp(0.9f, 1f, c) : new Vector3(s, s, 1f);
            }, done);
        }

        public void Hide(Action done = null)
        {
            if (hiding) return;
            if (!gameObject.activeInHierarchy) { if (done != null) done(); return; }
            hiding = true;
            var g = Group();
            UITween.Cancel(g);
            g.blocksRaycasts = false;
            if (card != null) UITween.Cancel(card);
            UISfx.Play(entry == PanelEntry.Suitcase ? Sfx.SuitcaseClose : Sfx.ClickPaper, 0.5f);
            if (card != null && !UITheme.ReduceEffects)
            {
                var rt = card;
                Vector2 rest = restKnown ? restPos : rt.anchoredPosition;
                UITween.Run(rt, UITheme.Chiusura, Ease.InCubic, v =>
                {
                    if (rt == null) return;
                    float s = Mathf.Lerp(1f, 0.94f, v);
                    rt.localScale = new Vector3(s, s, 1f);
                    rt.anchoredPosition = rest + new Vector2(0f, -40f * v);
                });
            }
            UITween.Alpha(g, 0f, UITheme.Chiusura, Ease.InCubic, () =>
            {
                hiding = false;
                gameObject.SetActive(false);
                if (card != null) { card.localScale = Vector3.one; card.localRotation = Quaternion.identity; if (restKnown) card.anchoredPosition = restPos; }
                g.blocksRaycasts = true;
                if (done != null) done();
            });
        }
    }
}
