using System.Collections;
using TMPro;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// Lucchetto d'ottone a combinazione: le ruote girano davvero (le cifre scorrono con scatto e clic), selezione e tastiera numerica.
    /// Codice giusto: l'arco si apre con un salto e una luce dorata. Codice sbagliato: piccola scossa e le ruote tornano a zero.
    /// </summary>
    public class LucchettoPanel : PuzzlePanel
    {
        const float WheelH = 96f;

        class Wheel
        {
            public Image bg;
            public RectTransform cur, old;
            public TextMeshProUGUI curT, oldT;
        }

        string code;
        int n;
        int[] digits;
        Wheel[] wheels;
        int sel;
        bool locked;
        RectTransform body, shackle;

        protected override void BuildContent(RectTransform area)
        {
            code = (def.codice ?? "").Trim();
            if (code.Length == 0)
            {
                Debug.LogWarning("Lucchetto senza codice: " + def.id);
                Solved();
                return;
            }
            n = Mathf.Min(code.Length, 8);
            digits = new int[n];
            wheels = new Wheel[n];

            // corpo del lucchetto + arco
            var bodyImg = UIFactory.MakePanel(area, "CorpoLucchetto", Color.white, false);
            bodyImg.sprite = UIArt.Brass(); bodyImg.type = Image.Type.Sliced;
            body = bodyImg.rectTransform;
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
            body.sizeDelta = new Vector2(n * 134f + 70f, 244f);
            body.anchoredPosition = new Vector2(0f, -28f);

            var arc = UIFactory.MakePanel(area, "Arco", Color.white, false);
            arc.sprite = UIArt.BrassFrame(); arc.type = Image.Type.Sliced; arc.fillCenter = false;
            shackle = arc.rectTransform;
            shackle.anchorMin = shackle.anchorMax = new Vector2(0.5f, 0.5f);
            shackle.pivot = new Vector2(0.5f, 0f);
            shackle.sizeDelta = new Vector2(150f, 112f);
            shackle.anchoredPosition = new Vector2(0f, -28f + 244f * 0.5f - 6f);
            shackle.SetAsFirstSibling();
            body.SetSiblingIndex(1);

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                float x = (i - (n - 1) * 0.5f) * 134f;
                var col = UIFactory.NewRect("Ruota" + i, area);
                col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
                col.sizeDelta = new Vector2(110f, 220f);
                col.anchoredPosition = new Vector2(x, -28f);

                var up = UIFactory.MakeButton(col, "+", () => Change(idx, 1), new Vector2(110f, 56f), ButtonStyle.Tag, 38f);
                PlaceTop((RectTransform)up.transform, 0f, 56f);
                var down = UIFactory.MakeButton(col, "-", () => Change(idx, -1), new Vector2(110f, 56f), ButtonStyle.Tag, 38f);
                var dr = (RectTransform)down.transform;
                dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0f); dr.pivot = new Vector2(0.5f, 0f);
                dr.anchoredPosition = Vector2.zero; dr.sizeDelta = new Vector2(110f, 56f);

                var w = new Wheel();
                var window = UIFactory.MakePanel(col, "Finestra", new Color(0.98f, 0.93f, 0.8f), true);
                window.sprite = UIArt.PaperDark(); window.type = Image.Type.Sliced;
                var wr = window.rectTransform;
                wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
                wr.anchoredPosition = Vector2.zero; wr.sizeDelta = new Vector2(110f, WheelH);
                var wb = window.gameObject.AddComponent<Button>();
                wb.transition = Selectable.Transition.None; wb.targetGraphic = window;
                wb.onClick.AddListener(() => Select(idx));
                w.bg = window;

                var clip = UIFactory.NewRect("Rullo", wr);
                UIFactory.Stretch(clip, 4f, 4f, 4f, 4f);
                clip.gameObject.AddComponent<RectMask2D>();
                w.cur = MakeDigit(clip, "0", out w.curT);
                w.old = MakeDigit(clip, "0", out w.oldT);
                w.old.gameObject.SetActive(false);

                // ombre sopra e sotto: danno l'idea del cilindro
                var shadeT = UIFactory.MakePanel(wr, "OmbraSu", new Color(0.15f, 0.08f, 0.02f, 0.35f), false);
                shadeT.sprite = UIArt.White();
                shadeT.rectTransform.anchorMin = new Vector2(0f, 1f); shadeT.rectTransform.anchorMax = new Vector2(1f, 1f);
                shadeT.rectTransform.pivot = new Vector2(0.5f, 1f); shadeT.rectTransform.sizeDelta = new Vector2(-8f, 14f);
                shadeT.rectTransform.anchoredPosition = new Vector2(0f, -4f);
                var shadeB = UIFactory.MakePanel(wr, "OmbraGiu", new Color(0.15f, 0.08f, 0.02f, 0.35f), false);
                shadeB.sprite = UIArt.White();
                shadeB.rectTransform.anchorMin = new Vector2(0f, 0f); shadeB.rectTransform.anchorMax = new Vector2(1f, 0f);
                shadeB.rectTransform.pivot = new Vector2(0.5f, 0f); shadeB.rectTransform.sizeDelta = new Vector2(-8f, 14f);
                shadeB.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                wheels[i] = w;
            }
            Select(0);
            ShowAction("Apri", TryOpen);
            Info("Gira le ruote con + e - (oppure scrivi le cifre con la tastiera), poi premi Apri.");
        }

        static void PlaceTop(RectTransform r, float y, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y); r.sizeDelta = new Vector2(110f, h);
        }

        static RectTransform MakeDigit(RectTransform parent, string text, out TextMeshProUGUI t)
        {
            t = UIFactory.MakeText(parent, text, 72, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            t.enableAutoSizing = false; t.fontSize = 72;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(100f, WheelH);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        void Select(int i)
        {
            sel = Mathf.Clamp(i, 0, n - 1);
            for (int k = 0; k < n; k++)
                wheels[k].bg.color = k == sel ? new Color(1f, 0.93f, 0.62f) : new Color(0.98f, 0.93f, 0.8f);
        }

        /// <summary>Imposta la cifra con animazione di scorrimento: dir +1 = la nuova cifra sale dal basso, -1 = scende dall'alto, 0 = secca.</summary>
        void Roll(int i, int value, int dir)
        {
            var w = wheels[i];
            string prev = digits[i].ToString();
            digits[i] = value;
            w.curT.text = value.ToString();
            UITween.Cancel(w.cur);
            if (dir == 0 || UITheme.ReduceEffects)
            {
                w.cur.anchoredPosition = Vector2.zero;
                w.old.gameObject.SetActive(false);
                return;
            }
            w.oldT.text = prev;
            w.old.gameObject.SetActive(true);
            UISfx.Play(Sfx.Tick, 0.5f, 0.12f);
            UITween.Run(w.cur, 0.16f, Ease.OutCubic, v =>
            {
                if (w.cur == null) return;
                w.cur.anchoredPosition = new Vector2(0f, Mathf.Lerp(-WheelH * dir, 0f, v));
                w.old.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, WheelH * dir, v));
            }, () => { if (w.old != null) w.old.gameObject.SetActive(false); });
        }

        void Change(int i, int delta)
        {
            if (solved || locked) return;
            Roll(i, (digits[i] + delta + 10) % 10, delta);
            Select(i);
        }

        void TryOpen()
        {
            if (solved || locked) return;
            var sb = new System.Text.StringBuilder(n);
            for (int i = 0; i < n; i++) sb.Append(digits[i]);
            if (sb.ToString() == code.Substring(0, n))
            {
                locked = true;
                UISfx.Play(Sfx.ClickBrass, 1f);
                // l'arco salta e si apre; luce dorata
                if (!UITheme.ReduceEffects)
                {
                    Vector2 rest = shackle.anchoredPosition;
                    UITween.Run(shackle, 0.5f, Ease.Linear, v =>
                    {
                        if (shackle == null) return;
                        float k = UITween.Evaluate(Ease.OutBack, v);
                        shackle.anchoredPosition = rest + new Vector2(0f, 30f * k);
                        shackle.localRotation = Quaternion.Euler(0f, 0f, -14f * k);
                    });
                }
                UIParticles.BurstAtRect(ParticleKind.Spark, shackle, 14);
                UISfx.Play(Sfx.Chime, 0.7f);
                var glow = UIFactory.MakePanel(body, "Luce", new Color(UITheme.Oro.r, UITheme.Oro.g, UITheme.Oro.b, 0f), false);
                glow.sprite = UIArt.Glow();
                UIFactory.Stretch(glow.rectTransform, -60f, -60f, -60f, -60f);
                UITween.Fade(glow, 0.55f, 0.3f);
                UITween.After(this, 0.5f, () => { locked = false; Solved("Clic! Il lucchetto si apre."); });
            }
            else
            {
                Wrong(def.erroreMsg);
                UIFx.Shake(body, 6f, 0.3f);
                StartCoroutine(ResetWheels());
            }
        }

        IEnumerator ResetWheels()
        {
            locked = true;
            yield return new WaitForSecondsRealtime(0.25f);
            bool any = true;
            while (any)
            {
                any = false;
                for (int i = 0; i < n; i++)
                {
                    if (digits[i] == 0) continue;
                    any = true;
                    int dir = digits[i] <= 5 ? -1 : 1;
                    Roll(i, (digits[i] + dir + 10) % 10, dir);
                }
                yield return new WaitForSecondsRealtime(0.07f);
            }
            locked = false;
        }

        protected override void OnUpdate()
        {
            if (solved || digits == null || locked) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            for (int k = 0; k < 10; k++)
            {
                var d = kb[k == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + k - 1)]; // enum: Digit1..Digit9, Digit0
                var p = kb[(Key)((int)Key.Numpad0 + k)];
                if (d.wasPressedThisFrame || p.wasPressedThisFrame)
                {
                    Roll(sel, k, 1);
                    Select(sel + 1);
                }
            }
            if (kb.backspaceKey.wasPressedThisFrame) Select(sel - 1);
            if (kb.leftArrowKey.wasPressedThisFrame) Select(sel - 1);
            if (kb.rightArrowKey.wasPressedThisFrame) Select(sel + 1);
            if (kb.upArrowKey.wasPressedThisFrame) Change(sel, 1);
            if (kb.downArrowKey.wasPressedThisFrame) Change(sel, -1);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) TryOpen();
        }
    }
}
