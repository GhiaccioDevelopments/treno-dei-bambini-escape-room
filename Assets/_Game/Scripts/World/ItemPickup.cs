using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.World
{
    /// <summary>Oggetto-ricordo: toccarlo avvia il flashback. Se già nell'inventario, mostra solo la descrizione.</summary>
    public class ItemPickup : Interactable
    {
        public string oggettoId;

        Glow glow;
        string currentPrompt;

        void Awake() { glow = GetComponentInChildren<Glow>(); }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        bool Collected
        {
            get { return GameManager.I != null && GameManager.I.Save.inventario.Contains(oggettoId); }
        }

        void Refresh()
        {
            bool done = Collected;
            if (glow != null) glow.on = !done;
            if (done) currentPrompt = "Ricordo già vissuto";
            else
            {
                var o = GameData.Oggetto(oggettoId);
                currentPrompt = !string.IsNullOrEmpty(prompt) ? prompt : "Esamina: " + (o != null ? o.nome : oggettoId);
            }
        }

        public override string GetPrompt()
        {
            if (currentPrompt == null) Refresh();
            return currentPrompt;
        }

        public override void Interact()
        {
            var o = GameData.Oggetto(oggettoId);
            if (Collected)
            {
                if (UIRoot.I != null && o != null) UIRoot.I.Toast(o.nome + ": " + o.descrizione, 5f);
                Refresh();
                return;
            }

            var s = GameManager.I.Save;
            s.ritornoScena = SceneManager.GetActiveScene().name;
            s.ritornoSpawn = "oggetto_" + oggettoId;
            GameManager.I.SaveGame();
            GameManager.I.StartFlashback(oggettoId);
        }
    }
}
