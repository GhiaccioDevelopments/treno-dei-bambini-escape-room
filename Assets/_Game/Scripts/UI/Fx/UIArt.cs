using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.UI.Fx
{
    /// <summary>
    /// Texture procedurali della UI d'epoca (carta, ottone, ceralacca, biglietto, cartellino, timbri, icone...).
    /// Generate al volo da codice e messe in cache: nessun file nel progetto, zero peso aggiunto alla build.
    /// Le sprite 9-slice hanno il bordo indicato (usare Image.type = Sliced).
    /// </summary>
    public static class UIArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Texture2D grainTex;

        // ------------------------------------------------------------ rumore e forme
        static float H(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>Value noise 0..1 con frequenza f (cicli per pixel).</summary>
        static float VN(float x, float y, float f, int seed)
        {
            x *= f; y *= f;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = H(xi, yi, seed), b = H(xi + 1, yi, seed), c = H(xi, yi + 1, seed), d = H(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Distanza con segno da un rettangolo arrotondato (negativa dentro).</summary>
        static float RR(float px, float py, float w, float h, float r)
        {
            float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
            float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Color32 C(Color c, float a = 1f)
        {
            return new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(a));
        }

        delegate void Paint(Color32[] px, int w, int h);

        static Sprite Make(string key, int w, int h, float border, Paint paint, bool linear = false)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            var px = new Color32[w * h];
            paint(px, w, h);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "UIArt_" + key;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = border > 0f
                ? Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border))
                : Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.name = tex.name;
            cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------ carta
        static void PaintPaper(Color32[] px, int w, int h, Color baseC, float r, float jitter, int seed, float edgeDark, float fibers)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = VN(x, y, 0.28f, seed) * 2f - 1f;
                    float d = RR(x + 0.5f, y + 0.5f, w, h, r) + n * jitter;
                    float a = Mathf.Clamp01(0.5f - d);
                    float depth = Mathf.Clamp01(-d / 8f);
                    float stain = VN(x, y, 0.045f, seed + 1) - 0.5f;
                    float fib = (H(x, y, seed + 2) - 0.5f) * fibers + (VN(x, y * 0.25f, 0.5f, seed + 3) - 0.5f) * fibers;
                    Color c = baseC * (0.97f + stain * 0.22f + fib);
                    c = Color.Lerp(c, c * new Color(0.62f, 0.5f, 0.38f), (1f - depth) * edgeDark);
                    c.a = 1f;
                    px[y * w + x] = C(c, a);
                }
        }

        /// <summary>Carta invecchiata con bordo irregolare/bruciacchiato. 9-slice (bordo 26).</summary>
        public static Sprite Paper() { return Make("paper", 128, 128, 26f, (p, w, h) => PaintPaper(p, w, h, UITheme.CartaChiara, 10f, 3.2f, 11, 0.85f, 0.06f)); }

        /// <summary>Carta più scura/morbida per le caselle e le righe di testo. 9-slice.</summary>
        public static Sprite PaperDark() { return Make("paperdark", 96, 96, 20f, (p, w, h) => PaintPaper(p, w, h, UITheme.CartaScura, 8f, 2.2f, 21, 0.55f, 0.05f)); }

        /// <summary>Cartellino manila (pulsanti secondari e prompt). 9-slice.</summary>
        public static Sprite Tag() { return Make("tag", 96, 64, 18f, (p, w, h) => PaintPaper(p, w, h, new Color32(0xDB, 0xC4, 0x8E, 0xFF), 9f, 1.2f, 31, 0.6f, 0.05f)); }

        /// <summary>Fotografia/cartolina (bordo bianco). 9-slice.</summary>
        public static Sprite Postcard()
        {
            return Make("postcard", 96, 96, 12f, (p, w, h) =>
            {
                PaintPaper(p, w, h, new Color32(0xF4, 0xEE, 0xDC, 0xFF), 3f, 0.8f, 41, 0.4f, 0.04f);
                // finissima linea interna
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w - 12f, h - 12f, 2f);
                        float dd = Mathf.Abs(d);
                        if (x > 5 && y > 5 && x < w - 5 && y < h - 5 && dd < 0.8f) { var c = p[y * w + x]; p[y * w + x] = C(new Color(c.r / 255f, c.g / 255f, c.b / 255f) * 0.85f, c.a / 255f); }
                    }
            });
        }

        /// <summary>Pagina del quaderno anni '40. 9-slice (bordo sinistro largo per il margine).</summary>
        public static Sprite Notebook() { return Make("notebook", 128, 128, 26f, (p, w, h) => PaintPaper(p, w, h, new Color32(0xEF, 0xE6, 0xCE, 0xFF), 6f, 1.6f, 51, 0.5f, 0.04f)); }

        /// <summary>Copertina nera del quaderno (cornice scura). 9-slice.</summary>
        public static Sprite Cover()
        {
            return Make("cover", 96, 96, 28f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w, h, 12f);
                        float a = Mathf.Clamp01(0.5f - d);
                        float g = 0.10f + (VN(x, y, 0.5f, 61) - 0.5f) * 0.05f + Mathf.Clamp01(-d / 6f) * 0.03f;
                        float edge = Mathf.Clamp01(1f - Mathf.Abs(d + 3f) / 1.6f) * 0.18f;
                        p[y * w + x] = C(new Color(g + edge, g * 0.95f + edge, g * 0.9f + edge), a);
                    }
            });
        }

        // ------------------------------------------------------------ metalli e cera
        /// <summary>Targa d'ottone con bordo inciso. 9-slice (bordo 22).</summary>
        public static Sprite Brass()
        {
            return Make("brass", 96, 64, 22f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w, h, 14f);
                        float a = Mathf.Clamp01(0.5f - d);
                        float t = y / (float)(h - 1); // 0 basso, 1 alto
                        Color c = Color.Lerp(UITheme.Ottone * 0.78f, UITheme.OttoneLuce, Mathf.Pow(t, 1.3f));
                        float brush = (VN(x * 0.15f, y * 6f, 1f, 71) - 0.5f) * 0.10f;
                        c *= 1f + brush;
                        float bev = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 1.6f);
                        c = Color.Lerp(c, t > 0.5f ? UITheme.OttoneLuce * 1.15f : UITheme.Ottone * 0.55f, bev * 0.8f);
                        float eng = Mathf.Clamp01(1f - Mathf.Abs(d + 6.5f) / 1.1f);
                        c = Color.Lerp(c, UITheme.Ottone * 0.5f, eng * 0.65f);
                        c.a = 1f;
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        /// <summary>Cornice d'ottone (centro trasparente) con ribattini ai quattro angoli. 9-slice (bordo 30).</summary>
        public static Sprite BrassFrame()
        {
            return Make("brassframe", 96, 96, 30f, (p, w, h) =>
            {
                const float th = 11f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w, h, 12f);
                        float dIn = RR(x + 0.5f, y + 0.5f, w - 2f * th, h - 2f * th, 5f);
                        // dIn è calcolato rispetto a un rettangolo centrato più piccolo: traslo
                        dIn = RR(x + 0.5f - th, y + 0.5f - th, w - 2f * th, h - 2f * th, 5f);
                        float a = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(dIn + 0.5f);
                        float t = y / (float)(h - 1);
                        Color c = Color.Lerp(UITheme.Ottone * 0.7f, UITheme.OttoneLuce, t);
                        c *= 1f + (VN(x, y, 0.4f, 81) - 0.5f) * 0.12f;
                        float bev = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 1.5f);
                        c = Color.Lerp(c, UITheme.OttoneLuce * 1.1f, bev * 0.6f);
                        float inner = Mathf.Clamp01(1f - Mathf.Abs(dIn) / 1.6f);
                        c = Color.Lerp(c, UITheme.Ottone * 0.45f, inner * 0.7f);
                        // ribattini
                        float rv = Mathf.Min(Mathf.Min(Dist(x, y, 6.5f, 6.5f), Dist(x, y, w - 6.5f, 6.5f)),
                                             Mathf.Min(Dist(x, y, 6.5f, h - 6.5f), Dist(x, y, w - 6.5f, h - 6.5f)));
                        if (rv < 3.4f) c = Color.Lerp(UITheme.Ottone * 0.5f, UITheme.OttoneLuce * 1.2f, Mathf.Clamp01(1.4f - rv * 0.4f));
                        c.a = 1f;
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        static float Dist(float x, float y, float cx, float cy)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Sigillo di ceralacca (targa rossa irregolare con riflesso). 9-slice (bordo 24).</summary>
        public static Sprite Seal()
        {
            return Make("seal", 96, 64, 24f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float n = VN(x, y, 0.18f, 91) * 2f - 1f;
                        float d = RR(x + 0.5f, y + 0.5f, w - 2f, h - 2f, 18f) + n * 2.4f;
                        float a = Mathf.Clamp01(0.5f - d);
                        float t = y / (float)(h - 1);
                        Color c = Color.Lerp(UITheme.Ceralacca * 0.8f, UITheme.Ceralacca * 1.45f, t);
                        float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 5f) / 2.2f);
                        c = Color.Lerp(c, UITheme.Ceralacca * 0.6f, rim * 0.6f);
                        float hl = Mathf.Clamp01(1f - Dist(x, y, w * 0.28f, h * 0.78f) / (h * 0.55f));
                        c = Color.Lerp(c, new Color(0.95f, 0.55f, 0.5f), hl * hl * 0.45f);
                        c.a = 1f;
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        // ------------------------------------------------------------ biglietto, tasti, timbri
        /// <summary>Biglietto ferroviario (stub a sinistra separato da linea tratteggiata). 9-slice (bordo 30).</summary>
        public static Sprite Ticket()
        {
            return Make("ticket", 128, 64, 30f, (p, w, h) =>
            {
                PaintPaper(p, w, h, new Color32(0xE9, 0xDE, 0xC0, 0xFF), 4f, 1f, 101, 0.5f, 0.05f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color32 q = p[y * w + x];
                        Color c = new Color(q.r / 255f, q.g / 255f, q.b / 255f);
                        bool band = (y > 4 && y < 8) || (y < h - 5 && y > h - 9);
                        bool dash = Mathf.Abs(x - 22) < 1 && (y % 8) < 4 && y > 4 && y < h - 5;
                        if (band && x > 4 && x < w - 5) c = Color.Lerp(c, UITheme.InchiostroBlu, 0.75f);
                        if (dash) c = Color.Lerp(c, UITheme.Inchiostro, 0.55f);
                        p[y * w + x] = C(c, q.a / 255f);
                    }
            });
        }

        /// <summary>Tasto di macchina da scrivere (per il prompt "E"). 9-slice (bordo 14).</summary>
        public static Sprite KeyCap()
        {
            return Make("keycap", 48, 48, 14f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w, h, 12f);
                        float a = Mathf.Clamp01(0.5f - d);
                        float dIn = RR(x + 0.5f - 4f, y + 0.5f - 6f, w - 8f, h - 8f, 9f);
                        Color c = new Color(0.12f, 0.1f, 0.09f);
                        if (dIn < 0f) c = Color.Lerp(new Color(0.9f, 0.84f, 0.7f), new Color(0.97f, 0.93f, 0.82f), y / (float)h);
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(dIn) / 1.3f);
                        c = Color.Lerp(c, UITheme.OttoneLuce, ring * 0.9f);
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        /// <summary>Contorno di timbro (rettangolo a inchiostro con macchie), centro trasparente. 9-slice (bordo 16). Colorare con Image.color.</summary>
        public static Sprite StampFrame()
        {
            return Make("stampframe", 64, 64, 16f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w, h, 8f);
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(d + 3.2f) / 2.6f);
                        float ring2 = Mathf.Clamp01(1f - Mathf.Abs(d + 8.5f) / 1.1f) * 0.8f;
                        float a = Mathf.Max(ring, ring2);
                        float mott = VN(x, y, 0.55f, 111);
                        a *= mott > 0.28f ? 0.92f : 0.45f;
                        p[y * w + x] = C(Color.white, a);
                    }
            });
        }

        /// <summary>Contorno tratteggiato a matita per gli slot vuoti. 9-slice (bordo 16).</summary>
        public static Sprite DashedFrame()
        {
            return Make("dashed", 64, 64, 16f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = RR(x + 0.5f, y + 0.5f, w - 2f, h - 2f, 10f);
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 1.1f);
                        bool dash = ((x + y) % 9) < 5;
                        float a = ring * (dash ? 0.85f : 0f);
                        p[y * w + x] = C(UITheme.Inchiostro, a);
                    }
            });
        }

        // ------------------------------------------------------------ forme semplici
        public static Sprite White()
        {
            return Make("white", 8, 8, 0f, (p, w, h) => { for (int i = 0; i < p.Length; i++) p[i] = new Color32(255, 255, 255, 255); });
        }

        /// <summary>Cerchio morbido (alone / punto di luce). Bianco, da colorare.</summary>
        public static Sprite Glow()
        {
            return Make("glow", 64, 64, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = Dist(x, y, w * 0.5f, h * 0.5f) / (w * 0.5f);
                        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                        p[y * w + x] = C(Color.white, a);
                    }
            });
        }

        /// <summary>Sbuffo di vapore: macchia morbida e irregolare. Bianco, da colorare.</summary>
        public static Sprite Smoke()
        {
            return Make("smoke", 96, 96, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = Dist(x, y, w * 0.5f, h * 0.5f) / (w * 0.5f);
                        float n = VN(x, y, 0.09f, 121) * 0.6f + VN(x, y, 0.2f, 122) * 0.4f;
                        float a = Mathf.Clamp01(1f - d - (1f - n) * 0.55f);
                        a = a * a * 1.6f;
                        p[y * w + x] = C(Color.white, Mathf.Clamp01(a));
                    }
            });
        }

        /// <summary>Stella a quattro punte per scintille dorate.</summary>
        public static Sprite Spark()
        {
            return Make("spark", 32, 32, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f), dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                        float star = Mathf.Clamp01(1f - Mathf.Pow(dx, 0.5f) - Mathf.Pow(dy, 0.5f) + 0.0f);
                        float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.6f);
                        p[y * w + x] = C(Color.white, Mathf.Clamp01(star * 1.6f + core));
                    }
            });
        }

        /// <summary>Rombo dorato con smusso (indicatore interagibile).</summary>
        public static Sprite Diamond()
        {
            return Make("diamond", 48, 48, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f), dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                        float d = dx + dy; // 1 sul bordo
                        float a = Mathf.Clamp01((1f - d) * 0.5f * w);
                        Color c = Color.Lerp(UITheme.Ottone, UITheme.OttoneLuce * 1.1f, Mathf.Clamp01((1f - d) * 2.2f));
                        float side = ((x + 0.5f - w * 0.5f) < 0f ? 1f : 0.8f) * ((y + 0.5f - h * 0.5f) > 0f ? 1f : 0.85f);
                        c *= side;
                        float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) / 0.06f);
                        c = Color.Lerp(c, UITheme.Oro * 1.2f, rim * 0.7f);
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        // ------------------------------------------------------------ icone
        public static Sprite IconSuitcase()
        {
            return Make("icon_valigia", 64, 64, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color c = Color.clear;
                        float body = RR(x + 0.5f, y + 0.5f - 6f, 52f, 36f, 5f);
                        // il corpo è centrato: traslo
                        body = RR(x + 0.5f - 6f, y + 0.5f - 8f, 52f, 36f, 5f);
                        if (body < 0.5f)
                        {
                            float t = (y - 8f) / 36f;
                            c = Color.Lerp(UITheme.Cartone * 0.7f, UITheme.Cartone * 1.15f, t);
                            c.a = Mathf.Clamp01(0.5f - body);
                            if (x > 6 + 14 - 1 && x < 6 + 14 + 1) c = Color.Lerp(c, UITheme.Inchiostro, 0.3f);
                            if (Mathf.Abs(body + 2.5f) < 0.8f) c = Color.Lerp(c, UITheme.Inchiostro, 0.4f);
                            if (y > 24 && y < 28 && x > 28 && x < 36) c = UITheme.OttoneLuce; // chiusura
                            if ((x < 13 || x > 49) && (y < 15 || y > 36)) c = Color.Lerp(c, UITheme.Ottone, 0.8f); // angoli
                        }
                        // maniglia
                        float hd = Mathf.Abs(RR(x + 0.5f - 22f, y + 0.5f - 42f, 20f, 12f, 4f));
                        if (hd < 1.4f && y > 42) c = new Color(UITheme.Inchiostro.r, UITheme.Inchiostro.g, UITheme.Inchiostro.b, 1f);
                        p[y * w + x] = C(c, c.a);
                    }
            });
        }

        public static Sprite IconWatch()
        {
            return Make("icon_orologio", 64, 64, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = Dist(x, y, 32f, 28f);
                        Color c = Color.clear;
                        if (d < 22f)
                        {
                            c = d > 17f ? Color.Lerp(UITheme.Ottone, UITheme.OttoneLuce, y / 64f) : new Color(0.95f, 0.92f, 0.82f);
                            c.a = Mathf.Clamp01(22.5f - d);
                            if (d <= 17f)
                            {
                                // lancette: 12 e 3
                                if (Mathf.Abs(x + 0.5f - 32f) < 1.1f && y + 0.5f > 28f && y + 0.5f < 28f + 12f) c = UITheme.Inchiostro;
                                if (Mathf.Abs(y + 0.5f - 28f) < 1.1f && x + 0.5f > 32f && x + 0.5f < 32f + 8f) c = UITheme.Inchiostro;
                                for (int k = 0; k < 12; k++)
                                {
                                    float ang = k * Mathf.PI / 6f;
                                    if (Dist(x, y, 32f + Mathf.Sin(ang) * 14.5f, 28f + Mathf.Cos(ang) * 14.5f) < 1.3f) c = UITheme.Inchiostro;
                                }
                            }
                        }
                        // anello superiore
                        float ring = Mathf.Abs(Dist(x, y, 32f, 55f) - 5f);
                        if (ring < 1.3f && y > 48) c = UITheme.Ottone;
                        if (x > 29 && x < 36 && y > 47 && y < 52) c = UITheme.OttoneLuce;
                        p[y * w + x] = C(c, c.a);
                    }
            });
        }

        public static Sprite IconLens()
        {
            return Make("icon_lente", 64, 64, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color c = Color.clear;
                        float d = Dist(x, y, 26f, 38f);
                        if (d < 20f && d > 15f) { c = Color.Lerp(UITheme.Ottone, UITheme.OttoneLuce, y / 64f); c.a = 1f; }
                        else if (d <= 15f) c = new Color(0.85f, 0.95f, 1f, 0.35f);
                        // manico
                        float ax = x + 0.5f - 40f, ay = y + 0.5f - 24f;
                        float along = (ax - ay) * 0.7071f, perp = (ax + ay) * 0.7071f;
                        if (along > -2f && along < 18f && Mathf.Abs(perp) < 3.2f) c = UITheme.Cartone;
                        p[y * w + x] = C(c, c.a);
                    }
            });
        }

        // ------------------------------------------------------------ pellicola, iride
        /// <summary>Grana di pellicola con qualche graffio verticale. Texture ripetibile (RawImage).</summary>
        public static Texture2D Grain()
        {
            if (grainTex != null) return grainTex;
            const int n = 128;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = H(x, y, 201);
                    float a = v > 0.86f ? (v - 0.86f) * 5f : 0f;
                    float dark = v < 0.07f ? 0.5f : 0f;
                    if (x == 23 || x == 91) a = Mathf.Max(a, H(23, y / 9, 3) > 0.55f ? 0.35f : 0f);
                    byte g = (byte)(dark > 0f ? 20 : 235);
                    px[y * n + x] = new Color32(g, g, g, (byte)(Mathf.Clamp01(Mathf.Max(a, dark)) * 255f));
                }
            grainTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            grainTex.name = "UIArt_grain";
            grainTex.wrapMode = TextureWrapMode.Repeat;
            grainTex.filterMode = FilterMode.Point;
            grainTex.SetPixels32(px);
            grainTex.Apply(false, true);
            return grainTex;
        }

        /// <summary>
        /// Iride: opaca fuori, foro centrale dal bordo irregolare. burn = bordo che brucia (arancio) invece di nero netto.
        /// Il foro ha raggio 0.22 della larghezza. Va scalata e circondata da 4 pannelli pieni.
        /// </summary>
        public static Sprite Iris(bool burn)
        {
            return Make(burn ? "iris_burn" : "iris", 256, 256, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x + 0.5f - w * 0.5f) / w, dy = (y + 0.5f - h * 0.5f) / h;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx);
                        float wob = burn ? (VN(Mathf.Cos(ang) * 40f + 50f, Mathf.Sin(ang) * 40f + 50f, 0.35f, 131) - 0.5f) * 0.05f : 0f;
                        float edge = 0.22f + wob;
                        float a = Mathf.Clamp01((r - edge) / (burn ? 0.05f : 0.012f));
                        Color c = new Color(0.02f, 0.015f, 0.01f);
                        if (burn)
                        {
                            float rim = Mathf.Clamp01(1f - Mathf.Abs(r - edge - 0.012f) / 0.03f);
                            c = Color.Lerp(c, new Color(0.95f, 0.55f, 0.18f), rim * (1f - a * 0.3f));
                        }
                        p[y * w + x] = C(c, a);
                    }
            });
        }

        public static Sprite Vignette(Color tint)
        {
            return Make("vig_" + ColorUtility.ToHtmlStringRGB(tint), 128, 128, 0f, (p, w, h) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                        float a = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) * 1.1f);
                        p[y * w + x] = C(tint, a * a);
                    }
            });
        }

        static Texture2D ruledTex;

        /// <summary>Texture ripetibile (4x30) con una riga azzurra: righe del quaderno. Usare con RawImage.uvRect.</summary>
        public static Texture2D Ruled()
        {
            if (ruledTex != null) return ruledTex;
            var px = new Color32[4 * 30];
            for (int y = 0; y < 30; y++)
                for (int x = 0; x < 4; x++)
                    px[y * 4 + x] = y == 0 ? new Color32(80, 110, 160, 90) : new Color32(0, 0, 0, 0);
            ruledTex = new Texture2D(4, 30, TextureFormat.RGBA32, false);
            ruledTex.name = "UIArt_ruled";
            ruledTex.wrapMode = TextureWrapMode.Repeat;
            ruledTex.filterMode = FilterMode.Point;
            ruledTex.SetPixels32(px);
            ruledTex.Apply(false, true);
            return ruledTex;
        }
    }
}
