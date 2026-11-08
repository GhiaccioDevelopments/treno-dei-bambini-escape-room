using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TrenoBambini.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R023: generatore riproducibile delle risorse del menu e dell'inventario.
    /// Treno > R023 > Genera tutto: texture della latta (smalto verde consumato, coperchio con un motivo originale: locomotiva a vapore nel cartiglio,
    /// ruggine leggera, graffi, ammaccature nella normal map), meshes con spigoli smussati, set da tavolo della latta (prefab in Resources/Inventario),
    /// prefab delle scarpe (FBX fatto in Blender, vedi ArtSource/Models/Scarpe), materiale del fumo del menu, crediti.json da CREDITS.md.
    /// Tutte le texture sono generate da codice (nessun asset esterno), 1024 px al massimo.
    /// </summary>
    public static class R023Builder
    {
        const string TexDir = "Assets/Art/Textures/Latta";
        const string MatDir = "Assets/Art/Materials";
        const string MeshDir = "Assets/Art/Models/Latta";
        const string ResInv = "Assets/_Game/Resources/Inventario";
        const string ResMod = "Assets/_Game/Resources/Modelli";
        const string ResMenu = "Assets/_Game/Resources/Menu";

        [MenuItem("Treno/R023/Genera tutto")]
        public static void Tutto()
        {
            GeneraCrediti();
            PreparaMenu();
            GeneraLatta();
            GeneraPrefabScarpe();
            GeneraStudio();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[R023] Risorse generate.");
        }

        static void Cartella(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Cartella(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ crediti.json da CREDITS.md
        [MenuItem("Treno/R023/Genera crediti.json da CREDITS.md")]
        public static void GeneraCrediti()
        {
            var lines = File.ReadAllLines("CREDITS.md");
            var file = new CreditiFile();
            int cAsset = -1, cTipo = -1, cAutore = -1, cLic = -1;
            foreach (var raw in lines)
            {
                string l = raw.Trim();
                if (!l.StartsWith("|")) { cAsset = -1; continue; }
                var cells = l.Trim('|').Split('|');
                for (int i = 0; i < cells.Length; i++) cells[i] = Pulisci(cells[i]);
                if (cells.Length > 0 && cells[0].StartsWith("---")) continue;
                if (cells.Length > 0 && (cells[0] == "Asset" || cells[0] == "Nome"))
                {
                    cAsset = 0; cTipo = cAutore = cLic = -1;
                    for (int i = 0; i < cells.Length; i++)
                    {
                        if (cells[i] == "Tipo") cTipo = i;
                        else if (cells[i].StartsWith("Autore")) cAutore = i;
                        else if (cells[i].StartsWith("Licenza")) cLic = i;
                    }
                    continue;
                }
                if (cAsset < 0 || cAutore < 0 || cLic < 0 || cells.Length <= Math.Max(cAutore, cLic)) continue;
                var v = new CreditoVoce { asset = cells[0], tipo = cTipo >= 0 && cTipo < cells.Length ? cells[cTipo] : "", autore = cells[cAutore], licenza = cells[cLic] };
                if (string.IsNullOrWhiteSpace(v.asset)) continue;
                file.voci.Add(v);
            }
            string json = JsonUtility.ToJson(file, true);
            File.WriteAllText("Assets/_Game/Resources/Data/crediti.json", json);
            AssetDatabase.ImportAsset("Assets/_Game/Resources/Data/crediti.json");
            Debug.Log("[R023] crediti.json: " + file.voci.Count + " voci.");
        }

        static string Pulisci(string s)
        {
            s = s.Trim();
            s = Regex.Replace(s, @"\[([^\]]+)\]\([^)]*\)", "$1"); // [CC0](url) -> CC0
            s = s.Replace("**", "").Replace("`", "");
            return s;
        }

        // ------------------------------------------------------------------ menu: materiale del fumo
        [MenuItem("Treno/R023/Prepara materiale fumo del menu")]
        public static void PreparaMenu()
        {
            Cartella(ResMenu);
            string path = ResMenu + "/M_FumoMenu.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) { Debug.LogError("[R023] Shader particelle URP non trovato."); return; }
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetColor("_BaseColor", Color.white);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ texture della latta
        static float H(int x, int y, int s)
        {
            unchecked { int h = x * 374761393 + y * 668265263 + s * 1442695041; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        }
        static float VN(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(H(xi, yi, s), H(xi + 1, yi, s), fx), Mathf.Lerp(H(xi, yi + 1, s), H(xi + 1, yi + 1, s), fx), fy);
        }
        static float Fbm(float x, float y, int s, int oct = 5)
        {
            float a = 0.5f, sum = 0f, f = 1f;
            for (int i = 0; i < oct; i++) { sum += a * VN(x * f, y * f, s + i * 17); f *= 2.03f; a *= 0.5f; }
            return sum;
        }
        static float Edge(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        static float SdBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - r;
        }
        static float SdCircle(Vector2 p, Vector2 c, float r) { return (p - c).magnitude - r; }
        static float SdCapsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }
        static float Cov(float sd) { return Mathf.Clamp01(0.5f - sd / 1.6f); }

        static readonly Color Smalto = new Color(0.07f, 0.27f, 0.20f);
        static readonly Color Crema = new Color(0.84f, 0.75f, 0.54f);
        static readonly Color Oro = new Color(0.72f, 0.54f, 0.20f);
        static readonly Color Inchiostro = new Color(0.06f, 0.10f, 0.09f);
        static readonly Color Metallo = new Color(0.34f, 0.32f, 0.29f);
        static readonly Color Ruggine = new Color(0.42f, 0.19f, 0.08f);

        /// <summary>Consumo comune a corpo e coperchio: smalto scrostato che lascia il metallo nudo, ruggine leggera, graffi sottili.</summary>
        static Color Consuma(Color c, float u, float v, int seed, float bordo, List<Vector4> graffi)
        {
            float m = Fbm(u * 7f, v * 7f, seed);
            float edge = Mathf.Clamp01(1f - Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) / 0.05f) * bordo;
            float worn = Edge(0.60f, 0.72f, m + edge * 0.45f);
            var bare = Color.Lerp(Metallo, Metallo * 1.15f, Fbm(u * 40f, v * 40f, seed + 5));
            float rust = Edge(0.66f, 0.80f, Fbm(u * 11f + 7f, v * 11f + 3f, seed + 9)) * Mathf.Clamp01(worn * 1.2f + edge * 0.5f);
            c = Color.Lerp(c, bare, worn * 0.9f);
            c = Color.Lerp(c, Ruggine, rust * 0.6f);
            // sporco leggero nelle pieghe
            c *= 0.95f + 0.1f * Fbm(u * 3f, v * 3f, seed + 21);
            foreach (var g in graffi)
            {
                float d = SdCapsule(new Vector2(u, v), new Vector2(g.x, g.y), new Vector2(g.z, g.w), 0.0009f);
                if (d < 0.0009f) c = Color.Lerp(c, bare, Mathf.Clamp01(0.5f - d / 0.0018f) * 0.3f);
            }
            return c;
        }

        static List<Vector4> Graffi(int n, int seed)
        {
            var l = new List<Vector4>();
            for (int i = 0; i < n; i++)
            {
                float x = H(i, 1, seed), y = H(i, 2, seed), a = (H(i, 3, seed) - 0.5f) * 3f, len = 0.02f + H(i, 4, seed) * 0.06f;
                l.Add(new Vector4(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len));
            }
            return l;
        }

        static void SalvaTex(string path, Texture2D t, bool normal)
        {
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !normal;
            imp.maxTextureSize = 1024;
            imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }

        [MenuItem("Treno/R023/Genera texture e materiali della latta")]
        public static void GeneraLatta()
        {
            Cartella(TexDir); Cartella(MeshDir);

            // corpo
            int W = 512;
            var corpo = new Texture2D(W, W, TextureFormat.RGB24, false);
            var gC = Graffi(14, 11);
            var px = new Color[W * W];
            for (int y = 0; y < W; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / W;
                    var c = Smalto * (0.9f + 0.2f * Fbm(u * 5f, v * 5f, 3));
                    px[y * W + x] = Consuma(c, u, v, 31, 0.6f, gC);
                }
            corpo.SetPixels(px);
            SalvaTex(TexDir + "/T_Latta_Corpo.png", corpo, false);

            // coperchio 1024x768: bordo crema, filetto oro, raggi, cartiglio con locomotiva a vapore (motivo originale)
            int LW = 1024, LH = 768;
            var lid = new Texture2D(LW, LH, TextureFormat.RGB24, false);
            var lp = new Color[LW * LH];
            var gL = Graffi(22, 23);
            var C = new Vector2(LW * 0.5f, LH * 0.5f);
            for (int y = 0; y < LH; y++)
                for (int x = 0; x < LW; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float u = p.x / LW, v = p.y / LH;
                    var c = Smalto;
                    // raggi dal centro
                    float ang = Mathf.Atan2(p.y - C.y, p.x - C.x);
                    float rays = Mathf.Pow(Mathf.Abs(Mathf.Sin(ang * 12f)), 6f);
                    c = Color.Lerp(c, Smalto * 1.45f, rays * 0.5f * Mathf.Clamp01(((p - C).magnitude - 190f) / 200f));
                    // cornice: banda crema, filetto scuro, filetto oro
                    c = Color.Lerp(c, Crema, Cov(SdBox(p, C, new Vector2(LW * 0.5f - 22f, LH * 0.5f - 22f), 28f)) * (1f - Cov(SdBox(p, C, new Vector2(LW * 0.5f - 58f, LH * 0.5f - 58f), 18f))));
                    c = Color.Lerp(c, Oro, Cov(Mathf.Abs(SdBox(p, C, new Vector2(LW * 0.5f - 74f, LH * 0.5f - 74f), 14f)) - 3f));
                    // greca di rombi lungo la banda
                    if (y > 30 && y < 50 || y > LH - 50 && y < LH - 30)
                    {
                        float dd = Mathf.Abs(((x % 40) - 20f)) + Mathf.Abs(((y % 20) - 10f)) * 2f;
                        c = Color.Lerp(c, Smalto, Mathf.Clamp01(1f - dd / 14f) * 0.8f);
                    }
                    // ornamenti d'angolo: quarti di cerchio oro
                    foreach (var k in new[] { new Vector2(110f, 110f), new Vector2(LW - 110f, 110f), new Vector2(110f, LH - 110f), new Vector2(LW - 110f, LH - 110f) })
                    {
                        float ring = Mathf.Abs(SdCircle(p, k, 34f)) - 3f;
                        c = Color.Lerp(c, Oro, Cov(ring));
                        c = Color.Lerp(c, Oro, Cov(SdCircle(p, k, 9f)));
                    }
                    // cartiglio centrale: anello oro, campo scuro
                    c = Color.Lerp(c, Oro, Cov(SdCircle(p, C, 236f)));
                    c = Color.Lerp(c, Smalto * 0.8f, Cov(SdCircle(p, C, 226f)));
                    c = Color.Lerp(c, Inchiostro, Cov(SdCircle(p, C, 214f)));
                    c = Color.Lerp(c, Crema * 0.85f, Cov(Mathf.Abs(SdCircle(p, C, 198f)) - 2f));
                    // locomotiva a vapore stilizzata (volta a sinistra)
                    float L = LocomotivaSd(p - C);
                    c = Color.Lerp(c, Crema, Cov(L));
                    // sbuffi di fumo
                    for (int s = 0; s < 4; s++)
                    {
                        var sp = C + new Vector2(-92f + s * 38f, 82f + s * 26f);
                        c = Color.Lerp(c, Crema * 0.75f, Cov(SdCircle(p, sp, 14f + s * 6f)) * (0.85f - s * 0.15f));
                    }
                    // binario
                    c = Color.Lerp(c, Crema, Cov(SdBox(p, C + new Vector2(0f, -102f), new Vector2(170f, 3f), 1f)));
                    lp[y * LW + x] = Consuma(c, u, v, 57, 1f, gL);
                }
            lid.SetPixels(lp);
            SalvaTex(TexDir + "/T_Latta_Coperchio.png", lid, false);

            // normal map: martellatura leggera e qualche ammaccatura larga
            var nm = new Texture2D(W, W, TextureFormat.RGB24, false);
            var h = new float[W * W];
            for (int y = 0; y < W; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)W;
                    h[y * W + x] = Fbm(u * 48f, v * 48f, 71, 3) * 0.25f + Fbm(u * 6f, v * 6f, 83, 4) * 0.9f;
                }
            var npx = new Color[W * W];
            for (int y = 0; y < W; y++)
                for (int x = 0; x < W; x++)
                {
                    float hl = h[y * W + (x + W - 1) % W], hr = h[y * W + (x + 1) % W];
                    float hd = h[((y + W - 1) % W) * W + x], hu = h[((y + 1) % W) * W + x];
                    var n = new Vector3((hl - hr) * 5f, (hd - hu) * 5f, 1f).normalized;
                    npx[y * W + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                }
            nm.SetPixels(npx);
            SalvaTex(TexDir + "/T_Latta_Normale.png", nm, true);

            // materiali
            var tCorpo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_Latta_Corpo.png");
            var tLid = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_Latta_Coperchio.png");
            var tNm = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_Latta_Normale.png");
            Materiale("M_Latta_Corpo", tCorpo, tNm, 0.2f, 0.45f, Color.white);
            Materiale("M_Latta_Coperchio", tLid, tNm, 0.2f, 0.5f, Color.white);
            Materiale("M_Latta_Interno", null, tNm, 0.6f, 0.35f, new Color(0.42f, 0.41f, 0.38f));
            Materiale("M_Lacci_Scarpe", null, null, 0f, 0.15f, new Color(0.06f, 0.05f, 0.04f));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Distanza con segno (in pixel, centro del cartiglio) della silhouette di una locomotiva a vapore volta a sinistra. Disegno originale.</summary>
        static float LocomotivaSd(Vector2 p)
        {
            float d = SdBox(p, new Vector2(-30f, 6f), new Vector2(105f, 36f), 10f);                 // caldaia
            d = Mathf.Min(d, SdCircle(p, new Vector2(-132f, 6f), 36f));                             // fronte rotondo
            d = Mathf.Min(d, SdBox(p, new Vector2(100f, 12f), new Vector2(38f, 52f), 4f));          // cabina
            d = Mathf.Min(d, SdBox(p, new Vector2(100f, 70f), new Vector2(48f, 7f), 2f));           // tetto
            d = Mathf.Min(d, SdBox(p, new Vector2(-100f, 58f), new Vector2(11f, 26f), 2f));         // ciminiera
            d = Mathf.Min(d, SdBox(p, new Vector2(-100f, 84f), new Vector2(21f, 6f), 3f));          // bocca della ciminiera
            d = Mathf.Min(d, SdCircle(p, new Vector2(-28f, 50f), 17f));                             // duomo
            d = Mathf.Min(d, SdBox(p, new Vector2(18f, 50f), new Vector2(9f, 12f), 2f));            // duomo della sabbia
            d = Mathf.Min(d, SdBox(p, new Vector2(-80f, -34f), new Vector2(60f, 7f), 2f));          // telaio basso
            d = Mathf.Min(d, SdCapsule(p, new Vector2(-170f, -34f), new Vector2(-150f, -52f), 5f)); // cacciapietre
            foreach (float wx in new[] { -110f, -50f, 10f }) d = Mathf.Min(d, SdCircle(p, new Vector2(wx, -62f), 26f)); // ruote
            d = Mathf.Min(d, SdCircle(p, new Vector2(94f, -52f), 38f));                             // ruota grande
            // raggi: bucano ruote grandi con un cerchietto scuro (sottrae)
            float hole = Mathf.Min(SdCircle(p, new Vector2(94f, -52f), 12f), SdCircle(p, new Vector2(10f, -62f), 8f));
            hole = Mathf.Min(hole, Mathf.Min(SdCircle(p, new Vector2(-50f, -62f), 8f), SdCircle(p, new Vector2(-110f, -62f), 8f)));
            // finestrino della cabina
            hole = Mathf.Min(hole, SdBox(p, new Vector2(100f, 24f), new Vector2(15f, 14f), 3f));
            return Mathf.Max(d, -hole);
        }

        static Material Materiale(string nome, Texture2D baseMap, Texture2D normal, float metal, float smooth, Color tint)
        {
            string path = MatDir + "/" + nome + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetColor("_BaseColor", tint);
            m.SetTexture("_BaseMap", baseMap);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", smooth);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", 0.6f); m.EnableKeyword("_NORMALMAP"); }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ mesh con spigoli smussati
        /// <summary>Scatola con spigoli arrotondati (raggio r), griglia fitta (cella ~cell) per poter ammaccare. Faccia +Y nel sottomesh 0 se topSeparata.</summary>
        static Mesh RBox(string nome, Vector3 size, float r, float cell, bool topSeparata, Func<Vector3, Vector3, Vector3> deforma = null)
        {
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
            var tTop = new List<int>(); var tRest = new List<int>();
            var h = size * 0.5f;
            for (int axis = 0; axis < 3; axis++)
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    int ua = (axis + 1) % 3, va = (axis + 2) % 3;
                    var us = Coords(h[ua], r, cell); var vs = Coords(h[va], r, cell);
                    int start = verts.Count;
                    for (int j = 0; j < vs.Count; j++)
                        for (int i = 0; i < us.Count; i++)
                        {
                            var pt = Vector3.zero;
                            pt[axis] = sgn * h[axis]; pt[ua] = us[i]; pt[va] = vs[j];
                            var inner = new Vector3(Mathf.Clamp(pt.x, -(h.x - r), h.x - r), Mathf.Clamp(pt.y, -(h.y - r), h.y - r), Mathf.Clamp(pt.z, -(h.z - r), h.z - r));
                            var dir = pt - inner;
                            var n = dir.sqrMagnitude > 1e-12f ? dir.normalized : (Vector3)Vector3.up;
                            var pos = inner + n * r;
                            if (deforma != null) pos = deforma(pos, n);
                            verts.Add(pos); norms.Add(n);
                            uvs.Add(new Vector2(pt[ua] / size[ua] + 0.5f, pt[va] / size[va] + 0.5f));
                        }
                    var list = (topSeparata && axis == 1 && sgn == 1) ? tTop : tRest;
                    // orientamento: il triangolo deve guardare verso l'esterno (asse della faccia)
                    var eu = Vector3.zero; eu[ua] = 1f; var ev = Vector3.zero; ev[va] = 1f;
                    bool flip = Vector3.Dot(Vector3.Cross(eu, ev), sgn * Axis(axis)) > 0f;
                    for (int j = 0; j < vs.Count - 1; j++)
                        for (int i = 0; i < us.Count - 1; i++)
                        {
                            int a = start + j * us.Count + i, b = a + 1, c = a + us.Count, d = c + 1;
                            if (!flip) { list.Add(a); list.Add(c); list.Add(b); list.Add(b); list.Add(c); list.Add(d); }
                            else { list.Add(a); list.Add(b); list.Add(c); list.Add(b); list.Add(d); list.Add(c); }
                        }
                }
            var m = new Mesh { name = nome };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs);
            if (topSeparata) { m.subMeshCount = 2; m.SetTriangles(tTop, 0); m.SetTriangles(tRest, 1); }
            else { m.subMeshCount = 1; var all = new List<int>(tTop); all.AddRange(tRest); m.SetTriangles(all, 0); }
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }

        static Vector3 Axis(int a) { var v = Vector3.zero; v[a] = 1f; return v; }

        static List<float> Coords(float half, float r, float cell)
        {
            var l = new List<float> { -half, -half + r };
            float a = -half + r, b = half - r;
            int n = Mathf.Max(1, Mathf.CeilToInt((b - a) / cell));
            for (int i = 1; i < n; i++) l.Add(a + (b - a) * i / n);
            l.Add(half - r); l.Add(half);
            return l;
        }

        static Mesh SalvaMesh(Mesh m)
        {
            string path = MeshDir + "/" + m.name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { EditorUtility.CopySerialized(m, old); UnityEngine.Object.DestroyImmediate(m); return old; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ prefab delle scarpe
        [MenuItem("Treno/R023/Genera prefab scarpe")]
        public static void GeneraPrefabScarpe()
        {
            Cartella(ResMod);
            string fbx = "Assets/Art/Models/Scarpe/Scarpe.fbx";
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp == null) { Debug.LogWarning("[R023] FBX delle scarpe mancante: " + fbx); return; }
            imp.globalScale = 1f; imp.useFileScale = true; imp.meshCompression = ModelImporterMeshCompression.Low;
            imp.isReadable = false; imp.importCameras = false; imp.importLights = false; imp.importAnimation = false;
            imp.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = new GameObject("scarpe");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            var mats = new[]
            {
                AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Cuoio_Scarpe.mat"),
                AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Suola_Scarpa.mat"),
                AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Lacci_Scarpe.mat")
            };
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
            {
                var cur = r.sharedMaterials; var next = new Material[cur.Length];
                for (int i = 0; i < next.Length; i++) next[i] = mats[Mathf.Min(i, mats.Length - 1)];
                r.sharedMaterials = next;
                r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            }
            PrefabUtility.SaveAsPrefabAsset(root, ResMod + "/scarpe.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ set da tavolo con la latta
        [MenuItem("Treno/R023/Genera set della latta (prefab)")]
        public static void GeneraStudio()
        {
            Cartella(ResInv);
            var mCorpo = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Corpo.mat");
            var mLid = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Coperchio.mat");
            var mInt = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Interno.mat");
            var mLegno = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_TavoloLegno01.mat");
            var mPanno = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Panno_Terra.mat");
            if (mCorpo == null) { GeneraLatta(); mCorpo = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Corpo.mat"); mLid = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Coperchio.mat"); mInt = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Latta_Interno.mat"); }

            float L = 0.36f, D = 0.25f, Hh = 0.1f, s = 0.008f, r = 0.0035f;
            var root = new GameObject("StudioLatta");

            var tavolo = Cubo(root.transform, "Tavolo", new Vector3(0f, -0.03f, 0f), new Vector3(2.6f, 0.06f, 1.6f), mLegno);
            var panno = Cubo(root.transform, "Panno", new Vector3(-0.1f, 0.002f, 0f), new Vector3(0.95f, 0.004f, 0.6f), mPanno);
            panno.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);

            var tin = new GameObject("Latta").transform;
            tin.SetParent(root.transform, false);
            tin.localPosition = new Vector3(-0.14f, 0f, 0.02f);
            tin.localRotation = Quaternion.Euler(0f, 8f, 0f);

            Parte(tin, "Fondo", RBox("Latta_Fondo", new Vector3(L, s, D), r, 0.03f, false), new Vector3(0f, s / 2f, 0f), mCorpo);
            Parte(tin, "Interno", RBox("Latta_Interno", new Vector3(L - 2 * s, 0.002f, D - 2 * s), 0.0005f, 0.1f, false), new Vector3(0f, s + 0.001f, 0f), mInt);
            Parte(tin, "Posteriore", RBox("Latta_Posteriore", new Vector3(L, Hh, s), r, 0.012f, false), new Vector3(0f, Hh / 2f, D / 2f - s / 2f), mCorpo);
            // parete anteriore con un'ammaccatura
            Func<Vector3, Vector3, Vector3> ammacca = (pos, n) =>
            {
                var c = new Vector3(-0.07f, 0.045f, 0f);
                float d2 = (pos.x - c.x) * (pos.x - c.x) + (pos.y - c.y) * (pos.y - c.y);
                if (n.z < -0.5f) pos.z += 0.0045f * Mathf.Exp(-d2 / (0.03f * 0.03f));
                return pos;
            };
            Parte(tin, "Anteriore", RBox("Latta_Anteriore", new Vector3(L, Hh, s), r, 0.01f, false, ammacca), new Vector3(0f, Hh / 2f, -D / 2f + s / 2f), mCorpo);
            Parte(tin, "Sinistra", RBox("Latta_Sinistra", new Vector3(s, Hh, D), r, 0.012f, false), new Vector3(-L / 2f + s / 2f, Hh / 2f, 0f), mCorpo);
            Parte(tin, "Destra", RBox("Latta_Destra", new Vector3(s, Hh, D), r, 0.012f, false), new Vector3(L / 2f - s / 2f, Hh / 2f, 0f), mCorpo);
            Parte(tin, "OrloAnteriore", RBox("Latta_Orlo", new Vector3(L + 0.006f, 0.008f, s + 0.006f), 0.002f, 0.05f, false), new Vector3(0f, Hh - 0.002f, -D / 2f + s / 2f), mInt);
            var imb = Parte(tin, "Imbottitura", RBox("Latta_Imbottitura", new Vector3(L - 2 * s - 0.01f, 0.02f, D - 2 * s - 0.01f), 0.006f, 0.04f, false), new Vector3(0f, s + 0.012f, 0f), mPanno);

            // il coperchio e' in piedi dietro la latta, appoggiato indietro: si vede la faccia stampata
            var lid = new GameObject("Coperchio").transform;
            lid.SetParent(root.transform, false);
            lid.localPosition = new Vector3(-0.27f, 0.125f, 0.21f);
            lid.localRotation = Quaternion.Euler(-78f, 8f, 0f);
            var top = RBox("Coperchio_Piastra", new Vector3(L + 0.008f, s, D + 0.008f), 0.0035f, 0.03f, true);
            var pr = Parte(lid, "Piastra", top, Vector3.zero, mLid);
            pr.GetComponent<MeshRenderer>().sharedMaterials = new[] { mLid, mCorpo };
            Parte(lid, "Bordo", RBox("Coperchio_Bordo", new Vector3(L - 0.01f, 0.012f, D - 0.01f), 0.003f, 0.05f, false), new Vector3(0f, -0.008f, 0f), mInt);

            // dove compare l'oggetto da esaminare
            var punto = new GameObject("PuntoOggetto").transform;
            punto.SetParent(root.transform, false);
            punto.localPosition = new Vector3(-0.11f, 0.19f, -0.04f);

            // luci: candela, lampada dall'alto e un filo di luce sull'oggetto
            var cand = new GameObject("Candela").transform;
            cand.SetParent(root.transform, false);
            cand.localPosition = new Vector3(-0.72f, 0f, 0.2f);
            var cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.DestroyImmediate(cil.GetComponent<Collider>());
            cil.name = "Cera"; cil.transform.SetParent(cand, false);
            cil.transform.localPosition = new Vector3(0f, 0.05f, 0f); cil.transform.localScale = new Vector3(0.035f, 0.05f, 0.035f);
            var mCera = Materiale("M_Candela_Cera", null, null, 0f, 0.35f, new Color(0.85f, 0.78f, 0.6f));
            cil.GetComponent<MeshRenderer>().sharedMaterial = mCera;
            var fiamma = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DestroyImmediate(fiamma.GetComponent<Collider>());
            fiamma.name = "Fiamma"; fiamma.transform.SetParent(cand, false);
            fiamma.transform.localPosition = new Vector3(0f, 0.115f, 0f); fiamma.transform.localScale = new Vector3(0.012f, 0.022f, 0.012f);
            var mFiamma = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mFiamma.SetColor("_BaseColor", new Color(1f, 0.78f, 0.35f));
            if (AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Candela_Fiamma.mat") != null) AssetDatabase.DeleteAsset(MatDir + "/M_Candela_Fiamma.mat");
            AssetDatabase.CreateAsset(mFiamma, MatDir + "/M_Candela_Fiamma.mat");
            fiamma.GetComponent<MeshRenderer>().sharedMaterial = mFiamma;
            var lc = new GameObject("LuceCandela"); lc.transform.SetParent(cand, false); lc.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            var l1 = lc.AddComponent<Light>(); l1.type = LightType.Point; l1.range = 3.2f; l1.intensity = 1.6f; l1.color = new Color(1f, 0.66f, 0.3f); l1.shadows = LightShadows.Soft;
            var lamp = new GameObject("LuceLampada"); lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(-0.55f, 0.95f, -0.55f);
            lamp.transform.LookAt(root.transform.position + new Vector3(-0.12f, 0.05f, 0.05f));
            var l2 = lamp.AddComponent<Light>(); l2.type = LightType.Spot; l2.spotAngle = 60f; l2.innerSpotAngle = 25f; l2.range = 3f; l2.intensity = 4.2f;
            l2.color = new Color(1f, 0.92f, 0.8f); l2.shadows = LightShadows.Soft;
            var lcop = new GameObject("LuceCoperchio"); lcop.transform.SetParent(root.transform, false);
            lcop.transform.localPosition = new Vector3(-0.2f, 0.5f, -0.3f);
            lcop.transform.LookAt(root.transform.position + new Vector3(-0.15f, 0.14f, 0.21f));
            var l4 = lcop.AddComponent<Light>(); l4.type = LightType.Spot; l4.spotAngle = 40f; l4.innerSpotAngle = 20f; l4.range = 1.6f; l4.intensity = 2.4f; l4.color = new Color(1f, 0.92f, 0.8f); l4.shadows = LightShadows.None;
            var fill = new GameObject("LuceOggetto"); fill.transform.SetParent(root.transform, false);
            fill.transform.localPosition = new Vector3(0.2f, 0.45f, -0.45f);
            var l3 = fill.AddComponent<Light>(); l3.type = LightType.Point; l3.range = 1.8f; l3.intensity = 0.7f; l3.color = new Color(1f, 0.9f, 0.78f);

            // camera del set (spenta: la accende il pannello) con profilo di post-processing proprio
            var cam = new GameObject("CameraLatta");
            cam.transform.SetParent(root.transform, false);
            cam.transform.localPosition = new Vector3(0.06f, 0.78f, -1.1f);
            cam.transform.LookAt(root.transform.position + new Vector3(0.06f, 0.05f, 0f));
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 32f; c.nearClipPlane = 0.05f; c.farClipPlane = 6f; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.035f, 0.022f, 0.014f);
            c.enabled = false;
            var cd = cam.AddComponent<UniversalAdditionalCameraData>();
            cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.None;

            var vgo = new GameObject("VolumeLatta"); vgo.transform.SetParent(root.transform, false);
            var vol = vgo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 95;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.3f); ca.saturation.Override(-8f); ca.colorFilter.Override(new Color(1f, 0.9f, 0.78f));
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.5f); vg.smoothness.Override(0.55f);
            var fg = prof.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium1); fg.intensity.Override(0.4f);
            var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.35f); bl.threshold.Override(1.2f);
            string profPath = ResInv + "/VP_Latta.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profPath) != null) AssetDatabase.DeleteAsset(profPath);
            AssetDatabase.CreateAsset(prof, profPath);
            foreach (var comp in prof.components) AssetDatabase.AddObjectToAsset(comp, prof);
            vol.sharedProfile = prof;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ResInv + "/StudioLatta.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[R023] Set della latta: " + AssetDatabase.GetAssetPath(prefab));
        }

        static GameObject Cubo(Transform parent, string nome, Vector3 pos, Vector3 scala, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            g.name = nome; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scala;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        static GameObject Parte(Transform parent, string nome, Mesh mesh, Vector3 pos, Material m)
        {
            var g = new GameObject(nome);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            mesh = SalvaMesh(mesh);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>();
            var mats = new Material[mesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            mr.sharedMaterials = mats;
            return g;
        }
    }
}
