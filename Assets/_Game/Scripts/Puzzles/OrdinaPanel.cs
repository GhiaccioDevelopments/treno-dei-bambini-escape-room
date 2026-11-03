using System.Collections.Generic;
using TMPro;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>Ordina gli eventi (carte con frecce Su/Giù). Verifica dice quante carte sono al posto giusto.</summary>
    public class OrdinaPanel : PuzzlePanel
    {
        readonly List<int> order = new List<int>(); // indici degli eventi nell'ordine mostrato
        RectTransform area;

        protected override void BuildContent(RectTransform a)
        {
            area = a;
            int n = def.eventi != null ? def.eventi.Count : 0;
            if (n == 0)
            {
                Debug.LogWarning("Enigma 'ordina' senza eventi: " + def.id);
                Solved();
                return;
            }
            for (int i = 0; i < n; i++) order.Add(i);
            Shuffle();
            Rebuild();
            ShowAction("Verifica", Verify);
            Info("Metti le carte in ordine cronologico con Su e Giù, poi premi Verifica.");
        }

        void Shuffle()
        {
            int n = order.Count;
            if (n < 2) return;
            for (int tries = 0; tries < 20; tries++)
            {
                for (int i = n - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
                }
                if (CorrectCount() < n) return;
            }
            int t = order[0]; order[0] = order[1]; order[1] = t;
        }

        int CorrectCount()
        {
            int c = 0;
            for (int i = 0; i < order.Count; i++) if (order[i] == i) c++;
            return c;
        }

        void Rebuild()
        {
            UIFactory.Clear(area);
            int n = order.Count;
            float availH = area.rect.height > 50f ? area.rect.height : 320f;
            float h = Mathf.Clamp(availH / n - 6f, 36f, 58f);
            float w = area.rect.width > 50f ? area.rect.width : 1000f;

            for (int i = 0; i < n; i++)
            {
                int pos = i;
                var e = def.eventi[order[i]];

                var row = UIFactory.MakePanel(area, "Carta" + i, solved ? new Color(0.78f, 0.88f, 0.7f) : UIFactory.CartaScura, false);
                var rt = row.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * (h + 6f));
                rt.sizeDelta = new Vector2(0f, h);

                var num = UIFactory.MakeText(rt, (i + 1).ToString(), 26, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold);
                num.rectTransform.anchorMin = new Vector2(0f, 0f); num.rectTransform.anchorMax = new Vector2(0f, 1f);
                num.rectTransform.pivot = new Vector2(0f, 0.5f);
                num.rectTransform.offsetMin = new Vector2(8f, 0f); num.rectTransform.offsetMax = new Vector2(54f, 0f);

                string label = solved && !string.IsNullOrEmpty(e.data) ? e.data + " - " + e.testo : e.testo;
                var txt = UIFactory.MakeText(rt, label, 24, UIFactory.Inchiostro, TextAlignmentOptions.Left);
                UIFactory.Stretch(txt.rectTransform, 60f, 2f, 220f, 2f);
                txt.enableAutoSizing = true; txt.fontSizeMin = 14; txt.fontSizeMax = 24;

                var down = UIFactory.MakeButton(rt, "Giù", () => Move(pos, 1), new Vector2(100f, h - 6f), UIFactory.Marrone, null, 22);
                var dr = (RectTransform)down.transform;
                dr.anchorMin = new Vector2(1f, 0.5f); dr.anchorMax = new Vector2(1f, 0.5f); dr.pivot = new Vector2(1f, 0.5f);
                dr.anchoredPosition = new Vector2(-6f, 0f); dr.sizeDelta = new Vector2(100f, h - 6f);
                var up = UIFactory.MakeButton(rt, "Su", () => Move(pos, -1), new Vector2(100f, h - 6f), UIFactory.Marrone, null, 22);
                var ur = (RectTransform)up.transform;
                ur.anchorMin = new Vector2(1f, 0.5f); ur.anchorMax = new Vector2(1f, 0.5f); ur.pivot = new Vector2(1f, 0.5f);
                ur.anchoredPosition = new Vector2(-112f, 0f); ur.sizeDelta = new Vector2(100f, h - 6f);

                if (i == 0) up.interactable = false;
                if (i == n - 1) down.interactable = false;
                if (solved) { up.interactable = false; down.interactable = false; }
            }
        }

        void Move(int pos, int delta)
        {
            if (solved) return;
            int to = pos + delta;
            if (to < 0 || to >= order.Count) return;
            int t = order[pos]; order[pos] = order[to]; order[to] = t;
            Rebuild();
        }

        void Verify()
        {
            if (solved) return;
            int c = CorrectCount();
            if (c == order.Count) Solved("Ordine perfetto!");
            else
            {
                string m = "Hai messo " + c + " carte su " + order.Count + " al posto giusto. Riprova!";
                if (!string.IsNullOrEmpty(def.erroreMsg)) m += "\n" + def.erroreMsg;
                Wrong(m);
            }
        }

        protected override void OnSolvedVisual() { Rebuild(); }
    }
}
