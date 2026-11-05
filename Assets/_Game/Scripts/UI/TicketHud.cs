using System.Collections.Generic;
using TMPro;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Contatore enigmi a biglietto del treno: una casella per enigma; quando uno è risolto la punzonatrice buca la casella
    /// (animazione + suono + coriandoli di carta). Sostituisce il vecchio testo "Enigmi: n/4".
    /// </summary>
    public class TicketHud : MonoBehaviour
    {
        static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        RectTransform ticket;
        TextMeshProUGUI extraLabel;
        RectTransform boxRow;
        readonly List<Image> holes = new List<Image>();
        readonly List<RectTransform> boxes = new List<RectTransform>();
        int total = -1;
        int punched;
        bool shown;

        public static TicketHud Create(RectTransform parent)
        {
            var rt = UIFactory.NewRect("Biglietto", parent);
            var t = rt.gameObject.AddComponent<TicketHud>();
            t.ticket = rt;
            t.Build();
            rt.gameObject.SetActive(false);
            return t;
        }

        void Build()
        {
            var img = ticket.gameObject.AddComponent<Image>();
            img.sprite = UIArt.Ticket();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            UIFactory.Place(ticket, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(300f, 100f));

            var title = UIFactory.MakeText(ticket, "NAPOLI  →  RICORDO", 15, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Stretch(title.rectTransform, 40f, 0f, 14f, 10f);
            title.characterSpacing = 4f;

            boxRow = UIFactory.NewRect("Caselle", ticket);
            boxRow.anchorMin = new Vector2(0f, 0f); boxRow.anchorMax = new Vector2(0f, 1f);
            boxRow.pivot = new Vector2(0f, 0.5f);
            boxRow.anchoredPosition = new Vector2(40f, -4f);
            boxRow.sizeDelta = new Vector2(10f, -52f);

            extraLabel = UIFactory.MakeText(ticket, "", 17, UITheme.Inchiostro, TextAlignmentOptions.BottomLeft);
            UIFactory.Stretch(extraLabel.rectTransform, 40f, 6f, 14f, 0f);

            // dicitura sul matrice
            var stub = UIFactory.MakeText(ticket, "N.", 14, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(stub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(22f, 30f));
        }

        void Rebuild(int n)
        {
            UIFactory.Clear(boxRow);
            holes.Clear(); boxes.Clear();
            total = n;
            const float box = 44f, gap = 8f;
            for (int i = 0; i < n; i++)
            {
                var b = UIFactory.MakePanel(boxRow, "Casella" + i, new Color(1f, 1f, 1f, 0.8f), false);
                b.sprite = UIArt.PaperDark();
                b.type = Image.Type.Sliced;
                UIFactory.Place(b.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (box + gap), 0f), new Vector2(box, box));
                var num = UIFactory.MakeText(b.rectTransform, Roman[Mathf.Min(i, Roman.Length - 1)], 20, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
                UIFactory.Stretch(num.rectTransform);
                var hole = UIFactory.MakePanel(b.rectTransform, "Foro", new Color(0.12f, 0.08f, 0.05f, 1f), false);
                hole.sprite = UIFactory.Circle();
                UIFactory.Place(hole.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
                hole.gameObject.SetActive(false);
                holes.Add(hole);
                boxes.Add(b.rectTransform);
            }
            float w = 52f + n * (box + gap);
            ticket.sizeDelta = new Vector2(Mathf.Max(260f, w + 20f), 100f);
        }

        /// <summary>solved/total enigmi; extra = riga di testo sotto (es. oggetti trovati). total &lt;= 0 nasconde il biglietto.</summary>
        public void Set(int solved, int totalCount, string extra)
        {
            if (totalCount <= 0) { Hide(); return; }
            bool first = !shown;
            if (first)
            {
                shown = true;
                ticket.gameObject.SetActive(true);
                total = -1;
            }
            if (total != totalCount) { Rebuild(totalCount); punched = 0; }
            if (first)
            {
                // già risolti prima di entrare: caselle bucate senza animazione
                for (int i = 0; i < solved && i < holes.Count; i++) holes[i].gameObject.SetActive(true);
                punched = Mathf.Min(solved, holes.Count);
                ticket.anchoredPosition = new Vector2(UITheme.ReduceEffects ? 16f : -420f, -16f);
                UITween.Cancel(ticket);
                UITween.Move(ticket, new Vector2(16f, -16f), 0.45f, Ease.OutCubic, null, 0.25f);
            }
            else if (solved < punched)
            {
                for (int i = solved; i < holes.Count; i++) holes[i].gameObject.SetActive(false);
                punched = solved;
            }
            else
            {
                int s = Mathf.Min(solved, holes.Count);
                for (int i = punched; i < s; i++) Punch(i, (i - punched) * 0.3f);
                punched = s;
            }
            extraLabel.text = extra ?? "";
        }

        void Punch(int i, float delay)
        {
            var hole = holes[i];
            var box = boxes[i];
            UITween.After(hole, delay, () =>
            {
                if (hole == null) return;
                UISfx.Play(Sfx.Punch, 0.8f);
                hole.gameObject.SetActive(true);
                var hr = hole.rectTransform;
                hr.localScale = Vector3.zero;
                UITween.Scale(hr, Vector3.one, 0.25f, Ease.OutBack);
                UIFx.Pop(box, 1.25f, 0.4f);
                UIParticles.BurstAtRect(ParticleKind.Paper, box, 8);
            });
        }

        public void Hide()
        {
            shown = false;
            total = -1;
            punched = 0;
            UITween.Cancel(ticket);
            if (ticket != null) ticket.gameObject.SetActive(false);
        }

        /// <summary>Per compatibilità col vecchio SetHudCounter(testo): solo una riga di testo, senza caselle.</summary>
        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text)) { Hide(); return; }
            Set(0, 0, text); // 0 caselle => nasconde; usiamo comunque la riga extra
            shown = true;
            ticket.gameObject.SetActive(true);
            Rebuild(0);
            extraLabel.text = text;
            ticket.sizeDelta = new Vector2(Mathf.Max(260f, extraLabel.GetPreferredValues(text).x + 60f), 100f);
        }
    }
}
