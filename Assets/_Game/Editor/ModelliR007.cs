using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R007 tappa 2: importa i modelli 3D veri (preparati da ArtSource/Models con Blender e PIL), crea i materiali URP Lit uniformati
    /// (albedo, normal, mask con metallico/occlusione/levigatezza), le varianti (pentole rame, bacinella azzurra) e la scena di prova
    /// Assets/_Game/Scenes/Prove/R007_Modelli.unity con i confronti "primitiva attuale contro modello nuovo".
    /// NON sostituisce i modelli nelle scene del gioco (lo fa la parte B dopo l'ok dell'utente).
    /// Menu: Treno/R007/...
    /// </summary>
    public static class ModelliR007
    {
        const string ModelRoot = "Assets/Art/Models";
        const string MatRoot = "Assets/Art/Materials";
        const string ScenaProva = "Assets/_Game/Scenes/Prove/R007_Modelli.unity";
        static readonly Color EpochTint = new Color(0.95f, 0.91f, 0.84f, 1f); // come AssetImportHelper: uniforma i colori d'epoca

        // modelli senza texture propria: materiale gia' nel progetto (nome FBX/materiale -> asset)
        static readonly Dictionary<string, string> MaterialiProgetto = new Dictionary<string, string>
        {
            { "Fontana/Water", MatRoot + "/M_Acqua.mat" },
        };

        static string BuildDir { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ArtSource", "Models", "build")); } }

        // ================================================================== importazione

        [MenuItem("Treno/R007/Importa modelli tappa 2")]
        public static void ImportaTutti()
        {
            var files = Directory.GetFiles(BuildDir, "*.json");
            Array.Sort(files);
            var fatti = new HashSet<string>();
            var report = new List<string>();
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Modelli R007", Path.GetFileNameWithoutExtension(files[i]), (float)i / files.Length);
                    var j = JObject.Parse(File.ReadAllText(files[i]));
                    string nome = (string)j["nome"], cart = (string)j["cartella"];
                    string fbx = ModelRoot + "/" + cart + "/" + nome + ".fbx";
                    if (!File.Exists(fbx)) continue;
                    ImportaUno(j, nome, cart, fbx, report);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ModelliR007] Importati " + files.Length + " descrittori.\n" + string.Join("\n", report));
        }

        static void ImportaUno(JObject j, string nome, string cart, string fbx, List<string> report)
        {
            var tex = j["texture"] as JObject ?? new JObject();
            var piatti = j["piatti"] as JObject ?? new JObject();
            var doppia = new HashSet<string>(j["doppia_faccia"] != null ? j["doppia_faccia"].ToObject<string[]>() : new string[0]);
            var clip = new HashSet<string>(j["alpha_clip"] != null ? j["alpha_clip"].ToObject<string[]>() : new string[0]);
            var mats = j["materiali"].ToObject<string[]>();
            string td = ModelRoot + "/" + cart + "/Textures";
            var mappa = new Dictionary<string, Material>();
            var varianti = new Dictionary<string, Dictionary<string, Material>>(); // variante -> (nome materiale sorgente -> materiale variante)

            foreach (var m in mats)
            {
                string key = m.Replace('.', '_');
                string matName = "M_" + cart + (mats.Length > 1 || cart != nome && !nome.StartsWith("Panno_") ? "_" + key : "");
                if (nome.StartsWith("Panno_")) matName = "M_Panni";
                string path = MatRoot + "/" + matName + ".mat";
                string progetto;
                if (MaterialiProgetto.TryGetValue(cart + "/" + m, out progetto))
                {
                    var mp = AssetDatabase.LoadAssetAtPath<Material>(progetto);
                    if (mp != null) mappa[m] = mp;
                    continue;
                }
                var info = tex[m] as JObject;
                Texture2D alb = null, nor = null, ms = null;
                float levig = 0.3f;
                if (info != null)
                {
                    bool aClip = clip.Contains(m);
                    alb = CaricaTex(td, (string)info["albedo"], TipoTex.Colore, aClip);
                    nor = CaricaTex(td, (string)info["normal"], TipoTex.Normale, false);
                    ms = CaricaTex(td, (string)info["mask"], TipoTex.Dati, false);
                    if (info["levigatezza"] != null) levig = (float)info["levigatezza"];
                }
                var flat = piatti[m] as JObject;
                Color tint = EpochTint;
                float metallo = 0f;
                if (flat != null)
                {
                    var c = flat["colore"].ToObject<float[]>();
                    tint = new Color(c[0] * EpochTint.r, c[1] * EpochTint.g, c[2] * EpochTint.b, 1f);
                    metallo = (float)flat["metallico"];
                    levig = (float)flat["levig"];
                }
                string alphaMode = clip.Contains(m) ? "MASK" : "OPAQUE";
                var mat = AssetImportHelper.MakeLit(path, alb, nor, ms, tint, null, Color.black, alphaMode, doppia.Contains(m), ms != null ? 1f : levig, ms);
                mat.SetFloat("_Metallic", ms != null ? 0f : metallo);
                EditorUtility.SetDirty(mat);
                mappa[m] = mat;

                if (info != null)
                    foreach (var p in info.Properties())
                    {
                        if (!p.Name.StartsWith("albedo_")) continue;
                        string var = p.Name.Substring(7);
                        var altAlb = CaricaTex(td, (string)p.Value, TipoTex.Colore, false);
                        var vmat = AssetImportHelper.MakeLit(MatRoot + "/" + matName + "_" + var + ".mat", altAlb, nor, ms, tint, null, Color.black, alphaMode, doppia.Contains(m), ms != null ? 1f : levig, ms);
                        vmat.SetFloat("_Metallic", 0f);
                        EditorUtility.SetDirty(vmat);
                        if (!varianti.ContainsKey(var)) varianti[var] = new Dictionary<string, Material>();
                        varianti[var][m] = vmat;
                    }
            }

            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
            mi.generateSecondaryUV = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            foreach (var kv in mappa)
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            mi.SaveAndReimport();

            // varianti: prefab con i materiali scambiati (es. pentole di rame)
            foreach (var v in varianti)
            {
                if (cart == "Panni") continue; // le varianti di colore dei panni sono solo materiali (M_Panni_B, M_Panni_C)
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var sm = r.sharedMaterials;
                    for (int k = 0; k < sm.Length; k++)
                        foreach (var kv in v.Value)
                            if (mappa.ContainsKey(kv.Key) && sm[k] == mappa[kv.Key]) sm[k] = kv.Value;
                    r.sharedMaterials = sm;
                }
                PrefabUtility.SaveAsPrefabAsset(inst, ModelRoot + "/" + cart + "/" + nome + "_" + v.Key + ".prefab");
                UnityEngine.Object.DestroyImmediate(inst);
            }

            // controllo scala: dimensioni reali dell'LOD0 contro quelle dichiarate da Blender
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var b = Bounds0(asset);
            var dim = j["dim"] != null && j["dim"].HasValues ? j["dim"].ToObject<float[]>() : null;
            report.Add(nome + ": " + b.size.ToString("F3") + (dim != null ? "  (Blender " + dim[0] + "," + dim[2] + "," + dim[1] + " = x,y,z Unity)" : ""));
        }

        static Bounds Bounds0(GameObject go)
        {
            Bounds b = new Bounds();
            bool primo = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.Contains("_LOD") && !mf.name.EndsWith("_LOD0")) continue;
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var t = mf.transform;
                var c = t.TransformPoint(mb.center);
                var s = Vector3.Scale(mb.size, t.lossyScale);
                var bb = new Bounds(c, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
                if (primo) { b = bb; primo = false; } else b.Encapsulate(bb);
            }
            return b;
        }

        enum TipoTex { Colore, Normale, Dati }

        static Texture2D CaricaTex(string td, string file, TipoTex tipo, bool alpha)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string p = td + "/" + file;
            if (!File.Exists(p)) { Debug.LogWarning("[ModelliR007] Texture mancante: " + p); return null; }
            AssetDatabase.ImportAsset(p);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) return null;
            imp.textureType = tipo == TipoTex.Normale ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = tipo == TipoTex.Colore;
            imp.alphaSource = (tipo == TipoTex.Dati || alpha) ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.alphaIsTransparency = alpha;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = false;
            imp.isReadable = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        // ================================================================== scena di prova

        class Voce
        {
            public string id;
            public string scena;                 // scena da cui copiare la primitiva attuale
            public string[] primitive;           // nomi degli oggetti (piu' parti, es. corpo e testa)
            public string[] nuovi;               // asset dei modelli nuovi (FBX o prefab)
            public float yawNuovo = 180f;        // i modelli glTF guardano +Z in Unity: 180 gradi per mostrare il fronte alla camera (che guarda verso +Z)
            public float yawPrim = 180f;
            public float spazio = 1.5f;          // distanza dal centro, per parte
            public bool primoPiano;
            public bool soloNuovo;
        }

        static string P(string cart, string file) { return ModelRoot + "/" + cart + "/" + file; }

        static List<Voce> Voci()
        {
            return new List<Voce>
            {
                new Voce { id = "sedia", scena = "10_Hub_Vicolo", primitive = new[] { "SediaPaglia" }, nuovi = new[] { P("SediaPaglia", "SediaPaglia.fbx") }, spazio = 0.7f },
                new Voce { id = "letto", scena = "20_Int_BassoAntonietta", primitive = new[] { "LettoFerro" }, nuovi = new[] { P("LettoFerro", "LettoFerro.fbx") }, spazio = 1.5f },
                new Voce { id = "credenza", scena = "20_Int_BassoAntonietta", primitive = new[] { "Credenza" }, nuovi = new[] { P("Credenza", "Credenza.fbx") }, spazio = 1.0f },
                new Voce { id = "bancarella", scena = "10_Hub_Vicolo", primitive = new[] { "Bancarella" }, nuovi = new[] { P("Bancarella", "Bancarella.fbx") }, spazio = 1.7f },
                new Voce { id = "bicicletta", scena = "10_Hub_Vicolo", primitive = new[] { "Bicicletta" }, nuovi = new[] { P("Bicicletta", "Bicicletta.fbx") }, spazio = 1.3f, primoPiano = true },
                new Voce { id = "pozzo", scena = "10_Hub_Vicolo", primitive = new[] { "Pozzo" }, nuovi = new[] { P("Pozzo", "Pozzo.fbx") }, spazio = 1.8f },
                new Voce { id = "fontana", scena = "10_Hub_Vicolo", primitive = new[] { "Fontanella" }, nuovi = new[] { P("Fontana", "Fontana.fbx") }, spazio = 3.0f },
                new Voce { id = "panchina", scena = "30_FB_Scarpe", primitive = new[] { "PanchinaTufo" }, nuovi = new[] { P("PanchinaPopolare", "PanchinaPopolare.fbx") }, spazio = 1.5f },
                new Voce { id = "valigia", scena = "10_Hub_Vicolo", primitive = new[] { "Valigia" }, nuovi = new[] { P("Valigia", "Valigia.fbx"), P("Valigia2", "Valigia2.fbx") }, spazio = 0.7f },
                new Voce { id = "sacco", scena = "10_Hub_Vicolo", primitive = new[] { "Sacco" }, nuovi = new[] { P("Sacco", "Sacco.fbx") }, spazio = 0.8f },
                new Voce { id = "tavolino", scena = "30_FB_Scarpe", primitive = new[] { "Tavolino" }, nuovi = new[] { P("Tavolino", "Tavolino.fbx") }, spazio = 1.0f },
                new Voce { id = "pentole", scena = "20_Int_BassoAntonietta", primitive = new[] { "Appeso_Pentola0", "Appeso_Pentola1" },
                    nuovi = new[] { P("PentolaRame", "PentolaRame.fbx"), P("PadellaRame", "PadellaRame.fbx") },
                    spazio = 0.35f, primoPiano = true },
                new Voce { id = "madonna", scena = "10_Hub_Vicolo", primitive = new[] { "EdicolaStatuetta", "EdicolaStatuettaTesta" }, nuovi = new[] { P("MadonnaStatuetta", "MadonnaStatuetta.fbx") }, spazio = 0.3f, primoPiano = true },
                new Voce { id = "radio", scena = "30_FB_Scarpe", primitive = new[] { "Radio" }, nuovi = new[] { P("Radio", "Radio.fbx") }, spazio = 0.35f },
                new Voce { id = "gabbia", scena = "10_Hub_Vicolo", primitive = new[] { "GabbiaGalline" }, nuovi = new[] { P("GabbiaGalline", "GabbiaGalline.fbx") }, spazio = 0.7f, yawNuovo = 0f },
                new Voce { id = "lavatoio", scena = "10_Hub_Vicolo", primitive = new[] { "Lavatoio" }, nuovi = new[] { P("Lavatoio", "Lavatoio.fbx") }, spazio = 1.8f },
                new Voce { id = "bacinella", scena = "10_Hub_Vicolo", primitive = new[] { "Bacinella" }, nuovi = new[] { P("Bacinella", "Bacinella.fbx"), P("Bacinella", "Bacinella_azzurro.prefab") }, spazio = 0.4f },
                new Voce { id = "cassa_frutta", scena = "30_FB_Scarpe", primitive = new[] { "CassaFrutta" },
                    nuovi = new[] { P("CassaFrutta", "CassaFrutta_Arance.fbx"), P("CassaFrutta", "CassaFrutta_Pomodori.fbx"), P("CassaFrutta", "CassaFrutta_Verdura.fbx") }, spazio = 0.5f },
            };
        }

        static GameObject Nuovo(string nome, Transform parent) { var g = new GameObject(nome); g.transform.SetParent(parent, false); return g; }

        static Bounds BoundsRend(GameObject go)
        {
            bool primo = true;
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (primo) { b = r.bounds; primo = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Appoggia il gruppo a terra e lo centra su (x,z).</summary>
        static void Posa(GameObject gruppo, float x, float z, float yaw)
        {
            gruppo.transform.position = Vector3.zero;
            gruppo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var b = BoundsRend(gruppo);
            gruppo.transform.position = new Vector3(x - b.center.x, -b.min.y, z - b.center.z);
        }

        [MenuItem("Treno/R007/Costruisci scena di prova modelli")]
        public static void CostruisciProva()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var voci = Voci();
            // 1) copie delle primitive dalle scene del gioco (aperte in additivo, poi chiuse)
            var scena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var copie = new Dictionary<string, List<GameObject>>();
            var cloth = new Dictionary<string, GameObject>();
            string[] scene = { "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe" };
            foreach (var sn in scene)
            {
                var sc = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + sn + ".unity", OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scena);
                foreach (var v in voci)
                {
                    if (v.scena != sn) continue;
                    var lista = new List<GameObject>();
                    foreach (var nm in v.primitive)
                    {
                        var g = TrovaInScena(sc, nm);
                        if (g == null) { Debug.LogWarning("[ModelliR007] Primitiva non trovata: " + sn + "/" + nm); continue; }
                        var c = UnityEngine.Object.Instantiate(g);
                        c.name = nm;
                        if (v.primitive.Length == 1) c.transform.rotation = Quaternion.identity; // fronte a +Z, come nei generatori
                        SceneManager.MoveGameObjectToScene(c, scena);
                        lista.Add(c);
                    }
                    copie[v.id] = lista;
                }
                if (sn == "10_Hub_Vicolo")
                    foreach (var k in new[] { "Lenzuolo", "Camicia", "Pantaloni", "Straccio" })
                    {
                        var g = TrovaInScena(sc, "Panno_" + k);
                        if (g == null) continue;
                        var c = UnityEngine.Object.Instantiate(g);
                        c.name = "Panno_" + k;
                        SceneManager.MoveGameObjectToScene(c, scena);
                        cloth[k] = c;
                    }
                EditorSceneManager.CloseScene(sc, true);
            }
            SceneManager.SetActiveScene(scena);

            // 2) luce, suolo, camera
            var sun = new GameObject("Sole");
            sun.AddComponent<Light>().type = LightType.Directional;
            var camGo = new GameObject("Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            var suolo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suolo.name = "Suolo";
            suolo.transform.position = new Vector3(0f, -0.5f, 200f);
            suolo.transform.localScale = new Vector3(60f, 1f, 420f);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "_Game", "Scenes", "Prove")));
            // i cubi hanno UV 0..1 su tutta la faccia: materiali copia con il tiling a ~2 m per ripetizione
            var matSuolo = MatProva("M_ProvaSuolo", MatRoot + "/M_BasoliLavica.mat", new Vector2(30f, 210f));
            suolo.GetComponent<Renderer>().sharedMaterial = matSuolo;
            var matMuro = MatProva("M_ProvaMuro", MatRoot + "/M_Intonaco_Ocra.mat", new Vector2(20f, 2.5f));

            // 3) una cella per voce
            float z = 0f;
            var rootAll = Nuovo("Confronti", null);
            foreach (var v in voci)
            {
                var cella = Nuovo("Cella_" + v.id, rootAll.transform);
                cella.transform.position = new Vector3(0f, 0f, z);
                var prim = Nuovo("Primitiva", cella.transform);
                List<GameObject> lista;
                if (copie.TryGetValue(v.id, out lista))
                    foreach (var g in lista) g.transform.SetParent(prim.transform, true);
                Posa(prim, -v.spazio, z, v.yawPrim);
                var nuovi = Nuovo("Nuovi", cella.transform);
                float x0 = v.spazio;
                for (int i = 0; i < v.nuovi.Length; i++)
                {
                    var a = AssetDatabase.LoadAssetAtPath<GameObject>(v.nuovi[i]);
                    if (a == null) { Debug.LogWarning("[ModelliR007] Asset nuovo mancante: " + v.nuovi[i]); continue; }
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(a);
                    var gr = Nuovo("N" + i, nuovi.transform);
                    inst.transform.SetParent(gr.transform, false);
                    Posa(gr, x0 + i * v.spazio * 1.6f, z, v.yawNuovo);
                }
                var muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
                muro.name = "Fondale_" + v.id;
                muro.transform.position = new Vector3(0f, 2.5f, z + 5f);
                muro.transform.localScale = new Vector3(40f, 5f, 0.3f);
                muro.GetComponent<Renderer>().sharedMaterial = matMuro;
                muro.transform.SetParent(rootAll.transform, true);
                z += 14f;
            }
            // 4) panni stesi: una corda con le primitive a sinistra e i nuovi capi a destra
            {
                var cella = Nuovo("Cella_panni", rootAll.transform);
                float cz = z;
                var corda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                corda.name = "Corda";
                corda.transform.SetParent(cella.transform, false);
                corda.transform.position = new Vector3(0f, 2.3f, cz);
                corda.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                corda.transform.localScale = new Vector3(0.015f, 4.7f, 0.015f);
                var prim = Nuovo("Primitiva", cella.transform);
                float x = -4.0f;
                foreach (var k in new[] { "Lenzuolo", "Camicia", "Pantaloni", "Straccio" })
                {
                    GameObject g;
                    if (!cloth.TryGetValue(k, out g)) continue;
                    g.transform.SetParent(prim.transform, true);
                    g.transform.position = new Vector3(x, 2.3f, cz);
                    g.transform.rotation = Quaternion.identity;
                    x += 1.1f;
                }
                var nuovi = Nuovo("Nuovi", cella.transform);
                x = 0.7f;
                foreach (var k in new[] { "Lenzuolo", "LenzuoloRammendato", "Camicia", "Mutandoni", "Tovaglia", "Straccio", "Camicia", "Lenzuolo", "Mutandoni", "Straccio" })
                {
                    var a = AssetDatabase.LoadAssetAtPath<GameObject>(P("Panni", "Panno_" + k + ".fbx"));
                    if (a == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(a);
                    inst.transform.SetParent(nuovi.transform, false);
                    inst.transform.position = new Vector3(x, 2.3f, cz);
                    x += k.StartsWith("Lenzuolo") ? 1.25f : 0.95f;
                }
                var muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
                muro.name = "Fondale_panni";
                muro.transform.position = new Vector3(0f, 2.5f, cz + 3f);
                muro.transform.localScale = new Vector3(40f, 5f, 0.3f);
                muro.GetComponent<Renderer>().sharedMaterial = matMuro;
                muro.transform.SetParent(rootAll.transform, true);
            }

            // 5) tutti gli LOD a 0 per i confronti
            foreach (var lg in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include))
                lg.ForceLOD(0);

            LookBuilder.ApplyLookProva(scena);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "_Game", "Scenes", "Prove")));
            EditorSceneManager.SaveScene(scena, ScenaProva);
            Debug.Log("[ModelliR007] Scena di prova salvata: " + ScenaProva);
        }

        static Material MatProva(string nome, string origine, Vector2 tiling)
        {
            string path = "Assets/_Game/Scenes/Prove/" + nome + ".mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>(origine);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            m.CopyPropertiesFromMaterial(src);
            foreach (var prop in new[] { "_BaseMap", "_BumpMap", "_MainTex", "_MetallicGlossMap", "_OcclusionMap" })
                if (m.HasProperty(prop)) m.SetTextureScale(prop, tiling);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject TrovaInScena(Scene sc, string nome)
        {
            foreach (var root in sc.GetRootGameObjects())
            {
                if (root.name == nome) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == nome) return t.gameObject;
            }
            return null;
        }


        // ================================================================== parte B: confronto dei modelli rifatti

        const string ScenaRifatti = "Assets/_Game/Scenes/Prove/R007_Rifatti.unity";

        /// <summary>Scena con i 4 modelli rifatti (bancarella, panchina, sedia, bicicletta), la gabbia, il lavatoio scurito e i panni nelle 3 varianti di colore.</summary>
        [MenuItem("Treno/R007/Scena e foto dei modelli rifatti")]
        public static void ScenaERifatti()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Sole").AddComponent<Light>().type = LightType.Directional;
            var camGo = new GameObject("Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "_Game", "Scenes", "Prove")));
            var suolo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suolo.name = "Suolo";
            suolo.transform.position = new Vector3(0f, -0.5f, 60f);
            suolo.transform.localScale = new Vector3(60f, 1f, 160f);
            suolo.GetComponent<Renderer>().sharedMaterial = MatProva("M_ProvaSuolo", MatRoot + "/M_BasoliLavica.mat", new Vector2(30f, 80f));
            var matMuro = MatProva("M_ProvaMuro", MatRoot + "/M_Intonaco_Ocra.mat", new Vector2(20f, 2.5f));
            var root = Nuovo("Confronti", null);
            // cella 1: i modelli rifatti
            var c1 = Nuovo("Cella_rifatti", root.transform);
            var n1 = Nuovo("Nuovi", c1.transform);
            string[] liste = { P("Bancarella", "Bancarella.fbx"), P("PanchinaPopolare", "PanchinaPopolare.fbx"), P("SediaPaglia", "SediaPaglia.fbx"), P("SediaPaglia", "SediaPaglia.fbx"),
                               P("Bicicletta", "Bicicletta.fbx"), P("GabbiaGalline", "GabbiaGalline.fbx"), P("Lavatoio", "Lavatoio.fbx") };
            float[] xs = { -4.6f, -1.9f, -0.2f, 0.5f, 1.8f, 3.2f, 5.6f };
            float[] yw = { 180f, 180f, 200f, 160f, 180f, 180f, 180f };
            for (int i = 0; i < liste.Length; i++)
            {
                var a = AssetDatabase.LoadAssetAtPath<GameObject>(liste[i]);
                if (a == null) continue;
                var gr = Nuovo("N" + i, n1.transform);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(a);
                inst.transform.SetParent(gr.transform, false);
                Posa(gr, xs[i], 0f, yw[i]);
            }
            var muro1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro1.name = "Fondale_rifatti"; muro1.transform.position = new Vector3(0f, 2.5f, 4f); muro1.transform.localScale = new Vector3(40f, 5f, 0.3f);
            muro1.GetComponent<Renderer>().sharedMaterial = matMuro; muro1.transform.SetParent(root.transform, true);
            // cella 2: i panni, tre varianti di colore, su due fili
            float z2 = 30f;
            var c2 = Nuovo("Cella_panni2", root.transform);
            var n2 = Nuovo("Nuovi", c2.transform);
            string[] capi = { "Lenzuolo", "Camicia", "Mutandoni", "LenzuoloRammendato", "Straccio", "Tovaglia", "Camicia", "Lenzuolo", "Mutandoni", "Straccio" };
            string[] mm = { "M_Panni", "M_Panni_B", "M_Panni_C" };
            for (int fila = 0; fila < 2; fila++)
            {
                var corda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                corda.name = "Corda";
                corda.transform.SetParent(n2.transform, false);
                corda.transform.position = new Vector3(0f, 2.4f - fila * 0.0f, z2 - fila * 1.6f);
                corda.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                corda.transform.localScale = new Vector3(0.015f, 5.2f, 0.015f);
                float x = -4.6f;
                for (int k = 0; k < capi.Length; k++)
                {
                    var a = AssetDatabase.LoadAssetAtPath<GameObject>(P("Panni", "Panno_" + capi[k] + ".fbx"));
                    if (a == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(a);
                    inst.transform.SetParent(n2.transform, false);
                    inst.transform.position = new Vector3(x, 2.4f, z2 - fila * 1.6f);
                    if (k % 3 == 1) inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/" + mm[(k + fila) % 3] + ".mat");
                    foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
                    x += capi[k].StartsWith("Lenzuolo") || capi[k] == "Tovaglia" ? 1.15f : 0.8f;
                }
            }
            var muro2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro2.name = "Fondale_panni2"; muro2.transform.position = new Vector3(0f, 2.5f, z2 + 3f); muro2.transform.localScale = new Vector3(40f, 5f, 0.3f);
            muro2.GetComponent<Renderer>().sharedMaterial = matMuro; muro2.transform.SetParent(root.transform, true);
            foreach (var lg in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include)) lg.ForceLOD(0);
            LookBuilder.ApplyLookProva(scena);
            EditorSceneManager.SaveScene(scena, ScenaRifatti);
            scenaFoto = "Prove/R007_Rifatti";
            Foto("rifatti", false, "t2b_rifatti");
            Foto("panni2", false, "t2b_panni");
            scenaFoto = "Prove/R007_Modelli";
            Debug.Log("[ModelliR007] Scena " + ScenaRifatti + " e foto t2b_rifatti / t2b_panni.");
        }

        // ================================================================== foto

        [MenuItem("Treno/R007/Scatta confronti modelli")]
        public static void ScattaConfronti()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenaProva) EditorSceneManager.OpenScene(ScenaProva, OpenSceneMode.Single);
            foreach (var lg in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include)) lg.ForceLOD(0);
            var voci = Voci();
            foreach (var v in voci) Foto(v.id, v.primoPiano, "t2_" + v.id);
            Foto("panni", false, "t2_panni");
            Debug.Log("[ModelliR007] Foto scattate in " + FotoDocumentazione.Cartella);
        }

        /// <summary>Scatta una foto di una sola cella (o solo del lato nuovo per i primi piani).</summary>
        public static void Foto(string id, bool conPrimoPiano, string nomeFile, float fov = 38f, float yaw = 0f, float pitchExtra = 0f)
        {
            var cella = GameObject.Find("Cella_" + id);
            if (cella == null) { Debug.LogWarning("[ModelliR007] Cella mancante: " + id); return; }
            // un solo fondale alla volta: gli altri, in mezzo, nasconderebbero la scena
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                if (r.name.StartsWith("Fondale_")) r.gameObject.SetActive(r.name == "Fondale_" + id);
            ScattaCella(cella, false, nomeFile, fov, yaw);
            if (conPrimoPiano) ScattaCella(cella, true, nomeFile + "_primopiano", fov, 22f);
        }

        static string scenaFoto = "Prove/R007_Modelli";

        static void ScattaCella(GameObject cella, bool soloNuovi, string nomeFile, float fov, float yaw)
        {
            Bounds b = new Bounds();
            bool primo = true;
            foreach (var r in cella.GetComponentsInChildren<Renderer>())
            {
                if (r.name.StartsWith("Fondale") || r.name == "Suolo") continue;
                if (soloNuovi && r.GetComponentInParent<Transform>() != null && !EDiscendente(r.transform, "Nuovi")) continue;
                if (primo) { b = r.bounds; primo = false; } else b.Encapsulate(r.bounds);
            }
            if (primo) return;
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tanH = tanV * 16f / 9f;
            float dist = Mathf.Max(b.size.x / (2f * tanH), b.size.y / (2f * tanV)) * (soloNuovi ? 1.25f : 1.2f) + b.size.z * 0.5f;
            dist = Mathf.Max(dist, 0.8f);
            var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
            var centro = b.center + Vector3.up * (b.size.y * 0.02f);
            var pos = centro + dir * dist + Vector3.up * (dist * 0.16f);
            var look = Quaternion.LookRotation(centro - pos);
            var e = look.eulerAngles;
            float pitch = e.x > 180f ? e.x - 360f : e.x;
            Debug.Log("[ModelliR007] " + nomeFile + " pos " + pos + " yaw " + e.y + " pitch " + pitch + " bounds " + b);
            FotoDocumentazione.Scatta(scenaFoto, pos, e.y, pitch, 2, nomeFile, fov);
        }

        static bool EDiscendente(Transform t, string nomeAvo)
        {
            for (var p = t; p != null; p = p.parent) if (p.name == nomeAvo) return true;
            return false;
        }
    }
}
