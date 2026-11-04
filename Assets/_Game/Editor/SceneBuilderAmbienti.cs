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

        static GameObject Model(string model, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool collider = true, string objName = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/" + model + "/" + model + ".fbx");
            if (asset == null)
            {
                Debug.LogError("[SceneBuilder] Modello mancante: " + model);
                return new GameObject(objName ?? model);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = objName ?? model;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;

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
            // LOD: i modelli sono a un solo livello, quindi LOD0 completo e culling sotto il 2% dello schermo
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(0.02f, rs) });
            lg.RecalculateBounds();
            propCount++;
            return go;
        }

        // ------------------------------------------------------------------ pareti con aperture

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

        static GameObject Facade(Transform parent, string name, WallFrame fr, float len, float depth, int floors, string plaster,
                                 System.Random r, float bay, List<float> doorU)
        {
            string[] subs = { plaster, "M_ArenariaBlocchi", "M_LegnoVernicePelata", "Finestra", "M_FerroRuggine02", "M_LegnoDipintoBlu", "M_CoppiArgilla" };
            var kb = new KitBuilder(subs);
            var lod = new KitBuilder(plaster, "Finestra");
            float h = floors * 3f;
            const float T = 0.45f;

            // volumi: corpo (dietro il guscio), tetto, parapetto, cornicione
            fr.Box(kb, 0, len, T, depth, 0, h, SWall, TileWall, 4 | 16 | 32);
            fr.Box(kb, 0, len, 0.35f, depth, h, h + 0.06f, SRoof, 1.5f, 16);
            fr.Box(kb, 0, len, 0f, 0.35f, h, h + 0.6f, SWall, TileWall, 16);
            fr.Box(kb, -0.02f, len + 0.02f, -0.06f, 0.41f, h + 0.6f, h + 0.68f, SStone, TileStone, 16);
            fr.Box(kb, 0, len, -0.3f, 0.1f, h - 0.28f, h, SStone, TileStone, 0);
            fr.Box(kb, 0, len, -0.18f, 0.1f, h - 0.42f, h - 0.28f, SStone, TileStone, 0);
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
                        foreach (var du in doorU) if (Mathf.Abs(du - c) < 1.45f) nearDoor = true;
                        if (nearDoor) continue;
                    }
                    if (c < 0.9f || c > len - 0.9f) continue;
                    bool bal = f >= 1 && r.NextDouble() < 0.22;
                    float y0 = f * 3f + (bal ? 0.28f : 1.1f);
                    var op = new WallOpening(c, 0.95f, y0, f * 3f + 2.55f);
                    ops.Add(op);
                    if (bal) balconies.Add(f * 100000L + Mathf.RoundToInt(c * 100f));
                }
                if (f == 0) foreach (var du in doorU) ops.Add(new WallOpening(du, 1.3f, 0f, 2.4f, true));
                perFloor[f] = ops;
            }

            for (int f = 0; f < floors; f++)
            {
                float y0 = f * 3f, y1 = y0 + 3f;
                var ops = perFloor[f];
                ShellPieces(kb, fr, len, y0, y1, T, new List<WallOpening>(ops), SWall, TileWall, true, f < floors - 1);

                foreach (var o in ops)
                {
                    float w = o.w;
                    if (o.door)
                    {
                        // cornice in pietra + stipite in legno + soglia
                        fr.Box(kb, o.U0 - 0.16f, o.U0, -0.06f, 0.3f, 0f, o.y1 + 0.02f, SStone, TileStone, 16);
                        fr.Box(kb, o.U1, o.U1 + 0.16f, -0.06f, 0.3f, 0f, o.y1 + 0.02f, SStone, TileStone, 16);
                        fr.Box(kb, o.U0 - 0.16f, o.U1 + 0.16f, -0.09f, 0.3f, o.y1, o.y1 + 0.2f, SStone, TileStone, 0);
                        fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.3f, 0f, 0f, 0.12f, SStone, TileStone, 16);
                        fr.Box(kb, o.U0, o.U0 + 0.1f, 0.08f, 0.3f, 0f, o.y1, SWood, TileWood, 16);
                        fr.Box(kb, o.U1 - 0.1f, o.U1, 0.08f, 0.3f, 0f, o.y1, SWood, TileWood, 16);
                        fr.Box(kb, o.U0, o.U1, 0.08f, 0.3f, o.y1 - 0.1f, o.y1, SWood, TileWood, 0);
                        fr.Box(kb, o.U0, o.U1, 0.2f, 0.24f, 0f, o.y1, SGlass, 2f, 0);
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
                    fr.Box(kb, o.U0 - 0.13f, o.U1 + 0.13f, -0.06f, 0.2f, o.y1, o.y1 + 0.15f, SStone, TileStone, 0);
                    fr.Box(kb, o.U0 - 0.1f, o.U0, -0.04f, 0.2f, o.y0, o.y1, SStone, TileStone, 0);
                    fr.Box(kb, o.U1, o.U1 + 0.1f, -0.04f, 0.2f, o.y0, o.y1, SStone, TileStone, 0);
                    if (!bal)
                    {
                        fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.15f, 0.2f, o.y0 - 0.08f, o.y0, SStone, TileStone, 0);
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
                fr.Box(kb, g.x, g.y, -0.06f, T, 0f, 0.9f, SStone, TileStone, 8 | 16);
                fr.Box(kb, g.x, g.y, -0.1f, T, 0.9f, 0.96f, SStone, TileStone, 8 | 16);
            }
            for (int f = 1; f < floors; f++)
                fr.Box(kb, 0, len, -0.05f, 0.1f, f * 3f - 0.08f, f * 3f + 0.08f, SStone, TileStone, 0);

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

        static void Rubble(Transform parent, System.Random r, Vector3 center, int count, float radius)
        {
            Ground("TerraMacerie", parent, center + new Vector3(-radius * 1.8f, 0.001f, -radius * 1.8f), center + new Vector3(radius * 1.8f, 0.014f, radius * 1.8f),
                "M_TerraPolverosa", 1.5f, false);
            for (int i = 0; i < count; i++)
            {
                float s = 1.0f + (float)r.NextDouble() * 1.0f;
                var off = new Vector3((float)(r.NextDouble() * 2 - 1) * radius, 0f, (float)(r.NextDouble() * 2 - 1) * radius);
                float stack = i > count / 2 ? 0.3f : 0f;
                var go = Model("Macerie_Mattoni", parent, center + off + new Vector3(0f, stack, 0f), (float)r.NextDouble() * 360f, s, true, "Macerie");
                go.transform.localRotation = Quaternion.Euler((float)r.NextDouble() * 12f, go.transform.localEulerAngles.y, (float)r.NextDouble() * 12f);
            }
            Box("TraveSpezzata", parent, center + new Vector3(0.2f, 0.6f, 0.1f), new Vector3(0.14f, 0.14f, 1.8f), "M_AssiStagionate", new Vector3(18f, 35f, 8f));
        }

        static void Cart(Transform parent, Vector3 pos)
        {
            Model("Carretto_Filip", parent, pos, 90f, 1.15f, true, "Carretto");
        }

        static void Bucket(Transform parent, Vector3 pos)
        {
            Model("SecchioMetallo", parent, pos, pos.x * 37f, 1.1f, true, "Secchio");
        }

        static void Lamp(Transform parent, Vector3 pos)
        {
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
            Prim(PrimitiveType.Capsule, "EdicolaStatuetta", parent, fr.W(-0.05f, -0.12f, 1.6f), new Vector3(0.12f, 0.22f, 0.12f), "Cera", null, false);
            Model("Candela", parent, fr.W(0.32f, -0.2f, 1.2f), 0f, 1.0f, false, "EdicolaCandela");
            Prim(PrimitiveType.Sphere, "EdicolaLumino", parent, fr.W(0.32f, -0.2f, 1.33f), new Vector3(0.05f, 0.08f, 0.05f), "Glow", null, false);
        }

        // ------------------------------------------------------------------ 10_Hub_Vicolo

        static float HubInnerLeft(float z) { return (z >= -6f && z <= 2f) ? 5f : 3.5f; }
        static float HubInnerRight(float z) { return (z >= 8f && z <= 14f) ? 5f : 3.5f; }

        static void BuildHub()
        {
            var scene = NewScene();
            kitScene = "Hub"; kitIndex = 0;
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            Ambient(new Color(0.78f, 0.70f, 0.58f), new Color(0.55f, 0.47f, 0.38f), new Color(0.30f, 0.24f, 0.18f));
            Sun(new Color(1f, 0.90f, 0.72f), 1.0f, new Vector3(52f, -32f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.76f, 0.60f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 80f;

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // selciato in basoli di lava e canaletta centrale
            Street("Selciato", env, -7f, 7f, -25f, 25f, 0.22f);

            // porte (registrate prima delle case, che aprono i varchi)
            var basso = MakeDoor(porte, env, "basso_antonietta", new Vector3(-5f, 0f, -2f), Vector3.right, "InsegnaRossa");
            basso.targetScene = "20_Int_BassoAntonietta";
            basso.targetSpawn = "ingresso";
            MakeDoor(porte, env, "casa_capa_e_fierro", new Vector3(3.5f, 0f, -9f), Vector3.left, "InsegnaBlu");
            MakeDoor(porte, env, "sede_partito", new Vector3(5f, 0f, 11f), Vector3.left, "InsegnaRossa");
            MakeDoor(porte, env, "stazione", new Vector3(0f, 0f, 20f), Vector3.back, "InsegnaCrema");
            MakeDoor(porte, env, "vicolo_stracci", new Vector3(0f, 0f, -20f), Vector3.forward, "InsegnaBlu");

            // case lato sinistro
            Building(env, -1, 3.5f, -24f, -6f, 6f, 3, "Tufo", r, 2.6f);
            Building(env, -1, 5.0f, -6f, 2f, 6f, 2, "Intonaco", r, 2.6f);
            Building(env, -1, 3.5f, 2f, 10f, 6f, 4, "TufoScuro", r, 2.6f);
            Building(env, -1, 3.5f, 10f, 18f, 6f, 3, "Intonaco", r, 2.6f);
            Building(env, -1, 3.5f, 18f, 24f, 6f, 4, "Tufo", r, 2.6f);
            // case lato destro
            Building(env, 1, 3.5f, -24f, -6f, 6f, 3, "Intonaco", r, 2.6f);
            Building(env, 1, 3.5f, -6f, 8f, 6f, 4, "Tufo", r, 2.6f);
            Building(env, 1, 5.0f, 8f, 14f, 6f, 3, "TufoScuro", r, 2.6f);
            Building(env, 1, 3.5f, 14f, 24f, 6f, 3, "Intonaco", r, 2.6f);
            // chiusure del vicolo (stazione a nord, vicolo degli stracci a sud)
            Closure(env, "ChiusuraNord", 20f, Vector3.back, -3.5f, 3.5f, 4f, 4, "Tufo", r, 2.6f);
            Closure(env, "ChiusuraSud", -20f, Vector3.forward, -3.5f, 3.5f, 4f, 4, "TufoScuro", r, 2.6f);

            // lampioni a muro accanto alle porte
            WallLamp(env, new Vector3(-5f, 0f, -2f), Vector3.right, Vector3.forward, 1.25f);
            WallLamp(env, new Vector3(5f, 0f, 11f), Vector3.left, Vector3.forward, -1.25f);
            WallLamp(env, new Vector3(0f, 0f, 20f), Vector3.back, Vector3.right, 1.25f);

            // panni stesi
            float[] lz = { -17f, -12f, -3.5f, 1.0f, 5.5f, 12.5f, 16.5f };
            for (int i = 0; i < lz.Length; i++)
            {
                float z = lz[i];
                float yL = 5.0f + (i % 3) * 0.9f;
                float yR = yL - 0.35f - (float)r.NextDouble() * 0.4f;
                Clothesline(env, r, z, yL, yR, -HubInnerLeft(z) + 0.05f, HubInnerRight(z) - 0.05f, 4 + (i % 3));
            }

            // oggetti di scena
            Rubble(env, r, new Vector3(2.0f, 0f, -16.5f), 9, 0.8f);
            Cart(env, new Vector3(3.9f, 0f, 9.5f));
            Bucket(env, new Vector3(3.0f, 0f, -8.0f));
            Bucket(env, new Vector3(-3.3f, 0f, 5.5f));
            Model("SecchioLegno", env, new Vector3(-4.4f, 0f, -3.4f), 20f, 1.1f, true, "Secchio");
            Lamp(env, new Vector3(2.7f, 0f, -5.5f));
            Lamp(env, new Vector3(-2.7f, 0f, 14f));
            Shrine(env, new Vector3(-3.5f, 0f, 6.5f), Vector3.right);
            // panchina in tufo
            var kb = new KitBuilder("M_ArenariaBlocchi");
            kb.Box(new Vector3(-3.5f, 0f, 11.7f), new Vector3(-2.9f, 0.5f, 13.3f), 0, TileStone);
            MeshObject(env, "PanchinaTufo", kb.Build(KitName("Panchina")), new[] { "M_ArenariaBlocchi" }, true);
            // botte, casse e cesto
            Model("BotteVino", env, new Vector3(-3.1f, 0f, 3.8f), 0f, 1.25f, true, "Botte");
            Model("BotteVino", env, new Vector3(-3.15f, 0f, -15.0f), 40f, 1.25f, true, "Botte");
            Model("CassaLegno", env, new Vector3(3.0f, 0f, 16.5f), 25f, 1.5f, true, "Cassa");
            Model("CassaLegno", env, new Vector3(3.05f, 0.5f, 16.45f), -10f, 1.2f, true, "Cassa");
            Model("CestaVimini", env, new Vector3(-3.0f, 0f, 12.0f), 0f, 1.6f, false, "Cesto");
            Model("ScalaLegno", env, new Vector3(3.22f, 0f, -12.5f), 90f, 1.6f, false, "Scala"); // appoggiata al muro est (x = 3.5)
            SetStatic(env.gameObject);

            // muri invisibili ai bordi
            var inv = new GameObject("MuriInvisibili").transform;
            InvisibleWall("MuroInvisibile_Ovest", inv, new Vector3(-5.8f, 4f, 0f), new Vector3(1f, 8f, 44f));
            InvisibleWall("MuroInvisibile_Est", inv, new Vector3(5.8f, 4f, 0f), new Vector3(1f, 8f, 44f));
            InvisibleWall("MuroInvisibile_Nord", inv, new Vector3(0f, 4f, 20.5f), new Vector3(14f, 8f, 1f));
            InvisibleWall("MuroInvisibile_Sud", inv, new Vector3(0f, 4f, -20.5f), new Vector3(14f, 8f, 1f));

            // spawn
            Spawn("inizio", new Vector3(0f, 0f, -13f), 0f, spawns);
            Spawn("da_basso_antonietta", new Vector3(-1.8f, 0f, -2f), -90f, spawns); // al centro del vicolo, lontano dalla porta (x = -5)

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.85f, 0.76f, 0.60f), true);
            BuildPlayer(new Vector3(0f, 0.05f, -13f), 0f, cam);
            SaveScene(scene, "10_Hub_Vicolo");
        }

        // ------------------------------------------------------------------ interni

        // parete interna con aperture, zoccolo e cappello; fr: o sul piano interno, n verso la stanza, d verso l'esterno
        static GameObject InteriorWall(Transform parent, string name, WallFrame fr, float len, float h, float thick,
                                       List<WallOpening> ops, string plaster, string dado, float dadoH, string trim, bool glassPane = true)
        {
            string[] subs = { plaster, dado, "M_LegnoVernicePelata", "Finestra", "M_FerroRuggine02", trim };
            var kb = new KitBuilder(subs);
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
                fr.Box(kb, g.x, g.y, -0.03f, 0.02f, 0f, dadoH, 1, TileWall, 8 | 16);
                fr.Box(kb, g.x, g.y, -0.045f, 0.02f, dadoH, dadoH + 0.05f, 5, TileStone, 8);
            }
            fr.Box(kb, -0.02f, len + 0.02f, -0.02f, thick + 0.02f, h, h + 0.1f, 5, TileStone, 16);

            foreach (var o in ops)
            {
                bool isDoor = o.y0 < 0.01f;
                // stipiti e architrave in legno
                fr.Box(kb, o.U0 - 0.08f, o.U0, -0.04f, 0.28f, o.y0, o.y1, 2, TileWood, isDoor ? 16 : 0);
                fr.Box(kb, o.U1, o.U1 + 0.08f, -0.04f, 0.28f, o.y0, o.y1, 2, TileWood, isDoor ? 16 : 0);
                fr.Box(kb, o.U0 - 0.08f, o.U1 + 0.08f, -0.04f, 0.28f, o.y1, o.y1 + 0.1f, 2, TileWood, 0);
                if (!isDoor)
                {
                    fr.Box(kb, o.U0 - 0.1f, o.U1 + 0.1f, -0.1f, 0.28f, o.y0 - 0.06f, o.y0, 2, TileWood, 0);
                    // vetro e traversi
                    if (glassPane) fr.Box(kb, o.U0, o.U1, 0.2f, 0.23f, o.y0, o.y1, 3, 2f, 0);
                    float midU = (o.U0 + o.U1) * 0.5f, midY = (o.y0 + o.y1) * 0.5f;
                    fr.Box(kb, midU - 0.02f, midU + 0.02f, 0.14f, 0.24f, o.y0, o.y1, 2, TileWood, 0);
                    fr.Box(kb, o.U0, o.U1, 0.14f, 0.24f, midY - 0.02f, midY + 0.02f, 2, TileWood, 0);
                }
            }
            var mesh = kb.Build(KitName(name));
            var go = MeshObject(parent, name, mesh, subs, false);
            Vector3 c, s;
            fr.Bounds(0, len, 0, thick, 0, h, out c, out s);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = c; bc.size = s;
            return go;
        }

        static void BuildBasso()
        {
            var scene = NewScene();
            kitScene = "Basso"; kitIndex = 0;
            var r = new System.Random(1947);
            doorWallPoints.Clear();
            Ambient(new Color(0.62f, 0.52f, 0.40f), new Color(0.42f, 0.34f, 0.26f), new Color(0.24f, 0.19f, 0.14f));
            Sun(new Color(1f, 0.88f, 0.68f), 0.6f, new Vector3(55f, -25f, 0f));

            var env = new GameObject("Ambiente").transform;
            var props = new GameObject("Arredo").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // stanza 6 x 5 m, x in [-3,3], z in [-2.5,2.5], pareti alte 3 m, spesse 30 cm verso l'esterno
            Ground("Pavimento", env, new Vector3(-3.3f, -0.3f, -2.8f), new Vector3(3.3f, 0f, 2.8f), "M_PavimentoCotto", 1.2f);
            const float H = 3.0f, TH = 0.3f;
            const string PL = "M_IntonacoMattoni", DADO = "M_IntonacoRossoSbiadito", TRIM = "M_ArenariaCrepe";
            // nord: finestrella chiusa
            InteriorWall(env, "MuroNord", new WallFrame(new Vector3(-3f, 0f, 2.5f), Vector3.right, Vector3.back), 6f, H, TH,
                new List<WallOpening> { new WallOpening(4.6f, 0.9f, 1.4f, 2.3f) }, PL, DADO, 1.0f, TRIM);
            // sud: porta d'uscita
            InteriorWall(env, "MuroSud", new WallFrame(new Vector3(3f, 0f, -2.5f), Vector3.left, Vector3.forward), 6f, H, TH,
                new List<WallOpening> { new WallOpening(3f, 1.3f, 0f, 2.2f, true) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroOvest", new WallFrame(new Vector3(-3f, 0f, -2.5f), Vector3.forward, Vector3.right), 5f, H, TH,
                new List<WallOpening>(), PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroEst", new WallFrame(new Vector3(3f, 0f, 2.5f), Vector3.back, Vector3.left), 5f, H, TH,
                new List<WallOpening>(), PL, DADO, 1.0f, TRIM);
            // angoli esterni (le pareti si sovrappongono solo di 0.3 m: chiusura degli spigoli)
            int cornerIdx = 0;
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    var corner = new KitBuilder(PL);
                    corner.Box(new Vector3(sx * 3f, 0f, sz * 2.5f), new Vector3(sx * 3.3f, H, sz * 2.8f), 0, TileWall, 16);
                    MeshObject(env, "MuroAngolo_" + (++cornerIdx), corner.Build(KitName("Angolo")), new[] { PL }, true);
                }

            // ---- arredo
            // letto (testata al muro nord, lato ovest)
            {
                var kb = new KitBuilder("M_LegnoVernicePelata", "M_TessutoLino", "Tessuto1");
                Vector3 c = new Vector3(-1.95f, 0f, 1.2f);
                kb.Box(c + new Vector3(-0.9f, 0.18f, -1.25f), c + new Vector3(0.9f, 0.3f, 1.25f), 0, TileWood);
                foreach (var sx in new[] { -1f, 1f })
                    foreach (var sz in new[] { -1f, 1f })
                        kb.Box(c + new Vector3(sx * 0.85f - 0.05f, 0f, sz * 1.2f - 0.05f), c + new Vector3(sx * 0.85f + 0.05f, 0.18f, sz * 1.2f + 0.05f), 0, TileWood);
                kb.Box(c + new Vector3(-0.9f, 0.18f, 1.2f), c + new Vector3(0.9f, 1.05f, 1.3f), 0, TileWood);
                kb.Box(c + new Vector3(-0.9f, 0.18f, -1.3f), c + new Vector3(0.9f, 0.55f, -1.2f), 0, TileWood);
                kb.Box(c + new Vector3(-0.85f, 0.3f, -1.2f), c + new Vector3(0.85f, 0.5f, 1.2f), 1, 1.0f);
                kb.Box(c + new Vector3(-0.87f, 0.5f, -1.15f), c + new Vector3(0.87f, 0.57f, 0.4f), 2, 1.0f);
                kb.Box(c + new Vector3(-0.4f, 0.5f, 0.8f), c + new Vector3(0.4f, 0.62f, 1.18f), 1, 0.6f);
                MeshObject(props, "Letto", kb.Build(KitName("Letto")), kb.matNames, true);
            }
            // tavolo, sedie, lume a petrolio
            Model("TavoloLegno01", props, new Vector3(1.6f, 0f, 0.9f), 0f, 1.2f, true, "Tavolo");
            Model("SediaDipinta", props, new Vector3(1.15f, 0f, 0.15f), 15f, 1.0f, true, "Sedia");
            Model("SediaDipinta", props, new Vector3(2.05f, 0f, 1.65f), 195f, 1.0f, true, "Sedia");
            var lampTable = Model("LampadaPetrolio_A", props, new Vector3(1.55f, 0.66f, 0.95f), 0f, 1.2f, false, "LampadaTavolo");
            Prim(PrimitiveType.Sphere, "FiammaLampada", props, new Vector3(1.55f, 0.84f, 0.95f), new Vector3(0.06f, 0.1f, 0.06f), "Glow", null, false);
            Model("CestaVimini", props, new Vector3(2.2f, 0.66f, 0.8f), 30f, 1.2f, false, "Cesto");

            // braciere
            var br = Empty("Braciere", props, new Vector3(-0.1f, 0f, -0.5f)).transform;
            Cyl("CiotolaBraciere", br, new Vector3(0f, 0.4f, 0f), 0.55f, 0.14f, "M_FerroRuggine02");
            Cyl("PiedeBraciere", br, new Vector3(0f, 0.2f, 0f), 0.1f, 0.4f, "M_FerroRuggine02", null, false);
            Cyl("BaseBraciere", br, new Vector3(0f, 0.03f, 0f), 0.4f, 0.06f, "M_FerroRuggine02", null, false);
            Cyl("Brace", br, new Vector3(0f, 0.46f, 0f), 0.45f, 0.03f, "Glow", null, false);
            Model("SecchioMetallo", props, new Vector3(0.45f, 0f, -0.75f), 10f, 1.1f, true, "SecchioCarbone");

            // cassapanca (cassa grande) con vaso
            Model("CassaLegno", props, new Vector3(2.65f, 0f, -0.6f), 90f, 1.45f, true, "Cassapanca");
            Model("VasoTerracotta", props, new Vector3(2.65f, 0.52f, -0.35f), 0f, 1.0f, false, "Vaso");

            // angolo di deposito: botte e casse
            Model("BotteVino", props, new Vector3(-2.55f, 0f, -1.95f), 0f, 1.2f, true, "Botte");
            Model("CassaLegno", props, new Vector3(-2.55f, 0f, -0.85f), 90f, 1.2f, true, "Cassa");
            Model("CassaLegno", props, new Vector3(-2.55f, 0.43f, -0.85f), 78f, 1.0f, true, "Cassa");
            Model("ScalaLegno", props, new Vector3(2.9f, 0f, -1.75f), 90f, 1.5f, false, "Scala");

            // altarino con lumino (muro nord)
            {
                var fr = new WallFrame(new Vector3(0.2f, 0f, 2.5f), Vector3.right, Vector3.back);
                var kb = new KitBuilder("M_ArenariaCrepe", "M_LegnoDipintoBlu", "M_LegnoVernicePelata");
                fr.Box(kb, -0.5f, -0.38f, -0.08f, 0f, 1.1f, 2.2f, 0, TileStone, 0);
                fr.Box(kb, 0.38f, 0.5f, -0.08f, 0f, 1.1f, 2.2f, 0, TileStone, 0);
                fr.Box(kb, -0.56f, 0.56f, -0.1f, 0f, 2.2f, 2.32f, 0, TileStone, 0);
                fr.Box(kb, -0.38f, 0.38f, -0.03f, -0.01f, 1.1f, 2.2f, 1, 1f, 0);
                fr.Box(kb, -0.55f, 0.55f, -0.3f, 0f, 1.0f, 1.07f, 2, TileWood, 0);
                fr.Box(kb, -0.4f, -0.34f, -0.26f, -0.04f, 0.4f, 1.0f, 2, TileWood, 0);
                fr.Box(kb, 0.34f, 0.4f, -0.26f, -0.04f, 0.4f, 1.0f, 2, TileWood, 0);
                MeshObject(props, "Altarino", kb.Build(KitName("Altarino")), kb.matNames, false);
                Prim(PrimitiveType.Capsule, "StatuettaAltarino", props, fr.W(-0.05f, -0.1f, 1.38f), new Vector3(0.1f, 0.2f, 0.1f), "Cera", null, false);
                Model("Candela", props, fr.W(0.22f, -0.15f, 1.07f), 0f, 1.0f, false, "CeroAltarino");
                Prim(PrimitiveType.Sphere, "LuminoAltarino", props, fr.W(0.22f, -0.15f, 1.2f), new Vector3(0.05f, 0.08f, 0.05f), "Glow", null, false);
            }

            // tenda divisoria con bastone
            Cyl("BastoneTenda", props, new Vector3(-0.95f, 2.45f, 1.65f), 0.04f, 1.6f, "M_FerroRuggine02", new Vector3(90f, 0f, 0f), false);
            Box("Tenda", props, new Vector3(-0.95f, 1.25f, 1.65f), new Vector3(0.04f, 2.3f, 1.5f), "Tessuto2", null, false);

            // panni appesi
            Clothesline(props, r, 0.3f, 2.5f, 2.4f, -0.8f, 2.9f, 3);

            // porta d'uscita nel varco del muro sud (z = -2.5)
            var doorFr = new WallFrame(new Vector3(0f, 0f, -2.5f), Vector3.left, Vector3.forward);
            var door = DoorLeaf(props, "PortaUscita", doorFr, 0f, 0.08f, 0.2f, 1.15f, 2.15f, 0.25f); // come prima: fronte del collider a z = -2.25
            var d = door.AddComponent<Door>();
            d.targetScene = "10_Hub_Vicolo";
            d.targetSpawn = "da_basso_antonietta";

            // le scarpe (due blocchi a forma di scarpa) vicino alla porta
            var shoes = Empty("Scarpe", props, new Vector3(1.3f, 0f, -2.0f));
            ShoeObject(shoes.transform, new Vector3(-0.1f, 0f, 0f), 8f, 1f);
            ShoeObject(shoes.transform, new Vector3(0.12f, 0f, 0.03f), -14f, 1f);
            var bc = shoes.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.08f, 0f);
            bc.size = new Vector3(0.5f, 0.18f, 0.42f);
            var shoeGlow = shoes.AddComponent<Glow>();
            shoeGlow.intensity = 0.35f; // alone discreto: la forma della scarpa deve restare leggibile
            var pick = shoes.AddComponent<ItemPickup>();
            pick.oggettoId = "scarpe";

            // luce calda sopra il tavolo (lume a petrolio): una sola Point Light
            var lgo = new GameObject("Luce Calda");
            lgo.transform.position = new Vector3(1.2f, 1.7f, 0.5f);
            var pl = lgo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.70f, 0.40f);
            pl.intensity = 4f;
            pl.range = 9f;
            pl.shadows = LightShadows.None;

            Spawn("ingresso", new Vector3(0f, 0f, -1.7f), 0f, spawns);
            Spawn("oggetto_scarpe", new Vector3(0.7f, 0f, -1.4f), 90f, spawns);

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.10f, 0.07f, 0.05f), true);
            BuildPlayer(new Vector3(0f, 0.05f, -1.7f), 0f, cam, CamProfilo.InternoPiccolo);
            SaveScene(scene, "20_Int_BassoAntonietta");
        }

        // ------------------------------------------------------------------ 30_FB_Scarpe

        static void BuildFlashback()
        {
            var scene = NewScene();
            kitScene = "Ricordo"; kitIndex = 0;
            doorWallPoints.Clear();
            Color sepiaFog = new Color(0.62f, 0.50f, 0.34f);
            Ambient(new Color(0.72f, 0.60f, 0.42f), new Color(0.52f, 0.42f, 0.30f), new Color(0.28f, 0.22f, 0.15f));
            Sun(new Color(1f, 0.86f, 0.62f), 0.9f, new Vector3(55f, -40f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sepiaFog;
            RenderSettings.fogStartDistance = 4f;
            RenderSettings.fogEndDistance = 22f;

            EnsureLightMaterials();
            var env = new GameObject("Ambiente").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // stanza 10 x 10 m, pareti alte 4 m, spesse 30 cm verso l'esterno
            Ground("Pavimento", env, new Vector3(-5.3f, -0.3f, -5.3f), new Vector3(5.3f, 0f, 5.3f), "M_AssiConsunte", 1.4f);
            const float H = 4.0f, TH = 0.3f;
            const string PL = "M_IntonacoMattoni", DADO = "M_AssiStagionate", TRIM = "M_ArenariaCrepe";
            InteriorWall(env, "MuroNord", new WallFrame(new Vector3(-5f, 0f, 5f), Vector3.right, Vector3.back), 10f, H, TH,
                new List<WallOpening>(), PL, DADO, 1.2f, TRIM);
            InteriorWall(env, "MuroSud", new WallFrame(new Vector3(5f, 0f, -5f), Vector3.left, Vector3.forward), 10f, H, TH,
                new List<WallOpening>(), PL, DADO, 1.2f, TRIM);
            // ovest: grande finestra (il vetro e' un pannello luminoso con sporco e sfumatura, vedi BuildFlashbackLuci)
            InteriorWall(env, "MuroOvest", new WallFrame(new Vector3(-5f, 0f, -5f), Vector3.forward, Vector3.right), 10f, H, TH,
                new List<WallOpening> { new WallOpening(5f, 2.4f, 1.0f, 2.8f) }, PL, DADO, 1.2f, TRIM, false);
            Box("VetroFinestra", env, new Vector3(-5.22f, 1.9f, 0f), new Vector3(0.03f, 1.8f, 2.4f), "M_LuceFinestra", null, false);
            // est: porta dell'uscita
            InteriorWall(env, "MuroEst", new WallFrame(new Vector3(5f, 0f, 5f), Vector3.back, Vector3.left), 10f, H, TH,
                new List<WallOpening> { new WallOpening(5f, 1.4f, 0f, 2.4f, true) }, PL, DADO, 1.2f, TRIM);
            // pilastri d'angolo (Muro*: la camera li rende trasparenti) e chiusure esterne
            int pilIdx = 0;
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                {
                    var pil = new KitBuilder("M_ArenariaCrepe", PL);
                    pil.Box(new Vector3(sx * 4.5f, 0f, sz * 4.5f), new Vector3(sx * 5f, H, sz * 5f), 0, TileStone, 16);
                    pil.Box(new Vector3(sx * 5f, 0f, sz * 5f), new Vector3(sx * 5.3f, H, sz * 5.3f), 1, TileWall, 16);
                    MeshObject(env, "MuroPilastro_" + (++pilIdx), pil.Build(KitName("Pilastro")), new[] { "M_ArenariaCrepe", PL }, true);
                }

            FlashbackProps(env);
            SetStatic(env.gameObject);

            // uscita: bagliore nel vano della porta est
            var exitRoot = new GameObject("Uscita").transform;
            var exit = Box("EscapeExit", exitRoot, new Vector3(5.1f, 1.2f, 0f), new Vector3(0.12f, 2.4f, 1.4f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_scarpe";

            Spawn("inizio", new Vector3(0f, 0f, 0f), 0f, spawns);

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, sepiaFog, true);
            BuildPlayer(new Vector3(0f, 0.05f, 0f), 0f, cam, CamProfilo.InternoLargo);
            SaveScene(scene, "30_FB_Scarpe");
        }
    }
}
