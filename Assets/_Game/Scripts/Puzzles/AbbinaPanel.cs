using System.Collections.Generic;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>Abbina: colonna sinistra e destra (mescolata), si selezionano a coppie.</summary>
    public class AbbinaPanel : PuzzlePanel
    {
        Button[] left, right;
        bool[] matchedLeft, matchedRight;
        int[] rightOrder;   // posizione colonna destra -> indice coppia
        int selLeft = -1, selRight = -1;
        int matches;

        static readonly Color Sel = new Color(0.8f, 0.55f, 0.25f);

        protected override void BuildContent(RectTransform area)
        {
            int n = def.coppie != null ? def.coppie.Count : 0;
            if (n == 0)
            {
                Debug.LogWarning("Enigma 'abbina' senza coppie: " + def.id);
                Solved();
                return;
            }
            left = new Button[n]; right = new Button[n];
            matchedLeft = new bool[n]; matchedRight = new bool[n];
            rightOrder = new int[n];
            for (int i = 0; i < n; i++) rightOrder[i] = i;
            for (int tries = 0; tries < 20; tries++)
            {
                for (int i = n - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int t = rightOrder[i]; rightOrder[i] = rightOrder[j]; rightOrder[j] = t;
                }
                bool same = true;
                for (int i = 0; i < n; i++) if (rightOrder[i] != i) { same = false; break; }
                if (!same || n < 2) break;
            }

            float availH = area.rect.height > 50f ? area.rect.height : 320f;
            float h = Mathf.Clamp(availH / n - 8f, 56f, 64f);

            var colL = UIFactory.NewRect("ColonnaSx", area);
            colL.anchorMin = new Vector2(0f, 0f); colL.anchorMax = new Vector2(0.5f, 1f);
            colL.offsetMin = Vector2.zero; colL.offsetMax = new Vector2(-16f, 0f);
            var colR = UIFactory.NewRect("ColonnaDx", area);
            colR.anchorMin = new Vector2(0.5f, 0f); colR.anchorMax = new Vector2(1f, 1f);
            colR.offsetMin = new Vector2(16f, 0f); colR.offsetMax = Vector2.zero;

            for (int i = 0; i < n; i++)
            {
                int li = i;
                left[i] = MakeRow(colL, def.coppie[i].sinistra, i, h, () => ClickLeft(li));
                int ri = i;
                right[i] = MakeRow(colR, def.coppie[rightOrder[i]].destra, i, h, () => ClickRight(ri));
            }
            Info("Tocca un elemento a sinistra e poi quello che gli corrisponde a destra.");
        }

        static Button MakeRow(RectTransform col, string label, int i, float h, UnityEngine.Events.UnityAction cb)
        {
            var b = UIFactory.MakeButton(col, label, cb, new Vector2(0f, h), UIFactory.Marrone, null, 22);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -i * (h + 8f));
            rt.sizeDelta = new Vector2(0f, h);
            return b;
        }

        void ClickLeft(int i)
        {
            if (solved || matchedLeft[i]) return;
            if (selLeft >= 0) UIFactory.SetBg(left[selLeft], UIFactory.Marrone);
            selLeft = i;
            UIFactory.SetBg(left[i], Sel);
            TryMatch();
        }

        void ClickRight(int j)
        {
            if (solved || matchedRight[j]) return;
            if (selRight >= 0) UIFactory.SetBg(right[selRight], UIFactory.Marrone);
            selRight = j;
            UIFactory.SetBg(right[j], Sel);
            TryMatch();
        }

        void TryMatch()
        {
            if (selLeft < 0 || selRight < 0) return;
            int l = selLeft, r = selRight;
            selLeft = -1; selRight = -1;

            if (rightOrder[r] == l)
            {
                matchedLeft[l] = true; matchedRight[r] = true;
                UIFactory.SetBg(left[l], Ok); UIFactory.SetBg(right[r], Ok);
                left[l].interactable = false; right[r].interactable = false;
                matches++;
                if (matches >= left.Length) Solved("Tutte le coppie sono giuste!");
                else SetFeedback("Giusto! Ne mancano " + (left.Length - matches) + ".", Ok);
            }
            else
            {
                UIFactory.SetBg(left[l], UIFactory.Marrone);
                UIFactory.SetBg(right[r], UIFactory.Marrone);
                Wrong(def.erroreMsg);
            }
        }
    }
}
