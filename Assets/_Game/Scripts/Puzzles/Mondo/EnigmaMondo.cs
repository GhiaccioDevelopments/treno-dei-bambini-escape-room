using System;
using System.Collections.Generic;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R009/R020: un enigma che e' un oggetto d'epoca nel mondo (la fotografia storta, il calendario, la cassetta di latta, il baule...).
    /// Nessun marcatore colorato: si riconosce dal prompt "Esamina" e, solo se l'impostazione lo chiede, da un brillio leggero.
    /// E apre la vista ravvicinata (o il pannello 2D con "Enigmi semplificati").
    /// </summary>
    public class EnigmaMondo : Interactable
    {
        public static readonly List<EnigmaMondo> Tutti = new List<EnigmaMondo>();
        public static float UltimoProgresso;
        public static float SogliaBagliore = 150f; // secondi senza progressi dopo i quali un enigma disponibile brilla (la prova la abbassa)

        public EnigmaDef def;
        public string chiave;
        public Action<EnigmaMondo> alRisolto;
        public Action<string> progresso; // testo extra dell'HUD (oggetti nascosti)

        public bool segreto; // R011: enigma segreto: mai brillio automatico (ne' dopo 150 s ne' con "evidenzia interattivi")
        NascostoPuzzle nascosto;
        Glow glow;
        bool bagliore;

        public bool Risolto { get { return MondoStato.Risolto(chiave); } }
        public bool Disponibile { get { return !Risolto && MondoStato.HaTutti(def.richiede); } }

        protected override void OnEnable() { base.OnEnable(); if (!Tutti.Contains(this)) Tutti.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Tutti.Remove(this); }

        public static EnigmaMondo Crea(EnigmaDef d, string chiave, Transform parent)
        {
            var root = new GameObject("Enigma_" + d.id);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(d.x, d.y, d.z);
            root.transform.localScale = Vector3.one * (d.scala > 0f ? d.scala : 1f);
            MondoFab.Oggetto(d, root.transform, false);
            foreach (var p in d.parti)
                if (p.nascosta) { var t = root.transform.Find(p.id); if (t != null) t.gameObject.SetActive(false); }
            // collider dell'insieme (trigger: non blocca il giocatore, serve ai raggi del mouse e alla distanza del prompt)
            var b = MondoFab.Limiti(root.transform);
            var bc = root.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = root.transform.InverseTransformPoint(b.center);
            var inv = root.transform.localScale;
            bc.size = new Vector3(Mathf.Max(0.3f, b.size.x / Mathf.Max(0.001f, inv.x)), Mathf.Max(0.3f, b.size.y / Mathf.Max(0.001f, inv.y)), Mathf.Max(0.3f, b.size.z / Mathf.Max(0.001f, inv.z)));
            root.transform.rotation = Quaternion.Euler(0f, d.yaw, 0f);

            var e = root.AddComponent<EnigmaMondo>();
            e.def = d;
            e.chiave = chiave;
            e.glow = root.AddComponent<Glow>();
            e.glow.on = false; e.glow.bob = false; e.glow.intensity = 0.6f;
            e.Aggiorna();
            return e;
        }

        void Update()
        {
            bool vuole = !segreto && Disponibile && ((GameManager.I != null && GameManager.I.Save.evidenziaInterattivi) || (Time.time - UltimoProgresso > SogliaBagliore && UltimoProgresso > 0f));
            if (vuole != bagliore) { bagliore = vuole; if (glow != null) glow.on = vuole; }
        }

        public void Aggiorna()
        {
            string t = !string.IsNullOrEmpty(def.prompt) ? def.prompt : def.titolo;
            if (Risolto) prompt = string.Format(GameData.TestiEnigmi.esaminato, t);
            else prompt = string.Format(GameData.TestiEnigmi.esamina, t);
        }

        public override string GetPrompt() { Aggiorna(); return prompt; }

        public override void Interact()
        {
            if (def == null || UIRoot.I == null || EsaminaView.Current != null) return;
            if (Risolto)
            {
                UIRoot.I.Toast(def.titolo + ": già risolto." + (string.IsNullOrEmpty(def.appunto) ? "" : " " + def.appunto), 4f);
                return;
            }
            if (!MondoStato.HaTutti(def.richiede))
            {
                UIRoot.I.Toast(!string.IsNullOrEmpty(def.bloccatoMsg) ? def.bloccatoMsg : GameData.TestiEnigmi.bloccato, 4f);
                return;
            }

            if ((def.tipo ?? "").ToLowerInvariant() == "nascosto" && def.oggetti != null && def.oggetti.Count > 0 && !TuttiTrovati())
            {
                if (nascosto == null)
                    nascosto = NascostoPuzzle.Begin(def, null, progresso, () => { nascosto = null; });
                else nascosto.ShowHint();
                return;
            }
            Apri();
        }

        bool TuttiTrovati()
        {
            for (int i = 0; i < def.oggetti.Count; i++) if (!MondoStato.Ha(def.oggetti[i].id)) return false;
            return true;
        }

        public void Apri()
        {
            string mec = EsaminaView.Meccanismo(def);
            bool semplificato = GameManager.I != null && GameManager.I.Save.enigmiSemplificati;
            if (mec == "ordina" || semplificato)
            {
                var d2 = Semplificato(mec);
                if (d2 != null && PuzzlePanel.Open(d2, OnSolved) != null) return;
            }
            if (EsaminaView.Apri(def, OnSolved) == null) UIRoot.I.Toast(GameData.TestiEnigmi.nonDisponibile);
        }

        /// <summary>Versione 2D: stessa soluzione con un pannello classico (lucchetto, ordina, quiz).</summary>
        EnigmaDef Semplificato(string mec)
        {
            var c = JsonUtility.FromJson<EnigmaDef>(JsonUtility.ToJson(def));
            if (mec == "ordina") { c.tipo = "ordina"; return c.eventi.Count > 0 ? c : null; }
            if (mec == "ruote" && !string.IsNullOrEmpty(def.codice)) { c.tipo = "lucchetto"; return c; }
            // le domande della versione semplificata portano alla stessa conclusione: quiz con le opzioni dell'enigma
            var q = c.domande.FindAll(d => d.opzioni != null && d.opzioni.Count > 0);
            if (q.Count == 0) return null;
            c.tipo = "quiz"; c.domande = q;
            return c;
        }

        void OnSolved()
        {
            UltimoProgresso = Time.time;
            MondoStato.Completa(def, chiave);
            // un solo annuncio: l'appunto se c'e', altrimenti il primo oggetto prodotto
            if (!string.IsNullOrEmpty(def.appunto)) { if (UIRoot.I != null) UIRoot.I.Toast(string.Format(GameData.TestiEnigmi.quadernoFmt, def.appunto), 5f); }
            else if (def.produce != null && def.produce.Count > 0) MondoStato.Annuncia(def.produce[0]);
            Aggiorna();
            if (glow != null) glow.on = false;
            var cb = alRisolto;
            if (cb != null) cb(this);
            if (!string.IsNullOrEmpty(def.finale)) AvviaRicordo(def.finale);
        }

        void AvviaRicordo(string oggettoId)
        {
            var s = GameManager.I.Save;
            s.ritornoScena = SceneManager.GetActiveScene().name;
            s.ritornoSpawn = "oggetto_" + oggettoId;
            GameManager.I.SaveGame();
            AudioManager.FireAt("raccolta", transform.position);
            GameManager.I.StartFlashback(oggettoId);
        }
    }

    /// <summary>
    /// R009/R020: all'ingresso di hub e basso crea gli oggetti-enigma elencati in enigmi_mondo.json per quella scena
    /// e nasconde le scarpe del basso finche' il baule non e' stato aperto.
    /// </summary>
    public static class MondoSpawner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aggancia()
        {
            SceneManager.sceneLoaded -= OnScena;
            SceneManager.sceneLoaded += OnScena;
        }

        static void OnScena(Scene sc, LoadSceneMode mode)
        {
            if (GameManager.IsMenuScene(sc.name)) return;
            if (mode == LoadSceneMode.Additive && GameManager.MenuBackdrop) return;
            EnigmaMondo.UltimoProgresso = Time.time;
            GameObject radice = null;
            foreach (var e in GameData.EnigmiMondo)
            {
                if (e.scena != sc.name) continue;
                if (radice == null)
                {
                    radice = new GameObject("EnigmiMondo");
                    SceneManager.MoveGameObjectToScene(radice, sc);
                }
                EnigmaMondo.Crea(e, "mondo/" + e.id, radice.transform);
                Sostituisci(sc, e.sostituisce);
            }
            NascondiRicordi(sc);
        }

        /// <summary>Nasconde gli oggetti di scena che l'enigma rimpiazza (stesso posto, stesso ruolo, ora esaminabili).</summary>
        public static void Sostituisci(Scene sc, string nomi)
        {
            if (string.IsNullOrEmpty(nomi)) return;
            var cerca = new HashSet<string>();
            foreach (var n in nomi.Split(',')) cerca.Add(n.Trim());
            foreach (var root in sc.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (cerca.Contains(t.name) && t.GetComponentInParent<EnigmaMondo>() == null) t.gameObject.SetActive(false);
        }

        /// <summary>L'oggetto-ricordo di un enigma con `finale` resta nascosto finche' l'enigma non e' risolto (o l'oggetto e' gia' nell'inventario).</summary>
        static void NascondiRicordi(Scene sc)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            foreach (var e in GameData.EnigmiMondo)
            {
                if (e.scena != sc.name || string.IsNullOrEmpty(e.finale)) continue;
                if (gm.Save.inventario.Contains(e.finale) || MondoStato.Risolto("mondo/" + e.id)) continue;
                foreach (var p in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
                    if (p.oggettoId == e.finale) p.gameObject.SetActive(false);
            }
        }
    }
}
