using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.Mappa
{
    /// <summary>
    /// R019: sprite procedurali della minimappa (freccetta del giocatore, X rossa a matita, freccia sul bordo) e glifi dei punti d'interesse
    /// (striscia Resources/Mappe/glifi.png, generata da Treno ▸ Mappa ▸ Genera piante). Tutto in cache; nessun file in piu' oltre alla striscia.
    /// </summary>
    public static class MappaArt
    {
        public static readonly string[] OrdineGlifi = { "porta", "fontana", "edicola", "pozzo", "lavatoio", "uscita", "stazione", "bottega" };
        const int CellaGlifo = 48;

        static Sprite freccia, croce, bordo;
        static readonly Dictionary<string, Sprite> glifi = new Dictionary<string, Sprite>();
        static Texture2D glifiTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { freccia = null; croce = null; bordo = null; glifi.Clear(); glifiTex = null; }

        static float Cop(float d) { return Mathf.Clamp01(0.5f - d); }

        static Sprite Crea(int n, System.Func<float, float, Color> paint)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = paint(x + 0.5f, y + 0.5f);
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static float DistSeg(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay, t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / Mathf.Max(0.0001f, abx * abx + aby * aby));
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Freccetta d'inchiostro che punta in alto, con un filo di carta intorno per staccarla dalla pianta.</summary>
        public static Sprite Freccia()
        {
            if (freccia != null) return freccia;
            var ink = new Color(0.14f, 0.09f, 0.06f); var carta = new Color(0.97f, 0.93f, 0.8f);
            const int n = 64;
            freccia = Crea(n, (x, y) =>
            {
                // freccia a punta di lancia: punta (32, 58), ali (14, 8) e (50, 8), tacca (32, 20)
                float u = x, v = y;
                float d = Mathf.Max(DistTri(u, v, 32f, 58f, 12f, 6f, 32f, 18f), DistTri(u, v, 32f, 58f, 32f, 18f, 52f, 6f));
                float inner = Mathf.Min(DistTri(u, v, 32f, 58f, 12f, 6f, 32f, 18f), DistTri(u, v, 32f, 58f, 32f, 18f, 52f, 6f));
                float aInk = Cop(inner);
                float aPaper = Cop(inner - 3.2f);
                if (aPaper <= 0f) return new Color(carta.r, carta.g, carta.b, 0f);
                var c = Color.Lerp(carta, ink, aInk);
                c.a = aPaper;
                return c;
            });
            return freccia;
        }

        // distanza con segno (negativa dentro) da un triangolo
        static float DistTri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float e0 = Edge(px, py, ax, ay, bx, by), e1 = Edge(px, py, bx, by, cx, cy), e2 = Edge(px, py, cx, cy, ax, ay);
            bool neg = (e0 < 0 && e1 < 0 && e2 < 0) || (e0 > 0 && e1 > 0 && e2 > 0);
            float d = Mathf.Min(DistSeg(px, py, ax, ay, bx, by), Mathf.Min(DistSeg(px, py, bx, by, cx, cy), DistSeg(px, py, cx, cy, ax, ay)));
            return neg ? -d : d;
        }

        static float Edge(float px, float py, float ax, float ay, float bx, float by) { return (px - ax) * (by - ay) - (py - ay) * (bx - ax); }

        /// <summary>X rossa a matita dentro un cerchio tremolante: il segno del prossimo obiettivo.</summary>
        public static Sprite Croce()
        {
            if (croce != null) return croce;
            var rosso = new Color(0.62f, 0.10f, 0.08f);
            const int n = 96;
            croce = Crea(n, (x, y) =>
            {
                float cx = 48f, cy = 48f;
                float ang = Mathf.Atan2(y - cy, x - cx);
                float rr = 36f + Mathf.Sin(ang * 3f + 1.2f) * 1.6f + Mathf.Sin(ang * 5f) * 0.9f;
                float dc = Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - rr) - 2.2f;
                float d1 = DistSeg(x, y, 27f, 27f, 70f, 70f) - 4.4f;
                float d2 = DistSeg(x, y, 69f, 26f, 28f, 69f) - 4.4f;
                float a = Mathf.Max(Cop(dc), Mathf.Max(Cop(d1), Cop(d2)));
                // alone chiaro per leggere la X anche sulle zone a tratteggio
                float halo = Mathf.Max(Cop(Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - rr) - 5.5f), Mathf.Max(Cop(d1 - 3f), Cop(d2 - 3f)));
                if (a <= 0.01f && halo <= 0.01f) return new Color(1f, 0.95f, 0.85f, 0f);
                var c = Color.Lerp(new Color(0.98f, 0.94f, 0.82f), rosso, a);
                c.a = Mathf.Max(a, halo * 0.75f);
                return c;
            });
            return croce;
        }

        /// <summary>Punta di freccia rossa (verso l'alto) per l'obiettivo fuori dalla minimappa.</summary>
        public static Sprite Bordo()
        {
            if (bordo != null) return bordo;
            var rosso = new Color(0.62f, 0.10f, 0.08f); var carta = new Color(0.98f, 0.94f, 0.82f);
            const int n = 64;
            bordo = Crea(n, (x, y) =>
            {
                float d = DistTri(x, y, 32f, 56f, 8f, 10f, 56f, 10f);
                float a = Cop(d); float ap = Cop(d - 3f);
                if (ap <= 0f) return new Color(carta.r, carta.g, carta.b, 0f);
                var c = Color.Lerp(carta, rosso, a);
                c.a = ap;
                return c;
            });
            return bordo;
        }

        /// <summary>Sprite del glifo (porta, fontana, ...) tagliato dalla striscia; null se la striscia manca.</summary>
        public static Sprite Glifo(string tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return null;
            Sprite s;
            if (glifi.TryGetValue(tipo, out s)) return s;
            if (glifiTex == null) glifiTex = Resources.Load<Texture2D>("Mappe/glifi");
            int i = System.Array.IndexOf(OrdineGlifi, tipo);
            if (glifiTex != null && i >= 0)
                s = Sprite.Create(glifiTex, new Rect(i * CellaGlifo, 0, CellaGlifo, CellaGlifo), new Vector2(0.5f, 0.5f), 100f);
            glifi[tipo] = s;
            return s;
        }
    }
}
