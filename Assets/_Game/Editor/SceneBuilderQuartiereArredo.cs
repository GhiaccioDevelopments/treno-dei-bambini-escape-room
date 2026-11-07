using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 2: oggetti di scena, punti di riferimento (campanile, grande arco, edicola illuminata, bottega con luce calda),
    /// archi, scale esterne, cortili e panni stesi del quartiere. Tutto generato da codice (mesh del kit, nessun asset esterno nuovo).
    /// </summary>
    public static partial class SceneBuilder
    {
        static readonly Dictionary<string, Mesh> propMesh = new Dictionary<string, Mesh>();

        // punti d'interesse per la richiesta R011 (segreti): nome, posizione, descrizione
        sealed class PuntoSegreto { public string nome, descr; public Vector3 pos; }
        static readonly List<PuntoSegreto> segreti = new List<PuntoSegreto>();
        static void Segreto(string nome, Vector3 pos, string descr) { segreti.Add(new PuntoSegreto { nome = nome, pos = pos, descr = descr }); }

        // ------------------------------------------------------------------ pezzi generati

        /// <summary>R007 t2: chiave del pezzo di primitive -> modello 3D vero (Assets/Art/Models). Le funzioni fill/mats restano come riserva ma non si usano.</summary>
        static string ModelloPerChiave(string key, Vector3 pos)
        {
            switch (key)
            {
                case "SediaPaglia": return "SediaPaglia";
                case "B_Credenza": return "Credenza";
                case "B_LettoFerro": return "LettoFerro";
                case "Bancarella1": case "Bancarella2": case "Bancarella3": case "Bancarella4": return "Bancarella";
                case "Bicicletta": return "Bicicletta";
                case "Pozzo": return "Pozzo";
                case "Fontana": return "Fontana";
                case "Panchina": return "PanchinaPopolare";
                case "Valigia": return (((int)(pos.x * 10f) + (int)(pos.z * 7f)) & 1) == 0 ? "Valigia" : "Valigia2";
                case "Sacco": return "Sacco";
                case "Tavolino": return "Tavolino";
                case "Gabbia": return "GabbiaGalline";
                case "Lavatoio": return "Lavatoio";
                case "B_Radio": return "Radio";
                case "R_Frutta0": return "CassaFrutta/CassaFrutta_Verdura.fbx";
                case "R_Frutta1": return "CassaFrutta/CassaFrutta_Arance.fbx";
                case "R_Frutta2": return "CassaFrutta/CassaFrutta_Pomodori.fbx";
            }
            return null;
        }

        static GameObject Pezzo(Transform parent, string name, string key, string[] mats, Action<KitBuilder> fill, Vector3 pos, float yaw,
                                float cull = 0.03f, bool collider = true)
        {
            string modello = ModelloPerChiave(key, pos);
            if (modello != null)
            {
                var g = ModelloIst(modello, parent, pos, yaw, key == "Tavolino" ? new Vector3(0.7f, 1f, 0.7f) : Vector3.one, collider, name, cull); // tavolino piu' stretto: 0,72 m
                g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                return g;
            }
            Mesh m;
            if (!propMesh.TryGetValue(key, out m) || m == null)
            {
                var kb = new KitBuilder(mats);
                fill(kb);
                m = kb.Build("Hub_P_" + key);
                propMesh[key] = m;
            }
            var go = MeshObject(parent, name, m, mats, collider);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(cull, new Renderer[] { go.GetComponent<Renderer>() }) });
            lg.RecalculateBounds();
            if (ATerra(pos)) RegistraMobile(go);
            propCount++;
            return go;
        }

        static void Bancarella(KitBuilder kb)
        {
            const float tw = TileWood;
            kb.Box(new Vector3(-0.9f, 0.8f, -0.4f), new Vector3(0.9f, 0.86f, 0.4f), 0, tw);
            foreach (var sx in new[] { -0.88f, 0.84f }) foreach (var sz in new[] { -0.38f, 0.34f })
                kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.05f, 0.8f, sz + 0.05f), 0, tw, KitBuilder.NY);
            foreach (var sx in new[] { -0.92f, 0.86f }) foreach (var sz in new[] { -0.42f, 0.36f })
                kb.Box(new Vector3(sx, 0.86f, sz), new Vector3(sx + 0.06f, 2.1f, sz + 0.06f), 0, tw, KitBuilder.NY);
            kb.Box(new Vector3(-1.05f, 2.1f, -0.55f), new Vector3(1.05f, 2.15f, 0.55f), 1, 0.6f);
            kb.Box(new Vector3(-1.05f, 1.85f, 0.5f), new Vector3(1.05f, 2.1f, 0.54f), 1, 0.6f);
            kb.Box(new Vector3(-0.8f, 0.86f, -0.3f), new Vector3(-0.3f, 1.08f, 0.3f), 2, 0.5f);
            kb.Box(new Vector3(-0.25f, 0.86f, -0.28f), new Vector3(0.25f, 0.96f, 0.28f), 3, 0.5f);
            kb.Box(new Vector3(0.35f, 0.86f, -0.3f), new Vector3(0.8f, 1.02f, 0.3f), 2, 0.5f);
            kb.Box(new Vector3(-0.6f, 0.12f, -0.3f), new Vector3(0.6f, 0.4f, 0.3f), 2, 0.5f, KitBuilder.NY);
        }

        static void GabbiaGalline(KitBuilder kb)
        {
            const float tw = TileWood;
            kb.Box(new Vector3(-0.4f, 0.25f, -0.3f), new Vector3(0.4f, 0.3f, 0.3f), 0, tw);
            foreach (var sx in new[] { -0.4f, 0.37f }) foreach (var sz in new[] { -0.3f, 0.27f })
            {
                kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.03f, 0.25f, sz + 0.03f), 0, tw, KitBuilder.NY);
                kb.Box(new Vector3(sx, 0.3f, sz), new Vector3(sx + 0.03f, 0.75f, sz + 0.03f), 0, tw, 0);
            }
            kb.Box(new Vector3(-0.42f, 0.75f, -0.32f), new Vector3(0.42f, 0.79f, 0.32f), 0, tw);
            for (float x = -0.34f; x < 0.36f; x += 0.1f)
            {
                kb.Box(new Vector3(x, 0.3f, -0.3f), new Vector3(x + 0.012f, 0.75f, -0.288f), 1, 1f, KitBuilder.NY | KitBuilder.PY);
                kb.Box(new Vector3(x, 0.3f, 0.288f), new Vector3(x + 0.012f, 0.75f, 0.3f), 1, 1f, KitBuilder.NY | KitBuilder.PY);
            }
            for (float z = -0.2f; z < 0.25f; z += 0.1f)
            {
                kb.Box(new Vector3(-0.4f, 0.3f, z), new Vector3(-0.388f, 0.75f, z + 0.012f), 1, 1f, KitBuilder.NY | KitBuilder.PY);
                kb.Box(new Vector3(0.388f, 0.3f, z), new Vector3(0.4f, 0.75f, z + 0.012f), 1, 1f, KitBuilder.NY | KitBuilder.PY);
            }
        }

        static void Bicicletta(KitBuilder kb)
        {
            // asse lungo x, ruote nel piano xy; sub 0 = ferro nero, 1 = cuoio, 2 = gomma
            Vector3 rear = new Vector3(-0.52f, 0.34f, 0f), front = new Vector3(0.52f, 0.34f, 0f);
            kb.Torus(rear, Vector3.forward, 0.33f, 0.018f, 14, 2, 0.3f);
            kb.Torus(front, Vector3.forward, 0.33f, 0.018f, 14, 2, 0.3f);
            kb.Torus(rear, Vector3.forward, 0.29f, 0.006f, 14, 0, 0.3f);
            kb.Torus(front, Vector3.forward, 0.29f, 0.006f, 14, 0, 0.3f);
            Vector3 bb = new Vector3(0f, 0.32f, 0f), seat = new Vector3(-0.2f, 0.92f, 0f), headLow = new Vector3(0.38f, 0.62f, 0f), headTop = new Vector3(0.42f, 0.92f, 0f);
            kb.Tube(bb, seat, 0.014f, 5, 0, 0.3f);
            kb.Tube(seat, headTop, 0.014f, 5, 0, 0.3f);
            kb.Tube(bb, headLow, 0.014f, 5, 0, 0.3f);
            kb.Tube(headLow, headTop, 0.016f, 5, 0, 0.3f);
            kb.Tube(headLow, front, 0.012f, 5, 0, 0.3f);
            kb.Tube(headTop, new Vector3(0.46f, 1.0f, 0f), 0.012f, 5, 0, 0.3f);
            kb.Tube(new Vector3(0.46f, 1.0f, -0.25f), new Vector3(0.46f, 1.0f, 0.25f), 0.011f, 5, 0, 0.3f);
            foreach (var z in new[] { -0.07f, 0.07f })
            {
                kb.Tube(seat + new Vector3(0f, -0.02f, z), rear + new Vector3(0f, 0f, z), 0.01f, 5, 0, 0.3f);
                kb.Tube(bb + new Vector3(0f, 0f, z), rear + new Vector3(0f, 0f, z), 0.01f, 5, 0, 0.3f);
            }
            kb.Tube(new Vector3(0f, 0.32f, -0.17f), new Vector3(0f, 0.32f, 0.17f), 0.01f, 5, 0, 0.3f);
            kb.Box(new Vector3(-0.32f, 0.92f, -0.08f), new Vector3(-0.1f, 0.97f, 0.08f), 1, 0.3f);
            kb.Tube(seat, seat + new Vector3(0f, 0.06f, 0f), 0.01f, 5, 0, 0.3f);
            kb.Tube(new Vector3(-0.3f, 0.36f, 0.06f), new Vector3(-0.2f, 0.02f, 0.2f), 0.008f, 4, 0, 0.3f); // cavalletto
        }

        static void SediaPaglia(KitBuilder kb)
        {
            const float tw = TileWood;
            foreach (var sx in new[] { -0.22f, 0.19f }) foreach (var sz in new[] { -0.22f, 0.19f })
                kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.035f, 0.45f, sz + 0.035f), 0, tw, KitBuilder.NY);
            kb.Box(new Vector3(-0.23f, 0.45f, -0.23f), new Vector3(0.23f, 0.5f, 0.23f), 1, 0.4f);
            foreach (var sx in new[] { -0.22f, 0.19f }) kb.Box(new Vector3(sx, 0.5f, -0.22f), new Vector3(sx + 0.035f, 0.95f, -0.185f), 0, tw);
            for (int i = 0; i < 3; i++) kb.Box(new Vector3(-0.2f, 0.62f + i * 0.12f, -0.215f), new Vector3(0.2f, 0.67f + i * 0.12f, -0.19f), 0, tw);
            kb.Box(new Vector3(-0.22f, 0.9f, -0.225f), new Vector3(0.22f, 0.95f, -0.18f), 0, tw);
        }

        static void Pozzo(KitBuilder kb)
        {
            kb.Box(new Vector3(-0.7f, 0f, -0.7f), new Vector3(0.7f, 0.9f, -0.45f), 0, TileStone);
            kb.Box(new Vector3(-0.7f, 0f, 0.45f), new Vector3(0.7f, 0.9f, 0.7f), 0, TileStone);
            kb.Box(new Vector3(-0.7f, 0f, -0.45f), new Vector3(-0.45f, 0.9f, 0.45f), 0, TileStone);
            kb.Box(new Vector3(0.45f, 0f, -0.45f), new Vector3(0.7f, 0.9f, 0.45f), 0, TileStone);
            kb.Box(new Vector3(-0.45f, 0.1f, -0.45f), new Vector3(0.45f, 0.14f, 0.45f), 0, TileStone);
            kb.Box(new Vector3(-0.62f, 0.9f, -0.04f), new Vector3(-0.56f, 2.1f, 0.04f), 1, TileWood);
            kb.Box(new Vector3(0.56f, 0.9f, -0.04f), new Vector3(0.62f, 2.1f, 0.04f), 1, TileWood);
            kb.Box(new Vector3(-0.66f, 2.05f, -0.06f), new Vector3(0.66f, 2.15f, 0.06f), 1, TileWood);
            kb.Box(new Vector3(-0.04f, 1.4f, -0.04f), new Vector3(0.04f, 2.05f, 0.04f), 2, 1f);
        }

        static void Lavatoio(KitBuilder kb)
        {
            kb.Box(new Vector3(-1.4f, 0f, -0.42f), new Vector3(1.4f, 0.85f, -0.3f), 0, TileStone);
            kb.Box(new Vector3(-1.4f, 0f, 0.3f), new Vector3(1.4f, 0.85f, 0.42f), 0, TileStone);
            kb.Box(new Vector3(-1.4f, 0f, -0.3f), new Vector3(-1.28f, 0.85f, 0.3f), 0, TileStone);
            kb.Box(new Vector3(1.28f, 0f, -0.3f), new Vector3(1.4f, 0.85f, 0.3f), 0, TileStone);
            kb.Box(new Vector3(-1.28f, 0f, -0.3f), new Vector3(1.28f, 0.5f, 0.3f), 0, TileStone);
            kb.Box(new Vector3(-1.2f, 0.5f, -0.28f), new Vector3(1.2f, 0.62f, 0.28f), 1, 1f, KitBuilder.NY);
            kb.Box(new Vector3(-1.45f, 0.85f, -0.46f), new Vector3(1.45f, 0.9f, 0.46f), 0, TileStone);
            kb.Box(new Vector3(-1.1f, 0.62f, -0.5f), new Vector3(-0.2f, 0.7f, -0.42f), 2, TileWood);
        }

        static void Fontana(KitBuilder kb)
        {
            kb.Box(new Vector3(-1.9f, 0f, -1.9f), new Vector3(1.9f, 0.14f, 1.9f), 0, TileStone, KitBuilder.NY);
            kb.Box(new Vector3(-1.7f, 0.14f, -1.7f), new Vector3(1.7f, 0.28f, 1.7f), 0, TileStone);
            kb.Box(new Vector3(-1.5f, 0.28f, -1.5f), new Vector3(1.5f, 0.9f, -1.3f), 0, TileStone);
            kb.Box(new Vector3(-1.5f, 0.28f, 1.3f), new Vector3(1.5f, 0.9f, 1.5f), 0, TileStone);
            kb.Box(new Vector3(-1.5f, 0.28f, -1.3f), new Vector3(-1.3f, 0.9f, 1.3f), 0, TileStone);
            kb.Box(new Vector3(1.3f, 0.28f, -1.3f), new Vector3(1.5f, 0.9f, 1.3f), 0, TileStone);
            kb.Box(new Vector3(-1.3f, 0.28f, -1.3f), new Vector3(1.3f, 0.6f, 1.3f), 0, TileStone);
            kb.Box(new Vector3(-1.3f, 0.6f, -1.3f), new Vector3(1.3f, 0.64f, 1.3f), 1, 2f, KitBuilder.NY);
            kb.Box(new Vector3(-0.3f, 0.6f, -0.3f), new Vector3(0.3f, 1.35f, 0.3f), 0, TileStone);
            kb.Box(new Vector3(-0.7f, 1.35f, -0.7f), new Vector3(0.7f, 1.45f, 0.7f), 0, TileStone);
            kb.Box(new Vector3(-0.6f, 1.45f, -0.6f), new Vector3(0.6f, 1.52f, 0.6f), 1, 1f, KitBuilder.NY);
            kb.Box(new Vector3(-0.15f, 1.45f, -0.15f), new Vector3(0.15f, 2.15f, 0.15f), 0, TileStone);
            kb.Box(new Vector3(-0.25f, 2.15f, -0.25f), new Vector3(0.25f, 2.25f, 0.25f), 0, TileStone);
            foreach (var s in new[] { new Vector2(1.3f, 0f), new Vector2(-1.3f, 0f), new Vector2(0f, 1.3f), new Vector2(0f, -1.3f) })
                kb.Box(new Vector3(s.x - 0.05f, 0.62f, s.y - 0.05f), new Vector3(s.x + 0.05f, 0.8f, s.y + 0.05f), 2, 1f);
        }

        // ------------------------------------------------------------------ arco / ponte sulla strada

        /// <summary>Ponte-arco sopra una strada: lungo x (span lungo x) o lungo z. center = a quota del suolo al centro della campata.</summary>
        static void PonteArco(Transform env, string nome, Vector3 center, bool lungoX, float span, float spessore, float quotaImposta, float altezza, string plaster)
        {
            var root = new GameObject("Casa_Ponte_" + nome);
            root.transform.SetParent(env, false);
            root.transform.position = center;
            root.transform.rotation = Quaternion.Euler(0f, lungoX ? 0f : 90f, 0f);
            float R = span * 0.5f, ys = quotaImposta, H = altezza, hz = spessore * 0.5f;
            string[] mats = { plaster, "M_ArenariaBlocchi", "M_LegnoVernicePelata", "Finestra" };
            var kc = new KitBuilder(mats);
            int N = 16;
            for (int i = 0; i < N; i++)
            {
                float u0 = -R + 2f * R * i / N, u1 = -R + 2f * R * (i + 1) / N;
                float y0 = ys + Mathf.Sqrt(Mathf.Max(0f, R * R - u0 * u0)), y1 = ys + Mathf.Sqrt(Mathf.Max(0f, R * R - u1 * u1));
                foreach (var sz in new[] { -1, 1 })
                {
                    float z = sz * hz;
                    kc.Quad(new Vector3(u0, y0, z), new Vector3(u1, y1, z), new Vector3(u1, H, z), new Vector3(u0, H, z), new Vector3(0, 0, sz), 0, TileWall);
                }
                var mid = new Vector3((u0 + u1) * 0.5f, ys + Mathf.Sqrt(Mathf.Max(0f, R * R - ((u0 + u1) * 0.5f) * ((u0 + u1) * 0.5f))), 0f);
                Vector3 nin = (new Vector3(0f, ys, 0f) - mid).normalized;
                kc.Quad(new Vector3(u0, y0, -hz), new Vector3(u1, y1, -hz), new Vector3(u1, y1, hz), new Vector3(u0, y0, hz), nin, 1, TileStone);
            }
            var kd = new KitBuilder(mats);
            float apex = ys + R;
            kd.Box(new Vector3(-R, H - 0.35f, -hz - 0.08f), new Vector3(R, H, hz + 0.08f), 1, TileStone, 0);      // cornicione
            kd.Box(new Vector3(-R, H, -hz), new Vector3(R, H + 0.5f, hz - 0.35f), 0, TileWall, KitBuilder.NY);       // parapetto
            kd.Box(new Vector3(-R, H, hz - 0.35f), new Vector3(R, H + 0.06f, hz), 0, TileWall, KitBuilder.NY);
            // finestra sopra il colmo
            float wy0 = apex + 0.5f, wy1 = Mathf.Min(H - 0.7f, wy0 + 1.7f);
            if (wy1 - wy0 > 1.0f)
                foreach (var sz in new[] { -1, 1 })
                {
                    float z0 = sz > 0 ? hz : -hz - 0.04f, z1 = sz > 0 ? hz + 0.04f : -hz;
                    kd.Box(new Vector3(-0.5f, wy0, z0), new Vector3(0.5f, wy1, z1), 3, 2f, 0);
                    kd.Box(new Vector3(-0.58f, wy1, z0 - (sz < 0 ? 0.03f : 0f)), new Vector3(0.58f, wy1 + 0.12f, z1 + (sz > 0 ? 0.03f : 0f)), 1, TileStone, 0);
                    kd.Box(new Vector3(-0.62f, wy0 - 0.1f, z0 - (sz < 0 ? 0.08f : 0f)), new Vector3(0.62f, wy0, z1 + (sz > 0 ? 0.08f : 0f)), 1, TileStone, 0);
                    kd.Box(new Vector3(-0.025f, wy0, z0), new Vector3(0.025f, wy1, z1), 2, TileWood, 0);
                }
            var mc = kc.Build("Hub_Ponte_" + nome);
            var gc = MeshObject(root.transform, "Curvo_Arco", mc, mats, false);
            var md = kd.Build("Hub_Ponte_" + nome + "_D");
            MeshObject(root.transform, "Dettagli", md, mats, false);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, (apex + H + 0.5f) * 0.5f, 0f);
            bc.size = new Vector3(span, H + 0.5f - apex, spessore);
            // spalle ai lati del colmo: impediscono alla camera di passare attraverso la muratura vicino agli appoggi
            var bl = root.AddComponent<BoxCollider>();
            bl.center = new Vector3(-R + 0.35f, (ys + apex) * 0.5f + 0.2f, 0f); bl.size = new Vector3(0.7f, apex - ys - 0.6f, spessore);
            var br = root.AddComponent<BoxCollider>();
            br.center = new Vector3(R - 0.35f, (ys + apex) * 0.5f + 0.2f, 0f); br.size = new Vector3(0.7f, apex - ys - 0.6f, spessore);
        }

        // ------------------------------------------------------------------ campanile

        static void Campanile(Transform env, Vector3 basePos)
        {
            string[] mats = { "M_ArenariaBlocchi", "M_IntonacoRustico05", "Finestra", "M_LuceFinestra", "M_CoppiArgilla", "M_FerroRuggine02" };
            var kb = new KitBuilder(mats);
            float s = 2.2f;
            kb.Box(new Vector3(-s - 0.3f, 0f, -s - 0.3f), new Vector3(s + 0.3f, 2.2f, s + 0.3f), 0, TileStone, KitBuilder.NY);
            kb.Box(new Vector3(-s, 2.2f, -s), new Vector3(s, 17f, s), 1, TileWall);
            foreach (var yb in new[] { 6f, 11f, 16.4f }) kb.Box(new Vector3(-s - 0.1f, yb, -s - 0.1f), new Vector3(s + 0.1f, yb + 0.25f, s + 0.1f), 0, TileStone);
            foreach (var y in new[] { 4.5f, 8.5f, 13f })
                foreach (var sgn in new[] { -1, 1 })
                {
                    kb.Box(new Vector3(-0.25f, y, sgn * (s + 0.01f) - 0.01f), new Vector3(0.25f, y + 1.1f, sgn * (s + 0.01f) + 0.01f), 2, 1f, 0);
                    kb.Box(new Vector3(sgn * (s + 0.01f) - 0.01f, y, -0.25f), new Vector3(sgn * (s + 0.01f) + 0.01f, y + 1.1f, 0.25f), 2, 1f, 0);
                }
            // cella campanaria con aperture luminose (punto di riferimento anche a distanza)
            kb.Box(new Vector3(-s + 0.1f, 17f, -s + 0.1f), new Vector3(s - 0.1f, 20.4f, s - 0.1f), 1, TileWall);
            foreach (var sgn in new[] { -1, 1 })
            {
                kb.Box(new Vector3(-0.7f, 17.5f, sgn * (s - 0.1f) - 0.015f), new Vector3(0.7f, 19.8f, sgn * (s - 0.1f) + 0.015f), 3, 1f, 0);
                kb.Box(new Vector3(sgn * (s - 0.1f) - 0.015f, 17.5f, -0.7f), new Vector3(sgn * (s - 0.1f) + 0.015f, 19.8f, 0.7f), 3, 1f, 0);
            }
            kb.Box(new Vector3(-s - 0.15f, 20.4f, -s - 0.15f), new Vector3(s + 0.15f, 20.8f, s + 0.15f), 0, TileStone);
            for (int i = 0; i < 7; i++)
            {
                float h0 = 20.8f + i * 0.9f, k = s + 0.15f - i * 0.33f;
                kb.Box(new Vector3(-k, h0, -k), new Vector3(k, h0 + 0.9f, k), 4, 1.5f, i == 0 ? KitBuilder.NY : 0);
            }
            kb.Box(new Vector3(-0.05f, 27.1f, -0.05f), new Vector3(0.05f, 28.6f, 0.05f), 5, 1f);
            kb.Box(new Vector3(-0.35f, 27.8f, -0.04f), new Vector3(0.35f, 27.9f, 0.04f), 5, 1f);
            var root = new GameObject("Casa_Campanile");
            root.transform.SetParent(env, false);
            root.transform.position = basePos;
            var mesh = kb.Build("Hub_Campanile");
            var g = MeshObject(root.transform, "Campanile", mesh, mats, false);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 14f, 0f); bc.size = new Vector3(2f * s + 0.6f, 28f, 2f * s + 0.6f);
            var lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(0.004f, new Renderer[] { g.GetComponent<Renderer>() }) });
            lg.RecalculateBounds();
            var luce = new GameObject("LuceCampanile");
            luce.transform.SetParent(root.transform, false);
            luce.transform.localPosition = new Vector3(0f, 18.6f, 0f);
        }

        // ------------------------------------------------------------------ edicola grande illuminata

        static void EdicolaGrande(Transform env, Vector3 wallPoint, Vector3 normal)
        {
            var fr = new WallFrame(wallPoint, Vector3.Cross(Vector3.up, normal), normal);
            var kb = new KitBuilder("M_ArenariaBlocchi", "M_LegnoDipintoBlu", "M_FerroRuggine02");
            // pilastri, architrave, timpano a gradoni e ripiano per le offerte
            fr.Box(kb, -1.1f, -0.85f, -0.3f, 0f, 0.9f, 3.1f, 0, TileStone, 0);
            fr.Box(kb, 0.85f, 1.1f, -0.3f, 0f, 0.9f, 3.1f, 0, TileStone, 0);
            fr.Box(kb, -1.25f, 1.25f, -0.34f, 0f, 3.1f, 3.3f, 0, TileStone, 0);
            fr.Box(kb, -1.0f, 1.0f, -0.3f, 0f, 3.3f, 3.45f, 0, TileStone, 0);
            fr.Box(kb, -0.6f, 0.6f, -0.26f, 0f, 3.45f, 3.6f, 0, TileStone, 0);
            fr.Box(kb, -0.25f, 0.25f, -0.22f, 0f, 3.6f, 3.75f, 0, TileStone, 0);
            fr.Box(kb, -1.3f, 1.3f, -0.62f, 0f, 0.8f, 0.97f, 0, TileStone, 0);      // ripiano (sopra la quota 0,93 dello zoccolo)
            fr.Box(kb, -1.2f, 1.2f, -0.55f, 0f, 0.55f, 0.8f, 0, TileStone, 0);
            fr.Box(kb, -0.85f, 0.85f, -0.07f, -0.01f, 0.9f, 3.1f, 1, 1f, 0);        // fondo azzurro del nicchione (sporge 7 cm: la fascia marcapiano della facciata sporge 5 cm, niente facce complanari)
            // grata di ferro bassa davanti al ripiano
            for (int i = 0; i <= 12; i++) { float pu = -1.15f + i * (2.3f / 12f); fr.Box(kb, pu - 0.01f, pu + 0.01f, -0.6f, -0.58f, 0.9f, 1.25f, 2, 1f, 0); }
            fr.Box(kb, -1.17f, 1.17f, -0.62f, -0.56f, 1.25f, 1.28f, 2, 1f, 0);
            MeshObject(env, "EdicolaGrande", kb.Build("Hub_EdicolaGrande_" + (int)(wallPoint.x * 10) + "_" + (int)(wallPoint.z * 10)), new[] { "M_ArenariaBlocchi", "M_LegnoDipintoBlu", "M_FerroRuggine02" }, true);
            // R007 t2: la Madonna e' il modello vero (0,9 m), in piedi sul ripiano della nicchia (quota 1,25 m)
            Model("MadonnaStatuetta", env, fr.W(0f, -0.30f, 1.25f), Mathf.Atan2(fr.n.x, fr.n.z) * Mathf.Rad2Deg, 1.0f, false, "EdicolaStatuetta");
            for (int i = -1; i <= 1; i++)
            {
                Model("Candela", env, fr.W(i * 0.7f, -0.3f, 0.97f), 0f, 1.1f, false, "EdicolaCandela"); // R007 t2: sul ripiano (quota 0,97), non dentro
                Prim(PrimitiveType.Sphere, "EdicolaLumino", env, fr.W(i * 0.7f, -0.3f, 1.06f), new Vector3(0.06f, 0.09f, 0.06f), "Glow", null, false);
            }
            var luce = new GameObject("LuceEdicolaGrande");
            luce.transform.SetParent(env, false);
            luce.transform.position = fr.W(0f, -1.1f, 1.9f);
            noBalcone.Add(wallPoint);
        }

        // ------------------------------------------------------------------ scala esterna e terrazzino

        static void ScalaTerrazzino(Transform env)
        {
            // cortile C3, parete nord (z = 32, normale verso -z), modulo cieco x -23..-15; quota 2.4
            float y0 = 2.4f, zWall = 32f;
            float zA = zWall - 1.1f, zB = zWall - 0.2f;      // larghezza della scala 0,9 m, a 20 cm dal muro
            int n = 16; float rs = 2.6f / n, tr = 0.28f, xStart = -15.9f;
            var kb = new KitBuilder("M_ArenariaBlocchi", "M_FerroRuggine02", "M_LegnoVernicePelata");
            for (int i = 0; i < n; i++)
                kb.Box(new Vector3(xStart - (i + 1) * tr, y0, zA), new Vector3(xStart - i * tr, y0 + (i + 1) * rs, zB), 0, TileStone, KitBuilder.NY);
            float xp = xStart - n * tr;                      // fine scala
            kb.Box(new Vector3(xp - 2.0f, y0, zA - 0.25f), new Vector3(xp, y0 + 2.6f, zB), 0, TileStone, KitBuilder.NY);   // terrazzino (pilastro pieno)
            kb.Box(new Vector3(xp - 2.05f, y0 + 2.6f, zA - 0.3f), new Vector3(xp, y0 + 2.7f, zB), 0, TileStone, 0);        // lastra
            // ringhiera sul lato esterno della scala e sul bordo del terrazzino
            float top = y0 + 2.7f;
            for (int i = 0; i <= n; i += 2)
            {
                float x = xStart - i * tr; float yb = y0 + Mathf.Max(1, i) * rs;
                kb.Box(new Vector3(x - 0.012f, yb, zA - 0.012f), new Vector3(x + 0.012f, yb + 0.9f, zA + 0.012f), 1, 1f, 0);
            }
            kb.Tube(new Vector3(xStart, y0 + rs + 0.95f, zA), new Vector3(xp, y0 + 2.6f + 0.95f, zA), 0.02f, 5, 1, 1f);
            for (float x = xp - 2.0f; x <= xp; x += 0.25f) kb.Box(new Vector3(x - 0.012f, top, zA - 0.3f), new Vector3(x + 0.012f, top + 0.9f, zA - 0.28f), 1, 1f, 0);
            for (float z = zA - 0.3f; z <= zB - 0.1f; z += 0.25f) kb.Box(new Vector3(xp - 2.05f, top, z - 0.012f), new Vector3(xp - 2.03f, top + 0.9f, z + 0.012f), 1, 1f, 0);
            kb.Box(new Vector3(xp - 2.07f, top + 0.9f, zA - 0.31f), new Vector3(xp, top + 0.94f, zA - 0.27f), 1, 1f, 0);
            kb.Box(new Vector3(xp - 2.07f, top + 0.9f, zA - 0.31f), new Vector3(xp - 2.03f, top + 0.94f, zB), 1, 1f, 0);
            // sottoscala: portella di assi nella fiancata sud
            float pxA = xStart - 11f * tr, pxB = xStart - 14f * tr;
            kb.Box(new Vector3(pxB, y0, zA - 0.06f), new Vector3(pxA, y0 + 1.15f, zA), 2, TileWood, 0);
            var go = MeshObject(env, "ScalaEsterna", kb.Build("Hub_ScalaTerrazzino"), new[] { "M_ArenariaBlocchi", "M_FerroRuggine02", "M_LegnoVernicePelata" }, false);
            for (int i = 0; i < n; i++)
            {
                var bc = go.AddComponent<BoxCollider>();
                float tp = y0 + (i + 1) * rs;
                bc.center = new Vector3(xStart - (i + 0.5f) * tr, (y0 + tp) * 0.5f, (zA + zB) * 0.5f);
                bc.size = new Vector3(tr, tp - y0, zB - zA);
            }
            var bp = go.AddComponent<BoxCollider>();
            bp.center = new Vector3(xp - 1.0f, y0 + 1.35f, (zA - 0.25f + zB) * 0.5f); bp.size = new Vector3(2.05f, 2.7f, zB - zA + 0.25f);
            // ringhiere piene: collider sottili per non cadere dal bordo
            var r1 = go.AddComponent<BoxCollider>();
            r1.center = new Vector3(xp - 1.0f, top + 0.45f, zA - 0.29f); r1.size = new Vector3(2.07f, 0.9f, 0.04f);
            var r2 = go.AddComponent<BoxCollider>();
            r2.center = new Vector3(xp - 2.04f, top + 0.45f, (zA - 0.3f + zB) * 0.5f); r2.size = new Vector3(0.04f, 0.9f, zB - zA + 0.3f);
            // dettagli sul terrazzino: cassetta e vaso di basilico
            Segreto("Terrazzino del cortile C3", new Vector3(xp - 1.0f, top, (zA + zB) * 0.5f - 0.1f), "Terrazzino a 2,6 m, raggiungibile con la scala esterna; qui ci sta un oggetto nascosto o una conversazione dall'alto.");
            Segreto("Sottoscala del cortile C3", new Vector3((pxA + pxB) * 0.5f, y0, zA - 0.5f), "Portella di assi nella fiancata della scala esterna (lato cortile): ripostiglio chiuso.");
        }

        // ------------------------------------------------------------------ panni stesi e lanterne tra le facciate

        static void Stenditoio(Transform parent, System.Random r, Vector3 p0, Vector3 p1, int cloths)
        {
            const float inset = 0.08f;
            Vector3 d = p1 - p0; d.y = 0f;
            float len = d.magnitude - 2f * inset;
            d.Normalize();
            Vector3 a = p0 + d * inset, b = p1 - d * inset;
            Vector3 mid = (a + b) * 0.5f;
            float ang = Mathf.Atan2(b.y - a.y, len) * Mathf.Rad2Deg;
            float yawLine = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            var filo = Box("Filo", parent, mid, new Vector3(len, 0.015f, 0.015f), "M_FerroRuggine02", new Vector3(0f, yawLine, ang), false);
            float maxW = len / (cloths + 1f) * 0.68f;
            for (int k = 1; k <= cloths; k++)
            {
                float tt = k / (cloths + 1f) + ((float)r.NextDouble() - 0.5f) * 0.03f;
                double roll = r.NextDouble();
                string kind = roll < 0.22 ? "Lenzuolo" : roll < 0.58 ? "Camicia" : roll < 0.84 ? "Pantaloni" : "Straccio";
                int variant = r.Next(0, 2);
                float baseW = kind == "Lenzuolo" ? 1.25f : kind == "Camicia" ? 0.81f : kind == "Pantaloni" ? 0.64f : 0.54f; // larghezze dei modelli Panno_* (R007 t2)
                float s = 0.8f + (float)r.NextDouble() * 0.35f;
                float sx = Mathf.Min(s, maxW / baseW);
                Vector3 pt = Vector3.Lerp(a, b, tt);
                string mat = ClothColors[r.Next(ClothColors.Length)];
                if (kind == "Lenzuolo" && r.NextDouble() < 0.6) mat = "PannoBianco";
                ClothObject(parent, kind, variant, mat, new Vector3(pt.x, pt.y - 0.03f, pt.z), yawLine + (float)(r.NextDouble() - 0.5) * 12f, new Vector3(sx, s, sx));
            }
        }

        // ------------------------------------------------------------------ componi il quartiere

        static void ArredaQuartiere(Transform env, System.Random r)
        {
            segreti.Clear();
            propMesh.Clear();
            EnsureWaterMaterial();

            // ---- punti di riferimento
            // grande arco sulla via principale (z 19..22), arco-sottopasso sul vicolo del cortile, ponticelli sulle traverse
            PonteArco(env, "Grande", PT(0f, 20.5f), true, 6f, 3f, 3.6f, 10.5f, "Intonaco");
            PonteArco(env, "Sottopasso", PT(-23f, -21.5f), true, 4f, 3f, 2.8f, 9f, "TufoScuro");
            PonteArco(env, "T4", PT(11.5f, 1f), false, 4f, 3f, 2.9f, 9f, "IntonacoOcra");
            PonteArco(env, "T7", PT(-12.5f, 16f), false, 4f, 3f, 2.9f, 9.5f, "IntonacoRosa");
            Model("LanternaAppesa", env, PT(0f, 20.5f, 3.35f), 0f, 1.3f, false, "LanternaArco");
            var lc = new GameObject("LuceLampione"); lc.transform.SetParent(env, false); lc.transform.position = PT(0f, 20.5f, 2.9f);
            noBalcone.Add(PT(0f, 20.5f));

            Campanile(env, new Vector3(-15f, 2f, 49f));

            EdicolaGrande(env, PT(-8f, 37f), Vector3.right);
            Segreto("Edicola votiva grande", PT(-7.2f, 37f), "Madonna illuminata dello slargo alto: ex-voto, candele, lettere lasciate fra i fiori.");

            // fontanella al centro della prima piazzetta
            Pezzo(env, "Fontanella", "Fontana", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Fontana, PT(0f, -35f), 0f, 0.01f);
            Segreto("Nicchia dietro la fontanella", PT(0f, -43f), "Fra la fontanella e il muro sud: una nicchia con un tombino chiuso e panni di una lavandaia.");
            Lamp(env, PT(-6.5f, -32f)); Lamp(env, PT(6.5f, -32f)); Lamp(env, PT(-6.5f, -40f));
            var pan = new KitBuilder("M_ArenariaBlocchi");
            pan.Box(new Vector3(-1.0f, 0f, -0.28f), new Vector3(1.0f, 0.5f, 0.28f), 0, TileStone);
            propMesh["Panchina"] = pan.Build("Hub_P_Panchina");
            foreach (var p in new[] { new Vector3(-5.5f, 0f, -36.5f), new Vector3(5.5f, 0f, -36.5f) })
            {
                var go = MeshObject(env, "PanchinaTufo", propMesh["Panchina"], new[] { "M_ArenariaBlocchi" }, true);
                go.transform.position = PT(p.x, p.z); go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); RegistraMobile(go);
            }

            // ---- luci d'ambiente accanto alle porte vere
            foreach (var f in feats)
                if (f.tipo == "porta") WallLamp(env, f.wp, f.n, Vector3.Cross(f.n, Vector3.up), 1.25f);

            Cortili(env, r);
            LungoLeStrade(env, r);
            PanniStesi(env, r);
            Bancarelle(env, r);
            TestiEManifesti(env, r);
            ScalaTerrazzino(env);
            Segreti(env);
        }

        static void EnsureWaterMaterial()
        {
            string path = "Assets/Art/Materials/M_Acqua.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.22f, 0.3f, 0.3f));
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            matCache["Acqua"] = m;
        }
    }
}
