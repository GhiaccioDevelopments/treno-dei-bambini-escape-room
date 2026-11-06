using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Core;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 1: verifica delle compenetrazioni nelle scene di gioco.
    /// Per ogni scena segnala (1) coppie di renderer i cui ingombri orientati (OBB della mesh) si intersecano oltre la
    /// tolleranza, (2) oggetti sospesi (nessun appoggio entro 5 cm sotto la base), (3) oggetti sotto il pavimento,
    /// (4) punti di partenza (SpawnPoint) dentro un oggetto.
    /// Menu: Treno/Verifica/Compenetrazioni. In batch: TrenoBambini.EditorTools.VerificaCompenetrazioni.Esegui()
    /// Rapporto in console e in Docs/richieste/media/R006/compenetrazioni.txt.
    /// </summary>
    public static class VerificaCompenetrazioni
    {
        const string ScenesFolder = "Assets/_Game/Scenes";
        const string ReportPath = "Docs/richieste/media/R006/compenetrazioni.txt";
        static readonly string[] Scene = { "01_Titolo", "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe" };

        /// <summary>Penetrazione minima (m) oltre la quale una coppia viene segnalata.</summary>
        public const float Tolleranza = 0.02f;
        /// <summary>Distanza massima (m) sotto la base entro cui un oggetto e' considerato appoggiato.</summary>
        public const float SogliaAppoggio = 0.05f;
        /// <summary>Quota minima della base sotto il pavimento (m) prima di segnalare "sotto il pavimento".</summary>
        public const float SogliaSottoPavimento = 0.03f;

        // pavimenti e terreni: superfici di appoggio, mai segnalati come compenetrazione tra loro o con gli oggetti appoggiati
        static readonly Regex RxPavimento = new Regex(@"^(Selciato|Pavimento|FondoVicolo|TerraMacerie)");
        // strutture (pareti, facciate): si controlla il loro corpo, non le decorazioni
        static readonly Regex RxStruttura = new Regex(@"^(Casa_|Chiusura|Muro)");
        // contatti voluti con le pareti: battenti nei varchi, insegne, lampioni e pannelli fissati al muro
        static readonly Regex RxFissatoAlMuro = new Regex(@"^(Porta|Insegna|LampioneMuro|Edicola|Altarino|Stenditoio)");
        // oggetti appesi/fissati per costruzione: niente controllo di appoggio
        static readonly Regex RxAppeso = new Regex(@"^(Panno|Filo|Tenda|BastoneTenda|Insegna|LampioneMuro|Stenditoio|Fiamma|Lumino|Brace|VetroFinestra|EscapeExit|Edicola|Altarino|Statuetta|Luce)");
        // cumuli d'insieme: i pezzi si sovrappongono per natura (macerie, trave caduta sulle macerie)
        // luci/bagliori emissivi e vetri luminosi
        static readonly Regex RxLuce = new Regex(@"^(Fiamma|Lumino|EdicolaLumino|Brace|VetroFinestra|EscapeExit|Glow)");
        static readonly Regex RxPannoAppeso = new Regex(@"^Panno_");
        static readonly Regex RxSostegno = new Regex(@"^(Filo|Stenditoio|BastoneTenda)");
        static readonly Regex RxCumulo = new Regex(@"^(Macerie|TraveSpezzata)");

        sealed class Segnalazione { public float depth; public Obb a, b; public Vector3 pos; }

        sealed class Obb
        {
            public Renderer rend;
            public GameObject item;      // radice dell'oggetto di scena (figlio del contenitore)
            public string nome;          // nome per il rapporto
            public Vector3 c;            // centro mondo
            public Vector3[] ax;         // 3 assi mondo (unitari)
            public Vector3 h;            // semi-estensioni lungo gli assi
            public bool pavimento, struttura;
            public Bounds aabb;
        }

        [MenuItem("Treno/Verifica/Compenetrazioni")]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int n = Esegui();
            Debug.Log("[Verifica] Compenetrazioni: " + n + " segnalazioni. Rapporto: " + ReportPath);
        }

        /// <summary>Verifica tutte le scene di gioco; scrive il rapporto e restituisce il numero di segnalazioni.</summary>
        public static int Esegui()
        {
            return Esegui(Scene);
        }

        public static int Esegui(string[] scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Verifica compenetrazioni - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Tolleranza penetrazione " + (Tolleranza * 100f).ToString("0") + " cm, appoggio " + (SogliaAppoggio * 100f).ToString("0")
                          + " cm, sotto il pavimento " + (SogliaSottoPavimento * 100f).ToString("0") + " cm.");
            sb.AppendLine();
            int totale = 0;
            foreach (var n in scene)
            {
                string path = ScenesFolder + "/" + n + ".unity";
                if (!File.Exists(path)) { sb.AppendLine("[" + n + "] scena mancante"); continue; }
                var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var righe = new List<string>();
                VerificaScena(sc.name, righe);
                sb.AppendLine("== " + n + ": " + righe.Count + " segnalazioni ==");
                foreach (var l in righe) sb.AppendLine("  " + l);
                sb.AppendLine();
                totale += righe.Count;
            }
            sb.AppendLine("TOTALE: " + totale);
            string full = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Verifica] " + (totale == 0 ? "Nessuna compenetrazione." : sb.ToString()));
            return totale;
        }

        // ------------------------------------------------------------------ verifica di una scena

        static void VerificaScena(string scena, List<string> out_)
        {
            idItem.Clear();
            var obbs = Raccogli();
            // 1) coppie che si intersecano (un'unica segnalazione per coppia di oggetti, con la penetrazione massima)
            var coppie = new Dictionary<long, Segnalazione>();
            for (int i = 0; i < obbs.Count; i++)
                for (int j = i + 1; j < obbs.Count; j++)
                {
                    var a = obbs[i]; var b = obbs[j];
                    if (a.item == b.item) continue;
                    if (a.pavimento || b.pavimento) continue;
                    if (a.struttura && b.struttura) continue;
                    if (!a.aabb.Intersects(b.aabb)) continue;
                    if (Esclusa(a, b)) continue;
                    float d = Penetrazione(a, b);
                    if (d <= Tolleranza) continue;
                    int ia = IdItem(a.item), ib = IdItem(b.item);
                    long key = ia < ib ? ((long)ia << 32) ^ (uint)ib : ((long)ib << 32) ^ (uint)ia;
                    Segnalazione s;
                    if (!coppie.TryGetValue(key, out s) || s.depth < d) coppie[key] = new Segnalazione { depth = d, a = a, b = b, pos = Vector3.Lerp(a.c, b.c, 0.5f) };
                }
            foreach (var kv in coppie)
            {
                var s = kv.Value;
                out_.Add("[" + scena + "] COMPENETRAZIONE " + (s.depth * 100f).ToString("0.0") + " cm: " + s.a.nome + " <-> " + s.b.nome + "  @" + Fmt(s.pos));
            }
            // 2) sospesi e 3) sotto il pavimento (a livello di oggetto di scena: contano le scatole piu' basse)
            var perItem = new Dictionary<GameObject, List<Obb>>();
            foreach (var o in obbs)
            {
                List<Obb> l;
                if (!perItem.TryGetValue(o.item, out l)) perItem[o.item] = l = new List<Obb>();
                l.Add(o);
            }
            foreach (var kv in perItem)
            {
                var parti = kv.Value;
                if (parti[0].pavimento || parti[0].struttura) continue;
                float base_ = float.MaxValue;
                foreach (var o in parti) base_ = Mathf.Min(base_, o.aabb.min.y);
                var rif = parti[0];
                if (base_ < -SogliaSottoPavimento)
                {
                    out_.Add("[" + scena + "] SOTTO IL PAVIMENTO " + (-base_ * 100f).ToString("0.0") + " cm: " + rif.nome + "  @" + Fmt(rif.c));
                    continue;
                }
                if (RxAppeso.IsMatch(kv.Key.name) || RxAppeso.IsMatch(rif.rend.gameObject.name)) continue;
                if (base_ <= SogliaAppoggio) continue; // a terra
                bool ok = false;
                foreach (var o in parti)
                {
                    if (o.aabb.min.y > base_ + SogliaAppoggio) continue; // solo la base dell'oggetto
                    if (Appoggiato(o, obbs)) { ok = true; break; }
                }
                if (!ok)
                    out_.Add("[" + scena + "] SOSPESO (base a " + (base_ * 100f).ToString("0") + " cm, nessun appoggio entro "
                             + (SogliaAppoggio * 100f).ToString("0") + " cm): " + rif.nome + "  @" + Fmt(rif.c));
            }
            // 4) spawn dentro un oggetto
            foreach (var sp in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
            {
                var p = sp.transform.position + Vector3.up * 0.5f;
                foreach (var o in obbs)
                {
                    if (o.pavimento) continue;
                    if (Dentro(o, p, 0.25f)) out_.Add("[" + scena + "] SPAWN '" + sp.id + "' dentro " + o.nome + "  @" + Fmt(sp.transform.position));
                }
            }
        }

        static readonly Dictionary<GameObject, int> idItem = new Dictionary<GameObject, int>();
        static int IdItem(GameObject g)
        {
            int id;
            if (!idItem.TryGetValue(g, out id)) idItem[g] = id = idItem.Count + 1;
            return id;
        }

        static string Fmt(Vector3 v) { return "(" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ", " + v.z.ToString("0.00") + ")"; }

        static bool Esclusa(Obb a, Obb b)
        {
            // struttura contro oggetto fissato al muro (battenti, insegne, lampioni, edicole): contatto voluto
            if (a.struttura && RxFissatoAlMuro.IsMatch(b.item.name)) return true;
            if (b.struttura && RxFissatoAlMuro.IsMatch(a.item.name)) return true;
            if (RxCumulo.IsMatch(a.item.name) && RxCumulo.IsMatch(b.item.name)) return true;
            // panno steso: il bordo alto sta a cavallo del filo / dell'asta (contatto voluto)
            if (RxPannoAppeso.IsMatch(a.item.name) && RxSostegno.IsMatch(b.item.name)) return true;
            if (RxPannoAppeso.IsMatch(b.item.name) && RxSostegno.IsMatch(a.item.name)) return true;
            return false;
        }

        // ------------------------------------------------------------------ raccolta degli ingombri

        static List<Obb> Raccogli()
        {
            var list = new List<Obb>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!r.enabled) continue;
                var go = r.gameObject;
                if (go.name == "LOD1") continue;                    // livello a distanza della stessa facciata
                if (go.GetComponentInParent<TrenoBambini.Player.ThirdPersonController>() != null) continue; // il giocatore
                if (go.GetComponentInParent<Canvas>() != null) continue;
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var item = ItemRoot(go.transform);
                // luci e bagliori emissivi (fiamme, lumini, braci, vetri luminosi) stanno dentro i loro supporti per costruzione
                if (RxLuce.IsMatch(go.name)) continue;
                bool pav = RxPavimento.IsMatch(item.name) || RxPavimento.IsMatch(go.name);
                bool str = RxStruttura.IsMatch(item.name);
                string nome = Percorso(go.transform);
                var t = go.transform;
                var ls = t.lossyScale;
                // le facciate: corpo (collider della radice) + decorazioni del piano terra (zoccolo, cornici, soglie, grate); le pareti interne: scatole del kit
                var bc = str && go.name == "LOD0" && item.GetComponent<BoxCollider>() != null ? item.GetComponent<BoxCollider>() : null;
                if (bc != null)
                {
                    var tc = bc.transform;
                    var s = bc.size; var lc = tc.lossyScale;
                    list.Add(Crea(r, item, nome, tc.TransformPoint(bc.center), new[] { tc.right.normalized, tc.up.normalized, tc.forward.normalized },
                        new Vector3(Mathf.Abs(s.x * lc.x), Mathf.Abs(s.y * lc.y), Mathf.Abs(s.z * lc.z)) * 0.5f, pav, str));
                    var deco = ScatoleKit(mf.sharedMesh);
                    if (deco != null)
                        foreach (var b in deco)
                        {
                            var ob = Crea(r, item, nome, t.TransformPoint(b.center), new[] { t.right.normalized, t.up.normalized, t.forward.normalized },
                                new Vector3(Mathf.Abs(b.extents.x * ls.x), Mathf.Abs(b.extents.y * ls.y), Mathf.Abs(b.extents.z * ls.z)), pav, str);
                            if (ob.aabb.min.y < 3.3f) list.Add(ob);
                        }
                    continue;
                }
                var ax = new[] { t.right.normalized, t.up.normalized, t.forward.normalized };
                var mesh = mf.sharedMesh;
                string ap = AssetDatabase.GetAssetPath(mesh);
                List<Bounds> scatole = !pav && ap.StartsWith(KitFolder) ? ScatoleKit(mesh) : null;
                if (scatole != null)
                {
                    // mesh del kit: ogni scatola e' un ingombro a se' (scaffali, banchi, letti non sono un unico blocco pieno)
                    foreach (var b in scatole)
                        list.Add(Crea(r, item, nome, t.TransformPoint(b.center), ax,
                            new Vector3(Mathf.Abs(b.extents.x * ls.x), Mathf.Abs(b.extents.y * ls.y), Mathf.Abs(b.extents.z * ls.z)), pav, str));
                }
                else
                {
                    var mb = mesh.bounds;
                    list.Add(Crea(r, item, nome, t.TransformPoint(mb.center), ax,
                        new Vector3(Mathf.Abs(mb.extents.x * ls.x), Mathf.Abs(mb.extents.y * ls.y), Mathf.Abs(mb.extents.z * ls.z)), pav, str));
                }
            }
            return list;
        }

        const string KitFolder = "Assets/_Game/Generated/Kit";

        static Obb Crea(Renderer r, GameObject item, string nome, Vector3 c, Vector3[] ax, Vector3 h, bool pav, bool str)
        {
            var o = new Obb { rend = r, item = item, nome = nome, c = c, ax = ax, h = h, pavimento = pav, struttura = str };
            var bb = new Bounds(c, Vector3.zero);
            for (int k = 0; k < 8; k++)
                bb.Encapsulate(c + ax[0] * h.x * ((k & 1) == 0 ? -1 : 1) + ax[1] * h.y * ((k & 2) == 0 ? -1 : 1) + ax[2] * h.z * ((k & 4) == 0 ? -1 : 1));
            o.aabb = bb;
            return o;
        }

        static readonly Dictionary<Mesh, List<Bounds>> cacheScatole = new Dictionary<Mesh, List<Bounds>>();

        // Ricostruisce le scatole di una mesh del kit (KitBuilder.Box emette fino a 6 quad consecutivi nell'ordine -x,+x,-y,+y,-z,+z):
        // un nuovo quad con ordine non crescente apre una nuova scatola. Se la mesh non ha la struttura attesa restituisce null.
        static List<Bounds> ScatoleKit(Mesh m)
        {
            List<Bounds> res;
            if (cacheScatole.TryGetValue(m, out res)) return res;
            res = null;
            var v = m.vertices; var nrm = m.normals;
            int nq = v.Length / 4;
            // GenerateSecondaryUVSet puo' aggiungere vertici in coda: i quad originali stanno all'inizio
            var tmp = new List<Bounds>();
            bool ok = nq > 0 && nrm.Length == v.Length;
            int prevOrd = 99;
            Bounds cur = new Bounds();
            bool open = false;
            for (int q = 0; q < nq && ok; q++)
            {
                var nn = nrm[q * 4];
                int ord;
                if (Mathf.Abs(nn.x) > 0.99f) ord = nn.x < 0 ? 0 : 1;
                else if (Mathf.Abs(nn.y) > 0.99f) ord = nn.y < 0 ? 2 : 3;
                else if (Mathf.Abs(nn.z) > 0.99f) ord = nn.z < 0 ? 4 : 5;
                else { ok = false; break; }
                var qb = new Bounds(v[q * 4], Vector3.zero);
                for (int k = 1; k < 4; k++) qb.Encapsulate(v[q * 4 + k]);
                if (!open || ord <= prevOrd)
                {
                    if (open) tmp.Add(cur);
                    cur = qb; open = true;
                }
                else cur.Encapsulate(qb);
                prevOrd = ord;
            }
            if (ok && open) { tmp.Add(cur); res = tmp; }
            cacheScatole[m] = res;
            return res;
        }

        // oggetto di scena = figlio diretto di un contenitore (Ambiente, Arredo, Porte...); un oggetto alla radice e' un oggetto a se'
        static readonly Regex RxContenitore = new Regex(@"^(Ambiente|Arredo|Porte|Diorama|Uscita|MuriInvisibili)$");
        static GameObject ItemRoot(Transform t)
        {
            var cur = t;
            while (cur.parent != null && !RxContenitore.IsMatch(cur.parent.name)) cur = cur.parent;
            return cur.gameObject;
        }

        static string Percorso(Transform t)
        {
            string s = t.name;
            int depth = 0;
            while (t.parent != null && depth++ < 3) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        // ------------------------------------------------------------------ correzione automatica (usata dai costruttori di scena)

        /// <summary>
        /// Sposta in orizzontale gli oggetti "mobili" (appoggiati a terra) che si compenetrano con pareti o altri oggetti, lungo l'asse di
        /// penetrazione minima, finche' non ci sono piu' intersezioni oltre la tolleranza. I "cavalieri" (oggetti appoggiati sopra un mobile)
        /// seguono il loro supporto. Restituisce il numero di spostamenti eseguiti. Usa gli stessi ingombri della verifica.
        /// </summary>
        public static int Risolvi(IList<GameObject> mobili, IDictionary<GameObject, List<GameObject>> cavalieri, int maxIter = 60)
        {
            var set = new Dictionary<GameObject, int>();
            for (int i = 0; i < mobili.Count; i++) if (mobili[i] != null) set[mobili[i]] = i;
            int spostamenti = 0;
            for (int it = 0; it < maxIter; it++)
            {
                idItem.Clear();
                var obbs = Raccogli();
                bool mosso = false;
                for (int i = 0; i < obbs.Count && !mosso; i++)
                    for (int j = i + 1; j < obbs.Count && !mosso; j++)
                    {
                        var a = obbs[i]; var b = obbs[j];
                        if (a.item == b.item) continue;
                        if (a.pavimento || b.pavimento) continue;
                        if (a.struttura && b.struttura) continue;
                        if (!a.aabb.Intersects(b.aabb)) continue;
                        if (Esclusa(a, b)) continue;
                        int ia, ib;
                        bool ma = set.TryGetValue(a.item, out ia), mb = set.TryGetValue(b.item, out ib);
                        if (!ma && !mb) continue;
                        Obb mover = ma && mb ? (ia > ib ? a : b) : (ma ? a : b);
                        Obb altro = mover == a ? b : a;
                        // un mobile non si sposta per colpa di un proprio cavaliere
                        List<GameObject> cav;
                        if (cavalieri != null && cavalieri.TryGetValue(mover.item, out cav) && cav.Contains(altro.item)) continue;
                        if (cavalieri != null && cavalieri.TryGetValue(altro.item, out cav) && cav.Contains(mover.item)) continue;
                        Vector3 dir; float ov;
                        if (!SeparazioneOrizzontale(mover, altro, out dir, out ov)) continue;
                        if (ov <= Tolleranza) continue;
                        var delta = dir * (ov + 0.01f);
                        Muovi(mover.item, delta);
                        if (cavalieri != null && cavalieri.TryGetValue(mover.item, out cav)) foreach (var c in cav) if (c != null) Muovi(c, delta);
                        spostamenti++;
                        mosso = true;
                    }
                if (!mosso) break;
            }
            return spostamenti;
        }

        static void Muovi(GameObject g, Vector3 delta)
        {
            g.transform.position += delta;
        }

        // minima traslazione orizzontale che separa 'mover' da 'altro'; dir punta dall'altro verso il mover
        static bool SeparazioneOrizzontale(Obb mover, Obb altro, out Vector3 dir, out float ov)
        {
            dir = Vector3.zero; ov = 0f;
            var cand = new List<Vector3>();
            foreach (var o in new[] { mover, altro })
                foreach (var u in new[] { o.ax[0], o.ax[1], o.ax[2] })
                {
                    var h = new Vector3(u.x, 0f, u.z);
                    if (h.sqrMagnitude > 0.25f) cand.Add(h.normalized);
                }
            float best = float.MaxValue;
            var d = mover.c - altro.c;
            foreach (var u in cand)
            {
                float ra = Raggio(mover, u), rb = Raggio(altro, u);
                float dd = Vector3.Dot(d, u);
                float o = ra + rb - Mathf.Abs(dd);
                if (o <= 0f) return false; // separati lungo un asse orizzontale: niente da fare in orizzontale
                if (o < best) { best = o; dir = dd >= 0f ? u : -u; }
            }
            if (best == float.MaxValue) return false;
            ov = best;
            return true;
        }

        static float Raggio(Obb o, Vector3 u)
        {
            return o.h.x * Mathf.Abs(Vector3.Dot(o.ax[0], u)) + o.h.y * Mathf.Abs(Vector3.Dot(o.ax[1], u)) + o.h.z * Mathf.Abs(Vector3.Dot(o.ax[2], u));
        }

        // ------------------------------------------------------------------ geometria

        // penetrazione minima (SAT su 15 assi) tra due OBB; 0 se separate
        static float Penetrazione(Obb a, Obb b)
        {
            var axes = new List<Vector3>(15);
            axes.AddRange(a.ax); axes.AddRange(b.ax);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    var c = Vector3.Cross(a.ax[i], b.ax[j]);
                    if (c.sqrMagnitude > 1e-4f) axes.Add(c.normalized);
                }
            var d = b.c - a.c;
            float min = float.MaxValue;
            foreach (var u in axes)
            {
                float ra = a.h.x * Mathf.Abs(Vector3.Dot(a.ax[0], u)) + a.h.y * Mathf.Abs(Vector3.Dot(a.ax[1], u)) + a.h.z * Mathf.Abs(Vector3.Dot(a.ax[2], u));
                float rb = b.h.x * Mathf.Abs(Vector3.Dot(b.ax[0], u)) + b.h.y * Mathf.Abs(Vector3.Dot(b.ax[1], u)) + b.h.z * Mathf.Abs(Vector3.Dot(b.ax[2], u));
                float ov = ra + rb - Mathf.Abs(Vector3.Dot(d, u));
                if (ov <= 0f) return 0f;
                if (ov < min) min = ov;
            }
            return min;
        }

        static bool Dentro(Obb o, Vector3 p, float margine)
        {
            var d = p - o.c;
            return Mathf.Abs(Vector3.Dot(d, o.ax[0])) < o.h.x + margine
                && Mathf.Abs(Vector3.Dot(d, o.ax[1])) < o.h.y + margine
                && Mathf.Abs(Vector3.Dot(d, o.ax[2])) < o.h.z + margine;
        }

        // appoggio: almeno un punto campione della base cade (con 5 cm di tolleranza) dentro l'ingombro di un altro oggetto
        static bool Appoggiato(Obb o, List<Obb> tutti)
        {
            float yb = o.aabb.min.y;
            var pts = new List<Vector3> { o.c };
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    pts.Add(o.c + o.ax[0] * o.h.x * 0.6f * sx + o.ax[2] * o.h.z * 0.6f * sz);
            foreach (var p0 in pts)
            {
                var p = new Vector3(p0.x, yb - SogliaAppoggio * 0.5f, p0.z);
                foreach (var q in tutti)
                {
                    if (q.item == o.item) continue;
                    if (Dentro(q, p, SogliaAppoggio * 0.5f)) return true;
                }
            }
            return false;
        }
    }
}
