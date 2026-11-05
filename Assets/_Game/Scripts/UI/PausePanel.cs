using TMPro;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Pausa come quaderno di scuola anni '40: copertina nera, pagina a righe con margine rosso, voci a inchiostro con sottolineatura.</summary>
    public class PausePanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        GameObject view;
        PanelFx fx;
        Slider volume;
        Button reduceBtn;

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", new Color(0.06f, 0.04f, 0.02f, 0.82f), true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;
            fx = view.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Notebook;

            // copertina + pagina
            var cover = UIFactory.MakePanel(veil.rectTransform, "Copertina", Color.white, true);
            cover.sprite = UIArt.Cover(); cover.type = Image.Type.Sliced;
            UIFactory.Place(cover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 690f));
            fx.card = cover.rectTransform;
            var page = UIFactory.MakePanel(cover.rectTransform, "Pagina", Color.white, false);
            page.sprite = UIArt.Notebook(); page.type = Image.Type.Sliced;
            UIFactory.Stretch(page.rectTransform, 26f, 22f, 22f, 22f);
            var pr = page.rectTransform;

            // righe azzurre ripetute + margine rosso
            var rulesGo = UIFactory.NewRect("Righe", pr);
            UIFactory.Stretch(rulesGo, 18f, 18f, 18f, 18f);
            var rules = rulesGo.gameObject.AddComponent<RawImage>();
            rules.texture = UIArt.Ruled();
            rules.raycastTarget = false;
            rules.uvRect = new Rect(0f, 0f, 1f, 600f / 30f);
            var margin = UIFactory.MakePanel(pr, "Margine", new Color(0.7f, 0.2f, 0.18f, 0.55f), false);
            margin.sprite = UIArt.White();
            margin.rectTransform.anchorMin = new Vector2(0f, 0f); margin.rectTransform.anchorMax = new Vector2(0f, 1f);
            margin.rectTransform.pivot = new Vector2(0f, 0.5f);
            margin.rectTransform.anchoredPosition = new Vector2(78f, 0f);
            margin.rectTransform.sizeDelta = new Vector2(2f, -24f);

            var content = UIFactory.NewRect("Voci", pr);
            UIFactory.Stretch(content, 90f, 20f, 20f, 20f);
            var vl = UIFactory.AddVLayout(content.gameObject, 8f);
            vl.padding = new RectOffset(0, 0, 10, 10);

            var title = UIFactory.MakeText(content, "Pausa", 48, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            Add(title, 440f, 70f);
            Add(Item(content, "Riprendi", Hide), 440f, 60f);
            Add(Item(content, "Valigia", () => { if (UIRoot.I != null) UIRoot.I.Inventory.Show(); }), 440f, 60f);

            Add(UIFactory.MakeText(content, "Volume", 26, UITheme.Inchiostro, TextAlignmentOptions.Center), 440f, 34f);
            volume = UIFactory.MakeSlider(content, 1f, OnVolume, new Vector2(400f, 46f));
            Add(volume, 400f, 46f);

            reduceBtn = Item(content, "", OnToggleReduce);
            Add(reduceBtn, 440f, 60f);
            Add(Item(content, "Salva", OnSave), 440f, 60f);
            Add(Item(content, "Torna al titolo", OnTitle), 440f, 60f);

            view.SetActive(false);
        }

        static Button Item(Transform parent, string label, UnityEngine.Events.UnityAction cb)
        {
            return UIFactory.MakeButton(parent, label, cb, new Vector2(440f, 60f), ButtonStyle.Ink, 32f);
        }

        static void Add(Component c, float w, float h)
        {
            ((RectTransform)c.transform).sizeDelta = new Vector2(w, h);
        }

        void RefreshReduceLabel()
        {
            UIFactory.SetLabel(reduceBtn, "Effetti ridotti: " + (UITheme.ReduceEffects ? "sì" : "no"));
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock();
            volume.SetValueWithoutNotify(AudioManager.I != null ? AudioManager.I.Volume : 1f);
            RefreshReduceLabel();
            transform.SetAsLastSibling();
            fx.Show();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            fx.Hide();
            if (GameManager.I != null) GameManager.I.PopBlock();
        }

        void OnVolume(float v)
        {
            if (AudioManager.I != null) AudioManager.I.SetVolume(v);
        }

        void OnToggleReduce()
        {
            if (UIRoot.I != null) UIRoot.I.SetReduceEffects(!UITheme.ReduceEffects);
            RefreshReduceLabel();
        }

        void OnSave()
        {
            if (GameManager.I == null) return;
            GameManager.I.SaveGame();
            if (UIRoot.I != null) UIRoot.I.Toast("Partita salvata.");
        }

        void OnTitle()
        {
            if (UIRoot.I == null || GameManager.I == null) return;
            UIRoot.I.ShowConfirm("Torna al titolo", "Vuoi tornare alla schermata del titolo? I progressi vengono salvati.",
                "Sì, torna al titolo", "Annulla", () =>
                {
                    GameManager.I.SaveGame();
                    Hide();
                    GameManager.I.ResetBlocks();
                    SceneLoader.I.Load(GameManager.SceneTitolo, null);
                });
        }
    }
}
