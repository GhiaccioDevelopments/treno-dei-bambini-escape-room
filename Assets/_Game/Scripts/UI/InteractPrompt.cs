using TMPro;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Prompt "Interagisci": cartellino appeso a uno spago che cade dall'alto e dondola, con tasto E da macchina da scrivere;
    /// più un rombo dorato nel mondo 3D sopra l'oggetto interagibile. Vive in un layer con Canvas annidato (animazioni per frame isolate).
    /// </summary>
    public class InteractPrompt : MonoBehaviour
    {
        const string KeyPrefix = "[E] ";

        RectTransform layer;
        RectTransform tagRt, key, markerRoot, markerGlow;
        Image tagImg, stringImg, markerImg;
        TextMeshProUGUI label, keyLabel;
        string shownText;
        bool visible, hiding, special;
        float restY = 150f;

        // indicatore nel mondo
        bool markerWanted, markerShown;
        Vector3 markerWorld;
        Vector2 markerPos;
        bool markerHasPos;

        public static InteractPrompt Create(RectTransform parent)
        {
            var rt = UIFactory.NewRect("Prompt", parent);
            UIFactory.Stretch(rt);
            var p = rt.gameObject.AddComponent<InteractPrompt>();
            p.layer = rt;
            p.Build();
            return p;
        }

        void Build()
        {
            // indicatore nel mondo
            markerRoot = UIFactory.NewRect("Indicatore", layer);
            markerRoot.anchorMin = markerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            markerRoot.sizeDelta = new Vector2(44f, 44f);
            markerGlow = UIFactory.MakePanel(markerRoot, "Alone", new Color(UITheme.Oro.r, UITheme.Oro.g, UITheme.Oro.b, 0.55f), false).rectTransform;
            markerGlow.GetComponent<Image>().sprite = UIArt.Glow();
            markerGlow.anchorMin = markerGlow.anchorMax = new Vector2(0.5f, 0.5f);
            markerGlow.sizeDelta = new Vector2(110f, 110f);
            markerImg = UIFactory.MakePanel(markerRoot, "Rombo", Color.white, false);
            markerImg.sprite = UIArt.Diamond();
            markerImg.rectTransform.anchorMin = markerImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            markerImg.rectTransform.sizeDelta = new Vector2(34f, 34f);
            markerRoot.gameObject.SetActive(false);

            // cartellino con spago (pivot in alto al centro: dondola come un pendolo)
            tagRt = UIFactory.NewRect("Cartellino", layer);
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0.5f, 0f);
            tagRt.pivot = new Vector2(0.5f, 1f);
            tagRt.sizeDelta = new Vector2(560f, 66f);
            tagImg = tagRt.gameObject.AddComponent<Image>();
            tagImg.sprite = UIArt.Tag();
            tagImg.type = Image.Type.Sliced;
            tagImg.raycastTarget = false;

            stringImg = UIFactory.MakePanel(tagRt, "Spago", new Color(0.35f, 0.25f, 0.15f, 1f), false);
            stringImg.sprite = UIArt.White();
            var sr = stringImg.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 1f);
            sr.pivot = new Vector2(0.5f, 0f);
            sr.anchoredPosition = new Vector2(0f, -4f);
            sr.sizeDelta = new Vector2(3f, 900f);

            key = UIFactory.NewRect("Tasto", tagRt);
            key.anchorMin = key.anchorMax = new Vector2(0f, 0.5f);
            key.pivot = new Vector2(0f, 0.5f);
            key.anchoredPosition = new Vector2(18f, 0f);
            key.sizeDelta = new Vector2(46f, 46f);
            var ki = key.gameObject.AddComponent<Image>();
            ki.sprite = UIArt.KeyCap();
            ki.type = Image.Type.Sliced;
            ki.raycastTarget = false;
            keyLabel = UIFactory.MakeText(key, "E", 26, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(keyLabel.rectTransform, 0f, 5f, 0f, 0f);

            label = UIFactory.MakeText(tagRt, "", 28, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(label.rectTransform, 74f, 4f, 20f, 4f);
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 28;

            tagRt.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ prompt
        public void Show(string raw, bool isSpecial)
        {
            if (string.IsNullOrEmpty(raw)) { Hide(); return; }
            bool hasKey = raw.StartsWith(KeyPrefix);
            string text = hasKey ? raw.Substring(KeyPrefix.Length) : raw;

            if (visible && !hiding)
            {
                if (text != shownText)
                {
                    shownText = text;
                    string t = text;
                    UISfx.Play(Sfx.ClickPaper, 0.4f);
                    UIFx.FlipX(tagRt, () => { label.text = t; });
                }
                SetSpecial(isSpecial);
                return;
            }

            // entrata: il cartellino cade dall'alto e dondola
            visible = true; hiding = false;
            shownText = text;
            label.text = text;
            key.gameObject.SetActive(hasKey);
            UIFactory.Stretch(label.rectTransform, hasKey ? 74f : 20f, 4f, 20f, 4f);
            float w = Mathf.Clamp(label.GetPreferredValues(text).x + (hasKey ? 120f : 70f), 240f, 760f);
            tagRt.sizeDelta = new Vector2(w, 66f);
            SetSpecial(isSpecial);

            UITween.Cancel(tagRt);
            tagRt.gameObject.SetActive(true);
            tagRt.localScale = Vector3.one;
            tagImg.color = special ? new Color(1f, 0.93f, 0.7f, 1f) : Color.white;
            UISfx.Play(Sfx.TagDrop, 0.5f);
            if (UITheme.ReduceEffects)
            {
                tagRt.anchoredPosition = new Vector2(0f, restY + 66f);
                tagRt.localRotation = Quaternion.identity;
                var cg = EnsureGroup();
                cg.alpha = 0f;
                UITween.Alpha(cg, 1f, 0.15f);
                return;
            }
            EnsureGroup().alpha = 1f;
            float fromY = restY + 66f + 420f;
            float toY = restY + 66f;
            UITween.Run(tagRt, 1.3f, Ease.Linear, v =>
            {
                if (tagRt == null) return;
                float fall = UITween.Evaluate(Ease.OutBounce, Mathf.Clamp01(v / 0.35f));
                tagRt.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(fromY, toY, fall));
                float swing = Mathf.Sin(v * 17f) * 11f * Mathf.Pow(1f - v, 2f) * Mathf.Clamp01(v * 6f);
                tagRt.localRotation = Quaternion.Euler(0f, 0f, swing);
            }, () => { if (tagRt != null) tagRt.localRotation = Quaternion.identity; });
            if (special) UIParticles.BurstAtRect(ParticleKind.Spark, tagRt, 6);
        }

        CanvasGroup EnsureGroup()
        {
            var g = tagRt.GetComponent<CanvasGroup>();
            if (g == null) g = tagRt.gameObject.AddComponent<CanvasGroup>();
            return g;
        }

        void SetSpecial(bool s)
        {
            if (special == s) return;
            special = s;
            tagImg.color = s ? new Color(1f, 0.93f, 0.7f, 1f) : Color.white;
        }

        public void Hide()
        {
            if (!visible || hiding) return;
            hiding = true;
            UITween.Cancel(tagRt);
            var g = EnsureGroup();
            if (UITheme.ReduceEffects)
            {
                UITween.Alpha(g, 0f, 0.1f, Ease.Linear, FinishHide);
                return;
            }
            Vector2 from = tagRt.anchoredPosition;
            UITween.Run(tagRt, 0.22f, Ease.InBack, v =>
            {
                if (tagRt == null) return;
                tagRt.anchoredPosition = from + new Vector2(0f, 260f * v);
                g.alpha = 1f - v;
            }, FinishHide);
        }

        void FinishHide()
        {
            visible = false; hiding = false;
            if (tagRt != null) tagRt.gameObject.SetActive(false);
        }

        /// <summary>Il giocatore ha premuto E: il tasto si abbassa, il cartellino fa un piccolo salto.</summary>
        public void Press()
        {
            if (!visible || hiding) return;
            UISfx.Play(Sfx.ClickBrass, 0.6f);
            UIFx.Pop(key, 0.78f, 0.25f);
            if (!UITheme.ReduceEffects)
            {
                Vector2 rest = new Vector2(0f, restY + 66f);
                UITween.Run(tagRt, 0.22f, Ease.Linear, v => { if (tagRt != null) tagRt.anchoredPosition = rest + new Vector2(0f, Mathf.Sin(v * Mathf.PI) * 14f); },
                    () => { if (tagRt != null) tagRt.anchoredPosition = rest; });
            }
        }

        // ------------------------------------------------------------ indicatore nel mondo
        public void SetMarker(bool show, Vector3 world)
        {
            markerWanted = show;
            markerWorld = world;
        }

        void LateUpdate()
        {
            bool want = markerWanted && Camera.main != null;
            Vector2 target = Vector2.zero;
            if (want)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(markerWorld);
                if (sp.z <= 0.05f) want = false;
                else RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, sp, null, out target);
            }
            if (want && !markerShown)
            {
                markerShown = true;
                markerHasPos = false;
                markerRoot.gameObject.SetActive(true);
                markerRoot.localScale = Vector3.zero;
                UITween.Cancel(markerRoot);
                UITween.Scale(markerRoot, Vector3.one, 0.35f, Ease.OutBack);
            }
            else if (!want && markerShown)
            {
                markerShown = false;
                UITween.Cancel(markerRoot);
                UITween.Scale(markerRoot, Vector3.zero, 0.15f, Ease.InCubic, () => { if (markerRoot != null && !markerShown) markerRoot.gameObject.SetActive(false); });
            }
            if (!markerShown) return;

            if (want)
            {
                if (!markerHasPos) { markerPos = target; markerHasPos = true; }
                else markerPos = Vector2.Lerp(markerPos, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            }
            float t = Time.unscaledTime;
            float bob = UITheme.ReduceEffects ? 0f : Mathf.Sin(t * 3f) * 6f;
            markerRoot.anchoredPosition = markerPos + new Vector2(0f, bob);
            float pulse = UITheme.ReduceEffects ? 1f : 1f + 0.18f * Mathf.Sin(t * 4.5f);
            markerGlow.localScale = new Vector3(pulse, pulse, 1f);
        }
    }
}
