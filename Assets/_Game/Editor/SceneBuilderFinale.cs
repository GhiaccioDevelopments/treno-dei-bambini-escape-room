using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Puzzles;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R016: il finale nel basso di Antonietta nel 1994 (scena 80_Basso_1994). Stesso impianto del basso (BuildBasso con Anno1994 = true: niente lume sul tavolo,
    /// niente scarpe, porta sul vicolo come uscita, lampadina nuda fredda) + arredo del 1994 (lenzuola sui mobili, scatoloni, televisore a tubo, radio, lampadina)
    /// + la stanza a due tempi (DueTempi: per ogni posto dei ricordi una versione 1946 accesa solo per qualche secondo, luci dinamiche locali, nessun nuovo bake).
    /// Le postazioni degli enigmi stanno in flashback_finale.json (ArtSource/Finale/genera_flashback_finale.py): le coordinate devono coincidere.
    /// Menu: Treno > Scene > Costruisci finale 1994 (anche dentro "Costruisci tutte le scene").
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiFinalePath = "Docs/richieste/media/R016/punti_segreti_finale.md";

        /// <summary>R016: true mentre BuildBasso costruisce la variante 1994.</summary>
        static bool Anno1994;

        static readonly Vector3 FinN1 = new Vector3(3.6f, 0f, -1.95f);
        static readonly Vector3 FinScarpe = new Vector3(0.6f, 0f, -2.0f);
        static readonly Vector3 FinStracci = new Vector3(3.7f, 0f, 10.3f);
        static readonly Vector3 FinVolantino = new Vector3(-3.98f, 0.965f, 1.98f);
        static readonly Vector3 FinCappotto = new Vector3(1.15f, 1.1f, -2.3f);
        static readonly Vector3 FinSpartito = new Vector3(-3.85f, 0.62f, 7.35f);
        static readonly Vector3 FinN3 = new Vector3(2.3f, 0.66f, 1.2f);
        static readonly Vector3 FinN4 = new Vector3(-3.85f, 0f, 6.0f);
        static readonly Vector3 FinN5 = new Vector3(-1.3f, 0f, -1.95f);

        [MenuItem("Treno/Scene/Costruisci finale 1994 (R016)")]
        public static void BuildFinaleSolo()
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
            BuildFinale1994();
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string path = ScenePath("80_Basso_1994");
            if (!list.Exists(s => s.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("80_Basso_1994");
            Debug.Log("[Finale] Scena costruita: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        [MenuItem("Treno/Scene/Prova automatica finale (Play)")]
        public static void ProvaFinale()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Finale] Esci dal Play prima di avviare la prova."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool("finaletest", true);
            EditorSceneManager.OpenScene(ScenePath("00_Boot"));
            EditorApplication.isPlaying = true;
        }

        static void BuildFinale1994()
        {
            Anno1994 = true;
            try { BuildBasso(); }
            finally { Anno1994 = false; }
        }

        /// <summary>Lenzuolo bianco steso su un mobile (scatola un po' piu' grande, con l'orlo che pende).</summary>
        static void Lenzuolo(Transform p, Vector3 centro, Vector3 dim, float yaw)
        {
            Box("Lenzuolo", p, centro + Vector3.up * (dim.y * 0.5f + 0.01f), new Vector3(dim.x + 0.08f, 0.02f, dim.z + 0.08f), "PannoBianco", new Vector3(0f, yaw, 0f), false);
            Box("Lenzuolo_Orlo", p, centro + new Vector3(0f, dim.y * 0.25f, 0f), new Vector3(dim.x + 0.1f, dim.y * 0.5f, dim.z + 0.1f), "PannoBianco", new Vector3(0f, yaw, 0f), false);
        }

        /// <summary>R016: l'arredo del 1994 sopra il basso (chiamato da BuildBasso quando Anno1994), l'uscita, il controller degli enigmi e la stanza a due tempi.</summary>
        static void Arreda1994(Transform env, Transform props)
        {
            var a94 = new GameObject("Arredo1994").transform;
            a94.SetParent(props, false);
            // mobili sotto le lenzuola (la credenza della stanza resta scoperta: ci va il volantino), scatoloni, televisore a tubo, radio, lampadina nuda
            Lenzuolo(a94, new Vector3(4.15f, 0.25f, -0.6f), new Vector3(0.9f, 0.5f, 0.5f), 90f);     // la cassapanca
            Lenzuolo(a94, new Vector3(1.85f, 0.45f, 7.2f), new Vector3(0.5f, 0.9f, 1.2f), 90f);      // la credenza della cucina
            foreach (var sc in new[] { new Vector4(-3.4f, -1.8f, 10f, 0.5f), new Vector4(-2.8f, -1.95f, -15f, 0.4f), new Vector4(-3.1f, -1.3f, 30f, 0.35f), new Vector4(3.9f, 4.6f, 5f, 0.45f) })
                Box("Scatolone", a94, new Vector3(sc.x, sc.w * 0.5f, sc.y), new Vector3(sc.w * 1.2f, sc.w, sc.w), "PannoOcra", new Vector3(0f, sc.z, 0f));
            Box("Televisore", a94, new Vector3(-1.0f, 0.55f, 3.2f), new Vector3(0.55f, 0.45f, 0.45f), "M_FerroRuggineGrossa");
            Box("Televisore_Schermo", a94, new Vector3(-1.0f, 0.57f, 2.97f), new Vector3(0.44f, 0.34f, 0.01f), "Finestra", null, false);
            Box("Televisore_Mobile", a94, new Vector3(-1.0f, 0.16f, 3.2f), new Vector3(0.7f, 0.32f, 0.5f), "M_LegnoVernicePelata");
            Cyl("Lampadina", a94, new Vector3(2.3f, 2.55f, 1.2f), 0.07f, 0.1f, "Glow", null, false);
            Box("AppesoFiloLampadina", a94, new Vector3(2.3f, 2.8f, 1.2f), new Vector3(0.01f, 0.45f, 0.01f), "M_FerroRuggine02", null, false);
            Spawn("inizio", new Vector3(0f, 0f, -1.2f), 0f, GameObject.Find("SpawnPoints").transform);

            // uscita: la porta sul vicolo (si apre quando tutti gli enigmi del finale sono risolti)
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, new Vector3(0f, 1.05f, -2.3f), new Vector3(1.0f, 2.0f, 0.06f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();
            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_finale";

            // la stanza a due tempi: per ogni posto un angolo del 1946 (spento), acceso dagli enigmi
            var dt = new GameObject("DueTempi").AddComponent<DueTempi>();
            dt.chiaveTutti = "flashback_finale/enigma_custodia";
            var lc = GameObject.Find("Luce Calda");
            dt.luce1994 = lc != null ? lc.GetComponent<Light>() : null;
            Angolo1946(dt, "scarpe", FinScarpe, new Vector3(1.2f, 0f, -1.2f), 200f);
            Angolo1946(dt, "stracci", FinStracci, new Vector3(4.6f, 0f, 11.2f), 220f);
            Angolo1946(dt, "volantino", FinVolantino, new Vector3(-3.1f, 0f, 2.6f), 250f);
            Angolo1946(dt, "cappotto", FinCappotto, new Vector3(1.9f, 0f, -1.6f), 160f);
            Angolo1946(dt, "spartito", FinSpartito, new Vector3(-2.4f, 0f, 6.4f), 300f);
        }

        /// <summary>Un angolo del 1946: luce calda di lume a petrolio (dinamica, senza ombre), il lume sul posto e Antonietta di spalle (figura di primitive). Non statico.</summary>
        static void Angolo1946(DueTempi dt, string id, Vector3 posto, Vector3 figura, float yaw)
        {
            var root = new GameObject("Tempo1946_" + id);
            var lg = new GameObject("Luce1946");
            lg.transform.SetParent(root.transform, false);
            lg.transform.position = new Vector3(posto.x, Mathf.Max(1.6f, posto.y + 0.9f), posto.z + 0.4f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.68f, 0.38f); l.intensity = 0f; l.range = 3.5f; l.shadows = LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            Prim(PrimitiveType.Sphere, "Fiamma1946", root.transform, lg.transform.position + Vector3.down * 0.3f, new Vector3(0.05f, 0.09f, 0.05f), "Glow", null, false);
            FiguraFolla(root.transform, "Antonietta1946_" + id, figura, yaw, 1.0f, false, "M_Panno_Grigio", "M_Panno_Terra", "M_Panno_Beige");
            foreach (var c in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c); // nel ricordo non si sbatte contro nessuno
            root.SetActive(false);
            dt.angoli.Add(new DueTempi.Angolo { chiave = "flashback_finale/enigma_posto_" + id, radice = root, luce = l });
        }

        static void SegretiFinale()
        {
            segreti.Clear();
            Segreto("Il telefono e la latta", FinN1, "Mobiletto con il telefono, il calendario 1994 e la scatola di latta (enigma principale N1).");
            Segreto("Il tavolo sotto la lampadina", FinN3, "La ricevuta da ricomporre (enigma principale N3).");
            Segreto("Sotto il letto", FinN4, "La custodia del violino sotto il letto di ferro (enigma principale N4).");
            Segreto("La porta sul vicolo", FinN5, "Carmine sulla soglia (enigma principale N5).");
            Segreto("La radio sul mobile", new Vector3(-1.0f, 0.8f, 3.25f), "Sopra il televisore, una radio sintonizzata male: la frequenza giusta e' segnata a matita sul mobile (enigma segreto).");
            Segreto("Davanti al televisore", new Vector3(-1.0f, 0f, 2.6f), "A terra davanti al televisore: un ritaglio di giornale con la fotografia di Amerigo in frac.");
            Segreto("Accanto al telefono", new Vector3(3.2f, 0f, -1.6f), "A terra accanto al mobiletto del telefono: un gettone telefonico.");
            Segreto("Sugli scatoloni", new Vector3(-3.4f, 0.5f, -1.8f), "Sopra lo scatolone piu' grande: un biglietto del treno del 1994.");
            Segreto("Mensola della cucina", new Vector3(3.0f, 0f, 5.0f), "Sul pavimento della cucina, vicino agli scatoloni: un santino nuovo.");
            Segreto("Davanzale del cortile", new Vector3(5.6f, 0f, 9.2f), "Nel cortile, ai piedi del muro: un ex-voto a forma di piede, come quello dell'edicola del vicolo.");
            var sb = new StringBuilder();
            sb.AppendLine("## Postazioni degli enigmi (flashback_finale.json, scena 80_Basso_1994)");
            ScriviDocSegreti(PuntiSegretiFinalePath, "Punti per i segreti del finale: il basso nel 1994 (R011/R016)", "80_Basso_1994",
                "stesso impianto del basso: stanza x -4,5..4,5 z -2,5..3,5; alcova x -4,5..-1,5 z 3,8..7,8; cucina x 1,5..4,5 z 3,8..7,8; cortile x 1,2..6,3 z 8,1..14,1.", sb.ToString());
        }
    }
}
