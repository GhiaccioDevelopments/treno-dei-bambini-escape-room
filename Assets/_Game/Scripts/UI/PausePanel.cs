using TMPro;
using TrenoBambini.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Pausa: Riprendi, Inventario, Volume, Salva, Torna al titolo.</summary>
    public class PausePanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        GameObject view;
        Slider volume;

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;

            var card = UIFactory.MakePanel(veil.rectTransform, "Carta", UIFactory.Carta, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
            var vl = UIFactory.AddVLayout(card.gameObject, 14f);
            vl.padding = new RectOffset(0, 0, 24, 24);

            Add(UIFactory.MakeText(card.rectTransform, "Pausa", 42, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold), 400f, 60f);
            Add(UIFactory.MakeButton(card.rectTransform, "Riprendi", Hide, new Vector2(400f, 64f)), 400f, 64f);
            Add(UIFactory.MakeButton(card.rectTransform, "Inventario", () => { if (UIRoot.I != null) UIRoot.I.Inventory.Show(); }, new Vector2(400f, 64f), UIFactory.Marrone), 400f, 64f);

            Add(UIFactory.MakeText(card.rectTransform, "Volume", 26, UIFactory.Inchiostro, TextAlignmentOptions.Center), 400f, 36f);
            volume = UIFactory.MakeSlider(card.rectTransform, 1f, OnVolume, new Vector2(400f, 44f));
            ((RectTransform)volume.transform).sizeDelta = new Vector2(400f, 44f);

            Add(UIFactory.MakeButton(card.rectTransform, "Salva", OnSave, new Vector2(400f, 64f), UIFactory.Marrone), 400f, 64f);
            Add(UIFactory.MakeButton(card.rectTransform, "Torna al titolo", OnTitle, new Vector2(400f, 64f)), 400f, 64f);

            view.SetActive(false);
        }

        static void Add(Component c, float w, float h)
        {
            ((RectTransform)c.transform).sizeDelta = new Vector2(w, h);
        }

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock();
            volume.SetValueWithoutNotify(AudioManager.I != null ? AudioManager.I.Volume : 1f);
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

        void OnVolume(float v)
        {
            if (AudioManager.I != null) AudioManager.I.SetVolume(v);
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
