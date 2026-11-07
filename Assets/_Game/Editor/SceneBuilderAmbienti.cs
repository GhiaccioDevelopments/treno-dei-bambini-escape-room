using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
using TrenoBambini.Puzzles;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Ambienti realistici d'epoca (Fase 2.3): hub (vicolo napoletano 1946), basso di Antonietta, stanza del flashback.
    /// Facciate e pareti sono mesh del kit (SceneBuilderKit.cs) con materiali PBR Poly Haven; i props sono i modelli di Assets/Art/Models.
    /// </summary>
    public static partial class SceneBuilder
    {
        // sottomesh della facciata
        const int SWall = 0, SStone = 1, SWood = 2, SGlass = 3, SIron = 4, SPaint = 5, SRoof = 6;
        const float TileWall = 2.5f, TileStone = 1.5f, TileWood = 1.0f;

        static int kitIndex;
        static string kitScene = "";
        static int kitMeshes, kitTris;
        static int propCount;

        static readonly Dictionary<string, string> LegacyToPbr = new Dictionary<string, string>
        {
            { "Tufo", "M_IntonacoRustico05" },
            { "TufoScuro", "M_ArenariaCrepe" },
            { "Intonaco", "M_IntonacoMattoni" },
            { "Legno", "M_LegnoVernicePelata" },
            { "Ciottoli", "M_BasoliLavica" },
            { "Ferro", "M_FerroRuggine02" },
            { "Ricordo", "M_IntonacoRossoSbiadito" },
        };

        // ------------------------------------------------------------------ infrastruttura

        static void PrepareKitFolder()
        {
            ArtGenerator.EnsureFolder("Assets/_Game/Generated");
            KitBuilder.Used.Clear();
            clothMeshes.Clear();
            shoeMesh = null;
            ArtGenerator.EnsureFolder(KitBuilder.Folder);
            kitIndex = 0; kitMeshes = 0; kitTris = 0; propCount = 0;
            facadeCache.Clear();
        }

        static void ReportKit()
        {
            // solo gli asset orfani (non riscritti in questa costruzione) vengono cancellati
            foreach (var g in AssetDatabase.FindAssets("t:Mesh", new[] { KitBuilder.Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!KitBuilder.Used.Contains(path)) AssetDatabase.DeleteAsset(path);
            }
            Debug.Log("[SceneBuilder] Kit: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " modelli FBX piazzati.");
        }

        // materiali speciali: tinte di tessuto e cuoio derivate dalle texture PBR (copie salvate in Art/Materials)
        static Material SpecialMat(string name)
        {
            switch (name)
            {
                case "Tessuto1": return TintedMat("M_TessutoLino", "M_Panno_Terracotta", new Color(0.78f, 0.46f, 0.36f));
                case "Tessuto2": return TintedMat("M_TessutoLino", "M_Panno_Azzurro", new Color(0.52f, 0.63f, 0.72f));
                case "Tessuto3": return AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_TessutoLino.mat");
                case "Cuoio": return TintedMat("M_TessutoIuta", "M_Cuoio_Scarpe", new Color(0.34f, 0.22f, 0.14f));
                case "PannoBianco": return TintedMat("M_TessutoLino", "M_Panno_Bianco", new Color(0.85f, 0.82f, 0.74f));
                case "PannoGrigio": return TintedMat("M_TessutoLino", "M_Panno_Grigio", new Color(0.58f, 0.58f, 0.56f));
                case "PannoBeige": return TintedMat("M_TessutoLino", "M_Panno_Beige", new Color(0.78f, 0.68f, 0.5f));
                case "PannoBlu": return TintedMat("M_TessutoLino", "M_Panno_BluSpento", new Color(0.42f, 0.5f, 0.58f));
                case "PannoTerra": return TintedMat("M_TessutoLino", "M_Panno_Terra", new Color(0.6f, 0.4f, 0.3f));
                case "PannoOcra": return TintedMat("M_TessutoLino", "M_Panno_Ocra", new Color(0.72f, 0.58f, 0.32f));
                case "SuolaScarpa": return TintedMat("M_TessutoIuta", "M_Suola_Scarpa", new Color(0.2f, 0.15f, 0.12f));
                case "InsegnaRossa": return TintedMat("M_LegnoDipintoBlu", "M_Insegna_Rossa", new Color(1f, 0.38f, 0.3f));
                case "InsegnaBlu": return TintedMat("M_LegnoDipintoBlu", "M_Insegna_Blu", new Color(1f, 1f, 1f));
                case "InsegnaCrema": return TintedMat("M_TessutoLino", "M_Insegna_Crema", new Color(0.95f, 0.88f, 0.7f));
                // R006: intonaci del quartiere (tinte desaturate, d'epoca) e fogliame
                case "IntonacoOcra": return TintedMat("M_IntonacoRustico05", "M_Intonaco_Ocra", new Color(1.0f, 0.84f, 0.52f));
                case "IntonacoRosa": return TintedMat("M_IntonacoRustico05", "M_Intonaco_Rosa", new Color(1.0f, 0.78f, 0.7f));
                case "IntonacoVerde": return TintedMat("M_IntonacoRustico05", "M_Intonaco_Verde", new Color(0.76f, 0.84f, 0.68f));
                case "IntonacoAzzurro": return TintedMat("M_IntonacoRustico05", "M_Intonaco_Azzurro", new Color(0.72f, 0.8f, 0.9f));
                case "IntonacoGrigio": return TintedMat("M_IntonacoMattoni", "M_Intonaco_Grigio", new Color(0.8f, 0.8f, 0.78f));
                case "Foglia": return TintedMat("M_TessutoLino", "M_Foglia", new Color(0.28f, 0.46f, 0.2f));
                case "Paglia": return TintedMat("M_CestaVimini", "M_Paglia", new Color(1.1f, 1.0f, 0.75f));
            }
            return null;
        }

        static Material TintedMat(string baseName, string newName, Color tint)
        {
            string path = "Assets/Art/Materials/" + newName + ".mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + baseName + ".mat");
            if (src == null) return null;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(src);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", src.GetColor("_BaseColor") * tint);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject MeshObject(Transform parent, string name, Mesh mesh, string[] mats, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var arr = new Material[mats.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = Mat(mats[i]);
            mr.sharedMaterials = arr;
            if (collider)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = mesh.bounds.center;
                bc.size = mesh.bounds.size;
            }
            kitMeshes++;
            kitTris += mesh.triangles.Length / 3;
            return go;
        }

        static string KitName(string baseName)
        {
            return kitScene + "_" + (++kitIndex).ToString("D2") + "_" + baseName;
        }

        // piano/blocco con una sola texture ancorata al mondo
        static GameObject Ground(string name, Transform parent, Vector3 min, Vector3 max, string mat, float tile, bool collider = true)
        {
            var kb = new KitBuilder(mat);
            kb.Box(min, max, 0, tile, KitBuilder.NY);
            var mesh = kb.Build(KitName(name));
            return MeshObject(parent, name, mesh, new[] { mat }, collider);
        }

        // ------------------------------------------------------------------ modelli FBX

        /// <summary>R006: quota del terreno in (x,z) per gli ambienti con dislivelli (hub); null = piano a quota 0.</summary>
        static System.Func<float, float, float> quotaTerreno;

        static bool ATerra(Vector3 pos)
        {
            float g = quotaTerreno != null ? quotaTerreno(pos.x, pos.z) : 0f;
            return pos.y - g <= 0.05f;
        }

        static GameObject Model(string model, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool collider = true, string objName = null, float cull = 0.02f)
        {
            return ModelloIst(model, parent, pos, yaw, Vector3.one * scale, collider, objName, cull);
        }

        /// <summary>
        /// Istanzia un modello FBX. "Nome" = Assets/Art/Models/Nome/Nome.fbx; "Cartella/File.ext" = percorso esplicito sotto Assets/Art/Models.
        /// R007 t2: i modelli con LOD0/1/2 (nomi *_LOD0..2) mantengono i tre livelli (soglie 0,40 / 0,15 / culling); gli altri hanno un solo livello.
        /// Collider: un unico BoxCollider sul volume del modello (R018: niente oggetti senza collider dove prima c'era).
        /// </summary>
        static GameObject ModelloIst(string model, Transform parent, Vector3 pos, float yaw, Vector3 scale, bool collider, string objName, float cull)
        {
            string path = model.Contains("/") ? "Assets/Art/Models/" + model : "Assets/Art/Models/" + model + "/" + model + ".fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError("[SceneBuilder] Modello mancante: " + path);
                return new GameObject(objName ?? model);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = objName ?? System.IO.Path.GetFileNameWithoutExtension(model);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;

            var rs = go.GetComponentsInChildren<Renderer>();
            if (collider)
            {
                bool any = false;
                Bounds b = new Bounds();
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var mb = mf.sharedMesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var p = go.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                        if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                    }
                }
                if (any)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = b.center;
                    bc.size = b.size;
                }
            }
            var lg = go.GetComponent<LODGroup>();
            if (lg == null) lg = go.AddComponent<LODGroup>();
            var liv = new System.Collections.Generic.List<Renderer>[3];
            for (int i = 0; i < 3; i++) liv[i] = new System.Collections.Generic.List<Renderer>();
            bool conLod = false;
            foreach (var r in rs)
            {
                int k = r.name.EndsWith("_LOD1") ? 1 : r.name.EndsWith("_LOD2") ? 2 : 0;
                if (k > 0) conLod = true;
                liv[k].Add(r);
            }
            if (conLod && liv[1].Count > 0)
            {
                var lods = new System.Collections.Generic.List<LOD> { new LOD(0.40f, liv[0].ToArray()), new LOD(liv[2].Count > 0 ? 0.15f : cull, liv[1].ToArray()) };
                if (liv[2].Count > 0) lods.Add(new LOD(cull, liv[2].ToArray()));
                lg.SetLODs(lods.ToArray());
            }
            else lg.SetLODs(new[] { new LOD(cull, rs) });
            lg.RecalculateBounds();
            propCount++;
            if (ATerra(pos)) RegistraMobile(go); // appoggiato a terra: a fine costruzione si allontana da muri e altri oggetti
            return go;
        }

        // ------------------------------------------------------------------ pareti con aperture

        /// <summary>R017: aggancio (m) delle cornici nelle aperture e rientro dei profili alle testate dei moduli. 1,2 cm: con depth a 24 bit e senza Z invertito (WebGL) 4 mm tremolano a distanza.</summary>
        const float Riporto = 0.012f;

        /// <summary>
        /// R017: profilo in pietra lungo la parete. Alle testate del modulo il corpo principale rientra di Riporto e una linguetta piu' bassa
        /// (rientrata di 3 cm sul fronte, 5 cm in fondo al modulo, e di 4 mm sopra e sotto: le linguette dei due moduli non sono complanari) lo prolunga dentro il profilo del modulo vicino: il giunto non resta aperto
        /// e le facce visibili dei due moduli non si sovrappongono.
        /// </summary>
        static void Trim(KitBuilder kb, WallFrame fr, float len, float u0, float u1, float d0, float d1, float y0, float y1, int sub, float tile, int skip)
        {
            bool ini = u0 <= 0.001f, fine = u1 >= len - 0.001f;
            float a = ini ? Riporto : u0, b = fine ? len - Riporto : u1;
            fr.Box(kb, a, b, d0, d1, y0, y1, sub, tile, skip);
            if (ini) fr.Box(kb, -0.03f, Riporto + 0.01f, d0 + 0.0213f, d1, y0 + 0.004f, y1 - 0.004f, sub, tile, 8 | 16 | 32);
            if (fine) fr.Box(kb, len - Riporto - 0.01f, len + 0.03f, d0 + 0.0455f, d1, y0 + 0.004f, y1 - 0.004f, sub, tile, 8 | 16 | 32);
        }

        static void ShellPieces(KitBuilder kb, WallFrame fr, float len, float y0, float y1, float thick, List<WallOpening> ops,
                                int sub, float tile, bool skipBottom, bool skipTop, bool skipBack = true)
        {
            ops.Sort((p, q) => p.u.CompareTo(q.u));
            int sb = skipBottom ? 16 : 0, st = skipTop ? 32 : 0;
            int bk = skipBack ? 8 : 0;
            float cur = 0f;
            foreach (var o in ops)
            {
                if (o.U0 > cur + 0.001f) fr.Box(kb, cur, o.U0, 0f, thick, y0, y1, sub, tile, bk | sb | st);
                if (o.y0 > y0 + 0.001f) fr.Box(kb, o.U0, o.U1, 0f, thick, y0, o.y0, sub, tile, bk | 1 | 2 | sb);
                if (o.y1 < y1 - 0.001f) fr.Box(kb, o.U0, o.U1, 0f, thick, o.y1, y1, sub, tile, bk | 1 | 2 | st);
                cur = o.U1;
            }
            if (len > cur + 0.001f) fr.Box(kb, cur, len, 0f, thick, y0, y1, sub, tile, bk | sb | st);
        }

        // ------------------------------------------------------------------ facciate esterne

        static string PlasterFor(string wallMat)
        {
            switch (wallMat)
            {
                case "Tufo": return "M_IntonacoRustico05";
                case "TufoScuro": return "M_IntonacoRossoSbiadito";
                case "Intonaco": return "M_IntonacoMattoni";
            }
            return string.IsNullOrEmpty(wallMat) ? "M_IntonacoRustico05" : wallMat;
        }

        // vero se un punto "da liberare" (lampione, edicola, ancoraggio di un filo) sta entro 1.9 m in pianta dal punto di parete dato
        static bool BalconeBloccato(Vector3 puntoSuParete)
        {
            foreach (var b in noBalcone)
            {
                var d = new Vector2(b.x - puntoSuParete.x, b.z - puntoSuParete.z);
                if (d.magnitude < 1.9f) return true;
            }
            return false;
        }

        /// <summary>R006: opzioni del modulo di facciata riusabile (quartiere). Con opt != null la facciata si costruisce nel sistema locale
        /// (origine nel punto della facciata, lungo +x, fronte verso -z) e la mesh e' condivisa tra tutte le istanze con la stessa chiave.</summary>
        internal sealed class FacadeOpt
        {
            public string key;          // nome stabile dell'asset mesh
            public int seed;
            public int balconyMask;     // piani (bit) che ammettono balconi
            public int doorKind;        // 0 = nessuna porta generica, 1 = porta chiusa, 2 = basso aperto, 3 = portone sbarrato, 4 = cancello
            public int interior;        // variante dell'interno del basso aperto
            public bool cieco;          // nessuna finestra al piano terra
            public int col;             // R017: fascia di quota (0..n): i profili orizzontali si alzano di col * PassoFascia rispetto ai moduli vicini
        }

        /// <summary>R017: passo (m) tra le quote dei profili orizzontali di moduli vicini (12 mm: regge il depth a 24 bit di WebGL).</summary>
        internal const float PassoFascia = 0.012f;

        sealed class FacadeCache { public Mesh m0, m1; public Vector3 bcc, bcs; }
        static readonly Dictionary<string, FacadeCache> facadeCache = new Dictionary<string, FacadeCache>();

        static GameObject FacadeInstance(Transform parent, string name, string[] subs, string plaster, FacadeCache c)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var g0 = MeshObject(root.transform, "LOD0", c.m0, subs, false);
            var g1 = MeshObject(root.transform, "LOD1", c.m1, new[] { plaster, "Finestra" }, false);
            var lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[]
            {
                new LOD(0.42f, new Renderer[] { g0.GetComponent<Renderer>() }),   // LOD aggressivi (R006): le facciate lontane passano subito al livello semplice
                new LOD(0.02f, new Renderer[] { g1.GetComponent<Renderer>() })
            });
            lg.RecalculateBounds();
            var bc = root.AddComponent<BoxCollider>();
            bc.center = c.bcc; bc.size = c.bcs;
            return root;
        }

        static GameObject Facade(Transform parent, string name, WallFrame fr, float len, float depth, int floors, string plaster,
                                 System.Random r, float bay, List<float> doorU, FacadeOpt opt = null)
        {
            string[] subs = { plaster, "M_ArenariaBlocchi", "M_LegnoVernicePelata", "Finestra", "M_FerroRuggine02", "M_LegnoDipintoBlu", "M_CoppiArgilla" };
            FacadeCache cached;
            if (opt != null && facadeCache.TryGetValue(opt.key, out cached)) return FacadeInstance(parent, name, subs, plaster, cached);
            if (opt != null) r = new System.Random(opt.seed);
            var kb = new KitBuilder(subs);
            kb.SubFisso = 0;
            float ofs = opt != null ? opt.col * PassoFascia : 0f;
            kb.ProteggiTestate(fr.AxisU, fr.AxisU == 0 ? fr.W(0, 0, 0).x : fr.W(0, 0, 0).z, fr.AxisU == 0 ? fr.W(len, 0, 0).x : fr.W(len, 0, 0).z);
            var lod = new KitBuilder(plaster, "Finestra");
            float h = floors * 3f;
            const float T = 0.45f;
            int dk = opt != null ? opt.doorKind : 0;
            float doorW = dk == 3 ? 2.4f : dk == 4 ? 2.0f : dk == 2 ? 1.7f : 1.3f;
            float doorH = dk == 3 ? 2.9f : 2.4f;
            float pocketDepth = 2.2f;
            float pu0 = 0f, pu1 = 0f;
            bool pocket = (dk == 2 || dk == 4) && doorU.Count > 0;
            if (pocket) { pu0 = doorU[0] - doorW * 0.5f; pu1 = doorU[0] + doorW * 0.5f; }

            // volumi: corpo (dietro il guscio), tetto, parapetto, cornicione
            if (!pocket) fr.Box(kb, 0, len, T, depth, 0, h, SWall, TileWall, 4 | 16 | 32);
            else
            {
                float ph = 2.7f, pd = Mathf.Min(T + pocketDepth, depth - 0.3f);
                fr.Box(kb, 0, pu0, T, depth, 0, h, SWall, TileWall, 4 | 16 | 32);
                fr.Box(kb, pu1, len, T, depth, 0, h, SWall, TileWall, 4 | 16 | 32);
                fr.Box(kb, pu0, pu1, pd, depth, 0, h, SWall, TileWall, 16 | 32);
                fr.Box(kb, pu0, pu1, T, pd, ph, h, SWall, TileWall, 4 | 32);
                fr.Box(kb, pu0, pu1, 0f, pd, -0.2f, 0.04f, SStone, TileStone, 16 | 1 | 2); // pavimento dell'interno
                PocketInterior(kb, fr, pu0, pu1, T, pd, dk == 4 ? -1 : opt.interior);
            }
            fr.Box(kb, 0, len, 0.35f, depth, h, h + 0.06f, SRoof, 1.5f, 16);
            fr.Box(kb, 0, len, 0f, 0.35f, h, h + 0.6f, SWall, TileWall, 16);
            Trim(kb, fr, len, 0f, len, -0.06f, 0.41f, h + 0.6f + ofs, h + 0.68f + ofs, SStone, TileStone, 16);
            // i profili in pietra rientrano di 4 mm alle testate del modulo: le loro facce non sono complanari a quelle del guscio
            Trim(kb, fr, len, 0f, len, -0.3f, 0.1f, h - 0.28f + ofs, h + ofs, SStone, TileStone, 0);
            Trim(kb, fr, len, 0f, len, -0.18f, 0.1f, h - 0.42f + ofs, h - 0.28f + ofs, SStone, TileStone, 0);
            // LOD1: guscio unico + parapetto
            fr.Box(lod, 0, len, 0, depth, 0, h, 0, TileWall, 16 | 32);
            fr.Box(lod, 0, len, 0f, 0.35f, h, h + 0.6f, 0, TileWall, 16);
            fr.Box(lod, 0, len, 0.35f, depth, h, h + 0.06f, 0, TileWall, 16);

            // aperture per piano
            var perFloor = new List<WallOpening>[floors];
            var balconies = new HashSet<long>();
            int nBays = Mathf.Max(1, Mathf.RoundToInt(len / bay));
            float step = len / nBays;
            for (int f = 0; f < floors; f++)
            {
                var ops = new List<WallOpening>();
                for (int i = 0; i < nBays; i++)
                {
                    float c = (i + 0.5f) * step;
                    if (f == 0)
                    {
                        bool nearDoor = false;
                        foreach (var du in doorU) if (Mathf.Abs(du - c) < (opt != null ? doorW * 0.5f + 0.8f : 1.45f)) nearDoor = true;
                        if (nearDoor) continue;
                        if (opt != null && opt.cieco) continue;
                    }
                    if (c < 0.9f || c > len - 0.9f) continue;
                    bool bal = f >= 1 && r.NextDouble() < 0.22;
                    if (opt == null && bal && BalconeBloccato(fr.W(c, 0f, 0f))) bal = false; // il balcone (sporge 78 cm) andrebbe a incastrarsi con lampioni, edicole o fili
                    if (opt != null && bal && ((opt.balconyMask >> f) & 1) == 0) bal = false;
                    float y0 = f * 3f + (bal ? 0.28f : 1.1f);
                    var op = new WallOpening(c, 0.95f, y0, f * 3f + 2.55f);
                    ops.Add(op);
                    if (bal) balconies.Add(f * 100000L + Mathf.RoundToInt(c * 100f));
                }
                if (f == 0) foreach (var du in doorU) ops.Add(new WallOpening(du, doorW, 0f, doorH, true));
                perFloor[f] = ops;
            }

            for (int f = 0; f < floors; f++)
            {
                float y0 = f * 3f, y1 = y0 + 3f;
                var ops = perFloor[f];
                ShellPieces(kb, fr, len, y0, y1, T, new List<WallOpening>(ops), SWall, TileWall, true, true); // la faccia superiore dell'ultimo piano e' sempre coperta da parapetto e tetto

                foreach (var o in ops)
                {
                    float w = o.w;
                    if (o.door)
                    {
                        // cornice in pietra + stipite in legno + soglia
                        fr.Box(kb, o.U0 - 0.16f, o.U0 + Riporto, -0.06f, 0.3f, 0f, o.y1 + 0.02f, SStone, TileStone, 16);
                        fr.Box(kb, o.U1 - Riporto, o.U1 + 0.16f, -0.06f, 0.3f, 0f, o.y1 + 0.02f, SStone, TileStone, 16);
                        fr.Box(kb, o.U0 - 0.16f, o.U1 + 0.16f, -0.09f, 0.3f, o.y1 - Riporto, o.y1 + 0.2f, SStone, TileStone, 0);
                        fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.3f, 0f, -0.1f, 0.12f, SStone, TileStone, 16);
                        fr.Box(kb, o.U0, o.U0 + 0.1f, 0.08f, 0.3f, 0f, o.y1, SWood, TileWood, 16);
                        fr.Box(kb, o.U1 - 0.1f, o.U1, 0.08f, 0.3f, 0f, o.y1, SWood, TileWood, 16);
                        fr.Box(kb, o.U0, o.U1, 0.08f, 0.3f, o.y1 - 0.1f, o.y1, SWood, TileWood, 0);
                        if (dk == 0) fr.Box(kb, o.U0, o.U1, 0.245f, 0.28f, 0f, o.y1, SGlass, 2f, 0); // dietro il battente (che occupa 0,14-0,24): niente facce complanari
                        else if (dk == 1) DoorLeafBoxes(kb, fr, o.U0, o.U1, o.y1, false);
                        else if (dk == 3) DoorLeafBoxes(kb, fr, o.U0, o.U1, o.y1, true);
                        else if (dk == 4) GateBars(kb, fr, o.U0, o.U1, o.y1);
                        fr.Box(lod, o.U0, o.U1, -0.01f, 0.0f, 0f, o.y1, 1, 2f, 0);
                        continue;
                    }
                    bool bal = balconies.Contains(f * 100000L + Mathf.RoundToInt(o.u * 100f));
                    // vetro scuro e telaio
                    fr.Box(kb, o.U0, o.U1, 0.26f, 0.29f, o.y0, o.y1, SGlass, 2f, 0);
                    fr.Box(kb, o.u - 0.025f, o.u + 0.025f, 0.18f, 0.3f, o.y0, o.y1, SWood, TileWood, 0);
                    float mid = o.y0 + (o.y1 - o.y0) * 0.62f;
                    fr.Box(kb, o.U0, o.U1, 0.18f, 0.3f, mid - 0.025f, mid + 0.025f, SWood, TileWood, 0);
                    fr.Box(kb, o.U0, o.U0 + 0.05f, 0.14f, 0.3f, o.y0, o.y1, SWood, TileWood, 0);
                    fr.Box(kb, o.U1 - 0.05f, o.U1, 0.14f, 0.3f, o.y0, o.y1, SWood, TileWood, 0);
                    fr.Box(lod, o.U0, o.U1, -0.01f, 0.0f, o.y0, o.y1, 1, 2f, 0);
                    // cornice in pietra
                    fr.Box(kb, o.U0 - 0.13f, o.U1 + 0.13f, -0.06f, 0.2f, o.y1 - Riporto, o.y1 + 0.15f, SStone, TileStone, 0);
                    fr.Box(kb, o.U0 - 0.1f, o.U0 + Riporto, -0.04f, 0.2f, o.y0, o.y1, SStone, TileStone, 0);
                    fr.Box(kb, o.U1 - Riporto, o.U1 + 0.1f, -0.04f, 0.2f, o.y0, o.y1, SStone, TileStone, 0);
                    if (!bal)
                    {
                        fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.15f, 0.2f, o.y0 - 0.08f, o.y0 + Riporto, SStone, TileStone, 0);
                    }
                    else
                    {
                        float sy = f * 3f + 0.24f;
                        fr.Box(kb, o.u - 0.85f, o.u + 0.85f, -0.78f, 0f, sy - 0.14f, sy, SStone, TileStone, 0);
                        for (int i = 0; i <= 9; i++)
                        {
                            float pu = o.u - 0.8f + i * (1.6f / 9f);
                            fr.Box(kb, pu - 0.012f, pu + 0.012f, -0.75f, -0.71f, sy, sy + 0.92f, SIron, 1f, 16 | 32);
                        }
                        for (int i = 1; i <= 4; i++)
                        {
                            float pd = -0.73f + i * 0.146f;
                            fr.Box(kb, o.u - 0.81f, o.u - 0.78f, pd - 0.012f, pd + 0.012f, sy, sy + 0.92f, SIron, 1f, 16 | 32);
                            fr.Box(kb, o.u + 0.78f, o.u + 0.81f, pd - 0.012f, pd + 0.012f, sy, sy + 0.92f, SIron, 1f, 16 | 32);
                        }
                        fr.Box(kb, o.u - 0.83f, o.u + 0.83f, -0.77f, -0.69f, sy + 0.92f, sy + 0.96f, SIron, 1f, 0);
                        fr.Box(kb, o.u - 0.83f, o.u - 0.77f, -0.77f, 0f, sy + 0.92f, sy + 0.96f, SIron, 1f, 0);
                        fr.Box(kb, o.u + 0.77f, o.u + 0.83f, -0.77f, 0f, sy + 0.92f, sy + 0.96f, SIron, 1f, 0);
                    }
                    // persiane
                    double roll = r.NextDouble();
                    if (!bal && roll < 0.38)
                    {
                        float lw = w * 0.5f;
                        fr.Box(kb, o.U0 - 0.12f - lw, o.U0 - 0.12f, -0.1f, -0.06f, o.y0, o.y1, SPaint, TileWood, 0);
                        fr.Box(kb, o.U1 + 0.12f, o.U1 + 0.12f + lw, -0.1f, -0.06f, o.y0, o.y1, SPaint, TileWood, 0);
                    }
                    else if (!bal && roll < 0.52)
                    {
                        fr.Box(kb, o.U0 - 0.02f, o.U1 + 0.02f, -0.08f, -0.04f, o.y0 - 0.02f, o.y1 + 0.02f, SPaint, TileWood, 0);
                    }
                }
            }

            // zoccolo in pietra (interrotto dalle porte) e fasce marcapiano
            var gaps = new List<Vector2>();
            float cursor = 0f;
            var sortedDoors = new List<float>(doorU);
            sortedDoors.Sort();
            foreach (var du in sortedDoors)
            {
                if (du - 0.75f > cursor) gaps.Add(new Vector2(cursor, du - 0.75f));
                cursor = du + 0.75f;
            }
            if (len > cursor) gaps.Add(new Vector2(cursor, len));
            foreach (var g in gaps)
            {
                // lo zoccolo scende sotto il suolo (-30 cm): la fascia di quota dei moduli non lascia fessure alla base
                Trim(kb, fr, len, g.x, g.y, -0.06f, T, -0.3f, 0.93f + ofs, SStone, TileStone, 8 | 16); // 0,93: non coincide con la sommita' dei gradini (multipli di 15 cm)
                Trim(kb, fr, len, g.x, g.y, -0.1f, T, 0.93f + ofs, 0.99f + ofs, SStone, TileStone, 8 | 16);
            }
            for (int f = 1; f < floors; f++)
                Trim(kb, fr, len, 0f, len, -0.05f, 0.1f, f * 3f - 0.08f + ofs, f * 3f + 0.08f + ofs, SStone, TileStone, 0);

            // comignoli
            if (depth > 2.4f)
            {
                int ch = len > 8f ? 2 : 1;
                for (int i = 0; i < ch; i++)
                {
                    float cu = 1f + (float)r.NextDouble() * Mathf.Max(0.1f, len - 2.5f);
                    float cd = depth * (0.5f + 0.3f * (float)r.NextDouble());
                    fr.Box(kb, cu, cu + 0.7f, cd, cd + 0.7f, h + 0.06f, h + 1.7f, SWall, TileWall, 16);
                    fr.Box(kb, cu - 0.06f, cu + 0.76f, cd - 0.06f, cd + 0.76f, h + 1.7f, h + 1.82f, SStone, TileStone, 16);
                }
            }

            // oggetto di scena: radice, LOD0, LOD1, collider unico del corpo
            if (opt != null)
            {
                var c = new FacadeCache();
                c.m0 = kb.Build(opt.key);
                c.m1 = lod.Build(opt.key + "_LOD1");
                fr.Bounds(0, len, 0, depth, 0, h, out c.bcc, out c.bcs);
                facadeCache[opt.key] = c;
                kitMeshes++; kitTris += c.m0.triangles.Length / 3;
                return FacadeInstance(parent, name, subs, plaster, c);
            }
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var m0 = kb.Build(KitName(name));
            var m1 = lod.Build(KitName(name + "_LOD1"));
            var g0 = MeshObject(root.transform, "LOD0", m0, subs, false);
            var g1 = MeshObject(root.transform, "LOD1", m1, new[] { plaster, "Finestra" }, false);
            var lg = root.AddComponent<LODGroup>();
            lg.SetLODs(new[]
            {
                new LOD(0.30f, new Renderer[] { g0.GetComponent<Renderer>() }),
                new LOD(0.01f, new Renderer[] { g1.GetComponent<Renderer>() })
            });
            lg.RecalculateBounds();
            Vector3 bcc, bcs;
            fr.Bounds(0, len, 0, depth, 0, h, out bcc, out bcs);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = bcc; bc.size = bcs;
            return root;
        }

        // battente in legno (porta chiusa o portone con assi e traverse) dentro il vano
        static void DoorLeafBoxes(KitBuilder kb, WallFrame fr, float u0, float u1, float y1, bool portone)
        {
            fr.Box(kb, u0, u1, 0.12f, 0.24f, 0f, y1, SWood, TileWood, 16);
            if (portone)
            {
                for (float u = u0 + 0.35f; u < u1 - 0.2f; u += 0.5f) fr.Box(kb, u, u + 0.03f, 0.07f, 0.12f, 0.05f, y1 - 0.05f, SWood, TileWood, 0);
                fr.Box(kb, u0 + 0.05f, u1 - 0.05f, 0.05f, 0.12f, 0.55f, 0.7f, SIron, 1f, 0);
                fr.Box(kb, u0 + 0.05f, u1 - 0.05f, 0.05f, 0.12f, y1 - 0.8f, y1 - 0.65f, SIron, 1f, 0);
                fr.Box(kb, (u0 + u1) * 0.5f - 0.02f, (u0 + u1) * 0.5f + 0.02f, 0.05f, 0.12f, 0.05f, y1 - 0.05f, SIron, 1f, 0);
            }
            else
            {
                fr.Box(kb, u0 + 0.14f, u1 - 0.14f, 0.09f, 0.12f, 0.2f, y1 * 0.42f, SWood, TileWood, 0);
                fr.Box(kb, u0 + 0.14f, u1 - 0.14f, 0.09f, 0.12f, y1 * 0.5f, y1 - 0.15f, SWood, TileWood, 0);
                fr.Box(kb, u1 - 0.24f, u1 - 0.12f, 0.06f, 0.12f, 1.0f, 1.2f, SIron, 1f, 0);
            }
        }

        // cancello in ferro: montanti e traverse, dietro si vede il buio
        static void GateBars(KitBuilder kb, WallFrame fr, float u0, float u1, float y1)
        {
            for (float u = u0 + 0.08f; u < u1; u += 0.14f) fr.Box(kb, u - 0.012f, u + 0.012f, 0.2f, 0.224f, 0f, y1, SIron, 1f, 16);
            fr.Box(kb, u0, u1, 0.19f, 0.23f, 0.25f, 0.32f, SIron, 1f, 0);
            fr.Box(kb, u0, u1, 0.19f, 0.23f, y1 * 0.5f, y1 * 0.5f + 0.07f, SIron, 1f, 0);
            fr.Box(kb, u0, u1, 0.19f, 0.23f, y1 - 0.12f, y1 - 0.05f, SIron, 1f, 0);
        }

        // interno accennato di un basso aperto (tutto dentro la mesh della facciata: nessun oggetto separato da far compenetrare)
        static void PocketInterior(KitBuilder kb, WallFrame fr, float u0, float u1, float dIn, float dBack, int variant)
        {
            if (variant < 0) return; // cancello: solo il buio
            if (variant == 0)
            {   // tavolo con due sgabelli e scaffale con sacchi
                fr.Box(kb, u0 + 0.25f, u0 + 0.25f + 0.8f, dIn + 0.4f, dIn + 1.0f, 0.74f, 0.79f, SWood, TileWood, 0);
                foreach (var dx in new[] { 0.28f, 0.72f }) foreach (var dz in new[] { 0.45f, 0.95f })
                    fr.Box(kb, u0 + dx - 0.03f, u0 + dx + 0.03f, dIn + dz - 0.03f, dIn + dz + 0.03f, 0f, 0.74f, SWood, TileWood, 16);
                fr.Box(kb, u1 - 0.85f, u1 - 0.1f, dBack - 0.35f, dBack, 0f, 1.7f, SWood, TileWood, 16);
                fr.Box(kb, u1 - 0.8f, u1 - 0.15f, dBack - 0.3f, dBack - 0.02f, 0.9f, 1.1f, SPaint, TileWood, 0);
            }
            else if (variant == 1)
            {   // letto addossato alla parete di fondo e comodino
                fr.Box(kb, u0 + 0.1f, u0 + 1.0f, dBack - 1.9f, dBack - 0.05f, 0.32f, 0.38f, SWood, TileWood, 0);
                fr.Box(kb, u0 + 0.1f, u0 + 1.0f, dBack - 1.9f, dBack - 1.8f, 0f, 0.8f, SWood, TileWood, 16);
                fr.Box(kb, u0 + 0.12f, u0 + 0.98f, dBack - 1.78f, dBack - 0.07f, 0.38f, 0.52f, SPaint, TileWood, 0);
                fr.Box(kb, u1 - 0.55f, u1 - 0.1f, dBack - 0.5f, dBack - 0.05f, 0f, 0.55f, SWood, TileWood, 16);
            }
            else
            {   // bottega: bancone, ripiani e botte
                fr.Box(kb, u0 + 0.1f, u1 - 0.1f, dIn + 0.45f, dIn + 0.9f, 0f, 0.95f, SWood, TileWood, 16);
                fr.Box(kb, u0 + 0.05f, u1 - 0.05f, dIn + 0.4f, dIn + 0.95f, 0.95f, 1.0f, SStone, TileStone, 0);
                fr.Box(kb, u0 + 0.05f, u1 - 0.05f, dBack - 0.3f, dBack, 0.9f, 0.95f, SWood, TileWood, 0);
                fr.Box(kb, u0 + 0.05f, u1 - 0.05f, dBack - 0.3f, dBack, 1.5f, 1.55f, SWood, TileWood, 0);
                fr.Box(kb, u0 + 0.05f, u0 + 0.9f, dBack - 0.9f, dBack - 0.1f, 0f, 0.9f, SPaint, TileWood, 16);
            }
        }

        // wrapper con la vecchia firma: edificio lungo z con facciata verso il vicolo
        static void Building(Transform parent, int side, float innerAbs, float z0, float z1, float depth,
                             int floors, string wallMat, System.Random r, float winStep)
        {
            float innerX = side * innerAbs;
            var fr = new WallFrame(new Vector3(innerX, 0f, z0), Vector3.forward, new Vector3(-side, 0f, 0f));
            var doors = new List<float>();
            foreach (var p in doorWallPoints)
                if (Mathf.Abs(p.x - innerX) < 0.01f && p.z > z0 + 0.7f && p.z < z1 - 0.7f) doors.Add(p.z - z0);
            Facade(parent, "Casa_" + (side < 0 ? "O" : "E") + Mathf.RoundToInt(z0 + 100), fr, z1 - z0, depth, floors, PlasterFor(wallMat), r, winStep, doors);
        }

        // chiusura trasversale del vicolo (normale n = verso il vicolo)
        static void Closure(Transform parent, string name, float zFront, Vector3 n, float x0, float x1, float depth, int floors,
                            string wallMat, System.Random r, float bay)
        {
            var fr = new WallFrame(new Vector3(x0, 0f, zFront), Vector3.right, n);
            var doors = new List<float>();
            foreach (var p in doorWallPoints)
                if (Mathf.Abs(p.z - zFront) < 0.01f && p.x > x0 + 0.7f && p.x < x1 - 0.7f) doors.Add(p.x - x0);
            Facade(parent, name, fr, x1 - x0, depth, floors, PlasterFor(wallMat), r, bay, doors);
        }

        // ------------------------------------------------------------------ porte

        static GameObject DoorLeaf(Transform parent, string name, WallFrame fr, float u, float dFront, float dBack, float width, float height, float protrude = 0.1f)
        {
            var kb = new KitBuilder("M_LegnoVernicePelata", "M_FerroRuggine02");
            float hw = width * 0.5f;
            fr.Box(kb, u - hw, u + hw, dFront, dBack, 0f, height, 0, TileWood, 16);
            // specchiature
            float p0 = dFront - 0.025f;
            fr.Box(kb, u - hw + 0.12f, u + hw - 0.12f, p0, dFront, 0.18f, height * 0.42f, 0, TileWood, 4 * 4);
            fr.Box(kb, u - hw + 0.12f, u + hw - 0.12f, p0, dFront, height * 0.5f, height - 0.15f, 0, TileWood, 4 * 4);
            // maniglia e piastra
            fr.Box(kb, u + hw - 0.22f, u + hw - 0.1f, dFront - 0.05f, dFront, 1.0f, 1.2f, 1, 1f, 0);
            fr.Box(kb, u + hw - 0.2f, u + hw - 0.12f, dFront - 0.1f, dFront - 0.05f, 1.08f, 1.12f, 1, 1f, 0);
            var mesh = kb.Build(KitName(name));
            var go = MeshObject(parent, name, mesh, new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, false);
            Vector3 c, s;
            fr.Bounds(u - hw, u + hw, -protrude, dBack, 0f, height, out c, out s); // il collider sporge davanti al piano della parete
            var bc = go.AddComponent<BoxCollider>();
            bc.center = c; bc.size = s;
            return go;
        }

        // porta con battente incassato e insegna; wallPoint = punto sul piano della facciata a terra, normal = verso il vicolo
        static Door MakeDoor(Transform doorsParent, Transform envParent, string id, Vector3 wallPoint,
                             Vector3 normal, string signMat)
        {
            var fr = new WallFrame(wallPoint, Vector3.Cross(Vector3.up, normal), normal);
            var go = DoorLeaf(doorsParent, "Porta_" + id, fr, 0f, 0.14f, 0.24f, 1.1f, 2.25f);
            // insegna sopra l'architrave
            var euler = Quaternion.LookRotation(normal).eulerAngles;
            Box("Insegna_" + id, envParent, wallPoint + normal * 0.07f + Vector3.up * 2.84f, new Vector3(1.5f, 0.3f, 0.06f), signMat, euler, false);
            doorWallPoints.Add(wallPoint);
            var d = go.AddComponent<Door>();
            d.luogoId = id;
            return d;
        }

        // ------------------------------------------------------------------ props esterni

        // pezzi di ogni cumulo di macerie: si riposano sul pavimento o sui pezzi sotto (AssestaMacerie) dopo lo spostamento automatico
        static readonly List<List<GameObject>> cumuli = new List<List<GameObject>>();
        static readonly List<GameObject> travi = new List<GameObject>();

        static void Rubble(Transform parent, System.Random r, Vector3 center, int count, float radius, float maxScale = 2.0f)
        {
            Ground("TerraMacerie", parent, center + new Vector3(-radius * 1.8f, 0.001f, -radius * 1.8f), center + new Vector3(radius * 1.8f, 0.014f, radius * 1.8f),
                "M_TerraPolverosa", 1.5f, false);
            var pezzi = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                float s = 1.0f + (float)r.NextDouble() * (maxScale - 1.0f);
                var off = new Vector3((float)(r.NextDouble() * 2 - 1) * radius, 0f, (float)(r.NextDouble() * 2 - 1) * radius);
                var go = Model("Macerie_Mattoni", parent, center + off, (float)r.NextDouble() * 360f, s, true, "Macerie");
                go.transform.localRotation = Quaternion.Euler((float)r.NextDouble() * 12f, go.transform.localEulerAngles.y, (float)r.NextDouble() * 12f);
                pezzi.Add(go);
            }
            cumuli.Add(pezzi);
            var trave = Box("TraveSpezzata", parent, center + new Vector3(0.2f, 0.6f, 0.1f), new Vector3(0.14f, 0.14f, 1.8f), "M_AssiStagionate", new Vector3(18f, 35f, 8f));
            travi.Add(trave);
        }

        // dopo lo spostamento automatico: ogni pezzo si posa sul pavimento o sul pezzo sotto (mai sotto terra, mai in aria); la trave si appoggia sul cumulo
        static void AssestaMacerie()
        {
            for (int k = 0; k < cumuli.Count; k++)
            {
                var posati = new List<GameObject>();
                int n = 0;
                foreach (var g in cumuli[k])
                {
                    PosaSuCumulo(g, posati, n++ >= cumuli[k].Count / 2); // la seconda meta' dei pezzi si impila sui primi
                    posati.Add(g);
                }
                if (k < travi.Count && travi[k] != null && posati.Count > 0)
                {
                    float top = 0f;
                    foreach (var g in posati) top = Mathf.Max(top, BoundsDi(g).max.y);
                    var b = BoundsDi(travi[k]);
                    travi[k].transform.position += Vector3.up * (top - 0.3f - b.min.y); // la trave affonda nel cumulo
                }
            }
            cumuli.Clear();
            travi.Clear();
        }

        static void Cart(Transform parent, Vector3 pos)
        {
            Model("Carretto_Filip", parent, pos, 90f, 1.05f, true, "Carretto"); // R007 t2: 1,05 (era 1,15) per non incastrarsi tra facciata e tavolini
        }

        static void Bucket(Transform parent, Vector3 pos)
        {
            Model("SecchioMetallo", parent, pos, pos.x * 37f, 1.1f, true, "Secchio");
        }

        static void Lamp(Transform parent, Vector3 pos)
        {
            noBalcone.Add(pos);
            var go = Model("LampioneStrada_Pixie", parent, pos, 0f, 1.25f, true, "Lampione");
            var l = new GameObject("LuceLampione");
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 2.75f, 0f);
        }

        // lampione da muro vicino a una porta
        static void WallLamp(Transform parent, Vector3 wallPoint, Vector3 normal, Vector3 along, float shift)
        {
            float yaw = Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg;
            var p = wallPoint + along * shift + normal * 0.12f + Vector3.up * 2.1f;
            var go = Model("LampioneMuro_Kigha", parent, p, yaw, 1.4f, false, "LampioneMuro");
            var l = new GameObject("LuceLampione");
            l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0f, 0.25f, 0.1f);
        }

        // edicola votiva sul muro; normal = verso il vicolo
        static void Shrine(Transform parent, Vector3 wallPoint, Vector3 normal)
        {
            noBalcone.Add(wallPoint);
            var fr = new WallFrame(wallPoint, Vector3.Cross(Vector3.up, normal), normal);
            var kb = new KitBuilder("M_ArenariaBlocchi", "M_LegnoDipintoBlu");
            fr.Box(kb, -0.58f, -0.4f, -0.14f, 0f, 1.2f, 2.7f, 0, TileStone, 0);
            fr.Box(kb, 0.4f, 0.58f, -0.14f, 0f, 1.2f, 2.7f, 0, TileStone, 0);
            fr.Box(kb, -0.65f, 0.65f, -0.18f, 0f, 2.7f, 2.85f, 0, TileStone, 0);
            fr.Box(kb, -0.5f, 0.5f, -0.15f, 0f, 2.85f, 2.95f, 0, TileStone, 0);
            fr.Box(kb, -0.3f, 0.3f, -0.12f, 0f, 2.95f, 3.05f, 0, TileStone, 0);
            fr.Box(kb, -0.68f, 0.68f, -0.32f, 0f, 1.12f, 1.2f, 0, TileStone, 0);
            fr.Box(kb, -0.4f, 0.4f, -0.04f, -0.01f, 1.2f, 2.7f, 1, 1f, 0);
            var mesh = kb.Build(KitName("Edicola"));
            MeshObject(parent, "Edicola", mesh, new[] { "M_ArenariaBlocchi", "M_LegnoDipintoBlu" }, true);
            // R007 t2: statuetta della Madonna (modello vero, 0,9 m) scalata a 0,45 m per le edicole piccole; la base appoggia sul ripiano a 1,38 m
            Model("MadonnaStatuetta", parent, fr.W(-0.05f, -0.17f, 1.38f), Mathf.Atan2(fr.n.x, fr.n.z) * Mathf.Rad2Deg, 0.5f, false, "EdicolaStatuetta");
            Model("Candela", parent, fr.W(0.32f, -0.2f, 1.2f), 0f, 1.0f, false, "EdicolaCandela");
            Prim(PrimitiveType.Sphere, "EdicolaLumino", parent, fr.W(0.32f, -0.2f, 1.33f), new Vector3(0.05f, 0.08f, 0.05f), "Glow", null, false);
        }

        // ------------------------------------------------------------------ 10_Hub_Vicolo

        // ------------------------------------------------------------------ interni

        // parete interna con aperture, zoccolo e cappello; fr: o sul piano interno, n verso la stanza, d verso l'esterno
        static GameObject InteriorWall(Transform parent, string name, WallFrame fr, float len, float h, float thick,
                                       List<WallOpening> ops, string plaster, string dado, float dadoH, string trim, bool glassPane = true)
        {
            string[] subs = { plaster, dado, "M_LegnoVernicePelata", "Finestra", "M_FerroRuggine02", trim };
            var kb = new KitBuilder(subs);
            kb.SubFisso = 0;
            int hn = 0; foreach (char ch in name) hn += ch;
            float dofs = ((fr.AxisU == 0 ? 0 : 1) + (hn % 2) * 2) * PassoFascia; // pareti che si incontrano a spigolo (assi diversi) o in linea (nome diverso): dado a quote diverse (niente facce superiori complanari)
            kb.ProteggiTestate(fr.AxisU, fr.AxisU == 0 ? fr.W(0, 0, 0).x : fr.W(0, 0, 0).z, fr.AxisU == 0 ? fr.W(len, 0, 0).x : fr.W(len, 0, 0).z);
            ShellPieces(kb, fr, len, 0f, h, thick, new List<WallOpening>(ops), 0, TileWall, true, false, false);
            // zoccolo (interrotto dalle porte)
            float cursor = 0f;
            var sorted = new List<WallOpening>(ops);
            sorted.Sort((p, q) => p.u.CompareTo(q.u));
            var gaps = new List<Vector2>();
            foreach (var o in sorted)
            {
                if (o.y0 > 0.01f) continue;
                if (o.U0 - 0.08f > cursor) gaps.Add(new Vector2(cursor, o.U0 - 0.08f));
                cursor = o.U1 + 0.08f;
            }
            if (len > cursor) gaps.Add(new Vector2(cursor, len));
            foreach (var g in gaps)
            {
                Trim(kb, fr, len, g.x, g.y, -0.03f, 0.02f, 0f, dadoH + dofs, 1, TileWall, 8 | 16);
                Trim(kb, fr, len, g.x, g.y, -0.045f, 0.02f, dadoH + dofs, dadoH + 0.05f + dofs, 5, TileStone, 8);
            }
            fr.Box(kb, -0.02f, len + 0.02f, -0.02f, thick + 0.02f, h, h + 0.1f, 5, TileStone, 16);

            foreach (var o in ops)
            {
                bool isDoor = o.y0 < 0.01f;
                // stipiti e architrave in legno
                fr.Box(kb, o.U0 - 0.08f, o.U0 + Riporto, -0.04f, 0.28f, o.y0, o.y1, 2, TileWood, isDoor ? 16 : 0);
                fr.Box(kb, o.U1 - Riporto, o.U1 + 0.08f, -0.04f, 0.28f, o.y0, o.y1, 2, TileWood, isDoor ? 16 : 0);
                fr.Box(kb, o.U0 - 0.08f, o.U1 + 0.08f, -0.04f, 0.28f, o.y1 - Riporto, o.y1 + 0.1f, 2, TileWood, 0);
                if (!isDoor)
                {
                    fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.1f, 0.28f, o.y0 - 0.06f, o.y0 + Riporto, 2, TileWood, 0);
                    // vetro e traversi
                    if (glassPane) fr.Box(kb, o.U0, o.U1, 0.2f, 0.23f, o.y0, o.y1, 3, 2f, 0);
                    float midU = (o.U0 + o.U1) * 0.5f, midY = (o.y0 + o.y1) * 0.5f;
                    fr.Box(kb, midU - 0.02f, midU + 0.02f, 0.14f, 0.24f, o.y0, o.y1, 2, TileWood, 0);
                    fr.Box(kb, o.U0, o.U1, 0.14f, 0.24f, midY - 0.02f, midY + 0.02f, 2, TileWood, 0);
                }
            }
            var mesh = kb.Build(KitName(name));
            var go = MeshObject(parent, name, mesh, subs, false);
            // collider a pezzi: i varchi (porte e finestre) restano liberi; sotto e sopra le aperture il muro e' pieno
            var colOps = new List<WallOpening>(ops);
            colOps.Sort((p, q) => p.u.CompareTo(q.u));
            float cu = 0f;
            Action<float, float, float, float> pezzo = (u0, u1, y0, y1) =>
            {
                if (u1 - u0 < 0.001f || y1 - y0 < 0.001f) return;
                Vector3 cc, ss;
                fr.Bounds(u0, u1, 0, thick, y0, y1, out cc, out ss);
                var bc = go.AddComponent<BoxCollider>();
                bc.center = cc; bc.size = ss;
            };
            foreach (var o in colOps)
            {
                pezzo(cu, o.U0, 0f, h);
                pezzo(o.U0, o.U1, 0f, o.y0);
                pezzo(o.U0, o.U1, o.y1, h);
                if (o.y0 > 0.01f)
                {
                    // R018: la finestra e' chiusa dal vetro (collider su un figlio dedicato): la camera non esce dalla stanza.
                    // Al giocatore non cambia nulla, il davanzale e' a 1,1 m.
                    Vector3 vc, vs;
                    fr.Bounds(o.U0, o.U1, 0, thick, o.y0, o.y1, out vc, out vs);
                    var vetro = new GameObject("VetroFinestraCamera");
                    vetro.transform.SetParent(go.transform, false);
                    var vb = vetro.AddComponent<BoxCollider>();
                    vb.center = vc; vb.size = vs;
                } // R018: la finestra e' chiusa dal vetro: la camera non esce dalla stanza (al giocatore non cambia nulla, il davanzale e' a 1,1 m)
                cu = o.U1;
            }
            pezzo(cu, len, 0f, h);
            return go;
        }

    }
}
