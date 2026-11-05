using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>Porta verso un'altra scena. Se luogoId è valorizzato usa i dati del luogo (e controlla "aperto").</summary>
    public class Door : Interactable
    {
        public string luogoId;
        public string targetScene;
        public string targetSpawn;

        string cachedPrompt;

        public override string GetPrompt()
        {
            if (cachedPrompt == null)
            {
                if (!string.IsNullOrEmpty(luogoId))
                {
                    var l = GameData.Luogo(luogoId);
                    cachedPrompt = "Entra: " + (l != null ? l.nome : luogoId);
                }
                else cachedPrompt = string.IsNullOrEmpty(prompt) ? "Apri la porta" : prompt;
            }
            return cachedPrompt;
        }

        public override void Interact()
        {
            string scena = targetScene;
            string spawn = targetSpawn;

            if (!string.IsNullOrEmpty(luogoId))
            {
                var l = GameData.Luogo(luogoId);
                if (l != null)
                {
                    if (!l.aperto)
                    {
                        AudioManager.FireAt("porta_chiusa", transform.position);
                        if (UIRoot.I != null) UIRoot.I.Toast(l.nome + " si aprirà più avanti.");
                        return;
                    }
                    if (string.IsNullOrEmpty(scena)) scena = l.scena;
                    if (string.IsNullOrEmpty(spawn)) spawn = l.spawn;
                }
                else Debug.LogWarning("Luogo non trovato: " + luogoId);
            }

            if (string.IsNullOrEmpty(scena))
            {
                Debug.LogWarning("Door senza scena di destinazione: " + name);
                AudioManager.FireAt("porta_chiusa", transform.position);
                if (UIRoot.I != null) UIRoot.I.Toast("Questa porta è chiusa a chiave.");
                return;
            }
            AudioManager.FireAt("porta_apre", transform.position);
            SceneLoader.I.Load(scena, spawn);
        }
    }
}
