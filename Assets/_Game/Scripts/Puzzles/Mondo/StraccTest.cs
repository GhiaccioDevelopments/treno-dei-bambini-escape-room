#if UNITY_EDITOR || DEBUG
// R012: prova automatica del capitolo "Stracci", solo negli editor e nelle build di sviluppo (mai nella build normale).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    /// R012: gioca il capitolo "Stracci" dall'inizio alla fine con le API pubbliche (come EnigmiSelfTest ma senza mouse virtuale): il fagotto compare solo dopo "Le scarpe",
    /// cutscene d'ingresso, scena 40_FB_Stracci, i cinque enigmi (anche gli errori gentili), salvataggio e ricarica a meta', uscita con l'oggetto in inventario,
    /// sede del Comitato sbloccata e fagotto sparito, versione "Enigmi semplificati", oggetti raccoglibili e postazioni raggiungibili dalla camera di gioco.
    /// Attiva SOLO con -straccitest [-straccitestout cartella] (nell'editor: SessionState "straccitest"). Scrive straccitest.log, OK.txt / FALLITA.txt; poi chiude.
    /// </summary>
    public class StraccTest : MonoBehaviour
    {
        static readonly string[] Args = Environment.GetCommandLineArgs();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            bool vai = false;
            foreach (var a in Args) if (a == "-straccitest") vai = true;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("straccitest", false)) vai = true;
#endif
            if (!vai) return;
            var go = new GameObject("StraccTest");
            DontDestroyOnLoad(go);
            go.AddComponent<StraccTest>();
        }

        static string Arg(string nome, string def)
        {
            for (int i = 0; i < Args.Length - 1; i++) if (Args[i] == nome) return Args[i + 1];
            return def;
        }

        string dir, logPath;
        int ok, ko;
        EsaminaView V { get { return EsaminaView.Current; } }

        void Log(string s) { Debug.Log("[StraccTest] " + s); File.AppendAllText(logPath, s + "\n", Encoding.UTF8); }
        void Check(string nome, bool cond, string dettaglio = "")
        {
            if (cond) ok++; else ko++;
            Log((cond ? "PASS " : "FAIL ") + nome + (dettaglio != "" ? "  [" + dettaglio + "]" : ""));
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            dir = Arg("-straccitestout", Path.Combine(Application.dataPath, "..", "straccitest"));
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "straccitest.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            string saveBackup = PlayerPrefs.GetString("treno_save", null);
            bool avevaSave = PlayerPrefs.HasKey("treno_save");
            Log("Inizio prova Stracci. Salvataggio presente: " + avevaSave);
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
            UnityEditor.SessionState.SetBool("straccitest", false);
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

        void ResetSalvataggio(bool scarpeFatte)
        {
            var s = GameManager.I.Save;
            s.inventario.Clear(); s.flashbackCompletati.Clear(); s.enigmiRisolti.Clear(); s.cutsceneViste.Clear(); s.oggettiEnigma.Clear();
            s.segretiTrovati.Clear(); s.segretiFlag.Clear();
            s.ritornoScena = ""; s.ritornoSpawn = ""; s.enigmiSemplificati = false; s.filmatoVisto = true;
            if (scarpeFatte)
            {
                s.inventario.Add("scarpe"); s.flashbackCompletati.Add("flashback_scarpe");
                foreach (var t in new[] { "porta_trovata", "chiave_basso", "porta_aperta" }) s.oggettiEnigma.Add(t);
            }
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

        // ------------------------------------------------------------------ flusso
        IEnumerator Flusso()
        {
            yield return Attendi(3f);
            Check("GameManager pronto", GameManager.I != null && SceneLoader.I != null);
            PlayerPrefs.DeleteKey("treno_save");

            // ---- 1. il fagotto compare solo dopo Le scarpe
            ResetSalvataggio(false);
            yield return Vai("20_Int_BassoAntonietta", "ingresso");
            Check("senza Le scarpe il fagotto non c'e'", FaiPickup() == null);
            ResetSalvataggio(true);
            yield return Vai("20_Int_BassoAntonietta", "ingresso");
            var pick = FaiPickup();
            Check("dopo Le scarpe il fagotto di stracci e' nel cortile del basso", pick != null);
            if (pick == null) yield break;
            var p = pick.transform.position;
            Check("il fagotto sta nel cortile (x 1,2..6,3 - z 8,1..14,1)", p.x > 1.2f && p.x < 6.3f && p.z > 8.1f && p.z < 14.1f, p.ToString("F2"));
            var col = pick.GetComponent<Collider>();
            Check("il fagotto ha un collider per il raggio di interazione", col != null);
            yield return ControllaVisibile(pick.gameObject, "fagotto");

            // ---- 2. cutscene d'ingresso e scena
            pick.Interact();
            yield return Attendi(1.0f);
            Check("parte la cutscene d'ingresso", UIRoot.I.Cutscene.IsPlaying);
            yield return ChiudiTutto(40);
            yield return ScenaPronta("40_FB_Stracci");
            Check("si entra in 40_FB_Stracci", SceneManager.GetActiveScene().name == "40_FB_Stracci");
            Check("cutscene d'ingresso registrata", GameManager.I.Save.cutsceneViste.Contains("cutscene_stracci#ingresso"));
            yield return ChiudiTutto(6);
            Check("cinque enigmi creati", EnigmaMondo.Tutti.FindAll(e => e != null && e.gameObject.scene.name == "40_FB_Stracci").Count == 5);
            var esci = FindAnyObjectByType<EscapeExit>();
            Check("uscita presente", esci != null);

            // ---- 3. S1 il giro degli stracci
            var s2 = Trova("enigma_ceste");
            Check("S2 bloccato prima di S1", s2 != null && !s2.Disponibile);
            var giro = Trova("enigma_giro");
            Check("S1 presente e disponibile", giro != null && giro.Disponibile);
            giro.Interact();
            yield return Attendi(0.6f);
            yield return ChiudiTutto(4);
            var nascosti = new List<HiddenItem>(FindObjectsByType<HiddenItem>(FindObjectsInactive.Exclude));
            Check("S1: dieci pezzi sparsi nel vicolo", nascosti.Count == 10, nascosti.Count.ToString());
            foreach (var h in nascosti) yield return ControllaVisibile(h.gameObject, h.item.id);
            foreach (var h in nascosti) { if (h != null) h.Interact(); yield return null; }
            yield return Attendi(0.8f);
            yield return ChiudiTutto(4);
            Check("S1: tutti i pezzi nel sacco", MondoStato.Ha("straccio_1") && MondoStato.Ha("straccio_10"));
            yield return ApriEnigma("enigma_giro");
            if (V != null)
            {
                Check("S1: meccanismo tessuti", EsaminaView.Meccanismo(V.Def) == "tessuti");
                int n = V.Def.passanti.Count;
                for (int i = 0; i < n; i++) { V.ProvaTessuto(i, "controluce"); V.ProvaTessuto(i, "filo"); V.ProvaTessuto(i, "spessore"); }
                yield return Attendi(1.0f);
                // tutto sbagliato: errore gentile, nessuna soluzione
                for (int i = 0; i < n; i++) V.AssegnaPassante(i, 3);
                Check("S1: etichette sbagliate -> errore gentile, non risolto", !V.Conferma() && !V.Risolto);
                V.Indizio(); V.Indizio(); V.Indizio();
                Check("S1: tre indizi", V.LivelloIndizio == 3);
                for (int i = 0; i < n; i++) V.AssegnaPassante(i, V.Def.mestieri.IndexOf(V.Def.passanti[i].mestiere));
                Check("S1: etichette giuste -> risolto", V.Conferma() && V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("S1: produce il sacco di stracci", MondoStato.Ha("sacco_stracci") && MondoStato.Risolto("flashback_stracci/enigma_giro"));

            // ---- 4. S2 le ceste
            yield return ApriEnigma("enigma_ceste");
            if (V != null)
            {
                V.ScegliMano("straccio_1"); V.Clicca("cesta_cotone");
                Check("S2: cesta sbagliata -> nessun progresso", V.Progresso == 0 && !V.Risolto);
                var mappa = new Dictionary<string, string[]>
                {
                    { "cesta_lana", new[] { "straccio_1", "straccio_4", "straccio_7" } }, { "cesta_cotone", new[] { "straccio_2", "straccio_5", "straccio_8" } },
                    { "cesta_lino", new[] { "straccio_3", "straccio_6" } }, { "cesta_scarto", new[] { "straccio_9", "straccio_10" } }
                };
                foreach (var kv in mappa) foreach (var tok in kv.Value) { V.ScegliMano(tok); V.Clicca(kv.Key); yield return null; }
                Check("S2: tutti i pezzi nelle ceste -> risolto", V.Risolto, "progresso " + V.Progresso);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("S2: produce ceste_pronte", MondoStato.Ha("ceste_pronte"));

            // ---- salvataggio e ricarica a meta'
            GameManager.I.SaveGame();
            yield return Vai("40_FB_Stracci", "inizio");
            yield return ChiudiTutto(4);
            Check("ricarica: S1 e S2 ancora risolti", MondoStato.Risolto("flashback_stracci/enigma_giro") && MondoStato.Risolto("flashback_stracci/enigma_ceste"));
            Check("ricarica: S3 disponibile, S1 non ricreato come nascosto", Trova("enigma_bilancia") != null && Trova("enigma_bilancia").Disponibile && FindObjectsByType<HiddenItem>(FindObjectsInactive.Exclude).Length == 0);

            // ---- 5. S3 la bilancia
            yield return ApriEnigma("enigma_bilancia");
            if (V != null)
            {
                Check("S3: nel mondo il chiodo e' presente", V.ChiodoPresente);
                V.Clicca("campione"); V.Clicca("p1000");
                Check("S3: un chilo contro un chilo pende a sinistra (150 g)", Mathf.Abs(V.SbilanciamentoGrammi - 150f) < 0.5f, V.SbilanciamentoGrammi.ToString());
                V.Clicca("campione"); V.Clicca("p1000");
                V.Clicca("carico_lana"); V.Clicca("p1000"); V.Clicca("p200"); V.Clicca("p100");
                Check("S3: con il chiodo la pesata non vale", V.Progresso == 0 && V.ChiodoPresente);
                V.Clicca("chiodo");
                yield return Attendi(0.6f);
                Check("S3: chiodo tolto, nessun squilibrio", !V.ChiodoPresente);
                // ora lana = 1300 g con 1000+200+100 (gia' sul piatto): equilibrio con il chiodo tolto
                V.Clicca("p100"); V.Clicca("p100");
                Check("S3: lana 1300 g in equilibrio", Mathf.Abs(V.SbilanciamentoGrammi) < 0.5f && V.Progresso >= 2, "diff " + V.SbilanciamentoGrammi + " prog " + V.Progresso);
                V.Clicca("p1000"); V.Clicca("p200"); V.Clicca("p100");
                V.Clicca("carico_cotone"); V.Clicca("p500"); V.Clicca("p200"); V.Clicca("p100");
                Check("S3: cotone 800 g in equilibrio", Mathf.Abs(V.SbilanciamentoGrammi) < 0.5f && V.Progresso >= 3, "diff " + V.SbilanciamentoGrammi + " prog " + V.Progresso);
                V.Clicca("p200"); V.Clicca("p100"); // restano 500 g
                V.Clicca("carico_lino"); V.Clicca("p100");
                Check("S3: tre pesate e chiodo -> risolto", V.Risolto, "prog " + V.Progresso + " diff " + V.SbilanciamentoGrammi);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("S3: produce pesi_scritti", MondoStato.Ha("pesi_scritti"));

            // ---- 6. S4 il conto
            yield return ApriEnigma("enigma_conto");
            if (V != null)
            {
                V.Clicca("registro");
                V.Clicca("m10_0");
                Check("S4: una moneta da 10 -> sul banco 10 lire", V.TotaleBanco == 10, V.TotaleBanco.ToString());
                Check("S4: somma troppo bassa -> errore gentile", !V.ConfermaStracci() && !V.Risolto);
                foreach (var id in new[] { "m10_1", "m10_2", "m10_3", "m10_4", "m10_5", "m5_0", "m5_1", "m5_2", "m5_3", "m2_0", "m2_1", "m1_0", "m1_1" }) V.Clicca(id);
                Check("S4: somma troppo alta (85) -> errore gentile", V.TotaleBanco == 85 && !V.ConfermaStracci() && !V.Risolto, V.TotaleBanco.ToString());
                V.Clicca("m2_1"); V.Clicca("m1_1");
                Check("S4: 83 lire sul banco", V.TotaleBanco == 83, V.TotaleBanco.ToString());
                Check("S4: 83 lire -> pagato", V.ConfermaStracci() && V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }
            Check("S4: produce il soldo", MondoStato.Ha("soldo"));

            // ---- 7. S5 il muro dei mestieri
            yield return ApriEnigma("enigma_mestieri");
            if (V != null)
            {
                int n = V.Def.righe.Count;
                for (int i = 0; i < n; i++) V.ImpostaRiga(i, (i + 1) % n, (i + 2) % n);
                Check("S5: collegamenti sbagliati -> errore gentile", !V.ConfermaStracci() && !V.Risolto);
                V.Indizio(); V.Indizio(); V.Indizio();
                Check("S5: tre indizi", V.LivelloIndizio == 3);
                for (int i = 0; i < n; i++) V.ImpostaRiga(i, V.Def.mestieri.IndexOf(V.Def.righe[i].mestiere), V.Def.diritti.IndexOf(V.Def.righe[i].diritto));
                Check("S5: collegamenti giusti -> risolto", V.ConfermaStracci() && V.Risolto);
                yield return Attendi(0.6f);
                yield return Chiudi();
            }

            // ---- 8. uscita dal ricordo
            esci = FindAnyObjectByType<EscapeExit>();
            esci.Interact();
            yield return Attendi(1.0f);
            yield return ChiudiTutto(60);
            yield return ScenaPronta("20_Int_BassoAntonietta");
            var sv = GameManager.I.Save;
            Check("uscita: si torna al basso", SceneManager.GetActiveScene().name == "20_Int_BassoAntonietta");
            Check("uscita: gli stracci sono in inventario", sv.inventario.Contains("stracci"));
            Check("uscita: flashback_stracci completato", sv.flashbackCompletati.Contains("flashback_stracci"));
            Check("uscita: cutscene di chiusura vista", sv.cutsceneViste.Contains("cutscene_stracci#chiusura"));
            Check("uscita: il fagotto non c'e' piu'", FaiPickup() == null);
            var sede = GameData.Luogo("sede_partito");
            Check("sede_partito: sbloccata dagli stracci", sede != null && sede.sbloccatoDa == "stracci" && sv.inventario.Contains(sede.sbloccatoDa));
            var capa = GameData.Luogo("casa_capa_e_fierro");
            Check("casa_capa_e_fierro: descrizione da robivecchi", capa != null && capa.descrizione.Contains("robivecchi"));

            // ---- 9. versione "Enigmi semplificati"
            ResetSalvataggio(true);
            GameManager.I.Save.enigmiSemplificati = true;
            GameManager.I.SaveGame();
            yield return Vai("40_FB_Stracci", "inizio");
            yield return ChiudiTutto(4);
            foreach (var id in new[] { "enigma_giro", "enigma_ceste", "enigma_bilancia", "enigma_conto", "enigma_mestieri" })
            {
                var e = Trova(id);
                Check(id + " (semplificato): ha domande con opzioni", e != null && e.def.domande.Count >= 3 && e.def.domande[0].opzioni.Count >= 3);
                if (e == null) continue;
                foreach (var tk in e.def.richiede) MondoStato.Dai(tk);
                if (id == "enigma_giro") foreach (var o in e.def.oggetti) MondoStato.Dai(o.id);
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

            // ---- 10. mappa e obiettivi
            var m = GameData.MappaScena("40_FB_Stracci");
            Check("mappa: scena 40 con strade e punti", m != null && m.strade.Count >= 10 && m.punti.Count >= 1);
            int obiettivi = 0; foreach (var o in GameData.Mappe.obiettivi) if (o.capitolo == "flashback_stracci") obiettivi++;
            Check("obiettivi: cinque passi del capitolo", obiettivi == 5, obiettivi.ToString());
            ResetSalvataggio(true);
        }

        ItemPickup FaiPickup()
        {
            foreach (var p in FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) if (p.oggettoId == "stracci") return p;
            return null;
        }

        /// <summary>Un oggetto e' visibile dalla camera di gioco: ha renderer attivi e, partendo dal punto di spawn, la linea di vista verso di lui e' libera da muri (collider non trigger).</summary>
        IEnumerator ControllaVisibile(GameObject g, string nome)
        {
            int rend = 0; foreach (var r in g.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy) rend++;
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            Vector3 alto = b.center + Vector3.up * (b.extents.y + 1.6f);
            bool libero = true;
            RaycastHit hit;
            if (Physics.Raycast(alto, (b.center - alto).normalized, out hit, (b.center - alto).magnitude, ~0, QueryTriggerInteraction.Ignore))
                libero = hit.collider.transform.IsChildOf(g.transform);
            Check(nome + ": ha renderer attivi e nessun muro o soffitto lo copre dall'alto", rend > 0 && libero, "renderer " + rend + (libero ? "" : " coperto da " + hit.collider.name));
            yield return null;
        }
    }
}
#endif
