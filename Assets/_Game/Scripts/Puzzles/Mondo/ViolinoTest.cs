#if UNITY_EDITOR || DEBUG
// R015: prova automatica del capitolo "Il violino", solo negli editor e nelle build di sviluppo (mai nella build normale).
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
    /// R015: gioca il capitolo "Il violino" dall'inizio alla fine con le API pubbliche (come CappottoTest): lo spartito compare nel cassettino del basso solo dopo
    /// "Il cappotto", cutscene d'ingresso, Modena (70_FB_Modena) con il forno, gli attrezzi, il pianoforte, le impronte e la melodia, salvataggio e ricarica a meta',
    /// uscita con lo spartito in inventario, versione "Enigmi semplificati", mappa, obiettivi e segreti.
    /// Attiva SOLO con -violinotest [-violinotestout cartella] (nell'editor: SessionState "violinotest", menu Treno > Scene > Prova automatica ricordo Violino).
    /// Scrive violinotest.log, OK.txt / FALLITA.txt; poi chiude.
    /// </summary>
    public class ViolinoTest : MonoBehaviour
    {
        const string Modena = "70_FB_Modena", Basso = "20_Int_BassoAntonietta";
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-violinotest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("violinotest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("ViolinoTest");
            DontDestroyOnLoad(go);
            go.AddComponent<ViolinoTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s) { Debug.Log("[ViolinoTest] " + s); File.AppendAllText(logPath, s + "\n", Encoding.UTF8); }
        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Arg("-violinotestout", Path.Combine(Application.dataPath, "..", "violinotest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "violinotest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova Violino. Salvataggio presente: " + avevaSave);
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
            UnityEditor.SessionState.SetBool("violinotest", false);
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

        int EnigmiInScena() { return EnigmaMondo.Tutti.FindAll(e => e != null && !e.segreto && e.gameObject.scene.name == SceneManager.GetActiveScene().name && e.chiave.StartsWith("flashback_violino/")).Count; }

        void ResetSalvataggio(bool cappottoFatto)
        {
            var s = GameManager.I.Save;
            s.inventario.Clear(); s.flashbackCompletati.Clear(); s.enigmiRisolti.Clear(); s.cutsceneViste.Clear(); s.oggettiEnigma.Clear();
            s.segretiTrovati.Clear(); s.segretiFlag.Clear();
            s.ritornoScena = ""; s.ritornoSpawn = ""; s.enigmiSemplificati = false; s.filmatoVisto = true;
            foreach (var o in new[] { "scarpe", "stracci", "volantino" }) s.inventario.Add(o);
            foreach (var f in new[] { "flashback_scarpe", "flashback_stracci", "flashback_volantino" }) s.flashbackCompletati.Add(f);
            foreach (var t in new[] { "porta_trovata", "chiave_basso", "porta_aperta" }) s.oggettiEnigma.Add(t);
            if (cappottoFatto) { s.inventario.Add("cappotto"); s.flashbackCompletati.Add("flashback_cappotto"); }
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
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) if (p.oggettoId == "spartito") return p;
            return null;
        }

        /// <summary>Porta il forno nella finestra giusta con legna e serranda (avanti veloce), poi chiude la serranda.</summary>
        void ForneGiusto()
        {
            var f = V.Def.forno;
            for (int k = 0; k < 60; k++)
            {
                if (V.Temperatura < f.giusto0) V.Legna();
                else if (V.Temperatura > f.giusto1) { if (!V.SerrandaAperta) V.Serranda(); V.AvanzaForno(1f); }
                else break;
            }
            if (V.SerrandaAperta) V.Serranda();
        }

        IEnumerator FineFrase()
        {
            float t = 0f;
            yield return Attendi(0.2f);
            while (V != null && V.StaSuonando && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Attendi(0.2f);
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");

            // ---- 1. lo spartito compare nel cassettino del basso solo dopo il cappotto
            ResetSalvataggio(false);
            yield return Vai(Basso, "ingresso");
            Check("senza il cappotto lo spartito non c'e'", Pickup() == null);
            ResetSalvataggio(true);
            yield return Vai(Basso, "ingresso");
            var pick = Pickup();
            Check("dopo il cappotto lo spartito e' nel basso", pick != null);
            if (pick == null) yield break;
            var p = pick.transform.position;
            Check("lo spartito e' sulla credenza della cucina (x 1,5..2,6 - y 0,8..1,3 - z 6,3..7,6)", p.x > 1.5f && p.x < 2.6f && p.y > 0.8f && p.y < 1.3f && p.z > 6.3f && p.z < 7.6f, p.ToString("F2"));
            Check("lo spartito ha un collider per il raggio di interazione", pick.GetComponent<Collider>() != null);

            // ---- 2. cutscene d'ingresso e Modena
            pick.Interact();
            yield return Attendi(1.0f);
            Check("parte la cutscene d'ingresso", UIRoot.I.Cutscene.IsPlaying);
            yield return ChiudiTutto(40);
            yield return ScenaPronta(Modena);
            Check("si entra in " + Modena, SceneManager.GetActiveScene().name == Modena);
            Check("cutscene d'ingresso registrata", GameManager.I.Save.cutsceneViste.Contains("cutscene_violino#ingresso"));
            yield return ChiudiTutto(6);
            Check("Modena: cinque enigmi", EnigmiInScena() == 5, EnigmiInScena().ToString());
            Check("Modena: l'uscita dal ricordo (cancello)", FindAnyObjectByType<EscapeExit>() != null);
            Check("Modena: la neve che cade", GameObject.Find("NeveCade") != null);
            var att = Trova("enigma_attrezzi");
            Check("V2 bloccato prima del pane", att != null && !att.Disponibile);

            // ---- 3. V1 il forno
            yield return ApriEnigma("enigma_forno");
            if (V != null)
            {
                Check("V1: meccanismo forno", EsaminaView.Meccanismo(V.Def) == "forno");
                Check("V1: senza leggere il forno non si inforna", !V.Inforna() && V.FaseForno == 0);
                V.Farina();
                Check("V1: forno freddo -> farina bianca, lettura non buona", !V.FornoLetto && V.Cliccabile("sacchetto_farina"));
                for (int k = 0; k < 8; k++) V.Legna();
                V.Farina();
                Check("V1: troppa legna -> farina nera", !V.FornoLetto && V.Temperatura > V.Def.forno.giusto1, V.Temperatura.ToString("0"));
                ForneGiusto();
                V.Farina();
                Check("V1: legna e serranda -> nocciola", V.FornoLetto, V.Temperatura.ToString("0"));
                Check("V1: si inforna", V.Inforna() && V.FaseForno == 1);
                Check("V1: subito fuori e' pallido -> si rimette dentro", !V.Sforna() && V.FaseForno == 1);
                V.AvanzaForno(V.Def.forno.cottura + V.Def.forno.tardi + 1f);
                Check("V1: dimenticato nel forno -> bruciato, si ricomincia (nessuna penalita')", V.FaseForno == 0 && !V.Risolto);
                ForneGiusto();
                V.Farina();
                V.Inforna();
                V.AvanzaForno(V.Def.forno.cottura);
                Check("V1: clessidra vuota -> si sforna, risolto", V.Sforna() && V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V1: produce pane_alcide", MondoStato.Ha("pane_alcide") && MondoStato.Risolto("flashback_violino/enigma_forno"));

            // ---- 4. V2 gli attrezzi che suonano
            yield return ApriEnigma("enigma_attrezzi");
            if (V != null)
            {
                Check("V2: meccanismo suoni", EsaminaView.Meccanismo(V.Def) == "suoni");
                Check("V2: cinque suoni da riconoscere", V.Def.suoni.Count == 5);
                Check("V2: toccare un attrezzo prima di ascoltare non conta", !V.Attrezzo("chiave") && V.TurnoSuono == 0);
                V.AscoltaSuono();
                Check("V2: Ascolta disegna l'onda (alternativa visiva)", V.SuonoSentito && V.UltimoMessaggio.Contains(V.Def.suoni[0].onda));
                string sbagliato = V.Def.suoni[0].attrezzo == "chiave" ? "feltri" : "chiave";
                Check("V2: attrezzo sbagliato -> errore gentile", !V.Attrezzo(sbagliato) && V.TurnoSuono == 0);
                foreach (var s in V.Def.suoni) { V.AscoltaSuono(); V.Attrezzo(s.attrezzo); }
                Check("V2: cinque suoni riconosciuti -> risolto", V.Risolto && V.TurnoSuono == 5);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V2: produce diapason", MondoStato.Ha("diapason"));

            // ---- 5. V3 il pianoforte stonato (battimenti)
            yield return ApriEnigma("enigma_pianoforte");
            if (V != null)
            {
                Check("V3: meccanismo accordatura", EsaminaView.Meccanismo(V.Def) == "accordatura");
                Check("V3: senza scegliere la corda la chiave non gira", !V.Giro(1));
                V.ScegliCorda(0);
                float s0 = Mathf.Abs(V.Scarto(0));
                V.Giro(Math.Sign(V.Scarto(0)));
                Check("V3: girare dalla parte sbagliata accelera il battimento", Mathf.Abs(V.Scarto(0)) > s0 && !V.Risolto);
                for (int c = 0; c < V.Def.corde.Count; c++)
                {
                    V.ScegliCorda(c);
                    for (int k = 0; k < 40 && Mathf.Abs(V.Scarto(c)) > 0.01f; k++) V.Giro(-Math.Sign(V.Scarto(c)));
                }
                Check("V3: tre corde senza battimento -> risolto", V.Risolto && V.CordeAccordate == 3);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V3: produce violino_promesso", MondoStato.Ha("violino_promesso"));

            // ---- salvataggio e ricarica a meta'
            GameManager.I.SaveGame();
            yield return Vai(Modena, "inizio");
            yield return ChiudiTutto(4);
            Check("ricarica: V1-V3 ancora risolti", MondoStato.Risolto("flashback_violino/enigma_forno") && MondoStato.Risolto("flashback_violino/enigma_attrezzi") && MondoStato.Risolto("flashback_violino/enigma_pianoforte"));
            Check("ricarica: V4 disponibile", Trova("enigma_impronte") != null && Trova("enigma_impronte").Disponibile);

            // ---- 6. V4 le impronte nella neve
            yield return ApriEnigma("enigma_impronte");
            if (V != null)
            {
                Check("V4: meccanismo impronte", EsaminaView.Meccanismo(V.Def) == "impronte");
                Check("V4: la pista non si segue prima di sapere di chi sono le orme", !V.SeguiRamo("ramo_1_a"));
                V.ToccaScarpone(1);
                Check("V4: lo scarpone di Luzio non combacia con le orme della porta", !V.Confronta(1, "orma_porta") && V.Colpevole < 0);
                V.ToccaScarpone(0);
                Check("V4: lo scarpone di Rivo combacia", V.Confronta(0, "orma_porta") && V.Colpevole == 0);
                Check("V4: ramo sbagliato -> errore gentile", !V.SeguiRamo("ramo_1_b") && V.PassoPista == 0);
                V.SeguiRamo("ramo_1_a"); V.SeguiRamo("ramo_2_b");
                Check("V4: due bivi giusti", V.PassoPista == 2 && !V.Risolto);
                V.SeguiRamo("ramo_3_a");
                Check("V4: la legnaia -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V4: produce violino_ritrovato", MondoStato.Ha("violino_ritrovato"));
            Check("V4: parte la cutscene di meta' (Rivo restituisce il violino)", GameManager.I.Save.cutsceneViste.Contains("cutscene_violino#meta"));

            // ---- 7. V5 la prima melodia
            yield return ApriEnigma("enigma_melodia");
            if (V != null)
            {
                Check("V5: meccanismo melodia", EsaminaView.Meccanismo(V.Def) == "melodia");
                Check("V5: prima di ascoltare le note non contano", !V.SuonaNota(0) && V.PosizioneNota == 0);
                V.AscoltaFrase();
                Check("V5: Alcide suona la frase", V.StaSuonando);
                yield return FineFrase();
                var f0 = V.Def.frasi[0].note;
                int sbagliata = (f0[0] + 1) % V.Def.note.Count;
                Check("V5: nota sbagliata -> Alcide la rifa' piu' lenta", !V.SuonaNota(sbagliata) && V.Frase == 0);
                yield return Attendi(1.4f);
                yield return FineFrase();
                foreach (var n in f0) V.SuonaNota(n);
                Check("V5: prima frase ripetuta", V.Frase == 1 && !V.Risolto);
                V.AscoltaFrase();
                yield return FineFrase();
                foreach (var n in V.Def.frasi[1].note) V.SuonaNota(n);
                Check("V5: seconda frase -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V5: produce spartito_alcide", MondoStato.Ha("spartito_alcide"));

            // ---- 8. uscita dal ricordo
            var esci = FindAnyObjectByType<EscapeExit>();
            if (esci != null) esci.Interact();
            yield return Attendi(1.0f);
            yield return ChiudiTutto(60);
            yield return ScenaPronta(Basso);
            var sv = GameManager.I.Save;
            Check("uscita: si torna al basso", SceneManager.GetActiveScene().name == Basso);
            Check("uscita: lo spartito e' in inventario", sv.inventario.Contains("spartito"));
            Check("uscita: flashback_violino completato", sv.flashbackCompletati.Contains("flashback_violino"));
            Check("uscita: cutscene di chiusura vista", sv.cutsceneViste.Contains("cutscene_violino#chiusura"));
            Check("uscita: il cassettino non c'e' piu'", Pickup() == null);
            var mod = GameData.Luogo("modena");
            Check("modena: il messaggio indica il cassettino", mod != null && !string.IsNullOrEmpty(mod.sbloccatoMsg));

            // ---- 9. versione "Enigmi semplificati"
            ResetSalvataggio(true);
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.SaveGame();
            yield return Vai(Modena, "inizio");
            yield return ChiudiTutto(4);
            foreach (var e in new List<EnigmaMondo>(EnigmaMondo.Tutti))
            {
                if (e == null || e.segreto || e.gameObject.scene.name != Modena || !e.chiave.StartsWith("flashback_violino/")) continue;
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
            GameManager.I.Save.enigmiSemplificati = false;

            // ---- 10. mappa, obiettivi, segreti
            Check("mappa: scena 70 con strade e punti", GameData.MappaScena(Modena) != null && GameData.MappaScena(Modena).strade.Count >= 5);
            int obiettivi = 0; foreach (var o in GameData.Mappe.obiettivi) if (o.capitolo == "flashback_violino") obiettivi++;
            Check("obiettivi: cinque passi del capitolo", obiettivi == 5, obiettivi.ToString());
            int segreti = 0; foreach (var s in GameData.Segreti.segreti) if (s.scena == Modena) segreti++;
            Check("segreti: un enigma segreto e cinque collezionabili", segreti == 6, segreti.ToString());
            Check("segreti: stanza di Modena con cinque condizioni", GameData.Segreti.stanze.Exists(st => st.scena == Modena && st.completo.Count == 5));
            ResetSalvataggio(true);
        }
    }
}
#endif
