using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrenoBambini.UI.Fx
{
    public enum ButtonStyle { Brass, Tag, Seal, Card, Ink }

    /// <summary>
    /// Comportamento animato dei pulsanti: hover (ingrandisce, riflesso sull'ottone), pressione a timbro (scala 0,94 + polvere),
    /// rilascio elastico, suono per materiale, "respiro" sul pulsante consigliato.
    /// Si aggiunge in UIFactory.MakeButton: tutti i pulsanti ne sono dotati. Funziona con mouse, touch (senza hover) e tastiera/click del Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public ButtonStyle style = ButtonStyle.Brass;
        /// <summary>Pulsante consigliato: "respira" lentamente quando è fermo.</summary>
        public bool recommended;
        public float hoverScale = 1.045f;

        Button btn;
        RectTransform rt;
        bool hover, down;
        float calmAt;
        RectTransform glint;
        Image underline;
        float pulse;

        void Awake()
        {
            btn = GetComponent<Button>();
            rt = (RectTransform)transform;
            btn.onClick.AddListener(OnClick);
        }

        void OnEnable()
        {
            hover = down = false;
            if (rt != null) rt.localScale = Vector3.one;
            if (underline != null) underline.fillAmount = 0f;
            calmAt = 0f;
        }

        void OnDisable()
        {
            UITween.Cancel(rt);
            UITween.Cancel(this);
            if (rt != null) rt.localScale = Vector3.one;
        }

        // ------------------------------------------------------------ eventi
        public void OnPointerEnter(PointerEventData e)
        {
            if (!btn.interactable) return;
            hover = true;
            if (e.pointerId >= 0) return; // touch: niente hover
            UISfx.Play(Sfx.Hover, 0.5f, 0.1f);
            if (!down) Animate(hoverScale, 0.14f, Ease.OutCubic);
            if (style == ButtonStyle.Brass) Glint();
            if (style == ButtonStyle.Ink) Underline(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hover = false;
            if (!btn.interactable || down) { if (style == ButtonStyle.Ink) Underline(false); return; }
            Animate(1f, 0.14f, Ease.OutCubic);
            if (style == ButtonStyle.Ink) Underline(false);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!btn.interactable) return;
            down = true;
            Animate(0.94f, 0.07f, Ease.InCubic);
            ParticleKind k = style == ButtonStyle.Tag || style == ButtonStyle.Card || style == ButtonStyle.Ink ? ParticleKind.Dust : ParticleKind.Ink;
            if (style == ButtonStyle.Brass) k = ParticleKind.Dust;
            UIParticles.BurstAtScreen(k, e.position, style == ButtonStyle.Seal ? 6 : 3);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!down) return;
            down = false;
            if (!btn.interactable) return;
            // su touch non c'è hover: dopo il rilascio si torna a 1
            bool stay = hover && e.pointerId < 0;
            Animate(stay ? hoverScale : 1f, 0.32f, Ease.OutBack);
        }

        void OnClick()
        {
            switch (style)
            {
                case ButtonStyle.Brass: UISfx.Play(Sfx.ClickBrass, 0.7f); break;
                case ButtonStyle.Seal: UISfx.Play(Sfx.ClickSeal, 0.85f); break;
                default: UISfx.Play(Sfx.ClickPaper, 0.8f); break;
            }
        }

        // ------------------------------------------------------------ animazioni
        void Animate(float s, float dur, Ease ease)
        {
            UITween.Cancel(rt);
            UITween.Scale(rt, new Vector3(s, s, 1f), dur, ease);
            calmAt = Time.unscaledTime + dur + 0.1f;
        }

        void Glint()
        {
            if (UITheme.ReduceEffects) return;
            if (glint == null)
            {
                var clip = UIFactory.NewRect("Riflesso", rt);
                UIFactory.Stretch(clip, 8f, 8f, 8f, 8f);
                clip.gameObject.AddComponent<RectMask2D>();
                var bar = UIFactory.MakePanel(clip, "Barra", new Color(1f, 0.97f, 0.85f, 0.38f), false);
                bar.sprite = UIArt.White();
                glint = bar.rectTransform;
                glint.anchorMin = new Vector2(0f, 0f); glint.anchorMax = new Vector2(0f, 1f);
                glint.pivot = new Vector2(0.5f, 0.5f);
                glint.sizeDelta = new Vector2(26f, 40f);
                glint.localRotation = Quaternion.Euler(0f, 0f, 18f);
            }
            float w = rt.rect.width;
            glint.gameObject.SetActive(true);
            UITween.Cancel(glint);
            UITween.Run(glint, 0.45f, Ease.OutCubic, v =>
            {
                if (glint != null) glint.anchoredPosition = new Vector2(Mathf.Lerp(-30f, w + 30f, v), 0f);
            }, () => { if (glint != null) glint.gameObject.SetActive(false); });
        }

        void Underline(bool on)
        {
            if (underline == null)
            {
                var u = UIFactory.MakePanel(rt, "Sottolineatura", UITheme.Ceralacca, false);
                u.sprite = UIArt.White();
                u.type = Image.Type.Filled;
                u.fillMethod = Image.FillMethod.Horizontal;
                u.fillAmount = 0f;
                var r = u.rectTransform;
                r.anchorMin = new Vector2(0.05f, 0f); r.anchorMax = new Vector2(0.95f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 6f);
                r.sizeDelta = new Vector2(0f, 3f);
                underline = u;
            }
            float from = underline.fillAmount;
            UITween.Cancel(underline);
            UITween.Float(underline, from, on ? 1f : 0f, on ? 0.28f : 0.15f, Ease.OutCubic, v => { if (underline != null) underline.fillAmount = v; });
        }

        void Update()
        {
            if (!recommended || hover || down || !btn.interactable || UITheme.ReduceEffects) return;
            if (Time.unscaledTime < calmAt) return;
            pulse += Time.unscaledDeltaTime * 2.2f;
            float s = 1f + 0.015f * (0.5f + 0.5f * Mathf.Sin(pulse));
            rt.localScale = new Vector3(s, s, 1f);
        }
    }
}
