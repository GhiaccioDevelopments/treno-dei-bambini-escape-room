using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R014: il ricordo "Il cappotto" su due scene giocabili (decisione D2 dello schema):
    ///  - 60_FB_Stazione: una grande stazione dell'inverno 1946-47, generica e ispirata alla vecchia stazione di Napoli (D1, nessun edificio vero):
    ///    atrio con i tavoli del Comitato e della Croce Rossa, la montagna di cappotti, panche, valigie di cartone e la folla (figure semplici, leggere);
    ///    la testata dei binari sotto la pensilina e la banchina del binario 1, con il treno (tre carrozze di terza classe, la 214 al centro) e la locomotiva.
    ///    Si sale sulla carrozza 214 da una porta (Door, luogo "ricordo_vagone") che si apre solo dopo il saluto di Antonietta (gettone "partenza").
    ///  - 61_FB_Vagone: l'interno della carrozza 214, stretto ma pieno: panche di legno, reticelle con le valigie, finestrini da cui scorre il paesaggio
    ///    (PaesaggioTreno), la carta della linea sulla parete di testa e, in fondo, la porta della discesa (uscita dal ricordo).
    /// Le postazioni degli enigmi stanno in flashback_cappotto.json (ArtSource/Cappotto/genera_flashback_cappotto.py): le coordinate devono coincidere.
    /// Menu: Treno > Scene > Costruisci ricordo Cappotto (anche dentro "Costruisci tutte le scene").
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiStazionePath = "Docs/richieste/media/R014/punti_segreti_stazione.md";
        const string PuntiSegretiVagonePath = "Docs/richieste/media/R014/punti_segreti_vagone.md";
        const float HAtrio = 9f;          // altezza dell'atrio
        const float HPensilina = 7.2f;    // altezza della pensilina
        const float QBinari = -0.6f;      // quota della massicciata (la banchina e' a 0)
        const float XBinario1 = -5.65f, XBinario2 = 5.65f;
        const float HCarrozza = 2.6f;     // altezza interna della carrozza 214

        // postazioni del JSON (enigmi nel mondo)
        static readonly Vector3 CapC1 = new Vector3(-8.0f, 0f, -7.0f);
        static readonly Vector3 CapC2 = new Vector3(8.0f, 0f, -7.0f);
        static readonly Vector3 CapC3 = new Vector3(-2.4f, 0f, 14.0f);
        static readonly Vector3 CapPortaTreno = new Vector3(-4.2f, 0f, 17.6f);
        static readonly Vector3 CapC4 = new Vector3(-0.9f, 0f, 17.9f);
        static readonly Vector3 CapC5 = new Vector3(0f, 0f, 9.0f);
        static readonly Vector3 CapValigia = new Vector3(1.1f, 2.0f, 5.0f);
        static readonly Vector3 CapUscita = new Vector3(0f, 0f, 20.3f);

        [MenuItem("Treno/Scene/Costruisci ricordo Cappotto (R014)")]
        public static void BuildCappottoSolo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ArtGenerator.EnsureMaterials();
            matCache.Clear();
            ArtGenerator.EnsureFolder(ScenesFolder);
            ArtGenerator.EnsureFolder("Assets/_Game/Generated");
            ArtGenerator.EnsureFolder(KitBuilder.Folder);
            facadeCache.Clear();
            kitMeshes = 0; kitTris = 0; propCount = 0;
            EnsureLightMaterials();
            BuildFlashbackStazione();
            BuildFlashbackVagone();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var n in new[] { "60_FB_Stazione", "61_FB_Vagone" })
            {
                string path = ScenePath(n);
                if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("60_FB_Stazione");
            LookBuilder.ApplyLookToScene("61_FB_Vagone");
            Debug.Log("[Cappotto] Scene costruite: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        [MenuItem("Treno/Scene/Prova automatica ricordo Cappotto (Play)")]
        public static void ProvaCappotto()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Cappotto] Esci dal Play prima di avviare la prova."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("cappottotest", true);
            EditorSceneManager.OpenScene(ScenePath("00_Boot"));
            EditorApplication.isPlaying = true;
        }

        // ------------------------------------------------------------------ utilita' comuni

        /// <summary>Figura di primitive per la folla (madri, bambini, volontarie, ferrovieri): leggera, con collider a capsula. scala 1 = adulto 1,72 m.</summary>
        static Transform FiguraFolla(Transform parent, string nome, Vector3 pos, float yaw, float scala, bool seduta, string matGonna, string matBusto, string matTesta)
        {
            var punto = new GameObject(nome).transform;
            punto.SetParent(parent, false);
            punto.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            punto.localScale = Vector3.one * scala;
            if (seduta) punto.position += Vector3.up * (0.47f - 0.43f * scala); // le cosce poggiano sulla seduta a 0,47 m (le gambe dei bambini penzolano)
            var g = new GameObject("Figura").transform;
            g.SetParent(punto, false);
            if (!seduta)
            {
                Cyl("Folla_Gonna", g, new Vector3(0f, 0.42f, 0f), 0.42f, 0.84f, matGonna, null, false);
                Cyl("Folla_Busto", g, new Vector3(0f, 1.15f, 0f), 0.4f, 0.62f, matBusto, null, false);
                Prim(PrimitiveType.Sphere, "Folla_Testa", g, new Vector3(0f, 1.6f, 0f), new Vector3(0.22f, 0.26f, 0.23f), matTesta, null, false);
                var cap = punto.gameObject.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.86f, 0f); cap.radius = 0.3f; cap.height = 1.72f;
            }
            else
            {
                // seduta su una panca alta 0,45 m (per scala 1): cosce in avanti (verso +z locale), busto e testa
                Box("Folla_Cosce", g, new Vector3(0f, 0.5f, 0.2f), new Vector3(0.36f, 0.14f, 0.42f), matGonna, null, false);
                Box("Folla_Gambe", g, new Vector3(0f, 0.24f, 0.38f), new Vector3(0.32f, 0.46f, 0.12f), matGonna, null, false);
                Cyl("Folla_Busto", g, new Vector3(0f, 0.86f, 0f), 0.38f, 0.6f, matBusto, null, false);
                Prim(PrimitiveType.Sphere, "Folla_Testa", g, new Vector3(0f, 1.32f, 0f), new Vector3(0.22f, 0.26f, 0.23f), matTesta, null, false);
            }
            return punto;
        }

        // ------------------------------------------------------------------ 60_FB_Stazione: layout

        static void DefinisciZoneStazione()
        {
            zone.Clear();
            Z("Atrio", "stazione", -15, 15, -16, 0, 0f);
            Z("Testata", "stazione", -11, 11, 0, 4, 0f);
            Z("Banchina", "stazione", -4, 4, 4, 44, 0f);
        }

        static void BuildFlashbackStazione()
        {
            var scene = NewScene();
            kitScene = "Stazione"; kitIndex = 0; pre = "Staz";
            var r = new System.Random(1946 + 14);
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            segreti.Clear();
            passanti.Clear();
            riservati.Clear();
            propMesh.Clear();
            feats.Clear();
            zonePonte.Clear();
            // mattina d'inverno: luce fredda e bassa che entra dal lucernario della pensilina, fumo grigio (nebbia); il look (LookBuilder) rifinisce
            Color fogStazione = new Color(0.56f, 0.55f, 0.52f);
            Ambient(new Color(0.62f, 0.62f, 0.62f), new Color(0.46f, 0.44f, 0.40f), new Color(0.22f, 0.20f, 0.17f));
            Sun(new Color(0.92f, 0.88f, 0.80f), 1.1f, new Vector3(28f, -24f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogStazione;
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 70f;

            EnsureLightMaterials();
            DefinisciZoneStazione();
            RasterizzaZone();
            quotaTerreno = (x, z) => GroundY(x, z, false); // fuori dalle zone (binari) niente avvisi: quota 0
            foreach (var p in new[] { CapC1, CapC2, CapC3, CapPortaTreno }) riservati.Add(new Vector2(p.x, p.z));

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo");
            CostruisciSuolo(env);
            CostruisciAtrio(env, r);
            CostruisciPensilina(env, r);
            CostruisciTreno(env, porte);
            RisolviSpazio("60_FB_Stazione");
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_cappotto"; // qui nascono C1, C2, C3; l'uscita e' nel vagone

            Vector3 start = new Vector3(0f, 0f, -13.5f);
            Spawn("inizio", start, 0f, spawns);

            SegretiStazione();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, fogStazione, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno);
            SaveScene(scene, "60_FB_Stazione");
            Debug.Log("[Cappotto] Stazione costruita: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone).");
            quotaTerreno = null;
            pre = "Hub";
        }

        // ------------------------------------------------------------------ l'atrio

        static void CostruisciAtrio(Transform env, System.Random r)
        {
            const string PL = "M_IntonacoMattoni";
            const string PI = "M_ArenariaBlocchi";
            float yc = HAtrio * 0.5f;
            var at = new GameObject("Atrio").transform;
            at.SetParent(env, false);
            // muri perimetrali (zoccolo in pietra, intonaco sopra)
            Box("MuroAtrio_Sud", at, new Vector3(0f, yc, -16.1f), new Vector3(30.4f, HAtrio, 0.2f), PL);
            Box("MuroAtrio_Ovest", at, new Vector3(-15.1f, yc, -8f), new Vector3(0.2f, HAtrio, 16.4f), PL);
            Box("MuroAtrio_Est", at, new Vector3(15.1f, yc, -8f), new Vector3(0.2f, HAtrio, 16.4f), PL);
            // muro nord con tre arcate verso la testata dei binari (x -8,5..-5,5; -1,5..1,5; 5,5..8,5, alte 5 m) e i due tratti ciechi oltre la pensilina
            float[,] pieni = { { -15f, -8.5f }, { -5.5f, -1.5f }, { 1.5f, 5.5f }, { 8.5f, 15f } };
            for (int i = 0; i < 4; i++)
            {
                float x0 = pieni[i, 0], x1 = pieni[i, 1];
                Box("MuroAtrio_Nord_" + i, at, new Vector3((x0 + x1) * 0.5f, yc, 0.1f), new Vector3(x1 - x0, HAtrio, 0.2f), PL);
            }
            foreach (float xa in new[] { -7f, 0f, 7f })
            {
                Box("MuroAtrio_Arco", at, new Vector3(xa, 5f + (HAtrio - 5f) * 0.5f, 0.1f), new Vector3(3f, HAtrio - 5f, 0.2f), PL);
                Box("ArcoStipite", at, new Vector3(xa - 1.55f, 2.5f, 0.1f), new Vector3(0.1f, 5f, 0.34f), PI, null, false);
                Box("ArcoStipite", at, new Vector3(xa + 1.55f, 2.5f, 0.1f), new Vector3(0.1f, 5f, 0.34f), PI, null, false);
                Box("ArcoChiave", at, new Vector3(xa, 5.1f, 0.1f), new Vector3(3.3f, 0.2f, 0.34f), PI, null, false);
            }
            // zoccolo di pietra sui muri lunghi
            Box("AppesoZoccolo", at, new Vector3(0f, 0.6f, -15.97f), new Vector3(29.9f, 1.2f, 0.06f), PI, null, false);
            Box("AppesoZoccolo", at, new Vector3(-14.97f, 0.6f, -8f), new Vector3(0.06f, 1.2f, 15.9f), PI, null, false);
            Box("AppesoZoccolo", at, new Vector3(14.97f, 0.6f, -8f), new Vector3(0.06f, 1.2f, 15.9f), PI, null, false);
            // soffitto a cassettoni (travi in ferro) e finestroni alti con vetri smerigliati
            Box("MuroSoffittoAtrio", at, new Vector3(0f, HAtrio + 0.1f, -8f), new Vector3(30.6f, 0.2f, 16.6f), PL);
            for (float x = -12f; x <= 12.1f; x += 4f)
                Box("AppesoTraveAtrio", at, new Vector3(x, HAtrio - 0.2f, -8f), new Vector3(0.25f, 0.4f, 15.9f), "M_FerroRuggineGrossa", null, false);
            foreach (float z in new[] { -12f, -4f })
                foreach (int lato in new[] { -1, 1 })
                {
                    Box("Finestrone", at, new Vector3(lato * 14.96f, 6.2f, z), new Vector3(0.04f, 3.2f, 2.6f), "PannoBianco", null, false);
                    Box("FinestroneTelaio", at, new Vector3(lato * 14.92f, 6.2f, z), new Vector3(0.05f, 3.3f, 0.08f), "M_FerroRuggineGrossa", null, false);
                    Box("FinestroneTelaio", at, new Vector3(lato * 14.92f, 6.2f, z), new Vector3(0.05f, 0.08f, 2.7f), "M_FerroRuggineGrossa", null, false);
                }
            // la porta grande d'ingresso (chiusa: dalla citta' si e' gia' entrati)
            Box("PortoneIngresso", at, new Vector3(0f, 2.2f, -15.94f), new Vector3(3.6f, 4.4f, 0.12f), "M_LegnoVernicePelata", null, false);
            Box("PortoneCornice", at, new Vector3(0f, 4.55f, -15.92f), new Vector3(4.1f, 0.3f, 0.16f), PI, null, false);

            // insegne e manifesti (testi in testi_ambiente.json, gruppo "ricordo")
            Manifesto(at, "StazioneNapoli", new Vector3(0f, 6.6f, -0.02f), Vector3.back, 4.2f, 0.8f, "Insegna_Stazione");
            Manifesto(at, "ComitatoBambini", new Vector3(-14.98f, 3.0f, -7f), Vector3.right, 3.4f, 0.64f, "Insegna_Comitato");
            Manifesto(at, "CroceRossa", new Vector3(14.98f, 3.0f, -7f), Vector3.left, 3.4f, 0.64f, "Insegna_CroceRossa");
            Manifesto(at, "SalaAspetto", new Vector3(0f, 5.3f, -15.98f), Vector3.forward, 4.6f, 0.86f, "Insegna_SalaAspetto");
            Manifesto(at, "AvvisoPartenza", new Vector3(-3.4f, 1.8f, -15.98f), Vector3.forward, 0.62f, 0.93f, "Manifesto_AvvisoPartenza");
            Manifesto(at, "Treni", new Vector3(3.4f, 1.8f, -15.98f), Vector3.forward, 0.62f, 0.93f, "Manifesto_Treni");
            Manifesto(at, "ComitatoVisite", new Vector3(-14.98f, 1.8f, -11.5f), Vector3.right, 0.62f, 0.93f, "Manifesto_Visite");
            // l'orologio sopra l'arcata centrale
            Cyl("Orologio", at, new Vector3(0f, 7.7f, -0.05f), 1.1f, 0.08f, "M_FerroRuggineGrossa", new Vector3(90f, 0f, 0f), false);
            Cyl("OrologioQuadrante", at, new Vector3(0f, 7.7f, -0.1f), 0.95f, 0.02f, "PannoBianco", new Vector3(90f, 0f, 0f), false);
            Box("OrologioLancetta", at, new Vector3(0f, 7.85f, -0.12f), new Vector3(0.04f, 0.32f, 0.01f), "M_FerroRuggine02", null, false);
            Box("OrologioLancetta", at, new Vector3(0.12f, 7.7f, -0.125f), new Vector3(0.26f, 0.035f, 0.01f), "M_FerroRuggine02", null, false);

            // luci: lampade appese (il look aggiunge una Point Light a ogni "LuceLampione")
            foreach (var p in new[] { new Vector3(-9f, 0f, -11f), new Vector3(0f, 0f, -11f), new Vector3(9f, 0f, -11f), new Vector3(-9f, 0f, -4f), new Vector3(0f, 0f, -4f), new Vector3(9f, 0f, -4f) })
            {
                var lg = Model("LanternaAppesa", at, new Vector3(p.x, HAtrio - 2.2f, p.z), 0f, 1.3f, false, "LanternaAppesa");
                var l = new GameObject("LuceLampione");
                l.transform.SetParent(lg.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Box("AppesoCatenaLanterna", at, new Vector3(p.x, HAtrio - 1.0f, p.z), new Vector3(0.03f, 2.0f, 0.03f), "M_FerroRuggine02", null, false);
            }

            // le volontarie dietro i tavoli (figure semplici: i modelli MakeHuman verranno dopo) e la folla
            FiguraFolla(at, "Npc_VolontariaComitato", new Vector3(-8.0f, 0f, -6.0f), 180f, 0.98f, false, "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Beige");
            FiguraFolla(at, "Npc_VolontariaCroceRossa", new Vector3(8.0f, 0f, -5.9f), 180f, 1.0f, false, "M_Panno_Bianco", "M_Panno_Bianco", "M_Panno_Beige");
            FiguraFolla(at, "Npc_Pachiochia", new Vector3(-6.2f, 0f, -14.3f), 30f, 0.93f, false, "M_Panno_Grigio", "M_Panno_Grigio", "M_Panno_Beige");
            var folla = new[]
            {
                new Vector4(-5.6f, -9.2f, 20f, 1f), new Vector4(-3.4f, -8.4f, 200f, 0.6f), new Vector4(2.6f, -6.6f, 160f, 1f), new Vector4(3.3f, -6.9f, 140f, 0.62f),
                new Vector4(-11.6f, -12.4f, 70f, 1f), new Vector4(-11.0f, -12.0f, 90f, 0.58f), new Vector4(11.4f, -12.0f, 250f, 1f), new Vector4(12.4f, -2.8f, 200f, 0.6f),
                new Vector4(-12.6f, -2.6f, 120f, 1f), new Vector4(5.6f, -1.6f, 0f, 1f), new Vector4(6.1f, -1.8f, 10f, 0.64f), new Vector4(-5.8f, -1.4f, 350f, 0.6f),
            };
            string[] gonne = { "M_Panno_Terra", "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Ocra", "M_Panno_Terracotta" };
            for (int i = 0; i < folla.Length; i++)
            {
                var f = folla[i];
                FiguraFolla(at, (f.w < 0.8f ? "Folla_Bambino_" : "Folla_Madre_") + i, new Vector3(f.x, 0f, f.y), f.z, f.w, false, gonne[i % gonne.Length], gonne[(i + 2) % gonne.Length], "M_Panno_Beige");
                riservati.Add(new Vector2(f.x, f.y));
            }

            // panche della sala d'aspetto, valigie di cartone e sacchi
            foreach (var p in new[] { new Vector3(-4f, 0f, -11f), new Vector3(4f, 0f, -11f), new Vector3(-4f, 0f, -3.6f), new Vector3(4f, 0f, -3.6f) })
                Panchina(at, p, 0f);
            Valigia(at, new Vector3(-4.6f, 0f, -10.4f), 15f); Valigia(at, new Vector3(3.3f, 0f, -10.3f), -20f); Valigia(at, new Vector3(4.9f, 0f, -4.2f), 80f);
            Valigia(at, new Vector3(-12.2f, 0f, -11.2f), 40f); Valigia(at, new Vector3(12.0f, 0f, -2.0f), -60f);
            Sacco(at, new Vector3(-3.0f, 0f, -4.3f), 10f); Sacco(at, new Vector3(11.0f, 0f, -12.6f), -25f); Sacco(at, new Vector3(-12.9f, 0f, -3.4f), 30f);
            Model("CassaLegno", at, new Vector3(-13.6f, 0f, -14.4f), 10f, 1.4f, true, "Cassa");
            Model("CassaLegno", at, new Vector3(13.6f, 0f, -14.6f), -15f, 1.4f, true, "Cassa");

            // la montagna di cappotti della Croce Rossa accanto al tavolo (C2): stoffe ammucchiate e un baule
            var mucchio = new GameObject("MucchioCappotti").transform;
            mucchio.SetParent(at, false);
            string[] panni = { "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Terra", "M_Panno_Ocra", "M_TessutoIuta", "M_Panno_Terracotta" };
            for (int i = 0; i < 14; i++)
            {
                float a = i * 2.4f, rr = 0.15f + 0.07f * (i % 5);
                var px = new Vector3(11.6f + Mathf.Cos(a) * rr, 0.12f + 0.11f * (i / 3), -8.6f + Mathf.Sin(a) * rr);
                Box("Cappotto_Mucchio", mucchio, px, new Vector3(0.62f - 0.03f * (i % 4), 0.1f, 0.48f), panni[i % panni.Length], new Vector3((i % 3) * 4f, i * 37f, (i % 2) * 6f), false);
            }
            var mb = mucchio.gameObject.AddComponent<BoxCollider>();
            mb.center = new Vector3(11.6f, 0.45f, -8.6f); mb.size = new Vector3(1.2f, 0.9f, 1.0f);
            Model("CassaLegno", at, new Vector3(12.9f, 0f, -7.6f), 90f, 1.3f, true, "BauleCroceRossa");
            Model("CestaVimini", at, new Vector3(10.9f, 0f, -9.8f), 0f, 1.3f, true, "CestaScarpe");

            Segreto("Tavolo del Comitato", CapC1, "Il tavolo del Comitato con il registro e i cartellini (enigma principale C1).");
            Segreto("Tavolo della Croce Rossa", CapC2, "Il tavolo della Croce Rossa con i cappotti e l'attaccapanni (enigma principale C2).");
            Segreto("Panca della sala d'aspetto", new Vector3(4.0f, 0f, -10.4f), "Davanti alla panca della sala d'aspetto, vicino all'ingresso: un biglietto di terza classe.");
            Segreto("Accanto al mucchio di cappotti", new Vector3(11.2f, 0f, -6.6f), "A terra accanto alla montagna di cappotti della Croce Rossa: un bottone.");
        }

        // ------------------------------------------------------------------ la pensilina e la banchina

        static void CostruisciPensilina(Transform env, System.Random r)
        {
            const string PL = "M_IntonacoMattoni";
            var pe = new GameObject("Pensilina").transform;
            pe.SetParent(env, false);
            float yc = HPensilina * 0.5f;
            // muri laterali della pensilina (oltre i binari) e chiusure fra atrio e pensilina
            Box("MuroPensilina_Ovest", pe, new Vector3(-11.1f, yc, 23f), new Vector3(0.2f, HPensilina, 46f), PL);
            Box("MuroPensilina_Est", pe, new Vector3(11.1f, yc, 23f), new Vector3(0.2f, HPensilina, 46f), PL);
            for (float z = 6f; z < 45f; z += 8f)
                foreach (int lato in new[] { -1, 1 })
                {
                    Box("Finestrone", pe, new Vector3(lato * 10.97f, 4.0f, z), new Vector3(0.04f, 3.0f, 3.2f), "PannoBianco", null, false);
                    Box("FinestroneTelaio", pe, new Vector3(lato * 10.93f, 4.0f, z), new Vector3(0.05f, 3.1f, 0.08f), "M_FerroRuggineGrossa", null, false);
                }
            // tetto della pensilina: due falde con un lucernario smerigliato in mezzo, capriate di ferro
            Box("MuroSoffittoPensilina_O", pe, new Vector3(-6.4f, HPensilina + 0.55f, 23f), new Vector3(9.6f, 0.15f, 46.2f), "M_AssiStagionate", new Vector3(0f, 0f, 7f));
            Box("MuroSoffittoPensilina_E", pe, new Vector3(6.4f, HPensilina + 0.55f, 23f), new Vector3(9.6f, 0.15f, 46.2f), "M_AssiStagionate", new Vector3(0f, 0f, -7f));
            Box("Lucernario", pe, new Vector3(0f, HPensilina + 1.12f, 23f), new Vector3(3.4f, 0.06f, 46.2f), "PannoBianco", null, false);
            for (float z = 2f; z < 46f; z += 6f)
            {
                Box("AppesoCatena", pe, new Vector3(0f, HPensilina - 0.1f, z), new Vector3(22f, 0.22f, 0.18f), "M_FerroRuggineGrossa", null, false);
                foreach (int lato in new[] { -1, 1 })
                {
                    // colonne di ghisa sulla banchina (a 0,6 m dal bordo) con il capitello
                    if (z > 5f && z < 43f)
                    {
                        Cyl("Colonna", pe, new Vector3(lato * 3.3f, HPensilina * 0.5f - 0.1f, z), 0.28f, HPensilina - 0.2f, "M_FerroRuggineGrossa");
                        Box("ColonnaCapitello", pe, new Vector3(lato * 3.3f, HPensilina - 0.3f, z), new Vector3(0.6f, 0.3f, 0.6f), "M_FerroRuggineGrossa", null, false);
                        Cyl("ColonnaBase", pe, new Vector3(lato * 3.3f, 0.15f, z), 0.44f, 0.3f, "M_FerroRuggineGrossa", null, false);
                    }
                }
            }
            // binari: massicciata, traversine e rotaie (una mesh per binario), respingenti in testa
            foreach (float xb in new[] { XBinario1, XBinario2 })
            {
                float lato = Mathf.Sign(xb);
                var mass = Box("MassicciataBinario", pe, new Vector3(xb + lato * 0.3f, QBinari + 0.035f, 37f), new Vector3(5.2f, 0.06f, 66f), "M_TerraPolverosa");
                var kb = new KitBuilder("M_AssiConsunte", "M_FerroRuggine02");
                for (float z = 4.6f; z < 70f; z += 0.9f)
                    kb.Box(new Vector3(-1.25f, 0f, z - 0.12f), new Vector3(1.25f, 0.12f, z + 0.12f), 0, 1f, KitBuilder.NY);
                foreach (float xr in new[] { -0.72f, 0.72f })
                    kb.Box(new Vector3(xr - 0.035f, 0.12f, 4.4f), new Vector3(xr + 0.035f, 0.27f, 70f), 1, 1f, KitBuilder.NY);
                var bin = MeshObject(pe, "Binario", kb.Build(KitName("Binario")), new[] { "M_AssiConsunte", "M_FerroRuggine02" }, false);
                bin.transform.position = new Vector3(xb, QBinari + 0.06f, 0f);
                // respingenti (fine corsa) contro la testata
                Box("Respingente", pe, new Vector3(xb, 0.1f, 4.35f), new Vector3(2.4f, 1.4f, 0.5f), "M_LegnoVernicePelata");
                foreach (float xr in new[] { -0.8f, 0.8f })
                    Cyl("RespingentePiatto", pe, new Vector3(xb + xr, 0.55f, 4.05f), 0.36f, 0.12f, "M_FerroRuggineGrossa", new Vector3(90f, 0f, 0f), false);
            }
            // bordi della banchina e della testata verso i binari: muretti invisibili (non si cade sui binari)
            InvisibleWall("MuroInvisibile_BordoOvest", pe, new Vector3(-4.05f, 0.7f, 24f), new Vector3(0.1f, 1.4f, 40f));
            InvisibleWall("MuroInvisibile_BordoEst", pe, new Vector3(4.05f, 0.7f, 24f), new Vector3(0.1f, 1.4f, 40f));
            InvisibleWall("MuroInvisibile_FineBanchina", pe, new Vector3(0f, 0.7f, 44.05f), new Vector3(8.2f, 1.4f, 0.1f));
            InvisibleWall("MuroInvisibile_TestataOvest", pe, new Vector3(-7.5f, 0.7f, 4.05f), new Vector3(7f, 1.4f, 0.1f));
            InvisibleWall("MuroInvisibile_TestataEst", pe, new Vector3(7.5f, 0.7f, 4.05f), new Vector3(7f, 1.4f, 0.1f));
            // bordo bianco della banchina (gesso) e cartelli dei binari
            Box("BordoBanchina", pe, new Vector3(-3.85f, 0.006f, 24f), new Vector3(0.3f, 0.012f, 40f), "PannoBianco", null, false);
            Box("BordoBanchina", pe, new Vector3(3.85f, 0.006f, 24f), new Vector3(0.3f, 0.012f, 40f), "PannoBianco", null, false);
            Manifesto(pe, "Binario1", new Vector3(-3.3f, 3.2f, 5.85f), Vector3.back, 1.4f, 0.26f, "Insegna_Binario1");
            Manifesto(pe, "Binario2", new Vector3(3.3f, 3.2f, 5.85f), Vector3.back, 1.4f, 0.26f, "Insegna_Binario2");

            // lampade della pensilina
            for (float z = 8f; z < 44f; z += 9f)
            {
                var lg = Model("LanternaAppesa", pe, new Vector3(0f, HPensilina - 1.6f, z), 0f, 1.2f, false, "LanternaAppesa");
                var l = new GameObject("LuceLampione");
                l.transform.SetParent(lg.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Box("AppesoCatenaLanterna", pe, new Vector3(0f, HPensilina - 0.85f, z), new Vector3(0.03f, 1.3f, 0.03f), "M_FerroRuggine02", null, false);
            }

            // sulla banchina: Antonietta accanto alla cassa con la valigia (C3), il capotreno, madri che salutano, carrelli
            FiguraFolla(pe, "Npc_Antonietta", new Vector3(-1.25f, 0f, 14.05f), 270f, 1.0f, false, "M_Panno_Grigio", "M_Panno_Terra", "M_Panno_Beige");
            FiguraFolla(pe, "Npc_Controllore", new Vector3(-2.4f, 0f, 18.9f), 250f, 1.02f, false, "M_Panno_BluSpento", "M_Panno_BluSpento", "M_Panno_Beige");
            foreach (var f in new[] { new Vector4(-2.6f, 23.5f, 260f, 1f), new Vector4(-1.4f, 26.0f, 240f, 1f), new Vector4(-2.9f, 30.5f, 270f, 0.95f), new Vector4(1.8f, 12.0f, 200f, 1f),
                                       new Vector4(2.4f, 20.0f, 300f, 1f), new Vector4(-2.2f, 9.0f, 230f, 0.62f) })
            {
                FiguraFolla(pe, f.w < 0.8f ? "Folla_Bambino" : "Folla_Madre", new Vector3(f.x, 0f, f.y), f.z, f.w, false, "M_Panno_Terra", "M_Panno_Grigio", "M_Panno_Beige");
                riservati.Add(new Vector2(f.x, f.y));
            }
            Cart(pe, new Vector3(2.4f, 0f, 34f));
            Valigia(pe, new Vector3(-2.3f, 0f, 24.3f), 70f); Valigia(pe, new Vector3(1.9f, 0f, 12.8f), -10f);
            Panchina(pe, new Vector3(2.6f, 0f, 16f), 90f);
            Panchina(pe, new Vector3(2.6f, 0f, 28f), 90f);

            Segreto("Valigia di Antonietta", CapC3, "La cassa con la valigia di cartone sulla banchina, accanto ad Antonietta (enigma principale C3).");
            Segreto("Porta della carrozza 214", CapPortaTreno, "La porta della carrozza 214: si sale sul treno dopo il saluto (porta verso 61_FB_Vagone).");
            Segreto("Banchina, sotto il numero della carrozza", new Vector3(-3.5f, 0f, 21.0f), "A terra sulla banchina, sotto il numero dipinto sulla fiancata della carrozza 214: un gessetto.");
        }

        // ------------------------------------------------------------------ il treno

        /// <summary>Carrozza di terza classe vista da fuori, da z0 a z1 sul binario x. La fiancata verso la banchina e' a x + 1,4 (binario 1, banchina a est).</summary>
        static GameObject CarrozzaFuori(Transform parent, string nome, float x, float z0, float z1)
        {
            var c = new GameObject(nome).transform;
            c.SetParent(parent, false);
            float L = z1 - z0, zc = (z0 + z1) * 0.5f;
            var cassa = Box("Carrozza_Cassa", c, new Vector3(x, 1.85f, zc), new Vector3(2.8f, 2.7f, L), "M_LegnoVernicePelata");
            Prim(PrimitiveType.Cylinder, "Carrozza_Tetto", c, new Vector3(x, 3.2f, zc), new Vector3(2.9f, L * 0.5f, 0.55f), "M_FerroRuggineGrossa", new Vector3(90f, 0f, 0f), false);
            Box("Carrozza_Telaio", c, new Vector3(x, 0.32f, zc), new Vector3(2.3f, 0.36f, L - 0.6f), "M_FerroRuggineGrossa", null, false);
            foreach (float zr in new[] { z0 + 1.8f, z0 + 3.2f, z1 - 3.2f, z1 - 1.8f })
                foreach (float xr in new[] { -0.75f, 0.75f })
                    Cyl("Carrozza_Ruota", c, new Vector3(x + xr, 0.19f, zr), 0.92f, 0.12f, "M_FerroRuggineGrossa", new Vector3(0f, 0f, 90f), false);
            // finestrini sulla fiancata della banchina (scuri) e cornici
            for (float z = z0 + 2.3f; z < z1 - 1.5f; z += 2f)
            {
                Box("Carrozza_Finestrino", c, new Vector3(x + 1.405f, 2.1f, z), new Vector3(0.02f, 0.75f, 1.0f), "Finestra", null, false);
                Box("Carrozza_Finestrino", c, new Vector3(x - 1.405f, 2.1f, z), new Vector3(0.02f, 0.75f, 1.0f), "Finestra", null, false);
            }
            // porte alle due testate, predellini e maniglioni
            foreach (float zp in new[] { z0 + 0.8f, z1 - 0.8f })
            {
                Box("Carrozza_Porta", c, new Vector3(x + 1.41f, 1.75f, zp), new Vector3(0.02f, 2.1f, 0.85f), "M_AssiStagionate", null, false);
                Box("Carrozza_Predellino", c, new Vector3(x + 1.25f, 0.28f, zp), new Vector3(0.3f, 0.05f, 0.8f), "M_FerroRuggine02", null, false);
                Box("Carrozza_Maniglione", c, new Vector3(x + 1.47f, 1.6f, zp - 0.5f), new Vector3(0.03f, 1.1f, 0.03f), "M_FerroRuggine02", null, false);
            }
            // respingenti alle testate
            foreach (float zt in new[] { z0 - 0.15f, z1 + 0.15f })
                foreach (float xr in new[] { -0.8f, 0.8f })
                    Cyl("Carrozza_Respingente", c, new Vector3(x + xr, 0.55f, zt), 0.3f, 0.3f, "M_FerroRuggineGrossa", new Vector3(90f, 0f, 0f), false);
            return cassa;
        }

        static void CostruisciTreno(Transform env, Transform porte)
        {
            var tr = new GameObject("Treno").transform;
            tr.SetParent(env, false);
            CarrozzaFuori(tr, "Carrozza_A", XBinario1, 4.9f, 16.4f);
            CarrozzaFuori(tr, "Carrozza_214", XBinario1, 16.8f, 28.6f);
            CarrozzaFuori(tr, "Carrozza_C", XBinario1, 29.0f, 40.8f);
            // il numero della carrozza 214 sulla fiancata (lo si legge salendo: serve al segreto della valigia)
            Manifesto(tr, "Carrozza214", new Vector3(XBinario1 + 1.42f, 2.95f, 22.7f), Vector3.right, 2.2f, 0.42f, "Insegna_Carrozza214");
            // tender e locomotiva a vapore
            Box("Tender", tr, new Vector3(XBinario1, 1.5f, 43.5f), new Vector3(2.7f, 2.4f, 4.6f), "M_FerroRuggineGrossa");
            Box("TenderCarbone", tr, new Vector3(XBinario1, 2.75f, 43.5f), new Vector3(2.3f, 0.25f, 4.0f), "M_MuroRotto", null, false);
            Box("LocomotivaCabina", tr, new Vector3(XBinario1, 2.25f, 47.3f), new Vector3(2.8f, 2.9f, 2.6f), "M_FerroRuggineGrossa");
            Prim(PrimitiveType.Cylinder, "LocomotivaCaldaia", tr, new Vector3(XBinario1, 2.1f, 52.6f), new Vector3(1.8f, 4.0f, 1.8f), "M_FerroRuggineGrossa", new Vector3(90f, 0f, 0f));
            Cyl("LocomotivaCamino", tr, new Vector3(XBinario1, 3.4f, 55.4f), 0.55f, 1.1f, "M_FerroRuggineGrossa", null, false);
            Cyl("LocomotivaDuomo", tr, new Vector3(XBinario1, 3.15f, 52.2f), 0.7f, 0.5f, "M_FerroRuggine02", null, false);
            Box("LocomotivaTelaio", tr, new Vector3(XBinario1, 0.75f, 51.5f), new Vector3(2.4f, 0.5f, 10f), "M_FerroRuggineGrossa", null, false);
            foreach (float zr in new[] { 49.3f, 51.3f, 53.3f })
                foreach (float xr in new[] { -0.85f, 0.85f })
                    Cyl("LocomotivaRuota", tr, new Vector3(XBinario1 + xr, 0.53f, zr), 1.6f, 0.12f, "M_FerroRuggineGrossa", new Vector3(0f, 0f, 90f), false);
            // sul binario 2 una vecchia carrozza in sosta
            CarrozzaFuori(tr, "Carrozza_Sosta", XBinario2, 14f, 25.8f);

            // la porta per salire sulla carrozza 214 (luogo "ricordo_vagone": si apre con il gettone "partenza", cioe' dopo il saluto di Antonietta)
            var go = Box("Porta_Carrozza214", porte, new Vector3(XBinario1 + 1.45f, 1.2f, CapPortaTreno.z), new Vector3(0.04f, 2.2f, 0.9f), "M_AssiStagionate"); // davanti alla porta dipinta della fiancata
            var d = go.AddComponent<Door>();
            d.luogoId = "ricordo_vagone";
            d.targetScene = "61_FB_Vagone";
            d.targetSpawn = "salita";
        }

        // ------------------------------------------------------------------ segreti della stazione

        static void SegretiStazione()
        {
            // ordine del rapporto dei punti (Docs/richieste/media/R014/punti_segreti_stazione.md): enigmi, porta, poi i collezionabili
            var gia = new List<PuntoSegreto>(segreti);
            segreti.Clear();
            foreach (var n in new[] { "Tavolo del Comitato", "Tavolo della Croce Rossa", "Valigia di Antonietta", "Porta della carrozza 214",
                                      "Panca della sala d'aspetto", "Accanto al mucchio di cappotti", "Banchina, sotto il numero della carrozza" })
                foreach (var s in gia) if (s.nome == n) segreti.Add(s);
            foreach (var s in gia) if (!segreti.Contains(s)) segreti.Add(s);
            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_cappotto.json, scena 60_FB_Stazione)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_cartellini", CapC1); riga("enigma_cappotti", CapC2); riga("enigma_mela", CapC3); riga("Porta_Carrozza214", CapPortaTreno);
            sb.AppendLine();
            sb.AppendLine("## Zone");
            sb.AppendLine();
            sb.AppendLine("| Zona | x0..x1 | z0..z1 | Quota |");
            sb.AppendLine("|------|--------|--------|-------|");
            foreach (var z in zone) sb.AppendLine("| " + z.nome + " | " + z.x0 + ".." + z.x1 + " | " + z.z0 + ".." + z.z1 + " | " + z.y + " |");
            ScriviDocSegreti(PuntiSegretiStazionePath, "Punti per i segreti del ricordo \"Il cappotto\": la stazione (R011/R014)", "60_FB_Stazione", null, sb.ToString());
        }

        // ------------------------------------------------------------------ 61_FB_Vagone

        static void DefinisciZoneVagone()
        {
            zone.Clear();
            Z("VestiboloSud", "legno", -1.4f, 1.4f, -2.5f, 0f, 0f);
            Z("Carrozza", "legno", -1.4f, 1.4f, 0f, 18f, 0f);
            Z("VestiboloNord", "legno", -1.4f, 1.4f, 18f, 20.5f, 0f);
        }

        static void BuildFlashbackVagone()
        {
            var scene = NewScene();
            kitScene = "Vagone"; kitIndex = 0; pre = "Vag";
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            segreti.Clear();
            passanti.Clear();
            riservati.Clear();
            propMesh.Clear();
            feats.Clear();
            zonePonte.Clear();
            // pomeriggio d'inverno fuori dai finestrini (PaesaggioTreno porta la luce verso la notte), dentro lampade a petrolio
            Color fogCampagna = new Color(0.62f, 0.6f, 0.55f);
            Ambient(new Color(0.58f, 0.58f, 0.56f), new Color(0.42f, 0.40f, 0.36f), new Color(0.2f, 0.18f, 0.15f));
            Sun(new Color(1f, 0.86f, 0.66f), 1.6f, new Vector3(20f, 70f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogCampagna;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 90f;

            EnsureLightMaterials();
            DefinisciZoneVagone();
            RasterizzaZone();
            quotaTerreno = (x, z) => GroundY(x, z, false);
            foreach (var p in new[] { CapC4, CapC5, CapUscita }) riservati.Add(new Vector2(p.x, p.z));

            var env = new GameObject("Ambiente").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo");
            CostruisciSuolo(env);
            CostruisciCarrozza(env);
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            // il paesaggio che scorre fuori dai finestrini (runtime, non statico)
            var pg = new GameObject("Paesaggio");
            var pt = pg.AddComponent<PaesaggioTreno>();
            pt.xCarrozza = 1.5f; pt.zMin = -40f; pt.zMax = 60f;

            // uscita: la porta della discesa in fondo al vestibolo nord
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, CapUscita + new Vector3(0f, 1.05f, 0.12f), new Vector3(0.85f, 2.0f, 0.08f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_cappotto"; // qui nascono C4 e C5 (e c'e' l'uscita)

            // camera: orbita stretta, i muri tra camera e Amerigo si dissolvono (come nel basso)
            var vr = new GameObject("VolumiCamera").transform;
            VolumeCamera(vr, "CamVol_Carrozza", new Vector3(0f, HCarrozza * 0.5f, 9f), new Vector3(2.8f, HCarrozza, 23f), 0.5f, 62f, -0.4f, 0.45f);

            Vector3 start = new Vector3(0f, 0f, -1.2f);
            Spawn("salita", start, 0f, spawns);
            Spawn("inizio", start, 0f, spawns);

            SegretiVagone();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, fogCampagna, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.InternoPiccolo);
            SaveScene(scene, "61_FB_Vagone");
            Debug.Log("[Cappotto] Vagone costruito: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone).");
            quotaTerreno = null;
            pre = "Hub";
        }

        /// <summary>Centri (z) degli scompartimenti della carrozza: nove, ogni 2 m, ciascuno con due panche per lato.</summary>
        static IEnumerable<float> Scompartimenti() { for (int k = 0; k < 9; k++) yield return 1f + 2f * k; }

        static void CostruisciCarrozza(Transform env)
        {
            const string LG = "M_AssiStagionate";
            var ca = new GameObject("Carrozza").transform;
            ca.SetParent(env, false);
            float H = HCarrozza;
            // fiancate: fascia bassa (0..1), fascia alta (1,75..2,6) e montanti fra i finestrini (1..1,75)
            foreach (int lato in new[] { -1, 1 })
            {
                string s = lato < 0 ? "O" : "E";
                Box("MuroCarrozza_Basso_" + s, ca, new Vector3(lato * 1.45f, 0.5f, 9f), new Vector3(0.1f, 1.0f, 23f), LG);
                Box("MuroCarrozza_Alto_" + s, ca, new Vector3(lato * 1.45f, 1.75f + (H - 1.75f) * 0.5f, 9f), new Vector3(0.1f, H - 1.75f, 23f), LG);
                // montanti: dai vestiboli (pieni) e fra un finestrino e l'altro (finestrini z c-0,55..c+0,55)
                var pieni = new List<Vector2> { new Vector2(-2.5f, 0.45f), new Vector2(17.55f, 20.5f) };
                foreach (float c in Scompartimenti()) if (c + 2f < 18f) pieni.Add(new Vector2(c + 0.55f, c + 1.45f));
                foreach (var p in pieni)
                    Box("MuroCarrozza_Montante_" + s, ca, new Vector3(lato * 1.45f, 1.375f, (p.x + p.y) * 0.5f), new Vector3(0.1f, 0.75f, p.y - p.x), LG);
                foreach (float c in Scompartimenti())
                    Box("Davanzale", ca, new Vector3(lato * 1.35f, 0.98f, c), new Vector3(0.1f, 0.04f, 1.1f), "M_LegnoVernicePelata", null, false);
            }
            // testate e pareti dei vestiboli (porta centrale larga 0,8 m), soffitto con le lampade
            Box("MuroCarrozza_TestataSud", ca, new Vector3(0f, H * 0.5f, -2.55f), new Vector3(3.0f, H, 0.1f), LG);
            foreach (float zt in new[] { 0f, 18f })
            {
                Box("MuroCarrozza_Divisorio_O", ca, new Vector3(-0.9f, H * 0.5f, zt + 0.04f), new Vector3(1.0f, H, 0.08f), LG);
                Box("MuroCarrozza_Divisorio_E", ca, new Vector3(0.9f, H * 0.5f, zt + 0.04f), new Vector3(1.0f, H, 0.08f), LG);
                Box("MuroCarrozza_Divisorio_Sopra", ca, new Vector3(0f, 2.1f + (H - 2.1f) * 0.5f, zt + 0.04f), new Vector3(0.8f, H - 2.1f, 0.08f), LG);
            }
            // testata nord: due tratti pieni ai lati della porta della discesa (l'uscita sta nel vano)
            Box("MuroCarrozza_TestataNord_O", ca, new Vector3(-0.95f, H * 0.5f, 20.55f), new Vector3(1.1f, H, 0.1f), LG);
            Box("MuroCarrozza_TestataNord_E", ca, new Vector3(0.95f, H * 0.5f, 20.55f), new Vector3(1.1f, H, 0.1f), LG);
            Box("MuroCarrozza_TestataNord_Sopra", ca, new Vector3(0f, 2.1f + (H - 2.1f) * 0.5f, 20.55f), new Vector3(0.8f, H - 2.1f, 0.1f), LG);
            Box("MuroSoffittoCarrozza", ca, new Vector3(0f, H + 0.08f, 9f), new Vector3(3.0f, 0.16f, 23.2f), "M_LegnoVernicePelata");
            Manifesto(ca, "PericoloSporgersi", new Vector3(-0.9f, 2.3f, -0.01f), Vector3.back, 0.9f, 0.17f, "Insegna_Sporgersi");
            Manifesto(ca, "Carrozza214", new Vector3(0.9f, 2.3f, -0.01f), Vector3.back, 0.9f, 0.17f, "Insegna_Carrozza214");

            // panche di legno (una mesh per tutte) con il loro collider, reticelle per i bagagli sopra i finestrini
            var kb = new KitBuilder("M_LegnoVernicePelata", "M_FerroRuggine02");
            var colliders = new List<Bounds>();
            foreach (float c in Scompartimenti())
                foreach (int lato in new[] { -1, 1 })
                    foreach (int verso in new[] { -1, 1 })
                    {
                        // panca: seduta a 0,45 m (profonda 0,45), schienale dalla parte del muro fra due scompartimenti
                        float zs = c + verso * 0.6f, zb = c + verso * 0.86f;
                        float x0 = lato < 0 ? -1.4f : 0.45f, x1 = lato < 0 ? -0.45f : 1.4f;
                        kb.Box(new Vector3(x0, 0.42f, zs - 0.225f), new Vector3(x1, 0.47f, zs + 0.225f), 0, 1f);
                        kb.Box(new Vector3(x0, 0.47f, zb - 0.025f), new Vector3(x1, 1.15f, zb + 0.025f), 0, 1f);
                        kb.Box(new Vector3(x0 + 0.05f, 0f, zs - 0.2f), new Vector3(x1 - 0.05f, 0.42f, zs - 0.16f), 0, 1f, KitBuilder.NY);
                        colliders.Add(new Bounds(new Vector3((x0 + x1) * 0.5f, 0.3f, zs), new Vector3(x1 - x0, 0.6f, 0.45f)));
                        colliders.Add(new Bounds(new Vector3((x0 + x1) * 0.5f, 0.8f, zb), new Vector3(x1 - x0, 0.7f, 0.05f)));
                    }
            foreach (int lato in new[] { -1, 1 })
            {
                float xi = lato * 1.0f, xe = lato * 1.4f;
                kb.Box(new Vector3(Mathf.Min(xi, xe), 1.97f, 0.2f), new Vector3(Mathf.Max(xi, xe), 2.0f, 17.8f), 1, 1f);   // il piano della reticella
                kb.Box(new Vector3(xi - 0.015f, 2.0f, 0.2f), new Vector3(xi + 0.015f, 2.06f, 17.8f), 1, 1f);                // il bordo
                for (float z = 0.4f; z < 17.8f; z += 2f)
                    kb.Box(new Vector3(Mathf.Min(xi, xe), 1.9f, z - 0.015f), new Vector3(Mathf.Max(xi, xe), 1.97f, z + 0.015f), 1, 1f);   // le mensole
            }
            var panche = MeshObject(ca, "Panche", kb.Build(KitName("Panche")), new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, false);
            foreach (var b in colliders) { var bc = panche.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size; }

            // valigie di cartone e fagotti sulle reticelle (lontano dalla valigia del segreto, a z 5 sul lato est)
            foreach (var v in new[] { new Vector3(-1.2f, 2.0f, 2.2f), new Vector3(-1.2f, 2.0f, 7.6f), new Vector3(1.2f, 2.0f, 9.4f), new Vector3(-1.2f, 2.0f, 13.1f), new Vector3(1.2f, 2.0f, 15.8f) })
            {
                Box("ValigiaCartone", ca, v + new Vector3(0f, 0.11f, 0f), new Vector3(0.34f, 0.22f, 0.5f), "Cuoio", new Vector3(0f, (v.z * 13f) % 9f - 4f, 0f), false);
                Box("ValigiaSpago", ca, v + new Vector3(0f, 0.11f, 0f), new Vector3(0.35f, 0.225f, 0.015f), "PannoBeige", new Vector3(0f, (v.z * 13f) % 9f - 4f, 0f), false);
            }
            // lampade a petrolio appese al soffitto (il look aggiunge la luce a ogni "LuceBottega")
            for (float z = 1f; z < 20f; z += 4f)
            {
                var lg = Model("LanternaAppesa", ca, new Vector3(0f, H - 0.5f, z), 0f, 0.8f, false, "LanternaCarrozza");
                Box("AppesoCatenaLanterna", ca, new Vector3(0f, H - 0.2f, z), new Vector3(0.025f, 0.4f, 0.025f), "M_FerroRuggine02", null, false);
                var l = new GameObject("LuceBottega");
                l.transform.SetParent(lg.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            }
            // altri bambini del treno, seduti (figure semplici) negli scompartimenti lontani dagli enigmi
            var sedute = new[] { new Vector4(-0.95f, 1.6f, 180f, 0.62f), new Vector4(0.95f, 0.4f, 0f, 0.6f), new Vector4(-0.95f, 5.6f, 180f, 0.58f),
                                 new Vector4(0.95f, 12.4f, 0f, 0.62f), new Vector4(-0.95f, 13.6f, 180f, 0.6f), new Vector4(0.95f, 15.6f, 180f, 0.64f) };
            for (int i = 0; i < sedute.Length; i++)
            {
                var s = sedute[i];
                FiguraFolla(ca, "Folla_BambinoSeduto_" + i, new Vector3(s.x, 0f, s.y), s.z, s.w, true, i % 2 == 0 ? "M_Panno_Terra" : "M_Panno_BluSpento", "M_Panno_Grigio", "M_Panno_Beige");
            }
            // una volontaria che accompagna i bambini, seduta nel primo scompartimento
            FiguraFolla(ca, "Npc_Accompagnatrice", new Vector3(0.95f, 0f, 1.6f), 180f, 1.0f, true, "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Beige");

            Segreto("Corridoio, accanto alla panca", new Vector3(0.25f, 0f, 12.4f), "A terra nel corridoio, accanto alla panca dello scompartimento: una cartolina.");
            Segreto("Davanzale del finestrino", new Vector3(1.3f, 1.0f, 15.0f), "Sul davanzale di un finestrino del lato est: un fazzoletto ricamato.");
        }

        static void SegretiVagone()
        {
            var gia = new List<PuntoSegreto>(segreti);
            segreti.Clear();
            Segreto("Carta della linea", CapC4, "La carta ferroviaria sulla parete di testa, in fondo alla carrozza (enigma principale C4).");
            Segreto("Scompartimento dei bambini", CapC5, "Lo scompartimento a meta' carrozza con Peppino, Rosetta e Gaetano (enigma principale C5).");
            Segreto("Reticella della valigia", CapValigia, "Sulla reticella del lato est, sopra lo scompartimento a z 5: una valigia con il lucchetto a tre rotelle.");
            foreach (var s in gia) segreti.Add(s);
            Segreto("Uscita dal ricordo", CapUscita, "La porta della discesa in fondo al vestibolo nord, che riporta al presente.");
            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_cappotto.json, scena 61_FB_Vagone)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_finestrino", CapC4); riga("enigma_paura", CapC5); riga("enigma_valigia (segreto)", CapValigia);
            ScriviDocSegreti(PuntiSegretiVagonePath, "Punti per i segreti del ricordo \"Il cappotto\": la carrozza 214 (R011/R014)", "61_FB_Vagone",
                "carrozza x -1,4..1,4 (corridoio x -0,45..0,45) z 0..18, vestiboli z -2,5..0 e 18..20,5; scompartimenti ogni 2 m (centri z 1, 3, ..., 17); reticelle a 2,0 m.", sb.ToString());
        }
    }
}
