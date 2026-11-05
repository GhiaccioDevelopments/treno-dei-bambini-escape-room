using System.Collections.Generic;
using TMPro;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// Ordina gli eventi trascinando le carte con mouse o dito (drag and drop): la carta sollevata proietta un'ombra
    /// e le altre si spostano per fare spazio. Per l'accessibilità da tastiera: clic sulla carta per sceglierla, poi frecce Su/Giù (o W/S).
    /// Verifica: le carte al posto giusto ricevono una puntina; nessun rosso aggressivo per quelle sbagliate.
    /// </summary>
    public class OrdinaPanel : PuzzlePanel
    {
        const float Gap = 6f;

        readonly List<int> order = new List<int>(); // order[posizione] = indice dell'evento
        RectTransform area;
        float rowH;
        RectTransform[] rows;
        Image[] bgs, shadows, pins;
        TextMeshProUGUI[] labels, numbers;
        int dragEvent = -1, selectedEvent = -1;
        float grabOffset;

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
            BuildRows();
            ShowAction("Verifica", Verify);
            Info("Trascina le carte per metterle in ordine cronologico, poi premi Verifica.");
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

        Vector2 Slot(int pos) { return new Vector2(0f, -pos * (rowH + Gap)); }

        void BuildRows()
        {
            int n = order.Count;
            float availH = area.rect.height > 50f ? area.rect.height : 320f;
            rowH = Mathf.Clamp(availH / n - Gap, 40f, 62f);
            rows = new RectTransform[n]; bgs = new Image[n]; shadows = new Image[n]; pins = new Image[n];
            labels = new TextMeshProUGUI[n]; numbers = new TextMeshProUGUI[n];

            for (int e = 0; e < n; e++)
            {
                var ev = def.eventi[e];
                var shadow = UIFactory.MakePanel(area, "Ombra" + e, new Color(0f, 0f, 0f, 0.18f), false);
                shadow.sprite = UIArt.PaperDark(); shadow.type = Image.Type.Sliced;
                shadows[e] = shadow;

                var bg = UIFactory.MakePanel(area, "Carta" + e, new Color(1f, 0.97f, 0.88f), true);
                bg.sprite = UIArt.PaperDark(); bg.type = Image.Type.Sliced;
                bgs[e] = bg;
                var rt = bg.rectTransform;
                rows[e] = rt;
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, rowH);
                rt.anchoredPosition = Slot(order.IndexOf(e));

                // maniglia a tre righette: dice "si può trascinare"
                for (int g = 0; g < 3; g++)
                {
                    var line = UIFactory.MakePanel(rt, "Presa", new Color(UITheme.Inchiostro.r, UITheme.Inchiostro.g, UITheme.Inchiostro.b, 0.35f), false);
                    line.sprite = UIArt.White();
                    UIFactory.Place(line.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, (g - 1) * 7f), new Vector2(18f, 3f));
                }
                numbers[e] = UIFactory.MakeText(rt, "", 26, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
                numbers[e].rectTransform.anchorMin = new Vector2(0f, 0f); numbers[e].rectTransform.anchorMax = new Vector2(0f, 1f);
                numbers[e].rectTransform.pivot = new Vector2(0f, 0.5f);
                numbers[e].rectTransform.offsetMin = new Vector2(38f, 0f); numbers[e].rectTransform.offsetMax = new Vector2(84f, 0f);
                labels[e] = UIFactory.MakeText(rt, ev.testo, 24, UITheme.Inchiostro, TextAlignmentOptions.Left);
                UIFactory.Stretch(labels[e].rectTransform, 92f, 2f, 40f, 2f);
                labels[e].enableAutoSizing = true; labels[e].fontSizeMin = 14; labels[e].fontSizeMax = 24;

                var pin = UIFactory.MakePanel(rt, "Puntina", UITheme.Ceralacca, false);
                pin.sprite = UIFactory.Circle();
                UIFactory.Place(pin.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(22f, 22f));
                var hl = UIFactory.MakePanel(pin.rectTransform, "Riflesso", new Color(1f, 0.8f, 0.75f, 0.8f), false);
                hl.sprite = UIFactory.Circle();
                UIFactory.Place(hl.rectTransform, new Vector2(0.35f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7f, 7f));
                pin.gameObject.SetActive(false);
                pins[e] = pin;

                var card = bg.gameObject.AddComponent<OrdinaCard>();
                card.owner = this; card.eventIndex = e;
                SyncShadow(e, false);
            }
            RefreshNumbers();
        }

        void RefreshNumbers()
        {
            for (int pos = 0; pos < order.Count; pos++) numbers[order[pos]].text = (pos + 1) + ".";
        }

        void SyncShadow(int e, bool lifted)
        {
            var s = shadows[e].rectTransform;
            var r = rows[e];
            s.anchorMin = r.anchorMin; s.anchorMax = r.anchorMax; s.pivot = r.pivot;
            s.sizeDelta = r.sizeDelta;
            float off = lifted ? 10f : 3f;
            s.anchoredPosition = r.anchoredPosition + new Vector2(off * 0.6f, -off);
            var c = shadows[e].color; c.a = lifted ? 0.4f : 0.16f; shadows[e].color = c;
        }

        void LateUpdate()
        {
            if (rows == null) return;
            // le ombre seguono le carte (anche mentre si animano)
            for (int e = 0; e < rows.Length; e++) SyncShadow(e, e == dragEvent);
        }

        // ---------------------------------------------------------------- trascinamento
        public void BeginDrag(int e, PointerEventData ped)
        {
            if (solved || dragEvent >= 0) return;
            dragEvent = e;
            var rt = rows[e];
            UITween.Cancel(rt);
            rt.SetAsLastSibling();
            shadows[e].transform.SetSiblingIndex(rt.GetSiblingIndex() - 1);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, ped.position, null, out local);
            float centerY = rt.anchoredPosition.y - rowH * 0.5f + area.rect.yMax;
            grabOffset = centerY - local.y;
            UITween.Scale(rt, new Vector3(1.03f, 1.03f, 1f), 0.1f, Ease.OutCubic);
            UISfx.Play(Sfx.ClickPaper, 0.5f);
            Select(e);
        }

        public void Drag(int e, PointerEventData ped)
        {
            if (dragEvent != e) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, ped.position, null, out local);
            float centerY = local.y + grabOffset;
            float top = centerY + rowH * 0.5f - area.rect.yMax;
            float minY = -(order.Count - 1) * (rowH + Gap) - 10f;
            top = Mathf.Clamp(top, minY, 10f);
            rows[e].anchoredPosition = new Vector2(0f, top);

            int target = Mathf.Clamp(Mathf.RoundToInt(-top / (rowH + Gap)), 0, order.Count - 1);
            int cur = order.IndexOf(e);
            if (target != cur)
            {
                order.RemoveAt(cur);
                order.Insert(target, e);
                UISfx.Play(Sfx.Tick, 0.5f, 0.1f);
                for (int pos = 0; pos < order.Count; pos++)
                {
                    int other = order[pos];
                    if (other == e) continue;
                    UITween.Cancel(rows[other]);
                    UITween.Move(rows[other], Slot(pos), 0.16f, Ease.OutCubic);
                }
                RefreshNumbers();
            }
        }

        public void EndDrag(int e)
        {
            if (dragEvent != e) return;
            dragEvent = -1;
            var rt = rows[e];
            UITween.Cancel(rt);
            UITween.Scale(rt, Vector3.one, 0.2f, Ease.OutBack);
            UITween.Move(rt, Slot(order.IndexOf(e)), 0.22f, Ease.OutBack);
            UISfx.Play(Sfx.ClickPaper, 0.4f);
        }

        // ---------------------------------------------------------------- selezione e tastiera
        public void Select(int e)
        {
            selectedEvent = e;
            for (int i = 0; i < bgs.Length; i++)
                bgs[i].color = i == e && !solved ? new Color(1f, 0.93f, 0.7f) : new Color(1f, 0.97f, 0.88f);
        }

        void MoveSelected(int delta)
        {
            if (solved || selectedEvent < 0 || dragEvent >= 0) return;
            int cur = order.IndexOf(selectedEvent);
            int to = cur + delta;
            if (to < 0 || to >= order.Count) return;
            int other = order[to];
            order[to] = selectedEvent; order[cur] = other;
            UITween.Cancel(rows[selectedEvent]); UITween.Cancel(rows[other]);
            UITween.Move(rows[selectedEvent], Slot(to), 0.18f, Ease.OutBack);
            UITween.Move(rows[other], Slot(cur), 0.18f, Ease.OutCubic);
            UISfx.Play(Sfx.Tick, 0.5f, 0.1f);
            RefreshNumbers();
        }

        protected override void OnUpdate()
        {
            if (solved || rows == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) MoveSelected(-1);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) MoveSelected(1);
        }

        // ---------------------------------------------------------------- verifica
        void Verify()
        {
            if (solved || dragEvent >= 0) return;
            int c = CorrectCount();
            if (c == order.Count) { Solved("Ordine perfetto!"); return; }

            for (int pos = 0; pos < order.Count; pos++)
            {
                int e = order[pos];
                bool ok = e == pos;
                if (ok && !pins[e].gameObject.activeSelf)
                {
                    pins[e].gameObject.SetActive(true);
                    UIFx.Pop(pins[e].rectTransform, 1.6f, 0.35f);
                    UISfx.Play(Sfx.ClickBrass, 0.3f, 0.15f);
                }
                else if (!ok)
                {
                    pins[e].gameObject.SetActive(false);
                    UIFx.Wobble(rows[e], 1.5f, 0.45f);
                }
            }
            string m = "Hai messo " + c + " carte su " + order.Count + " al posto giusto. Riprova!";
            if (!string.IsNullOrEmpty(def.erroreMsg)) m += "\n" + def.erroreMsg;
            Wrong(m);
        }

        protected override void OnSolvedVisual()
        {
            if (rows == null) return;
            for (int e = 0; e < rows.Length; e++)
            {
                var ev = def.eventi[e];
                if (!string.IsNullOrEmpty(ev.data)) labels[e].text = ev.data + " - " + ev.testo;
                bgs[e].color = new Color(0.82f, 0.92f, 0.74f);
                if (!pins[e].gameObject.activeSelf) { pins[e].gameObject.SetActive(true); UIFx.Pop(pins[e].rectTransform, 1.6f, 0.35f); }
            }
        }
    }

    /// <summary>Carta trascinabile dell'enigma "ordina" (mouse e touch).</summary>
    public class OrdinaCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public OrdinaPanel owner;
        public int eventIndex;

        public void OnBeginDrag(PointerEventData e) { if (owner != null) owner.BeginDrag(eventIndex, e); }
        public void OnDrag(PointerEventData e) { if (owner != null) owner.Drag(eventIndex, e); }
        public void OnEndDrag(PointerEventData e) { if (owner != null) owner.EndDrag(eventIndex); }
        public void OnPointerClick(PointerEventData e) { if (owner != null) owner.Select(eventIndex); }
    }
}
