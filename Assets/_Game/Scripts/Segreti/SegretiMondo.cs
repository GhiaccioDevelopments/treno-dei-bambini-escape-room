using System;
using System.Collections;
using System.Collections.Generic;
using TrenoBambini.Audio;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Segreti
{
    /// <summary>
    /// R011: all'ingresso di una scena crea i pezzi di mondo dei segreti (collezionabili, biglietti, scritte, decori), gli enigmi segreti (stesso sistema di R009/R020,
    /// EnigmaMondo), le campane di gesso e gli easter egg elencati in segreti.json per quella scena. Niente e' scritto nelle scene: nuove stanze = nuove righe nel JSON.
    /// Nessun indicatore, nessuna freccia: i segreti si trovano guardando (luce, suono, scritta).
    /// </summary>
    public static class SegretiSpawner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aggancia()
        {
            SceneManager.sceneLoaded -= OnScena;
            SceneManager.sceneLoaded += OnScena;
        }

        public static readonly List<SegretoPezzo> Pezzi = new List<SegretoPezzo>();

        static void OnScena(Scene sc, LoadSceneMode mode)
        {
            if (GameManager.IsMenuScene(sc.name)) return;
            if (mode == LoadSceneMode.Additive && GameManager.MenuBackdrop) return;
            var f = GameData.Segreti;
            GameObject radice = null;
            Func<Transform> root = () =>
            {
                if (radice == null)
                {
                    radice = new GameObject("SegretiMondo");
                    SceneManager.MoveGameObjectToScene(radice, sc);
                }
                return radice.transform;
            };
            Pezzi.RemoveAll(p => p == null);
            foreach (var d in f.pezzi) if (d.scena == sc.name) Pezzi.Add(SegretoPezzo.Crea(d, root()));
            foreach (var e in f.enigmi)
            {
                if (e.scena != sc.name) continue;
                var ee = e;
                var em = EnigmaMondo.Crea(ee, "segreti/" + ee.id, root());
                em.alRisolto = x => AlRisolto(ee);
                MondoSpawner.Sostituisci(sc, ee.sostituisce);
            }
            foreach (var c in f.campane) if (c.scena == sc.name && !MondoStato.Ha(c.produce)) CampanaGesso.Crea(c, root());
            foreach (var u in f.uova) if (u.scena == sc.name) UovoSegreto.Crea(u, root());
        }

        /// <summary>Un enigma segreto risolto: segna il segreto, mostra la pagina "dietro le quinte".</summary>
        static void AlRisolto(EnigmaDef e)
        {
            if (string.IsNullOrEmpty(e.segreto)) return;
            var sg = SegretiStato.Def(e.segreto);
            if (!SegretiStato.Trova(e.segreto) || sg == null) return;
            MostraRicompensa(sg);
        }

        /// <summary>Pagina "dietro le quinte" di un enigma segreto (ricompensa solo narrativa).</summary>
        public static void MostraRicompensa(SegretoDef sg)
        {
            var t = GameData.Segreti.testi;
            if (UIRoot.I != null && sg != null && !string.IsNullOrEmpty(sg.ricompensa))
                UIRoot.I.ShowMessage(t.dietroQuinteTitolo + ": " + sg.nome, sg.ricompensa + "\n\n" + t.quadernoRiga, null);
        }

        /// <summary>Posizione sul suolo: dal punto (x,y+0.6,z) scende fino al primo solido (esclusi i trigger e i pezzi dei segreti); se non trova nulla resta y.</summary>
        public static Vector3 SulSuolo(Vector3 p, float sopra = 0.6f, float maxGiu = 2.5f)
        {
            var org = new Vector3(p.x, p.y + sopra, p.z);
            var hits = Physics.RaycastAll(org, Vector3.down, maxGiu + sopra, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool trovato = false; Vector3 pt = p;
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<SegretoPezzo>() != null || h.collider.GetComponentInParent<EnigmaMondo>() != null) continue;
                if (h.distance < best) { best = h.distance; pt = h.point; trovato = true; }
            }
            return trovato ? new Vector3(p.x, pt.y, p.z) : p;
        }
    }

    /// <summary>Un pezzo di mondo dei segreti: si raccoglie, si legge, si apre o e' solo un decoro. Compare e sparisce secondo le condizioni del JSON.</summary>
    public class SegretoPezzo : Interactable
    {
        public SegretoPezzoDef def;
        public GameObject visuale;
        public bool presa;
        Vector3 pos0, slide;
        float tAgg;
        Collider zona;
        readonly List<Transform> altri = new List<Transform>();
        readonly List<Vector3> altriPos0 = new List<Vector3>();

        public bool Visibile
        {
            get
            {
                if (presa) return false;
                if (!SegretiStato.Tutte(def.appareQuando)) return false;
                if (!string.IsNullOrEmpty(def.nascondiQuando) && SegretiStato.Condizione(def.nascondiQuando)) return false;
                return true;
            }
        }

        public override bool CanInteract { get { return def != null && def.ruolo != "decoro" && Visibile; } }

        public static SegretoPezzo Crea(SegretoPezzoDef d, Transform parent)
        {
            var go = new GameObject("Segreto_" + d.id);
            go.transform.SetParent(parent, false);
            Vector3 p = new Vector3(d.x, d.y, d.z);
            if ((d.appoggio ?? "suolo") == "suolo") p = SegretiSpawner.SulSuolo(p, 0.5f) + new Vector3(0f, d.dy > 0f ? 0f : 0f, 0f);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(d.rx, d.yaw, d.rz);
            go.transform.localScale = Vector3.one * (d.scala > 0f ? d.scala : 1f);
            var v = new GameObject("Visuale");
            v.transform.SetParent(go.transform, false);
            var pseudo = new EnigmaDef { id = d.id, parti = d.parti };
            MondoFab.Oggetto(pseudo, v.transform, false);
            foreach (var pt in d.parti)
                if (pt.nascosta) { var t = v.transform.Find(pt.id); if (t != null) t.gameObject.SetActive(false); }
            var e = go.AddComponent<SegretoPezzo>();
            e.def = d; e.visuale = v; e.pos0 = p;
            e.slide = new Vector3(d.sx, d.sy, d.sz);
            if (d.ruolo != "decoro")
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true; sc.radius = Mathf.Max(0.1f, d.raggio) / go.transform.localScale.x; sc.center = new Vector3(0f, 0.05f, 0f);
                e.zona = sc;
            }
            e.prompt = e.PromptTesto();
            e.presa = e.GiaPreso();
            if (!string.IsNullOrEmpty(d.scorriOggetto)) e.TrovaAltri(d.scorriOggetto, parent.gameObject.scene);
            e.Aggiorna(true);
            return e;
        }

        /// <summary>Il pezzo e' gia' stato preso in una partita precedente (salvataggio): non riappare.</summary>
        bool GiaPreso()
        {
            if (def.ruolo == "lettura" || def.ruolo == "decoro") return false;
            if (!string.IsNullOrEmpty(def.segreto) && SegretiStato.Trovato(def.segreto)) return true;
            return MondoStato.Ha(def.id);
        }

        /// <summary>Oggetti di scena (per nome, entro 3 m dal pezzo) che si spostano insieme al pezzo.</summary>
        void TrovaAltri(string nomi, Scene sc)
        {
            var cerca = new HashSet<string>();
            foreach (var n in nomi.Split(',')) cerca.Add(n.Trim());
            foreach (var root in sc.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!cerca.Contains(t.name) || t.IsChildOf(transform)) continue;
                    if (Vector3.Distance(new Vector3(t.position.x, 0f, t.position.z), new Vector3(pos0.x, 0f, pos0.z)) > 3f) continue;
                    // niente figli di un oggetto gia' elencato (si muoverebbero due volte)
                    bool figlio = false;
                    foreach (var o in altri) if (t.IsChildOf(o)) figlio = true;
                    if (figlio) continue;
                    // il mobile di scena e' static (batching, luce cotta): non si muove. Si lascia la copia identica, non batchata, e si spegne l'originale.
                    var copia = UnityEngine.Object.Instantiate(t.gameObject, t.parent);
                    copia.name = t.name + "_Mobile";
                    copia.transform.SetPositionAndRotation(t.position, t.rotation);
                    t.gameObject.SetActive(false);
                    altri.Add(copia.transform); altriPos0.Add(copia.transform.position);
                }
        }

        string PromptTesto()
        {
            if (!string.IsNullOrEmpty(def.prompt)) return def.prompt;
            var t = GameData.Segreti.testi;
            return def.ruolo == "lettura" ? string.Format(t.guarda, def.id) : string.Format(t.raccogli, def.id);
        }

        void Update()
        {
            if (def == null) return;
            tAgg -= Time.unscaledDeltaTime;
            if (tAgg <= 0f) { tAgg = 0.25f; Aggiorna(false); }
            if (!string.IsNullOrEmpty(def.scorriQuando) && slide.sqrMagnitude > 0f)
            {
                bool via = SegretiStato.Condizione(def.scorriQuando);
                Vector3 target = via ? pos0 + slide : pos0;
                if ((transform.position - target).sqrMagnitude > 1e-6f) transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 0.6f);
                for (int i = 0; i < altri.Count; i++)
                    if (altri[i] != null)
                    {
                        Vector3 t2 = via ? altriPos0[i] + slide : altriPos0[i];
                        if ((altri[i].position - t2).sqrMagnitude > 1e-6f) altri[i].position = Vector3.MoveTowards(altri[i].position, t2, Time.deltaTime * 0.6f);
                    }
            }
        }

        public void Aggiorna(bool subito)
        {
            bool v = Visibile;
            if (visuale != null && visuale.activeSelf != v) visuale.SetActive(v);
            if (zona != null) zona.enabled = v;
            if (subito && !string.IsNullOrEmpty(def.scorriQuando) && SegretiStato.Condizione(def.scorriQuando))
            {
                transform.position = pos0 + slide;
                for (int i = 0; i < altri.Count; i++) if (altri[i] != null) altri[i].position = altriPos0[i] + slide;
            }
        }

        public override string GetPrompt() { return prompt; }

        public override void Interact()
        {
            if (def == null || presa || !CanInteract) return;
            var t = GameData.Segreti.testi;
            if (!SegretiStato.Tutte(def.richiede))
            {
                string m = !string.IsNullOrEmpty(def.richiedeMsg) ? def.richiedeMsg : t.richiedeGenerico;
                if (UIRoot.I != null && !string.IsNullOrEmpty(m)) UIRoot.I.Toast(m, 4f);
                return;
            }
            var sg = string.IsNullOrEmpty(def.segreto) ? null : SegretiStato.Def(def.segreto);
            string titolo = !string.IsNullOrEmpty(def.titolo) ? def.titolo : (sg != null ? sg.nome : "");
            bool primaVolta = def.produce.Exists(p => !MondoStato.Ha(p)) || !MondoStato.Ha(def.id);
            foreach (var p in def.produce) MondoStato.Dai(p);
            switch (def.ruolo)
            {
                case "lettura":
                    // si puo' rileggere quante volte si vuole
                    if (UIRoot.I != null && !string.IsNullOrEmpty(def.msg)) UIRoot.I.ShowMessage(titolo, def.msg, null);
                    if (primaVolta) { MondoStato.Dai(def.id); GameManager.I.SaveGame(); }
                    AudioManager.FireAt("trovato", transform.position);
                    return;
                case "indizio":
                    MondoStato.Dai(def.id);
                    Prendi();
                    if (UIRoot.I != null && !string.IsNullOrEmpty(def.msg)) UIRoot.I.ShowMessage(titolo, def.msg + "\n\n" + t.quadernoRiga, null);
                    break;
                case "apri":
                    MondoStato.Dai(def.id);
                    Prendi();
                    if (UIRoot.I != null && !string.IsNullOrEmpty(def.msg)) UIRoot.I.Toast(def.msg, 4f);
                    break;
                default: // raccogli
                    MondoStato.Dai(def.id);
                    Prendi();
                    bool nuovo = sg != null && SegretiStato.Trova(def.segreto);
                    if (UIRoot.I != null)
                    {
                        string testo = sg != null ? sg.scheda : def.msg;
                        if (!string.IsNullOrEmpty(def.msg) && sg != null) testo = def.msg + "\n\n" + sg.scheda;
                        bool enigma = sg != null && sg.tipo == "enigma";
                        if (nuovo || sg == null) UIRoot.I.ShowMessage(titolo, (enigma ? def.msg : testo) + (sg != null ? "\n\n" + t.quadernoRiga : ""), enigma && nuovo ? (Action)(() => SegretiSpawner.MostraRicompensa(sg)) : null);
                    }
                    break;
            }
            GameManager.I.SaveGame();
        }

        void Prendi()
        {
            presa = true;
            UISfx.Play(Sfx.Sparkle, 0.8f);
            AudioManager.FireAt("trovato", transform.position);
            if (Camera.main != null)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(transform.position);
                if (sp.z > 0f) UIParticles.BurstAtScreen(ParticleKind.Spark, sp, 8);
            }
            Aggiorna(true);
        }
    }

    /// <summary>
    /// Campana disegnata col gesso. Dopo aver letto la sequenza (gettone `richiede`) si salta sulle caselle nell'ordine giusto: ci si ferma un istante su ognuna.
    /// Prima di leggere la sequenza e' solo un gioco da bambini: nessun errore, nessun messaggio.
    /// </summary>
    public class CampanaGesso : MonoBehaviour
    {
        CampanaDef def;
        Transform pg;
        int prog, corrente = -1, ultimo = -1;
        float da, ultimoMsg = -99f;
        bool fatto;

        public static CampanaGesso Crea(CampanaDef d, Transform parent)
        {
            var go = new GameObject("Campana_" + d.id);
            go.transform.SetParent(parent, false);
            go.transform.position = SegretiSpawner.SulSuolo(new Vector3(d.x, d.y, d.z), 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, d.yaw, 0f);
            foreach (var c in d.caselle)
            {
                string k = "c" + c.n;
                float hx = c.w * 0.5f, hz = c.d * 0.5f, sp = 0.035f;
                Linea(go.transform, k + "a", c.x, c.z - hz, c.w + sp, sp);
                Linea(go.transform, k + "b", c.x, c.z + hz, c.w + sp, sp);
                Linea(go.transform, k + "c", c.x - hx, c.z, sp, c.d + sp);
                Linea(go.transform, k + "d", c.x + hx, c.z, sp, c.d + sp);
                var num = MondoFab.Pezzo(new ParteDef { id = k + "n", forma = "testo", testo = c.n.ToString(), colore = "#e8e4d6", font = "mano", dimensione = Mathf.Min(c.w, c.d) * 0.5f, sx = c.w, sy = c.d, x = c.x, y = 0.008f, z = c.z, rx = 90f }, go.transform, false);
            }
            var e = go.AddComponent<CampanaGesso>();
            e.def = d;
            return e;
        }

        static void Linea(Transform parent, string id, float x, float z, float w, float d)
        {
            MondoFab.Pezzo(new ParteDef { id = id, forma = "cubo", materiale = "gesso", x = x, y = 0.004f, z = z, sx = w, sy = 0.006f, sz = d }, parent, false);
        }

        void Update()
        {
            if (fatto || def == null) return;
            if (pg == null) { var pi = PlayerInteractor.Active; if (pi != null) pg = pi.transform; if (pg == null) return; }
            if (!string.IsNullOrEmpty(def.richiede) && !MondoStato.Ha(def.richiede)) return;   // prima del graffito e' solo un gioco
            Vector3 l = transform.InverseTransformPoint(pg.position);
            if (Mathf.Abs(l.y) > 1.0f) { corrente = -1; return; }
            int n = -1;
            foreach (var c in def.caselle)
                if (Mathf.Abs(l.x - c.x) <= c.w * 0.5f && Mathf.Abs(l.z - c.z) <= c.d * 0.5f) { n = c.n; break; }
            if (n != corrente) { corrente = n; da = Time.time; return; }
            if (n < 0 || Time.time - da < def.sosta || n == ultimo) return;
            ultimo = n;
            Passo(n);
        }

        void Passo(int n)
        {
            var t = GameData.Segreti.testi;
            string seq = def.sequenza ?? "";
            char atteso = prog < seq.Length ? seq[prog] : '?';
            if (n.ToString()[0] == atteso)
            {
                prog++;
                UISfx.Play(Sfx.Chime, 0.35f);
                if (prog >= seq.Length) { Fatto(); return; }
                return;
            }
            // errore gentile: si ricomincia, senza punizioni. Se la casella e' proprio la prima della sequenza vale come primo passo.
            prog = (seq.Length > 0 && n.ToString()[0] == seq[0]) ? 1 : 0;
            UISfx.Play(Sfx.SoftError, 0.25f);
            if (Time.time - ultimoMsg > 8f && UIRoot.I != null && !string.IsNullOrEmpty(t.campanaSbagliata)) { UIRoot.I.Toast(t.campanaSbagliata, 4f); ultimoMsg = Time.time; }
        }

        void Fatto()
        {
            fatto = true;
            var t = GameData.Segreti.testi;
            MondoStato.Dai(def.produce);
            GameManager.I.SaveGame();
            AudioManager.Fire("enigma_risolto");
            if (UIRoot.I != null && !string.IsNullOrEmpty(t.campanaFatta)) UIRoot.I.Toast(t.campanaFatta, 5f);
        }

        /// <summary>Per la prova automatica: tocca una casella come se il giocatore si fosse fermato.</summary>
        public void ProvaPasso(int n) { Passo(n); }
        public CampanaDef Def { get { return def; } }
    }

    /// <summary>Easter egg discreti: violino lontano (solo dopo il finale, aggancio per R016) e scarpe piccole in piu' sul filo (dopo il capitolo "Cappotto").</summary>
    public class UovoSegreto : MonoBehaviour
    {
        public UovoDef def;
        AudioSource src;
        public bool Attivo;

        public static UovoSegreto Crea(UovoDef d, Transform parent)
        {
            var go = new GameObject("Uovo_" + d.id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(d.x, d.y, d.z);
            var e = go.AddComponent<UovoSegreto>();
            e.def = d;
            e.Attivo = SegretiStato.Tutte(d.quando);
            if (!e.Attivo) return e;
            if (d.tipo == "violino") e.Violino();
            else if (d.tipo == "scarpe") e.Scarpe();
            return e;
        }

        void Violino()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 4f; src.maxDistance = Mathf.Max(8f, def.raggio);
            src.loop = true; src.playOnAwake = false;
            var lp = gameObject.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 2600f;
            AudioLibrary.GetMusic(this, MusicSynth.Solo, clip =>
            {
                if (clip == null || src == null) return;
                src.clip = clip;
                src.volume = def.volume * (AudioManager.I != null ? AudioManager.I.ChannelGain(AudioChannel.Musica) : 1f);
                src.Play();
            });
        }

        public bool StaSuonando { get { return src != null && src.isPlaying; } }

        void Scarpe()
        {
            // un paio piccolo, di cartone, annodato per i lacci accanto agli altri
            var cordaP = new ParteDef { id = "corda", forma = "cilindro", materiale = "stoffa", colore = "#2b241c", x = 0f, y = 0.2f, z = 0f, sx = 0.012f, sy = 0.2f, sz = 0.012f };
            MondoFab.Pezzo(cordaP, transform, false);
            for (int i = 0; i < 2; i++)
            {
                var sc = MondoFab.Scarpa(new ParteDef { id = "scarpina_" + i, testo = "cartone", x = 0f, y = -0.04f, z = (i == 0 ? -0.07f : 0.07f), rz = -80f, ry = 90f, colore = "#5a4a34", sx = 0.7f, sy = 0.7f, sz = 0.7f }, transform, false);
            }
        }
    }
}
