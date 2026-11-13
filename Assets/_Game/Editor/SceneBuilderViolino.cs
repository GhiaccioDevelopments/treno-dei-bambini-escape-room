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
    /// R015: il ricordo "Il violino" in una sola scena giocabile (decisione D1 dello schema): 70_FB_Modena, una corte di pianura nell'inverno 1946-47.
    ///  - la corte innevata (pozzo, pagliaio, carretto, alberi spogli, un pupazzo di neve) chiusa da un muretto, con il cancello a sud (uscita dal ricordo);
    ///  - a ovest la casa di Derna: cucina con la stufa accesa e la stanzetta di Amerigo (letto vero, cuscino, finestra sulla neve);
    ///  - a est la casa-laboratorio dei Benvenuti: il forno di Rosa, il banco dell'accordatore, il pianoforte verticale (gli oggetti degli enigmi li crea EnigmaMondo dal JSON);
    ///  - a nord la strada bianca fra due staccionate, fino alla scuola: un'aula sola con banchi, lavagna, stufa e carta d'Italia.
    /// Look diverso da Napoli: neve, nebbia bassa, luce fredda fuori e interni caldi (LookBuilder). Fiocchi di neve: un sistema di particelle leggero.
    /// Le postazioni degli enigmi stanno in flashback_violino.json (ArtSource/Violino/genera_flashback_violino.py): le coordinate devono coincidere.
    /// Menu: Treno > Scene > Costruisci ricordo Violino (anche dentro "Costruisci tutte le scene").
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiModenaPath = "Docs/richieste/media/R015/punti_segreti_modena.md";
        const float HCasa = 3.2f;        // altezza interna delle case e della scuola
        const float SpMuro = 0.5f;       // spessore dei muri delle case (mattoni intonacati)

        // postazioni del JSON (enigmi nel mondo)
        static readonly Vector3 VioV1 = new Vector3(9.6f, 0f, -6.7f);    // forno di Rosa (laboratorio, muro sud)
        static readonly Vector3 VioV2 = new Vector3(15.0f, 0f, -1.6f);   // banco dell'accordatore (laboratorio, muro est)
        static readonly Vector3 VioV3 = new Vector3(15.0f, 0f, 4.2f);    // pianoforte (laboratorio, muro est)
        static readonly Vector3 VioV4 = new Vector3(0f, 0f, 9.0f);       // impronte nella neve (imbocco della strada)
        static readonly Vector3 VioV5 = new Vector3(0f, 0f, 47.4f);      // cattedra della scuola
        static readonly Vector3 VioUscita = new Vector3(0f, 0f, -12.2f); // cancello della corte

        [MenuItem("Treno/Scene/Costruisci ricordo Violino (R015)")]
        public static void BuildViolinoSolo()
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
            BuildFlashbackModena();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string path = ScenePath("70_FB_Modena");
            if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("70_FB_Modena");
            Debug.Log("[Violino] Scena costruita: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        [MenuItem("Treno/Scene/Prova automatica ricordo Violino (Play)")]
        public static void ProvaViolino()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Violino] Esci dal Play prima di avviare la prova."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("violinotest", true);
            EditorSceneManager.OpenScene(ScenePath("00_Boot"));
            EditorApplication.isPlaying = true;
        }

        // ------------------------------------------------------------------ layout

        static void DefinisciZoneModena()
        {
            zone.Clear();
            Z("Corte", "neve", -6f, 6f, -12f, 12f, 0f);
            Z("SogliaDerna", "cotto", -6.6f, -5.9f, -3f, -1.6f, 0f);
            Z("CucinaDerna", "cotto", -15.5f, -6.5f, -7.5f, 1f, 0f);
            Z("PortaStanzetta", "legno", -9.4f, -8.4f, 0.9f, 1.4f, 0f);
            Z("Stanzetta", "legno", -15.5f, -6.5f, 1.3f, 7.5f, 0f);
            Z("SogliaLaboratorio", "legno", 5.9f, 6.6f, -0.7f, 0.7f, 0f);
            Z("Laboratorio", "legno", 6.5f, 15.5f, -7.5f, 7.5f, 0f);
            Z("Strada", "neve", -2.5f, 2.5f, 12f, 40f, 0f);
            Z("SogliaScuola", "legno", -0.7f, 0.7f, 39.9f, 40.6f, 0f);
            Z("Scuola", "legno", -5.5f, 5.5f, 40.5f, 49.5f, 0f);
        }

        static void BuildFlashbackModena()
        {
            var scene = NewScene();
            kitScene = "Modena"; kitIndex = 0; pre = "Mod";
            var r = new System.Random(1946 + 15);
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
            // mattina d'inverno in pianura: cielo bianco, luce fredda e diffusa, nebbia bassa che chiude l'orizzonte; il look (LookBuilder) rifinisce
            Color nebbia = new Color(0.78f, 0.79f, 0.80f);
            Ambient(new Color(0.74f, 0.76f, 0.80f), new Color(0.62f, 0.62f, 0.62f), new Color(0.5f, 0.5f, 0.5f));
            Sun(new Color(0.9f, 0.92f, 0.96f), 0.9f, new Vector3(22f, 150f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = nebbia;
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 55f;

            EnsureLightMaterials();
            DefinisciZoneModena();
            RasterizzaZone();
            quotaTerreno = (x, z) => GroundY(x, z, false);
            foreach (var p in new[] { VioV1, VioV2, VioV3, VioV4, VioV5, VioUscita }) riservati.Add(new Vector2(p.x, p.z));

            var env = new GameObject("Ambiente").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo");
            CostruisciSuolo(env);
            CostruisciCampagna(env, r);
            CostruisciCasaDerna(env);
            CostruisciLaboratorio(env);
            CostruisciStradaScuola(env, r);
            RisolviSpazio("70_FB_Modena");
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            Fiocchi(); // la neve che cade (runtime, non statica)

            // uscita: il cancello della corte, a sud
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, VioUscita + new Vector3(0f, 1.05f, -0.1f), new Vector3(1.9f, 2.0f, 0.08f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_violino";

            // camera: negli interni orbita stretta e muri che si dissolvono (come nel basso e nella bottega di Capa 'e fierro)
            var vr = new GameObject("VolumiCamera").transform;
            VolumeCamera(vr, "CamVol_CasaDerna", new Vector3(-11f, HCasa * 0.5f, 0f), new Vector3(9f, HCasa, 15f), 0.62f, 58f, -0.5f, 0.6f);
            VolumeCamera(vr, "CamVol_Laboratorio", new Vector3(11f, HCasa * 0.5f, 0f), new Vector3(9f, HCasa, 15f), 0.62f, 58f, -0.5f, 0.6f);
            VolumeCamera(vr, "CamVol_Scuola", new Vector3(0f, HCasa * 0.5f, 45f), new Vector3(11f, HCasa, 9f), 0.66f, 58f, -0.5f, 0.6f);

            Vector3 start = new Vector3(0f, 0f, -10f);
            Spawn("inizio", start, 0f, spawns);

            SegretiModena();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, nebbia, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno);
            SaveScene(scene, "70_FB_Modena");
            Debug.Log("[Violino] Modena costruita: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone).");
            quotaTerreno = null;
            pre = "Hub";
        }

        // ------------------------------------------------------------------ utilita'

        /// <summary>Muro pieno da (x0,z0) a (x1,z1) (allineato agli assi), alto h, con un vano (porta o finestra) opzionale fra u0 e u1 lungo il muro, da quota v0 a v1.</summary>
        static void MuroConVano(Transform p, string nome, float x0, float z0, float x1, float z1, float h, string mat, float u0 = -1f, float u1 = -1f, float v0 = 0f, float v1 = 2.2f)
        {
            bool lungoX = Mathf.Abs(x1 - x0) > Mathf.Abs(z1 - z0);
            float a0 = lungoX ? Mathf.Min(x0, x1) : Mathf.Min(z0, z1), a1 = lungoX ? Mathf.Max(x0, x1) : Mathf.Max(z0, z1);
            float c = lungoX ? (z0 + z1) * 0.5f : (x0 + x1) * 0.5f;
            Action<string, float, float, float, float> pezzo = (n, b0, b1, y0, y1) =>
            {
                if (b1 - b0 < 0.01f || y1 - y0 < 0.01f) return;
                var pos = lungoX ? new Vector3((b0 + b1) * 0.5f, (y0 + y1) * 0.5f, c) : new Vector3(c, (y0 + y1) * 0.5f, (b0 + b1) * 0.5f);
                var dim = lungoX ? new Vector3(b1 - b0, y1 - y0, SpMuro) : new Vector3(SpMuro, y1 - y0, b1 - b0);
                Box(n, p, pos, dim, mat);
            };
            if (u0 < 0f) { pezzo(nome, a0, a1, 0f, h); return; }
            pezzo(nome + "_A", a0, u0, 0f, h);
            pezzo(nome + "_B", u1, a1, 0f, h);
            pezzo(nome + "_Sotto", u0, u1, 0f, v0);
            pezzo(nome + "_Sopra", u0, u1, v1, h);
        }

        /// <summary>Tetto a due falde coperto di neve sopra una casa (x0..x1, z0..z1), colmo parallelo a z.</summary>
        static void TettoInnevato(Transform p, string nome, float x0, float x1, float z0, float z1, float h)
        {
            float w = (x1 - x0) * 0.5f + 0.4f, cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, L = z1 - z0 + 0.8f;
            float ang = 24f, lw = w / Mathf.Cos(ang * Mathf.Deg2Rad);
            float yc = h + 0.25f + Mathf.Tan(ang * Mathf.Deg2Rad) * w * 0.5f;
            Box(nome + "_FaldaO", p, new Vector3(cx - w * 0.5f, yc, cz), new Vector3(lw, 0.18f, L), "M_CoppiArgilla", new Vector3(0f, 0f, ang), false);
            Box(nome + "_FaldaE", p, new Vector3(cx + w * 0.5f, yc, cz), new Vector3(lw, 0.18f, L), "M_CoppiArgilla", new Vector3(0f, 0f, -ang), false);
            Box(nome + "_NeveO", p, new Vector3(cx - w * 0.5f, yc + 0.12f, cz), new Vector3(lw * 0.96f, 0.08f, L - 0.1f), "Neve", new Vector3(0f, 0f, ang), false);
            Box(nome + "_NeveE", p, new Vector3(cx + w * 0.5f, yc + 0.12f, cz), new Vector3(lw * 0.96f, 0.08f, L - 0.1f), "Neve", new Vector3(0f, 0f, -ang), false);
            Box("MuroSoffitto_" + nome, p, new Vector3(cx, h + 0.1f, cz), new Vector3(x1 - x0, 0.2f, z1 - z0), "M_AssiStagionate");
        }

        /// <summary>Stufa di ghisa con il tubo fino al soffitto; accesa = brace che brilla (e una luce calda: il look la aggiunge a "LuceBottega").</summary>
        static void Stufa(Transform p, string nome, Vector3 pos, bool accesa)
        {
            Cyl(nome, p, pos + new Vector3(0f, 0.42f, 0f), 0.55f, 0.84f, "M_FerroRuggineGrossa");
            Cyl(nome + "_Piano", p, pos + new Vector3(0f, 0.86f, 0f), 0.62f, 0.04f, "M_FerroRuggineGrossa", null, false);
            Cyl(nome + "_Tubo", p, pos + new Vector3(0f, 0.88f + (HCasa - 0.88f) * 0.5f, 0f), 0.13f, HCasa - 0.88f, "M_FerroRuggine02", null, false);
            Box(nome + "_Sportello", p, pos + new Vector3(0f, 0.35f, -0.28f), new Vector3(0.24f, 0.2f, 0.02f), "M_FerroRuggine02", null, false);
            if (accesa)
            {
                Box("Brace_" + nome, p, pos + new Vector3(0f, 0.35f, -0.295f), new Vector3(0.18f, 0.12f, 0.01f), "Glow", null, false);
                var l = new GameObject("LuceBottega");
                l.transform.SetParent(p, false);
                l.transform.position = pos + new Vector3(0f, 0.5f, -0.5f);
            }
        }

        static void Lampada(Transform p, Vector3 pos)
        {
            var lg = Model("LanternaAppesa", p, pos, 0f, 0.9f, false, "LanternaAppesa");
            Box("AppesoCatenaLanterna", p, pos + new Vector3(0f, (HCasa - pos.y) * 0.5f, 0f), new Vector3(0.025f, HCasa - pos.y, 0.025f), "M_FerroRuggine02", null, false);
            var l = new GameObject("LuceBottega");
            l.transform.SetParent(lg.transform, false);
            l.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        }

        static void AlberoSpoglio(Transform p, Vector3 pos, float h, System.Random r)
        {
            Cyl("Albero_Tronco", p, pos + new Vector3(0f, h * 0.35f, 0f), 0.28f, h * 0.7f, "M_AssiStagionate");
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f + (float)r.NextDouble() * 30f;
                Cyl("Albero_Ramo", p, pos + new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.5f, h * (0.62f + 0.06f * k), Mathf.Cos(a * Mathf.Deg2Rad) * 0.5f), 0.08f, h * 0.38f,
                    "M_AssiStagionate", new Vector3(35f, a, 0f), false);
            }
        }

        // ------------------------------------------------------------------ la campagna e la corte

        static void CostruisciCampagna(Transform env, System.Random r)
        {
            var ca = new GameObject("Campagna").transform;
            ca.SetParent(env, false);
            // i campi innevati tutto intorno (solo da vedere: niente collider, la nebbia li chiude)
            Box("CampiNeve", ca, new Vector3(0f, -0.03f, 18f), new Vector3(140f, 0.02f, 140f), "Neve", null, false);
            // muretto della corte con il cancello a sud (vano x -1..1) e l'imbocco della strada a nord (x -2,5..2,5)
            const string MU = "M_IntonacoMattoni";
            Box("MuroCorte_Sud_O", ca, new Vector3(-3.665f, 0.75f, -12.35f), new Vector3(4.67f, 1.5f, 0.3f), MU);
            Box("MuroCorte_Sud_E", ca, new Vector3(3.665f, 0.75f, -12.35f), new Vector3(4.67f, 1.5f, 0.3f), MU);
            Box("MuroCorte_Nord_O", ca, new Vector3(-4.25f, 0.75f, 12.15f), new Vector3(3.5f, 1.5f, 0.3f), MU);
            Box("MuroCorte_Nord_E", ca, new Vector3(4.25f, 0.75f, 12.15f), new Vector3(3.5f, 1.5f, 0.3f), MU);
            foreach (int lato in new[] { -1, 1 })
            {
                // fra le case e il muretto: tratti a nord e a sud delle case (le case vanno da z -8 a 8)
                Box("MuroCorte_Lato", ca, new Vector3(lato * 6.15f, 0.75f, -10.15f), new Vector3(0.3f, 1.5f, 4.1f), MU);
                Box("MuroCorte_Lato", ca, new Vector3(lato * 6.15f, 0.75f, 10.15f), new Vector3(0.3f, 1.5f, 4.1f), MU);
                Box("MuroCorte_NeveSopra", ca, new Vector3(lato * 6.15f, 1.53f, -10.15f), new Vector3(0.32f, 0.06f, 4.1f), "Neve", null, false);
                Box("MuroCorte_NeveSopra", ca, new Vector3(lato * 6.15f, 1.53f, 10.15f), new Vector3(0.32f, 0.06f, 4.1f), "Neve", null, false);
                // pilastri del cancello e le due ante aperte verso l'esterno
                Box("PilastroCancello", ca, new Vector3(lato * 1.15f, 1.0f, -12.35f), new Vector3(0.36f, 2.0f, 0.36f), "M_ArenariaBlocchi");
                Box("AppesoAntaCancello", ca, new Vector3(lato * 1.45f, 0.8f, -13.0f), new Vector3(0.05f, 1.4f, 1.1f), "M_FerroRuggineGrossa", new Vector3(0f, lato * 20f, 0f), false);
            }
            // nella corte: pozzo, pagliaio, carretto, legnaia, alberi spogli e il pupazzo di neve
            Cyl("Pozzo", ca, new Vector3(-3.6f, 0.45f, 3.2f), 1.2f, 0.9f, "M_ArenariaBlocchi");
            Cyl("Pozzo_Neve", ca, new Vector3(-3.6f, 0.91f, 3.2f), 1.15f, 0.04f, "Neve", null, false);
            Box("Pozzo_Trave", ca, new Vector3(-3.6f, 1.9f, 3.2f), new Vector3(1.3f, 0.08f, 0.08f), "M_AssiStagionate", null, false);
            foreach (float dx in new[] { -0.6f, 0.6f }) Box("Pozzo_Montante", ca, new Vector3(-3.6f + dx, 1.4f, 3.2f), new Vector3(0.08f, 1.0f, 0.08f), "M_AssiStagionate", null, false);
            Prim(PrimitiveType.Sphere, "Pagliaio", ca, new Vector3(-4.6f, 1.0f, 9.6f), new Vector3(2.0f, 2.4f, 2.0f), "Paglia");
            Prim(PrimitiveType.Sphere, "Pagliaio_Neve", ca, new Vector3(-4.6f, 1.9f, 9.6f), new Vector3(1.4f, 0.7f, 1.4f), "Neve", null, false);
            Model("Carretto_Filip", ca, new Vector3(4.3f, 0f, -9.5f), 200f, 1.0f, true, "Carretto");
            Box("Legnaia_Corte", ca, new Vector3(4.6f, 0.6f, 10.6f), new Vector3(1.8f, 1.2f, 0.8f), "M_CassaLegno");
            Box("Legnaia_Corte_Neve", ca, new Vector3(4.6f, 1.23f, 10.6f), new Vector3(1.8f, 0.06f, 0.8f), "Neve", null, false);
            AlberoSpoglio(ca, new Vector3(4.2f, 0f, 4.6f), 5.5f, r);
            AlberoSpoglio(ca, new Vector3(-4.4f, 0f, -6.2f), 4.8f, r);
            // il pupazzo di neve dei ragazzi
            Prim(PrimitiveType.Sphere, "PupazzoNeve_Base", ca, new Vector3(-2.2f, 0.35f, 7.2f), new Vector3(0.75f, 0.7f, 0.75f), "Neve");
            Prim(PrimitiveType.Sphere, "PupazzoNeve_Busto", ca, new Vector3(-2.2f, 0.9f, 7.2f), new Vector3(0.5f, 0.48f, 0.5f), "Neve", null, false);
            Prim(PrimitiveType.Sphere, "PupazzoNeve_Testa", ca, new Vector3(-2.2f, 1.3f, 7.2f), new Vector3(0.34f, 0.34f, 0.34f), "Neve", null, false);
            Cyl("PupazzoNeve_Naso", ca, new Vector3(-2.2f, 1.3f, 7.0f), 0.04f, 0.14f, "PannoOcra", new Vector3(90f, 0f, 0f), false);
            foreach (var f in new[] { new Vector3(-12f, 0f, 22f), new Vector3(10f, 0f, 26f), new Vector3(-9f, 0f, 34f), new Vector3(12f, 0f, 38f), new Vector3(-14f, 0f, -16f), new Vector3(14f, 0f, -18f) })
                AlberoSpoglio(ca, f, 6f + (float)r.NextDouble() * 2f, r);
            Segreto("Impronte nella neve", VioV4, "All'imbocco della strada, le impronte nella neve che portano verso la scuola (enigma principale V4).");
            Segreto("Dietro il pagliaio", new Vector3(-4.6f, 0f, 11.3f), "Fra il pagliaio e il muretto nord della corte: un pupazzetto di neve fatto da un bambino.");
            Segreto("Cancello della corte", VioUscita, "Il cancello a sud della corte: l'uscita dal ricordo.");
        }

        // ------------------------------------------------------------------ la casa di Derna (cucina e stanzetta)

        static void CostruisciCasaDerna(Transform env)
        {
            var ca = new GameObject("CasaDerna").transform;
            ca.SetParent(env, false);
            const string IN = "IntonacoOcra";
            // perimetro x -16..-6, z -8..8; porta sul muro est (z -3..-1,6), finestre su ovest e nord
            MuroConVano(ca, "MuroDerna_Est", -6.25f, -8f, -6.25f, 8f, HCasa, IN, -3f, -1.6f);
            MuroConVano(ca, "MuroDerna_Ovest", -15.75f, -8f, -15.75f, 8f, HCasa, IN, 3.8f, 5.2f, 1.0f, 2.0f);
            MuroConVano(ca, "MuroDerna_Sud", -15.5f, -7.75f, -6.5f, -7.75f, HCasa, IN, -12.5f, -11f, 1.0f, 2.0f);
            MuroConVano(ca, "MuroDerna_Nord", -15.5f, 7.75f, -6.5f, 7.75f, HCasa, IN, -11.8f, -10.4f, 1.0f, 2.0f);
            // tramezzo fra cucina e stanzetta (z 1,1), con la porta x -9,4..-8,4
            Box("MuroDerna_Tramezzo_A", ca, new Vector3(-12.45f, HCasa * 0.5f, 1.15f), new Vector3(6.1f, HCasa, 0.2f), IN);
            Box("MuroDerna_Tramezzo_B", ca, new Vector3(-7.45f, HCasa * 0.5f, 1.15f), new Vector3(1.9f, HCasa, 0.2f), IN);
            Box("MuroDerna_Tramezzo_Sopra", ca, new Vector3(-8.9f, 2.65f, 1.15f), new Vector3(1.0f, HCasa - 2.1f, 0.2f), IN);
            TettoInnevato(ca, "TettoDerna", -16f, -6f, -8f, 8f, HCasa);
            // vetri delle finestre (luce di neve) e davanzali
            Box("VetroFinestra", ca, new Vector3(-15.76f, 1.5f, 4.5f), new Vector3(0.04f, 1.0f, 1.4f), "M_LuceFinestra", null, false);
            Box("VetroFinestra", ca, new Vector3(-11.75f, 1.5f, -7.76f), new Vector3(1.5f, 1.0f, 0.04f), "M_LuceFinestra", null, false);
            Box("VetroFinestra", ca, new Vector3(-11.1f, 1.5f, 7.76f), new Vector3(1.4f, 1.0f, 0.04f), "M_LuceFinestra", null, false);
            Box("Davanzale", ca, new Vector3(-15.41f, 0.98f, 4.5f), new Vector3(0.18f, 0.05f, 1.5f), "M_LegnoVernicePelata", null, false);
            // cucina: stufa accesa, tavolo con le sedie, credenza, lampada
            Stufa(ca, "StufaDerna", new Vector3(-14.6f, 0f, -6.6f), true);
            Model("TavoloLegno01", ca, new Vector3(-11f, 0f, -4f), 0f, 1.0f, true, "TavoloCucina");
            Model("SediaDipinta", ca, new Vector3(-11.9f, 0f, -4f), 90f, 1.0f, true, "SediaCucina");
            Model("SediaDipinta", ca, new Vector3(-10.1f, 0f, -4f), 270f, 1.0f, true, "SediaCucina");
            Model("Credenza", ca, new Vector3(-15.3f, 0f, -2.0f), 90f, 1.0f, true, "CredenzaDerna");
            Model("PentolaRame", ca, new Vector3(-14.6f, 0.88f, -6.6f), 0f, 1.0f, false, "PentolaStufa");
            Lampada(ca, new Vector3(-11f, 2.3f, -4f));
            // stanzetta di Amerigo: letto vero con il cuscino, comodino, coperta
            Box("Letto_Telaio", ca, new Vector3(-13.6f, 0.25f, 5.2f), new Vector3(1.0f, 0.5f, 2.0f), "M_LegnoVernicePelata");
            Box("Letto_Materasso", ca, new Vector3(-13.6f, 0.56f, 5.2f), new Vector3(0.92f, 0.14f, 1.9f), "PannoBianco", null, false);
            Box("Letto_Coperta", ca, new Vector3(-13.6f, 0.64f, 4.9f), new Vector3(0.96f, 0.05f, 1.3f), "PannoTerra", null, false);
            Box("Letto_Cuscino", ca, new Vector3(-13.6f, 0.68f, 5.95f), new Vector3(0.6f, 0.12f, 0.3f), "PannoBianco", null, false);
            Box("Letto_Testiera", ca, new Vector3(-13.6f, 0.75f, 6.2f), new Vector3(1.0f, 1.0f, 0.06f), "M_LegnoVernicePelata", null, false);
            Box("Comodino", ca, new Vector3(-12.6f, 0.3f, 6.3f), new Vector3(0.45f, 0.6f, 0.4f), "M_LegnoVernicePelata");
            Model("Candela", ca, new Vector3(-12.6f, 0.6f, 6.3f), 0f, 1.0f, false, "CandelaComodino");
            Lampada(ca, new Vector3(-11f, 2.4f, 4.2f));
            Segreto("Tavolo della cucina di Derna", new Vector3(-11f, 0.8f, -4f), "Sul tavolo della cucina di Derna, accanto alla stufa: una cartolina.");
            Segreto("Davanzale della stanzetta", new Vector3(-15.4f, 1.0f, 4.5f), "Sul davanzale della finestra della stanzetta di Amerigo: un quaderno a righe.");
        }

        // ------------------------------------------------------------------ la casa-laboratorio dei Benvenuti

        static void CostruisciLaboratorio(Transform env)
        {
            var la = new GameObject("Laboratorio").transform;
            la.SetParent(env, false);
            const string IN = "IntonacoRosa";
            // perimetro x 6..16, z -8..8; porta sul muro ovest (z -0,7..0,7), finestre a nord e a sud-ovest
            MuroConVano(la, "MuroLab_Ovest", 6.25f, -8f, 6.25f, 8f, HCasa, IN, -0.7f, 0.7f);
            MuroConVano(la, "MuroLab_Est", 15.75f, -8f, 15.75f, 8f, HCasa, IN);
            MuroConVano(la, "MuroLab_Sud", 6.5f, -7.75f, 15.5f, -7.75f, HCasa, IN);
            MuroConVano(la, "MuroLab_Nord", 6.5f, 7.75f, 15.5f, 7.75f, HCasa, IN, 10.3f, 11.7f, 1.0f, 2.0f);
            TettoInnevato(la, "TettoLab", 6f, 16f, -8f, 8f, HCasa);
            Box("VetroFinestra", la, new Vector3(11f, 1.5f, 7.76f), new Vector3(1.4f, 1.0f, 0.04f), "M_LuceFinestra", null, false);
            Manifesto(la, "Laboratorio", new Vector3(5.98f, 2.6f, 0f), Vector3.left, 2.6f, 0.4f, "Insegna_Laboratorio");
            Manifesto(la, "Forno", new Vector3(9.6f, 2.3f, -7.48f), Vector3.forward, 0.9f, 0.2f, "Insegna_Forno");
            // canna fumaria del forno (il forno vero lo crea l'enigma V1) e mensole con le pagnotte
            Box("CannaFumaria", la, new Vector3(9.6f, 2.3f, -7.3f), new Vector3(0.5f, 1.8f, 0.4f), "M_IntonacoMattoni", null, false);
            Box("Mensola_Pane", la, new Vector3(7.0f, 1.4f, -7.35f), new Vector3(1.2f, 0.05f, 0.3f), "M_AssiStagionate", null, false);
            Box("Madia", la, new Vector3(7.4f, 0.4f, -5.2f), new Vector3(0.6f, 0.8f, 1.2f), "M_LegnoVernicePelata");
            Model("SecchioLegno", la, new Vector3(7.2f, 0f, -3.6f), 0f, 1.0f, true, "SecchioAcqua");
            Box("SaccoFarina", la, new Vector3(11.4f, 0.35f, -7.1f), new Vector3(0.6f, 0.7f, 0.45f), "M_TessutoIuta");
            // lato dell'accordatore: scaffale con le cassette degli attrezzi, una tastiera smontata, cassa di un pianoforte rotto
            Box("Scaffale", la, new Vector3(15.28f, 1.1f, -4.6f), new Vector3(0.4f, 2.2f, 1.4f), "M_LegnoVernicePelata");
            Model("CassaLegno", la, new Vector3(13.8f, 0f, -6.6f), 15f, 1.2f, true, "CassaAttrezzi");
            Box("TastieraSmontata", la, new Vector3(12.6f, 0.82f, 6.9f), new Vector3(1.3f, 0.06f, 0.24f), "M_LegnoVernicePelata", null, false);
            Box("Cavalletto", la, new Vector3(12.6f, 0.4f, 6.9f), new Vector3(1.2f, 0.8f, 0.3f), "M_AssiStagionate");
            Model("SediaDipinta", la, new Vector3(13.9f, 0f, 4.2f), 270f, 1.0f, true, "SgabelloPiano");
            Lampada(la, new Vector3(9.5f, 2.4f, -4f));
            Lampada(la, new Vector3(13.4f, 2.4f, 1.4f));
            Segreto("Forno di Rosa", VioV1, "Il forno a legna di Rosa contro il muro sud del laboratorio (enigma principale V1).");
            Segreto("Banco dell'accordatore", VioV2, "Il banco degli attrezzi di Alcide contro il muro est (enigma principale V2).");
            Segreto("Pianoforte", VioV3, "Il pianoforte verticale aperto contro il muro est (enigma principale V3).");
            Segreto("Sotto il banco dell'accordatore", new Vector3(14.2f, 0f, -3.2f), "A terra sotto il banco di Alcide, fra i trucioli: una corda di violino arrotolata.");
        }

        // ------------------------------------------------------------------ la strada e la scuola

        static void CostruisciStradaScuola(Transform env, System.Random r)
        {
            var st = new GameObject("StradaScuola").transform;
            st.SetParent(env, false);
            // staccionate ai lati della strada (sono anche il bordo percorribile)
            foreach (int lato in new[] { -1, 1 })
            {
                float x = lato * 2.65f;
                for (float z = 12.5f; z < 40f; z += 2f)
                    Box("Staccionata_Palo", st, new Vector3(x, 0.55f, z), new Vector3(0.1f, 1.1f, 0.1f), "M_AssiStagionate", null, false);
                Box("Staccionata_Traverso", st, new Vector3(x, 0.85f, 26f), new Vector3(0.06f, 0.08f, 28f), "M_AssiStagionate", null, false);
                Box("Staccionata_Traverso", st, new Vector3(x, 0.45f, 26f), new Vector3(0.06f, 0.08f, 28f), "M_AssiStagionate", null, false);
                InvisibleWall("MuroInvisibile_Strada", st, new Vector3(x, 0.7f, 26f), new Vector3(0.1f, 1.4f, 28f));
            }
            // la scuola: x -6..6, z 40..50; porta a sud (x -0,7..0,7), finestre a est e a ovest
            const string IN = "IntonacoGrigio";
            MuroConVano(st, "MuroScuola_Sud", -6f, 40.25f, 6f, 40.25f, HCasa, IN, -0.7f, 0.7f);
            MuroConVano(st, "MuroScuola_Nord", -6f, 49.75f, 6f, 49.75f, HCasa, IN);
            MuroConVano(st, "MuroScuola_Ovest", -5.75f, 40.5f, -5.75f, 49.5f, HCasa, IN, 41.4f, 43f, 1.0f, 2.2f);
            MuroConVano(st, "MuroScuola_Est", 5.75f, 40.5f, 5.75f, 49.5f, HCasa, IN, 43.2f, 44.8f, 1.0f, 2.2f);
            TettoInnevato(st, "TettoScuola", -6f, 6f, 40f, 50f, HCasa);
            Box("VetroFinestra", st, new Vector3(-5.76f, 1.6f, 42.2f), new Vector3(0.04f, 1.2f, 1.6f), "M_LuceFinestra", null, false);
            Box("VetroFinestra", st, new Vector3(5.76f, 1.6f, 44f), new Vector3(0.04f, 1.2f, 1.6f), "M_LuceFinestra", null, false);
            Manifesto(st, "ScuolaElementare", new Vector3(0f, 2.75f, 39.98f), Vector3.back, 2.6f, 0.42f, "Insegna_Scuola");
            Manifesto(st, "Lavagna", new Vector3(-2.4f, 1.6f, 49.48f), Vector3.back, 1.8f, 1.2f, "Manifesto_Lavagna");
            Manifesto(st, "Alfabeto", new Vector3(5.48f, 2.2f, 47.2f), Vector3.left, 1.2f, 0.8f, "Manifesto_Alfabeto");
            // banchi a due posti in tre file (corridoio centrale libero fino alla cattedra), stufa nell'angolo
            for (int fila = 0; fila < 3; fila++)
                foreach (float x in new[] { -3.2f, 3.2f })
                {
                    float z = 41.8f + fila * 1.6f;
                    Box("Banco_Piano", st, new Vector3(x, 0.72f, z), new Vector3(1.6f, 0.05f, 0.5f), "M_LegnoVernicePelata");
                    Box("Banco_Sedile", st, new Vector3(x, 0.42f, z - 0.45f), new Vector3(1.6f, 0.04f, 0.3f), "M_LegnoVernicePelata", null, false);
                    Box("Banco_Fianco", st, new Vector3(x - 0.75f, 0.36f, z - 0.2f), new Vector3(0.05f, 0.72f, 0.8f), "M_FerroRuggine02", null, false);
                    Box("Banco_Fianco", st, new Vector3(x + 0.75f, 0.36f, z - 0.2f), new Vector3(0.05f, 0.72f, 0.8f), "M_FerroRuggine02", null, false);
                }
            Stufa(st, "StufaScuola", new Vector3(4.7f, 0f, 48.7f), true);
            Lampada(st, new Vector3(-2f, 2.4f, 44.5f));
            Lampada(st, new Vector3(2f, 2.4f, 44.5f));
            // la legnaia della scuola (dove Rivo ha nascosto il violino), fuori, accanto al muro ovest
            Box("LegnaiaScuola", st, new Vector3(-4.0f, 0.7f, 38.6f), new Vector3(2.4f, 1.4f, 1.6f), "M_CassaLegno");
            Box("LegnaiaScuola_Tetto", st, new Vector3(-4.0f, 1.5f, 38.6f), new Vector3(2.6f, 0.08f, 1.8f), "Neve", new Vector3(8f, 0f, 0f), false);
            Segreto("Cattedra della scuola", VioV5, "La cattedra in fondo all'aula con il violino e lo spartito (enigma principale V5).");
            Segreto("Parete della carta d'Italia", new Vector3(-5.5f, 0f, 46.0f), "Sulla parete ovest dell'aula: la carta d'Italia appesa sopra la stufa spenta di riserva (enigma segreto).");
            Segreto("Banco di Amerigo", new Vector3(3.2f, 0.75f, 43.4f), "Sul banco della seconda fila a destra: la pagella di Amerigo.");
        }

        /// <summary>Fiocchi di neve: un sistema di particelle leggero sopra la corte e la strada (non statico, poche centinaia di fiocchi).</summary>
        static void Fiocchi()
        {
            var go = new GameObject("NeveCade");
            go.transform.position = new Vector3(0f, 9f, 18f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 10f;
            main.startLifetime = 16f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = 0.035f;
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 55f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(34f, 0.5f, 70f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.25f, -0.4f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.3f;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = MaterialeFiocchi();
        }

        static Material MaterialeFiocchi()
        {
            const string path = "Assets/_Game/Generated/M_FiocchiNeve.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) { Debug.LogWarning("[Violino] Shader delle particelle URP non trovato: fiocchi senza materiale."); return null; }
            m = new Material(sh);
            m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.85f));
            m.SetFloat("_Surface", 1f); // trasparente
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ segreti

        static void SegretiModena()
        {
            // ordine del rapporto dei punti (Docs/richieste/media/R015/punti_segreti_modena.md): enigmi, uscita, poi i collezionabili e l'enigma segreto
            var gia = new List<PuntoSegreto>(segreti);
            segreti.Clear();
            foreach (var n in new[] { "Forno di Rosa", "Banco dell'accordatore", "Pianoforte", "Impronte nella neve", "Cattedra della scuola", "Cancello della corte",
                                      "Parete della carta d'Italia", "Banco di Amerigo", "Davanzale della stanzetta", "Dietro il pagliaio",
                                      "Sotto il banco dell'accordatore", "Tavolo della cucina di Derna" })
                foreach (var s in gia) if (s.nome == n) segreti.Add(s);
            foreach (var s in gia) if (!segreti.Contains(s)) segreti.Add(s);
            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_violino.json, scena 70_FB_Modena)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_forno", VioV1); riga("enigma_attrezzi", VioV2); riga("enigma_pianoforte", VioV3); riga("enigma_impronte", VioV4); riga("enigma_melodia", VioV5);
            ScriviDocSegreti(PuntiSegretiModenaPath, "Punti per i segreti del ricordo \"Il violino\": Modena (R011/R015)", "70_FB_Modena",
                "corte x -6..6 z -12..12; casa di Derna x -16..-6 z -8..8 (cucina z < 1, stanzetta z > 1); laboratorio x 6..16 z -8..8; strada x -2,5..2,5 z 12..40; scuola x -6..6 z 40..50.",
                sb.ToString());
        }
    }
}
