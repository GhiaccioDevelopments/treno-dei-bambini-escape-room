using System;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// Base dei pannelli modali degli enigmi: carta su cornice d'ottone, titolo con numero romano, consegna, area contenuto, feedback,
    /// pulsanti Indizio / Chiudi / azione. Errore = oscillazione gentile + messaggio + riprova (mai rosso aggressivo);
    /// successo = timbro "RISOLTO" che sbatte sul pannello, poi la spiegazione a macchina da scrivere e "Avanti".
    /// </summary>
    public abstract class PuzzlePanel : MonoBehaviour
    {
        protected EnigmaDef def;
        protected RectTransform content;
        protected RectTransform card;
        protected bool solved;

        Action onSolved;
        TextMeshProUGUI feedback;
        Button actionBtn, hintBtn, closeBtn;
        Action actionCb;
        int hintIndex;
        bool closed;
        PanelFx fx;

        public static readonly Color Ok = UITheme.Esatto;
        static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        /// <summary>Crea il pannello adatto al tipo. Null se il tipo non è gestito da un pannello (es. nascosto).</summary>
        public static PuzzlePanel Open(EnigmaDef def, Action onSolved)
        {
            if (def == null || UIRoot.I == null) return null;
            Type t;
            switch ((def.tipo ?? "").ToLowerInvariant())
            {
                case "quiz": t = typeof(QuizPanel); break;
                case "lucchetto": t = typeof(LucchettoPanel); break;
                case "ordina": t = typeof(OrdinaPanel); break;
                case "abbina": t = typeof(AbbinaPanel); break;
                default:
                    Debug.LogWarning("Tipo di enigma non gestito dal pannello: " + def.tipo);
                    return null;
            }
            var go = new GameObject("Pannello_" + def.tipo, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(UIRoot.I.ModalLayer, false);
            var p = (PuzzlePanel)go.AddComponent(t);
            p.Init(def, onSolved);
            return p;
        }

        void Init(EnigmaDef d, Action solvedCb)
        {
            def = d;
            onSolved = solvedCb;
            if (GameManager.I != null) GameManager.I.PushBlock();
            UIRoot.I.PushModal();
            UIRoot.I.HidePrompt();
            BuildFrame();
            BuildContent(content);
            fx.Show();
        }

        void BuildFrame()
        {
            var self = (RectTransform)transform;
            UIFactory.Stretch(self);
            var veil = UIFactory.MakePanel(self, "Velo", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            fx = veil.gameObject.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Pop;

            var cardImg = UIFactory.MakePaper(veil.rectTransform, "Carta", Color.white, true);
            UIFactory.Place(cardImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 680f));
            card = cardImg.rectTransform;
            fx.card = card;
            UIFactory.MakeBrassFrame(card, 8f);
            var cr = card;

            string numero = "";
            var ctrl = FindAnyObjectByType<EscapeRoomController>();
            if (ctrl != null)
            {
                int idx = ctrl.IndexOfEnigma(def);
                if (idx >= 0) numero = Roman[Mathf.Min(idx, Roman.Length - 1)] + " · ";
            }
            var title = UIFactory.MakeText(cr, numero + (def.titolo ?? ""), 36, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(30f, -70f); title.rectTransform.offsetMax = new Vector2(-30f, -24f);

            var consegna = UIFactory.MakeText(cr, def.consegna ?? "", 24, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            consegna.rectTransform.anchorMin = new Vector2(0f, 1f); consegna.rectTransform.anchorMax = new Vector2(1f, 1f);
            consegna.rectTransform.pivot = new Vector2(0.5f, 1f);
            consegna.rectTransform.offsetMin = new Vector2(48f, -170f); consegna.rectTransform.offsetMax = new Vector2(-48f, -76f);
            consegna.enableAutoSizing = true; consegna.fontSizeMin = 16; consegna.fontSizeMax = 24;

            content = UIFactory.NewRect("Contenuto", cr);
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(48f, 190f); content.offsetMax = new Vector2(-48f, -175f);

            feedback = UIFactory.MakeText(cr, "", 24, UITheme.Inchiostro, TextAlignmentOptions.Center);
            feedback.rectTransform.anchorMin = new Vector2(0f, 0f); feedback.rectTransform.anchorMax = new Vector2(1f, 0f);
            feedback.rectTransform.pivot = new Vector2(0.5f, 0f);
            feedback.rectTransform.offsetMin = new Vector2(48f, 100f); feedback.rectTransform.offsetMax = new Vector2(-48f, 184f);
            feedback.enableAutoSizing = true; feedback.fontSizeMin = 14; feedback.fontSizeMax = 24;

            bool hasHints = def.indizi != null && def.indizi.Count > 0;
            hintBtn = UIFactory.MakeButton(cr, "Indizio", ShowHint, new Vector2(200f, 64f), ButtonStyle.Tag, 26f, hasHints);
            UIFactory.Place((RectTransform)hintBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 24f), new Vector2(200f, 64f));
            var lens = UIFactory.MakeImage(hintBtn.transform, "Lente", UIArt.IconLens(), Color.white);
            UIFactory.Place(lens.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(38f, 38f));
            UIFactory.Stretch(hintBtn.GetComponentInChildren<TextMeshProUGUI>().rectTransform, 50f, 6f, 10f, 6f);
            closeBtn = UIFactory.MakeButton(cr, "Chiudi", Close, new Vector2(200f, 64f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)closeBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(250f, 24f), new Vector2(200f, 64f));
            actionBtn = UIFactory.MakeButton(cr, "Avanti", OnActionClicked, new Vector2(280f, 64f), ButtonStyle.Seal, 28f, true);
            UIFactory.Place((RectTransform)actionBtn.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 24f), new Vector2(280f, 64f));
            actionBtn.gameObject.SetActive(false);
        }

        protected abstract void BuildContent(RectTransform area);

        /// <summary>Hook per aggiornare il contenuto quando l'enigma è risolto.</summary>
        protected virtual void OnSolvedVisual() { }
        protected virtual void OnUpdate() { }

        void Update()
        {
            if (closed) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (solved) FinishSolved(); else Close();
                return;
            }
            OnUpdate();
        }

        // ---------------------------------------------------------------- API per i pannelli derivati
        protected void ShowAction(string label, Action cb)
        {
            actionCb = cb;
            UIFactory.SetLabel(actionBtn, label);
            actionBtn.gameObject.SetActive(true);
        }

        protected void HideAction()
        {
            actionCb = null;
            actionBtn.gameObject.SetActive(false);
        }

        void OnActionClicked()
        {
            var cb = actionCb;
            if (cb != null) cb();
        }

        protected void SetFeedback(string text, Color color)
        {
            feedback.color = color;
            Typewriter.Play(feedback, text ?? "", 90f, false);
        }

        protected void Info(string text) { SetFeedback(text, UIFactory.Inchiostro); }

        /// <summary>Errore gentile: oscillazione del pannello, suono morbido, messaggio (specifico, dell'enigma o generico).</summary>
        protected void Wrong(string specific)
        {
            string m = !string.IsNullOrEmpty(specific) ? specific
                : (!string.IsNullOrEmpty(def.erroreMsg) ? def.erroreMsg : "Non è ancora quello giusto, ma non preoccuparti: riprova!");
            SetFeedback(m, UITheme.Ceralacca);
            UISfx.Play(Sfx.SoftError, 0.6f);
            UIFx.Wobble(card, 0.8f, 0.5f);
        }

        protected void Solved(string prefix = null)
        {
            if (solved) return;
            solved = true;
            string t = prefix ?? "";
            if (!string.IsNullOrEmpty(def.spiegazione)) t += (t.Length > 0 ? "\n" : "") + def.spiegazione;
            if (!string.IsNullOrEmpty(def.temaCivico)) t += (t.Length > 0 ? "\n" : "") + "Educazione civica: " + def.temaCivico;
            if (t.Length == 0) t = "Enigma risolto!";
            hintBtn.gameObject.SetActive(false);
            closeBtn.gameObject.SetActive(false);
            SetContentInteractable(false);
            OnSolvedVisual();
            // il timbro sbatte, poi la spiegazione si scrive
            string text = t;
            SlamStamp(card, "RISOLTO", UITheme.InchiostroBlu, new Vector2(330f, 110f), new Vector2(240f, 40f), -9f, 54f, () =>
            {
                if (closed) return;
                SetFeedback(text, Ok);
                ShowAction("Avanti", FinishSolved);
            });
        }

        /// <summary>Timbro a inchiostro che sbatte su "parent" (scossa dello schermo, polvere, suono). Ritorna il rettangolo del timbro.</summary>
        public static RectTransform SlamStamp(RectTransform parent, string text, Color color, Vector2 size, Vector2 pos, float rotation, float fontSize, Action done)
        {
            var img = UIFactory.MakePanel(parent, "Timbro", new Color(color.r, color.g, color.b, 0.88f), false);
            img.sprite = UIArt.StampFrame(); img.type = Image.Type.Sliced;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var lbl = UIFactory.MakeText(rt, text, fontSize, color, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(lbl.rectTransform, 10f, 4f, 10f, 4f);
            lbl.enableAutoSizing = true; lbl.fontSizeMin = 14; lbl.fontSizeMax = fontSize;
            lbl.characterSpacing = 6f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();

            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            if (UITheme.ReduceEffects)
            {
                cg.alpha = 0f;
                UITween.Alpha(cg, 1f, 0.15f, Ease.OutCubic, done);
                return rt;
            }
            cg.alpha = 0f;
            rt.localScale = new Vector3(2.4f, 2.4f, 1f);
            UITween.Run(rt, 0.22f, Ease.InCubic, v =>
            {
                if (rt == null) return;
                float s = Mathf.Lerp(2.4f, 1f, v);
                rt.localScale = new Vector3(s, s, 1f);
                cg.alpha = Mathf.Clamp01(v * 2f);
            }, () =>
            {
                if (rt == null) return;
                rt.localScale = Vector3.one;
                UISfx.Play(Sfx.Stamp, 0.9f);
                UIFx.Shake(parent, 7f, 0.25f);
                UIParticles.BurstAtRect(ParticleKind.Dust, rt, 6);
                UIParticles.BurstAtRect(ParticleKind.Ink, rt, 5);
                UIFx.Pop(rt, 1.06f, 0.2f);
                if (done != null) UITween.After(rt, 0.35f, done);
            });
            return rt;
        }

        protected void SetContentInteractable(bool value)
        {
            var btns = content.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < btns.Length; i++) btns[i].interactable = value;
        }

        // ---------------------------------------------------------------- indizi / chiusura
        void ShowHint()
        {
            if (def.indizi == null || def.indizi.Count == 0)
            {
                Info("Per questo enigma non ci sono indizi: rileggi con calma la consegna.");
                return;
            }
            int i = hintIndex % def.indizi.Count;
            hintIndex++;
            UISfx.Play(Sfx.Sparkle, 0.4f);
            Info("Indizio " + (i + 1) + "/" + def.indizi.Count + ": " + def.indizi[i]);
        }

        void FinishSolved()
        {
            var cb = onSolved;
            onSolved = null;
            Close();
            if (cb != null) cb();
        }

        protected void Close()
        {
            if (closed) return;
            closed = true;
            if (GameManager.I != null) GameManager.I.PopBlock();
            if (UIRoot.I != null) UIRoot.I.PopModal();
            if (fx != null) fx.Hide(() => { if (this != null) Destroy(gameObject); });
            else Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (closed) return;
            closed = true;
            if (GameManager.I != null) GameManager.I.PopBlock();
            if (UIRoot.I != null) UIRoot.I.PopModal();
        }
    }
}
