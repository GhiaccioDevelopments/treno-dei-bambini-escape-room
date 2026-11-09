using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R011: texture dei pezzi di carta dei segreti (ritaglio di giornale, cartolina col Vesuvio, AM-lire, settebello, biglietti delle edicole, lista della spesa).
    /// Testi e parametri in testi_ambiente.json, gruppo "segreti" (ArtSource/Segreti/aggiungi_testi_ambiente.py). Stessa carta invecchiata e stesso carattere 5x7
    /// degli altri testi d'ambiente; i PNG sono piccoli (96-256 px) e finiscono in Assets/_Game/Resources/Texture/Segreti/, dove MondoFab li carica con materiale "tex:&lt;chiave&gt;".
    /// </summary>
    public static partial class SceneBuilder
    {
        const string CartellaTextureSegreti = "Assets/_Game/Resources/Texture/Segreti";

        [MenuItem("Treno/Segreti/Genera texture dei pezzi")]
        public static void GeneraTextureSegreti()
        {
            var file = JsonUtility.FromJson<TestiAmbienteFile>(File.ReadAllText(PercorsoTestiAmbiente));
            ArtGenerator.EnsureFolder("Assets/_Game/Resources/Texture");
            ArtGenerator.EnsureFolder(CartellaTextureSegreti);
            int n = 0;
            foreach (var t in file.testi)
            {
                if (t.gruppo != "segreti") continue;
                var m = new float[t.w * t.h];
                bool cornice = true;
                switch (t.tipo)
                {
                    case "ritaglio": MascheraRitaglio(m, t); cornice = false; break;
                    case "cartolina": MascheraCartolina(m, t); break;
                    case "amlire": MascheraAmLire(m, t); break;
                    case "settebello": MascheraSettebello(m, t); break;
                    case "biglietto_scarpa": MascheraBigliettoScarpa(m, t); cornice = false; break;
                    case "biglietto_mela": MascheraBigliettoMela(m, t); cornice = false; break;
                    case "biglietto_treno": MascheraBigliettoTreno(m, t); cornice = false; break;
                    case "lista": MascheraLista(m, t); cornice = false; break;
                    default: Debug.LogWarning("[Segreti] tipo di texture sconosciuto: " + t.tipo); continue;
                }
                ScriviPngSegreti(t, m, cornice);
                n++;
            }
            AssetDatabase.Refresh();
            Debug.Log("[Segreti] Texture dei pezzi generate: " + n + " in " + CartellaTextureSegreti);
        }

        static void ScriviPngSegreti(TestoAmbiente t, float[] mask, bool cornice)
        {
            int w = t.w, h = t.h, sd = t.seed;
            var carta = new Color(t.carta[0], t.carta[1], t.carta[2]);
            var ink = new Color(t.inchiostro[0], t.inchiostro[1], t.inchiostro[2]);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1), v = 1f - y / (float)(h - 1);
                    float n = ValueNoise(u * 9f + sd, v * 12f + sd) * 0.5f + ValueNoise(u * 40f, v * 40f + sd) * 0.5f;
                    float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 14f);
                    Color bg = carta * (0.78f + 0.22f * n) * (0.65f + 0.35f * edge);
                    float stain = Mathf.Clamp01(ValueNoise(u * 3f + 7f * sd, v * 3f) - 0.62f) * 1.8f;
                    bg *= 1f - stain * 0.35f;
                    float dBordo = Mathf.Min(Mathf.Min(u, 1f - u) * w, Mathf.Min(v, 1f - v) * h);
                    float k = mask[y * w + x] * (0.75f + 0.25f * ValueNoise(u * 60f, v * 60f));
                    if (cornice && dBordo < 5f && dBordo > 2f) k = Mathf.Max(k, 0.9f);
                    Color c = Color.Lerp(bg, ink * (0.8f + 0.2f * n), k);
                    c.a = 1f;
                    tex.SetPixel(x, h - 1 - y, c);
                }
            tex.Apply();
            string path = CartellaTextureSegreti + "/" + t.chiave.ToLowerInvariant() + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void Pieno(float[] m, int w, int h, int x0, int y0, int x1, int y1, float val)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++) m[y * w + x] = Mathf.Max(m[y * w + x], val);
        }

        static void TestoSx(float[] m, int w, int h, string s, int x, int y, int px)
        {
            // Stampa centra sul punto: si sposta di meta' larghezza
            Stampa(m, w, h, s, x + LarghezzaTesto(s) * px / 2, y, px);
        }

        static void MascheraRitaglio(float[] m, TestoAmbiente t)
        {
            int w = t.w, h = t.h;
            Stampa(m, w, h, t.righe[0], w / 2, 14, 2);
            Pieno(m, w, h, 14, 36, w - 15, 37, 0.9f);               // filetto sotto il titolo
            Stampa(m, w, h, t.righe[1], w / 2, 44, 1);
            Pieno(m, w, h, 14, 56, w - 15, 56, 0.6f);
            // due colonne di testo piccolo: le righe dopo la seconda, meta' a sinistra e meta' a destra
            int corpo = t.righe.Length - 2, sx = (corpo + 1) / 2;
            for (int i = 0; i < corpo; i++)
            {
                int col = i < sx ? 0 : 1, riga = i < sx ? i : i - sx;
                int cx = col == 0 ? w / 4 + 2 : 3 * w / 4 - 2;
                Stampa(m, w, h, t.righe[i + 2], cx, 68 + riga * 11, 1);
            }
            Pieno(m, w, h, w / 2, 64, w / 2, 64 + sx * 11 + 4, 0.5f);   // filetto fra le colonne
            // un riquadro con un disegno a tratti (il treno) in basso
            Pieno(m, w, h, 14, 64 + sx * 11 + 14, w - 15, 64 + sx * 11 + 14, 0.6f);
            int by = 64 + sx * 11 + 22;
            Pieno(m, w, h, 40, by + 20, 120, by + 30, 0.75f); Pieno(m, w, h, 100, by + 4, 112, by + 20, 0.75f);
            for (int k = 0; k < 4; k++) Pieno(m, w, h, 126 + k * 22, by + 14, 142 + k * 22, by + 30, 0.55f);
            Pieno(m, w, h, 40, by + 31, 220, by + 32, 0.8f);
        }

        static void MascheraCartolina(float[] m, TestoAmbiente t)
        {
            int w = t.w, h = t.h;
            int mx = 12, my = 12, hor = my + (h - 2 * my) * 58 / 100;
            // sole
            CerchioTratto(m, w, h, w * 0.78f, my + 26, 10, 1.6f, t.seed);
            // Vesuvio: profilo pieno con la sella fra il Somma e il cono, fumo leggero
            for (int x = mx; x < w - mx; x++)
            {
                float u = (x - mx) / (float)(w - 2 * mx);
                float cono = Mathf.Max(0f, 1f - Mathf.Abs(u - 0.42f) / 0.2f);          // cono
                float somma = Mathf.Max(0f, 1f - Mathf.Abs(u - 0.62f) / 0.25f) * 0.55f; // fianco piu' basso
                float alt = Mathf.Max(cono * 0.62f, somma * 0.62f);
                int ytop = hor - Mathf.RoundToInt(alt * (hor - my - 18));
                for (int y = ytop; y < hor; y++) m[y * w + x] = Mathf.Max(m[y * w + x], y < ytop + 3 ? 0.9f : 0.6f);
            }
            for (int i = 0; i < 5; i++) CerchioTratto(m, w, h, w * 0.43f + i * 5 - 6, my + 24 - i * 4, 3 + i, 1f, t.seed + i);
            // mare: tratti orizzontali sempre piu' radi
            for (int y = hor + 4; y < h - my - 18; y += 5)
                for (int x = mx + ((y / 5) % 2) * 6; x < w - mx - 8; x += 16) Pieno(m, w, h, x, y, x + 8, y, 0.45f);
            Pieno(m, w, h, mx, hor, w - mx, hor, 0.9f);
            // un tratto di barca
            Pieno(m, w, h, 60, hor + 14, 82, hor + 16, 0.9f); Pieno(m, w, h, 70, hor + 4, 71, hor + 14, 0.9f); Pieno(m, w, h, 71, hor + 5, 80, hor + 13, 0.5f);
            Stampa(m, w, h, t.righe[0], w / 2, h - my - 12, 1);
        }

        static void MascheraAmLire(float[] m, TestoAmbiente t)
        {
            int w = t.w, h = t.h;
            // fregio a guilloche: due onde intrecciate nella fascia del bordo
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Min(x, w - 1 - x), dy = Mathf.Min(y, h - 1 - y);
                    float d = Mathf.Min(dx, dy);
                    if (d >= 4 && d < 18)
                    {
                        float a = Mathf.Sin(x * 0.55f + 3f * Mathf.Sin(y * 0.31f)), b = Mathf.Sin(y * 0.55f + 3f * Mathf.Sin(x * 0.31f));
                        float f = Mathf.Min(dx < dy ? Mathf.Abs(b) : Mathf.Abs(a), 1f);
                        if (f < 0.16f) m[y * w + x] = 0.6f;
                    }
                    if (d == 2 || d == 19) m[y * w + x] = 0.85f;
                    // rosone a sinistra: cerchi concentrici con battimento
                    float rx = x - 56f, ry = y - h * 0.5f, r = Mathf.Sqrt(rx * rx + ry * ry);
                    if (r < 34f && Mathf.Abs(Mathf.Sin(r * 0.9f + Mathf.Atan2(ry, rx) * 6f)) < 0.14f) m[y * w + x] = Mathf.Max(m[y * w + x], 0.55f);
                    if (r > 33f && r < 35f) m[y * w + x] = 0.85f;
                }
            // sigla e dicitura: nessuna cifra
            Stampa(m, w, h, t.righe[0], 168, 40, 3);
            Stampa(m, w, h, t.righe[1], 156, 74, 1);
            Pieno(m, w, h, 100, 66, 224, 66, 0.7f);
        }

        static void MascheraSettebello(float[] m, TestoAmbiente t)
        {
            int w = t.w, h = t.h, s = t.seed;
            var posti = new[] { new Vector2(30, 40), new Vector2(66, 40), new Vector2(24, 84), new Vector2(48, 84), new Vector2(72, 84), new Vector2(30, 128), new Vector2(66, 128) };
            for (int i = 0; i < posti.Length; i++)
            {
                CerchioTratto(m, w, h, posti[i].x, posti[i].y, 10, 1.5f, s + i * 3);
                CerchioTratto(m, w, h, posti[i].x, posti[i].y, 5, 1.2f, s + i * 3 + 1);
            }
            TestoSx(m, w, h, t.righe[0], 8, 10, 2);
            TestoSx(m, w, h, t.righe[0], w - 18, h - 24, 2);
        }

        static void MascheraBigliettoScarpa(float[] m, TestoAmbiente t)
        {
            int w = t.w, s = t.seed; float k = 1.8f;
            Tratto(m, w, t.h, 20, 66, 104, 66, k, s); Tratto(m, w, t.h, 20, 66, 22, 38, k, s + 1);
            Tratto(m, w, t.h, 22, 38, 50, 34, k, s + 2); Tratto(m, w, t.h, 50, 34, 64, 48, k, s + 3);
            Tratto(m, w, t.h, 64, 48, 104, 58, k, s + 4); Tratto(m, w, t.h, 104, 58, 104, 66, k, s + 5);
            for (int i = 0; i < 3; i++) Tratto(m, w, t.h, 54 + i * 5, 38 + i * 4, 62 + i * 5, 34 + i * 4, k * 0.8f, s + 6 + i);
            Tratto(m, w, t.h, 14, 72, 112, 72, k * 0.7f, s + 10);
        }

        static void MascheraBigliettoMela(float[] m, TestoAmbiente t)
        {
            int w = t.w, s = t.seed; float k = 1.8f;
            CerchioTratto(m, w, t.h, 64, 54, 24, k, s);
            Tratto(m, w, t.h, 64, 30, 66, 20, k, s + 1);
            Tratto(m, w, t.h, 66, 22, 82, 16, k, s + 2); Tratto(m, w, t.h, 82, 16, 74, 28, k, s + 3); Tratto(m, w, t.h, 74, 28, 66, 22, k, s + 4);
            Tratto(m, w, t.h, 52, 44, 56, 40, k * 0.8f, s + 5);
        }

        static void MascheraBigliettoTreno(float[] m, TestoAmbiente t)
        {
            int w = t.w, s = t.seed; float k = 1.8f;
            Tratto(m, w, t.h, 18, 66, 18, 42, k, s); Tratto(m, w, t.h, 18, 42, 92, 42, k, s + 1); Tratto(m, w, t.h, 92, 42, 92, 66, k, s + 2);
            Tratto(m, w, t.h, 18, 56, 92, 56, k, s + 3); Tratto(m, w, t.h, 30, 42, 30, 30, k, s + 4); Tratto(m, w, t.h, 26, 30, 35, 30, k, s + 5);
            Tratto(m, w, t.h, 92, 48, 108, 48, k, s + 6); Tratto(m, w, t.h, 92, 66, 18, 66, k, s + 7);
            CerchioTratto(m, w, t.h, 34, 72, 6, k, s + 8); CerchioTratto(m, w, t.h, 76, 72, 6, k, s + 9);
            CerchioTratto(m, w, t.h, 36, 20, 4, k * 0.8f, s + 10); CerchioTratto(m, w, t.h, 46, 12, 6, k * 0.8f, s + 11);
        }

        static void MascheraLista(float[] m, TestoAmbiente t)
        {
            int w = t.w, h = t.h;
            for (int i = 0; i < t.righe.Length; i++) Stampa(m, w, h, t.righe[i], w / 2, 14 + i * 13, 1);
            for (int i = 0; i < t.righe.Length; i++) Pieno(m, w, h, 10, 24 + i * 13, w - 11, 24 + i * 13, 0.18f);
        }
    }
}
