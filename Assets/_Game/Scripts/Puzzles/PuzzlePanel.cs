using System;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// Base dei pannelli modali degli enigmi: titolo, consegna, area contenuto, feedback, pulsanti Indizio / Chiudi / azione.
    /// Errore = messaggio gentile + riprova; successo = spiegazione + "Avanti".
    /// </summary>
    public abstract class PuzzlePanel : MonoBehaviour
    {
        protected EnigmaDef def;
        protected RectTransform content;
        protected bool solved;

        Action onSolved;
        TextMeshProUGUI feedback;
        Button actionBtn, hintBtn, closeBtn;
        Action actionCb;
        int hintIndex;
        bool closed;

        public static readonly Color Ok = new Color(0.2f, 0.42f, 0.18f);

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
        }

        void BuildFrame()
        {
            var self = (RectTransform)transform;
            UIFactory.Stretch(self);
            var veil = UIFactory.MakePanel(self, "Velo", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);

            var card = UIFactory.MakePanel(self, "Carta", UIFactory.Carta, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 680f));
            var cr = card.rectTransform;

            var title = UIFactory.MakeText(cr, def.titolo ?? "", 36, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(30f, -70f); title.rectTransform.offsetMax = new Vector2(-30f, -16f);

            var consegna = UIFactory.MakeText(cr, def.consegna ?? "", 24, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft);
            consegna.rectTransform.anchorMin = new Vector2(0f, 1f); consegna.rectTransform.anchorMax = new Vector2(1f, 1f);
            consegna.rectTransform.pivot = new Vector2(0.5f, 1f);
            consegna.rectTransform.offsetMin = new Vector2(40f, -170f); consegna.rectTransform.offsetMax = new Vector2(-40f, -76f);
            consegna.enableAutoSizing = true; consegna.fontSizeMin = 16; consegna.fontSizeMax = 24;

            content = UIFactory.NewRect("Contenuto", cr);
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(40f, 190f); content.offsetMax = new Vector2(-40f, -175f);

            feedback = UIFactory.MakeText(cr, "", 24, UIFactory.Inchiostro, TextAlignmentOptions.Center);
            feedback.rectTransform.anchorMin = new Vector2(0f, 0f); feedback.rectTransform.anchorMax = new Vector2(1f, 0f);
            feedback.rectTransform.pivot = new Vector2(0.5f, 0f);
            feedback.rectTransform.offsetMin = new Vector2(40f, 100f); feedback.rectTransform.offsetMax = new Vector2(-40f, 184f);
            feedback.enableAutoSizing = true; feedback.fontSizeMin = 14; feedback.fontSizeMax = 24;

            hintBtn = UIFactory.MakeButton(cr, "Indizio", ShowHint, new Vector2(200f, 64f), UIFactory.Marrone);
            UIFactory.Place((RectTransform)hintBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 22f), new Vector2(200f, 64f));
            closeBtn = UIFactory.MakeButton(cr, "Chiudi", Close, new Vector2(200f, 64f), UIFactory.Marrone);
            UIFactory.Place((RectTransform)closeBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(246f, 22f), new Vector2(200f, 64f));
            actionBtn = UIFactory.MakeButton(cr, "Avanti", OnActionClicked, new Vector2(280f, 64f));
            UIFactory.Place((RectTransform)actionBtn.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(280f, 64f));
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
            feedback.text = text ?? "";
            feedback.color = color;
        }

        protected void Info(string text) { SetFeedback(text, UIFactory.Inchiostro); }

        /// <summary>Errore gentile (prima il messaggio specifico, poi quello dell'enigma, poi uno generico).</summary>
        protected void Wrong(string specific)
        {
            string m = !string.IsNullOrEmpty(specific) ? specific
                : (!string.IsNullOrEmpty(def.erroreMsg) ? def.erroreMsg : "Non è ancora quello giusto, ma non preoccuparti: riprova!");
            SetFeedback(m, UIFactory.Accento);
        }

        protected void Solved(string prefix = null)
        {
            if (solved) return;
            solved = true;
            string t = prefix ?? "";
            if (!string.IsNullOrEmpty(def.spiegazione)) t += (t.Length > 0 ? "\n" : "") + def.spiegazione;
            if (!string.IsNullOrEmpty(def.temaCivico)) t += (t.Length > 0 ? "\n" : "") + "Educazione civica: " + def.temaCivico;
            if (t.Length == 0) t = "Enigma risolto!";
            SetFeedback(t, Ok);
            hintBtn.gameObject.SetActive(false);
            closeBtn.gameObject.SetActive(false);
            SetContentInteractable(false);
            OnSolvedVisual();
            ShowAction("Avanti", FinishSolved);
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
            Destroy(gameObject);
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
