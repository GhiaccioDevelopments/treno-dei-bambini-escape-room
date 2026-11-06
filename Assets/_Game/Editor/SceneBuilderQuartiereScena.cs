using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>R006 tappa 2: costruzione della scena 10_Hub_Vicolo (orchestrazione) e disposizione degli oggetti di scena.</summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiPath = "Docs/richieste/media/R006/punti_segreti_hub.md";

        /// <summary>Solo per lo sviluppo dell'hub: ricostruisce soltanto la scena 10_Hub_Vicolo (senza toccare i mesh delle altre scene) e applica il look.</summary>
        [MenuItem("Treno/Sviluppo/Costruisci solo l'hub")]
        public static void BuildHubSolo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ArtGenerator.EnsureMaterials();
            matCache.Clear();
            ArtGenerator.EnsureFolder(ScenesFolder);
            ArtGenerator.EnsureFolder("Assets/_Game/Generated");
            ArtGenerator.EnsureFolder(KitBuilder.Folder);
            facadeCache.Clear();
            kitMeshes = 0; kitTris = 0; propCount = 0;
            BuildHub();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("10_Hub_Vicolo");
            Debug.Log("[SceneBuilder] Hub: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi di arredo.");
        }

        static void BuildHub()
        {
            var scene = NewScene();
            kitScene = "Hub"; kitIndex = 0; pre = "Hub"; riservati.Clear();
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            Ambient(new Color(0.78f, 0.70f, 0.58f), new Color(0.55f, 0.47f, 0.38f), new Color(0.30f, 0.24f, 0.18f));
            Sun(new Color(1f, 0.90f, 0.72f), 1.0f, new Vector3(52f, -32f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.76f, 0.60f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 80f;

            DefinisciZone();
            RasterizzaZone();
            quotaTerreno = GroundY;
            DefinisciFeature();

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            CostruisciSuolo(env);
            CostruisciTetti(env);
            CostruisciFacciate(env, porte, r);
            ArredaQuartiere(env, r);
            RisolviSpazio("10_Hub_Vicolo");
            AssestaMacerie();
            SetStatic(env.gameObject);

            Spawn("inizio", PT(0f, -40f), 0f, spawns);
            Spawn("da_basso_antonietta", PT(-6.3f, -2f), -90f, spawns); // nello slargo, di fronte alla porta (x = -9)

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.85f, 0.76f, 0.60f), true);
            var start = PT(0f, -40f);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno, true);
            SaveScene(scene, "10_Hub_Vicolo");
            float area = AreaCalpestabile();
            Debug.Log("[Quartiere] Hub costruito: " + area.ToString("0") + " m2 calpestabili (" + zone.Count + " zone), " + moduli.Count + " moduli di facciata, " + hubAvvisi + " avvisi.");
            quotaTerreno = null;
        }

        // ------------------------------------------------------------------ helper di piazzamento

        static void Mob(GameObject go) { RegistraMobile(go); }

        static void Basilico(Transform env, Vector3 pos, float yaw)
        {
            var vaso = Model("VasoTerracotta", env, pos, yaw, 1.0f, true, "VasoBasilico");
            var foglie = Prim(PrimitiveType.Sphere, "Basilico", env, Vector3.zero, new Vector3(0.3f, 0.22f, 0.3f), "Foglia", null, false);
            foglie.transform.position = new Vector3(pos.x, TopY(vaso) + 0.11f - 0.003f, pos.z);
            Cavaliere(foglie, vaso);
        }

        static void Valigia(Transform env, Vector3 pos, float yaw)
        {
            Pezzo(env, "Valigia", "Valigia", new[] { "Cuoio", "M_FerroRuggine02" }, kb =>
            {
                kb.Box(new Vector3(-0.3f, 0f, -0.09f), new Vector3(0.3f, 0.4f, 0.09f), 0, 0.4f, KitBuilder.NY);
                kb.Box(new Vector3(-0.1f, 0.4f, -0.03f), new Vector3(0.1f, 0.44f, 0.03f), 1, 0.2f);
                kb.Box(new Vector3(-0.2f, 0.12f, -0.095f), new Vector3(-0.17f, 0.3f, 0.095f), 1, 0.2f);
                kb.Box(new Vector3(0.17f, 0.12f, -0.095f), new Vector3(0.2f, 0.3f, 0.095f), 1, 0.2f);
            }, pos, yaw, 0.04f);
        }

        static void Sacco(Transform env, Vector3 pos, float yaw)
        {
            Pezzo(env, "Sacco", "Sacco", new[] { "PannoBeige" }, kb =>
            {
                kb.Box(new Vector3(-0.25f, 0f, -0.18f), new Vector3(0.25f, 0.55f, 0.18f), 0, 0.3f, KitBuilder.NY);
                kb.Box(new Vector3(-0.15f, 0.55f, -0.12f), new Vector3(0.15f, 0.65f, 0.12f), 0, 0.3f);
            }, pos, yaw, 0.04f);
        }

        static void Tavolino(Transform env, Vector3 pos, float yaw)
        {
            Pezzo(env, "Tavolino", "Tavolino", new[] { "M_LegnoVernicePelata" }, kb =>
            {
                kb.Box(new Vector3(-0.4f, 0.72f, -0.4f), new Vector3(0.4f, 0.77f, 0.4f), 0, TileWood);
                foreach (var sx in new[] { -0.36f, 0.31f }) foreach (var sz in new[] { -0.36f, 0.31f })
                    kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.05f, 0.72f, sz + 0.05f), 0, TileWood, KitBuilder.NY);
            }, pos, yaw, 0.03f);
        }

        static void Bacinella(Transform env, Vector3 pos)
        {
            var go = Cyl("Bacinella", env, pos + Vector3.up * 0.07f, 0.55f, 0.14f, "M_SecchioMetallo", null, true);
            Mob(go);
        }

        static bool VicinoAFeature(Vector3 p, float dist)
        {
            foreach (var f in feats)
                if (f.tipo == "porta" || f.tipo == "portone" || f.tipo == "cancello" || f.tipo == "basso")
                    if (new Vector2(f.wp.x - p.x, f.wp.z - p.z).magnitude < dist) return true;
            foreach (var q in riservati) if (new Vector2(q.x - p.x, q.y - p.z).magnitude < 1.6f) return true;
            if (pre != "Hub") return false;
            var inizio = new Vector2(0f, -40f);
            if (new Vector2(p.x - inizio.x, p.z - inizio.y).magnitude < 3f) return true;
            if (new Vector2(p.x + 6.3f, p.z + 2f).magnitude < 2f) return true;
            return false;
        }

        // piazza un oggetto scelto a caso lungo un muro; yawMuro = orientamento "parallelo al muro"
        static void PosaTipo(Transform env, System.Random r, int tipo, Vector3 p, float yaw)
        {
            switch (tipo)
            {
                case 0: { var c = Model("CassaLegno", env, p, yaw, 1.3f, true, "Cassa"); if (r.NextDouble() < 0.35)
                        { var c2 = Model("CassaLegno", env, p + Vector3.up * 0.6f, yaw + 15f, 1.1f, true, "Cassa"); Impila(c2, c, new Vector2(0.03f, 0f)); } break; }
                case 1: Model("CestaVimini", env, p, yaw, 1.5f, true, "Cesta"); break;
                case 2: if (r.NextDouble() < 0.5) Model("SecchioMetallo", env, p, yaw, 1.1f, true, "Secchio"); else Model("SecchioLegno", env, p, yaw, 1.1f, true, "Secchio"); break;
                case 3: Model("BotteVino", env, p, yaw, 1.2f, true, "Botte"); break;
                case 4: Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, p, yaw + 180f, 0.03f); break;
                case 5: Basilico(env, p, yaw); Basilico(env, p + new Vector3(0.4f, 0f, 0.1f), yaw + 40f); break;
                case 6: Bacinella(env, p); break;
                case 7: Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, p, yaw, 0.03f); break;
                case 8: Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, p, yaw, 0.03f); break;
                case 9: Model("Carretto_Filip", env, p, yaw + 90f, 1.15f, true, "Carretto"); break;
                case 10: Sacco(env, p, yaw); Sacco(env, p + new Vector3(0.75f, 0f, 0.05f), yaw + 30f); break;
                default: Valigia(env, p, yaw); break;
            }
        }

        static void LungoLeStrade(Transform env, System.Random r)
        {
            // pesi: cassa, cesta, secchio, botte, sedia, basilico, bacinella, gabbia, bici, carretto, sacchi, valigia
            int[] pool = { 0, 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 5, 6, 7, 7, 8, 9, 10, 11 };
            foreach (var z in zone)
            {
                if (z.rampa || z.suolo != "strada") continue;
                bool ns = (z.x1 - z.x0) < (z.z1 - z.z0);
                float L = ns ? z.z1 - z.z0 : z.x1 - z.x0;
                float W = ns ? z.x1 - z.x0 : z.z1 - z.z0;
                int idx = 0;
                for (float t = 2.0f; t < L - 1.5f; t += 3.6f + (float)r.NextDouble() * 3.0f)
                {
                    int side = (idx++ % 2 == 0) ? -1 : 1;
                    float off = 0.8f + (float)r.NextDouble() * 0.15f;
                    float lat = side < 0 ? off : W - off;
                    Vector3 p; Vector3 verso;
                    if (ns) { p = new Vector3(z.x0 + lat, 0f, z.z0 + t); verso = new Vector3(side < 0 ? -1f : 1f, 0f, 0f); }
                    else { p = new Vector3(z.x0 + t, 0f, z.z0 + lat); verso = new Vector3(0f, 0f, side < 0 ? -1f : 1f); }
                    // vero muro dietro? (altrimenti e' l'imbocco di un'altra via)
                    var q = p + verso * (off + 0.35f);
                    if (Walk(Mathf.FloorToInt((q.x - rasX0) / Ras), Mathf.FloorToInt((q.z - rasZ0) / Ras))) continue;
                    p.y = GroundY(p.x, p.z);
                    if (VicinoAFeature(p, 2.4f)) continue;
                    // dentro l'area di un ponte? niente problema, ma non sopra una scalinata
                    int tipo = pool[r.Next(pool.Length)];
                    float yaw = (ns ? 0f : 90f) + (float)(r.NextDouble() - 0.5) * 24f + (r.NextDouble() < 0.5 ? 0f : 180f);
                    PosaTipo(env, r, tipo, p, yaw);
                }
            }
        }

        // ------------------------------------------------------------------ cortili, piazzette, lotto

        static void Cortili(Transform env, System.Random r)
        {
            // C1: lavatoio contro la parete ovest, pozzo, galline, panni, sedie
            Pezzo(env, "Lavatoio", "Lavatoio", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Lavatoio, PT(-28.1f, -30f), 90f, 0.02f);
            Pezzo(env, "Pozzo", "Pozzo", new[] { "M_ArenariaBlocchi", "M_LegnoVernicePelata", "M_FerroRuggine02" }, Pozzo, PT(-21f, -30.5f), 0f, 0.02f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(-18.4f, -33.6f), 20f, 0.03f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(-19.4f, -34.0f), -10f, 0.03f);
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-25.6f, -33.2f), 30f, 0.03f);
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-24.8f, -33.7f), -20f, 0.03f);
            Bacinella(env, PT(-26.5f, -27.2f)); Bacinella(env, PT(-27.3f, -27.7f));
            Model("SecchioMetallo", env, PT(-26.6f, -32.5f), 10f, 1.1f, true, "Secchio");
            Basilico(env, PT(-17.9f, -26.2f), 0f); Basilico(env, PT(-24.6f, -25.9f), 90f);
            Model("CestaVimini", env, PT(-22.5f, -33.8f), 25f, 1.5f, true, "Cesta");
            Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, PT(-17.9f, -29.5f), 90f, 0.03f);

            // C3: tavolo con le sedie (partita a carte), botti, vasi, carretto
            var tav = Model("TavoloLegno01", env, PT(-19.5f, 26f), 0f, 1.3f, true, "Tavolo");
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-19.5f, 27.1f), 180f, 0.03f);
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-18.3f, 26f), 90f, 0.03f);
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-20.7f, 26f), -90f, 0.03f);
            Model("BotteVino", env, PT(-24.1f, 24.0f), 0f, 1.25f, true, "Botte");
            Model("BotteVino", env, PT(-23.2f, 23.4f), 40f, 1.25f, true, "Botte");
            Model("CassaLegno", env, PT(-13.9f, 29.5f), 20f, 1.4f, true, "Cassa");
            Basilico(env, PT(-14.0f, 24.5f), 0f); Basilico(env, PT(-14.0f, 25.2f), 0f); Basilico(env, PT(-24.1f, 30.5f), 0f);
            Model("Carretto_Filip", env, PT(-15.5f, 23.2f), 0f, 1.15f, true, "Carretto");
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(-24.2f, 28.5f), 90f, 0.03f);

            // slargo del basso di Antonietta: panni, vasi, sedie sul davanti delle case
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-7.2f, -3.2f), 10f, 0.03f);
            Pezzo(env, "SediaPaglia", "SediaPaglia", new[] { "M_LegnoVernicePelata", "Paglia" }, SediaPaglia, PT(-6.4f, -3.3f), -25f, 0.03f);
            Basilico(env, PT(-8.3f, -0.7f), 0f); Basilico(env, PT(-7.6f, -0.7f), 0f);
            Bacinella(env, PT(-4.5f, -0.8f));
            Model("SecchioLegno", env, PT(-8.4f, -3.4f), 20f, 1.1f, true, "Secchio");
            Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, PT(-4.2f, -3.2f), 0f, 0.03f);

            // lotto bombardato: macerie, travi, muretti sbrecciati
            Rubble(env, r, PT(14f, -18f), 8, 1.0f);
            Rubble(env, r, PT(20.5f, -21f), 9, 1.1f);
            Rubble(env, r, PT(24.5f, -17f), 7, 0.9f);
            Rubble(env, r, PT(13.5f, -22.5f), 6, 0.8f);
            Rubble(env, r, PT(23.5f, -20.5f), 6, 0.7f);
            Box("Macerie_Muro", env, PT(18f, -23.3f, 0.7f), new Vector3(3.2f, 1.4f, 0.45f), "M_MuroRotto", new Vector3(0f, 8f, 0f));
            Box("Macerie_Muro", env, PT(26.2f, -20f, 0.55f), new Vector3(0.45f, 1.1f, 2.6f), "M_MuroRotto", new Vector3(0f, -6f, 0f));
            Box("Macerie_Muro", env, PT(11.6f, -16f, 0.9f), new Vector3(0.45f, 1.8f, 2.2f), "M_MuroRotto", new Vector3(0f, 4f, 0f));
            Model("SecchioMetallo", env, PT(16.8f, -16.4f), 0f, 1.1f, true, "Secchio");
            Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, PT(19.5f, -15.4f), 80f, 0.03f);

            // piazzetta della fontanella
            Model("CassaLegno", env, PT(-9f, -42.5f), 10f, 1.5f, true, "Cassa");
            Model("BotteVino", env, PT(-8.2f, -41.2f), 0f, 1.25f, true, "Botte");
            Cart(env, PT(8f, -37f));
            Sacco(env, PT(-1.8f, -43.1f), 20f); Sacco(env, PT(-0.8f, -43.3f), -15f);
            Model("CassaLegno", env, PT(1.4f, -43.2f), -10f, 1.3f, true, "Cassa");

            // slargo dell'edicola
            Lamp(env, PT(4.8f, 32.5f)); Lamp(env, PT(-4.8f, 41.5f)); Lamp(env, PT(4.8f, 41.5f));
            foreach (var p in new[] { new Vector3(-4.5f, 0f, 31.2f), new Vector3(4.6f, 0f, 38.2f) })
            {
                var go = MeshObject(env, "PanchinaTufo", propMesh["Panchina"], new[] { "M_ArenariaBlocchi" }, true);
                go.transform.position = PT(p.x, p.z); go.transform.rotation = Quaternion.Euler(0f, 0f, 0f); RegistraMobile(go);
            }
            Basilico(env, PT(-7.3f, 33.0f), 0f); Basilico(env, PT(-7.3f, 41.2f), 0f);
            Model("CestaVimini", env, PT(-7.1f, 40.2f), 0f, 1.5f, true, "Cesta");
            Model("CassaLegno", env, PT(6.8f, 44.0f), 0f, 1.4f, true, "Cassa");

            // largo della stazione: valigie di cartone e panchine
            Lamp(env, PT(-4.6f, 54.3f)); Lamp(env, PT(4.6f, 54.3f));
            Valigia(env, PT(-3.0f, 50.6f), 20f); Valigia(env, PT(-2.4f, 50.9f), -30f); Valigia(env, PT(3.4f, 52.4f), 80f);
            Sacco(env, PT(3.9f, 52.1f), 10f);
            foreach (var p in new[] { new Vector3(-4.7f, 0f, 52.2f), new Vector3(4.7f, 0f, 49.2f) })
            {
                var go = MeshObject(env, "PanchinaTufo", propMesh["Panchina"], new[] { "M_ArenariaBlocchi" }, true);
                go.transform.position = PT(p.x, p.z); go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); RegistraMobile(go);
            }
        }

        static void Bancarelle(Transform env, System.Random r)
        {
            string[] m = { "M_LegnoVernicePelata", "Tessuto1", "M_CassaLegno", "PannoOcra" };
            string[] m2 = { "M_LegnoVernicePelata", "Tessuto2", "M_CassaLegno", "PannoBeige" };
            Pezzo(env, "Bancarella", "Bancarella1", m, Bancarella, PT(-7f, -29.0f), 180f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella2", m2, Bancarella, PT(5.5f, 30.3f), 0f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella3", m, Bancarella, PT(5.5f, 43.7f), 180f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella4", m2, Bancarella, PT(1.95f, 6.5f), 270f, 0.03f);
        }

        // ------------------------------------------------------------------ panni stesi

        static void PanniStesi(Transform env, System.Random r)
        {
            // (x, z, lungo): 'x' = il filo corre lungo x (strada nord-sud), 'z' = corre lungo z (traversa est-ovest)
            var linee = new List<Vector3>();
            Func<float, float, char, int> add = (x, z, c) => { linee.Add(new Vector3(x, z, c)); return 0; };
            add(0, -24, 'x'); add(0, -15, 'x'); add(0, 8.5f, 'x'); add(0, 13, 'x'); add(0, 24.5f, 'x');
            add(-23, -14, 'x'); add(-23, 0, 'x'); add(19, 6, 'x'); add(19, 20.5f, 'x'); add(19, -8.5f, 'x'); add(19, 31, 'x');
            add(-27, -18, 'z'); add(-17, -18, 'z'); add(-9, -18, 'z');
            add(-27, 6, 'z'); add(-17, 6, 'z'); add(-9, 6, 'z');
            add(-27, 16, 'z'); add(-23, 16, 'z'); add(-8, 16, 'z');
            add(5, -12, 'z'); add(6, 1, 'z'); add(24, 1, 'z'); add(7, 16, 'z'); add(25, 16, 'z'); add(12, 35, 'z'); add(25, 35, 'z');
            add(-27, -30, 'z'); add(-19, -30, 'z'); add(-23, 27, 'z');
            PanniDaLista(env, r, linee);
        }

        static void PanniDaLista(Transform env, System.Random r, List<Vector3> linee)
        {
            int k = 0;
            foreach (var l in linee)
            {
                bool lungoX = (char)(int)l.z == 'x';
                float cx = l.x, cz = l.y;
                if (!Walk(Mathf.FloorToInt((cx - rasX0) / Ras), Mathf.FloorToInt((cz - rasZ0) / Ras))) { Debug.LogWarning("[Quartiere] Filo fuori strada @" + cx + "," + cz); hubAvvisi++; continue; }
                // estremi: primo non calpestabile da una parte e dall'altra, sulla stessa quota
                float yq = GroundY(cx, cz);
                float a = 0f, b = 0f; bool ok = true;
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    float s = 0f;
                    for (; s < 14f; s += Ras)
                    {
                        float x = lungoX ? cx + dir * (s + 0.25f) : cx, z = lungoX ? cz : cz + dir * (s + 0.25f);
                        int ix = Mathf.FloorToInt((x - rasX0) / Ras), iz = Mathf.FloorToInt((z - rasZ0) / Ras);
                        if (!Walk(ix, iz)) break;
                        if (Mathf.Abs(GroundY(x, z) - yq) > 0.01f) { ok = false; break; }
                    }
                    if (dir < 0) a = s; else b = s;
                }
                if (!ok || a + b > 12.5f || a + b < 2f) { Debug.LogWarning("[Quartiere] Filo scartato @" + cx + "," + cz + " campata " + (a + b)); hubAvvisi++; continue; }
                bool vicino = false;
                foreach (var p in zonePonte)
                    if (cx > p.x - 1f && cx < p.y + 1f && cz > p.z - 1f && cz < p.w + 1f) vicino = true;
                if (vicino) continue;
                float y = yq + 6.25f + (k % 3) * 0.08f;
                Vector3 p0, p1;
                if (lungoX) { p0 = new Vector3(cx - a, y, cz); p1 = new Vector3(cx + b, y - 0.06f - (float)r.NextDouble() * 0.06f, cz); }
                else { p0 = new Vector3(cx, y, cz - a); p1 = new Vector3(cx, y - 0.06f - (float)r.NextDouble() * 0.06f, cz + b); }
                Stenditoio(env, r, p0, p1, 3 + (k % 3));
                k++;
            }
        }

        // ------------------------------------------------------------------ punti per i segreti (R011)

        static void Segreti(Transform env)
        {
            Segreto("Retro del lavatoio (cortile C1)", PT(-27.4f, -26.3f), "Angolo fra lavatoio e muro nord: bacinelle e panni, ottimo per un biglietto o un oggetto piccolo.");
            Segreto("Pozzo del cortile C1", PT(-21f, -30.5f), "Il pozzo ha una secchia legata e un'asse sconnessa: il fondo puo' ospitare un oggetto o un indizio.");
            Segreto("Vicolo cieco S1, angolo buio", PT(-27f, -10f), "Vicolo cieco chiuso da un portone sbarrato; sotto l'ombra della grondaia c'e' spazio per una cassetta.");
            Segreto("Cancello del vicolo T7", PT(-27.5f, 16f), "Cancello di ferro: si intravede un cortile buio dietro le sbarre (anche solo una voce o una luce).");
            Segreto("Portone sbarrato del vicolo T1", PT(-27.5f, -18f), "Portone con le assi inchiodate: graffiti e segni lasciati dai ragazzi.");
            Segreto("Sottopasso verso il cortile C1", PT(-23f, -21.5f), "Passaggio coperto sotto l'arco: nicchia nel pilastro, adatta a un oggetto nascosto.");
            Segreto("Sotto il grande arco", PT(-2.4f, 20.5f), "Alla base del pilastro ovest del grande arco, dietro un cesto.");
            Segreto("Dietro le macerie del lotto", PT(25f, -22.2f), "Fra il muretto sbrecciato e la parete est del lotto: pochi passi fuori dalla vista.");
            Segreto("Cavita' sotto la trave caduta", PT(15f, -19.2f), "Fra due cumuli di macerie, accanto alla trave spezzata.");
            Segreto("Bottega dalla luce calda", PT(7f, -26.6f), "Davanti al basso aperto sulla piazzetta: ripiano con bottiglie e scatole, luce calda visibile da lontano.");
            Segreto("Edicola piccola (traversa T3)", PT(-14f, 6.8f), "Edicola votiva piccola con lumino: ex-voto e un biglietto fra i fiori.");
            Segreto("Edicola piccola (traversa T5)", PT(12f, 15.2f), "Edicola votiva piccola nella traversa alta, quasi sempre in ombra.");
            Segreto("Bacheca dei manifesti (traversa T2)", PT(14f, -11.2f), "Bacheca con manifesti originali: dietro quello piu' vecchio c'e' un foglio.");
            Segreto("Gradino della chiesetta", PT(-5.5f, 43.8f), "Gradino davanti al portone della chiesetta, sotto il campanile.");
            Segreto("Vicolo cieco S2 con casse", PT(25f, 24f), "Vicolo laterale alto chiuso da un portone: casse accatastate, ripostiglio.");
            Segreto("Nicchia a lato della scalinata F2", PT(-2.3f, 8.8f), "Rientranza accanto all'inizio della seconda scalinata.");
            Segreto("Valigie dimenticate al largo della stazione", PT(-2.8f, 50.4f), "Valigie di cartone e sacchi: ricordo dei bambini in partenza.");
            Segreto("Sotto la finestra dello slargo di Antonietta", PT(-6.5f, -0.8f), "Angolo con vasi di basilico, bacinella e sedie impagliate davanti ai bassi.");
            Segreto("Tavolo da gioco del cortile C3", PT(-19.5f, 26f), "Tavolo con le sedie: partita a carte interrotta; qualcosa sotto il tavolo.");
            ScriviPuntiSegreti();
        }

        static void ScriviPuntiSegreti()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Punti per i segreti dell'hub (R011)");
            sb.AppendLine();
            sb.AppendLine("Generato da `SceneBuilder.BuildHub` (R006, tappa 2). Coordinate mondo nella scena `10_Hub_Vicolo` (x, y = quota del suolo, z).");
            sb.AppendLine("Non toccare a mano: rigenerare la scena da **Treno > Costruisci tutte le scene** aggiorna questo file.");
            sb.AppendLine();
            sb.AppendLine("| # | Punto | x | y | z | Idea |");
            sb.AppendLine("|---|-------|---|---|---|------|");
            int i = 1;
            foreach (var s in segreti)
                sb.AppendLine("| " + (i++) + " | " + s.nome + " | " + s.pos.x.ToString("0.0") + " | " + s.pos.y.ToString("0.0") + " | " + s.pos.z.ToString("0.0") + " | " + s.descr + " |");
            sb.AppendLine();
            sb.AppendLine("## Zone dell'hub");
            sb.AppendLine();
            sb.AppendLine("| Zona | x0..x1 | z0..z1 | Quota |");
            sb.AppendLine("|------|--------|--------|-------|");
            foreach (var z in zone)
                sb.AppendLine("| " + z.nome + " | " + z.x0 + ".." + z.x1 + " | " + z.z0 + ".." + z.z1 + " | " + (z.rampa ? z.y + " -> " + z.y1 + " (scalinata)" : z.y.ToString()) + " |");
            string full = Path.GetFullPath(PuntiSegretiPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
