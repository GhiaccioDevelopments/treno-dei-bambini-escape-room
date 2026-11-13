#if UNITY_EDITOR || DEBUG
// R014: prova automatica del capitolo "Il cappotto", solo negli editor e nelle build di sviluppo (mai nella build normale).
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
    /// R014: gioca il capitolo "Il cappotto" dall'inizio alla fine con le API pubbliche (come VolantinoTest): il cappottino compare dietro la porta del basso solo dopo
    /// "Il volantino", cutscene d'ingresso, stazione (60_FB_Stazione) con i cartellini, i cappotti e la mela, la porta della carrozza chiusa fino al saluto, il vagone
    /// (61_FB_Vagone) con il finestrino e il dialogo, salvataggio e ricarica a meta', uscita con l'oggetto in inventario e Modena sbloccata, versione "Enigmi semplificati",
    /// mappa, obiettivi e segreti delle due scene.
    /// Attiva SOLO con -cappottotest [-cappottotestout cartella] (nell'editor: SessionState "cappottotest", menu Treno > Scene > Prova automatica ricordo Cappotto).
    /// Scrive cappottotest.log, OK.txt / FALLITA.txt; poi chiude.
    /// </summary>
    public class CappottoTest : MonoBehaviour
    {
        const string Stazione = "60_FB_Stazione", Vagone = "61_FB_Vagone", Basso = "20_Int_BassoAntonietta";
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-cappottotest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("cappottotest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("CappottoTest");
            DontDestroyOnLoad(go);
            go.AddComponent<CappottoTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s) { Debug.Log("[CappottoTest] " + s); File.AppendAllText(logPath, s + "\n", Encoding.UTF8); }
        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Arg("-cappottotestout", Path.Combine(Application.dataPath, "..", "cappottotest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "cappottotest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova Cappotto. Salvataggio presente: " + avevaSave);
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
            UnityEditor.SessionState.SetBool("cappottotest", false);
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

        // ------------------------------------------------------------------ utilita'
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

        IEnumerator Vai(string scena, string spawn)
        {
            SceneLoader.I.Load(scena, spawn);
            yield return ScenaPronta(scena);
        }

        /// <summary>Preme il primo pulsante Continua/Avanti/Salta visibile finche' ce ne sono (messaggi modali e cutscene).</summary>
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

        int EnigmiInScena() { return EnigmaMondo.Tutti.FindAll(e => e != null && !e.segreto && e.gameObject.scene.name == SceneManager.GetActiveScene().name && e.chiave.StartsWith("flashback_cappotto/")).Count; }

        void ResetSalvataggio(bool volantinoFatto)
        {
            var s = GameManager.I.Save;
            s.inventario.Clear(); s.flashbackCompletati.Clear(); s.enigmiRisolti.Clear(); s.cutsceneViste.Clear(); s.oggettiEnigma.Clear();
            s.segretiTrovati.Clear(); s.segretiFlag.Clear();
            s.ritornoScena = ""; s.ritornoSpawn = ""; s.enigmiSemplificati = false; s.filmatoVisto = true;
            foreach (var o in new[] { "scarpe", "stracci" }) s.inventario.Add(o);
            foreach (var f in new[] { "flashback_scarpe", "flashback_stracci" }) s.flashbackCompletati.Add(f);
            foreach (var t in new[] { "porta_trovata", "chiave_basso", "porta_aperta" }) s.oggettiEnigma.Add(t);
            if (volantinoFatto) { s.inventario.Add("volantino"); s.flashbackCompletati.Add("flashback_volantino"); }
            s.scena = Basso; s.spawn = "ingresso";
            GameManager.I.SaveGame();
        }

        /// <summary>Apre l'enigma con il vero Interact e attende la vista ravvicinata.</summary>
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
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) if (p.oggettoId == "cappotto") return p;
            return null;
        }

        Door PortaTreno()
        {
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Exclude)) if (d.luogoId == "ricordo_vagone") return d;
            return null;
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");

            // ---- 1. il cappottino compare dietro la porta del basso solo dopo il volantino
            ResetSalvataggio(false);
            yield return Vai(Basso, "ingresso");
            Check("senza il volantino il cappottino non c'e'", Pickup() == null);
            ResetSalvataggio(true);
            yield return Vai(Basso, "ingresso");
            var pick = Pickup();
            Check("dopo il volantino il cappottino e' nel basso", pick != null);
            if (pick == null) yield break;
            var p = pick.transform.position;
            Check("il cappottino e' appeso dietro la porta (x 0,6..1,8 - y 0,8..1,6 - z -2,6..-2,0)", p.x > 0.6f && p.x < 1.8f && p.y > 0.8f && p.y < 1.6f && p.z > -2.6f && p.z < -2.0f, p.ToString("F2"));
            Check("il cappottino ha un collider per il raggio di interazione", pick.GetComponent<Collider>() != null);

            // ---- 2. cutscene d'ingresso e stazione
            pick.Interact();
            yield return Attendi(1.0f);
            Check("parte la cutscene d'ingresso", UIRoot.I.Cutscene.IsPlaying);
            yield return ChiudiTutto(40);
            yield return ScenaPronta(Stazione);
            Check("si entra in " + Stazione, SceneManager.GetActiveScene().name == Stazione);
            Check("cutscene d'ingresso registrata", GameManager.I.Save.cutsceneViste.Contains("cutscene_cappotto#ingresso"));
            yield return ChiudiTutto(6);
            Check("stazione: tre enigmi (C1, C2, C3), gli altri due sono nel vagone", EnigmiInScena() == 3, EnigmiInScena().ToString());
            Check("stazione: nessuna uscita dal ricordo (l'uscita e' nel vagone)", FindAnyObjectByType<EscapeExit>() == null);
            Check("stazione: la porta della carrozza 214", PortaTreno() != null);
            Check("stazione: Antonietta sulla banchina (segnaposto)", GameObject.Find("Npc_Antonietta") != null);

            // ---- 3. C1 i cartellini
            var cap = Trova("enigma_cappotti");
            Check("C2 bloccato prima di C1", cap != null && !cap.Disponibile);
            yield return ApriEnigma("enigma_cartellini");
            if (V != null)
            {
                Check("C1: meccanismo cartellini", EsaminaView.Meccanismo(V.Def) == "cartellini");
                V.Clicca("riga_112");
                Check("C1: una riga del registro si legge", V.UltimoMessaggio.Length > 0 && !V.Risolto);
                // scambiati: 112 e 113 invertiti
                V.Appendi(0, "cartellino_115"); V.Appendi(1, "cartellino_112"); V.Appendi(2, "cartellino_114");
                Check("C1: tre cartellini appesi, non ancora controllati", V.CartelliniAppesi == 3 && !V.Risolto);
                V.Appendi(3, "cartellino_113");
                Check("C1: due cartellini scambiati -> errore gentile, non risolto", !V.Risolto && V.CartelliniAppesi == 4);
                V.Appendi(1, "cartellino_113");
                Check("C1: spostare un cartellino lo toglie dall'altro collo", V.CartelliniAppesi == 3, V.CartelliniAppesi.ToString());
                V.Indizio(); V.Indizio(); V.Indizio();
                Check("C1: tre indizi", V.LivelloIndizio == 3);
                V.Appendi(3, "cartellino_112");
                Check("C1: ogni cartellino al suo collo -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("C1: produce numero_112", MondoStato.Ha("numero_112") && MondoStato.Risolto("flashback_cappotto/enigma_cartellini"));

            // ---- 4. C2 i cappotti
            yield return ApriEnigma("enigma_cappotti");
            if (V != null)
            {
                Check("C2: meccanismo cappotti", EsaminaView.Meccanismo(V.Def) == "cappotti");
                Check("C2: i bambini ci sono nella vista ravvicinata", V.Cliccabile("fig_pasqualino") && V.Cliccabile("fig_amerigo"));
                Check("C2: troppo grande -> errore gentile", !V.Vesti(0, "cappotto_f") && V.Vestiti == 0);
                Check("C2: troppo piccolo -> errore gentile", !V.Vesti(2, "cappotto_a") && V.Vestiti == 0);
                Check("C2: misura giusta ma numero di un altro -> errore gentile", !V.Vesti(3, "cappotto_e") && V.Vestiti == 0);
                Check("C2: il cappottino a Pasqualino", V.Vesti(0, "cappotto_a"));
                V.Vesti(1, "cappotto_c"); V.Vesti(2, "cappotto_d");
                V.Vesti(3, "cappotto_b");
                Check("C2: quattro cappotti -> risolto", V.Risolto && V.Vestiti == 4);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("C2: produce cappotto e scarpe", MondoStato.Ha("cappotto_112") && MondoStato.Ha("scarpe_nuove"));
            Check("C2: parte la cutscene di meta' (le scarpe, la Pachiochia)", GameManager.I.Save.cutsceneViste.Contains("cutscene_cappotto#meta"));

            // ---- salvataggio e ricarica a meta'
            GameManager.I.SaveGame();
            yield return Vai(Stazione, "inizio");
            yield return ChiudiTutto(4);
            Check("ricarica: C1 e C2 ancora risolti", MondoStato.Risolto("flashback_cappotto/enigma_cartellini") && MondoStato.Risolto("flashback_cappotto/enigma_cappotti"));
            Check("ricarica: C3 disponibile", Trova("enigma_mela") != null && Trova("enigma_mela").Disponibile);

            // ---- 5. la porta della carrozza resta chiusa fino al saluto
            var porta = PortaTreno();
            if (porta != null)
            {
                porta.Interact();
                yield return Attendi(1.5f);
                Check("porta della carrozza chiusa prima del saluto", SceneManager.GetActiveScene().name == Stazione);
            }

            // ---- 6. C3 la mela
            yield return ApriEnigma("enigma_mela");
            if (V != null)
            {
                V.Clicca("valigia");
                Check("C3: la valigia senza la mela non fa niente", !V.Risolto);
                V.Clicca("mela");
                yield return Attendi(0.5f);
                Check("C3: la mela in mano", MondoStato.Ha("mela_annurca"));
                V.ScegliMano("mela_annurca");
                V.Clicca("valigia");
                Check("C3: la mela nella valigia -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("C3: produce partenza", MondoStato.Ha("partenza"));

            // ---- 7. si sale sulla carrozza 214
            porta = PortaTreno();
            Check("porta della carrozza presente", porta != null);
            if (porta == null) yield break;
            porta.Interact();
            yield return ScenaPronta(Vagone);
            yield return ChiudiTutto(4);
            Check("si sale nel vagone (" + Vagone + ")", SceneManager.GetActiveScene().name == Vagone);
            Check("vagone: due enigmi (C4, C5)", EnigmiInScena() == 2, EnigmiInScena().ToString());
            Check("vagone: uscita chiusa finche' mancano enigmi", FindAnyObjectByType<EscapeExit>() != null);
            Check("vagone: il paesaggio che scorre", FindAnyObjectByType<PaesaggioTreno>() != null);

            // ---- 8. C4 l'Italia dal finestrino
            yield return ApriEnigma("enigma_finestrino");
            if (V != null)
            {
                Check("C4: meccanismo finestrino", EsaminaView.Meccanismo(V.Def) == "finestrino");
                yield return Attendi(0.5f);
                Check("C4: tre citta' da segnare", V.CittaDaAppuntare == 3, V.CittaDaAppuntare.ToString());
                V.PortaCartello(0);
                Check("C4: un cartello delle ferrovie non si segna", !V.Appunta() && V.CittaAppuntate == 0, V.CartelloInVista);
                V.PortaCartello(1);
                Check("C4: Roma nel finestrino -> segnata", V.Appunta() && V.CittaAppuntate == 1, V.CartelloInVista);
                V.PortaCartello(4);
                V.PortaCartello(5); // Firenze passa senza essere segnata
                Check("C4: Firenze persa -> il controllore la ripete", V.ControlloreHaRipetuto);
                Check("C4: si segna lo stesso (nessuna penalita')", V.Appunta() && V.CittaAppuntate == 2);
                Check("C4: Modena prima di Bologna -> non ancora", !V.Arriva() && !V.Risolto);
                V.PortaCartello(7);
                V.Appunta();
                Check("C4: tre citta' segnate", V.CittaAppuntate == 3);
                V.Clicca("nome_modena");
                Check("C4: Modena sulla carta -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("C4: produce quasi_arrivati", MondoStato.Ha("quasi_arrivati"));

            // ---- 9. C5 i bambini che hanno paura
            yield return ApriEnigma("enigma_paura");
            if (V != null)
            {
                Check("C5: meccanismo dialogo", EsaminaView.Meccanismo(V.Def) == "dialogo");
                var fig = V.Def.figure;
                int sbagliata = fig[0].risposte.FindIndex(r => !r.giusta);
                Check("C5: una risposta che non aiuta non calma (e non e' un errore)", !V.Rispondi(0, sbagliata) && V.Calmati == 0 && !V.Risolto);
                for (int j = 0; j < fig.Count; j++) V.Rispondi(j, fig[j].risposte.FindIndex(r => r.giusta));
                Check("C5: tre bambini calmi -> risolto", V.Risolto && V.Calmati == 3, V.Calmati.ToString());
                yield return Attendi(0.6f);
                yield return Chiudi();
            }

            // ---- 10. uscita dal ricordo
            var esci = FindAnyObjectByType<EscapeExit>();
            if (esci != null) esci.Interact();
            yield return Attendi(1.0f);
            yield return ChiudiTutto(60);
            yield return ScenaPronta(Basso);
            var sv = GameManager.I.Save;
            Check("uscita: si torna al basso", SceneManager.GetActiveScene().name == Basso);
            Check("uscita: il cappotto e' in inventario", sv.inventario.Contains("cappotto"));
            Check("uscita: flashback_cappotto completato", sv.flashbackCompletati.Contains("flashback_cappotto"));
            Check("uscita: cutscene di chiusura vista", sv.cutsceneViste.Contains("cutscene_cappotto#chiusura"));
            Check("uscita: il cappottino non e' piu' dietro la porta", Pickup() == null);
            var mod = GameData.Luogo("modena");
            Check("modena: sbloccata dal cappotto", mod != null && mod.sbloccatoDa == "cappotto" && sv.inventario.Contains(mod.sbloccatoDa) && !string.IsNullOrEmpty(mod.sbloccatoMsg));
            var staz = GameData.Luogo("stazione");
            Check("stazione: indica il cappottino del basso", staz != null && staz.sbloccatoDa == "volantino" && !string.IsNullOrEmpty(staz.sbloccatoMsg));

            // ---- 11. versione "Enigmi semplificati"
            ResetSalvataggio(true);
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.SaveGame();
            foreach (var sc in new[] { Stazione, Vagone })
            {
                yield return Vai(sc, "inizio");
                yield return ChiudiTutto(4);
                foreach (var e in new List<EnigmaMondo>(EnigmaMondo.Tutti))
                {
                    if (e == null || e.segreto || e.gameObject.scene.name != sc || !e.chiave.StartsWith("flashback_cappotto/")) continue;
                    string id = e.def.id;
                    Check(id + " (semplificato): ha domande con opzioni", e.def.domande.Count >= 3 && e.def.domande[0].opzioni.Count >= 3);
                    foreach (var tk in e.def.richiede) MondoStato.Dai(tk);
                    e.Apri();
                    yield return Attendi(0.8f);
                    var pannelli = FindObjectsByType<PuzzlePanel>(FindObjectsInactive.Exclude);
                    Check(id + " (semplificato): si apre il pannello a quiz (non la vista 3D)", pannelli.Length > 0 && EsaminaView.Current == null, pannelli.Length.ToString());
                    foreach (var bt in FindObjectsByType<Button>(FindObjectsInactive.Exclude))
                    {
                        var tx = bt.GetComponentInChildren<TextMeshProUGUI>();
                        if (tx != null && tx.text == "Chiudi" && bt.gameObject.activeInHierarchy) { bt.onClick.Invoke(); break; }
                    }
                    yield return Attendi(0.8f);
                }
            }
            GameManager.I.Save.enigmiSemplificati = false;

            // ---- 12. mappa, obiettivi, segreti
            Check("mappa: scena 60 con strade e punti", GameData.MappaScena(Stazione) != null && GameData.MappaScena(Stazione).strade.Count >= 3);
            Check("mappa: scena 61 con strade e punti", GameData.MappaScena(Vagone) != null && GameData.MappaScena(Vagone).punti.Count >= 1);
            int obiettivi = 0; foreach (var o in GameData.Mappe.obiettivi) if (o.capitolo == "flashback_cappotto") obiettivi++;
            Check("obiettivi: cinque passi del capitolo", obiettivi == 5, obiettivi.ToString());
            int segreti = 0; foreach (var s in GameData.Segreti.segreti) if (s.scena == Stazione || s.scena == Vagone) segreti++;
            Check("segreti: un enigma segreto e cinque collezionabili nelle due scene", segreti == 6, segreti.ToString());
            Check("segreti: stanze della stazione (3 condizioni) e del vagone (2)", GameData.Segreti.stanze.Exists(st => st.scena == Stazione && st.completo.Count == 3) && GameData.Segreti.stanze.Exists(st => st.scena == Vagone && st.completo.Count == 2));
            ResetSalvataggio(true);
        }
    }
}
#endif
