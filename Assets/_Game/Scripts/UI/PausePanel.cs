using TMPro;
using TrenoBambini.Audio;
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
        Slider volume, music, ambient, effects, voices;
        Button lookBtn;
        const float ItemH = 42f, RowH = 32f;

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
            var vl = UIFactory.AddVLayout(content.gameObject, 5f);
            vl.padding = new RectOffset(0, 0, 4, 4);

            var title = UIFactory.MakeText(content, "Pausa", 42, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            Add(title, 440f, 50f);
            Add(Item(content, "Riprendi", Hide), 440f, ItemH);
            Add(Item(content, "Valigia", () => { if (UIRoot.I != null) UIRoot.I.Inventory.Show(); }), 440f, ItemH);

            // R002: volume generale + quattro canali, una riga per slider (etichetta a sinistra)
            volume = SliderRow(content, "Generale", OnVolume);
            music = SliderRow(content, "Musica", v => OnChannel(AudioChannel.Musica, v));
            ambient = SliderRow(content, "Ambiente", v => OnChannel(AudioChannel.Ambiente, v));
            effects = SliderRow(content, "Effetti", v => OnChannel(AudioChannel.Effetti, v));
            voices = SliderRow(content, "Voci", v => OnChannel(AudioChannel.Voci, v));

            // R008: grafica, audio e accessibilita' (effetti ridotti compresi) stanno nel menu Impostazioni
            Add(Item(content, "Impostazioni", () => { if (UIRoot.I != null && UIRoot.I.Settings != null) UIRoot.I.Settings.Show(); }), 440f, ItemH);
            // R005: comandi da tastiera e scelta di come si gira la visuale (salvata)
            Add(Item(content, "Comandi", OnComandi), 440f, ItemH);
            lookBtn = Item(content, "", OnToggleLook);
            Add(lookBtn, 440f, ItemH);
            Add(Item(content, "Salva", OnSave), 440f, ItemH);
            Add(Item(content, "Torna al titolo", OnTitle), 440f, ItemH);

            view.SetActive(false);
        }

        static Button Item(Transform parent, string label, UnityEngine.Events.UnityAction cb)
        {
            return UIFactory.MakeButton(parent, label, cb, new Vector2(440f, ItemH), ButtonStyle.Ink, 26f);
        }

        static Slider SliderRow(Transform parent, string label, UnityEngine.Events.UnityAction<float> onChange)
        {
            var row = UIFactory.NewRect("Riga_" + label, parent);
            row.sizeDelta = new Vector2(440f, RowH);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var t = UIFactory.MakeText(row, label, 24, UITheme.Inchiostro, TextAlignmentOptions.MidlineLeft);
            Add(t, 130f, RowH);
            var s = UIFactory.MakeSlider(row, 1f, onChange, new Vector2(290f, RowH));
            Add(s, 290f, RowH);
            return s;
        }

        static void Add(Component c, float w, float h)
        {
            ((RectTransform)c.transform).sizeDelta = new Vector2(w, h);
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock("pausa");
            var am = AudioManager.I;
            volume.SetValueWithoutNotify(am != null ? am.Volume : 1f);
            music.SetValueWithoutNotify(am != null ? am.GetChannel(AudioChannel.Musica) : 1f);
            ambient.SetValueWithoutNotify(am != null ? am.GetChannel(AudioChannel.Ambiente) : 1f);
            effects.SetValueWithoutNotify(am != null ? am.GetChannel(AudioChannel.Effetti) : 1f);
            voices.SetValueWithoutNotify(am != null ? am.GetChannel(AudioChannel.Voci) : 1f);
            if (am != null) am.Music.SetPaused(true); // la musica scende al 30% senza fermarsi
            RefreshLookLabel();
            transform.SetAsLastSibling();
            fx.Show();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (AudioManager.I != null) AudioManager.I.Music.SetPaused(false);
            fx.Hide();
            if (GameManager.I != null) { GameManager.I.PopBlock("pausa"); GameManager.I.SaveGame(); } // salva slider e preferenze
        }

        void OnVolume(float v)
        {
            if (AudioManager.I != null) AudioManager.I.SetVolume(v);
        }

        void OnChannel(AudioChannel ch, float v)
        {
            if (AudioManager.I != null) AudioManager.I.SetChannel(ch, v);
        }

        void RefreshLookLabel()
        {
            UIFactory.SetLabel(lookBtn, CursorManager.MouseLookMode ? "Visuale: muovendo il mouse" : "Visuale: col tasto destro");
        }

        void OnToggleLook()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            gm.Save.visualeTastoDestro = !gm.Save.visualeTastoDestro;
            gm.SaveGame();
            RefreshLookLabel();
            if (UIRoot.I != null)
                UIRoot.I.Toast(gm.Save.visualeTastoDestro
                    ? "Visuale: tieni premuto il tasto destro e muovi il mouse."
                    : "Visuale: muovi il mouse, senza tenere premuto nulla.");
        }

        void OnComandi()
        {
            if (UIRoot.I == null) return;
            UIRoot.I.ShowMessage("Comandi", UIRoot.ComandiTesto(!CursorManager.MouseLookMode), null);
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
