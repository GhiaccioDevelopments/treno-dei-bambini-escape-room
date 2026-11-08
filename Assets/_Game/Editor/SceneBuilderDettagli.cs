using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Dettagli degli ambienti (Fase 2.3): strada con canaletta, panni stesi a mesh, scarpa logora,
    /// materiali luminosi procedurali del flashback e arredo d'epoca della stanza del ricordo.
    /// </summary>
    public static partial class SceneBuilder
    {
        static readonly Dictionary<string, Mesh> clothMeshes = new Dictionary<string, Mesh>();

        // ------------------------------------------------------------------ salvataggio mesh con GUID stabile

        static Mesh SaveKitMesh(Mesh fresh, string name)
        {
            string path = KitBuilder.Folder + "/" + name + ".asset";
            fresh.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            KitBuilder.Used.Add(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(fresh, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(fresh);
                return existing;
            }
            AssetDatabase.CreateAsset(fresh, path);
            return fresh;
        }

        // ------------------------------------------------------------------ strada

        // basoli di lava con canaletta centrale incassata di 5 cm (un'unica mesh, un'unica lightmap)
        static GameObject Street(string name, Transform parent, float x0, float x1, float z0, float z1, float channelHalf)
        {
            var kb = new KitBuilder("M_BasoliLavica", "M_BasoliLavica_Bagnato");
            kb.Box(new Vector3(x0, -0.5f, z0), new Vector3(-channelHalf, 0f, z1), 0, 1.6f, KitBuilder.NY);
            kb.Box(new Vector3(channelHalf, -0.5f, z0), new Vector3(x1, 0f, z1), 0, 1.6f, KitBuilder.NY);
            kb.Box(new Vector3(-channelHalf, -0.5f, z0), new Vector3(channelHalf, -0.05f, z1), 1, 1.2f, KitBuilder.NY | KitBuilder.NX | KitBuilder.PX);
            var mesh = kb.Build(KitName(name));
            return MeshObject(parent, name, mesh, kb.matNames, true);
        }

        // ------------------------------------------------------------------ panni stesi

        // griglia di tessuto appesa al bordo superiore: il bordo alto resta dritto, in basso si formano pieghe
        static void AddSheet(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 topLeft, Vector3 right, Vector3 down,
                             int nx, int ny, float amp, float folds, float phase)
        {
            Vector3 normal = Vector3.Cross(right, down).normalized;
            int b = v.Count;
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float fx = i / (float)nx, fy = j / (float)ny;
                    float wave = Mathf.Sin(fx * Mathf.PI * folds + phase) * amp * (0.15f + 0.85f * fy);
                    float belly = Mathf.Sin(fx * Mathf.PI) * amp * 0.4f * fy;
                    v.Add(topLeft + right * fx + down * fy + normal * (wave + belly));
                    uv.Add(new Vector2(fx * right.magnitude / 0.6f, fy * down.magnitude / 0.6f));
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    int a = b + j * (nx + 1) + i, c = a + 1, d = a + (nx + 1), e = d + 1;
                    t.Add(a); t.Add(c); t.Add(d); t.Add(c); t.Add(e); t.Add(d);
                }
        }

        static Mesh ClothMesh(string kind, int variant)
        {
            string key = kind + variant;
            Mesh m;
            if (clothMeshes.TryGetValue(key, out m) && m != null) return m;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float ph = variant * 1.7f;
            switch (kind)
            {
                case "Lenzuolo":
                    AddSheet(v, uv, t, new Vector3(-0.55f, 0, 0), new Vector3(1.1f, 0, 0), new Vector3(0, -1.4f, 0), 10, 10, 0.07f, 3.5f + variant, ph);
                    break;
                case "Camicia":
                    AddSheet(v, uv, t, new Vector3(-0.22f, 0, 0), new Vector3(0.44f, 0, 0), new Vector3(0, -0.72f, 0), 5, 7, 0.035f, 2.5f, ph);
                    AddSheet(v, uv, t, new Vector3(-0.22f, 0, 0.005f), new Vector3(-0.3f, -0.1f, 0), new Vector3(0.02f, -0.22f, 0), 3, 3, 0.02f, 1.5f, ph);
                    AddSheet(v, uv, t, new Vector3(0.22f, 0, 0.005f), new Vector3(0.3f, -0.1f, 0), new Vector3(-0.02f, -0.22f, 0), 3, 3, 0.02f, 1.5f, ph + 1f);
                    break;
                case "Pantaloni":
                    AddSheet(v, uv, t, new Vector3(-0.21f, 0, 0), new Vector3(0.2f, 0, 0), new Vector3(0, -0.9f, 0), 3, 8, 0.03f, 1.5f, ph);
                    AddSheet(v, uv, t, new Vector3(0.01f, 0, 0), new Vector3(0.2f, 0, 0), new Vector3(0, -0.9f, 0), 3, 8, 0.03f, 1.5f, ph + 2f);
                    AddSheet(v, uv, t, new Vector3(-0.21f, 0, 0.02f), new Vector3(0.42f, 0, 0), new Vector3(0, -0.14f, 0), 4, 2, 0.01f, 1f, ph); // cintura: 2 cm davanti ai due panni (R017: niente facce complanari)
                    break;
                default: // straccio
                    AddSheet(v, uv, t, new Vector3(-0.2f, 0, 0), new Vector3(0.4f, 0, 0), new Vector3(0.03f, -0.5f, 0), 5, 5, 0.05f, 2f + variant, ph);
                    break;
            }
            // doppia faccia: copia con verso invertito
            int n = v.Count, tn = t.Count;
            for (int i = 0; i < n; i++) { v.Add(v[i]); uv.Add(uv[i]); }
            for (int i = 0; i < tn; i += 3) { t.Add(t[i] + n); t.Add(t[i + 2] + n); t.Add(t[i + 1] + n); }
            var mesh = new Mesh();
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            Unwrapping.GenerateSecondaryUVSet(mesh); // i panni sono statici: ricevono la lightmap come il resto della scena
            m = SaveKitMesh(mesh, "Panno_" + kind + "_" + variant);
            clothMeshes[key] = m;
            return m;
        }

        static GameObject ClothObject(Transform parent, string kind, int variant, string mat, Vector3 topPos, float yaw, Vector3 scale)
        {
            // R007 t2: panni veri (modelli Panno_* con LOD, alpha clip e bordi sfilacciati). Capo e variante di colore (3 materiali) dipendono dalla
            // posizione, quindi sono diversi da un'istanza all'altra e riproducibili. L'argomento mat (colori delle primitive) non si usa piu'.
            int h = Mathf.Abs((int)(topPos.x * 13.7f) + (int)(topPos.z * 7.3f) * 31 + (int)(topPos.y * 5f) + variant * 17);
            string capo = kind == "Camicia" ? "Camicia" : kind == "Pantaloni" ? "Mutandoni" : kind == "Straccio" ? "Straccio" : ((h & 3) == 1 ? "LenzuoloRammendato" : (h & 3) == 3 ? "Tovaglia" : "Lenzuolo");
            if ((h >> 3) % 3 == 0) yaw += 180f; // specchia il verso del vento
            var go = ModelloIst("Panni/Panno_" + capo + ".fbx", parent, topPos, yaw, scale, false, "Panno_" + kind, 0.04f);
            string[] mm = { "M_Panni", "M_Panni_B", "M_Panni_C" };
            var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + mm[(h >> 1) % 3] + ".mat");
            if (m != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
            return go;
        }

        static readonly string[] ClothColors = { "PannoBianco", "PannoGrigio", "PannoBeige", "PannoBlu", "PannoTerra", "PannoOcra", "PannoBianco" };

        // filo teso tra xL e xR (a ridosso delle pareti: gli estremi stanno 8 cm prima del piano di facciata, oltre le fasce in pietra);
        // i panni sono larghi al piu' l'80% dello spazio a loro assegnato e poco ruotati, cosi' non si toccano
        static void Clothesline(Transform parent, System.Random r, float z, float yL, float yR, float xL, float xR, int cloths)
        {
            const float inset = 0.08f;
            xL += inset; xR -= inset;
            float len = xR - xL;
            float ang = Mathf.Atan2(yR - yL, len) * Mathf.Rad2Deg;
            Box("Filo", parent, new Vector3((xL + xR) * 0.5f, (yL + yR) * 0.5f, z), new Vector3(len, 0.015f, 0.015f), "M_FerroRuggine02", new Vector3(0f, 0f, ang), false);
            float maxW = len / (cloths + 1f) * 0.8f;
            for (int k = 1; k <= cloths; k++)
            {
                float tt = k / (cloths + 1f) + ((float)r.NextDouble() - 0.5f) * 0.03f;
                double roll = r.NextDouble();
                string kind = roll < 0.22 ? "Lenzuolo" : roll < 0.58 ? "Camicia" : roll < 0.84 ? "Pantaloni" : "Straccio";
                int variant = r.Next(0, 2);
                float baseW = kind == "Lenzuolo" ? 1.25f : kind == "Camicia" ? 0.81f : kind == "Pantaloni" ? 0.64f : 0.54f; // larghezze dei modelli Panno_* (R007 t2)
                float s = 0.75f + (float)r.NextDouble() * 0.2f; // R007 t3: scala reale (camicia ~0,7 m, lenzuolo ~1,1 m)
                float sx = Mathf.Min(s, maxW / baseW);
                float yTop = Mathf.Lerp(yL, yR, tt) - 0.03f;
                string mat = ClothColors[r.Next(ClothColors.Length)];
                if (kind == "Lenzuolo" && r.NextDouble() < 0.6) mat = "PannoBianco";
                ClothObject(parent, kind, variant, mat, new Vector3(Mathf.Lerp(xL, xR, tt), yTop, z + ((float)r.NextDouble() - 0.5f) * 0.04f),
                    (float)(r.NextDouble() - 0.5) * 12f, new Vector3(sx, s, sx));
            }
        }

        // ------------------------------------------------------------------ scarpa logora

        static Mesh shoeMesh;

        // scarpa di cuoio consumata (lunga 28 cm) costruita per sezioni: tomaia, suola con tacco, apertura scura alla caviglia.
        static Mesh ShoeMesh()
        {
            if (shoeMesh != null) return shoeMesh;
            var v = new List<Vector3>(); var uv = new List<Vector2>();
            var tUp = new List<int>(); var tSole = new List<int>(); var tIn = new List<int>();
            const int K = 16, M = 10;
            const float L = 0.28f, z0 = -0.14f, baseY = 0.022f;
            float[] hw = new float[K + 1], hu = new float[K + 1], hs = new float[K + 1];
            for (int k = 0; k <= K; k++)
            {
                float s = k / (float)K;
                float round = Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow(Mathf.Abs(2f * s - 1f), 6f)));
                hw[k] = (0.038f + 0.03f * Mathf.Sin(Mathf.PI * Mathf.Pow(s, 0.8f))) * round;
                hu[k] = Mathf.Lerp(0.10f, 0.035f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.95f, s))) * Mathf.Sqrt(round);
                hs[k] = baseY + (s < 0.22f ? 0.018f : 0f);
            }
            var upIdx = new int[K + 1, M + 1];
            var soleIdx = new int[K + 1, 4];
            for (int k = 0; k <= K; k++)
            {
                float z = z0 + L * k / K;
                for (int m = 0; m <= M; m++)
                {
                    float th = Mathf.PI * m / M;
                    float x = hw[k] * Mathf.Cos(th);
                    float y = baseY + hu[k] * Mathf.Sin(th);
                    // apertura della caviglia: la parte alta del tallone scende
                    float sN = k / (float)K;
                    if (sN < 0.3f) y -= 0.024f * Mathf.Sin(th) * Mathf.Sin(th) * (1f - sN / 0.3f);
                    upIdx[k, m] = v.Count; v.Add(new Vector3(x, y, z)); uv.Add(new Vector2(x / 0.15f, z / 0.15f));
                }
                float sw = hw[k] + 0.006f;
                Vector3[] sp = { new Vector3(-sw, 0f, z), new Vector3(-sw, hs[k], z), new Vector3(sw, hs[k], z), new Vector3(sw, 0f, z) };
                for (int q = 0; q < 4; q++) { soleIdx[k, q] = v.Count; v.Add(sp[q]); uv.Add(new Vector2(sp[q].x / 0.15f, z / 0.15f)); }
            }
            for (int k = 0; k < K; k++)
            {
                Vector3 cUp = new Vector3(0f, baseY + 0.03f, z0 + L * k / K);
                Vector3 cSole = new Vector3(0f, 0.011f, z0 + L * k / K);
                for (int m = 0; m < M; m++)
                {
                    bool inside = k < 4 && m >= 3 && m <= 6;
                    ShoeQuad(v, inside ? tIn : tUp, upIdx[k, m], upIdx[k, m + 1], upIdx[k + 1, m + 1], upIdx[k + 1, m], cUp);
                }
                for (int q = 0; q < 4; q++)
                    ShoeQuad(v, tSole, soleIdx[k, q], soleIdx[k, (q + 1) % 4], soleIdx[k + 1, (q + 1) % 4], soleIdx[k + 1, q], cSole);
            }
            var mesh = new Mesh();
            mesh.SetVertices(v); mesh.SetUVs(0, uv);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(tUp, 0); mesh.SetTriangles(tSole, 1); mesh.SetTriangles(tIn, 2);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            shoeMesh = SaveKitMesh(mesh, "Scarpa_Logora");
            return shoeMesh;
        }

        // quad orientato verso l'esterno rispetto al centro della sezione
        static void ShoeQuad(List<Vector3> v, List<int> tl, int a, int b, int c, int d, Vector3 centre)
        {
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            Vector3 mid = (v[a] + v[b] + v[c] + v[d]) * 0.25f;
            if (Vector3.Dot(n, mid - centre) >= 0f) { tl.Add(a); tl.Add(b); tl.Add(c); tl.Add(a); tl.Add(c); tl.Add(d); }
            else { tl.Add(a); tl.Add(c); tl.Add(b); tl.Add(a); tl.Add(d); tl.Add(c); }
        }

        static GameObject ShoeObject(Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = new GameObject("Scarpa");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = ShoeMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { Mat("Cuoio"), Mat("SuolaScarpa"), Mat("Finestra") };
            mr.receiveGI = ReceiveGI.LightProbes;
            return go;
        }

        // ------------------------------------------------------------------ luci procedurali del flashback

        static Texture2D SaveGeneratedTexture(string name, int w, int h, System.Func<float, float, Color> pixel)
        {
            ArtGenerator.EnsureFolder("Assets/_Game/Generated/Tex");
            string path = "Assets/_Game/Generated/Tex/" + name + ".png";
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, pixel(x / (float)(w - 1), y / (float)(h - 1)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Hash(int x, int y)
        {
            unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        }

        static float ValueNoise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), fx), Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), fx), fy);
        }

        static Material EmissiveMat(string name, Texture2D tex, float intensity)
        {
            string path = "Assets/Art/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.08f));
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", 0.1f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void EnsureLightMaterials()
        {
            // finestra: luce calda sfumata verso i bordi, con aloni di sporco e striature sul vetro
            var win = SaveGeneratedTexture("T_LuceFinestra", 128, 128, (u, w) =>
            {
                float nx = u * 2f - 1f, ny = w * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx * 0.8f + ny * ny);
                float core = Mathf.Clamp01(1f - d * 0.85f);
                float dirt = 0.62f + 0.38f * ValueNoise(u * 7f, w * 9f);
                float streak = 0.92f + 0.08f * Mathf.Sin(u * 55f + ValueNoise(u * 4f, w * 4f) * 6f);
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(w, 1f - w)) * 5f);
                float k = (0.45f + 0.75f * core) * dirt * streak * (0.55f + 0.45f * edge);
                return new Color(1.0f * k, 0.80f * k, 0.5f * k);
            });
            EmissiveMat("M_LuceFinestra", win, 3.0f);
            // uscita: bagliore verticale piu' chiaro in alto, soffuso ai lati
            var door = SaveGeneratedTexture("T_LuceUscita", 64, 128, (u, w) =>
            {
                float side = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Abs(u * 2f - 1f), 2.5f) * 0.7f);
                float k = Mathf.Lerp(0.6f, 1.1f, w) * side * (0.9f + 0.1f * ValueNoise(u * 5f, w * 8f));
                return new Color(1.0f * k, 0.84f * k, 0.55f * k);
            });
            EmissiveMat("M_LuceUscita", door, 2.4f);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ arredo del flashback

        // scaffale a ripiani; mn = angolo minimo a terra
        static void AddShelf(KitBuilder kb, Vector3 mn, float lx, float lz, float h, float[] levels,
                             bool backOnMinZ, bool backOnMaxZ, bool backOnMinX, bool backOnMaxX)
        {
            float t = 0.05f;
            Vector3 mx = mn + new Vector3(lx, h, lz);
            foreach (var cx in new[] { mn.x, mx.x - t })
                foreach (var cz in new[] { mn.z, mx.z - t })
                    kb.Box(new Vector3(cx, 0f, cz), new Vector3(cx + t, h, cz + t), 0, TileWood);
            foreach (var y in levels) kb.Box(new Vector3(mn.x, y, mn.z), new Vector3(mx.x, y + 0.04f, mx.z), 0, TileWood);
            kb.Box(new Vector3(mn.x, h - 0.04f, mn.z), new Vector3(mx.x, h, mx.z), 0, TileWood);
            if (backOnMaxZ) kb.Box(new Vector3(mn.x, 0.05f, mx.z - 0.02f), new Vector3(mx.x, h - 0.04f, mx.z), 0, TileWood);
            if (backOnMinZ) kb.Box(new Vector3(mn.x, 0.05f, mn.z), new Vector3(mx.x, h - 0.04f, mn.z + 0.02f), 0, TileWood);
            if (backOnMinX) kb.Box(new Vector3(mn.x, 0.05f, mn.z), new Vector3(mn.x + 0.02f, h - 0.04f, mx.z), 0, TileWood);
            if (backOnMaxX) kb.Box(new Vector3(mx.x - 0.02f, 0.05f, mn.z), new Vector3(mx.x, h - 0.04f, mx.z), 0, TileWood);
        }
    }
}
