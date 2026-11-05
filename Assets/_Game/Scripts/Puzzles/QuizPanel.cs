using System.Collections.Generic;
using TMPro;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>Quiz a risposta multipla: una domanda alla volta, errore gentile e riprova.</summary>
    public class QuizPanel : PuzzlePanel
    {
        int q;
        readonly List<Button> options = new List<Button>();

        protected override void BuildContent(RectTransform area)
        {
            if (def.domande == null || def.domande.Count == 0)
            {
                Debug.LogWarning("Quiz senza domande: " + def.id);
                Solved();
                return;
            }
            UIFactory.AddVLayout(content.gameObject, 8f);
            ShowQuestion(0);
        }

        void ShowQuestion(int index)
        {
            q = index;
            UIFactory.Clear(content);
            options.Clear();
            HideAction();
            var d = def.domande[q];

            var head = UIFactory.MakeText(content, "Domanda " + (q + 1) + " di " + def.domande.Count + ": " + d.testo,
                26, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            head.rectTransform.sizeDelta = new Vector2(content.rect.width > 10f ? content.rect.width : 1000f, 64f);
            head.enableAutoSizing = true; head.fontSizeMin = 16; head.fontSizeMax = 26;

            int n = d.opzioni != null ? d.opzioni.Count : 0;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var b = UIFactory.MakeButton(content, d.opzioni[i], () => OnOption(idx), new Vector2(1000f, 56f), UIFactory.Marrone, null, 24);
                options.Add(b);
            }
            if (n == 0) { Debug.LogWarning("Domanda senza opzioni: " + d.testo); NextOrSolve(); }
            Info("");
        }

        void OnOption(int i)
        {
            var d = def.domande[q];
            if (i == d.corretta)
            {
                for (int k = 0; k < options.Count; k++) options[k].interactable = false;
                UIFactory.SetBg(options[i], new Color(0.75f, 0.9f, 0.7f)); // evidenzia la risposta giusta
                var ort = (RectTransform)options[i].transform;
                SlamStamp(ort, "ESATTO", Ok, new Vector2(200f, 44f), new Vector2(ort.rect.width * 0.5f - 130f, 0f), -6f, 30f, null);
                UISfx.Play(Sfx.Chime, 0.5f);
                UIParticles.BurstAtRect(ParticleKind.Spark, ort, 8);
                string expl = string.IsNullOrEmpty(d.spiegazione) ? "Esatto!" : "Esatto! " + d.spiegazione;
                if (q >= def.domande.Count - 1)
                    Solved(expl);
                else
                {
                    SetFeedback(expl, Ok);
                    ShowAction("Prossima domanda", () => ShowQuestion(q + 1));
                }
            }
            else
            {
                options[i].interactable = false;
                var wrt = (RectTransform)options[i].transform;
                UIFx.Wobble(wrt, 3f, 0.5f);
                var wt = options[i].GetComponentInChildren<TMP_Text>();
                if (wt != null) UITween.Run(wt, 0.6f, Ease.OutCubic, v => { if (wt != null) { var c = wt.color; c.a = Mathf.Lerp(1f, 0.4f, v); wt.color = c; } });
                Wrong(!string.IsNullOrEmpty(d.erroreMsg) ? d.erroreMsg : null);
            }
        }

        void NextOrSolve()
        {
            if (q >= def.domande.Count - 1) Solved(); else ShowQuestion(q + 1);
        }
    }
}
