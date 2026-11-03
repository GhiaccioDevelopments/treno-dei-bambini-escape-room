using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Genera via codice icone, ritratti, sfondi, vignetta, texture e materiali URP (palette seppia).
    /// Menu: Treno/Genera grafica
    /// </summary>
    public static class ArtGenerator
    {
        const string SpritesRoot = "Assets/_Game/Resources/Sprites";
        const string MaterialsRoot = "Assets/_Game/Art/Materials";
        const string TexturesRoot = "Assets/_Game/Art/Textures";

        // Palette seppia
        static readonly Color Ink = H(0x3B2A1A);
        static readonly Color Paper = H(0xE8D9B5);

        [MenuItem("Treno/Genera grafica")]
        public static void GenerateAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Genera grafica", "Icone...", 0.05f);
                SaveSprite(MakeIconScarpe(), SpritesRoot + "/oggetti/scarpe.png");

                EditorUtility.DisplayProgressBar("Genera grafica", "Ritratti...", 0.25f);
                SaveSprite(MakeAmerigo(), SpritesRoot + "/ritratti/amerigo.png");
                SaveSprite(MakeAntonietta(), SpritesRoot + "/ritratti/antonietta.png");
                SaveSprite(MakeTommasino(), SpritesRoot + "/ritratti/tommasino.png");
                SaveSprite(MakeMaddalena(), SpritesRoot + "/ritratti/maddalena.png");
                SaveSprite(MakeNarratore(), SpritesRoot + "/ritratti/narratore.png");

                EditorUtility.DisplayProgressBar("Genera grafica", "Sfondi...", 0.55f);
                SaveSprite(MakeSfondoVicolo(), SpritesRoot + "/sfondi/cs_vicolo.png");
                SaveSprite(MakeSfondoBasso(), SpritesRoot + "/sfondi/cs_basso.png");
                SaveSprite(MakeSfondoRicordo(), SpritesRoot + "/sfondi/cs_ricordo.png");

                EditorUtility.DisplayProgressBar("Genera grafica", "Vignetta...", 0.8f);
                SaveSprite(MakeVignetta(), SpritesRoot + "/ui/vignetta.png");

                EditorUtility.DisplayProgressBar("Genera grafica", "Materiali...", 0.9f);
                EnsureMaterials();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtGenerator] Grafica generata: icona, 5 ritratti, 3 sfondi, vignetta, materiali.");
        }

        // ------------------------------------------------------------------ utilita

        internal static Color H(int rgb, float a = 1f)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        internal static void EnsureFolder(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static string FullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        static void SaveSprite(Texture2D tex, string assetPath)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            File.WriteAllBytes(FullPath(assetPath), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) { Debug.LogError("[ArtGenerator] Importer non trovato: " + assetPath); return; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = 100f;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ canvas con supersampling

        sealed class Pix
        {
            public readonly int W, H, S;
            readonly int pw, ph;
            readonly Color[] px;

            public Pix(int w, int h, int supersample = 2)
            {
                W = w; H = h; S = supersample; pw = w * S; ph = h * S;
                px = new Color[pw * ph];
            }

            void Put(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= pw || y >= ph) return;
                int i = y * pw + x;
                if (c.a >= 0.999f) { px[i] = c; return; }
                Color d = px[i];
                float oa = c.a + d.a * (1f - c.a);
                if (oa <= 0f) return;
                px[i] = new Color(
                    (c.r * c.a + d.r * d.a * (1f - c.a)) / oa,
                    (c.g * c.a + d.g * d.a * (1f - c.a)) / oa,
                    (c.b * c.a + d.b * d.a * (1f - c.a)) / oa,
                    oa);
            }

            public void Fill(Color c)
            {
                for (int i = 0; i < px.Length; i++) px[i] = c;
            }

            public void GradientV(Color bottom, Color top)
            {
                for (int y = 0; y < ph; y++)
                {
                    Color c = Color.Lerp(bottom, top, y / (float)(ph - 1));
                    for (int x = 0; x < pw; x++) px[y * pw + x] = c;
                }
            }

            public void Rect(float x0, float y0, float x1, float y1, Color c)
            {
                int ix0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) * S));
                int ix1 = Mathf.Min(pw - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) * S) - 1);
                int iy0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) * S));
                int iy1 = Mathf.Min(ph - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) * S) - 1);
                for (int y = iy0; y <= iy1; y++)
                    for (int x = ix0; x <= ix1; x++) Put(x, y, c);
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt((cx - rx) * S));
                int x1 = Mathf.Min(pw - 1, Mathf.CeilToInt((cx + rx) * S));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((cy - ry) * S));
                int y1 = Mathf.Min(ph - 1, Mathf.CeilToInt((cy + ry) * S));
                for (int y = y0; y <= y1; y++)
                {
                    float fy = ((y + 0.5f) / S - cy) / ry;
                    for (int x = x0; x <= x1; x++)
                    {
                        float fx = ((x + 0.5f) / S - cx) / rx;
                        if (fx * fx + fy * fy <= 1f) Put(x, y, c);
                    }
                }
            }

            public void Poly(Vector2[] p, Color c)
            {
                if (p == null || p.Length < 3) return;
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < p.Length; i++)
                {
                    if (p[i].y < minY) minY = p[i].y;
                    if (p[i].y > maxY) maxY = p[i].y;
                }
                int y0 = Mathf.Max(0, Mathf.FloorToInt(minY * S));
                int y1 = Mathf.Min(ph - 1, Mathf.CeilToInt(maxY * S));
                var xs = new List<float>();
                for (int y = y0; y <= y1; y++)
                {
                    float yc = (y + 0.5f) / S;
                    xs.Clear();
                    for (int i = 0; i < p.Length; i++)
                    {
                        Vector2 a = p[i], b = p[(i + 1) % p.Length];
                        if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                        {
                            float t = (yc - a.y) / (b.y - a.y);
                            xs.Add(a.x + t * (b.x - a.x));
                        }
                    }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        int xa = Mathf.Max(0, Mathf.CeilToInt(xs[k] * S - 0.5f));
                        int xb = Mathf.Min(pw - 1, Mathf.FloorToInt(xs[k + 1] * S - 0.5f));
                        for (int x = xa; x <= xb; x++) Put(x, y, c);
                    }
                }
            }

            public void Line(Vector2 a, Vector2 b, float width, Color c)
            {
                float r = width / 2f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - r) * S));
                int x1 = Mathf.Min(pw - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + r) * S));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - r) * S));
                int y1 = Mathf.Min(ph - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + r) * S));
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        Vector2 p = new Vector2((x + 0.5f) / S, (y + 0.5f) / S);
                        float t = len2 < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                        if ((a + ab * t - p).sqrMagnitude <= r * r) Put(x, y, c);
                    }
            }

            public void Polyline(Vector2[] pts, float width, Color c)
            {
                for (int i = 0; i + 1 < pts.Length; i++) Line(pts[i], pts[i + 1], width, c);
            }

            public void OutEllipse(float cx, float cy, float rx, float ry, Color fill, Color outline, float t = 2.5f)
            {
                Ellipse(cx, cy, rx + t, ry + t, outline);
                Ellipse(cx, cy, rx, ry, fill);
            }

            public void OutPoly(Vector2[] pts, Color fill, Color outline, float t = 2.5f)
            {
                for (int i = 0; i < pts.Length; i++) Line(pts[i], pts[(i + 1) % pts.Length], t * 2f, outline);
                Poly(pts, fill);
            }

            public void Noise(int seed, float amount)
            {
                var rnd = new System.Random(seed);
                for (int i = 0; i < px.Length; i++)
                {
                    if (px[i].a <= 0f) continue;
                    float k = 1f + ((float)rnd.NextDouble() - 0.5f) * 2f * amount;
                    px[i] = new Color(Mathf.Clamp01(px[i].r * k), Mathf.Clamp01(px[i].g * k), Mathf.Clamp01(px[i].b * k), px[i].a);
                }
            }

            public void Vignette(float strength)
            {
                for (int y = 0; y < ph; y++)
                {
                    float fy = (y + 0.5f) / ph * 2f - 1f;
                    for (int x = 0; x < pw; x++)
                    {
                        float fx = (x + 0.5f) / pw * 2f - 1f;
                        float d = Mathf.Sqrt(fx * fx + fy * fy);
                        float k = 1f - strength * Mathf.SmoothStep(0.35f, 1.35f, d);
                        Color c = px[y * pw + x];
                        px[y * pw + x] = new Color(c.r * k, c.g * k, c.b * k, c.a);
                    }
                }
            }

            public Texture2D ToTexture()
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                var o = new Color[W * H];
                int n = S * S;
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float r = 0, g = 0, b = 0, a = 0;
                        for (int sy = 0; sy < S; sy++)
                            for (int sx = 0; sx < S; sx++)
                            {
                                Color p = px[(y * S + sy) * pw + x * S + sx];
                                r += p.r * p.a; g += p.g * p.a; b += p.b * p.a; a += p.a;
                            }
                        o[y * W + x] = a > 0f ? new Color(r / a, g / a, b / a, a / n) : new Color(0, 0, 0, 0);
                    }
                tex.SetPixels(o);
                tex.Apply();
                return tex;
            }
        }

        // ------------------------------------------------------------------ icona scarpe

        static void DrawShoe(Pix p, float dx, float dy, float s, Color leather)
        {
            Func<float, float, Vector2> T = (x, y) => new Vector2(dx + x * s, dy + y * s);
            Color sole = H(0x2A1C12);
            Color lace = H(0xE0CFA8);
            // suola
            p.OutPoly(new[] { T(22, 28), T(108, 28), T(110, 38), T(24, 38) }, sole, Ink, 1.5f);
            // tomaia
            p.OutPoly(new[] { T(26, 38), T(26, 80), T(48, 84), T(56, 66), T(80, 58), T(102, 52), T(106, 38) }, leather, Ink, 2f);
            // puntale
            p.Poly(new[] { T(84, 38), T(106, 38), T(102, 52), T(84, 57) }, Color.Lerp(leather, Color.black, 0.18f));
            // allacciatura
            p.Line(T(50, 70), T(62, 78), 2.2f * s, lace);
            p.Line(T(58, 62), T(70, 70), 2.2f * s, lace);
            p.Line(T(66, 56), T(78, 63), 2.2f * s, lace);
            // toppa e riflesso
            p.Rect(30 * s + dx, 46 * s + dy, 44 * s + dx, 58 * s + dy, Color.Lerp(leather, H(0xC9A96B), 0.35f));
            p.Line(T(30, 74), T(44, 78), 2f * s, Color.Lerp(leather, Color.white, 0.25f));
        }

        static Texture2D MakeIconScarpe()
        {
            var p = new Pix(128, 128);
            p.Rect(4, 4, 124, 124, H(0xCDB88F));
            p.Rect(8, 8, 120, 120, Paper);
            p.Ellipse(64, 64, 46, 46, H(0xEFE2C2));
            DrawShoe(p, 36, 54, 0.7f, H(0x6E4C32));
            DrawShoe(p, 4, 8, 0.8f, H(0x4A331F));
            p.Noise(11, 0.03f);
            return p.ToTexture();
        }

        // ------------------------------------------------------------------ ritratti

        static Texture2D MakeAmerigo()
        {
            var p = new Pix(256, 256);
            Color skin = H(0xD9AE80), skinSh = H(0xB88A5E);
            Color jacket = H(0x6E5A44), shirt = H(0xE0D2B0), tweed = H(0x5B4833), tweedDark = H(0x44331F), hair = H(0x3B2A1A);

            p.Rect(112, 86, 144, 120, skinSh);
            p.OutPoly(new[] { V(38, 0), V(218, 0), V(206, 64), V(168, 92), V(88, 92), V(50, 64) }, jacket, Ink);
            p.Poly(new[] { V(104, 92), V(152, 92), V(128, 52) }, shirt);
            p.Line(V(104, 92), V(128, 52), 3f, Ink);
            p.Line(V(152, 92), V(128, 52), 3f, Ink);
            p.Rect(150, 24, 184, 50, H(0x8A7152));
            p.Line(V(150, 24), V(184, 24), 1.5f, Ink);
            p.Line(V(184, 24), V(184, 50), 1.5f, Ink);
            p.Line(V(184, 50), V(150, 50), 1.5f, Ink);
            p.Line(V(150, 50), V(150, 24), 1.5f, Ink);
            p.Ellipse(128, 36, 4, 4, Ink);

            p.OutEllipse(90, 148, 8, 11, skin, Ink, 2f);
            p.OutEllipse(166, 148, 8, 11, skin, Ink, 2f);
            p.OutEllipse(128, 152, 38, 44, skin, Ink);
            p.Poly(new[] { V(90, 170), V(94, 142), V(104, 166) }, hair);
            p.Poly(new[] { V(166, 170), V(162, 142), V(152, 166) }, hair);

            // coppola
            p.OutEllipse(128, 186, 52, 22, tweed, Ink);
            p.Poly(new[] { V(82, 178), V(174, 178), V(170, 168), V(86, 168) }, tweedDark);
            p.OutPoly(new[] { V(94, 172), V(162, 172), V(174, 162), V(128, 155), V(82, 162) }, tweedDark, Ink, 2f);
            p.Ellipse(128, 206, 5, 3, tweedDark);
            p.Line(V(92, 186), V(118, 198), 2f, tweedDark);
            p.Line(V(140, 200), V(166, 188), 2f, tweedDark);

            // volto
            p.Ellipse(112, 142, 7, 8, H(0xF2E8D0));
            p.Ellipse(144, 142, 7, 8, H(0xF2E8D0));
            p.Ellipse(113, 141, 4, 5, Ink);
            p.Ellipse(145, 141, 4, 5, Ink);
            p.Ellipse(114.5f, 143, 1.3f, 1.3f, Color.white);
            p.Ellipse(146.5f, 143, 1.3f, 1.3f, Color.white);
            p.Line(V(103, 153), V(121, 155), 3f, hair);
            p.Line(V(135, 155), V(153, 153), 3f, hair);
            p.Ellipse(128, 128, 4, 3, skinSh);
            p.Ellipse(103, 124, 9, 5, H(0xC98B6A, 0.5f));
            p.Ellipse(153, 124, 9, 5, H(0xC98B6A, 0.5f));
            p.Polyline(new[] { V(117, 119), V(123, 114), V(133, 114), V(139, 119) }, 2.6f, H(0x7A4A3A));
            p.Ellipse(108, 130, 1.2f, 1.2f, skinSh);
            p.Ellipse(150, 131, 1.2f, 1.2f, skinSh);
            p.Ellipse(145, 128, 1.2f, 1.2f, skinSh);

            p.Noise(21, 0.025f);
            return p.ToTexture();
        }

        static Texture2D MakeAntonietta()
        {
            var p = new Pix(256, 256);
            Color skin = H(0xD2A57A), skinSh = H(0xAE8258);
            Color scarf = H(0x2B2220), dress = H(0x4A3A2E), shawl = H(0x8C7050);

            p.Rect(110, 84, 146, 120, skinSh);
            p.OutPoly(new[] { V(30, 0), V(226, 0), V(212, 60), V(172, 90), V(84, 90), V(44, 60) }, dress, Ink);
            p.OutPoly(new[] { V(44, 60), V(84, 90), V(128, 66), V(172, 90), V(212, 60), V(206, 26), V(128, 44), V(50, 26) }, shawl, Ink, 2f);
            for (int i = 0; i < 8; i++)
            {
                p.Line(V(54 + i * 6, 26 - i * 0.2f), V(52 + i * 6, 14), 2f, shawl);
                p.Line(V(150 + i * 7, 40), V(152 + i * 7, 28), 2f, shawl);
            }
            p.Poly(new[] { V(112, 90), V(144, 90), V(128, 68) }, skinSh);

            p.OutEllipse(128, 154, 50, 54, scarf, Ink);
            p.OutEllipse(128, 146, 33, 41, skin, Ink);
            // fazzoletto: fronte e guance
            p.Poly(new[] { V(92, 158), V(98, 186), V(128, 198), V(158, 186), V(164, 158), V(152, 170), V(128, 176), V(104, 170) }, scarf);
            p.Poly(new[] { V(94, 150), V(98, 112), V(112, 100), V(106, 126), V(100, 150) }, scarf);
            p.Poly(new[] { V(162, 150), V(158, 112), V(144, 100), V(150, 126), V(156, 150) }, scarf);
            // nodo e code
            p.OutPoly(new[] { V(120, 98), V(106, 80), V(122, 84) }, scarf, Ink, 1.5f);
            p.OutPoly(new[] { V(136, 98), V(150, 80), V(134, 84) }, scarf, Ink, 1.5f);
            p.OutEllipse(128, 100, 10, 7, scarf, Ink, 1.5f);

            // volto
            p.Ellipse(114, 146, 7, 6, H(0xF2E8D0));
            p.Ellipse(142, 146, 7, 6, H(0xF2E8D0));
            p.Ellipse(114, 145, 4, 4.5f, Ink);
            p.Ellipse(142, 145, 4, 4.5f, Ink);
            p.Line(V(105, 156), V(123, 157), 2.6f, Ink);
            p.Line(V(133, 157), V(151, 156), 2.6f, Ink);
            p.Line(V(107, 137), V(121, 136), 1.6f, skinSh);
            p.Line(V(135, 136), V(149, 137), 1.6f, skinSh);
            p.Polyline(new[] { V(128, 150), V(126, 138), V(130, 132) }, 2.6f, skinSh);
            p.Line(V(118, 118), V(138, 118), 3.2f, H(0x7A4A3A));
            p.Line(V(100, 128), V(104, 118), 1.4f, skinSh);
            p.Line(V(156, 128), V(152, 118), 1.4f, skinSh);

            p.Noise(22, 0.025f);
            return p.ToTexture();
        }

        static Texture2D MakeTommasino()
        {
            var p = new Pix(256, 256);
            Color skin = H(0xDDB487), skinSh = H(0xBC8E62);
            Color shirt = H(0xC9B48A), stripe = H(0xA8946B), strap = H(0x5B4630), hair = H(0x3E2A1A);

            p.Rect(114, 68, 142, 98, skinSh);
            p.OutPoly(new[] { V(64, 0), V(192, 0), V(182, 52), V(154, 74), V(102, 74), V(74, 52) }, shirt, Ink);
            for (int y = 10; y <= 46; y += 12) p.Line(V(80, y), V(176, y), 3f, stripe);
            p.Line(V(104, 74), V(100, 0), 8f, strap);
            p.Line(V(152, 74), V(156, 0), 8f, strap);
            p.Ellipse(102, 30, 4, 4, H(0xC9A96B));
            p.Ellipse(154, 30, 4, 4, H(0xC9A96B));

            p.OutEllipse(82, 126, 9, 12, skin, Ink, 2f);
            p.OutEllipse(174, 126, 9, 12, skin, Ink, 2f);
            p.OutEllipse(128, 128, 46, 48, skin, Ink);
            // capelli spettinati
            p.Poly(new[] { V(84, 132), V(86, 160), V(100, 176), V(128, 182), V(156, 176), V(170, 160), V(172, 132), V(160, 152), V(140, 156), V(120, 150), V(100, 154) }, hair);
            p.Poly(new[] { V(110, 178), V(114, 198), V(126, 178) }, hair);
            p.Poly(new[] { V(126, 180), V(140, 200), V(146, 176) }, hair);
            p.Poly(new[] { V(98, 172), V(88, 190), V(112, 180) }, hair);
            p.Poly(new[] { V(150, 176), V(166, 192), V(168, 168) }, hair);

            // volto
            p.Ellipse(110, 126, 9, 10, H(0xF2E8D0));
            p.Ellipse(146, 126, 9, 10, H(0xF2E8D0));
            p.Ellipse(111, 125, 5.5f, 6.5f, Ink);
            p.Ellipse(147, 125, 5.5f, 6.5f, Ink);
            p.Ellipse(113, 128, 2f, 2f, Color.white);
            p.Ellipse(149, 128, 2f, 2f, Color.white);
            p.Line(V(100, 142), V(120, 144), 2.6f, hair);
            p.Line(V(136, 144), V(156, 142), 2.6f, hair);
            p.Ellipse(128, 112, 4.5f, 3.5f, skinSh);
            p.Ellipse(98, 108, 10, 6, H(0xC98B6A, 0.55f));
            p.Ellipse(158, 108, 10, 6, H(0xC98B6A, 0.55f));
            p.Poly(new[] { V(114, 100), V(142, 100), V(136, 90), V(120, 90) }, H(0x7A3F30));
            p.Rect(121, 98, 126, 101, H(0xF2E8D0));
            p.Rect(130, 98, 135, 101, H(0xF2E8D0));

            p.Noise(23, 0.025f);
            return p.ToTexture();
        }

        static Texture2D MakeMaddalena()
        {
            var p = new Pix(256, 256);
            Color skin = H(0xDDB28A), skinSh = H(0xBA8C64);
            Color hair = H(0x3A2418), blouse = H(0xE6D9BC), blouseSh = H(0xC9BA98), lips = H(0x9A5440);

            p.Rect(112, 84, 144, 118, skinSh);
            p.OutPoly(new[] { V(34, 0), V(222, 0), V(208, 62), V(170, 90), V(86, 90), V(48, 62) }, blouse, Ink);
            p.Poly(new[] { V(114, 92), V(142, 92), V(128, 66) }, skin);
            p.OutPoly(new[] { V(98, 92), V(126, 92), V(118, 62) }, blouseSh, Ink, 1.5f);
            p.OutPoly(new[] { V(158, 92), V(130, 92), V(138, 62) }, blouseSh, Ink, 1.5f);
            p.Ellipse(128, 50, 3.5f, 3.5f, Ink);
            p.Ellipse(128, 34, 3.5f, 3.5f, Ink);
            p.Ellipse(128, 18, 3.5f, 3.5f, Ink);
            p.Line(V(70, 60), V(84, 20), 2f, blouseSh);
            p.Line(V(186, 60), V(172, 20), 2f, blouseSh);

            // capelli corti (caschetto)
            p.OutEllipse(128, 150, 48, 52, hair, Ink);
            p.OutEllipse(128, 144, 34, 42, skin, Ink);
            p.OutPoly(new[] { V(92, 168), V(104, 192), V(132, 199), V(160, 190), V(166, 166), V(152, 176), V(124, 184), V(106, 168) }, hair, Ink, 1.5f);
            p.Poly(new[] { V(90, 168), V(84, 120), V(104, 116), V(96, 140), V(100, 168) }, hair);
            p.Poly(new[] { V(166, 168), V(172, 120), V(152, 116), V(160, 140), V(156, 168) }, hair);
            p.Line(V(102, 190), V(118, 176), 2f, H(0x5A3A28));

            // volto
            p.Ellipse(114, 146, 7, 5.5f, H(0xF2E8D0));
            p.Ellipse(142, 146, 7, 5.5f, H(0xF2E8D0));
            p.Ellipse(114, 145, 4, 4.3f, Ink);
            p.Ellipse(142, 145, 4, 4.3f, Ink);
            p.Line(V(105, 148), V(108, 152), 1.6f, Ink);
            p.Line(V(151, 148), V(148, 152), 1.6f, Ink);
            p.Polyline(new[] { V(104, 156), V(114, 160), V(124, 157) }, 2.4f, hair);
            p.Polyline(new[] { V(132, 157), V(142, 160), V(152, 156) }, 2.4f, hair);
            p.Polyline(new[] { V(128, 148), V(126, 134), V(130, 130) }, 2.2f, skinSh);
            p.Ellipse(128, 118, 10, 4.2f, lips);
            p.Line(V(118, 119), V(138, 119), 1.4f, H(0x6E3A2C));
            p.Ellipse(102, 126, 8, 4.5f, H(0xC98B6A, 0.45f));
            p.Ellipse(154, 126, 8, 4.5f, H(0xC98B6A, 0.45f));
            p.Ellipse(94, 124, 3f, 3f, H(0xC8A050));

            p.Noise(24, 0.025f);
            return p.ToTexture();
        }

        static Texture2D MakeNarratore()
        {
            var p = new Pix(256, 256);
            Color sil = H(0x55402C), cover = H(0x7A4A32), page = H(0xEADFC0), pageSh = H(0xCDBF98);

            // silhouette
            p.OutPoly(new[] { V(46, 0), V(210, 0), V(196, 76), V(158, 108), V(98, 108), V(60, 76) }, sil, Ink);
            p.OutEllipse(128, 172, 34, 40, sil, Ink);
            p.Ellipse(128, 120, 14, 12, sil);

            // libro aperto
            p.OutPoly(new[] { V(30, 24), V(128, 34), V(226, 24), V(226, 36), V(128, 46), V(30, 36) }, cover, Ink, 2f);
            p.OutPoly(new[] { V(38, 34), V(126, 44), V(126, 128), V(38, 114) }, page, Ink, 2f);
            p.OutPoly(new[] { V(130, 44), V(218, 34), V(218, 114), V(130, 128) }, pageSh, Ink, 2f);
            p.Line(V(128, 44), V(128, 128), 3f, Ink);
            for (int i = 0; i < 6; i++)
            {
                float yl = 54 + i * 10;
                p.Line(V(50, yl + 1), V(114, yl + 7), 1.8f, H(0x7A6A52));
                p.Line(V(142, yl + 7), V(206, yl + 1), 1.8f, H(0x7A6A52));
            }
            p.Noise(25, 0.025f);
            return p.ToTexture();
        }

        // ------------------------------------------------------------------ sfondi cutscene

        // punto sulla parete del vicolo in prospettiva: u=0 vicino, u=1 lontano; v=0 pavimento, v=1 cima
        static Vector2 WallPt(bool left, float u, float v)
        {
            float z = 1f + 4f * u;
            float f = (1f / z - 0.2f) / 0.8f;
            float xNear = left ? 0f : 640f, xFar = left ? 275f : 365f;
            float x = Mathf.Lerp(xFar, xNear, f);
            float yF = Mathf.Lerp(205f, 40f, f);
            float yT = Mathf.Lerp(300f, 520f, f);
            return new Vector2(x, Mathf.Lerp(yF, yT, v));
        }

        static void WallQuad(Pix p, bool left, float u0, float u1, float v0, float v1, Color c)
        {
            p.Poly(new[] { WallPt(left, u0, v0), WallPt(left, u1, v0), WallPt(left, u1, v1), WallPt(left, u0, v1) }, c);
        }

        static Texture2D MakeSfondoVicolo()
        {
            var p = new Pix(640, 360);
            var rnd = new System.Random(1946);
            p.GradientV(H(0xE8D2A0), H(0xF4E6C2));

            // parete di fondo
            p.Rect(275, 205, 365, 300, H(0xA98F68));
            p.Rect(305, 215, 322, 238, H(0x3A2A1C));
            p.Rect(330, 250, 345, 272, H(0x3A2A1C));

            // pareti laterali
            WallQuad(p, true, 0f, 1f, 0f, 1f, H(0x9C8058));
            WallQuad(p, false, 0f, 1f, 0f, 1f, H(0xC0A272));

            // finestre e porte
            float[] us = { 0.06f, 0.26f, 0.48f, 0.70f };
            float[] floorsV0 = { 0.24f, 0.46f, 0.68f, 0.90f };
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                for (int i = 0; i < us.Length; i++)
                {
                    for (int f = 0; f < floorsV0.Length; f++)
                    {
                        float u0 = us[i] + (left ? 0f : 0.04f), u1 = u0 + 0.1f;
                        float v0 = floorsV0[f], v1 = v0 + 0.12f;
                        WallQuad(p, left, u0 - 0.012f, u1 + 0.012f, v0 - 0.02f, v1 + 0.02f, H(0x5A4630));
                        bool lit = rnd.NextDouble() < 0.25;
                        WallQuad(p, left, u0, u1, v0, v1, lit ? H(0xE0B866) : H(0x2A1E15));
                        if (rnd.NextDouble() < 0.5)
                            WallQuad(p, left, u0 - 0.035f, u0 - 0.008f, v0, v1, H(0x6B4E30));
                    }
                }
                // porta a piano terra
                float du0 = left ? 0.24f : 0.50f;
                WallQuad(p, left, du0, du0 + 0.09f, 0f, 0.17f, H(0x4A3320));
                WallQuad(p, left, du0 + 0.01f, du0 + 0.08f, 0f, 0.155f, H(0x6B4A2C));
            }

            // pavimento
            p.Poly(new[] { V(0, 0), V(640, 0), V(640, 40), V(365, 205), V(275, 205), V(0, 40) }, H(0x72593F));
            p.Poly(new[] { V(0, 0), V(200, 0), V(300, 205), V(275, 205), V(0, 40) }, H(0x6A5239));
            p.Poly(new[] { V(280, 205), V(360, 205), V(470, 0), V(190, 0) }, H(0x806648, 0.5f));
            for (int i = 0; i < 700; i++)
            {
                float y = (float)rnd.NextDouble() * 205f;
                float xl = y >= 40f ? (y - 40f) / 165f * 275f : 0f;
                float xr = 640f - xl;
                float x = Mathf.Lerp(xl, xr, (float)rnd.NextDouble());
                float s = Mathf.Lerp(8f, 1.6f, y / 205f);
                bool light = rnd.NextDouble() < 0.5;
                p.Ellipse(x, y, s * 1.3f, s * 0.7f, light ? H(0x8C7352, 0.55f) : H(0x4C3A28, 0.5f));
            }

            // panni stesi
            float[] lu = { 0.18f, 0.42f, 0.66f };
            float[] lvL = { 0.62f, 0.52f, 0.60f };
            float[] lvR = { 0.55f, 0.64f, 0.50f };
            Color[] cloth = { H(0xA8503A), H(0x5F7580), H(0xE8DCC0), H(0x7A5A8A, 1f), H(0xC9A24A) };
            for (int i = 0; i < lu.Length; i++)
            {
                Vector2 a = WallPt(true, lu[i], lvL[i]);
                Vector2 b = WallPt(false, lu[i], lvR[i]);
                float z = 1f + 4f * lu[i];
                float scale = Mathf.Lerp(0.35f, 1f, (1f / z - 0.2f) / 0.8f);
                var pts = new Vector2[11];
                for (int k = 0; k < pts.Length; k++)
                {
                    float t = k / 10f;
                    Vector2 q = Vector2.Lerp(a, b, t);
                    q.y -= Mathf.Sin(t * Mathf.PI) * 14f * scale;
                    pts[k] = q;
                }
                p.Polyline(pts, 2f * scale + 0.6f, Ink);
                for (int k = 2; k <= 8; k += 2)
                {
                    Vector2 q = pts[k];
                    float w = (12f + 6f * (float)rnd.NextDouble()) * scale;
                    float h = (22f + 14f * (float)rnd.NextDouble()) * scale;
                    Color c = cloth[rnd.Next(cloth.Length)];
                    p.Poly(new[] { V(q.x - w, q.y), V(q.x + w, q.y), V(q.x + w * 0.9f, q.y - h), V(q.x - w * 0.9f, q.y - h * 0.96f) }, c);
                    p.Line(V(q.x - w, q.y), V(q.x + w, q.y), 1.2f * scale + 0.4f, Ink);
                }
            }

            // sagoma di bambino in lontananza
            p.Ellipse(320, 236, 4.5f, 5.5f, H(0x2A1C12));
            p.Poly(new[] { V(314, 231), V(326, 231), V(327, 214), V(313, 214) }, H(0x2A1C12));
            p.Line(V(316, 214), V(314, 205), 3.2f, H(0x2A1C12));
            p.Line(V(324, 214), V(326, 205), 3.2f, H(0x2A1C12));
            p.Poly(new[] { V(314, 240), V(326, 240), V(322, 246), V(318, 246) }, H(0x2A1C12));

            p.Noise(31, 0.04f);
            p.Vignette(0.5f);
            return p.ToTexture();
        }

        static Texture2D MakeSfondoBasso()
        {
            var p = new Pix(640, 360);
            var rnd = new System.Random(1947);
            p.Fill(H(0x6E5A40));

            // stanza in prospettiva
            p.Poly(new[] { V(110, 120), V(530, 120), V(530, 310), V(110, 310) }, H(0xB09468));
            p.Poly(new[] { V(0, 0), V(0, 360), V(110, 310), V(110, 120) }, H(0x8C7048));
            p.Poly(new[] { V(640, 0), V(640, 360), V(530, 310), V(530, 120) }, H(0x9C7F55));
            p.Poly(new[] { V(0, 360), V(640, 360), V(530, 310), V(110, 310) }, H(0x6E5A40));
            p.Poly(new[] { V(0, 0), V(640, 0), V(530, 120), V(110, 120) }, H(0x5E4833));
            for (int i = 0; i <= 10; i++)
            {
                float x = 110 + i * 42f;
                p.Line(V(x, 120), V(i * 64f, 0), 1.5f, H(0x45331F));
            }
            p.Line(V(110, 120), V(530, 120), 2f, H(0x45331F));
            for (int i = 1; i <= 3; i++)
            {
                float y = 120f - i * i * 12f;
                if (y > 0) p.Line(V(Mathf.Lerp(110, 0, i / 3.3f), y), V(Mathf.Lerp(530, 640, i / 3.3f), y), 1.2f, H(0x45331F));
            }
            // travi
            p.Line(V(110, 310), V(530, 310), 6f, H(0x3E2B1A));
            p.Line(V(0, 360), V(110, 310), 6f, H(0x3E2B1A));
            p.Line(V(640, 360), V(530, 310), 6f, H(0x3E2B1A));
            // macchie di intonaco
            for (int i = 0; i < 18; i++)
            {
                float x = 120 + (float)rnd.NextDouble() * 400f, y = 130 + (float)rnd.NextDouble() * 170f;
                p.Ellipse(x, y, 10 + (float)rnd.NextDouble() * 26f, 6 + (float)rnd.NextDouble() * 14f, H(0x8E7450, 0.35f));
            }

            // finestra con luce
            p.Rect(298, 196, 362, 286, H(0x4A3622));
            p.Rect(306, 204, 354, 278, H(0xFFF0C0));
            p.Line(V(330, 204), V(330, 278), 3f, H(0x4A3622));
            p.Line(V(306, 241), V(354, 241), 3f, H(0x4A3622));
            p.Poly(new[] { V(306, 196), V(354, 196), V(500, 0), V(300, 0) }, H(0xFFE9A8, 0.20f));

            // letto a sinistra
            p.Rect(120, 125, 130, 222, H(0x3E2B1A));
            p.Rect(120, 125, 250, 140, H(0x3E2B1A));
            p.Rect(124, 150, 256, 178, H(0xD8C8A4));
            p.Rect(124, 150, 256, 166, H(0x8A4A3A));
            p.Ellipse(152, 184, 24, 10, H(0xEADFC0));
            p.Line(V(124, 150), V(256, 150), 2f, Ink);
            // tavolo e sedie a destra
            p.Rect(398, 168, 506, 178, H(0x5A3F28));
            p.Rect(406, 126, 413, 168, H(0x5A3F28));
            p.Rect(491, 126, 498, 168, H(0x5A3F28));
            p.Ellipse(450, 188, 11, 10, H(0x2A2420));
            p.Rect(370, 126, 376, 168, H(0x3E2B1A));
            p.Rect(370, 150, 394, 156, H(0x3E2B1A));
            p.Rect(518, 126, 524, 168, H(0x3E2B1A));
            p.Rect(500, 150, 524, 156, H(0x3E2B1A));
            // braciere
            p.Poly(new[] { V(255, 62), V(305, 62), V(296, 38), V(264, 38) }, H(0x2A2420));
            p.Ellipse(280, 64, 36, 16, H(0xFFAA50, 0.22f));
            p.Ellipse(280, 62, 20, 6, H(0xE8803A));
            // filo con panni
            Vector2[] line = new Vector2[9];
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                line[i] = V(Mathf.Lerp(112, 528, t), Mathf.Lerp(292, 280, t) - Mathf.Sin(t * Mathf.PI) * 14f);
            }
            p.Polyline(line, 2f, Ink);
            Color[] cloth = { H(0xA8503A), H(0xE8DCC0), H(0x5F7580) };
            for (int i = 1; i < 8; i += 2)
            {
                var q = line[i];
                p.Poly(new[] { V(q.x - 14, q.y), V(q.x + 14, q.y), V(q.x + 12, q.y - 34), V(q.x - 12, q.y - 30) }, cloth[(i / 2) % cloth.Length]);
            }
            // tenda divisoria
            p.Poly(new[] { V(262, 296), V(290, 296), V(296, 150), V(268, 150) }, H(0x5F7580, 0.9f));
            p.Line(V(262, 296), V(268, 150), 2f, Ink);

            p.Noise(32, 0.04f);
            p.Vignette(0.6f);
            return p.ToTexture();
        }

        static Texture2D MakeSfondoRicordo()
        {
            var p = new Pix(640, 360);
            var rnd = new System.Random(1948);
            p.GradientV(H(0xB89E72), H(0xD9C49A));

            // sagome sbiadite di case sullo sfondo
            for (int i = 0; i < 9; i++)
            {
                float x = i * 80f - 10f;
                float h = 120f + (float)rnd.NextDouble() * 120f;
                p.Rect(x, 140, x + 76, 140 + h, H(0x9C8460, 0.35f));
                for (int k = 0; k < 3; k++)
                    p.Rect(x + 14, 170 + k * 50f, x + 30, 196 + k * 50f, H(0x6E5A40, 0.3f));
            }

            // pavimento
            p.Poly(new[] { V(0, 0), V(640, 0), V(640, 140), V(0, 140) }, H(0x8E7752, 0.9f));

            // luce dalla finestra
            p.Poly(new[] { V(250, 330), V(390, 330), V(560, 0), V(150, 0) }, H(0xFFEFC4, 0.18f));
            p.Rect(268, 190, 372, 330, H(0xFFF3D0, 0.85f));
            p.Line(V(320, 190), V(320, 330), 3f, H(0x8E7752));
            p.Line(V(268, 260), V(372, 260), 3f, H(0x8E7752));
            for (int i = 0; i < 6; i++) p.Ellipse(320, 260, 70 + i * 40f, 50 + i * 28f, H(0xFFF0C8, 0.05f));

            // scarpe in silhouette al centro
            Color sh = H(0x3B2A1A, 0.92f);
            p.Poly(new[] { V(240, 50), V(330, 50), V(334, 62), V(308, 76), V(286, 86), V(262, 90), V(244, 84) }, sh);
            p.Poly(new[] { V(330, 60), V(400, 60), V(404, 72), V(384, 84), V(364, 92), V(344, 94), V(336, 82) }, sh);
            p.Ellipse(320, 46, 120, 10, H(0x3B2A1A, 0.25f));

            // pulviscolo sospeso
            for (int i = 0; i < 160; i++)
            {
                float x = (float)rnd.NextDouble() * 640f, y = (float)rnd.NextDouble() * 360f;
                p.Ellipse(x, y, 1f + (float)rnd.NextDouble() * 2.5f, 1f + (float)rnd.NextDouble() * 2.5f, H(0xFFF0C8, 0.25f + (float)rnd.NextDouble() * 0.3f));
            }

            p.Noise(33, 0.05f);
            p.Vignette(0.7f);
            return p.ToTexture();
        }

        // ------------------------------------------------------------------ vignetta

        static Texture2D MakeVignetta()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = (x + 0.5f) / n * 2f - 1f;
                    float fy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, d)) * 0.92f;
                    px[y * n + x] = new Color(0.08f, 0.05f, 0.03f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ texture e materiali

        static Texture2D MakeCobbleTexture(int size, int cells, int seed)
        {
            var rnd = new System.Random(seed);
            var pts = new Vector2[cells, cells];
            var tone = new float[cells, cells];
            for (int i = 0; i < cells; i++)
                for (int j = 0; j < cells; j++)
                {
                    pts[i, j] = new Vector2((i + 0.2f + 0.6f * (float)rnd.NextDouble()) / cells, (j + 0.2f + 0.6f * (float)rnd.NextDouble()) / cells);
                    tone[i, j] = 0.82f + 0.3f * (float)rnd.NextDouble();
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            Color baseC = new Color(0.52f, 0.46f, 0.38f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    int ci = Mathf.Clamp((int)(p.x * cells), 0, cells - 1);
                    int cj = Mathf.Clamp((int)(p.y * cells), 0, cells - 1);
                    float d1 = 9f, d2 = 9f; int bi = 0, bj = 0;
                    for (int di = -1; di <= 1; di++)
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            int ni = ci + di, nj = cj + dj;
                            Vector2 off = Vector2.zero;
                            if (ni < 0) { ni += cells; off.x = -1f; } else if (ni >= cells) { ni -= cells; off.x = 1f; }
                            if (nj < 0) { nj += cells; off.y = -1f; } else if (nj >= cells) { nj -= cells; off.y = 1f; }
                            float d = (pts[ni, nj] + off - p).magnitude;
                            if (d < d1) { d2 = d1; d1 = d; bi = ni; bj = nj; }
                            else if (d < d2) d2 = d;
                        }
                    float edge = Mathf.SmoothStep(0f, 1f, (d2 - d1) * cells * 2.4f);
                    float k = tone[bi, bj] * (0.42f + 0.58f * edge);
                    px[y * size + x] = new Color(baseC.r * k, baseC.g * k, baseC.b * k, 1f);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D EnsureCobbleTexture()
        {
            string path = TexturesRoot + "/ciottoli.png";
            EnsureFolder(TexturesRoot);
            var tex = MakeCobbleTexture(256, 8, 1946);
            File.WriteAllBytes(FullPath(path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = true;
                imp.anisoLevel = 4;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static string MaterialPath(string name)
        {
            return MaterialsRoot + "/" + name + ".mat";
        }

        public static Material GetMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));
        }

        static Material MakeMat(string name, Color color, float smooth, float metal = 0f, Texture tex = null)
        {
            string path = MaterialPath(name);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[ArtGenerator] Shader 'Universal Render Pipeline/Lit' non trovato.");
                return null;
            }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = shader;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Crea o aggiorna tutti i materiali URP Lit (idempotente).</summary>
        public static void EnsureMaterials()
        {
            EnsureFolder(MaterialsRoot);
            var cobble = EnsureCobbleTexture();

            MakeMat("Tufo", H(0xB89A68), 0.08f);
            MakeMat("TufoScuro", H(0x7E6847), 0.08f);
            MakeMat("Intonaco", H(0xCDA878), 0.1f);
            MakeMat("Legno", H(0x65452A), 0.15f);
            var cio = MakeMat("Ciottoli", Color.white, 0.12f, 0f, cobble);
            if (cio != null) cio.SetTextureScale("_BaseMap", new Vector2(7f, 25f));
            MakeMat("Ferro", H(0x33302C), 0.45f, 0.6f);
            MakeMat("Tessuto1", H(0xA8503A), 0.05f);
            MakeMat("Tessuto2", H(0x5F7580), 0.05f);
            MakeMat("Tessuto3", H(0xE2D5B3), 0.05f);
            MakeMat("Amerigo", H(0x8A6A48), 0.1f);
            MakeMat("Ricordo", H(0xC7AE80), 0.05f);
            MakeMat("Finestra", H(0x1F1610), 0.3f);
            MakeMat("Cera", H(0xEBDFC0), 0.2f);

            var glow = MakeMat("Glow", H(0xFFD98A), 0.2f);
            if (glow != null)
            {
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.2f) * 1.8f);
                glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.SaveAssets();
        }
    }
}
