using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Inventario come valigia di cartone che si spalanca: gli oggetti sono cartoline leggermente storte (si raddrizzano e seguono
    /// il mouse in hover), gli oggetti non ancora trovati sono sagome tratteggiate con il nome del luogo. Il dettaglio si può "girare":
    /// sul retro ci sono il significato e il tema civico con un timbro.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        GameObject view;
        PanelFx fx;
        RectTransform card, grid, detail, front, back;
        Image detailIcon;
        TextMeshProUGUI detailTitle, detailBody, backBody, hint;
        Button flipBtn;
        string selected;
        bool showingBack;
        readonly List<PostcardFx> cards = new List<PostcardFx>();

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;
            fx = view.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Suitcase;

            // corpo della valigia: cartone + cornice d'ottone
            var body = UIFactory.MakePaper(veil.rectTransform, "Valigia", UITheme.Cartone, true);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 640f));
            card = body.rectTransform;
            fx.card = card;
            UIFactory.MakeBrassFrame(card, 8f);

            // targhetta del titolo (il "coperchio")
            var plate = UIFactory.MakePanel(card, "Targa", Color.white, false);
            plate.sprite = UIArt.Brass(); plate.type = Image.Type.Sliced;
            UIFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 64f));
            var title = UIFactory.MakeText(plate.rectTransform, "La valigia", 36, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(title.rectTransform);

            // scomparti (sinistra)
            grid = UIFactory.NewRect("Scomparti", card);
            grid.anchorMin = new Vector2(0f, 0f); grid.anchorMax = new Vector2(0f, 1f);
            grid.pivot = new Vector2(0f, 1f);
            grid.offsetMin = new Vector2(34f, 100f); grid.offsetMax = new Vector2(450f, -62f);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(120f, 150f);
            gl.spacing = new Vector2(14f, 14f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.childAlignment = TextAnchor.UpperLeft;

            // dettaglio (destra): una grande cartolina girevole
            var det = UIFactory.MakePaper(card, "Dettaglio", Color.white, true);
            detail = det.rectTransform;
            detail.anchorMin = new Vector2(0f, 0f); detail.anchorMax = new Vector2(1f, 1f);
            detail.offsetMin = new Vector2(480f, 100f); detail.offsetMax = new Vector2(-34f, -62f);

            front = UIFactory.NewRect("Fronte", detail);
            UIFactory.Stretch(front);
            detailIcon = UIFactory.MakeImage(front, "Icona", null, UIFactory.Marrone);
            UIFactory.Place(detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -26f), new Vector2(130f, 130f));
            detailTitle = UIFactory.MakeText(front, "", 34, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold);
            detailTitle.rectTransform.anchorMin = new Vector2(0f, 1f); detailTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailTitle.rectTransform.pivot = new Vector2(0f, 1f);
            detailTitle.rectTransform.offsetMin = new Vector2(176f, -146f); detailTitle.rectTransform.offsetMax = new Vector2(-24f, -26f);
            detailBody = UIFactory.MakeText(front, "", 23, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(detailBody.rectTransform, 30f, 80f, 30f, 170f);
            detailBody.enableAutoSizing = true; detailBody.fontSizeMin = 14; detailBody.fontSizeMax = 23;

            back = UIFactory.NewRect("Retro", detail);
            UIFactory.Stretch(back);
            backBody = UIFactory.MakeText(back, "", 22, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(backBody.rectTransform, 30f, 80f, 30f, 40f);
            backBody.enableAutoSizing = true; backBody.fontSizeMin = 14; backBody.fontSizeMax = 22;
            // timbro postale
            var stamp = UIFactory.MakePanel(back, "Timbro", new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.8f), false);
            stamp.sprite = UIArt.StampFrame(); stamp.type = Image.Type.Sliced;
            UIFactory.Place(stamp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(230f, 76f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            var st = UIFactory.MakeText(stamp.rectTransform, "NAPOLI\nEDUCAZIONE CIVICA", 17, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(st.rectTransform);
            back.gameObject.SetActive(false);

            flipBtn = UIFactory.MakeButton(detail, "Gira", FlipDetail, new Vector2(200f, 60f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)flipBtn.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 18f), new Vector2(200f, 60f));

            hint = UIFactory.MakeText(card, "", 20, new Color(0.96f, 0.9f, 0.75f, 0.9f), TextAlignmentOptions.BottomLeft);
            UIFactory.Stretch(hint.rectTransform, 40f, 18f, 400f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 40f);

            var close = UIFactory.MakeButton(card, "Chiudi", Hide, new Vector2(260f, 64f), ButtonStyle.Seal, 28f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(260f, 64f));

            view.SetActive(false);
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock();
            Refresh();
            transform.SetAsLastSibling();
            fx.Show();
            Cascade();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            fx.Hide();
            if (GameManager.I != null) GameManager.I.PopBlock();
        }

        void Cascade()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c == null) continue;
                var rt = (RectTransform)c.transform;
                float rot = c.restRotation;
                rt.localScale = Vector3.zero;
                UITween.Scale(rt, Vector3.one, 0.35f, Ease.OutBack, null, 0.2f + i * 0.04f);
                UITween.Rotate(rt, rot, 0.35f, Ease.OutCubic, null, 0.2f + i * 0.04f);
            }
        }

        void Refresh()
        {
            UIFactory.Clear(grid);
            cards.Clear();
            showingBack = false;
            var inv = GameManager.I != null ? GameManager.I.Save.inventario : null;
            bool any = inv != null && inv.Count > 0;
            if (any && (selected == null || !inv.Contains(selected))) selected = inv[0];
            if (!any) selected = null;

            if (any)
                for (int i = 0; i < inv.Count; i++) MakeCard(inv[i], i);

            // sagome degli oggetti ancora da trovare
            var all = GameData.Oggetti;
            int shown = any ? inv.Count : 0;
            if (all != null)
                for (int i = 0; i < all.Count; i++)
                {
                    if (any && inv.Contains(all[i].id)) continue;
                    MakeLocked(all[i], shown++);
                }
            ShowDetail(selected);
        }

        void MakeCard(string id, int index)
        {
            var o = GameData.Oggetto(id);
            var img = UIFactory.MakePanel(grid, "Cartolina_" + id, Color.white, true);
            img.sprite = UIArt.Postcard(); img.type = Image.Type.Sliced;
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { selected = id; ShowDetail(id); UISfx.Play(Sfx.ClickPaper, 0.6f); });

            var pic = UIFactory.NewRect("Foto", img.rectTransform);
            UIFactory.Stretch(pic, 12f, 40f, 12f, 12f);
            var photoBg = pic.gameObject.AddComponent<Image>();
            photoBg.color = new Color(0.36f, 0.3f, 0.22f, 1f);
            photoBg.raycastTarget = false;
            Sprite spr = o != null ? UIFactory.LoadSprite("oggetti", o.icona) : null;
            if (spr != null)
            {
                photoBg.color = new Color(0.9f, 0.84f, 0.7f, 1f);
                var icon = UIFactory.MakeImage(pic, "Icona", spr, Color.white);
                UIFactory.Stretch(icon.rectTransform, 6f, 6f, 6f, 6f);
            }
            string nome = o != null && !string.IsNullOrEmpty(o.nome) ? o.nome : id;
            if (spr == null)
            {
                var ini = UIFactory.MakeText(pic, nome.Substring(0, 1).ToUpperInvariant(), 56, UITheme.Carta, TextAlignmentOptions.Center, FontStyles.Bold);
                UIFactory.Stretch(ini.rectTransform);
            }
            var lbl = UIFactory.MakeText(img.rectTransform, nome, 18, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f); lbl.rectTransform.anchorMax = new Vector2(1f, 0f);
            lbl.rectTransform.pivot = new Vector2(0.5f, 0f);
            lbl.rectTransform.offsetMin = new Vector2(8f, 6f); lbl.rectTransform.offsetMax = new Vector2(-8f, 38f);
            lbl.enableAutoSizing = true; lbl.fontSizeMin = 11; lbl.fontSizeMax = 18;

            var pc = img.gameObject.AddComponent<PostcardFx>();
            pc.photo = pic;
            pc.restRotation = Mathf.Sin(index * 12.9898f + 1.7f) * 3f; // storta in modo ripetibile, ±3°
            ((RectTransform)img.transform).localRotation = Quaternion.Euler(0f, 0f, pc.restRotation);
            cards.Add(pc);
        }

        void MakeLocked(OggettoDef o, int index)
        {
            var img = UIFactory.MakePanel(grid, "Slot_" + o.id, new Color(0.14f, 0.09f, 0.05f, 0.35f), true);
            img.sprite = UIArt.DashedFrame(); img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 1f);
            var ghost = UIFactory.MakePanel(img.rectTransform, "Sfondo", new Color(0.1f, 0.06f, 0.03f, 0.25f), false);
            UIFactory.Stretch(ghost.rectTransform, 6f, 6f, 6f, 6f);
            var q = UIFactory.MakeText(img.rectTransform, "?", 64, new Color(0.96f, 0.9f, 0.75f, 0.85f), TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(q.rectTransform, 0f, 40f, 0f, 0f);
            var luogo = GameData.Luogo(o.luogo);
            string dove = luogo != null ? luogo.nome : "";
            var lbl = UIFactory.MakeText(img.rectTransform, dove, 17, new Color(0.96f, 0.9f, 0.75f, 0.9f), TextAlignmentOptions.Center);
            lbl.rectTransform.anchorMin = new Vector2(0f, 0f); lbl.rectTransform.anchorMax = new Vector2(1f, 0f);
            lbl.rectTransform.pivot = new Vector2(0.5f, 0f);
            lbl.rectTransform.offsetMin = new Vector2(6f, 6f); lbl.rectTransform.offsetMax = new Vector2(-6f, 46f);
            lbl.enableAutoSizing = true; lbl.fontSizeMin = 11; lbl.fontSizeMax = 17;
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                UISfx.Play(Sfx.ClickPaper, 0.4f);
                hint.text = string.IsNullOrEmpty(dove) ? "Un ricordo ancora da trovare." : "Un ricordo ancora da trovare: cerca vicino a " + dove + ".";
            });
        }

        void FlipDetail()
        {
            if (selected == null) return;
            UIFx.FlipX(detail, () => { showingBack = !showingBack; ApplySide(); });
        }

        void ApplySide()
        {
            front.gameObject.SetActive(!showingBack);
            back.gameObject.SetActive(showingBack);
            UIFactory.SetLabel(flipBtn, showingBack ? "Gira indietro" : "Gira");
            if (showingBack)
            {
                UISfx.Play(Sfx.Stamp, 0.35f);
                UIParticles.BurstAtRect(ParticleKind.Dust, detail, 3);
            }
        }

        void ShowDetail(string id)
        {
            showingBack = false;
            ApplySide();
            hint.text = "";
            var o = GameData.Oggetto(id);
            flipBtn.gameObject.SetActive(id != null && o != null);
            if (id == null || o == null)
            {
                detailIcon.sprite = null;
                detailIcon.color = new Color(0f, 0f, 0f, 0f);
                detailTitle.text = "";
                detailBody.text = id == null
                    ? "La valigia è vuota. Esplora il vicolo e cerca gli oggetti che custodiscono i ricordi."
                    : "Oggetto sconosciuto.";
                return;
            }
            Sprite spr = UIFactory.LoadSprite("oggetti", o.icona);
            detailIcon.sprite = spr;
            detailIcon.color = spr != null ? Color.white : UIFactory.Marrone;
            detailTitle.text = o.nome;
            detailBody.text = o.descrizione ?? "";
            string t = "";
            if (!string.IsNullOrEmpty(o.significato)) t += "<b>Che cosa significa</b>\n" + o.significato;
            if (!string.IsNullOrEmpty(o.temaCivico)) t += (t.Length > 0 ? "\n\n" : "") + "<b>Educazione civica</b>\n" + o.temaCivico;
            backBody.text = t;
        }
    }

    /// <summary>Cartolina dell'inventario: in hover si raddrizza, si solleva e la foto segue il mouse (parallasse).</summary>
    public class PostcardFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform photo;
        public float restRotation;
        bool hover;
        RectTransform rt;

        void Awake() { rt = (RectTransform)transform; }

        public void OnPointerEnter(PointerEventData e)
        {
            if (e.pointerId >= 0) return;
            hover = true;
            UISfx.Play(Sfx.Hover, 0.4f, 0.1f);
            UITween.Cancel(rt);
            UITween.Scale(rt, new Vector3(1.1f, 1.1f, 1f), 0.15f, Ease.OutCubic);
            UITween.Rotate(rt, 0f, 0.15f, Ease.OutCubic);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hover = false;
            UITween.Cancel(rt);
            UITween.Scale(rt, Vector3.one, 0.18f, Ease.OutCubic);
            UITween.Rotate(rt, restRotation, 0.18f, Ease.OutCubic);
            if (photo != null) photo.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            hover = false;
            if (rt != null) { UITween.Cancel(rt); }
        }

        void Update()
        {
            if (!hover || photo == null || UITheme.ReduceEffects || Mouse.current == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, Mouse.current.position.ReadValue(), null, out local)) return;
            Vector2 n = new Vector2(local.x / Mathf.Max(1f, rt.rect.width), local.y / Mathf.Max(1f, rt.rect.height));
            photo.anchoredPosition = Vector2.Lerp(photo.anchoredPosition, new Vector2(n.x * 10f, n.y * 10f), 0.3f);
        }
    }
}
