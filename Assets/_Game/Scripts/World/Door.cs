using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
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
                    cachedPrompt = string.Format(GameData.TestiEnigmi.entraFmt, l != null ? l.nome : luogoId);
                }
                else cachedPrompt = string.IsNullOrEmpty(prompt) ? GameData.TestiEnigmi.porta : prompt;
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
                    // R020: porta chiusa a chiave finche' non si hanno i gettoni (porta trovata, chiave)
                    if (l.richiede != null && (string.IsNullOrEmpty(l.bypassOggetto) || !GameManager.I.Save.inventario.Contains(l.bypassOggetto)))
                    {
                        for (int i = 0; i < l.richiede.Count; i++)
                        {
                            if (MondoStato.Ha(l.richiede[i])) continue;
                            AudioManager.FireAt("porta_chiusa", transform.position);
                            string m = l.richiedeMsg != null && i < l.richiedeMsg.Count ? l.richiedeMsg[i] : GameData.TestiEnigmi.portaChiusa;
                            if (UIRoot.I != null) UIRoot.I.Toast(m, 4.5f);
                            return;
                        }
                    }
                    // R020: la prima volta la chiave gira nella toppa (inventario attivo: la chiave si usa sulla porta)
                    if (l.richiede != null && l.richiede.Count > 0 && !MondoStato.Ha("porta_aperta"))
                    {
                        MondoStato.Dai("porta_aperta");
                        if (UIRoot.I != null) UIRoot.I.Toast(!string.IsNullOrEmpty(l.aperturaMsg) ? l.aperturaMsg : GameData.TestiEnigmi.portaChiusa, 3.5f);
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
                if (UIRoot.I != null) UIRoot.I.Toast(GameData.TestiEnigmi.portaChiusa);
                return;
            }
            // il suono parte solo se la scena è davvero caricabile (altrimenti SceneLoader mostra l'avviso)
            if (!SceneLoader.I.IsLoading && Application.CanStreamedLevelBeLoaded(scena))
                AudioManager.FireAt("porta_apre", transform.position);
            SceneLoader.I.Load(scena, spawn);
        }
    }
}
