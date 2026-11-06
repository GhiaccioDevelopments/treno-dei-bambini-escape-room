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
        static readonly Regex RxFissatoAlMuro = new Regex(@"^(Porta|Appeso|Insegna|LampioneMuro|Edicola|Altarino|Stenditoio|Manifesto|Cartello|Lanterna)");
        // oggetti appesi/fissati per costruzione: niente controllo di appoggio
        static readonly Regex RxAppeso = new Regex(@"^(Appeso|Decal|TraveSpezzata|Macerie|Panno|Filo|Tenda|BastoneTenda|Insegna|LampioneMuro|Stenditoio|Fiamma|Lumino|Brace|VetroFinestra|EscapeExit|Edicola|Altarino|Statuetta|Luce|Manifesto|Cartello|Lanterna|Bucato)");
        // cumuli d'insieme: i pezzi si sovrappongono per natura (macerie, trave caduta sulle macerie)
        // luci/bagliori emissivi e vetri luminosi
        static readonly Regex RxLuce = new Regex(@"^(Fiamma|Lumino|EdicolaLumino|Brace|VetroFinestra|EscapeExit|Glow|Curvo|Acqua)");
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
            public bool quad;            // faccia piana di una mesh del kit (superficie sottile): il "solido" sta dietro la faccia
            public int nAxis; public int nSign; // asse locale (0=x,1=y,2=z della mesh) e verso della normale uscente della faccia
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
            foreach (var pr in CoppieVicine(obbs))
                {
                    var a = obbs[pr.Item1]; var b = obbs[pr.Item2];
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
                out_.Add("[" + scena + "] COMPENETRAZIONE " + (s.depth * 100f).ToString("0.0") + " cm: " + s.a.nome + (s.a.quad ? "[faccia]" : "") + " <-> " + s.b.nome + (s.b.quad ? "[faccia]" : "") + "  @" + Fmt(s.pos)
                         + "  (a: c" + Fmt(s.a.c) + " h" + Fmt(s.a.h) + "; b: c" + Fmt(s.b.c) + " h" + Fmt(s.b.h) + ")");
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
                    if (o.pavimento || o.quad) continue;
                    if (Dentro(o, p, 0.25f)) out_.Add("[" + scena + "] SPAWN '" + sp.id + "' dentro " + o.nome + "  @" + Fmt(sp.transform.position));
                }
            }
        }

        /// <summary>Coppie di ingombri con AABB sovrapposte (sweep lungo x: regge migliaia di ingombri). Ordine deterministico.</summary>
        static IEnumerable<Tuple<int, int>> CoppieVicine(List<Obb> obbs)
        {
            var idx = new int[obbs.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            Array.Sort(idx, (p, q) => { int c = obbs[p].aabb.min.x.CompareTo(obbs[q].aabb.min.x); return c != 0 ? c : p.CompareTo(q); }); // spareggio per indice: ordinamento stabile
            for (int ii = 0; ii < idx.Length; ii++)
            {
                var a = obbs[idx[ii]];
                float maxX = a.aabb.max.x;
                for (int jj = ii + 1; jj < idx.Length; jj++)
                {
                    var b = obbs[idx[jj]];
                    if (b.aabb.min.x > maxX) break;
                    if (a.aabb.max.y < b.aabb.min.y || b.aabb.max.y < a.aabb.min.y) continue;
                    if (a.aabb.max.z < b.aabb.min.z || b.aabb.max.z < a.aabb.min.z) continue;
                    int p = idx[ii], q = idx[jj];
                    yield return p < q ? Tuple.Create(p, q) : Tuple.Create(q, p);
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

        static string ChiaveOrdine(Transform t)
        {
            var sb = new StringBuilder();
            var st = new Stack<int>();
            for (var c = t; c != null; c = c.parent) st.Push(c.GetSiblingIndex());
            sb.Append(t.gameObject.scene.rootCount >= 0 ? t.root.name : "");
            foreach (var i in st) sb.Append('/').Append(i.ToString("D6"));
            return sb.ToString();
        }

        static List<Obb> Raccogli()
        {
            var list = new List<Obb>();
            // ordine stabile: per percorso di indici di fratellanza nella gerarchia (FindObjectsByType non garantisce alcun ordine)
            var rends = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
            var chiavi = new Dictionary<MeshRenderer, string>(rends.Length);
            foreach (var r0 in rends) chiavi[r0] = ChiaveOrdine(r0.transform);
            Array.Sort(rends, (x, y) => string.CompareOrdinal(chiavi[x], chiavi[y]));
            foreach (var r in rends)
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
                var ax = new[] { t.right.normalized, t.up.normalized, t.forward.normalized };
                var mesh = mf.sharedMesh;
                string ap = AssetDatabase.GetAssetPath(mesh);
                bool kit = !pav && ap.StartsWith(KitFolder);
                if (bc != null)
                {
                    var tc = bc.transform;
                    var s = bc.size; var lc = tc.lossyScale;
                    list.Add(Crea(r, item, nome, tc.TransformPoint(bc.center), new[] { tc.right.normalized, tc.up.normalized, tc.forward.normalized },
                        new Vector3(Mathf.Abs(s.x * lc.x), Mathf.Abs(s.y * lc.y), Mathf.Abs(s.z * lc.z)) * 0.5f, pav, str));
                }
                var facce = kit ? FacceKit(mesh) : null;
                if (facce != null)
                {
                    // mesh del kit: ogni faccia e' una superficie (le facce di scatole che si toccano non si fondono in un blocco falso)
                    foreach (var f in facce)
                    {
                        var h = new Vector3(Mathf.Abs(f.half.x * ls.x), Mathf.Abs(f.half.y * ls.y), Mathf.Abs(f.half.z * ls.z));
                        var ob = Crea(r, item, nome, t.TransformPoint(f.centro), ax, h, pav, str);
                        ob.quad = true; ob.nAxis = f.axis; ob.nSign = f.sign;
                        if (bc != null && ob.aabb.min.y >= item.transform.position.y + 3.3f) continue; // facciate: solo il piano terra e il primo piano
                        list.Add(ob);
                    }
                }
                else if (bc == null)
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

        struct Faccia { public Vector3 centro, half; public int axis, sign; }
        static readonly Dictionary<Mesh, List<Faccia>> cacheFacce = new Dictionary<Mesh, List<Faccia>>();

        // Facce piane di una mesh del kit: si leggono dai triangoli (la coppia di triangoli consecutivi di un quad con lo stesso piano si fonde in
        // un'unica faccia). Non si assume piu' l'ordine dei vertici: GenerateSecondaryUVSet puo' rimescolarli. Se una mesh ha normali non allineate
        // agli assi (tubi, archi) restituisce null e si ripiega sull'ingombro della mesh.
        static List<Faccia> FacceKit(Mesh m)
        {
            List<Faccia> res;
            if (cacheFacce.TryGetValue(m, out res)) return res;
            res = null;
            var v = m.vertices; var nrm = m.normals;
            var tmp = new List<Faccia>();
            bool ok = nrm.Length == v.Length && v.Length > 0;
            for (int sm = 0; sm < m.subMeshCount && ok; sm++)
            {
                var t = m.GetTriangles(sm);
                int i = 0;
                while (i + 2 < t.Length && ok)
                {
                    int axis, sign;
                    if (!AssiNormale(nrm[t[i]], out axis, out sign)) { ok = false; break; }
                    var b = new Bounds(v[t[i]], Vector3.zero); b.Encapsulate(v[t[i + 1]]); b.Encapsulate(v[t[i + 2]]);
                    float area1 = AreaTri(v[t[i]], v[t[i + 1]], v[t[i + 2]]);
                    bool unito = false;
                    if (i + 5 < t.Length)
                    {
                        int ax2, sg2;
                        if (AssiNormale(nrm[t[i + 3]], out ax2, out sg2) && ax2 == axis && sg2 == sign)
                        {
                            var b2 = b; b2.Encapsulate(v[t[i + 3]]); b2.Encapsulate(v[t[i + 4]]); b2.Encapsulate(v[t[i + 5]]);
                            float area2 = AreaTri(v[t[i + 3]], v[t[i + 4]], v[t[i + 5]]);
                            float piano1 = Mathf.Abs(Coord(v[t[i]], axis) - Coord(v[t[i + 3]], axis));
                            var sz = b2.size; float rett = axis == 0 ? sz.y * sz.z : axis == 1 ? sz.x * sz.z : sz.x * sz.y;
                            if (piano1 < 1e-3f && area1 + area2 > 1e-6f && Mathf.Abs(rett - (area1 + area2)) < 0.02f * (area1 + area2) + 1e-6f) { b = b2; unito = true; }
                        }
                    }
                    i += unito ? 6 : 3;
                    var half = b.extents;
                    if (axis == 0) half.x = 0.0005f; else if (axis == 1) half.y = 0.0005f; else half.z = 0.0005f;
                    if (half.x * half.y * half.z <= 0f) continue;
                    tmp.Add(new Faccia { centro = b.center, half = half, axis = axis, sign = sign });
                }
            }
            if (ok && tmp.Count > 0) res = tmp;
            cacheFacce[m] = res;
            return res;
        }

        static bool AssiNormale(Vector3 nn, out int axis, out int sign)
        {
            axis = 0; sign = 1;
            if (Mathf.Abs(nn.x) > 0.99f) { axis = 0; sign = nn.x < 0 ? -1 : 1; return true; }
            if (Mathf.Abs(nn.y) > 0.99f) { axis = 1; sign = nn.y < 0 ? -1 : 1; return true; }
            if (Mathf.Abs(nn.z) > 0.99f) { axis = 2; sign = nn.z < 0 ? -1 : 1; return true; }
            return false;
        }

        static float Coord(Vector3 p, int axis) { return axis == 0 ? p.x : axis == 1 ? p.y : p.z; }
        static float AreaTri(Vector3 a, Vector3 b, Vector3 c) { return Vector3.Cross(b - a, c - a).magnitude * 0.5f; }

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
        public static int Risolvi(IList<GameObject> mobili, IDictionary<GameObject, List<GameObject>> cavalieri, int maxIter = 120)
        {
            var set = new Dictionary<GameObject, int>();
            for (int i = 0; i < mobili.Count; i++) if (mobili[i] != null) set[mobili[i]] = i;
            int spostamenti = 0;
            for (int it = 0; it < maxIter; it++)
            {
                idItem.Clear();
                var obbs = Raccogli();
                var mossi = new HashSet<GameObject>();
                foreach (var pr in CoppieVicine(obbs))
                {
                    var a = obbs[pr.Item1]; var b = obbs[pr.Item2];
                    if (a.item == b.item) continue;
                    if (a.pavimento || b.pavimento) continue;
                    if (a.struttura && b.struttura) continue;
                    if (Esclusa(a, b)) continue;
                    int ia, ib;
                    bool ma = set.TryGetValue(a.item, out ia), mb = set.TryGetValue(b.item, out ib);
                    if (!ma && !mb) continue;
                    if (Penetrazione(a, b) <= Tolleranza) continue;
                    Obb mover = ma && mb ? (ia > ib ? a : b) : (ma ? a : b);
                    Obb altro = mover == a ? b : a;
                    if (mossi.Contains(mover.item)) continue; // un solo spostamento per oggetto e per giro: gli ingombri sono ricalcolati al giro dopo
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
                    mossi.Add(mover.item);
                    spostamenti++;
                }
                if (mossi.Count == 0) break;
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

        // profondita' (m) con cui 'o' entra dietro la faccia q (verso -normale), 0 se non la attraversa. E' limitata dalle sovrapposizioni
        // lungo i due assi della faccia: un oggetto che tocca soltanto il bordo (appoggiato sopra una tavola sottile) non e' una compenetrazione.
        static float ProfonditaDietroFaccia(Obb q, Obb o)
        {
            if (SAT(q, o) <= 0f) return 0f;
            var n = q.ax[q.nAxis] * q.nSign;
            float p = Vector3.Dot(q.c, n);
            float dietro = p - (Vector3.Dot(o.c, n) - Raggio(o, n));
            if (dietro <= 0f) return 0f;
            float res = dietro;
            var d = o.c - q.c;
            for (int k = 0; k < 3; k++)
            {
                if (k == q.nAxis) continue;
                var u = q.ax[k];
                float ov = Raggio(q, u) + Raggio(o, u) - Mathf.Abs(Vector3.Dot(d, u));
                if (ov < res) res = ov;
            }
            return Mathf.Max(0f, res);
        }

        // penetrazione tra due ingombri: per le facce del kit e' la profondita' dietro la faccia, per i solidi il SAT
        static float Penetrazione(Obb a, Obb b)
        {
            if (a.quad && b.quad) return Mathf.Max(ProfonditaDietroFaccia(a, b), ProfonditaDietroFaccia(b, a));
            if (a.quad) return ProfonditaDietroFaccia(a, b);
            if (b.quad) return ProfonditaDietroFaccia(b, a);
            return SAT(a, b);
        }

        // penetrazione minima (SAT su 15 assi) tra due OBB; 0 se separate
        static float SAT(Obb a, Obb b)
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

        // appoggio: almeno un punto campione della base sta sopra la faccia superiore di un altro oggetto, entro 5 cm in quota
        static bool Appoggiato(Obb o, List<Obb> tutti)
        {
            float yb = o.aabb.min.y;
            var pts = new List<Vector3> { o.c };
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    pts.Add(o.c + o.ax[0] * o.h.x * 0.6f * sx + o.ax[2] * o.h.z * 0.6f * sz);
            foreach (var p in pts)
                foreach (var q in tutti)
                {
                    if (q.item == o.item) continue;
                    if (q.quad && !(q.nAxis == 1 && q.nSign > 0)) continue; // appoggia solo su facce orizzontali rivolte in alto
                    var d = p - q.c;
                    if (Mathf.Abs(Vector3.Dot(d, q.ax[0])) > q.h.x + 0.01f || Mathf.Abs(Vector3.Dot(d, q.ax[2])) > q.h.z + 0.01f) continue;
                    float top = q.c.y + (q.quad ? 0f : q.h.y);
                    if (Mathf.Abs(yb - top) <= SogliaAppoggio) return true;
                }
            return false;
        }
    }
}
