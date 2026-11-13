using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Core;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R017: diagnosi delle superfici delle scene di gioco.
    /// 1) Sfarfallii (z-fighting): coppie di facce con la stessa normale, a meno di 2 mm di distanza lungo la normale e con aree che si
    ///    sovrappongono (oltre 3 mm in ogni direzione), tra renderer diversi o nello stesso mesh.
    /// 2) Buchi sul suolo: griglia di raggi verticali (passo 25 cm) su tutta l'area calpestabile (riempimento dal punto di partenza con la
    ///    capsula del giocatore); ogni punto dove il suolo calpestabile non ha una superficie visibile (solo facce frontali, come il rendering
    ///    con backface culling) e' un buco; se la sola faccia posteriore c'e', e' una faccia invertita.
    /// 3) Bordi aperti: dai punti calpestabili (griglia di 1 m, altezza della camera) raggi orizzontali e leggermente verso il basso in 16 direzioni:
    ///    un raggio che non colpisce niente (nemmeno a 150 m, il far della camera) mostra lo sfondo/cielo dove ci si aspetta un muro o il terreno.
    /// Menu: Treno/Verifica/Superfici e buchi. In batch: TrenoBambini.EditorTools.VerificaSuperfici.Esegui("prima"|"dopo").
    /// Rapporto: Docs/richieste/media/R017/rapporto_<etichetta>.txt. Non modifica le scene (i collisori di prova sono temporanei).
    /// </summary>
    public static class VerificaSuperfici
    {
        const string ScenesFolder = "Assets/_Game/Scenes";
        const string ReportFolder = "Docs/richieste/media/R017";
        static readonly string[] Scene = { "01_Titolo", "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci", "50_FB_Volantino", "60_FB_Stazione", "61_FB_Vagone", "70_FB_Modena" };

        public const float DistComplanare = 0.002f;   // 2 mm
        public const float SovrapposizioneMin = 0.003f; // 3 mm
        const float Passo = 0.25f;
        const float PassoRaggi = 1f;
        const float TolleranzaSuolo = 0.06f;

        [MenuItem("Treno/Verifica/Superfici e buchi")]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var r = Esegui("ultimo");
            Debug.Log("[Verifica] Superfici e buchi: " + r + " Rapporto: " + ReportFolder + "/rapporto_ultimo.txt");
        }

        /// <summary>Verifica tutte le scene, scrive il rapporto e restituisce una riga di riepilogo.</summary>
        public static string Esegui(string etichetta)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Verifica superfici e buchi (" + etichetta + ") - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Complanari: stessa normale, distanza < " + (DistComplanare * 1000f).ToString("0") + " mm, sovrapposizione > " + (SovrapposizioneMin * 1000f).ToString("0")
                          + " mm. Buchi: raggi dall'alto passo " + (Passo * 100f).ToString("0") + " cm sull'area calpestabile. Bordi aperti: raggi dalla camera, passo " + PassoRaggi.ToString("0") + " m.");
            sb.AppendLine();
            var tot = new int[4];
            var riepilogo = new StringBuilder();
            foreach (var n in Scene)
            {
                string path = ScenesFolder + "/" + n + ".unity";
                if (!File.Exists(path)) { sb.AppendLine("[" + n + "] scena mancante"); continue; }
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var s = new StringBuilder();
                int[] c = VerificaScena(n, s);
                sb.Append(s);
                for (int i = 0; i < 4; i++) tot[i] += c[i];
                riepilogo.Append(n + " [compl " + c[0] + " (visibili " + c[1] + "), buchi " + c[2] + ", bordi aperti " + c[3] + "]; ");
            }
            sb.AppendLine("TOTALE: complanari " + tot[0] + " (VISIBILI: " + tot[1] + "), buchi/facce invertite sul suolo " + tot[2] + ", raggi che escono nel vuoto " + tot[3]);
            string full = Path.GetFullPath(ReportFolder + "/rapporto_" + etichetta + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
            string rie = "TOTALE complanari " + tot[0] + " (visibili " + tot[1] + "), buchi " + tot[2] + ", bordi aperti " + tot[3] + " | " + riepilogo;
            return rie;
        }

        // ------------------------------------------------------------------ scena

        static int[] VerificaScena(string nome, StringBuilder sb)
        {
            var res = new int[4];
            sb.AppendLine("==================== " + nome + " ====================");

            foreach (var pl in GameObject.FindGameObjectsWithTag("Player")) pl.SetActive(false);
            Physics.SyncTransforms();

            // 2) buchi + 3) bordi aperti
            SpawnPoint sp = null;
            foreach (var s in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include)) { if (sp == null || s.id == "inizio" || s.id == "ingresso") sp = s; }
            float r = nome.StartsWith("30_") ? 0.2f : 0.28f, h = nome.StartsWith("30_") ? 1.0f : 1.75f;
            Area area = null;
            if (sp != null)
            {
                area = AreaCalpestabile(sp.transform.position, r, h);
                sb.AppendLine("-- Area calpestabile: " + area.celle.Count + " celle da " + (Passo * 100f).ToString("0") + " cm (" + (area.celle.Count * Passo * Passo).ToString("0") + " m2) dallo spawn '" + sp.id + "' --");
            }
            else sb.AppendLine("-- Nessuno SpawnPoint: scena senza area calpestabile (solo controllo complanari) --");

            camPunti.Clear();
            if (area != null)
            {
                int passo = Mathf.Max(1, area.celle.Count / 800);
                for (int i = 0; i < area.celle.Count; i += passo) camPunti.Add(area.celle[i] + Vector3.up * 2f);
            }
            else { var mc = Camera.main; if (mc != null) camPunti.Add(mc.transform.position); }
            var temp = AggiungiCollisoriVisivi();
            Physics.SyncTransforms();
            try
            {
            // 1) complanari
            var coppie = Complanari();
            res[0] = coppie.Count;
            foreach (var c in coppie) if (c.visibile) res[1]++;
            sb.AppendLine("-- Sfarfallii (superfici complanari): " + coppie.Count + " coppie di oggetti, di cui " + res[1] + " VISIBILI (qualunque materiale; almeno un punto della zona sovrapposta visto da una camera di prova). Le altre sono coperte da un volume --");
            coppie.Sort((a, b) => b.visibile.CompareTo(a.visibile) != 0 ? b.visibile.CompareTo(a.visibile) : b.triangoli.CompareTo(a.triangoli));
            int mostrate = 0;
            foreach (var c in coppie)
            {
                if (mostrate++ >= 400) { sb.AppendLine("  ... (altre " + (coppie.Count - 400) + ")"); break; }
                sb.AppendLine("  " + (c.visibile ? "[VISIBILE] " : c.matDiversi ? "[coperta] " : "[stesso mat] ") + c.nomeA + " (" + c.matA + ")  <>  " + c.nomeB + " (" + c.matB + ")  tri=" + c.triangoli
                              + "  pos=" + Fmt(c.pos) + "  n=" + Fmt(c.normale) + "  area sovrapposta=" + Fmt(c.ext.min) + ".." + Fmt(c.ext.max));
            }

                if (sp == null) { sb.AppendLine(); return res; }
                var buchi = new List<Vector3>(); var invertiti = new List<Vector3>();
                var tuttiPunti = new List<Vector3>(area.celle);
                tuttiPunti.AddRange(area.extra);
                foreach (var c in tuttiPunti)
                {
                    var p = c;
                    {
                        var o = p + Vector3.up * 0.4f;
                        Physics.queriesHitBackfaces = false;
                        RaycastHit hit;
                        bool ok = Physics.Raycast(o, Vector3.down, out hit, 1.0f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - c.y) <= TolleranzaSuolo + 0.25f && hit.normal.y > 0.3f;
                        if (ok) continue;
                        // un oggetto (scala, mobile) copre il suolo: la superficie visibile e' sopra, non e' un buco
                        if (Physics.Raycast(new Vector3(p.x, c.y + 2.3f, p.z), Vector3.down, out hit, 2.6f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y >= c.y - 0.3f) continue;
                        Physics.queriesHitBackfaces = true;
                        bool back = Physics.Raycast(o, Vector3.down, out hit, 1.0f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - c.y) <= 0.3f;
                        Physics.queriesHitBackfaces = false;
                        (back ? invertiti : buchi).Add(p);
                    }
                }
                res[2] = buchi.Count + invertiti.Count;
                sb.AppendLine("-- Buchi sul suolo calpestabile: " + buchi.Count + " punti senza superficie visibile, " + invertiti.Count + " punti con la sola faccia posteriore (normale invertita) --");
                Cluster(sb, "buco", buchi, 3f);
                Cluster(sb, "faccia invertita", invertiti, 3f);

                // bordi aperti
                int aperti = 0; int lanciati = 0;
                var apertiPos = new List<Vector3>();
                var apertiDir = new Dictionary<long, int>();
                var invDir = new List<Vector3>(); var apertiDettaglio = new List<string>();
                float far = 150f;
                var visti = new HashSet<long>();
                foreach (var c in area.celle)
                {
                    long key = ((long)Mathf.RoundToInt(c.x / PassoRaggi) << 32) ^ (uint)Mathf.RoundToInt(c.z / PassoRaggi);
                    if (!visti.Add(key)) continue;
                    var o = new Vector3(c.x, c.y + 1.7f, c.z);
                    for (int a = 0; a < 16; a++)
                    {
                        float az = a * 22.5f * Mathf.Deg2Rad;
                        foreach (float pitch in new[] { 0f, -12f })
                        {
                            float cp = Mathf.Cos(pitch * Mathf.Deg2Rad);
                            var dir = new Vector3(Mathf.Sin(az) * cp, Mathf.Sin(pitch * Mathf.Deg2Rad), Mathf.Cos(az) * cp);
                            lanciati++;
                            Physics.queriesHitBackfaces = false;
                            if (Physics.Raycast(o, dir, far, ~0, QueryTriggerInteraction.Ignore)) continue;
                            Physics.queriesHitBackfaces = true;
                            bool back = Physics.Raycast(o, dir, far, ~0, QueryTriggerInteraction.Ignore);
                            Physics.queriesHitBackfaces = false;
                            aperti++;
                            if (back) invDir.Add(o); else { apertiPos.Add(o); if (apertiDettaglio.Count < 60) apertiDettaglio.Add("  raggio da " + Fmt(o) + " azimut " + (a * 22.5f).ToString("0") + " gradi, inclinazione " + pitch.ToString("0")); }
                        }
                    }
                }
                res[3] = aperti;
                sb.AppendLine("-- Bordi aperti: " + aperti + " raggi su " + lanciati + " escono nel vuoto (" + apertiPos.Count + " senza nulla, " + invDir.Count + " che colpiscono solo una faccia posteriore) --");
                Cluster(sb, "vuoto visibile da", apertiPos, 6f);
                Cluster(sb, "faccia posteriore vista da", invDir, 6f);
                foreach (var l in apertiDettaglio) sb.AppendLine(l);
            }
            finally
            {
                Physics.queriesHitBackfaces = false;
                foreach (var t in temp) if (t != null) UnityEngine.Object.DestroyImmediate(t);
            }
            sb.AppendLine();
            return res;
        }

        /// <summary>Solo il controllo dei complanari sulla scena aperta (rapido, per iterare sui generatori); restituisce le righe VISIBILI.</summary>
        public static string SoloComplanari(int max)
        {
            var sb = new StringBuilder();
            var sp = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
            foreach (var pl in GameObject.FindGameObjectsWithTag("Player")) pl.SetActive(false);
            var temp = AggiungiCollisoriVisivi();
            Physics.SyncTransforms();
            try
            {
                var coppie = Complanari();
                int v = 0; foreach (var c in coppie) if (c.visibile) v++;
                sb.AppendLine("coppie " + coppie.Count + ", visibili " + v);
                int k = 0;
                foreach (var c in coppie)
                {
                    if (!c.visibile) continue;
                    if (k++ >= max) break;
                    sb.AppendLine(c.nomeA + "(" + c.matA + ") <> " + c.nomeB + "(" + c.matB + ") tri=" + c.triangoli + " n=" + Fmt(c.normale) + " ext=" + Fmt(c.ext.min) + ".." + Fmt(c.ext.max));
                }
            }
            finally { foreach (var t in temp) if (t != null) UnityEngine.Object.DestroyImmediate(t); }
            return sb.ToString();
        }

        static string F4(Vector3 v) { return "(" + v.x.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "," + v.z.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) + ")"; }
        static string Fmt(Vector3 v) { return "(" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ", " + v.z.ToString("0.00") + ")"; }

        static void Cluster(StringBuilder sb, string tipo, List<Vector3> punti, float lato)
        {
            if (punti.Count == 0) return;
            var m = new Dictionary<long, List<Vector3>>();
            foreach (var p in punti)
            {
                long k = ((long)Mathf.FloorToInt(p.x / lato) << 32) ^ (uint)Mathf.FloorToInt(p.z / lato);
                List<Vector3> l; if (!m.TryGetValue(k, out l)) m[k] = l = new List<Vector3>();
                l.Add(p);
            }
            var righe = new List<KeyValuePair<int, string>>();
            foreach (var kv in m)
            {
                var cen = Vector3.zero; foreach (var p in kv.Value) cen += p; cen /= kv.Value.Count;
                righe.Add(new KeyValuePair<int, string>(kv.Value.Count, "  " + tipo + " x" + kv.Value.Count + " intorno a " + Fmt(cen)));
            }
            righe.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (int i = 0; i < righe.Count && i < 40; i++) sb.AppendLine(righe[i].Value);
            if (righe.Count > 40) sb.AppendLine("  ... (altri " + (righe.Count - 40) + " gruppi)");
        }

        // ------------------------------------------------------------------ area calpestabile

        internal sealed class Area { public List<Vector3> celle = new List<Vector3>(); public List<Vector3> extra = new List<Vector3>(); }

        internal static Area AreaCalpestabile(Vector3 start, float r, float h)
        {
            var area = new Area();
            Bounds all = new Bounds(start, Vector3.zero);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude)) all.Encapsulate(c.bounds);
            int w = Mathf.CeilToInt((all.max.x - all.min.x) / Passo) + 2, d = Mathf.CeilToInt((all.max.z - all.min.z) / Passo) + 2;
            float x0 = all.min.x - Passo, z0 = all.min.z - Passo;
            var visto = new bool[w * d];
            var quota = new float[w * d];
            var coda = new Queue<int>();
            int is_ = Mathf.RoundToInt((start.x - x0) / Passo), iz_ = Mathf.RoundToInt((start.z - z0) / Passo);
            visto[iz_ * w + is_] = true; quota[iz_ * w + is_] = start.y;
            coda.Enqueue(iz_ * w + is_);
            area.celle.Add(start);
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (coda.Count > 0)
            {
                int cur = coda.Dequeue();
                int cx = cur % w, cz = cur / w;
                float cy = quota[cur];
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], nz = cz + dz[k];
                    if (nx < 0 || nz < 0 || nx >= w || nz >= d || visto[nz * w + nx]) continue;
                    float px = x0 + nx * Passo, pz = z0 + nz * Passo;
                    RaycastHit hit;
                    if (!Physics.Raycast(new Vector3(px, cy + 0.45f, pz), Vector3.down, out hit, 0.75f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    float y = hit.point.y;
                    if (y - cy > 0.21f) continue;
                    var p0 = new Vector3(px, y + r + 0.21f, pz);
                    var p1 = new Vector3(px, y + h - r, pz);
                    if (Physics.CheckCapsule(p0, p1, r, ~0, QueryTriggerInteraction.Ignore)) continue;
                    visto[nz * w + nx] = true; quota[nz * w + nx] = y;
                    area.celle.Add(new Vector3(px, y, pz));
                    coda.Enqueue(nz * w + nx);
                }
            }
            // punti casuali dentro ogni cella (fessure fra pezzi non allineati alla griglia): solo dove il suolo di gioco c'e' davvero, per non segnalare i bordi delle pedane
            var rng = new System.Random(17);
            foreach (var c in area.celle)
            {
                var p = new Vector3(c.x + (float)(rng.NextDouble() - 0.5) * Passo, c.y, c.z + (float)(rng.NextDouble() - 0.5) * Passo);
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(p.x, c.y + 0.45f, p.z), Vector3.down, out hit, 0.75f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - c.y) <= 0.1f)
                    area.extra.Add(new Vector3(p.x, hit.point.y, p.z));
            }
            return area;
        }

        /// <summary>Disattiva i collisori di gioco e mette un MeshCollider temporaneo su ogni renderer visibile: i raggi vedono solo cio' che si vede.</summary>
        static List<Component> AggiungiCollisoriVisivi()
        {
            var temp = new List<Component>();
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
                if (!(c is MeshCollider) || true) c.enabled = false;
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!mr.enabled || mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                if (mr.name == "PianoSicurezza") continue; // la rete di sicurezza non deve nascondere i buchi alla diagnosi
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            return temp;
        }

        // ------------------------------------------------------------------ complanari

        sealed class Tri { public Vector3 a, b, c, n; public float d; public int ren; public Bounds bb; public Material mat; }
        sealed class Coppia { public string nomeA, nomeB, matA, matB; public bool matDiversi, visibile; public int triangoli, valutate; public Vector3 pos, normale; public Bounds ext; }

        static string Percorso(Transform t)
        {
            var s = t.name;
            for (int i = 0; i < 2 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        static bool MatOk(Material a, Material b)
        {
            if (dbgMat == "") return true;
            var p = dbgMat.Split('|');
            string na = a != null ? a.name : "", nb = b != null ? b.name : "";
            if (p.Length == 1) return na.Contains(p[0]) || nb.Contains(p[0]);
            return (na.Contains(p[0]) && nb.Contains(p[1])) || (na.Contains(p[1]) && nb.Contains(p[0]));
        }
        static bool MeshDelGeneratore(MeshRenderer r)
        {
            var mf = r.GetComponent<MeshFilter>();
            string path = mf != null && mf.sharedMesh != null ? AssetDatabase.GetAssetPath(mf.sharedMesh) : "";
            return path.StartsWith("Assets/_Game/Generated") || string.IsNullOrEmpty(path);
        }
        static string dbgA, dbgB, dbgMat = ""; static readonly StringBuilder dbg = new StringBuilder();
        /// <summary>Dettaglio (vertici) dei triangoli complanari fra due oggetti, per cercare la causa di una segnalazione.</summary>
        public static string Dettaglio(string a, string b, string mat = "")
        {
            dbgA = a; dbgB = b; dbgMat = mat; dbg.Clear();
            var temp = AggiungiCollisoriVisivi();
            Physics.SyncTransforms();
            try { Complanari(); } finally { foreach (var t in temp) if (t != null) UnityEngine.Object.DestroyImmediate(t); dbgA = null; }
            return dbg.ToString();
        }

        static List<Coppia> Complanari()
        {
            var rend = new List<MeshRenderer>();
            var tris = new List<Tri>();
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!mr.enabled || mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh;
                var vs = m.vertices; var l2w = mr.transform.localToWorldMatrix;
                int ri = rend.Count; rend.Add(mr);
                var mats = mr.sharedMaterials;
                for (int sm = 0; sm < m.subMeshCount; sm++)
                {
                    Material matSm = mats.Length == 0 ? null : mats[Mathf.Min(sm, mats.Length - 1)];
                    var idx = m.GetIndices(sm);
                    for (int i = 0; i + 2 < idx.Length; i += 3)
                    {
                        var a = l2w.MultiplyPoint3x4(vs[idx[i]]); var b = l2w.MultiplyPoint3x4(vs[idx[i + 1]]); var c = l2w.MultiplyPoint3x4(vs[idx[i + 2]]);
                        var cr = Vector3.Cross(b - a, c - a);
                        float len = cr.magnitude;
                        if (len < 2e-5f) continue; // meno di ~1 cm2
                        var n = cr / len;
                        var bb = new Bounds(a, Vector3.zero); bb.Encapsulate(b); bb.Encapsulate(c);
                        tris.Add(new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), ren = ri, bb = bb, mat = matSm });
                    }
                }
            }
            // griglia spaziale 2 m
            const float cell = 2f;
            var grid = new Dictionary<long, List<int>>();
            var grandi = new List<int>();
            for (int i = 0; i < tris.Count; i++)
            {
                var bb = tris[i].bb;
                int x0 = Mathf.FloorToInt((bb.min.x - 0.01f) / cell), x1 = Mathf.FloorToInt((bb.max.x + 0.01f) / cell);
                int y0 = Mathf.FloorToInt((bb.min.y - 0.01f) / cell), y1 = Mathf.FloorToInt((bb.max.y + 0.01f) / cell);
                int z0 = Mathf.FloorToInt((bb.min.z - 0.01f) / cell), z1 = Mathf.FloorToInt((bb.max.z + 0.01f) / cell);
                long cnt = (long)(x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1);
                if (cnt > 3000) { grandi.Add(i); continue; }
                for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) for (int z = z0; z <= z1; z++)
                {
                    long k = ((long)(x + 4096) << 40) ^ ((long)(y + 4096) << 20) ^ (long)(z + 4096);
                    List<int> l; if (!grid.TryGetValue(k, out l)) grid[k] = l = new List<int>();
                    l.Add(i);
                }
            }
            var coppie = new Dictionary<long, Coppia>();
            var provate = new HashSet<long>();
            Action<int, int> prova = (i, j) =>
            {
                var A = tris[i]; var B = tris[j];
                if (A.ren == B.ren && !MeshDelGeneratore(rend[A.ren])) return; // mesh importati (FBX): non dipendono dai generatori
                long pk = (long)Mathf.Min(i, j) * 100000007L + Mathf.Max(i, j);
                if (!provate.Add(pk)) return;
                if (Vector3.Dot(A.n, B.n) < 0.9995f) return;
                // livelli diversi dello stesso LODGroup non sono mai visibili insieme
                var lgA = rend[A.ren].GetComponentInParent<LODGroup>();
                if (lgA != null && A.ren != B.ren && lgA == rend[B.ren].GetComponentInParent<LODGroup>()) return;
                if (Mathf.Abs(A.d - Vector3.Dot(A.n, B.a)) > DistComplanare || Mathf.Abs(A.d - Vector3.Dot(A.n, B.b)) > DistComplanare || Mathf.Abs(A.d - Vector3.Dot(A.n, B.c)) > DistComplanare) return;
                if (!Sovrapposti(A, B)) return;
                if (dbgA != null && dbg.Length < 6000)
                {
                    string na = Percorso(rend[A.ren].transform), nb = Percorso(rend[B.ren].transform);
                    if (((na.Contains(dbgA) && nb.Contains(dbgB)) || (na.Contains(dbgB) && nb.Contains(dbgA))) && MatOk(A.mat, B.mat))
                        dbg.AppendLine(na + " [" + F4(A.a) + F4(A.b) + F4(A.c) + "] vs " + nb + " [" + F4(B.a) + F4(B.b) + F4(B.c) + "] mat " + (A.mat != null ? A.mat.name : "?") + "/" + (B.mat != null ? B.mat.name : "?"));
                }
                int ra = Mathf.Min(A.ren, B.ren), rb = Mathf.Max(A.ren, B.ren);
                long ck = (long)ra * 1000003L + rb;
                Coppia cp;
                if (!coppie.TryGetValue(ck, out cp))
                {
                    var rA = rend[ra]; var rB = rend[rb];
                    cp = new Coppia { nomeA = Percorso(rA.transform), nomeB = Percorso(rB.transform), matA = "", matB = "", pos = (A.a + A.b + A.c) / 3f, normale = A.n };
                    coppie[ck] = cp;
                }
                // il materiale non conta: due facce complanari dello stesso materiale tremolano comunque (UV e lightmap diverse)
                if (A.mat != B.mat) cp.matDiversi = true;
                if (cp.valutate < 40 && !cp.visibile)
                {
                    cp.valutate++;
                    if (Visibile(A, B))
                    {
                        cp.visibile = true;
                        cp.matA = A.mat != null ? A.mat.name : "?"; cp.matB = B.mat != null ? B.mat.name : "?";
                        cp.pos = (A.a + A.b + A.c) / 3f; cp.normale = A.n;
                        var lo = Vector3.Max(A.bb.min, B.bb.min); var hi = Vector3.Min(A.bb.max, B.bb.max);
                        cp.ext = new Bounds((lo + hi) * 0.5f, hi - lo);
                    }
                }
                cp.triangoli++;
            };
            foreach (var kv in grid)
            {
                var l = kv.Value;
                for (int a = 0; a < l.Count; a++)
                    for (int b = a + 1; b < l.Count; b++)
                    {
                        if (!tris[l[a]].bb.Intersects(Espandi(tris[l[b]].bb))) continue;
                        prova(l[a], l[b]);
                    }
            }
            foreach (int g in grandi)
                for (int j = 0; j < tris.Count; j++)
                {
                    if (j == g || !tris[g].bb.Intersects(Espandi(tris[j].bb))) continue;
                    prova(g, j);
                }
            return new List<Coppia>(coppie.Values);
        }

        /// <summary>Vero se il baricentro del triangolo e' raggiungibile dall'esterno: un raggio uscente dalla faccia che incontra per prima
        /// una faccia posteriore parte dall'interno di un volume chiuso (faccia coperta, tipicamente nello spigolo fra due pareti).</summary>
        /// <summary>Posizioni di camera di prova (area calpestabile a 2 m d'altezza): una faccia e' visibile se almeno una vede il punto senza ostacoli.</summary>
        static readonly List<Vector3> camPunti = new List<Vector3>();

        static readonly float[][] Bari = { new[] { 1f/3, 1f/3, 1f/3 }, new[] { .6f, .2f, .2f }, new[] { .2f, .6f, .2f }, new[] { .2f, .2f, .6f }, new[] { .45f, .45f, .1f }, new[] { .1f, .45f, .45f }, new[] { .45f, .1f, .45f } };

        static bool DentroTri(Vector3 p, Tri t)
        {
            var v0 = t.b - t.a; var v1 = t.c - t.a; var v2 = p - t.a;
            float d00 = Vector3.Dot(v0, v0), d01 = Vector3.Dot(v0, v1), d11 = Vector3.Dot(v1, v1), d20 = Vector3.Dot(v2, v0), d21 = Vector3.Dot(v2, v1);
            float den = d00 * d11 - d01 * d01; if (Mathf.Abs(den) < 1e-12f) return false;
            float u = (d11 * d20 - d01 * d21) / den, w = (d00 * d21 - d01 * d20) / den;
            return u >= -0.001f && w >= -0.001f && u + w <= 1.001f;
        }

        /// <summary>Raggio massimo (m) delle camere di prova: l'hub e' lungo oltre 100 m.</summary>
        const float RaggioCamere = 120f;

        /// <summary>Visibile se, in almeno uno dei punti campionati nella zona sovrapposta (non solo il baricentro), una camera di prova nel semispazio davanti vede il punto con segmento libero.</summary>
        static bool Visibile(Tri A, Tri B)
        {
            var punti = new List<Vector3>();
            foreach (var b in Bari)
            {
                var q = A.a * b[0] + A.b * b[1] + A.c * b[2];
                if (DentroTri(q, B)) punti.Add(q);
            }
            var cb = (B.a + B.b + B.c) / 3f; if (DentroTri(cb, A)) punti.Add(cb);
            if (punti.Count == 0) punti.Add((A.a + A.b + A.c) / 3f);
            bool prima = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            bool vista = false;
            foreach (var q in punti)
            {
                var p = q + A.n * 0.004f;
                int provate = 0;
                foreach (var c in camPunti)
                {
                    var dir = c - p;
                    float dist = dir.magnitude;
                    if (dist < 0.05f || dist > RaggioCamere || Vector3.Dot(dir, A.n) <= 0.01f * dist) continue;
                    if (++provate > 120) break;
                    if (!Physics.Linecast(p, c, ~0, QueryTriggerInteraction.Ignore)) { vista = true; break; }
                }
                if (vista) break;
            }
            Physics.queriesHitBackfaces = prima;
            return vista;
        }

        static Bounds Espandi(Bounds b) { b.Expand(0.004f); return b; }

        /// <summary>Test di sovrapposizione fra due triangoli complanari con SAT: l'intersezione deve avere almeno SovrapposizioneMin di profondita' su ogni asse.</summary>
        static bool Sovrapposti(Tri A, Tri B)
        {
            var n = A.n;
            var u = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var v = Vector3.Cross(n, u);
            Vector2 P(Vector3 p) { return new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v)); }
            var ta = new[] { P(A.a), P(A.b), P(A.c) }; var tb = new[] { P(B.a), P(B.b), P(B.c) };
            for (int pass = 0; pass < 2; pass++)
            {
                var t = pass == 0 ? ta : tb;
                for (int e = 0; e < 3; e++)
                {
                    var edge = t[(e + 1) % 3] - t[e];
                    var ax = new Vector2(-edge.y, edge.x);
                    float l = ax.magnitude; if (l < 1e-9f) return false; ax /= l;
                    float minA = 1e9f, maxA = -1e9f, minB = 1e9f, maxB = -1e9f;
                    for (int i = 0; i < 3; i++)
                    {
                        float pa = Vector2.Dot(ta[i], ax), pb = Vector2.Dot(tb[i], ax);
                        minA = Mathf.Min(minA, pa); maxA = Mathf.Max(maxA, pa); minB = Mathf.Min(minB, pb); maxB = Mathf.Max(maxB, pb);
                    }
                    float ov = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
                    if (ov < SovrapposizioneMin) return false;
                }
            }
            return true;
        }
    }
}
