using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Inventario: griglia di icone a sinistra, dettaglio dell'oggetto a destra.</summary>
    public class InventoryPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        GameObject view;
        RectTransform grid;
        Image detailIcon;
        TextMeshProUGUI detailTitle, detailBody;
        string selected;

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;

            var card = UIFactory.MakePanel(veil.rectTransform, "Carta", UIFactory.Carta, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 640f));

            var title = UIFactory.MakeText(card.rectTransform, "Inventario", 40, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(30f, -76f); title.rectTransform.offsetMax = new Vector2(-30f, -16f);

            // griglia (sinistra)
            grid = UIFactory.NewRect("Griglia", card.rectTransform);
            grid.anchorMin = new Vector2(0f, 0f); grid.anchorMax = new Vector2(0f, 1f);
            grid.pivot = new Vector2(0f, 1f);
            grid.offsetMin = new Vector2(30f, 100f); grid.offsetMax = new Vector2(400f, -90f);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(110f, 110f);
            gl.spacing = new Vector2(12f, 12f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.UpperLeft;

            // dettaglio (destra)
            var det = UIFactory.NewRect("Dettaglio", card.rectTransform);
            det.anchorMin = new Vector2(0f, 0f); det.anchorMax = new Vector2(1f, 1f);
            det.offsetMin = new Vector2(430f, 100f); det.offsetMax = new Vector2(-30f, -90f);
            var detBg = det.gameObject.AddComponent<Image>();
            detBg.color = new Color(0.82f, 0.75f, 0.58f, 0.5f);
            detBg.raycastTarget = false;

            detailIcon = UIFactory.MakeImage(det, "Icona", null, UIFactory.Marrone);
            UIFactory.Place(detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(120f, 120f));
            detailTitle = UIFactory.MakeText(det, "", 34, UIFactory.Accento, TextAlignmentOptions.Left, FontStyles.Bold);
            detailTitle.rectTransform.anchorMin = new Vector2(0f, 1f); detailTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailTitle.rectTransform.pivot = new Vector2(0f, 1f);
            detailTitle.rectTransform.offsetMin = new Vector2(150f, -136f); detailTitle.rectTransform.offsetMax = new Vector2(-16f, -16f);
            detailBody = UIFactory.MakeText(det, "", 22, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(detailBody.rectTransform, 20f, 10f, 20f, 150f);
            detailBody.enableAutoSizing = true; detailBody.fontSizeMin = 14; detailBody.fontSizeMax = 22;

            var close = UIFactory.MakeButton(card.rectTransform, "Chiudi", Hide, new Vector2(260f, 64f));
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(260f, 64f));

            view.SetActive(false);
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock();
            Refresh();
            view.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            view.SetActive(false);
            if (GameManager.I != null) GameManager.I.PopBlock();
        }

        void Refresh()
        {
            UIFactory.Clear(grid);
            var inv = GameManager.I != null ? GameManager.I.Save.inventario : null;
            if (inv == null || inv.Count == 0)
            {
                selected = null;
                ShowDetail(null);
                return;
            }
            if (selected == null || !inv.Contains(selected)) selected = inv[0];

            for (int i = 0; i < inv.Count; i++)
            {
                string id = inv[i];
                var o = GameData.Oggetto(id);
                var btn = UIFactory.MakeButton(grid, "", () => { selected = id; ShowDetail(id); }, new Vector2(110f, 110f), UIFactory.CartaScura);
                Sprite spr = o != null ? UIFactory.LoadSprite("oggetti", o.icona) : null;
                if (spr != null)
                {
                    var icon = UIFactory.MakeImage(btn.transform, "Icona", spr, Color.white);
                    UIFactory.Stretch(icon.rectTransform, 8f, 8f, 8f, 8f);
                }
                else
                {
                    string nome = o != null && !string.IsNullOrEmpty(o.nome) ? o.nome : id;
                    UIFactory.SetLabel(btn, nome.Substring(0, 1).ToUpperInvariant());
                }
            }
            ShowDetail(selected);
        }

        void ShowDetail(string id)
        {
            var o = GameData.Oggetto(id);
            if (id == null || o == null)
            {
                detailIcon.sprite = null;
                detailIcon.color = new Color(0f, 0f, 0f, 0f);
                detailTitle.text = "";
                detailBody.text = id == null
                    ? "L'inventario è vuoto. Esplora il vicolo e cerca gli oggetti che custodiscono i ricordi."
                    : "Oggetto sconosciuto.";
                return;
            }
            Sprite spr = UIFactory.LoadSprite("oggetti", o.icona);
            detailIcon.sprite = spr;
            detailIcon.color = spr != null ? Color.white : UIFactory.Marrone;
            detailTitle.text = o.nome;
            string t = o.descrizione ?? "";
            if (!string.IsNullOrEmpty(o.significato)) t += "\n\n<b>Che cosa significa</b>\n" + o.significato;
            if (!string.IsNullOrEmpty(o.temaCivico)) t += "\n\n<b>Educazione civica</b>\n" + o.temaCivico;
            detailBody.text = t;
        }
    }
}
