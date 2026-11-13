#if UNITY_EDITOR || DEBUG
// R016: prova automatica del finale, solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Core
{
    using TrenoBambini.Puzzles;

    /// <summary>
    /// R016: gioca il finale dall'inizio alla fine con le API pubbliche: il telefono compare nel basso solo dopo "Il violino", cutscene d'ingresso,
    /// 80_Basso_1994 con il basso svuotato, i cinque ricordi al loro posto (e la stanza che torna al 1946), la ricevuta, la custodia, Carmine;
    /// salvataggio e ricarica a meta', cutscene di chiusura, flag finale_visto, titoli di coda, ritorno al menu; versione semplificata, mappa, segreti.
    /// Attiva SOLO con -finaletest [-finaletestout cartella] (nell'editor: SessionState "finaletest", menu Treno > Scene > Prova automatica finale).
    /// </summary>
    public class FinaleTest : MonoBehaviour
    {
        const string Finale = "80_Basso_1994", Basso = "20_Int_BassoAntonietta";
        static readonly string[] Args = Environment.GetCommandLineArgs();
        static readonly string[] Ricordi = { "scarpe", "stracci", "volantino", "cappotto", "spartito" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-finaletest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("finaletest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("FinaleTest");
            DontDestroyOnLoad(go);
            go.AddComponent<FinaleTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s) { Debug.Log("[FinaleTest] " + s); File.AppendAllText(logPath, s + "\n", Encoding.UTF8); }
        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Arg("-finaletestout", Path.Combine(Application.dataPath, "..", "finaletest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "finaletest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            bool fine = false;
            StartCoroutine(Corsa(() => fine = true));
            float t = 0f;
            while (!fine && t < 600f) { t += Time.unscaledDeltaTime; yield return null; }
            if (!fine) Check("la prova e' terminata entro 10 minuti", false);
            if (avevaSave) PlayerPrefs.SetString("treno_save", saveBackup); else PlayerPrefs.DeleteKey("treno_save");
            PlayerPrefs.Save();
            Log("Fine prova: " + ok + " PASS, " + ko + " FAIL");
            File.WriteAllText(Path.Combine(dir, ko == 0 ? "OK.txt" : "FALLITA.txt"), ok + " PASS, " + ko + " FAIL\n");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("finaletest", false);
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Corsa(Action fine)
        {
            Exception err = null;
            var it = Flusso();
            while (true)
            {
                bool avanti;
                try { avanti = it.MoveNext(); } catch (Exception e) { err = e; break; }
                if (!avanti) break;
                yield return it.Current;
            }
            if (err != null) Check("nessuna eccezione nella prova", false, err.ToString());
            fine();
        }

        IEnumerator Attendi(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator ScenaPronta(string scena)
        {
            float t = 0f;
            while (t < 30f)
            {
                t += Time.unscaledDeltaTime;
                if (SceneManager.GetActiveScene().name == scena && SceneLoader.I != null && !SceneLoader.I.IsLoading && SceneManager.sceneCount == 1) break;
                yield return null;
            }
            yield return Attendi(1.0f);
        }

        IEnumerator Vai(string scena, string spawn) { SceneLoader.I.Load(scena, spawn); yield return ScenaPronta(scena); }

        IEnumerator ChiudiTutto(int giri = 30)
        {
            for (int giro = 0; giro < giri; giro++)
            {
                Button trovato = null;
                foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
                {
                    var tx = b.GetComponentInChildren<TextMeshProUGUI>();
                    if (tx != null && (tx.text == "Continua" || tx.text == "Avanti" || tx.text == "Salta") && b.interactable && b.gameObject.activeInHierarchy) { trovato = b; break; }
                }
                bool cs = UIRoot.I != null && UIRoot.I.Cutscene != null && UIRoot.I.Cutscene.IsPlaying;
                if (trovato == null && !cs && EsaminaView.Current == null && !(SceneLoader.I != null && SceneLoader.I.IsLoading)) yield break;
                if (trovato != null) trovato.onClick.Invoke();
                yield return Attendi(0.5f);
            }
        }

        EnigmaMondo Trova(string id)
        {
            foreach (var e in EnigmaMondo.Tutti) if (e != null && e.def != null && e.def.id == id && e.gameObject.scene.name == SceneManager.GetActiveScene().name) return e;
            return null;
        }

        int EnigmiInScena() { return EnigmaMondo.Tutti.FindAll(e => e != null && !e.segreto && e.gameObject.scene.name == SceneManager.GetActiveScene().name && e.chiave.StartsWith("flashback_finale/")).Count; }

        void ResetSalvataggio(bool violinoFatto)
        {
            var s = GameManager.I.Save;
            s.inventario.Clear(); s.flashbackCompletati.Clear(); s.enigmiRisolti.Clear(); s.cutsceneViste.Clear(); s.oggettiEnigma.Clear();
            s.segretiTrovati.Clear(); s.segretiFlag.Clear();
            s.ritornoScena = ""; s.ritornoSpawn = ""; s.enigmiSemplificati = false; s.filmatoVisto = true;
            foreach (var o in new[] { "scarpe", "stracci", "volantino", "cappotto" }) s.inventario.Add(o);
            foreach (var f in new[] { "flashback_scarpe", "flashback_stracci", "flashback_volantino", "flashback_cappotto" }) s.flashbackCompletati.Add(f);
            foreach (var t in new[] { "porta_trovata", "chiave_basso", "porta_aperta" }) s.oggettiEnigma.Add(t);
            if (violinoFatto) { s.inventario.Add("spartito"); s.flashbackCompletati.Add("flashback_violino"); }
            s.scena = Basso; s.spawn = "ingresso";
            GameManager.I.SaveGame();
        }

        IEnumerator ApriEnigma(string id)
        {
            var e = Trova(id);
            Check(id + ": oggetto presente nel mondo", e != null);
            if (e == null) yield break;
            e.Interact();
            yield return Attendi(1.0f);
            if (V == null) yield return ChiudiTutto(3);
            yield return Attendi(0.5f);
            Check(id + ": si apre la vista ravvicinata", V != null);
        }

        IEnumerator Chiudi()
        {
            if (V != null) { if (V.Risolto) V.Finito(); else V.Chiudi(); }
            yield return Attendi(0.8f);
            yield return ChiudiTutto(40);
        }

        ItemPickup Pickup()
        {
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) if (p.oggettoId == "violino") return p;
            return null;
        }

        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");

            // ---- 1. il telefono compare nel basso solo dopo il violino
            ResetSalvataggio(false);
            yield return Vai(Basso, "ingresso");
            Check("senza il ricordo del violino il telefono non c'e'", Pickup() == null);
            ResetSalvataggio(true);
            yield return Vai(Basso, "ingresso");
            var pick = Pickup();
            Check("dopo il violino il telefono e' nel basso", pick != null);
            if (pick == null) yield break;

            // ---- 2. ingresso e basso del 1994
            pick.Interact();
            yield return Attendi(1.0f);
            Check("parte la cutscene d'ingresso", UIRoot.I.Cutscene.IsPlaying);
            yield return ChiudiTutto(40);
            yield return ScenaPronta(Finale);
            Check("si entra in " + Finale, SceneManager.GetActiveScene().name == Finale);
            yield return ChiudiTutto(6);
            Check("finale: nove postazioni", EnigmiInScena() == 9, EnigmiInScena().ToString());
            var dt = FindAnyObjectByType<DueTempi>();
            Check("finale: la stanza a due tempi con cinque angoli", dt != null && dt.angoli.Count == 5);
            Check("finale: nessuna scarpa da raccogliere (le scarpe si posano)", FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude).Length == 0);
            var posto = Trova("enigma_posto_scarpe");
            Check("i posti sono bloccati prima della latta", posto != null && !posto.Disponibile);

            // ---- 3. N1 il basso svuotato
            yield return ApriEnigma("enigma_basso94");
            if (V != null)
            {
                V.Clicca("telefono"); V.Clicca("calendario");
                Check("N1: due cose su tre non bastano", !V.Risolto);
                V.Clicca("cartellino");
                Check("N1: telefono, calendario e cartellino -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("N1: produce elenco_posti e cartellino_112", MondoStato.Ha("elenco_posti") && MondoStato.Ha("cartellino_112"));

            // ---- 4. N2 cinque ricordi al loro posto
            foreach (var r in Ricordi)
            {
                yield return ApriEnigma("enigma_posto_" + r);
                if (V == null) continue;
                Check("posto " + r + ": il vassoio c'e' nella vista ravvicinata", V.Cliccabile("ricordo_" + r));
                string altro = r == "scarpe" ? "cappotto" : "scarpe";
                V.Clicca("ricordo_" + altro);
                Check("posto " + r + ": il ricordo sbagliato -> «Qui non c'era»", !V.Risolto && V.UltimoMessaggio.Length > 0);
                V.Clicca("ricordo_" + r);
                Check("posto " + r + ": il ricordo giusto -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
                yield return Attendi(0.5f);
                Check("posto " + r + ": l'angolo torna al 1946", dt != null && dt.Ricordati > 0);
                Check("posto " + r + ": un frammento", MondoStato.Ha("frammento_" + r));
                yield return Attendi(dt != null ? dt.durata + 3f : 1f);
            }

            // ---- salvataggio e ricarica a meta'
            GameManager.I.SaveGame();
            yield return Vai(Finale, "inizio");
            yield return ChiudiTutto(4);
            dt = FindAnyObjectByType<DueTempi>();
            Check("ricarica: i cinque posti restano risolti", MondoStato.Risolto("flashback_finale/enigma_posto_spartito"));
            yield return Attendi(1.0f);
            Check("ricarica: gli angoli gia' ricordati non si ripetono", dt != null && dt.Ricordati == 0);

            // ---- 5. N3 la ricevuta
            yield return ApriEnigma("enigma_ricevuta");
            if (V != null)
            {
                Check("N3: meccanismo ricomponi", EsaminaView.Meccanismo(V.Def) == "ricomponi");
                Check("N3: pezzo nel posto sbagliato -> non combacia", !V.Posa("pezzo_0", 1) && V.PezziPosati == 0);
                for (int k = 0; k < 6; k++) V.Posa("pezzo_" + k, k);
                Check("N3: sei pezzi -> risolto", V.Risolto && V.PezziPosati == 6);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("N3: produce numero_polizza", MondoStato.Ha("numero_polizza"));

            // ---- 6. N4 la custodia
            yield return ApriEnigma("enigma_custodia");
            if (V != null)
            {
                V.Clicca("fermo");
                Check("N4: con il numero sbagliato la custodia resta chiusa", !V.Risolto);
                V.ImpostaRuote("4712");
                V.Clicca("fermo");
                Check("N4: 4712 -> la custodia si apre", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("N4: cutscene di meta' (Amerigo suona nel basso)", GameManager.I.Save.cutsceneViste.Contains("cutscene_finale#meta"));

            // ---- 7. N5 Carmine
            yield return ApriEnigma("enigma_carmine");
            if (V != null)
            {
                Check("N5: meccanismo dialogo", EsaminaView.Meccanismo(V.Def) == "dialogo");
                Check("N5: ogni risposta va bene (qui: suonare)", V.Rispondi(0, 2) && V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }

            // ---- 8. chiusura, titoli di coda, menu
            var esci = FindAnyObjectByType<EscapeExit>();
            if (esci != null) esci.Interact();
            yield return Attendi(1.0f);
            yield return ChiudiTutto(60);
            float t = 0f;
            while (!TitoliDiCoda.InCorso && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            Check("partono i titoli di coda", TitoliDiCoda.InCorso);
            var sv = GameManager.I.Save;
            Check("il violino e' in inventario e il finale e' completato", sv.inventario.Contains("violino") && sv.flashbackCompletati.Contains("flashback_finale"));
            Check("flag finale_visto (R011)", sv.segretiFlag.Contains("finale_visto"));
            Check("cutscene di chiusura vista", sv.cutsceneViste.Contains("cutscene_finale#chiusura"));
            TrenoBambini.Album.Trofei.Controlla(false);
            Check("R027: trofeo del finale e trofeo civico della famiglia sbloccati", sv.trofei.Contains("storia_finale") && sv.trofei.Contains("civico_famiglia"));
            Check("R027: lo sblocco e' nel salvataggio", PlayerPrefs.GetString("treno_save", "").Contains("storia_finale"));
            t = 0f;
            while (SceneManager.GetActiveScene().name != GameManager.SceneTitolo && t < 240f) { t += Time.unscaledDeltaTime; yield return null; }
            Check("dopo i titoli si torna al menu principale", SceneManager.GetActiveScene().name == GameManager.SceneTitolo);

            // ---- 9. versione semplificata, mappa, obiettivi, segreti
            ResetSalvataggio(true);
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.SaveGame();
            yield return Vai(Finale, "inizio");
            yield return ChiudiTutto(4);
            int semplici = 0;
            foreach (var e in EnigmaMondo.Tutti) if (e != null && !e.segreto && e.chiave.StartsWith("flashback_finale/") && e.def.domande.Count >= 3) semplici++;
            Check("ogni postazione ha le domande della versione semplificata", semplici == 9, semplici.ToString());
            GameManager.I.Save.enigmiSemplificati = false;
            Check("mappa: scena 80", GameData.MappaScena(Finale) != null && GameData.MappaScena(Finale).strade.Count == 4);
            int obiettivi = 0; foreach (var o in GameData.Mappe.obiettivi) if (o.capitolo == "flashback_finale") obiettivi++;
            Check("obiettivi: cinque passi del finale", obiettivi == 5, obiettivi.ToString());
            int segreti = 0; foreach (var s in GameData.Segreti.segreti) if (s.scena == Finale) segreti++;
            Check("segreti: la radio e cinque collezionabili", segreti == 6, segreti.ToString());
            ResetSalvataggio(true);
        }
    }
}
#endif
