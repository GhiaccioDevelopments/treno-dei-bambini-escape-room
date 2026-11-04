using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Fase 2.2: scena di prova degli asset realistici (Assets/_Game/Scenes/99_ProvaAsset.unity, fuori dalle Build Settings).
    /// Applica lo stesso look della hub (Volume VP_Epoca, nebbia, sole caldo radente, ambiente trilight, riflessi),
    /// dispone campioni di texture e modelli con etichette e crea le camere "Cam_*" usate per gli screenshot.
    /// Menu: Treno/Asset/Scena di prova asset  e  Treno/Asset/Screenshot scena di prova
    /// </summary>
    public static class AssetPreviewBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/99_ProvaAsset.unity";
        const string AuxFolder = "Assets/_Game/Scenes/99_ProvaAsset_Aux";
        const string MatRoot = "Assets/Art/Materials";
        const string ProfileEpoca = "Assets/Settings/Volumes/VP_Epoca.asset";
        const string ScreenshotDir = "Docs/Screenshots/2.2";

        // ---- scelte usate nella composizione finale (aggiornate dopo la valutazione)
        static string ChosenPlaster = "IntonacoRustico05";
        static string ChosenStone = "ArenariaCrepe";
        static string ChosenRoad = "BasoliLavica";
        static string ChosenPaintedWood = "LegnoVernicePelata";
        static string ChosenPlanks = "AssiStagionate";
                static string ChosenGround = "TerraPolverosa";

        // look della hub (LookBuilder.ApplySceneLook, caso 10_Hub_Vicolo)
        static readonly Color SunColor = new Color(1.00f, 0.82f, 0.58f);
        static readonly Color AmbSky = new Color(0.58f, 0.54f, 0.50f), AmbEq = new Color(0.40f, 0.36f, 0.31f), AmbGround = new Color(0.22f, 0.19f, 0.15f);
        static readonly Color Bg = new Color(0.66f, 0.57f, 0.45f);

        static readonly string[] HdriNames = { "CieloCoperto", "CieloVelatoCaldo" };

        static Material _labelBg, _labelFont;

        // salta gli asset scartati (cancellati dal progetto): la scena resta costruibile con i soli asset tenuti
        static string[] HaveMats(string[] a)
        {
            var l = new List<string>();
            foreach (var n in a) if (AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/M_" + n + ".mat") != null) l.Add(n);
            return l.ToArray();
        }
        static string[] HaveModels(string[] a)
        {
            var l = new List<string>();
            foreach (var n in a) if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/" + n + "/" + n + ".fbx") != null) l.Add(n);
            return l.ToArray();
        }

        // ================================================================== menu

        [MenuItem("Treno/Asset/Scena di prova asset")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(AuxFolder);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyLook();
            _labelBg = null; _labelFont = null;

            var root = new GameObject("ProvaAsset").transform;
            int st = 0;

            // 1. intonaci
            PanelStation(root, st++, "01_Intonaci", new[]
            { "IntonacoMattoni", "IntonacoRustico05", "IntonacoRossoSbiadito", "MuroRotto" }, 2);
            // 2. tufo / pietra
            PanelStation(root, st++, "02_TufoPietra", new[] { "ArenariaCrepe", "ArenariaBlocchi", "MuroRotto" }, 3);
            // 3. strada (secco in primo piano, bagnato dietro)
            RoadStation(root, st++, "03_StradaBasoli", new[] { "BasoliLavica" });
            // 4. legno
            PanelStation(root, st++, "04_LegnoAssi", new[] { "LegnoVernicePelata", "LegnoDipintoBlu", "AssiStagionate", "AssiConsunte" }, 3);
            // 5. pavimenti e terra
            FloorStation(root, st++, "05_Pavimenti", new[] { "PavimentoCotto", "PavimentoPiastreGrandi", "PavimentoBattuto", "TerraPolverosa" });
            // 6. tetto, ferro, tessuto
            PanelStation(root, st++, "06_TettoFerroTessuto", new[] { "CoppiArgilla", "FerroRuggine02", "FerroRuggineGrossa", "TessutoLino", "TessutoIuta" }, 3);
            // 7-9. modelli
            ModelStation(root, st++, "07_ModelliArredo", true, new[]
            { "SediaDipinta", "TavoloLegno01", "TavoloLegno02", "CassaLegno", "BotteVino", "ScalaLegno" });
            ModelStation(root, st++, "08_ModelliContenitori", false, new[]
            { "CestaVimini", "SecchioLegno", "VasoTerracotta", "SecchioMetallo" });
            ModelStation(root, st++, "09_ModelliLuci", false, new[]
            { "LampadaPetrolio_A", "LampadaPetrolio_B", "LanternaPH", "LanternaAppesa", "Candela" });
            ModelStation(root, st++, "10_ModelliLampioni", true, new[]
            { "LampioneMuro_Kigha", "LampioneStrada_Pixie" });
            ModelStation(root, st++, "11_ModelliCarretti", true, new[]
            { "Carretto_Filip", "Macerie_Mattoni" });
            // 10. HDRI
            HdriStation(root, st++);
            // 11. composizione d'insieme
            InsiemeStation(root, st++);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[AssetPreviewBuilder] Scena di prova creata: " + ScenePath);
        }

        [MenuItem("Treno/Asset/Screenshot scena di prova")]
        public static void RenderMenu() { RenderScreenshots(null); }

        // ================================================================== look

        static void ApplyLook()
        {
            var vgo = new GameObject("Look");
            var vol = vgo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfileEpoca);

            var sgo = new GameObject("Sole");
            var sun = sgo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sgo.transform.rotation = Quaternion.Euler(34f, -14f, 0f);
            sun.color = SunColor;
            sun.intensity = 3.0f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            RenderSettings.sun = sun;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbSky;
            RenderSettings.ambientEquatorColor = AmbEq;
            RenderSettings.ambientGroundColor = AmbGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = Bg;

            // riflessi: cubemap sfumata con i colori dell'ambiente (al posto della reflection probe baked del gioco)
            var cube = MakeReflectionCubemap();
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 0.5f;
        }

        static Cubemap MakeReflectionCubemap()
        {
            string path = AuxFolder + "/CM_Riflessi.asset";
            var old = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (old != null) return old;
            const int n = 32;
            var cm = new Cubemap(n, TextureFormat.RGBA32, false);
            Color top = Color.Lerp(AmbSky, Bg, 0.5f) * 1.25f, mid = Bg, bot = AmbGround * 1.2f;
            top.a = mid.a = bot.a = 1f;
            foreach (CubemapFace f in Enum.GetValues(typeof(CubemapFace)))
            {
                if (f == CubemapFace.Unknown) continue;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float v = y / (n - 1f);   // 0 = basso nel cubemap
                        Color c;
                        if (f == CubemapFace.PositiveY) c = top;
                        else if (f == CubemapFace.NegativeY) c = bot;
                        else c = v < 0.5f ? Color.Lerp(bot, mid, v * 2f) : Color.Lerp(mid, top, (v - 0.5f) * 2f);
                        px[y * n + x] = c;
                    }
                cm.SetPixels(px, f);
            }
            cm.Apply();
            AssetDatabase.CreateAsset(cm, path);
            return cm;
        }

        // ================================================================== mesh / materiali di servizio

        static Mesh QuadMesh(float w, float h, float tile)
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            string path = AuxFolder + "/Quad_" + w.ToString("0.#", ic) + "x" + h.ToString("0.#", ic) + "_t" + tile.ToString("0.#", ic) + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m != null) return m;
            m = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            m.vertices = new[] { new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(-w / 2, h / 2, 0), new Vector3(w / 2, h / 2, 0) };
            float u = w / tile, v = h / tile;
            m.uv = new[] { new Vector2(0, 0), new Vector2(u, 0), new Vector2(0, v), new Vector2(u, v) };
            m.uv2 = m.uv;
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.RecalculateBounds();
            m.RecalculateTangents();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Mat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/M_" + name + ".mat");
            if (m == null) Debug.LogWarning("[AssetPreviewBuilder] Materiale mancante: M_" + name);
            return m;
        }

        static Material AuxMat(string name, Color c, float metal, float smooth, bool unlit = false)
        {
            string path = AuxFolder + "/M_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            if (!unlit) { m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static GameObject Quad(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Etichetta 3D con fondino scuro, rivolta verso -Z (verso la camera).</summary>
        static void Label(Transform parent, string text, Vector3 pos, float size)
        {
            if (_labelFont == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _labelFont = new Material(Shader.Find("GUI/Text Shader")) { name = "M_ProvaFont" };
                _labelFont.mainTexture = font.material.mainTexture;
                AssetDatabase.CreateAsset(_labelFont, AuxFolder + "/M_ProvaFont.mat");
            }
            if (_labelBg == null) _labelBg = AuxMat("EtichettaSfondo", new Color(0.05f, 0.04f, 0.03f, 1f), 0, 0, true);

            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            tm.characterSize = size * 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.97f, 0.93f, 0.82f);
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;

            float wEst = text.Length * size * 0.62f + size * 0.6f;
            var bg = Quad(go.transform, "Sfondo", QuadMesh(1f, 1f, 1f), _labelBg, new Vector3(0, 0, 0.01f), Vector3.zero);
            bg.transform.localScale = new Vector3(wEst, size * 1.35f, 1f);
        }

        // ================================================================== stazioni

        const float StationSpacing = 40f;

        static Transform Station(Transform root, int index, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = new Vector3(index * StationSpacing, 0, 0);
            return t;
        }

        static void Ground(Transform st, float w, float d, string matName, float zCenter)
        {
            Quad(st, "Terreno", QuadMesh(w, d, 2.0f), Mat(matName), new Vector3(0, 0, zCenter), new Vector3(90, 0, 0));
        }

        static Camera MakeCamera(Transform st, string name, Vector3 pos, Vector3 lookAt, float fov = 32f)
        {
            var go = new GameObject("Cam_" + name);
            go.transform.SetParent(st, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(st.TransformPoint(lookAt));
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            return cam;
        }

        static void PanelStation(Transform root, int index, string name, string[] mats, int cols)
        {
            mats = HaveMats(mats);
            var st = Station(root, index, name);
            const float size = 2.4f, gap = 0.25f;
            Ground(st, 30, 16, ChosenGround, -2f);
            int rows = (mats.Length + cols - 1) / cols;
            var mesh = QuadMesh(size, size, 1.6f);
            for (int i = 0; i < mats.Length; i++)
            {
                int r = i / cols, c = i % cols;
                int inRow = Math.Min(cols, mats.Length - r * cols);
                float x = (c - (inRow - 1) / 2f) * (size + gap);
                float y = size / 2 + (rows - 1 - r) * (size + 0.45f);
                Quad(st, "Pannello_" + mats[i], mesh, Mat(mats[i]), new Vector3(x, y, 0), Vector3.zero);
                Label(st, mats[i], new Vector3(x, y - size / 2 + 0.2f, -0.04f), 0.17f);
            }
            float W = cols * (size + gap), H = rows * (size + 0.45f);
            float dist = Mathf.Max(W * 0.5f / 0.56f, H * 0.5f / 0.29f) + 1.0f;
            MakeCamera(st, name, new Vector3(0, H * 0.5f, -dist), new Vector3(0, H * 0.5f, 0), 32f);
        }

        static void FloorStation(Transform root, int index, string name, string[] mats)
        {
            mats = HaveMats(mats);
            var st = Station(root, index, name);
            const float size = 2.4f, gap = 0.2f;
            var mesh = QuadMesh(size, size, 1.6f);
            for (int i = 0; i < mats.Length; i++)
            {
                int r = i / 3, c = i % 3;
                float x = (c - 1f) * (size + gap);
                float z = r * (size + gap) + 0.4f;
                Quad(st, "Piano_" + mats[i], mesh, Mat(mats[i]), new Vector3(x, 0.001f, z + size / 2), new Vector3(90, 0, 0));
                Label(st, mats[i], new Vector3(x, 0.18f, z - 0.05f), 0.13f);
            }
            MakeCamera(st, name, new Vector3(0, 5.6f, -3.4f), new Vector3(0, 0, 2.6f), 46f);
        }

        static void RoadStation(Transform root, int index, string name, string[] mats)
        {
            mats = HaveMats(mats);
            var st = Station(root, index, name);
            const float size = 2.6f, gap = 0.2f;
            var mesh = QuadMesh(size, size, 1.5f);
            for (int i = 0; i < mats.Length; i++)
            {
                float x = (i - (mats.Length - 1) / 2f) * (size + gap);
                Quad(st, "Secco_" + mats[i], mesh, Mat(mats[i]), new Vector3(x, 0.001f, size / 2), new Vector3(90, 0, 0));
                Quad(st, "Bagnato_" + mats[i], mesh, Mat(mats[i] + "_Bagnato"), new Vector3(x, 0.001f, size * 1.5f + 0.1f), new Vector3(90, 0, 0));
                Label(st, mats[i] + " secco", new Vector3(x, 0.18f, -0.15f), 0.12f);
                Label(st, mats[i] + " bagnato", new Vector3(x, 0.18f, size + 0.0f), 0.12f);
            }
            float W = mats.Length * (size + gap);
            MakeCamera(st, name, new Vector3(0, 3.0f, -3.3f), new Vector3(0, 0, 2.9f), 40f);
        }

        // ---- modelli

        static void ModelStation(Transform root, int index, string name, bool bigScaleRef, string[] models)
        {
            models = HaveModels(models);
            var st = Station(root, index, name);
            Ground(st, 40, 16, ChosenGround, -2f);
            float gap = bigScaleRef ? 0.45f : 0.12f;
            float cursor = 0, maxH = 0;
            var placed = new List<(GameObject go, float w)>();

            // riferimento di scala: capsula 1.8 m (oppure cubo da 10 cm per gli oggetti piccoli)
            var refGo = GameObject.CreatePrimitive(bigScaleRef ? PrimitiveType.Capsule : PrimitiveType.Cube);
            refGo.name = bigScaleRef ? "Riferimento_1.8m" : "Riferimento_10cm";
            refGo.transform.SetParent(st, false);
            refGo.GetComponent<Renderer>().sharedMaterial = AuxMat("Riferimento", new Color(0.55f, 0.55f, 0.55f), 0, 0.2f);
            UnityEngine.Object.DestroyImmediate(refGo.GetComponent<Collider>());
            float rw = bigScaleRef ? 0.5f : 0.1f;
            refGo.transform.localScale = bigScaleRef ? new Vector3(0.5f, 0.9f, 0.5f) : new Vector3(0.1f, 0.1f, 0.1f);
            refGo.transform.localPosition = new Vector3(0, bigScaleRef ? 0.9f : 0.05f, 0);
            placed.Add((refGo, rw));
            Label(st, bigScaleRef ? "Riferimento 1.8 m" : "Riferimento 10 cm", new Vector3(0, bigScaleRef ? 0.08f : 0.03f, -0.45f), bigScaleRef ? 0.12f : 0.035f);
            maxH = bigScaleRef ? 1.8f : 0.1f;
            cursor = rw / 2 + gap;

            foreach (var m in models)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/" + m + "/" + m + ".fbx");
                if (prefab == null) { Debug.LogWarning("[AssetPreviewBuilder] Modello mancante: " + m); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, st);
                go.name = m;
                go.transform.localPosition = Vector3.zero;
                var b = GetBounds(go);
                float w = Mathf.Max(b.size.x, b.size.z);
                go.transform.localPosition = new Vector3(cursor + w / 2 - b.center.x, 0, -b.center.z);
                float lsize = Mathf.Clamp(w * 0.18f, bigScaleRef ? 0.08f : 0.03f, 0.2f);
                Label(st, m, new Vector3(cursor + w / 2, bigScaleRef ? 0.08f : 0.03f, -Mathf.Max(b.size.z / 2, 0.15f) - (bigScaleRef ? 0.3f : 0.09f)), bigScaleRef ? 0.1f : 0.035f);
                maxH = Mathf.Max(maxH, b.size.y);
                cursor += w + gap;
                placed.Add((go, w));
            }
            float total = cursor;
            // centra la fila sull'origine della stazione
            foreach (var t in st.GetComponentsInChildren<Transform>())
                if (t.parent == st && t.name != "Terreno" && !t.name.StartsWith("Cam_")) t.localPosition -= new Vector3(total / 2, 0, 0);

            float distW = (total * 0.5f + 0.2f) / 0.58f;
            float distH = (maxH * 0.55f + 0.2f) / 0.28f;
            float dist = Mathf.Max(distW, distH) + 0.3f;
            MakeCamera(st, name, new Vector3(0, maxH * 0.5f + 0.15f, -dist), new Vector3(0, maxH * 0.38f, 0), 32f);
        }

        static Bounds GetBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return new Bounds(b.center - go.transform.parent.position, b.size);
        }

        // ---- HDRI (sfera cromata, sfera ruvida, intonaco)

        static void HdriStation(Transform root, int index)
        {
            var st = Station(root, index, "12_HDRI");
            Ground(st, 30, 16, ChosenGround, -2f);
            var chrome = AuxMat("ProvaCromo", Color.white, 1f, 0.92f);
            var white = AuxMat("ProvaGesso", new Color(0.8f, 0.78f, 0.72f), 0f, 0.15f);
            string[] kinds = { "Cromo", "Gesso", "Intonaco" };
            for (int i = 0; i < 3; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Sfera_" + kinds[i];
                s.transform.SetParent(st, false);
                s.transform.localPosition = new Vector3((i - 1) * 1.6f, 0.7f, 0);
                s.transform.localScale = Vector3.one * 1.3f;
                UnityEngine.Object.DestroyImmediate(s.GetComponent<Collider>());
                s.GetComponent<Renderer>().sharedMaterial = i == 0 ? chrome : i == 1 ? white : Mat("IntonacoMattoni");
            }
            MakeCamera(st, "12_HDRI", new Vector3(0, 1.4f, -5.2f), new Vector3(0, 0.9f, 0), 38f);
        }

        // ---- composizione d'insieme: vicolo con gli asset scelti

        static void InsiemeStation(Transform root, int index)
        {
            var st = Station(root, index, "13_Insieme");
            const float len = 14f, hgt = 6f, half = 2.1f;
            Quad(st, "Strada", QuadMesh(2 * half, len, 1.5f), Mat(ChosenRoad + "_Bagnato"), new Vector3(0, 0, len / 2 - 2), new Vector3(90, 0, 0));
            // pareti: sinistra intonaco, destra pietra, rivolte verso il centro del vicolo
            Quad(st, "ParetePlaster", QuadMesh(len, hgt, 1.6f), Mat(ChosenPlaster), new Vector3(-half, hgt / 2, len / 2 - 2), new Vector3(0, -90, 0));
            Quad(st, "ParetePietra", QuadMesh(len, hgt, 1.6f), Mat(ChosenStone), new Vector3(half, hgt / 2, len / 2 - 2), new Vector3(0, 90, 0));
            Quad(st, "Fondale", QuadMesh(2 * half, hgt, 1.6f), Mat(ChosenPlaster), new Vector3(0, hgt / 2, len - 2), new Vector3(0, 0, 0));
            // portone in legno dipinto sulla parete di sinistra
            Quad(st, "Portone", QuadMesh(1.3f, 2.3f, 1.6f), Mat(ChosenPaintedWood), new Vector3(-half + 0.02f, 1.15f, 5.5f), new Vector3(0, -90, 0));
            Quad(st, "Persiana", QuadMesh(1.0f, 1.4f, 1.6f), Mat(ChosenPlanks), new Vector3(half - 0.02f, 2.4f, 7.5f), new Vector3(0, 90, 0));
            Place(st, "Carretto_Filip", new Vector3(1.0f, 0, 4.6f), 90f);
            Place(st, "CassaLegno", new Vector3(-1.5f, 0, 2.5f), -20f);
            Place(st, "SecchioLegno", new Vector3(-1.6f, 0, 6.2f), 0f);
            Place(st, "SediaDipinta", new Vector3(-1.4f, 0, 4.2f), 70f);
            Place(st, "Macerie_Mattoni", new Vector3(1.3f, 0, 8.5f), 30f);
            Place(st, "LampioneMuro_Kigha", new Vector3(-half + 0.1f, 3.0f, 3.0f), -90f);
            MakeCamera(st, "13_Insieme", new Vector3(0.2f, 1.6f, -1.6f), new Vector3(-0.1f, 1.7f, 8f), 55f);
        }

        static void Place(Transform st, string model, Vector3 pos, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/" + model + "/" + model + ".fbx");
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, st);
            go.name = model;
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, yaw, 0);
        }

        // ================================================================== screenshot

        /// <summary>Renderizza 1280x720 di ogni camera "Cam_*" della scena aperta in Docs/Screenshots/2.2.</summary>
        public static void RenderScreenshots(string onlyPrefix = null)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScreenshotDir));
            Directory.CreateDirectory(dir);
            var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var cam in cams)
            {
                if (!cam.name.StartsWith("Cam_")) continue;
                string n = cam.name.Substring(4);
                if (!string.IsNullOrEmpty(onlyPrefix) && !n.StartsWith(onlyPrefix)) continue;
                if (n == "12_HDRI")
                {
                    foreach (var h in HdriNames) RenderHdri(cam, dir, h);
                    continue;
                }
                Shoot(cam, Path.Combine(dir, n + ".png"));
            }
            Debug.Log("[AssetPreviewBuilder] Screenshot salvati in " + dir);
        }

        static void RenderHdri(Camera cam, string dir, string hdri)
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "/M_Sky_" + hdri + ".mat");
            if (sky == null) { Debug.LogWarning("Sky mancante: " + hdri); return; }
            var oSky = RenderSettings.skybox; var oRefl = RenderSettings.defaultReflectionMode; var oCust = RenderSettings.customReflectionTexture;
            var oAmb = RenderSettings.ambientMode; bool oFog = RenderSettings.fog; var oFlags = cam.clearFlags;
            RenderSettings.skybox = sky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();
            cam.clearFlags = CameraClearFlags.Skybox;
            Shoot(cam, Path.Combine(dir, "12_HDRI_" + hdri + ".png"));
            cam.clearFlags = oFlags;
            RenderSettings.skybox = oSky; RenderSettings.defaultReflectionMode = oRefl; RenderSettings.customReflectionTexture = oCust;
            RenderSettings.ambientMode = oAmb; RenderSettings.fog = oFog; RenderSettings.reflectionIntensity = 0.5f;
        }

        static void Shoot(Camera cam, string file)
        {
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            rt.antiAliasing = 4;
            var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
