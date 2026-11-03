using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>Personaggio che dice una battuta breve (toast).</summary>
    public class NpcInfo : Interactable
    {
        public string personaggioId;
        [TextArea] public string battuta;

        string cachedPrompt;

        public override string GetPrompt()
        {
            if (cachedPrompt == null)
            {
                if (!string.IsNullOrEmpty(prompt)) cachedPrompt = prompt;
                else
                {
                    var p = GameData.Personaggio(personaggioId);
                    cachedPrompt = "Parla con " + (p != null ? p.nome : "qualcuno");
                }
            }
            return cachedPrompt;
        }

        public override void Interact()
        {
            if (UIRoot.I == null) return;
            var p = GameData.Personaggio(personaggioId);
            string testo = string.IsNullOrEmpty(battuta) ? "..." : battuta;
            UIRoot.I.Toast(p != null ? p.nome + ": " + testo : testo, 4f);
        }
    }
}
