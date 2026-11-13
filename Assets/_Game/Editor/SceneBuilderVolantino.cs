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
    /// R013: il ricordo "Il volantino del treno" (50_FB_Volantino). Una sola scena, due zone senza caricamenti:
    ///  - il vicolo delle voci: lo stesso set del ricordo "Le scarpe" (stesse zone e stessi muri, decisione D1 dello schema) ma di sera e sotto la pioggia
    ///    (luoghi.json: meteo "pioggia"), con le botteghe chiuse, finestre accese, lampioni e la porta della sede aperta in fondo al largo in alto;
    ///  - la sede del Comitato: due stanze al pianterreno (la sala con bacheca, ciclostile e tavolo dell'accoglienza; la stanza dei registri con la scrivania del datario),
    ///    soffitto con travi, lampade a petrolio, manifesti nostri (testi_ambiente.json, chiavi Comitato*).
    /// Le postazioni degli enigmi e le voci stanno in flashback_volantino.json (ArtSource/Volantino/genera_flashback_volantino.py): le coordinate devono coincidere.
    /// Menu: Treno > Scene > Costruisci ricordo Volantino (anche dentro "Costruisci tutte le scene").
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiVolantinoPath = "Docs/richieste/media/R013/punti_segreti_volantino.md";
        const float QV = 0.9f;   // quota del largo in alto (piazzetta), della soglia e della sede
        const float HV = 3.5f;   // altezza interna della sede
        const float ZDivisorio = 50f; // muro fra la sala del Comitato e la stanza dei registri (porta in x -1..1)

        // postazioni del JSON (enigmi nel mondo)
        static readonly Vector3 VolV1 = new Vector3(-3.6f, 0f, -25.5f);
        static readonly Vector3 VolV2 = new Vector3(-7.55f, QV, 45.0f);
        static readonly Vector3 VolV3 = new Vector3(6.5f, QV, 44.5f);
        static readonly Vector3 VolV4 = new Vector3(0.0f, QV, 45.2f);
        static readonly Vector3 VolV5 = new Vector3(0.0f, QV, 56.0f);
        static readonly Vector3 VolLeggio = new Vector3(-6.9f, QV, 57.5f);
        static readonly Vector3 VolMaddalena = new Vector3(-5.0f, QV, 48.6f);
        static readonly Vector3 VolUscita = new Vector3(5.0f, QV, 59.88f);

        // le quattro voci: punto d'ascolto (= flashback_volantino.json, oggetti di enigma_voci)
        static readonly Vector3[] VolVoci =
        {
            new Vector3(1.8f, 0f, -9.5f), new Vector3(16.6f, 0f, -16.0f), new Vector3(-11.0f, 0f, 18.6f), new Vector3(-6.6f, QV, 30.5f)
        };

        [MenuItem("Treno/Scene/Costruisci ricordo Volantino (R013)")]
        public static void BuildVolantinoSolo()
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
            BuildFlashbackVolantino();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string path = ScenePath("50_FB_Volantino");
            if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("50_FB_Volantino");
            Debug.Log("[Volantino] Scena costruita: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        [MenuItem("Treno/Scene/Prova automatica ricordo Volantino (Play)")]
        public static void ProvaVolantino()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Volantino] Esci dal Play prima di avviare la prova."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("volantinotest", true);
            EditorSceneManager.OpenScene(ScenePath("00_Boot"));
            EditorApplication.isPlaying = true;
        }

        // ------------------------------------------------------------------ layout

        static void DefinisciZoneVolantino()
        {
            DefinisciZoneRicordo(); // il set del ricordo "Le scarpe" (D1): stesse vie, slarghi e cortili
            Z("SogliaSede", "strada", -1.75f, 1.75f, 37, 40, QV);
            Z("Sede", "cortile", -8, 8, 40, 60, QV);
        }

        static void DefinisciFeatureVolantino()
        {
            feats.Clear();
            zonePonte.Clear();
            // botteghe del ricordo "Le scarpe", di sera con le serrande accostate (nessuna insegna: le botteghe sono chiuse); interno accennato
            Fe("basso", -9, -3, 1, 0, 6, null, null).interno = 0;
            Fe("basso", -3, -15, 1, 0, 6, null, null).interno = 0;
            Fe("basso", 3, -6, -1, 0, 6, null, null).interno = 2;
            Fe("basso", 3, 0, -1, 0, 6, null, null).interno = 0;
            Fe("basso", -3, 8, 1, 0, 6, null, null).interno = 0;
            Fe("basso", 3, 16, -1, 0, 6, null, null).interno = 2;
            Fe("basso", -6, -27, 1, 0, 6, null, null).interno = 2;
            Fe("basso", 6, -27, -1, 0, 6, null, null).interno = 0;
            // chiusure dei vicoli ciechi: porte socchiuse da cui escono le voci
            Fe("portone", -16, 18, 1, 0, 4, null, null);
            Fe("cancello", 16, 8, -1, 0, 4, null, null);
            Fe("portone", 19, -12, -1, 0, 4, null, null);
            // muri ciechi: manifesti d'ingresso, graffito del cortile, edicole, muri del largo (il muro nord del largo ora ha la porta della sede)
            Fe("cieco", 0, -32, 0, 1, 6, null, null);               // manifesti d'ingresso
            Fe("cieco", 19, -17.5f, -1, 0, 4, null, null);          // muro del lavatoio
            Fe("cieco", -9, 20, 0, -1, 4, null, null);              // edicola, vicolo ovest
            Fe("cieco", 10, 10, 0, -1, 4, null, null);              // edicola, traversa est
            Fe("cieco", -8, 33, 1, 0, 6, null, null).piani = 3;     // muro ovest del largo
            Fe("cieco", 8, 29, -1, 0, 4, null, null).piani = 3;     // muro est del largo: manifesto del voto
        }

        // ------------------------------------------------------------------ scena

        static void BuildFlashbackVolantino()
        {
            var scene = NewScene();
            kitScene = "Volantino"; kitIndex = 0; pre = "Vol";
            var r = new System.Random(1946 + 13);
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            segreti.Clear();
            passanti.Clear();
            riservati.Clear();
            // sera di pioggia: nebbia grigio-azzurra, ambiente basso; il look (LookBuilder) rifinisce luci e nebbia
            Color fogSera = new Color(0.30f, 0.31f, 0.35f);
            Ambient(new Color(0.40f, 0.42f, 0.50f), new Color(0.30f, 0.30f, 0.34f), new Color(0.16f, 0.15f, 0.14f));
            Sun(new Color(0.70f, 0.74f, 0.88f), 0.35f, new Vector3(18f, -30f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogSera;
            RenderSettings.fogStartDistance = 8f;
            RenderSettings.fogEndDistance = 45f;

            EnsureLightMaterials();
            DefinisciZoneVolantino();
            RasterizzaZone();
            quotaTerreno = GroundY;
            DefinisciFeatureVolantino();
            foreach (var p in new[] { VolV1, VolV2, VolV3, VolV4, VolV5, VolLeggio, VolMaddalena })
                riservati.Add(new Vector2(p.x, p.z));
            foreach (var v in VolVoci) riservati.Add(new Vector2(v.x, v.z));

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo");
            CostruisciSuolo(env);
            CostruisciTetti(env);
            CostruisciFacciate(env, porte, r);
            ArredaVicoloVoci(env, r);
            CostruisciSede(env, r);
            RisolviSpazio("50_FB_Volantino");
            AssestaMacerie();
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            // uscita: porta sul retro della stanza dei registri (muro nord)
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, VolUscita + Vector3.up * 1.15f, new Vector3(1.3f, 2.3f, 0.1f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();
            {
                Vector3 c0 = VolUscita; var qf = Quaternion.identity.eulerAngles;
                Box("UscitaStipite", exitRoot, c0 + Vector3.right * 0.75f + Vector3.up * 1.2f + Vector3.back * 0.06f, new Vector3(0.16f, 2.4f, 0.2f), "M_AssiConsunte", qf, false);
                Box("UscitaStipite", exitRoot, c0 - Vector3.right * 0.75f + Vector3.up * 1.2f + Vector3.back * 0.06f, new Vector3(0.16f, 2.4f, 0.2f), "M_AssiConsunte", qf, false);
                Box("UscitaArchitrave", exitRoot, c0 + Vector3.up * 2.45f + Vector3.back * 0.06f, new Vector3(1.8f, 0.18f, 0.22f), "M_AssiConsunte", qf, false);
                Box("UscitaSoglia", exitRoot, c0 + Vector3.up * 0.03f + Vector3.back * 0.1f, new Vector3(1.6f, 0.06f, 0.35f), "M_ArenariaBlocchi", qf, false);
            }

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_volantino";

            // camera nella sede: orbita ridotta sotto il soffitto da 3,5 m (due stanze e la soglia)
            var vr = new GameObject("VolumiCamera").transform;
            VolumeCamera(vr, "CamVol_Sala", new Vector3(0f, QV + HV * 0.5f, 45f), new Vector3(16f, HV, 10f), 0.72f, 56f, -0.6f, 0.7f);
            vr.Find("CamVol_Sala").GetComponent<CameraProfileVolume>().dissolviMuri = false;
            VolumeCamera(vr, "CamVol_Registri", new Vector3(0f, QV + HV * 0.5f, 55f), new Vector3(16f, HV, 10f), 0.72f, 56f, -0.6f, 0.7f);
            vr.Find("CamVol_Registri").GetComponent<CameraProfileVolume>().dissolviMuri = false;
            VolumeCamera(vr, "CamVol_SogliaSede", new Vector3(0f, QV + HV * 0.5f, 38.5f), new Vector3(3.5f, HV, 3f), 0.6f, 58f, -0.5f, 0.5f);
            vr.Find("CamVol_SogliaSede").GetComponent<CameraProfileVolume>().dissolviMuri = false;

            Vector3 start = PT(0f, -29f);
            Spawn("inizio", start, 0f, spawns);

            SegretiVolantino();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, fogSera, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno);
            SaveScene(scene, "50_FB_Volantino");
            Debug.Log("[Volantino] Scena costruita: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone), " + moduli.Count + " moduli di facciata.");
            quotaTerreno = null;
            pre = "Hub";
        }

        // ------------------------------------------------------------------ il vicolo delle voci (sera, pioggia)

        static void ArredaVicoloVoci(Transform env, System.Random r)
        {
            propMesh.Clear();
            EnsureWaterMaterial();

            // botteghe chiuse per la sera (niente banchi fuori): resta accesa solo la lampada da muro
            foreach (var f in feats)
            {
                if (f.tipo != "basso") continue;
                WallLamp(env, f.wp, f.n, Vector3.Cross(f.n, Vector3.up), 1.25f);
            }

            // lampioni (di sera sono le luci principali), fontanella, pozzo, lavatoio delle voci
            Lamp(env, PT(-2.4f, -20f)); Lamp(env, PT(2.4f, -9.5f)); Lamp(env, PT(-2.4f, 5f)); Lamp(env, PT(2.4f, 20f));
            Lamp(env, PT(-5f, -30f)); Lamp(env, PT(5f, -30f)); Lamp(env, PT(-7f, 35.5f)); Lamp(env, PT(7f, 35.5f));
            Lamp(env, PT(14f, -11f)); Lamp(env, PT(-12.5f, 16.6f));
            Pezzo(env, "Fontanella", "Fontana", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Fontana, PT(-1f, 32f), 0f, 0.01f);
            Pezzo(env, "Pozzo", "Pozzo", new[] { "M_ArenariaBlocchi", "M_LegnoVernicePelata", "M_FerroRuggine02" }, Pozzo, PT(17.4f, -10.9f), 0f, 0.02f);
            Pezzo(env, "Lavatoio", "Lavatoio", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Lavatoio, PT(18.4f, -17.5f), 90f, 0.02f);
            Panchina(env, PT(-5.2f, 28.5f), 0f); Panchina(env, PT(5.8f, 35f), 0f);
            Cart(env, PT(-2.0f, 10f)); Cart(env, PT(12.5f, -19.0f));

            // cortile del lavatoio: secchi, cesta di panni bagnati, sedie (le donne si sono appena ritirate per la pioggia)
            Sedia(env, PT(10.5f, -10.6f), 30f); Sedia(env, PT(11.4f, -10.3f), -40f);
            Model("CestaVimini", env, PT(13.2f, -17.2f), 25f, 1.5f, true, "CestaPanni");
            Model("SecchioMetallo", env, PT(15.2f, -19.3f), 10f, 1.1f, true, "Secchio");
            Model("SecchioLegno", env, PT(14.4f, -19.0f), 0f, 1.1f, true, "Secchio");
            Bacinella(env, PT(14.4f, -14.2f));
            Basilico(env, PT(9.6f, -14.6f), 0f);

            // slargo e vicoli: casse, botti, sacchi sotto le tettoie
            Model("CassaLegno", env, PT(-8.2f, -7.3f), 10f, 1.4f, true, "Cassa");
            Model("CassaLegno", env, PT(-12.6f, 16.8f), 15f, 1.4f, true, "Cassa");
            Model("BotteVino", env, PT(-14.8f, 17.2f), 0f, 1.25f, true, "Botte");
            Sacco(env, PT(14.8f, 9.0f), 10f); Sacco(env, PT(14.2f, 6.8f), -20f);
            Model("CassaLegno", env, PT(15.2f, 7.8f), 0f, 1.3f, true, "Cassa");
            Basilico(env, PT(-8.3f, -0.8f), 0f);

            LungoLeStrade(env, r);
            Edicole(env);

            // manifesti: all'ingresso il manifesto dei treni (quello del filmato iniziale), sul muro est del largo il manifesto del voto
            int muri = 0;
            foreach (var f in feats)
            {
                if (f.tipo != "cieco") continue;
                var al = Vector3.Cross(f.n, Vector3.up);
                if (Mathf.Abs(f.wp.z + 32f) < 0.1f)
                {
                    muri |= 1;
                    Manifesto(env, "Treni", f.wp + al * 0.0f + Vector3.up * 1.75f, f.n, 0.62f, 0.93f, "Manifesto_Treni");
                    Manifesto(env, "ComitatoTreni", f.wp + al * -1.3f + Vector3.up * 1.7f, f.n, 0.62f, 0.93f, "Manifesto_ComitatoTreni");
                }
                else if (Mathf.Abs(f.wp.x - 8f) < 0.1f && Mathf.Abs(f.wp.z - 29f) < 0.1f)
                {
                    muri |= 2;
                    // strappato a meta': un lembo pende e un pezzo e' caduto a terra (collezionabile R011)
                    Manifesto(env, "ComitatoVoto", f.wp + al * 0.4f + Vector3.up * 1.75f, f.n, 0.62f, 0.93f, "Manifesto_ComitatoVoto");
                    Box("Manifesto_Lembo", env, f.wp + f.n * 0.02f + al * 0.15f + Vector3.up * 1.25f, new Vector3(0.24f, 0.32f, 0.004f), "PannoBeige",
                        (Quaternion.LookRotation(f.n, Vector3.up) * Quaternion.Euler(0f, 0f, 14f)).eulerAngles, false);
                }
            }
            if ((muri & 1) == 0) Debug.LogWarning("[Volantino] Muro cieco z=-32 non trovato: niente manifesti d'ingresso.");
            if ((muri & 2) == 0) Debug.LogWarning("[Volantino] Muro cieco (8, 29) non trovato: niente manifesto del voto.");

            // panni stesi dimenticati sotto la pioggia
            var linee = new List<Vector3>();
            linee.Add(new Vector3(0, -18, 'x')); linee.Add(new Vector3(0, 4, 'x')); linee.Add(new Vector3(-9, 18, 'z')); linee.Add(new Vector3(14, -10.5f, 'x'));
            PanniDaLista(env, r, linee);

        }

        // ------------------------------------------------------------------ la sede del Comitato

        static void CostruisciSede(Transform env, System.Random r)
        {
            const string PL = "M_IntonacoMattoni";
            float y0 = QV, yc = QV + HV * 0.5f;
            var sd = new GameObject("Sede").transform;
            sd.SetParent(env, false);
            // pareti interne (rivestono le facciate del sistema a zone): niente finestre del quartiere dentro la sede
            Box("MuroSede_Ovest", sd, new Vector3(-7.94f, yc, 50f), new Vector3(0.12f, HV, 20f), PL);
            Box("MuroSede_Est", sd, new Vector3(7.94f, yc, 50f), new Vector3(0.12f, HV, 20f), PL);
            Box("MuroSede_Nord", sd, new Vector3(0f, yc, 59.94f), new Vector3(16f, HV, 0.12f), PL);
            Box("MuroSede_SudO", sd, new Vector3(-4.875f, yc, 40.06f), new Vector3(6.25f, HV, 0.12f), PL);
            Box("MuroSede_SudE", sd, new Vector3(4.875f, yc, 40.06f), new Vector3(6.25f, HV, 0.12f), PL);
            Box("MuroSoglia_O", sd, new Vector3(-1.69f, yc, 38.5f), new Vector3(0.12f, HV, 3f), PL);
            Box("MuroSoglia_E", sd, new Vector3(1.69f, yc, 38.5f), new Vector3(0.12f, HV, 3f), PL);
            // divisorio fra la sala e la stanza dei registri, con la porta al centro (x -1..1, alta 2,3 m)
            Box("MuroDivisorio_O", sd, new Vector3(-4.5f, yc, ZDivisorio), new Vector3(7f, HV, 0.2f), PL);
            Box("MuroDivisorio_E", sd, new Vector3(4.5f, yc, ZDivisorio), new Vector3(7f, HV, 0.2f), PL);
            Box("MuroDivisorio_Sopraluce", sd, new Vector3(0f, y0 + 2.3f + (HV - 2.3f) * 0.5f, ZDivisorio), new Vector3(2f, HV - 2.3f, 0.2f), PL);
            Box("DivisorioStipite", sd, new Vector3(-1.05f, y0 + 1.15f, ZDivisorio), new Vector3(0.1f, 2.3f, 0.26f), "M_AssiConsunte", null, false);
            Box("DivisorioStipite", sd, new Vector3(1.05f, y0 + 1.15f, ZDivisorio), new Vector3(0.1f, 2.3f, 0.26f), "M_AssiConsunte", null, false);
            Box("DivisorioArchitrave", sd, new Vector3(0f, y0 + 2.36f, ZDivisorio), new Vector3(2.2f, 0.12f, 0.26f), "M_AssiConsunte", null, false);
            // soffitto con travi a vista
            Box("MuroSoffittoSede", sd, new Vector3(0f, y0 + HV + 0.075f, 50f), new Vector3(16.3f, 0.15f, 20.3f), PL);
            Box("MuroSoffittoSogliaSede", sd, new Vector3(0f, y0 + HV + 0.075f, 38.5f), new Vector3(3.8f, 0.15f, 3.2f), PL);
            foreach (float zt in new[] { 41.6f, 44.2f, 46.8f, 53.2f, 55.8f, 58.4f })
                Box("AppesoTraveSede", sd, new Vector3(0f, y0 + HV - 0.13f, zt), new Vector3(15.7f, 0.24f, 0.22f), "M_LegnoVernicePelata", null, false);
            // zoccolo di tavole
            Box("AppesoDado", sd, new Vector3(-7.86f, y0 + 0.55f, 50f), new Vector3(0.04f, 1.1f, 19.8f), "M_AssiStagionate", null, false);
            Box("AppesoDado", sd, new Vector3(7.86f, y0 + 0.55f, 50f), new Vector3(0.04f, 1.1f, 19.8f), "M_AssiStagionate", null, false);
            Box("AppesoDado", sd, new Vector3(0f, y0 + 0.55f, 59.86f), new Vector3(15.8f, 1.1f, 0.04f), "M_AssiStagionate", null, false);
            // porta d'ingresso sul largo: stipiti, architrave, soglia, un'insegna di cartone
            Box("SogliaStipite", sd, new Vector3(-1.9f, y0 + 1.5f, 36.95f), new Vector3(0.3f, 3.0f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaStipite", sd, new Vector3(1.9f, y0 + 1.5f, 36.95f), new Vector3(0.3f, 3.0f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaArchitrave", sd, new Vector3(0f, y0 + 2.85f, 36.95f), new Vector3(4.1f, 0.3f, 0.34f), "M_ArenariaCrepe", null, true);
            Box("SogliaPietra", sd, new Vector3(0f, y0 + 0.02f, 36.9f), new Vector3(3.5f, 0.04f, 0.5f), "M_ArenariaBlocchi", null, false);
            Manifesto(sd, "ComitatoTreni", new Vector3(2.6f, y0 + 1.7f, 36.98f), Vector3.back, 0.5f, 0.75f, "Manifesto_PortaSede");

            // luci: lampade a petrolio appese (il look aggiunge una Point Light a ogni "LuceBottega"), una finestra accesa sopra la porta
            foreach (var p in new[] { new Vector3(-4.2f, 0f, 42.6f), new Vector3(4.2f, 0f, 42.6f), new Vector3(-4.2f, 0f, 47.6f), new Vector3(4.2f, 0f, 47.6f),
                                       new Vector3(0f, 0f, 45.4f), new Vector3(-4f, 0f, 55f), new Vector3(4f, 0f, 55f), new Vector3(0f, 0f, 57.6f), new Vector3(0f, 0f, 38.6f) })
            {
                var lg = Model("LanternaAppesa", sd, new Vector3(p.x, y0 + HV - 0.85f, p.z), 0f, 1.0f, false, "LanternaAppesa");
                var l = new GameObject("LuceBottega");
                l.transform.SetParent(lg.transform, false);
                l.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                Box("AppesoCatenaLanterna", sd, new Vector3(p.x, y0 + HV - 0.45f, p.z), new Vector3(0.025f, 0.6f, 0.025f), "M_FerroRuggine02", null, false);
            }
            { var lp = new GameObject("LuceBottega"); lp.transform.SetParent(sd, false); lp.transform.position = new Vector3(0f, y0 + 3.2f, 36.4f); } // luce calda che esce dalla porta sul largo

            // ---- sala del Comitato: sedie spaiate, panca d'attesa, stufa, manifesti nostri, bacheca (enigma), ciclostile (enigma), tavolo dell'accoglienza (enigma)
            Panchina(sd, new Vector3(-3.0f, y0, 41.25f), 0f);
            Sedia(sd, new Vector3(-1.2f, y0, 46.3f), 175f); Sedia(sd, new Vector3(1.3f, y0, 46.4f), 190f); // dietro il tavolo dell'accoglienza: li' siedono le volontarie
            Sedia(sd, new Vector3(-6.4f, y0, 42.2f), 70f); Sedia(sd, new Vector3(4.6f, y0, 41.4f), 230f);
            Model("Tavolino", sd, new Vector3(5.6f, y0, 48.9f), 180f, 1.0f, true, "Tavolino");
            Model("SediaDipinta", sd, new Vector3(4.7f, y0, 48.8f), 100f, 1.0f, true, "Sedia");
            Model("CestaVimini", sd, new Vector3(7.3f, y0, 41.8f), 0f, 1.1f, true, "CestinoCarta");
            Manifesto(sd, "ComitatoVisite", new Vector3(7.88f, y0 + 1.75f, 47.6f), Vector3.left, 0.62f, 0.93f, "Manifesto_Visite");
            Manifesto(sd, "ComitatoVoto", new Vector3(-7.88f, y0 + 1.75f, 41.8f), Vector3.right, 0.62f, 0.93f, "Manifesto_VotoSala");
            Manifesto(sd, "Treni", new Vector3(3.2f, y0 + 1.8f, 49.88f), Vector3.back, 0.62f, 0.93f, "Manifesto_TreniSala");
            // Maddalena accanto alla bacheca (segnaposto: il modello 3D verra' con il suo generatore MakeHuman)
            var npc = new GameObject("Npc_Maddalena"); npc.transform.SetParent(sd, false);
            npc.transform.SetPositionAndRotation(VolMaddalena, Quaternion.Euler(0f, 200f, 0f));
            FiguraSegnaposto(npc.transform, "M_Panno_Grigio", "M_Panno_BluSpento", "M_Panno_Beige");

            // ---- stanza dei registri: scaffale con i registri, armadio delle visite, scrivania (enigma), leggio (segreto)
            Box("Scaffale_Fianco", sd, new Vector3(-7.6f, y0 + 1.1f, 52.75f), new Vector3(0.4f, 2.2f, 0.05f), "M_LegnoVernicePelata", null, true);
            Box("Scaffale_Fianco", sd, new Vector3(-7.6f, y0 + 1.1f, 55.25f), new Vector3(0.4f, 2.2f, 0.05f), "M_LegnoVernicePelata", null, true);
            // tre ripiani; quello alto (centro 2,08) ha il piano a 2,10 m: li' sta il segreto (x -7,6, z 53,5), quindi i registri in alto partono da z 54,2
            foreach (float yp in new[] { 0.06f, 0.6f, 1.18f })
                Box("Scaffale_Piano", sd, new Vector3(-7.6f, y0 + yp, 54f), new Vector3(0.4f, 0.04f, 2.45f), "M_LegnoVernicePelata", null, true);
            for (int i = 0; i < 8; i++)
            {
                float h = 0.3f + (float)r.NextDouble() * 0.06f;
                Box("Registro_Dorso", sd, new Vector3(-7.62f, y0 + 0.62f + h * 0.5f, 52.9f + i * 0.12f), new Vector3(0.3f, h, 0.09f), i % 3 == 0 ? "Cuoio" : "M_CassaLegno", new Vector3(0f, 0f, (float)(r.NextDouble() - 0.5) * 4f), false);
                Box("Registro_Dorso", sd, new Vector3(-7.62f, y0 + 1.2f + h * 0.5f, 54.2f + i * 0.12f), new Vector3(0.3f, h, 0.09f), i % 2 == 0 ? "Cuoio" : "M_CassaLegno", null, false);
            }
            Model("Credenza", sd, new Vector3(7.5f, y0, 54.5f), -90f, 1.0f, true, "ArmadioVisite");
            Model("Valigia", sd, new Vector3(7.0f, y0, 58.2f), 160f, 1.0f, true, "Valigia");
            Model("Valigia2", sd, new Vector3(6.9f, y0, 57.3f), 200f, 1.0f, true, "Valigia");
            Sedia(sd, new Vector3(0.2f, y0, 56.9f), 0f);
            Model("LampadaPetrolio_A", sd, new Vector3(-0.42f, y0 + 0.8f, 56.3f), 0f, 1.0f, false, "LampadaScrivania"); // sul piano della scrivania (enigma V5, piano a 0,80 m)
            var ls = new GameObject("LuceBottega"); ls.transform.SetParent(sd, false); ls.transform.position = new Vector3(-0.42f, y0 + 1.1f, 56.3f);
            Manifesto(sd, "ComitatoTreni", new Vector3(-3.4f, y0 + 1.8f, 59.88f), Vector3.back, 0.62f, 0.93f, "Manifesto_Registri");

            Segreto("Leggio dei registri", VolLeggio, "Il leggio contro la parete ovest della stanza dei registri: un registro con le impronte dei bambini.");
            Segreto("Cestino del ciclostile", new Vector3(6.6f, QV, 42.2f), "Accanto al cestino della carta, vicino al ciclostile: una matrice buttata.");
            Segreto("Scaffale dei registri", new Vector3(-7.6f, 2.1f, 53.5f), "Sul ripiano alto dello scaffale dei registri (parete ovest): un timbro vecchio.");
            Segreto("Sala, davanti alla panca", new Vector3(-3.0f, QV, 40.6f), "Davanti alla panca d'attesa, vicino alla porta della sede.");
        }

        /// <summary>Figura di primitive al posto di un personaggio non ancora modellato (come il segnaposto di Capa 'e fierro), con collider a capsula sul punto.</summary>
        static void FiguraSegnaposto(Transform punto, string matGonna, string matBusto, string matTesta)
        {
            Debug.LogWarning("[SceneBuilder] " + punto.name + ": personaggio 3D non ancora pronto, metto un segnaposto.");
            var g = new GameObject("Segnaposto").transform;
            g.SetParent(punto, false);
            Cyl("Segnaposto_Gonna", g, new Vector3(0f, 0.42f, 0f), 0.42f, 0.84f, matGonna, null, false);
            Cyl("Segnaposto_Busto", g, new Vector3(0f, 1.15f, 0f), 0.4f, 0.62f, matBusto, null, false);
            Prim(PrimitiveType.Sphere, "Segnaposto_Testa", g, new Vector3(0f, 1.6f, 0f), new Vector3(0.22f, 0.26f, 0.23f), matTesta, null, false);
            var cap = punto.gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.86f, 0f);
            cap.radius = 0.3f;
            cap.height = 1.72f;
        }

        // ------------------------------------------------------------------ segreti

        static void SegretiVolantino()
        {
            // l'ordine qui sotto e' quello del rapporto dei punti (Docs/richieste/media/R013/punti_segreti_volantino.md)
            var gia = new List<PuntoSegreto>(segreti);
            segreti.Clear();
            Segreto("Quaderno delle voci", VolV1, "Il quaderno sul muretto all'inizio del vicolo (enigma principale V1).");
            Segreto("Bacheca della sede", VolV2, "La bacheca di sughero sulla parete ovest della sala (enigma principale V2).");
            Segreto("Ciclostile della sede", VolV3, "Il ciclostile sul tavolo della parete est (enigma principale V3).");
            Segreto("Tavolo dell'accoglienza", VolV4, "Il tavolo lungo al centro della sala (enigma principale V4).");
            Segreto("Scrivania del datario", VolV5, "La scrivania della stanza dei registri (enigma principale V5).");
            foreach (var s in gia) if (s.nome == "Leggio dei registri" || s.nome == "Cestino del ciclostile" || s.nome == "Scaffale dei registri") segreti.Add(s);
            Segreto("Largo, sotto il manifesto strappato", new Vector3(7.2f, QV, 28.6f), "A terra sotto il manifesto del voto, sul muro est del largo in alto.");
            Segreto("Cortile, accanto al lavatoio", new Vector3(16.4f, 0f, -13.2f), "A terra nel cortile del lavatoio, verso il pozzo.");
            foreach (var s in gia) if (s.nome == "Sala, davanti alla panca") segreti.Add(s);
            Segreto("Uscita dal ricordo", VolUscita, "La porta sul retro della stanza dei registri, che riporta al presente.");
            foreach (var s in gia) if (s.nome != "Leggio dei registri" && s.nome != "Cestino del ciclostile" && s.nome != "Scaffale dei registri" && s.nome != "Sala, davanti alla panca") segreti.Add(s);

            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_volantino.json)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_voci", VolV1); riga("enigma_bacheca", VolV2); riga("enigma_ciclostile", VolV3); riga("enigma_ruoli", VolV4); riga("enigma_timbro", VolV5);
            string[] voci = { "voce_russia", "voce_tornano", "voce_male", "voce_forni" };
            for (int i = 0; i < VolVoci.Length; i++) riga(voci[i], VolVoci[i]);
            sb.AppendLine();
            sb.AppendLine("## Zone");
            sb.AppendLine();
            sb.AppendLine("| Zona | x0..x1 | z0..z1 | Quota |");
            sb.AppendLine("|------|--------|--------|-------|");
            foreach (var z in zone)
                sb.AppendLine("| " + z.nome + " | " + z.x0 + ".." + z.x1 + " | " + z.z0 + ".." + z.z1 + " | " + (z.rampa ? z.y + " -> " + z.y1 + " (scalinata)" : z.y.ToString()) + " |");
            ScriviDocSegreti(PuntiSegretiVolantinoPath, "Punti per i segreti del ricordo \"Il volantino del treno\" (R011/R013)", "50_FB_Volantino", null, sb.ToString());
        }
    }
}
