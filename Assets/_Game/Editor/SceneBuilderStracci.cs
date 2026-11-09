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
    /// R012: il ricordo "Stracci" (40_FB_Stracci), una sola scena con due zone senza caricamenti (D1): il vicolo degli stracci (cortili, macerie, fili del bucato,
    /// scale esterne, un terrazzino e un giro che fa da scorciatoia) e, in fondo, la bottega di Capa 'e fierro, un interno grande con soffitto e luce di lampada a petrolio.
    /// Riusa il sistema a zone rasterizzate del quartiere (suolo, facciate, tetti) con il prefisso "Str" per le mesh condivise.
    /// Le postazioni degli enigmi e i gettoni stanno in flashback_stracci.json (ArtSource/Stracci/genera_flashback_stracci.py): le coordinate devono coincidere.
    /// Menu: Treno > Scene > Costruisci ricordo Stracci (anche dentro "Costruisci tutte le scene").
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiStracciPath = "Docs/richieste/media/R012/punti_segreti_stracci.md";
        const float QB = 1.2f;   // quota del Vico Alto, del largo, della soglia e della bottega
        const float HB = 3.5f;   // altezza interna della bottega

        // postazioni del JSON (enigmi nel mondo)
        static readonly Vector3 StrS1 = new Vector3(-4.0f, 0f, -25.4f);
        static readonly Vector3 StrS2 = new Vector3(-6.6f, QB, 41.0f);
        static readonly Vector3 StrS3 = new Vector3(6.6f, QB, 39.6f);
        static readonly Vector3 StrS4 = new Vector3(6.6f, QB, 43.6f);
        static readonly Vector3 StrS5 = new Vector3(-1.0f, QB, 47.35f);
        static readonly Vector3 StrCarretto = new Vector3(-5.3f, QB, 45.2f);
        static readonly Vector3 StrCapa = new Vector3(2.2f, QB, 43.2f);
        static readonly Vector3 StrUscita = new Vector3(5.8f, QB, 47.88f);

        [MenuItem("Treno/Scene/Costruisci ricordo Stracci (R012)")]
        public static void BuildStracciSolo()
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
            BuildFlashbackStracci();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string path = ScenePath("40_FB_Stracci");
            if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("40_FB_Stracci");
            Debug.Log("[Stracci] Scena costruita: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        // ------------------------------------------------------------------ layout

        static void DefinisciZoneStracci()
        {
            zone.Clear();
            Z("Largo", "piazza", -6, 6, -30, -22, 0f);
            Z("Via", "strada", -3, 3, -22, 12, 0f);
            Rampa("ScalaVia", -3, 3, 12, 15, 0f, QB, 8);
            Z("ViaAlta", "strada", -3, 3, 15, 24, QB);
            Z("LargoBottega", "piazza", -6, 6, 24, 32, QB);
            Z("Soglia", "strada", -1.75f, 1.75f, 32, 35, QB);
            Z("Bottega", "cortile", -8, 8, 35, 48, QB);
            Z("VicoPanni", "strada", -16, -3, -9, -5, 0f);
            Z("CortileBucato", "cortile", -26, -16, -14, -1, 0f);
            Z("Lotto", "macerie", 3, 15, -20, -8, 0f);
            Z("VicoCarrettieri", "strada", 3, 14, 4, 8, 0f);
            Z("CortileCarrettieri", "cortile", 14, 24, -2, 12, 0f);
            Rampa("ScalaTerrazzo", 18, 22, 12, 15, 0f, QB, 8);
            Z("Terrazzino", "cortile", 14, 26, 15, 23, QB);
            Z("PassaggioAlto", "strada", 3, 14, 17, 21, QB);
        }

        static void DefinisciFeatureStracci()
        {
            feats.Clear();
            zonePonte.Clear();
            // portoni dove si bussa: cortile del bucato, cortile dei carrettieri, terrazzino
            Fe("portone", -26, -10, 1, 0, 4, null, null);
            Fe("portone", -26, -5, 1, 0, 4, null, null);
            Fe("portone", -21, -14, 0, 1, 4, null, null);
            Fe("portone", 24, 2, -1, 0, 4, null, null);
            Fe("portone", 24, 8, -1, 0, 4, null, null);
            Fe("portone", 20, 23, 0, -1, 4, null, null);
            // bassi aperti con interno accennato
            Fe("basso", -6, -26, 1, 0, 6, null, null).interno = 0;
            Fe("basso", 6, -26, -1, 0, 6, null, null).interno = 2;
            Fe("basso", -3, 19, 1, 0, 4, null, null).interno = 1;
            // chiusure e muri ciechi: manifesto della bottega, edicola votiva, graffiti
            Fe("cieco", -6, 28, 1, 0, 4, null, null);              // manifesto "si comprano stracci"
            Fe("cieco", 6, 28, -1, 0, 4, null, null);
            Fe("cieco", 12, -20, 0, 1, 6, null, null);             // lotto: muro sbrecciato
            Fe("cieco", 0, -30, 0, 1, 6, null, null);              // fondo del largo d'ingresso
        }

        static readonly string[] MatPanni = { "M_Panno_Beige", "M_Panno_Terra", "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Ocra", "M_Panno_Terracotta", "M_Panno_Bianco", "M_TessutoIuta" };

        // ------------------------------------------------------------------ scena

        static void BuildFlashbackStracci()
        {
            var scene = NewScene();
            kitScene = "Stracci"; kitIndex = 0; pre = "Str";
            var r = new System.Random(1946 + 12);
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            segreti.Clear();
            passanti.Clear();
            riservati.Clear();
            Color sepiaFog = new Color(0.62f, 0.50f, 0.34f);
            Ambient(new Color(0.72f, 0.60f, 0.42f), new Color(0.52f, 0.42f, 0.30f), new Color(0.28f, 0.22f, 0.15f));
            Sun(new Color(1f, 0.86f, 0.62f), 0.9f, new Vector3(55f, -40f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sepiaFog;
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 60f;

            EnsureLightMaterials();
            DefinisciZoneStracci();
            RasterizzaZone();
            quotaTerreno = GroundY;
            DefinisciFeatureStracci();
            foreach (var p in new[] { StrS1, StrS2, StrS3, StrS4, StrS5, StrCarretto, StrCapa })
                riservati.Add(new Vector2(p.x, p.z));
            foreach (var s in StracciGettoni) riservati.Add(new Vector2(s.x, s.z));

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo");
            CostruisciSuolo(env);
            CostruisciTetti(env);
            CostruisciFacciate(env, porte, r);
            ArredaVicoloStracci(env, r);
            CostruisciBottega(env, r);
            RisolviSpazio("40_FB_Stracci");
            AssestaMacerie();
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            // uscita: porta di servizio della bottega, sul muro nord (R012: "apre la porta della bottega verso il vicolo")
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, StrUscita + Vector3.up * 1.15f, new Vector3(1.3f, 2.3f, 0.1f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();
            {
                Vector3 c0 = StrUscita + Vector3.back * 0.0f; var qf = Quaternion.identity.eulerAngles;
                Box("UscitaStipite", exitRoot, c0 + Vector3.right * 0.75f + Vector3.up * 1.2f + Vector3.back * 0.06f, new Vector3(0.16f, 2.4f, 0.2f), "M_AssiConsunte", qf, false);
                Box("UscitaStipite", exitRoot, c0 - Vector3.right * 0.75f + Vector3.up * 1.2f + Vector3.back * 0.06f, new Vector3(0.16f, 2.4f, 0.2f), "M_AssiConsunte", qf, false);
                Box("UscitaArchitrave", exitRoot, c0 + Vector3.up * 2.45f + Vector3.back * 0.06f, new Vector3(1.8f, 0.18f, 0.22f), "M_AssiConsunte", qf, false);
                Box("UscitaSoglia", exitRoot, c0 + Vector3.up * 0.03f + Vector3.back * 0.1f, new Vector3(1.6f, 0.06f, 0.35f), "M_ArenariaBlocchi", qf, false);
            }

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_stracci";

            // camera nella bottega: orbita ridotta per stare sotto il soffitto da 3,5 m
            var vr = new GameObject("VolumiCamera").transform;
            VolumeCamera(vr, "CamVol_Bottega", new Vector3(0f, QB + HB * 0.5f, 41.5f), new Vector3(16f, HB, 13f), 0.72f, 56f, -0.6f, 0.7f);
            vr.Find("CamVol_Bottega").GetComponent<CameraProfileVolume>().dissolviMuri = false;
            VolumeCamera(vr, "CamVol_Soglia", new Vector3(0f, QB + HB * 0.5f, 33.5f), new Vector3(3.5f, HB, 3f), 0.6f, 58f, -0.5f, 0.5f);
            vr.Find("CamVol_Soglia").GetComponent<CameraProfileVolume>().dissolviMuri = false;

            Vector3 start = PT(0f, -27.5f);
            Spawn("inizio", start, 0f, spawns);

            SegretiStracci();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, sepiaFog, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno);
            SaveScene(scene, "40_FB_Stracci");
            Debug.Log("[Stracci] Scena costruita: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone), " + moduli.Count + " moduli di facciata.");
            quotaTerreno = null;
            pre = "Hub";
        }

        // coordinate dei dieci pezzi di stoffa (= flashback_stracci.json)
        static readonly Vector3[] StracciGettoni =
        {
            new Vector3(-1.6f, 0f, -14.5f), new Vector3(1.9f, 0f, -17.5f), new Vector3(8.0f, 0f, -14.2f), new Vector3(12.2f, 0f, -11.0f), new Vector3(-9.6f, 0f, -7.0f),
            new Vector3(-21.0f, 0f, -4.5f), new Vector3(-20.0f, 0f, -12.0f), new Vector3(10.0f, 0f, 6.2f), new Vector3(19.0f, 0f, 9.0f), new Vector3(22.0f, QB, 20.0f)
        };

        // ------------------------------------------------------------------ il vicolo

        static void ArredaVicoloStracci(Transform env, System.Random r)
        {
            propMesh.Clear();
            EnsureWaterMaterial();

            // insegna della bottega sopra la soglia e manifesto "si comprano stracci" sul muro del largo
            Box("Insegna_BottegaStracci", env, new Vector3(0f, QB + 3.05f, 31.88f), new Vector3(3.0f, 0.46f, 0.05f), "MP_BottegaStracci", Vector3.zero, false);
            foreach (var f in feats)
            {
                if (f.tipo != "cieco") continue;
                if (Mathf.Abs(f.wp.x + 6f) < 0.1f && Mathf.Abs(f.wp.z - 28f) < 0.1f)
                    Manifesto(env, "CompraStracci", f.wp + Vector3.Cross(f.n, Vector3.up) * 0.2f + Vector3.up * (QB + 1.7f), f.n, 0.9f, 1.35f, "Manifesto_CompraStracci");
                if (Mathf.Abs(f.wp.x - 6f) < 0.1f && Mathf.Abs(f.wp.z - 28f) < 0.1f)
                    Manifesto(env, "Treni", f.wp + Vector3.up * (QB + 1.7f), f.n, 0.62f, 0.93f, "Manifesto_Treni"); // il manifesto del filmato iniziale
            }
            foreach (var f in feats)
            {
                if (f.tipo != "basso") continue;
                WallLamp(env, f.wp, f.n, Vector3.Cross(f.n, Vector3.up), 1.25f);
                Negozio(env, f, r);
            }

            // lampioni e punti di riferimento
            Lamp(env, PT(-4.8f, -29f)); Lamp(env, PT(4.8f, -29f)); Lamp(env, PT(2.4f, -9f)); Lamp(env, PT(-2.4f, 5f)); Lamp(env, PT(2.4f, 20f)); Lamp(env, PT(-4.8f, 26f));
            Pezzo(env, "Pozzo", "Pozzo", new[] { "M_ArenariaBlocchi", "M_LegnoVernicePelata", "M_FerroRuggine02" }, Pozzo, PT(21.8f, 1f), 0f, 0.02f);
            Pezzo(env, "Lavatoio", "Lavatoio", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Lavatoio, PT(-25.1f, -9.5f), 90f, 0.02f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(-17.3f, -3.2f), 20f, 0.03f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(-17.2f, -2.2f), -10f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella1", new[] { "M_LegnoVernicePelata", "Tessuto1", "M_CassaLegno", "PannoOcra" }, Bancarella, PT(3.8f, -26.5f), 90f, 0.03f);
            Cart(env, PT(16.5f, 6.0f)); Cart(env, PT(-2.0f, 0f));

            // cortile del bucato: sedie, cesti di panni, secchi, basilico
            Sedia(env, PT(-19.0f, -11.5f), 20f); Sedia(env, PT(-18.2f, -11.8f), -30f);
            Model("CestaVimini", env, PT(-23.2f, -3.0f), 25f, 1.5f, true, "CestaPanni");
            Model("SecchioMetallo", env, PT(-24.5f, -12.5f), 10f, 1.1f, true, "Secchio");
            Model("SecchioLegno", env, PT(-17.4f, -6.0f), 0f, 1.1f, true, "Secchio");
            Basilico(env, PT(-18.0f, -1.9f), 0f); Basilico(env, PT(-24.8f, -2.2f), 0f);
            Bacinella(env, PT(-22.3f, -9.4f));

            // lotto bombardato: macerie, travi, muretti sbrecciati
            Rubble(env, r, PT(6.0f, -12f), 7, 1.1f); Rubble(env, r, PT(10.5f, -17.5f), 9, 1.3f); Rubble(env, r, PT(13.5f, -9.5f), 6, 1.0f); Rubble(env, r, PT(5.2f, -18.3f), 5, 0.8f);
            Box("Macerie_Muro", env, PT(8.5f, -19.4f, 0.7f), new Vector3(3.0f, 1.4f, 0.45f), "M_MuroRotto", new Vector3(0f, 8f, 0f));
            Box("Macerie_Muro", env, PT(14.4f, -14f, 0.55f), new Vector3(0.45f, 1.1f, 2.6f), "M_MuroRotto", new Vector3(0f, -6f, 0f));
            Model("SecchioMetallo", env, PT(9.2f, -9.6f), 0f, 1.1f, true, "Secchio");
            Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, PT(4.0f, -9.3f), 80f, 0.03f);
            MucchioStracci(env, r, PT(5.2f, -15.5f), 0.8f, 7);   // un cumulo di stracci tra i mattoni: non sono quelli da raccogliere (non hanno luce)

            // cortile dei carrettieri e terrazzino: carri, casse, sedie, panche
            Model("Carretto_Filip", env, PT(16.8f, 10.0f), 0f, 1.15f, true, "Carretto");
            Model("CassaLegno", env, PT(22.8f, 10.8f), 15f, 1.4f, true, "Cassa"); Model("CassaLegno", env, PT(22.4f, 3.5f), 80f, 1.3f, true, "Cassa");
            Model("BotteVino", env, PT(15.4f, -0.4f), 0f, 1.25f, true, "Botte");
            Sedia(env, PT(23.0f, 6.0f), 150f);
            Panchina(env, PT(20.0f, 21.4f), 0f);
            Model("TavoloLegno01", env, PT(17.5f, 18.5f), 0f, 1.3f, true, "Tavolo");
            Sedia(env, PT(17.5f, 19.5f), 180f); Sedia(env, PT(16.4f, 18.5f), 90f);
            Model("BotteVino", env, PT(24.6f, 16.2f), 0f, 1.25f, true, "Botte");
            Basilico(env, PT(24.6f, 20.8f), 0f); Basilico(env, PT(15.0f, 21.5f), 0f);
            Model("ScalaLegno", env, PT(14.4f, 22.6f), 90f, 1.1f, true, "Scala");

            // lungo le strade
            LungoLeStrade(env, r);

            // largo della bottega: banchi, casse, sacchi
            Model("CassaLegno", env, PT(-4.6f, 30.5f), 10f, 1.4f, true, "Cassa"); Sacco(env, PT(-5.0f, 28.8f), 25f); Sacco(env, PT(4.8f, 30.4f), -15f);
            Model("BotteVino", env, PT(4.9f, 25.5f), 0f, 1.25f, true, "Botte");
            Sedia(env, PT(-4.5f, 26f), 100f);

            // stendipanni sopra le vie
            var linee = new List<Vector3>();
            linee.Add(new Vector3(0, -2, 'x')); linee.Add(new Vector3(0, 10, 'x')); linee.Add(new Vector3(0, 23.2f, 'x'));
            linee.Add(new Vector3(-9, -7, 'z')); linee.Add(new Vector3(8.5f, 6, 'z'));
            PanniDaLista(env, r, linee);

            // i panni stesi sono stracci: gli stracci appesi alle finestre del cortile (decorazione)
            Pallone(env, PT(2.2f, -24.5f));
        }

        // cumulo di pezzi di stoffa (mucchio): una base bassa con collider e pezzi inclinati sopra
        static void MucchioStracci(Transform env, System.Random r, Vector3 c, float rad, int n)
        {
            var padre = new GameObject("MucchioStracci").transform;
            padre.SetParent(env, false);
            Box("MucchioStracci_Base", padre, c + Vector3.up * 0.25f, new Vector3(rad * 1.7f, 0.5f, rad * 1.5f), MatPanni[r.Next(MatPanni.Length)], new Vector3(0f, r.Next(360), 0f), true);
            for (int i = 0; i < n; i++)
            {
                float a = (float)(r.NextDouble() * Math.PI * 2.0), d = (float)r.NextDouble() * rad * 0.7f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * d, 0.5f + (float)r.NextDouble() * 0.28f, Mathf.Sin(a) * d);
                Box("MucchioStracci_Pezzo", padre, p, new Vector3(0.55f + (float)r.NextDouble() * 0.45f, 0.14f + (float)r.NextDouble() * 0.08f, 0.42f + (float)r.NextDouble() * 0.4f),
                    MatPanni[r.Next(MatPanni.Length)], new Vector3((float)(r.NextDouble() - 0.5) * 28f, r.Next(360), (float)(r.NextDouble() - 0.5) * 28f), false);
            }
        }

        // ------------------------------------------------------------------ la bottega

        static void CostruisciBottega(Transform env, System.Random r)
        {
            const string PL = "M_IntonacoMattoni";
            float y0 = QB, yc = QB + HB * 0.5f;
            var bt = new GameObject("Bottega").transform;
            bt.SetParent(env, false);
            // pareti interne (rivestono le facciate che il sistema a zone costruisce attorno): niente finestre del quartiere dentro la bottega
            Box("MuroBottega_Ovest", bt, new Vector3(-7.94f, yc, 41.5f), new Vector3(0.12f, HB, 13f), PL);
            Box("MuroBottega_Est", bt, new Vector3(7.94f, yc, 41.5f), new Vector3(0.12f, HB, 13f), PL);
            Box("MuroBottega_Nord", bt, new Vector3(0f, yc, 47.94f), new Vector3(16f, HB, 0.12f), PL);
            Box("MuroBottega_SudO", bt, new Vector3(-4.875f, yc, 35.06f), new Vector3(6.25f, HB, 0.12f), PL);
            Box("MuroBottega_SudE", bt, new Vector3(4.875f, yc, 35.06f), new Vector3(6.25f, HB, 0.12f), PL);
            Box("MuroSoglia_O", bt, new Vector3(-1.69f, yc, 33.5f), new Vector3(0.12f, HB, 3f), PL);
            Box("MuroSoglia_E", bt, new Vector3(1.69f, yc, 33.5f), new Vector3(0.12f, HB, 3f), PL);
            // soffitto con travi a vista (come il basso)
            Box("MuroSoffittoBottega", bt, new Vector3(0f, y0 + HB + 0.075f, 41.5f), new Vector3(16.3f, 0.15f, 13.3f), PL);
            Box("MuroSoffittoSoglia", bt, new Vector3(0f, y0 + HB + 0.075f, 33.5f), new Vector3(3.8f, 0.15f, 3.2f), PL);
            foreach (float zt in new[] { 36.6f, 38.9f, 41.2f, 43.5f, 45.8f })
                Box("AppesoTraveBottega", bt, new Vector3(0f, y0 + HB - 0.13f, zt), new Vector3(15.7f, 0.24f, 0.22f), "M_LegnoVernicePelata", null, false);
            foreach (float xt in new[] { -6f, 0f, 6f })
                Box("AppesoTraveLungaBottega", bt, new Vector3(xt, y0 + HB - 0.3f, 41.5f), new Vector3(0.24f, 0.2f, 12.8f), "M_LegnoVernicePelata", null, false);
            // zoccolo di tavole sulle pareti
            Box("AppesoDado", bt, new Vector3(-7.86f, y0 + 0.55f, 41.5f), new Vector3(0.04f, 1.1f, 12.8f), "M_AssiStagionate", null, false);
            Box("AppesoDado", bt, new Vector3(7.86f, y0 + 0.55f, 41.5f), new Vector3(0.04f, 1.1f, 12.8f), "M_AssiStagionate", null, false);
            Box("AppesoDado", bt, new Vector3(0f, y0 + 0.55f, 47.86f), new Vector3(15.8f, 1.1f, 0.04f), "M_AssiStagionate", null, false);
            // porta d'ingresso: stipiti e architrave sul filo del largo
            Box("SogliaStipite", bt, new Vector3(-1.9f, y0 + 1.5f, 31.95f), new Vector3(0.3f, 3.0f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaStipite", bt, new Vector3(1.9f, y0 + 1.5f, 31.95f), new Vector3(0.3f, 3.0f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaArchitrave", bt, new Vector3(0f, y0 + 2.85f, 31.95f), new Vector3(4.1f, 0.3f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaPietra", bt, new Vector3(0f, y0 + 0.02f, 31.9f), new Vector3(3.5f, 0.04f, 0.5f), "M_ArenariaBlocchi", null, false);

            // luci di lampada a petrolio: il look aggiunge una Point Light a ogni "LuceBottega"
            foreach (var p in new[] { new Vector3(-4.2f, 0f, 38.2f), new Vector3(4.2f, 0f, 38.2f), new Vector3(-4.2f, 0f, 44.4f), new Vector3(4.2f, 0f, 44.4f), new Vector3(0f, 0f, 41.5f), new Vector3(0f, 0f, 33.8f) })
            {
                var lg = Model("LanternaAppesa", bt, new Vector3(p.x, y0 + HB - 0.85f, p.z), 0f, 1.0f, false, "LanternaAppesa");
                var l = new GameObject("LuceBottega");
                l.transform.SetParent(lg.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Box("AppesoCatenaLanterna", bt, new Vector3(p.x, y0 + HB - 0.45f, p.z), new Vector3(0.025f, 0.6f, 0.025f), "M_FerroRuggine02", null, false);
            }

            // tavolo del banco di Capa 'e fierro e la sua sedia (la figura sta li' accanto)
            Model("TavoloLegno02", bt, new Vector3(StrCapa.x + 0.5f, QB, StrCapa.z - 0.2f), 90f, 1.2f, true, "Banco");
            Sedia(bt, new Vector3(StrCapa.x - 0.6f, QB, StrCapa.z + 0.1f), 90f);
            Model("LampadaPetrolio_A", bt, new Vector3(StrCapa.x + 0.5f, QB + 0.8f, StrCapa.z - 0.2f), 0f, 1.0f, false, "LampadaBanco");
            var lb = new GameObject("LuceBottega"); lb.transform.SetParent(bt, false); lb.transform.position = new Vector3(StrCapa.x + 0.5f, QB + 1.1f, StrCapa.z - 0.2f);
            // il punto dove sta Capa 'e fierro (la figura viene messa dal generatore del personaggio)
            var npc = new GameObject("Npc_CapaEFierro"); npc.transform.SetParent(bt, false);
            npc.transform.SetPositionAndRotation(StrCapa, Quaternion.Euler(0f, 100f, 0f));

            // montagne di stracci agli angoli nord, ferri vecchi, scaffali, carretto, ceste, sacchi
            MucchioStracci(bt, r, new Vector3(-6.2f, QB, 46.4f), 1.4f, 14);
            MucchioStracci(bt, r, new Vector3(6.4f, QB, 46.3f), 1.3f, 12);
            MucchioStracci(bt, r, new Vector3(-2.6f, QB, 46.6f), 0.9f, 8);
            MucchioStracci(bt, r, new Vector3(3.4f, QB, 36.9f), 1.0f, 8);
            // ferri vecchi: tubi, ruote, lamiere, una stufa
            Action<Vector3, float> ferri = (c, k) =>
            {
                var g = new GameObject("FerriVecchi").transform; g.SetParent(bt, false);
                Box("FerriVecchi_Base", g, c + Vector3.up * 0.22f, new Vector3(1.4f * k, 0.44f, 1.0f * k), "M_FerroRuggineGrossa", new Vector3(0f, r.Next(40), 0f), true);
                for (int i = 0; i < 6; i++)
                {
                    Vector3 p = c + new Vector3((float)(r.NextDouble() - 0.5) * 1.2f * k, 0.5f + (float)r.NextDouble() * 0.25f, (float)(r.NextDouble() - 0.5) * 0.8f * k);
                    if (i % 2 == 0) Cyl("FerriVecchi_Tubo", g, p, 0.07f + (float)r.NextDouble() * 0.05f, 0.9f, "M_FerroRuggine02", new Vector3(0f, r.Next(180), 90f), false);
                    else Box("FerriVecchi_Lamiera", g, p, new Vector3(0.6f, 0.03f, 0.4f), "M_FerroRuggineGrossa", new Vector3((float)(r.NextDouble() - 0.5) * 30f, r.Next(180), 0f), false);
                }
            };
            ferri(new Vector3(-6.8f, QB, 37.4f), 1.0f);
            ferri(new Vector3(6.9f, QB, 36.8f), 0.9f);
            ferri(new Vector3(-0.5f, QB, 38.6f), 0.7f);
            // ruote e padelle appese a un binario sul muro est
            Box("AppesoBinarioFerri", bt, new Vector3(7.8f, QB + 2.4f, 38.0f), new Vector3(0.05f, 0.05f, 4.0f), "M_FerroRuggine02", null, false);
            for (int i = 0; i < 6; i++)
            {
                float zp = 36.2f + i * 0.65f;
                Box("AppesoCordino", bt, new Vector3(7.8f, QB + 2.2f, zp), new Vector3(0.012f, 0.4f, 0.012f), "M_FerroRuggine02", null, false);
                if (i % 2 == 0) Model("PadellaRame", bt, new Vector3(7.6f, QB + 1.85f, zp), 90f, 1.0f, false, "Appeso_Padella");
                else Model("PentolaRame", bt, new Vector3(7.6f, QB + 1.85f, zp), 90f, 1.0f, false, "Appeso_Pentola");
            }
            // stracci appesi alle travi (pezzi buoni, ad asciugare)
            for (int i = 0; i < 9; i++)
            {
                Vector3 p = new Vector3(-6f + (i % 3) * 6f + (float)(r.NextDouble() - 0.5), QB + 2.4f - (i % 2) * 0.2f, 37.5f + (i / 3) * 4.2f);
                Box("Appeso_Straccio", bt, p, new Vector3(0.5f, 1.0f, 0.02f), MatPanni[r.Next(MatPanni.Length)], new Vector3(0f, r.Next(90), 0f), false);
            }
            // il carretto di Capa 'e fierro (segreto R011), casse, botti, sacchi, ceste, scala a pioli
            Model("Carretto_Filip", bt, StrCarretto, 90f, 1.05f, true, "CarrettoCapa");
            Model("ScalaLegno", bt, new Vector3(-7.5f, QB, 36.2f), 90f, 1.1f, true, "Scala");
            Model("CassaLegno", bt, new Vector3(-7.2f, QB, 43.8f), 0f, 1.4f, true, "Cassa"); Model("CassaLegno", bt, new Vector3(-7.2f, QB, 44.4f), 25f, 1.3f, true, "Cassa");
            Model("BotteVino", bt, new Vector3(7.3f, QB, 41.6f), 0f, 1.25f, true, "Botte");
            Sacco(bt, new Vector3(7.2f, QB, 45.0f), 20f); Sacco(bt, new Vector3(7.3f, QB, 45.7f), -25f); Sacco(bt, new Vector3(-3.6f, QB, 36.5f), 15f);
            Model("CestaVimini", bt, new Vector3(-4.0f, QB, 46.6f), 15f, 1.5f, true, "CestaStracci");
            Model("CestaVimini", bt, new Vector3(1.0f, QB, 46.8f), 40f, 1.5f, true, "CestaStracci");
            Model("SecchioMetallo", bt, new Vector3(-5.0f, QB, 38.4f), 0f, 1.1f, true, "Secchio");
            Model("Valigia", bt, new Vector3(3.8f, QB, 47.0f), 160f, 1.0f, true, "Valigia");
            Segreto("Mucchio di stracci della bottega", new Vector3(6.2f, QB + 0.55f, 46.3f), "Nel mucchio degli stracci di destra, in fondo alla bottega: un bottone di divisa dimenticato.");
            Segreto("Carretto di Capa 'e fierro", StrCarretto + Vector3.up * 0.9f, "Il carretto parcheggiato in fondo alla bottega: ha un cassetto sotto il sedile, che si apre girando la ruota.");
            Segreto("Cesta dei ferri vecchi", new Vector3(-6.8f, QB + 0.5f, 37.4f), "Tra i ferri vecchi dell'angolo sud-ovest: un tappo a corona schiacciato.");
        }

        // ------------------------------------------------------------------ segreti

        static void SegretiStracci()
        {
            Segreto("Lotto, tra i mattoni", PT(4.8f, -9.4f, 0.1f), "Fra i mattoni del lotto bombardato: un tappo a corona schiacciato.");
            Segreto("Cortile dei carrettieri, sotto il carro", PT(17.2f, 10.8f, 0.1f), "Sotto il carro del cortile dei carrettieri: un ritaglio di giornale piegato.");
            Segreto("Terrazzino, sul davanzale", PT(24.8f, 21.4f, 0.9f), "Sul davanzale del terrazzino: una carta da gioco dimenticata.");
            Segreto("Cortile del bucato, vicino al lavatoio", PT(-24.6f, -11.6f, 0.1f), "A terra vicino al lavatoio: una biglia di vetro.");
            Segreto("Banco d'esame del largo", StrS1 + Vector3.up * 0.9f, "Il banco degli stracci all'ingresso (enigma principale).");
            Segreto("Scala del terrazzino", PT(20f, 13.5f, 0.6f), "Le scale esterne che salgono al terrazzino: il giro che fa da scorciatoia verso il Vico Alto.");
            Segreto("Uscita dal ricordo", StrUscita, "La porta di servizio della bottega, sul muro nord, che riporta al presente.");
            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_stracci.json)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_giro", StrS1); riga("enigma_ceste", StrS2); riga("enigma_bilancia", StrS3); riga("enigma_conto", StrS4); riga("enigma_mestieri", StrS5);
            for (int i = 0; i < StracciGettoni.Length; i++) riga("straccio_" + (i + 1), StracciGettoni[i]);
            sb.AppendLine();
            sb.AppendLine("## Zone");
            sb.AppendLine();
            sb.AppendLine("| Zona | x0..x1 | z0..z1 | Quota |");
            sb.AppendLine("|------|--------|--------|-------|");
            foreach (var z in zone)
                sb.AppendLine("| " + z.nome + " | " + z.x0 + ".." + z.x1 + " | " + z.z0 + ".." + z.z1 + " | " + (z.rampa ? z.y + " -> " + z.y1 + " (scalinata)" : z.y.ToString()) + " |");
            ScriviDocSegreti(PuntiSegretiStracciPath, "Punti per i segreti del ricordo \"Stracci\" (R011/R012)", "40_FB_Stracci", null, sb.ToString());
        }
    }
}
