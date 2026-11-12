using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Core;
using TrenoBambini.Puzzles;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 3: verifica di raggiungibilita' senza Play. Riempie a griglia (passo 25 cm) lo spazio percorribile dal punto di partenza
    /// con una capsula come quella di Amerigo (adulto nel basso e nell'hub, bambino nel flashback; gradini fino a 20 cm) e controlla che porte,
    /// oggetti raccoglibili, uscite, postazioni degli enigmi e oggetti nascosti (flashback_scarpe.json) siano raggiungibili.
    /// Menu: Treno/Verifica/Raggiungibilita'. Rapporto: Docs/richieste/media/R006/raggiungibilita.txt
    /// </summary>
    public static class VerificaRaggiungibilita
    {
        const string ReportPath = "Docs/richieste/media/R006/raggiungibilita.txt";
        const float Passo = 0.25f;

        [MenuItem("Treno/Verifica/Raggiungibilita'")]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int n = Esegui();
            Debug.Log("[Verifica] Raggiungibilita': " + n + " problemi. Rapporto: " + ReportPath);
        }

        public static int Esegui()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Verifica raggiungibilita' - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            int problemi = 0;
            problemi += Scena(sb, "20_Int_BassoAntonietta", "ingresso", 0.28f, 1.75f);
            problemi += Scena(sb, "30_FB_Scarpe", "inizio", 0.2f, 1.0f);
            problemi += Scena(sb, "40_FB_Stracci", "inizio", 0.2f, 1.0f);
            problemi += Scena(sb, "50_FB_Volantino", "inizio", 0.2f, 1.0f);
            problemi += Scena(sb, "60_FB_Stazione", "inizio", 0.2f, 1.0f);
            problemi += Scena(sb, "61_FB_Vagone", "inizio", 0.2f, 1.0f);
            problemi += Scena(sb, "10_Hub_Vicolo", "inizio", 0.28f, 1.75f);
            sb.AppendLine();
            sb.AppendLine("TOTALE problemi: " + problemi);
            string full = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[Verifica] " + sb);
            return problemi;
        }

        sealed class Bersaglio { public string nome; public Vector3 pos; public bool punto; public Bounds box; }

        static int Scena(StringBuilder sb, string nome, string spawnId, float r, float h)
        {
            var sc = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + nome + ".unity", OpenSceneMode.Single);
            // il giocatore presente in scena non deve ostacolare la verifica
            foreach (var pl in GameObject.FindGameObjectsWithTag("Player")) pl.SetActive(false);
            Physics.SyncTransforms();
            SpawnPoint sp = null;
            foreach (var s in Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include)) if (s.id == spawnId) sp = s;
            sb.AppendLine();
            if (sp == null) { sb.AppendLine("== " + nome + ": spawn '" + spawnId + "' mancante =="); return 1; }

            // limiti dalla geometria
            Bounds all = new Bounds(sp.transform.position, Vector3.zero);
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude)) all.Encapsulate(c.bounds);
            int w = Mathf.CeilToInt((all.max.x - all.min.x) / Passo) + 2, d = Mathf.CeilToInt((all.max.z - all.min.z) / Passo) + 2;
            float x0 = all.min.x - Passo, z0 = all.min.z - Passo;
            var visto = new bool[w * d];
            var quota = new float[w * d];
            var coda = new Queue<int>();
            int is_ = Mathf.RoundToInt((sp.transform.position.x - x0) / Passo), iz_ = Mathf.RoundToInt((sp.transform.position.z - z0) / Passo);
            visto[iz_ * w + is_] = true; quota[iz_ * w + is_] = sp.transform.position.y;
            coda.Enqueue(iz_ * w + is_);
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            int n = 1;
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
                    var p0 = new Vector3(px, y + r + 0.21f, pz); // come stepOffset del CharacterController: i dislivelli sotto 20 cm si scavalcano
                    var p1 = new Vector3(px, y + h - r, pz);
                    if (Physics.CheckCapsule(p0, p1, r, ~0, QueryTriggerInteraction.Ignore)) continue;
                    visto[nz * w + nx] = true; quota[nz * w + nx] = y; n++;
                    coda.Enqueue(nz * w + nx);
                }
            }

            if (nome == "20_Int_BassoAntonietta")
            {
                sb.AppendLine("Mappa (x da " + x0.ToString("0.0") + ", z in alto = nord, ogni carattere 25 cm; '.' = percorribile):");
                for (int iz = d - 1; iz >= 0; iz -= 2)
                {
                    var line = new StringBuilder();
                    for (int ix = 0; ix < w; ix++) line.Append(visto[iz * w + ix] ? '.' : ' ');
                    sb.AppendLine(line.ToString());
                }
            }
            var bers = new List<Bersaglio>();
            foreach (var dr in Object.FindObjectsByType<Door>(FindObjectsInactive.Exclude)) { var c = dr.GetComponent<Collider>(); if (c != null) bers.Add(new Bersaglio { nome = "Porta " + dr.name, box = c.bounds }); }
            foreach (var ip in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude)) { var c = ip.GetComponent<Collider>(); if (c != null) bers.Add(new Bersaglio { nome = "Oggetto " + ip.oggettoId, box = c.bounds }); }
            foreach (var ex in Object.FindObjectsByType<EscapeExit>(FindObjectsInactive.Exclude)) { var c = ex.GetComponent<Collider>(); if (c != null) bers.Add(new Bersaglio { nome = "Uscita", box = c.bounds }); }
            foreach (var s in Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include)) bers.Add(new Bersaglio { nome = "Spawn " + s.id, pos = s.transform.position, punto = true });
            if (nome == "30_FB_Scarpe" || nome == "40_FB_Stracci" || nome == "50_FB_Volantino" || nome == "60_FB_Stazione" || nome == "61_FB_Vagone")
            {
                var json = Resources.Load<TextAsset>(nome == "30_FB_Scarpe" ? "Data/flashback_scarpe" : nome == "40_FB_Stracci" ? "Data/flashback_stracci" : nome == "50_FB_Volantino" ? "Data/flashback_volantino" : "Data/flashback_cappotto");
                if (json != null)
                {
                    var fb = JsonUtility.FromJson<TrenoBambini.Data.FlashbackDef>(json.text);
                    foreach (var e in fb.enigmi)
                    {
                        if (!string.IsNullOrEmpty(e.scena) && e.scena != nome) continue; // R014: ricordo su due scene
                        bers.Add(new Bersaglio { nome = "Enigma " + e.id, pos = new Vector3(e.x, e.y, e.z), punto = true });
                        if (e.oggetti != null) foreach (var o in e.oggetti) bers.Add(new Bersaglio { nome = "Nascosto " + o.id, pos = new Vector3(o.x, o.y, o.z), punto = true });
                    }
                }
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)) if (t.name.StartsWith("Passante_")) bers.Add(new Bersaglio { nome = t.name, pos = t.position, punto = true });
            }

            int male = 0;
            foreach (var b in bers)
            {
                float best = 1e9f;
                for (int iz = 0; iz < d; iz++)
                    for (int ix = 0; ix < w; ix++)
                    {
                        if (!visto[iz * w + ix]) continue;
                        var q = new Vector3(x0 + ix * Passo, quota[iz * w + ix], z0 + iz * Passo);
                        float dist;
                        if (b.punto) dist = new Vector2(q.x - b.pos.x, q.z - b.pos.z).magnitude + Mathf.Abs(q.y - b.pos.y) * 2f;
                        else { var cp = b.box.ClosestPoint(new Vector3(q.x, Mathf.Clamp(q.y + 0.9f, b.box.min.y, b.box.max.y), q.z)); dist = new Vector2(q.x - cp.x, q.z - cp.z).magnitude; }
                        if (dist < best) best = dist;
                    }
                float soglia = b.punto ? 0.5f : 1.2f;
                bool ok = best <= soglia;
                if (!ok) male++;
                sb.AppendLine("  " + (ok ? "ok   " : "NO   ") + b.nome + " (distanza dal percorribile " + best.ToString("0.00") + " m)");
            }
            sb.AppendLine("== " + nome + ": " + n + " celle percorribili (" + (n * Passo * Passo).ToString("0") + " m2), " + male + " problemi ==");
            return male;
        }
    }
}
