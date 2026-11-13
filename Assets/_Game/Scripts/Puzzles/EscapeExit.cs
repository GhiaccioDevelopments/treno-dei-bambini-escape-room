using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>Porta d'uscita dal ricordo: si apre quando tutti gli enigmi sono risolti.</summary>
    public class EscapeExit : Interactable
    {
        FlashbackDef flashback;
        bool open;
        Glow glow;

        void Awake() { glow = GetComponentInChildren<Glow>(); }

        public void Setup(FlashbackDef f, bool isOpen)
        {
            flashback = f;
            SetOpen(isOpen);
        }

        public void SetOpen(bool value)
        {
            open = value;
            if (glow == null) glow = GetComponentInChildren<Glow>();
            if (glow != null) glow.on = open;
            prompt = open ? "Esci dal ricordo" : "Porta chiusa";
        }

        public override void Interact()
        {
            if (UIRoot.I == null) return;
            if (!open)
            {
                UIRoot.I.Toast("La porta è chiusa: ti mancano ancora degli enigmi.", 3f);
                return;
            }
            if (flashback == null) { Debug.LogWarning("EscapeExit senza flashback."); return; }

            string oggetto = flashback.oggetto;
            string testo = string.IsNullOrEmpty(flashback.conclusione) ? "Il ricordo si dissolve..." : flashback.conclusione;
            UIRoot.I.ShowMessage(flashback.titolo, testo, () => GameManager.I.CompleteFlashback(oggetto), "nar_" + oggetto + "_002"); // R028: voce del Narratore
        }
    }
}
