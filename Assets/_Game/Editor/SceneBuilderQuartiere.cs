using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 2: l'hub diventa un grande quartiere dei Quartieri Spagnoli (autunno 1946).
    /// Il layout e' dato come insieme di zone calpestabili (rettangoli con quota) rasterizzate a 0,5 m; le facciate si ricavano
    /// automaticamente dai bordi del raster (moduli riusabili con mesh condivise), le porte e le chiusure sono "feature" che
    /// riservano un modulo sul muro. Arredo e punti di riferimento stanno in SceneBuilderQuartiereArredo.cs.
    /// </summary>
    public static partial class SceneBuilder
    {
        const float Ras = 0.5f;

        sealed class Zona
        {
            public string nome, suolo;
            public float x0, x1, z0, z1, y, y1;
            public bool rampa;
            public int passi;
        }

        sealed class Feat
        {
            public string tipo;           // porta, portone, cancello, cieco, basso
            public Vector3 wp;            // punto sul piano della facciata (y = quota del suolo)
            public Vector3 n;             // normale verso la strada
            public int len;               // lunghezza del modulo
            public int piani = 3;
            public string id, sign, intonaco;
            public int interno;
            public GameObject modulo;
            public float uPorta;          // posizione locale della porta nel modulo
        }

        sealed class Muro
        {
            public Vector3 n;
            public bool ew;               // la normale e' lungo x: il muro corre lungo z
            public float piano, c0, c1, baseY, key;
            public List<Feat> feat = new List<Feat>();
        }

        sealed class Modulo
        {
            public Muro m;
            public float lo, len, depth;  // lo = coordinata minima lungo l'asse del muro
            public Feat f;
            public Vector3 b0, b1;        // ingombro in pianta (x,z) e quota
            public GameObject go;
            public string nome;
            public int col;               // R017: fascia di quota (0..n): sfalsa di pochi mm i profili in pietra dei moduli vicini (niente superfici complanari)
        }

        static readonly List<Zona> zone = new List<Zona>();
        static readonly List<Feat> feats = new List<Feat>();
        static int[] ras;
        static int rasW, rasH;
        static float rasX0, rasZ0;
        static readonly List<Modulo> moduli = new List<Modulo>();
        static int hubAvvisi;
        /// <summary>R006: prefisso dei nomi delle mesh generate dal sistema a zone ("Hub" per il quartiere, "Ric" per la via del flashback).</summary>
        static string pre = "Hub";
        /// <summary>R006: punti (x,z) da tenere liberi dagli oggetti sparsi (postazioni degli enigmi, oggetti nascosti).</summary>
        static readonly List<Vector2> riservati = new List<Vector2>();

        // ------------------------------------------------------------------ layout

        static void Z(string nome, string suolo, float x0, float x1, float z0, float z1, float y)
        {
            zone.Add(new Zona { nome = nome, suolo = suolo, x0 = x0, x1 = x1, z0 = z0, z1 = z1, y = y, y1 = y });
        }

        static void Rampa(string nome, float x0, float x1, float z0, float z1, float y0, float y1, int passi)
        {
            zone.Add(new Zona { nome = nome, suolo = "scala", x0 = x0, x1 = x1, z0 = z0, z1 = z1, y = y0, y1 = y1, rampa = true, passi = passi });
        }

        static void DefinisciZone()
        {
            zone.Clear();
            // dorsale nord-sud in salita: piazzetta della fontanella -> vico -> scalinate -> slargo dell'edicola -> largo della stazione
            Z("P1_Fontanella", "piazza", -10, 10, -44, -28, 0f);
            Z("M1", "strada", -3, 3, -28, -8, 0f);
            Rampa("F1", -3, 3, -8, -5, 0f, 1.2f, 8);
            Z("M2", "strada", -3, 3, -5, 9, 1.2f);
            Z("SlargoAntonietta", "piazza", -9, -3, -4, 0, 1.2f);
            Rampa("F2", -3, 3, 9, 12, 1.2f, 2.4f, 8);
            Z("M3", "strada", -3, 3, 12, 26, 2.4f);
            Rampa("F3", -3, 3, 26, 29, 2.4f, 3.6f, 8);
            Z("P2_Edicola", "piazza", -8, 8, 29, 45, 3.6f);
            Rampa("F4", -3, 3, 45, 48, 3.6f, 4.8f, 8);
            Z("LargoStazione", "piazza", -6, 6, 48, 56, 4.8f);
            // traverse e cortili a ovest
            Z("T1", "strada", -29, -3, -20, -16, 0f);
            Z("W1a", "strada", -25, -21, -16, -6, 0f);
            Rampa("W1f", -25, -21, -6, -3, 0f, 1.2f, 8);
            Z("W1b", "strada", -25, -21, -3, 4, 1.2f);
            Z("W1stub", "strada", -25, -21, -25, -20, 0f);
            Z("C1_Lavatoio", "cortile", -29, -17, -35, -25, 0f);
            Z("S1", "strada", -29, -25, -12, -8, 0f);
            Z("T3", "strada", -29, -3, 4, 8, 1.2f);
            Z("T7", "strada", -29, -3, 14, 18, 2.4f);
            Z("C3stub", "strada", -19, -15, 18, 22, 2.4f);
            Z("C3_Terrazzino", "cortile", -25, -13, 22, 32, 2.4f);
            // traverse, lotto bombardato e vicolo laterale a est
            Z("T2", "strada", 3, 29, -14, -10, 0f);
            Z("Lotto", "macerie", 11, 27, -24, -14, 0f);
            Z("E1a", "strada", 17, 21, -10, -7, 0f);
            Rampa("E1f", 17, 21, -7, -4, 0f, 1.2f, 8);
            Z("E1b", "strada", 17, 21, -4, -1, 1.2f);
            Z("T4", "strada", 3, 29, -1, 3, 1.2f);
            Z("E1c", "strada", 17, 21, 3, 9, 1.2f);
            Rampa("E1g", 17, 21, 9, 12, 1.2f, 2.4f, 8);
            Z("E1d", "strada", 17, 21, 12, 26, 2.4f);
            Z("T5", "strada", 3, 29, 14, 18, 2.4f);
            Z("S2", "strada", 21, 27, 22, 26, 2.4f);
            Rampa("E1h", 17, 21, 26, 29, 2.4f, 3.6f, 8);
            Z("E1e", "strada", 17, 21, 29, 33, 3.6f);
            Z("T6", "strada", 8, 29, 33, 37, 3.6f);
        }

        static readonly List<Vector4> zonePonte = new List<Vector4>(); // x0,x1,z0,z1 dei ponti-arco: niente balconi nei moduli accanto

        static void DefinisciFeature()
        {
            feats.Clear();
            zonePonte.Clear();
            zonePonte.Add(new Vector4(-3, 3, 19, 22));
            zonePonte.Add(new Vector4(-25, -21, -23, -20));
            zonePonte.Add(new Vector4(10, 13, -1, 3));
            zonePonte.Add(new Vector4(-14, -11, 14, 18));
            // luoghi di luoghi.json (porte vere, chiuse finche' "aperto" e' falso)
            Fe("porta", -9, -2, 1, 0, 4, "basso_antonietta", "InsegnaRossa");
            Fe("porta", -29, 6, 1, 0, 4, "casa_capa_e_fierro", "MP_CapaFierro");
            Fe("porta", 29, 1, -1, 0, 4, "sede_partito", "MP_Comitato");
            Fe("porta", 0, 56, 0, -1, 6, "stazione", "MP_Stazione");
            Fe("porta", 6, -44, 0, 1, 6, "vicolo_stracci", "MP_Stracci");
            // chiusure credibili dei vicoli ciechi
            Fe("portone", -29, -18, 1, 0, 4, null, null);   // T1
            Fe("portone", -29, -10, 1, 0, 4, null, null);   // S1
            Fe("cancello", -29, 16, 1, 0, 4, null, null);   // T7
            Fe("portone", 29, 16, -1, 0, 4, null, null);    // T5
            Fe("cancello", 29, 35, -1, 0, 4, null, null);   // T6
            Fe("portone", 27, 24, -1, 0, 4, null, null);    // S2
            Fe("portone", -5.5f, 45, 0, -1, 5, null, null).piani = 4; // chiesetta, nord dello slargo
            // muri ciechi per edicole, lavatoio, terrazzino, manifesti
            Fe("cieco", -8, 37, 1, 0, 6, null, null).piani = 4;   // edicola grande
            Fe("cieco", -7, -28, 0, -1, 6, null, null);            // bancarella, P1
            Fe("cieco", -29, -30, 1, 0, 4, null, null);            // lavatoio, C1
            Fe("cieco", -19, 32, 0, -1, 8, null, null);            // terrazzino, C3
            Fe("cieco", -14, 8, 0, -1, 4, null, null);             // edicola piccola, T3
            Fe("cieco", 12, 14, 0, 1, 4, null, null);              // edicola piccola, T5
            Fe("cieco", 14, -10, 0, -1, 4, null, null);            // bacheca, T2
            // bassi aperti con interno accennato
            Fe("basso", 7, -28, 0, -1, 6, null, null).interno = 2;   // la bottega con la luce calda, in vista dalla piazzetta
            Fe("basso", -12, -16, 0, -1, 6, null, null).interno = 0;
            Fe("basso", -14, 4, 0, 1, 6, null, null).interno = 1;
            Fe("basso", 12, 3, 0, -1, 6, null, null).interno = 2;
            Fe("basso", 8, 18, 0, -1, 6, null, null).interno = 1;
            Fe("basso", -8, 14, 0, 1, 6, null, null).interno = 0;
        }

        static Feat Fe(string tipo, float x, float z, float nx, float nz, int len, string id, string sign)
        {
            var f = new Feat { tipo = tipo, n = new Vector3(nx, 0f, nz), len = len, id = id, sign = sign };
            f.wp = new Vector3(x, 0f, z);
            f.wp.y = GroundY(x + nx * 0.5f, z + nz * 0.5f);
            feats.Add(f);
            return f;
        }

        // ------------------------------------------------------------------ raster

        static void RasterizzaZone()
        {
            float minX = 1e9f, minZ = 1e9f, maxX = -1e9f, maxZ = -1e9f;
            foreach (var z in zone)
            {
                minX = Mathf.Min(minX, z.x0); maxX = Mathf.Max(maxX, z.x1);
                minZ = Mathf.Min(minZ, z.z0); maxZ = Mathf.Max(maxZ, z.z1);
            }
            rasX0 = Mathf.Floor(minX) - 3f; rasZ0 = Mathf.Floor(minZ) - 3f;
            rasW = Mathf.CeilToInt((maxX + 3f - rasX0) / Ras) + 1;
            rasH = Mathf.CeilToInt((maxZ + 3f - rasZ0) / Ras) + 1;
            ras = new int[rasW * rasH];
            for (int i = 0; i < zone.Count; i++)
            {
                var z = zone[i];
                int ix0 = Mathf.RoundToInt((z.x0 - rasX0) / Ras), ix1 = Mathf.RoundToInt((z.x1 - rasX0) / Ras);
                int iz0 = Mathf.RoundToInt((z.z0 - rasZ0) / Ras), iz1 = Mathf.RoundToInt((z.z1 - rasZ0) / Ras);
                for (int iz = iz0; iz < iz1; iz++)
                    for (int ix = ix0; ix < ix1; ix++) ras[iz * rasW + ix] = i + 1;
            }
        }

        static bool Walk(int ix, int iz)
        {
            return ix >= 0 && iz >= 0 && ix < rasW && iz < rasH && ras[iz * rasW + ix] > 0;
        }

        static Zona ZonaDi(int ix, int iz) { return zone[ras[iz * rasW + ix] - 1]; }

        static float YZona(Zona zn, float z)
        {
            if (!zn.rampa) return zn.y;
            float t = Mathf.Clamp01((z - zn.z0) / (zn.z1 - zn.z0));
            int i = Mathf.Min(zn.passi - 1, Mathf.FloorToInt(t * zn.passi));
            return zn.y + (i + 1) * (zn.y1 - zn.y) / zn.passi;
        }

        /// <summary>Quota del suolo in (x,z); fuori dalla mappa 0.</summary>
        static float GroundY(float x, float z) { return GroundY(x, z, true); }
        static float GroundY(float x, float z, bool avvisa)
        {
            if (ras == null) return 0f;
            int ix = Mathf.FloorToInt((x - rasX0) / Ras), iz = Mathf.FloorToInt((z - rasZ0) / Ras);
            if (!Walk(ix, iz))
            {
                if (avvisa) Debug.LogWarning("[SceneBuilder] GroundY fuori zona calpestabile in (" + x.ToString("0.0") + ", " + z.ToString("0.0") + "): quota 0.");
                return 0f;
            }
            return YZona(ZonaDi(ix, iz), z);
        }

        static Vector3 PT(float x, float z, float dy = 0f) { return new Vector3(x, GroundY(x, z) + dy, z); }

        /// <summary>Superficie calpestabile (m2) senza le scalinate? Tutte le zone, scalinate comprese.</summary>
        static float AreaCalpestabile()
        {
            int n = 0;
            for (int i = 0; i < ras.Length; i++) if (ras[i] > 0) n++;
            return n * Ras * Ras;
        }

        // ------------------------------------------------------------------ suolo

        sealed class RettSuolo { public string suolo; public float y, x0, x1, z0, z1; }

        static string MatSuolo(string suolo)
        {
            switch (suolo)
            {
                case "cortile": return "M_PavimentoBattuto";
                case "macerie": return "M_TerraPolverosa";
                case "stazione": return "M_PavimentoPiastreGrandi"; // R014: atrio e banchine della stazione
                case "legno": return "M_AssiConsunte";          // R014: pavimento della carrozza
                case "neve": return "Neve";                     // R015: corte e strada di Modena
                case "cotto": return "M_PavimentoCotto";        // R015: cucina di Derna
                default: return "M_BasoliLavica";
            }
        }

        static void CostruisciSuolo(Transform env)
        {
            var done = new bool[rasW * rasH];
            var rects = new List<RettSuolo>();
            for (int iz = 0; iz < rasH; iz++)
                for (int ix = 0; ix < rasW; ix++)
                {
                    int id = ras[iz * rasW + ix];
                    if (id == 0 || done[iz * rasW + ix]) continue;
                    var zn = zone[id - 1];
                    if (zn.rampa) continue;
                    int w = 1;
                    while (ix + w < rasW && ras[iz * rasW + ix + w] > 0 && !done[iz * rasW + ix + w] && Mathf.Approximately(ZoneKeyY(ix + w, iz), zn.y) && ZoneSuolo(ix + w, iz) == zn.suolo) w++;
                    int h = 1;
                    bool ok = true;
                    while (ok && iz + h < rasH)
                    {
                        for (int k = 0; k < w; k++)
                        {
                            int q = (iz + h) * rasW + ix + k;
                            if (done[q] || ras[q] == 0 || !Mathf.Approximately(ZoneKeyY(ix + k, iz + h), zn.y) || ZoneSuolo(ix + k, iz + h) != zn.suolo) { ok = false; break; }
                        }
                        if (ok) h++;
                    }
                    for (int j = 0; j < h; j++) for (int k = 0; k < w; k++) done[(iz + j) * rasW + ix + k] = true;
                    // il suolo prosegue per 30 cm sotto i muri (zoccolo delle facciate, battenti delle porte): niente fessure e appoggio sempre trovato
                    const float e = 0.3f;
                    bool sL = true, sR = true, sB = true, sT = true;
                    for (int j = 0; j < h; j++) { if (Walk(ix - 1, iz + j)) sL = false; if (Walk(ix + w, iz + j)) sR = false; }
                    for (int k = 0; k < w; k++) { if (Walk(ix + k, iz - 1)) sB = false; if (Walk(ix + k, iz + h)) sT = false; }
                    rects.Add(new RettSuolo { suolo = zn.suolo, y = zn.y, x0 = rasX0 + ix * Ras - (sL ? e : 0f), x1 = rasX0 + (ix + w) * Ras + (sR ? e : 0f),
                        z0 = rasZ0 + iz * Ras - (sB ? e : 0f), z1 = rasZ0 + (iz + h) * Ras + (sT ? e : 0f) });
                }

            // un mesh per tipo di suolo e per riquadro di 32 m
            var gruppi = new Dictionary<string, List<RettSuolo>>();
            foreach (var rc in rects)
            {
                int cx = Mathf.FloorToInt((rc.x0 + rc.x1) * 0.5f / 32f), cz = Mathf.FloorToInt((rc.z0 + rc.z1) * 0.5f / 32f);
                string key = rc.suolo + "_q" + Mathf.RoundToInt(rc.y * 10f) + "_" + (cx + 5) + "_" + (cz + 5);
                List<RettSuolo> l;
                if (!gruppi.TryGetValue(key, out l)) gruppi[key] = l = new List<RettSuolo>();
                l.Add(rc);
            }
            foreach (var kv in gruppi)
            {
                string mat = MatSuolo(kv.Value[0].suolo);
                var kb = new KitBuilder(mat);
                foreach (var rc in kv.Value) kb.Box(new Vector3(rc.x0, rc.y - 0.5f, rc.z0), new Vector3(rc.x1, rc.y, rc.z1), 0, 1.6f, KitBuilder.NY);
                var go = MeshObject(env, "Selciato_" + kv.Key, kb.Build(pre + "_Suolo_" + kv.Key), new[] { mat }, false);
                foreach (var rc in kv.Value)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = new Vector3((rc.x0 + rc.x1) * 0.5f, rc.y - 0.25f, (rc.z0 + rc.z1) * 0.5f);
                    bc.size = new Vector3(rc.x1 - rc.x0, 0.5f, rc.z1 - rc.z0);
                }
            }

            // scalinate: ogni gradino e' un blocco con il suo collider
            foreach (var zn in zone)
            {
                if (!zn.rampa) continue;
                var kb = new KitBuilder("M_ArenariaBlocchi");
                float r = (zn.y1 - zn.y) / zn.passi, t = (zn.z1 - zn.z0) / zn.passi;
                for (int i = 0; i < zn.passi; i++)
                    kb.Box(new Vector3(zn.x0, zn.y - 0.5f, zn.z0 + i * t - (i == 0 ? KitBuilder.Sporgenza : 0f)), new Vector3(zn.x1, zn.y + (i + 1) * r, zn.z0 + (i + 1) * t), 0, 1.5f, KitBuilder.NY | KitBuilder.PZ); // R017: il primo alzata sporge di 3,5 mm dalle testate dei moduli (niente facce complanari)
                var go = MeshObject(env, "Selciato_Scalinata_" + zn.nome, kb.Build(pre + "_Scala_" + zn.nome), new[] { "M_ArenariaBlocchi" }, false);
                for (int i = 0; i < zn.passi; i++)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    float top = zn.y + (i + 1) * r;
                    bc.center = new Vector3((zn.x0 + zn.x1) * 0.5f, (zn.y - 0.5f + top) * 0.5f, zn.z0 + (i + 0.5f) * t);
                    bc.size = new Vector3(zn.x1 - zn.x0, top - (zn.y - 0.5f), t);
                }
            }

            // terra di fondo: sotto i blocchi di case non visibili
            Ground("Selciato_Fondo", env, new Vector3(rasX0, -1.3f, rasZ0), new Vector3(rasX0 + rasW * Ras, -0.6f, rasZ0 + rasH * Ras), "M_TerraPolverosa", 4f, false);
        }

        // R006: blocchi pieni all'interno degli isolati (oltre i 3,5 m dai muri, dove non arrivano i corpi delle facciate): terrazzi di copertura.
        // Chiudono i vuoti visti dall'alto e dalle scalinate e fanno da occluder per l'occlusion culling.
        static void CostruisciTetti(Transform env)
        {
            const int R = 24; // 12 m
            var altezza = new float[rasW * rasH];
            for (int iz = 0; iz < rasH; iz++)
                for (int ix = 0; ix < rasW; ix++)
                {
                    if (ras[iz * rasW + ix] > 0) continue;
                    float best = 1e9f; float quota = 0f;
                    for (int dz = -R; dz <= R; dz++)
                        for (int dx = -R; dx <= R; dx++)
                        {
                            int jx = ix + dx, jz = iz + dz;
                            if (!Walk(jx, jz)) continue;
                            float d = Mathf.Sqrt(dx * dx + dz * dz) * Ras;
                            if (d < best) { best = d; var zn = ZonaDi(jx, jz); quota = zn.rampa ? Mathf.Min(zn.y, zn.y1) : zn.y; }
                        }
                    // solo celle piu' lontane di 3,5 m da qualsiasi spazio calpestabile (e non fuori dal rettangolo delle zone)
                    if (best >= 3.49f && best < 1e8f) altezza[iz * rasW + ix] = quota + 8.2f;
                    else if (best >= 1e8f) altezza[iz * rasW + ix] = 0f;
                }
            var done = new bool[rasW * rasH];
            var gruppi = new Dictionary<string, List<Vector4>>(); // x0,x1,z0,z1 + quota in lista parallela
            var quote = new Dictionary<string, List<float>>();
            for (int iz = 0; iz < rasH; iz++)
                for (int ix = 0; ix < rasW; ix++)
                {
                    float y = altezza[iz * rasW + ix];
                    if (y <= 0f || done[iz * rasW + ix]) continue;
                    int w = 1;
                    while (ix + w < rasW && !done[iz * rasW + ix + w] && Mathf.Approximately(altezza[iz * rasW + ix + w], y)) w++;
                    int h = 1; bool ok = true;
                    while (ok && iz + h < rasH)
                    {
                        for (int k = 0; k < w; k++) if (done[(iz + h) * rasW + ix + k] || !Mathf.Approximately(altezza[(iz + h) * rasW + ix + k], y)) { ok = false; break; }
                        if (ok) h++;
                    }
                    for (int j = 0; j < h; j++) for (int k = 0; k < w; k++) done[(iz + j) * rasW + ix + k] = true;
                    float x0 = rasX0 + ix * Ras, x1 = rasX0 + (ix + w) * Ras, z0 = rasZ0 + iz * Ras, z1 = rasZ0 + (iz + h) * Ras;
                    string key = Mathf.FloorToInt((x0 + x1) * 0.25f / 16f) + "_" + Mathf.FloorToInt((z0 + z1) * 0.25f / 16f);
                    if (!gruppi.ContainsKey(key)) { gruppi[key] = new List<Vector4>(); quote[key] = new List<float>(); }
                    gruppi[key].Add(new Vector4(x0, x1, z0, z1)); quote[key].Add(y);
                }
            foreach (var kv in gruppi)
            {
                var kb = new KitBuilder("M_CoppiArgilla", "M_IntonacoRustico05");
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var q = kv.Value[i]; float y = quote[kv.Key][i];
                    kb.Box(new Vector3(q.x, y - 0.1f, q.z), new Vector3(q.y, y, q.w), 0, 1.5f, KitBuilder.NY | KitBuilder.NX | KitBuilder.PX | KitBuilder.NZ | KitBuilder.PZ);
                }
                string nome = "Casa_Tetto_" + kv.Key.Replace('-', 'm');
                var go = MeshObject(env, nome, kb.Build(pre + "_Tetti_" + kv.Key.Replace('-', 'm')), new[] { "M_CoppiArgilla", "M_IntonacoRustico05" }, false);
                // collider unico per riquadro: serve alla camera (non puo' entrare) e a nessun altro
                foreach (var q in kv.Value)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = new Vector3((q.x + q.y) * 0.5f, 5f, (q.z + q.w) * 0.5f);
                    bc.size = new Vector3(q.y - q.x, 11f, q.w - q.z);
                }
            }
        }

        static float ZoneKeyY(int ix, int iz) { var zn = ZonaDi(ix, iz); return zn.rampa ? -999f : zn.y; }
        static string ZoneSuolo(int ix, int iz) { return ZonaDi(ix, iz).suolo; }

        // ------------------------------------------------------------------ muri e facciate

        static List<Muro> EstraiMuri()
        {
            var res = new List<Muro>();
            // muri che corrono lungo z (normale +-x)
            for (int dir = 0; dir < 2; dir++)
            {
                int solid = dir == 0 ? -1 : 1;                // cella solida a sinistra (muro con normale +x) o a destra (normale -x)
                for (int ix = 0; ix < rasW; ix++)
                {
                    Muro cur = null;
                    for (int iz = 0; iz <= rasH; iz++)
                    {
                        bool edge = iz < rasH && Walk(ix, iz) && !Walk(ix + solid, iz);
                        float key = 0f;
                        if (edge) { var zn = ZonaDi(ix, iz); key = zn.rampa ? 1000f + zn.y : zn.y; }
                        if (cur != null && (!edge || !Mathf.Approximately(cur.key, key))) { cur.c1 = rasZ0 + iz * Ras; res.Add(cur); cur = null; }
                        if (edge && cur == null)
                        {
                            var zn = ZonaDi(ix, iz);
                            cur = new Muro { ew = true, n = new Vector3(-solid, 0f, 0f), key = key, c0 = rasZ0 + iz * Ras,
                                piano = rasX0 + (solid < 0 ? ix : ix + 1) * Ras, baseY = zn.rampa ? Mathf.Min(zn.y, zn.y1) : zn.y };
                        }
                    }
                }
            }
            // muri che corrono lungo x (normale +-z)
            for (int dir = 0; dir < 2; dir++)
            {
                int solid = dir == 0 ? -1 : 1;
                for (int iz = 0; iz < rasH; iz++)
                {
                    Muro cur = null;
                    for (int ix = 0; ix <= rasW; ix++)
                    {
                        bool edge = ix < rasW && Walk(ix, iz) && !Walk(ix, iz + solid);
                        float key = 0f;
                        if (edge) { var zn = ZonaDi(ix, iz); key = zn.rampa ? 1000f + zn.y : zn.y; }
                        if (cur != null && (!edge || !Mathf.Approximately(cur.key, key))) { cur.c1 = rasX0 + ix * Ras; res.Add(cur); cur = null; }
                        if (edge && cur == null)
                        {
                            var zn = ZonaDi(ix, iz);
                            cur = new Muro { ew = false, n = new Vector3(0f, 0f, -solid), key = key, c0 = rasX0 + ix * Ras,
                                piano = rasZ0 + (solid < 0 ? iz : iz + 1) * Ras, baseY = zn.rampa ? Mathf.Min(zn.y, zn.y1) : zn.y };
                        }
                    }
                }
            }
            // abbina le feature ai muri
            foreach (var f in feats)
            {
                Muro best = null;
                foreach (var m in res)
                {
                    if (Vector3.Dot(m.n, f.n) < 0.99f) continue;
                    float pl = m.ew ? f.wp.x : f.wp.z, co = m.ew ? f.wp.z : f.wp.x;
                    if (Mathf.Abs(pl - m.piano) > 0.01f || co < m.c0 - 0.01f || co > m.c1 + 0.01f) continue;
                    best = m; break;
                }
                if (best == null) { Debug.LogWarning("[Quartiere] Feature senza muro: " + f.tipo + " " + f.id + " @" + f.wp); hubAvvisi++; continue; }
                best.feat.Add(f);
            }
            return res;
        }

        static int HashStab(string s)
        {
            unchecked { int h = 17; foreach (char c in s) h = h * 31 + c; return h & 0x7fffffff; }
        }

        // distanza (m) dal muro al primo spazio calpestabile dietro di esso, minima sul tratto [lo,hi]; 12 se non ce n'e'
        static float DistDietro(Muro m, float lo, float hi)
        {
            float min = 12f;
            for (float c = lo + 0.25f; c < hi; c += Ras)
            {
                float d = 12f;
                for (float s = 0.25f; s < 12f; s += Ras)
                {
                    float x = m.ew ? m.piano - m.n.x * s : c;
                    float z = m.ew ? c : m.piano - m.n.z * s;
                    if (Walk(Mathf.FloorToInt((x - rasX0) / Ras), Mathf.FloorToInt((z - rasZ0) / Ras))) { d = s; break; }
                }
                min = Mathf.Min(min, d);
            }
            return min;
        }

        static readonly string[] Intonaci = { "Tufo", "Intonaco", "TufoScuro", "IntonacoOcra", "IntonacoRosa", "IntonacoVerde", "IntonacoAzzurro", "IntonacoGrigio", "IntonacoOcra", "Tufo" };

        static void PianoModulo(Modulo md)
        {
            var m = md.m;
            float p0 = m.piano, p1 = m.piano - (m.ew ? m.n.x : m.n.z) * md.depth;
            float pmin = Mathf.Min(p0, p1), pmax = Mathf.Max(p0, p1);
            if (m.ew) { md.b0 = new Vector3(pmin, m.baseY, md.lo); md.b1 = new Vector3(pmax, m.baseY + 9f, md.lo + md.len); }
            else { md.b0 = new Vector3(md.lo, m.baseY, pmin); md.b1 = new Vector3(md.lo + md.len, m.baseY + 9f, pmax); }
        }

        static void AggiungiModuli(Muro m, System.Random r)
        {
            float L = m.c1 - m.c0;
            if (L < 0.05f) return;
            int sa = (m.ew ? Vector3.Cross(m.n, Vector3.up).z : Vector3.Cross(m.n, Vector3.up).x) > 0f ? 1 : -1;
            // feature ordinate lungo il muro
            var fl = new List<KeyValuePair<float, Feat>>();
            foreach (var f in m.feat)
            {
                float co = m.ew ? f.wp.z : f.wp.x;
                float s = sa > 0 ? co - m.c0 : m.c1 - co;
                fl.Add(new KeyValuePair<float, Feat>(s, f));
            }
            fl.Sort((p, q) => p.Key.CompareTo(q.Key));
            float cur = 0f;
            foreach (var kv in fl)
            {
                float f0 = kv.Key - kv.Value.len * 0.5f, f1 = f0 + kv.Value.len;
                if (f0 < cur - 0.01f || f1 > L + 0.01f)
                {
                    Debug.LogWarning("[Quartiere] Feature " + kv.Value.tipo + " " + kv.Value.id + kv.Value.sign + " @" + kv.Value.wp + " non entra nel muro (" + f0 + ".." + f1 + " su " + L + ")");
                    hubAvvisi++;
                    continue;
                }
                Gap(m, cur, f0, sa, r);
                AddMod(m, f0, kv.Value.len, sa, kv.Value);
                cur = f1;
            }
            Gap(m, cur, L, sa, r);
        }

        static void Gap(Muro m, float s0, float s1, int sa, System.Random r)
        {
            float g = s1 - s0;
            if (g < 0.05f) return;
            float s = s0;
            // R017: il tratto si copre per intero (prima gli arrotondamenti lasciavano varchi di 0,5-1 m nei muri: si vedeva il vuoto).
            // Il resto frazionario va all'ultimo modulo.
            int gi = Mathf.FloorToInt(g + 0.01f);
            float frac = g - gi;
            if (gi == 0) { AddMod(m, s, g, sa, null); return; }
            while (gi > 0)
            {
                int len;
                if (gi <= 8) len = gi;
                else
                {
                    var opz = new List<int>();
                    for (int c = 4; c <= 8; c++) if (gi - c == 0 || gi - c >= 4) opz.Add(c);
                    len = opz[r.Next(opz.Count)];
                }
                gi -= len;
                float L = len + (gi == 0 ? frac : 0f);
                AddMod(m, s, L, sa, null);
                s += L;
            }
        }

        static void AddMod(Muro m, float s, float len, int sa, Feat f)
        {
            var md = new Modulo { m = m, len = len, f = f };
            md.lo = sa > 0 ? m.c0 + s : m.c1 - s - len;
            float d = DistDietro(m, md.lo, md.lo + len);
            md.depth = Mathf.Clamp(Mathf.Floor(d * 0.5f), 1f, 3f);
            PianoModulo(md);
            moduli.Add(md);
        }

        // i muri con normale +-x cedono il passo a quelli con normale +-z agli spigoli: accorcia le estremita' sovrapposte
        static void AccorciaMuro(Muro m)
        {
            for (int guard = 0; guard < 8; guard++)
            {
                bool cambiato = false;
                float p1 = m.piano - m.n.x * 3f;
                float xmin = Mathf.Min(m.piano, p1), xmax = Mathf.Max(m.piano, p1);
                foreach (var o in moduli)
                {
                    if (o.m.ew) continue;
                    if (Mathf.Abs(o.m.baseY - m.baseY) > 30f) continue;
                    if (o.b1.x <= xmin + 0.01f || o.b0.x >= xmax - 0.01f) continue;
                    float zmin = o.b0.z, zmax = o.b1.z;
                    if (zmax <= m.c0 + 0.01f || zmin >= m.c1 - 0.01f) continue;
                    if (zmin <= m.c0 + 0.01f) { m.c0 = zmax; cambiato = true; }
                    else if (zmax >= m.c1 - 0.01f) { m.c1 = zmin; cambiato = true; }
                    if (cambiato) break;
                }
                if (!cambiato || m.c1 - m.c0 < 1f) break;
            }
        }

        static void CostruisciFacciate(Transform env, Transform porte, System.Random r)
        {
            moduli.Clear();
            var muri = EstraiMuri();
            foreach (var m in muri) if (!m.ew) AggiungiModuli(m, r);
            foreach (var m in muri)
            {
                if (!m.ew) continue;
                AccorciaMuro(m);
                // le feature fuori dal tratto accorciato vengono scartate dal controllo in AggiungiModuli
                AggiungiModuli(m, r);
            }
            // controllo di sovrapposizione tra i corpi (stesso livello)
            for (int i = 0; i < moduli.Count; i++)
                for (int j = i + 1; j < moduli.Count; j++)
                {
                    var a = moduli[i]; var b = moduli[j];
                    if (Mathf.Abs(a.m.baseY - b.m.baseY) > 8f) continue;
                    float ox = Mathf.Min(a.b1.x, b.b1.x) - Mathf.Max(a.b0.x, b.b0.x);
                    float oz = Mathf.Min(a.b1.z, b.b1.z) - Mathf.Max(a.b0.z, b.b0.z);
                    if (ox > 0.05f && oz > 0.05f)
                    {
                        hubAvvisi++;
                        Debug.LogWarning("[Quartiere] Corpi sovrapposti (" + ox.ToString("0.0") + " x " + oz.ToString("0.0") + ") @" + a.b0 + " / " + b.b0);
                    }
                }
            AssegnaFasceQuota();
            int idx = 0;
            foreach (var md in moduli) Istanzia(env, porte, md, r, idx++);
        }

        /// <summary>R017: passo (m) dello sfalsamento verticale tra moduli vicini (12 mm: regge il depth a 24 bit di WebGL).</summary>
        const float PassoQuotaModulo = PassoFascia;

        /// <summary>
        /// R017: i profili in pietra (zoccolo, fasce, cornicioni) di moduli adiacenti, in linea o a spigolo, si sovrappongono per pochi centimetri
        /// e le loro facce superiori sono alla stessa quota (sfarfallio). Ogni modulo prende una fascia di quota diversa da quella di tutti i
        /// moduli vicini (colorazione a grafo): i profili orizzontali del modulo si alzano di fascia * 12 mm (mesh distinta per fascia).
        /// </summary>
        static void AssegnaFasceQuota()
        {
            for (int i = 0; i < moduli.Count; i++)
            {
                var a = moduli[i];
                var usati = new HashSet<int>();
                for (int j = 0; j < i; j++)
                {
                    var b = moduli[j];
                    if (Mathf.Abs(a.m.baseY - b.m.baseY) > 12f) continue;
                    const float m = 0.7f; // i profili sporgono fino a 30 cm; margine abbondante
                    if (a.b1.x + m <= b.b0.x || b.b1.x + m <= a.b0.x || a.b1.z + m <= b.b0.z || b.b1.z + m <= a.b0.z) continue;
                    usati.Add(b.col);
                }
                int k = 0;
                while (usati.Contains(k)) k++;
                a.col = k;
            }
        }

        static Vector3 PuntoModulo(Modulo md)
        {
            var m = md.m;
            int sa = (m.ew ? Vector3.Cross(m.n, Vector3.up).z : Vector3.Cross(m.n, Vector3.up).x) > 0f ? 1 : -1;
            float start = sa > 0 ? md.lo : md.lo + md.len;
            return m.ew ? new Vector3(m.piano, m.baseY, start) : new Vector3(start, m.baseY, m.piano);
        }

        static bool VicinoAPonte(Modulo md)
        {
            foreach (var p in zonePonte)
            {
                float ox = Mathf.Min(md.b1.x, p.y + 1.5f) - Mathf.Max(md.b0.x, p.x - 1.5f);
                float oz = Mathf.Min(md.b1.z, p.w + 1.5f) - Mathf.Max(md.b0.z, p.z - 1.5f);
                if (ox > 0f && oz > 0f) return true;
            }
            return false;
        }

        static void Istanzia(Transform env, Transform porte, Modulo md, System.Random r, int idx)
        {
            float len = md.len; int depth = Mathf.RoundToInt(md.depth);
            string lenKey = len.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            var f = md.f;
            int floors = f != null ? f.piani : (r.NextDouble() < 0.5 ? 3 : 4);
            string plaster = f != null && f.intonaco != null ? f.intonaco : Intonaci[r.Next(Intonaci.Length)];
            double roll = r.NextDouble();
            var opt = new FacadeOpt();
            var doorU = new List<float>();
            string v;
            if (f != null && f.tipo == "porta") { v = "P_" + f.id; doorU.Add(len * 0.5f); f.uPorta = len * 0.5f; }
            else if (f != null && f.tipo == "portone") { v = "Portone"; opt.doorKind = 3; opt.cieco = true; doorU.Add(len * 0.5f); }
            else if (f != null && f.tipo == "cancello") { v = depth >= 3 ? "Cancello" : "Portone"; opt.doorKind = depth >= 3 ? 4 : 3; opt.cieco = true; doorU.Add(len * 0.5f); }
            else if (f != null && f.tipo == "cieco") { v = "Z"; opt.cieco = true; }
            else if (f != null && f.tipo == "basso" && depth >= 3 && len >= 5) { v = "D" + f.interno; opt.doorKind = 2; opt.interior = f.interno; doorU.Add(len * 0.5f); }
            else if (len >= 5 && depth >= 3 && roll < 0.14) { int it = r.Next(3); v = "D" + it; opt.doorKind = 2; opt.interior = it; doorU.Add(len * 0.5f); }
            else if (len >= 4 && roll < 0.34) { v = "C"; opt.doorKind = 1; doorU.Add(len * 0.5f); }
            else if (roll < 0.62 && !VicinoAPonte(md)) { v = "B"; opt.balconyMask = 2; }
            else v = "A";
            if (f != null && f.tipo == "porta") { opt.balconyMask = 0; }
            opt.col = md.col;
            opt.key = "Fac_L" + lenKey + "_F" + floors + "_D" + depth + "_" + v + (md.col > 0 ? "_c" + md.col : "");
            opt.seed = HashStab(opt.key);
            string nome = "Casa_" + (md.m.ew ? (md.m.n.x > 0 ? "E" : "O") : (md.m.n.z > 0 ? "N" : "S")) + "_" + idx;
            var fr = new WallFrame(Vector3.zero, Vector3.right, Vector3.back);
            var go = Facade(env, nome, fr, len, depth, floors, plaster, null, 2.6f, doorU, opt);
            var origin = PuntoModulo(md);
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(-md.m.n, Vector3.up));
            md.go = go; md.nome = nome;
            if (f != null)
            {
                f.modulo = go;
                if (f.tipo == "porta")
                {
                    var wp = origin + (md.m.ew ? new Vector3(0f, 0f, 1f) : new Vector3(1f, 0f, 0f)) * 0f;
                    wp = f.wp; wp.y = origin.y;
                    var d = MakeDoor(porte, env, f.id, wp, f.n, f.sign);
                    if (f.id == "basso_antonietta") { d.targetScene = "20_Int_BassoAntonietta"; d.targetSpawn = "ingresso"; }
                }
            }
            // luce calda dentro il basso aperto: il nome e' raccolto da LookBuilder (luce baked puntiforme)
            if (opt.doorKind == 2)
            {
                var l = new GameObject(f != null && f.interno == 2 ? "LuceBottega" : "LuceBasso");
                l.transform.SetParent(go.transform, false);
                l.transform.localPosition = new Vector3(len * 0.5f, 1.9f, -1.3f); // locale: fronte verso -z, dentro il muro = +z... vedi sotto
                l.transform.localPosition = new Vector3(len * 0.5f, 1.9f, 1.4f);
            }
        }
    }
}
