#if UNITY_EDITOR || DEBUG
// R013: prova automatica del capitolo "Il volantino del treno", solo negli editor e nelle build di sviluppo (mai nella build normale).
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
    /// R013: gioca il capitolo "Il volantino del treno" dall'inizio alla fine con le API pubbliche (come StraccTest): il volantino compare sulla credenza solo dopo "Stracci",
    /// cutscene d'ingresso, scena 50_FB_Volantino, le voci del vicolo (anche ascoltate avvicinandosi), la bacheca a fili (collegamenti fissati a coppie, verdetti),
    /// il ciclostile (frasi, ordine delle fasi, manovella troppo veloce), il tavolo dei ruoli, il datario (date fuori ordine e in ordine), salvataggio e ricarica a meta',
    /// uscita con l'oggetto in inventario e la stazione sbloccata, versione "Enigmi semplificati", mappa, obiettivi e segreti della scena.
    /// Attiva SOLO con -volantinotest [-volantinotestout cartella] (nell'editor: SessionState "volantinotest", menu Treno > Scene > Prova automatica ricordo Volantino).
    /// Scrive volantinotest.log, OK.txt / FALLITA.txt; poi chiude.
    /// </summary>
    public class VolantinoTest : MonoBehaviour
    {
        const string Scena = "50_FB_Volantino";
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-volantinotest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("volantinotest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("VolantinoTest");
            DontDestroyOnLoad(go);
            go.AddComponent<VolantinoTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s) { Debug.Log("[VolantinoTest] " + s); File.AppendAllText(logPath, s + "\n", Encoding.UTF8); }
        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Arg("-volantinotestout", Path.Combine(Application.dataPath, "..", "volantinotest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "volantinotest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova Volantino. Salvataggio presente: " + avevaSave);
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
            UnityEditor.SessionState.SetBool("volantinotest", false);
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

        void ResetSalvataggio(bool stracciFatti)
        {
            var s = GameManager.I.Save;
            s.inventario.Clear(); s.flashbackCompletati.Clear(); s.enigmiRisolti.Clear(); s.cutsceneViste.Clear(); s.oggettiEnigma.Clear();
            s.segretiTrovati.Clear(); s.segretiFlag.Clear();
            s.ritornoScena = ""; s.ritornoSpawn = ""; s.enigmiSemplificati = false; s.filmatoVisto = true;
            s.inventario.Add("scarpe"); s.flashbackCompletati.Add("flashback_scarpe");
            foreach (var t in new[] { "porta_trovata", "chiave_basso", "porta_aperta" }) s.oggettiEnigma.Add(t);
            if (stracciFatti) { s.inventario.Add("stracci"); s.flashbackCompletati.Add("flashback_stracci"); }
            s.scena = "20_Int_BassoAntonietta"; s.spawn = "ingresso";
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
            yield return ChiudiTutto(6);
        }

        ItemPickup Pickup()
        {
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) if (p.oggettoId == "volantino") return p;
            return null;
        }

        /// <summary>Sposta il giocatore (CharacterController spento durante lo spostamento).</summary>
        IEnumerator Teletrasporta(Vector3 pos)
        {
            var tpc = FindAnyObjectByType<TrenoBambini.Player.ThirdPersonController>();
            if (tpc == null) yield break;
            var cc = tpc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            tpc.transform.position = pos + Vector3.up * 0.05f;
            if (cc != null) cc.enabled = true;
            yield return Attendi(0.6f);
        }

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");

            // ---- 1. il volantino compare sulla credenza solo dopo Stracci
            ResetSalvataggio(false);
            yield return Vai("20_Int_BassoAntonietta", "ingresso");
            Check("senza Stracci il volantino non c'e'", Pickup() == null);
            ResetSalvataggio(true);
            yield return Vai("20_Int_BassoAntonietta", "ingresso");
            var pick = Pickup();
            Check("dopo Stracci il volantino e' nel basso", pick != null);
            if (pick == null) yield break;
            var p = pick.transform.position;
            Check("il volantino sta sulla credenza (x -4,5..-3,6 - y 0,9..1,1 - z 0,9..2,2)", p.x > -4.5f && p.x < -3.6f && p.y > 0.9f && p.y < 1.1f && p.z > 0.9f && p.z < 2.2f, p.ToString("F2"));
            Check("il volantino ha un collider per il raggio di interazione", pick.GetComponent<Collider>() != null);

            // ---- 2. cutscene d'ingresso e scena
            pick.Interact();
            yield return Attendi(1.0f);
            Check("parte la cutscene d'ingresso", UIRoot.I.Cutscene.IsPlaying);
            yield return ChiudiTutto(40);
            yield return ScenaPronta(Scena);
            Check("si entra in " + Scena, SceneManager.GetActiveScene().name == Scena);
            Check("cutscene d'ingresso registrata", GameManager.I.Save.cutsceneViste.Contains("cutscene_volantino#ingresso"));
            yield return ChiudiTutto(6);
            Check("cinque enigmi creati", EnigmaMondo.Tutti.FindAll(e => e != null && e.gameObject.scene.name == Scena).Count == 5);
            Check("uscita presente", FindAnyObjectByType<EscapeExit>() != null);
            Check("Maddalena (segnaposto) nella sede", GameObject.Find("Npc_Maddalena") != null);

            // ---- 3. V1 le voci del vicolo
            var bach = Trova("enigma_bacheca");
            Check("V2 bloccato prima di V1", bach != null && !bach.Disponibile);
            var voci = Trova("enigma_voci");
            Check("V1 presente e disponibile", voci != null && voci.Disponibile);
            voci.Interact();
            yield return Attendi(0.6f);
            yield return ChiudiTutto(4);
            var lista = new List<VoceVicolo>(FindObjectsByType<VoceVicolo>(FindObjectsInactive.Exclude));
            Check("V1: quattro voci nel vicolo", lista.Count == 4, lista.Count.ToString());
            foreach (var v in lista) Check("V1: " + v.name + " ha la luce della finestra in alto o sull'acqua", v.luce != null && v.luce.transform.position.y > v.transform.position.y + 0.9f);
            // la prima si sente avvicinandosi (ascolto nello spazio), le altre con E
            var prima = lista.Find(v => v.item != null && v.item.item.id == "voce_russia");
            if (prima != null)
            {
                yield return Teletrasporta(prima.transform.position + new Vector3(-0.6f, 0f, 0f)); // (verso il centro della via: il lampione sta a x 2,4)
                yield return ChiudiTutto(3);
                Check("V1: avvicinandosi la voce si sente da sola", MondoStato.Ha("voce_russia"));
            }
            foreach (var v in new List<VoceVicolo>(FindObjectsByType<VoceVicolo>(FindObjectsInactive.Exclude))) { if (v != null && v.item != null) v.item.Interact(); yield return null; }
            yield return Attendi(0.8f);
            yield return ChiudiTutto(4);
            Check("V1: tutte le voci nel quaderno", MondoStato.Ha("voce_russia") && MondoStato.Ha("voce_tornano") && MondoStato.Ha("voce_male") && MondoStato.Ha("voce_forni"));
            yield return Teletrasporta(new Vector3(0f, 0f, -27f));
            yield return ApriEnigma("enigma_voci");
            if (V != null)
            {
                Check("V1: meccanismo documenti", EsaminaView.Meccanismo(V.Def) == "documenti");
                Check("V1: riga sbagliata -> errore gentile", !V.RispondiDoc("quaderno", 0) && !V.Risolto);
                V.Indizio(); V.Indizio(); V.Indizio();
                Check("V1: tre indizi", V.LivelloIndizio == 3);
                foreach (var r in new[] { 1, 0, 2, 3 }) V.RispondiDoc("quaderno", r);
                Check("V1: le quattro fonti -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V1: produce voci_raccolte", MondoStato.Ha("voci_raccolte") && MondoStato.Risolto("flashback_volantino/enigma_voci"));

            // ---- 4. V2 la bacheca delle prove
            yield return ApriEnigma("enigma_bacheca");
            if (V != null)
            {
                Check("V2: meccanismo bacheca", EsaminaView.Meccanismo(V.Def) == "bacheca");
                V.Collega(0, "doc_corredo");
                V.Collega(1, "doc_registro");
                Check("V2: un solo collegamento giusto -> nessuno spago fissato", V.FiliFissati == 0 && V.FiliTesi == 2, V.FiliFissati + "/" + V.FiliTesi);
                V.Collega(0, "doc_cartolina");
                Check("V2: due collegamenti giusti -> due spaghi fissati", V.FiliFissati == 2, V.FiliFissati.ToString());
                Check("V2: uno spago fissato non si cambia", !V.Collega(0, "doc_visite"));
                V.Collega(2, "doc_lettera"); V.Collega(3, "doc_visite");
                Check("V2: due collegamenti scambiati -> restano liberi", V.FiliFissati == 2 && !V.Risolto, V.FiliFissati.ToString());
                V.Collega(2, "doc_visite"); V.Collega(3, "doc_lettera");
                Check("V2: tutti gli spaghi rossi", V.FiliFissati == 4, V.FiliFissati.ToString());
                for (int i = 0; i < 4; i++) V.ImpostaVerdetto(i, 1);
                Check("V2: verdetti 'vera' -> errore gentile, non risolto", !V.Risolto);
                for (int i = 0; i < 4; i++) V.ImpostaVerdetto(i, 2);
                Check("V2: verdetti 'falsa' -> risolto", V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V2: produce parole_giuste", MondoStato.Ha("parole_giuste"));

            // ---- salvataggio e ricarica a meta'
            GameManager.I.SaveGame();
            yield return Vai(Scena, "inizio");
            yield return ChiudiTutto(4);
            Check("ricarica: V1 e V2 ancora risolti", MondoStato.Risolto("flashback_volantino/enigma_voci") && MondoStato.Risolto("flashback_volantino/enigma_bacheca"));
            Check("ricarica: V3 disponibile, nessuna voce ricreata", Trova("enigma_ciclostile") != null && Trova("enigma_ciclostile").Disponibile && FindObjectsByType<VoceVicolo>(FindObjectsInactive.Exclude).Length == 0);

            // ---- 5. V3 il ciclostile
            yield return ApriEnigma("enigma_ciclostile");
            if (V != null)
            {
                Check("V3: meccanismo ciclostile", EsaminaView.Meccanismo(V.Def) == "ciclostile");
                var frasi = V.Def.frasi;
                int g0 = frasi.FindIndex(f => f.giusta), sbagliata = frasi.FindIndex(f => !f.giusta);
                V.ScegliFrase(g0);
                Check("V3: una frase sola -> errore gentile", !V.ComponiMatrice() && V.FaseStampa == 0);
                V.ScegliFrase(sbagliata);
                V.ScegliFrase(frasi.FindLastIndex(f => f.giusta));
                Check("V3: una frase che non si puo' controllare -> spiegazione", !V.ComponiMatrice() && V.UltimoMessaggio.Length > 0 && V.FaseStampa == 0);
                V.ScegliFrase(sbagliata); // tolta
                for (int i = 0; i < frasi.Count; i++) if (frasi[i].giusta && i != g0 && i != frasi.FindLastIndex(f => f.giusta)) V.ScegliFrase(i);
                Check("V3: tre frasi chiare e verificabili -> matrice scritta", V.ComponiMatrice() && V.FaseStampa == 1);
                V.Clicca("manovella");
                V.Clicca("inchiostro");
                Check("V3: manovella e inchiostro prima della matrice -> nessun passo avanti", V.FaseStampa == 1 && V.GiriFatti == 0);
                V.Clicca("matrice");
                V.Clicca("inchiostro");
                Check("V3: matrice sul tamburo e inchiostro", V.FaseStampa == 3);
                V.GiraManovellaAl(10f);
                Check("V3: troppo veloce -> il foglio sbava", !V.GiraManovellaAl(10.2f) && V.GiriFatti == 0 && !V.Risolto);
                foreach (var t in new[] { 11f, 12f, 13f, 14f, 15f }) V.GiraManovellaAl(t);
                Check("V3: cinque giri regolari -> risolto", V.Risolto, "giri " + V.GiriFatti);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V3: produce il pacco e le cose del tavolo", MondoStato.Ha("pacco_volantini") && MondoStato.Ha("ruolo_fascia") && MondoStato.Ha("ruolo_berretto"));

            // ---- 6. V4 chi fa che cosa
            yield return ApriEnigma("enigma_ruoli");
            if (V != null)
            {
                V.ScegliMano("ruolo_stetoscopio"); V.Clicca("chi_vestiti");
                Check("V4: persona sbagliata -> nessun progresso", V.Progresso == 0 && !V.Risolto);
                var giusti = new Dictionary<string, string> { { "chi_vestiti", "ruolo_fascia" }, { "chi_visita", "ruolo_stetoscopio" }, { "chi_iscrizione", "ruolo_registro" }, { "chi_famiglie", "ruolo_lettera" }, { "chi_viaggio", "ruolo_berretto" } };
                foreach (var kv in giusti) { V.ScegliMano(kv.Value); V.Clicca(kv.Key); yield return null; }
                Check("V4: ogni cosa alla sua persona -> risolto", V.Risolto, "progresso " + V.Progresso);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("V4: produce retro_stampato", MondoStato.Ha("retro_stampato"));

            // ---- 7. V5 il timbro della data
            yield return ApriEnigma("enigma_timbro");
            if (V != null)
            {
                Check("V5: meccanismo datario", EsaminaView.Meccanismo(V.Def) == "datario");
                string d0 = V.DataCorrente;
                V.Clicca("nastro_g");
                Check("V5: il nastro del giorno gira", V.DataCorrente != d0, d0 + " -> " + V.DataCorrente);
                V.Clicca("ritaglio_treno");
                V.ImpostaData(2, 6, 1946);
                Check("V5: la data del voto per prima -> non e' la prossima", !V.Timbra() && V.Timbrati == 0);
                V.ImpostaData(1, 1, 1946);
                Check("V5: una data che non c'e' -> errore gentile", !V.Timbra() && V.Timbrati == 0);
                V.ImpostaData(16, 12, 1945); V.Timbra();
                V.ImpostaData(29, 12, 1945); V.Timbra();
                Check("V5: due date in ordine", V.Timbrati == 2, V.Timbrati.ToString());
                V.ImpostaData(16, 12, 1945);
                Check("V5: una data gia' timbrata -> errore gentile", !V.Timbra() && V.Timbrati == 2);
                V.ImpostaData(19, 1, 1946); V.Timbra();
                V.ImpostaData(2, 6, 1946); V.Timbra();
                Check("V5: quattro date, l'ultima il 2 giugno 1946 -> risolto", V.Risolto && V.Timbrati == 4);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }

            // ---- 8. uscita dal ricordo
            var esci = FindAnyObjectByType<EscapeExit>();
            esci.Interact();
            yield return Attendi(1.0f);
            yield return ChiudiTutto(60);
            yield return ScenaPronta("20_Int_BassoAntonietta");
            var sv = GameManager.I.Save;
            Check("uscita: si torna al basso", SceneManager.GetActiveScene().name == "20_Int_BassoAntonietta");
            Check("uscita: il volantino e' in inventario", sv.inventario.Contains("volantino"));
            Check("uscita: flashback_volantino completato", sv.flashbackCompletati.Contains("flashback_volantino"));
            Check("uscita: cutscene di chiusura vista", sv.cutsceneViste.Contains("cutscene_volantino#chiusura"));
            Check("uscita: il volantino non e' piu' sulla credenza", Pickup() == null);
            var staz = GameData.Luogo("stazione");
            Check("stazione: sbloccata dal volantino", staz != null && staz.sbloccatoDa == "volantino" && sv.inventario.Contains(staz.sbloccatoDa) && !string.IsNullOrEmpty(staz.sbloccatoMsg));
            var sede = GameData.Luogo("sede_partito");
            Check("sede_partito: indica la credenza del basso", sede != null && sede.sbloccatoDa == "stracci" && !string.IsNullOrEmpty(sede.sbloccatoMsg));

            // ---- 9. versione "Enigmi semplificati"
            ResetSalvataggio(true);
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.SaveGame();
            yield return Vai(Scena, "inizio");
            yield return ChiudiTutto(4);
            foreach (var id in new[] { "enigma_voci", "enigma_bacheca", "enigma_ciclostile", "enigma_ruoli", "enigma_timbro" })
            {
                var e = Trova(id);
                Check(id + " (semplificato): ha domande con opzioni", e != null && e.def.domande.Count >= 3 && e.def.domande[0].opzioni.Count >= 3);
                if (e == null) continue;
                foreach (var tk in e.def.richiede) MondoStato.Dai(tk);
                if (id == "enigma_voci") foreach (var o in e.def.oggetti) MondoStato.Dai(o.id);
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
            var m = GameData.MappaScena(Scena);
            Check("mappa: scena 50 con strade e punti", m != null && m.strade.Count >= 10 && m.punti.Count >= 2);
            int obiettivi = 0; foreach (var o in GameData.Mappe.obiettivi) if (o.capitolo == "flashback_volantino") obiettivi++;
            Check("obiettivi: cinque passi del capitolo", obiettivi == 5, obiettivi.ToString());
            int segreti = 0; foreach (var s in GameData.Segreti.segreti) if (s.scena == Scena) segreti++;
            Check("segreti: un enigma segreto e cinque collezionabili nella scena", segreti == 6, segreti.ToString());
            Check("segreti: stanza del ricordo con le cinque condizioni", GameData.Segreti.stanze.Exists(st => st.scena == Scena && st.completo.Count == 5));
            ResetSalvataggio(true);
        }
    }
}
#endif
