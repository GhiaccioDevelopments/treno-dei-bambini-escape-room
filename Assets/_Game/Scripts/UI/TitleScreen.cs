using TMPro;
using TrenoBambini.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Schermata del titolo (scena 01_Titolo): Nuova partita, Continua, Crediti.</summary>
    public class TitleScreen : MonoBehaviour
    {
        GameObject root;

        void Start()
        {
            if (UIRoot.I == null) { Debug.LogWarning("UIRoot non disponibile."); return; }
            Build();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
        }

        void Build()
        {
            var panel = UIFactory.MakePanel(UIRoot.I.ScreenLayer, "Titolo", new Color(UIFactory.Carta.r, UIFactory.Carta.g, UIFactory.Carta.b, 0.9f), false);
            root = panel.gameObject;
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 600f));
            var vl = UIFactory.AddVLayout(root, 14f);
            vl.padding = new RectOffset(0, 0, 30, 30);

            Add(UIFactory.MakeText(panel.rectTransform, "Il treno dei bambini", 56, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold), 640f, 150f);
            Add(UIFactory.MakeText(panel.rectTransform, "Napoli, 1946", 32, UIFactory.Inchiostro, TextAlignmentOptions.Center, FontStyles.Italic), 640f, 50f);
            Add(UIFactory.MakeText(panel.rectTransform, "", 10, UIFactory.Inchiostro), 10f, 14f);

            Add(UIFactory.MakeButton(panel.rectTransform, "Nuova partita", OnNew, new Vector2(400f, 72f), null, null, 30), 400f, 72f);
            if (GameManager.I != null && GameManager.I.HasSave)
                Add(UIFactory.MakeButton(panel.rectTransform, "Continua", OnContinue, new Vector2(400f, 72f), UIFactory.Marrone, null, 30), 400f, 72f);
            Add(UIFactory.MakeButton(panel.rectTransform, "Crediti", OnCredits, new Vector2(400f, 72f), UIFactory.Marrone, null, 30), 400f, 72f);
        }

        static void Add(Component c, float w, float h)
        {
            ((RectTransform)c.transform).sizeDelta = new Vector2(w, h);
        }

        void OnNew()
        {
            if (GameManager.I == null) return;
            if (GameManager.I.HasSave)
                UIRoot.I.ShowConfirm("Nuova partita", "Esiste già un salvataggio. Se ricominci, verrà cancellato. Vuoi continuare?",
                    "Sì, ricomincia", "Annulla", () => GameManager.I.NewGame());
            else
                GameManager.I.NewGame();
        }

        void OnContinue()
        {
            if (GameManager.I != null) GameManager.I.Continue();
        }

        void OnCredits()
        {
            UIRoot.I.ShowMessage("Crediti",
                "Il treno dei bambini - Escape room\n\nGioco educativo di Educazione Civica ispirato al romanzo di Viola Ardone (2019) e al film di Cristina Comencini (2024).\n\nPersonaggi, dialoghi e grafica originali.\n\nGhiaccio Developments",
                null);
        }
    }
}
