using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 2: manifesti e insegne d'epoca del quartiere. I testi sono originali (nessun marchio, partito o nome reale) e vengono
    /// disegnati su texture con un piccolo carattere a matrice 5x7 definito qui: nessun font e nessun asset esterno.
    /// </summary>
    public static partial class SceneBuilder
    {
        // glifi 5x7, righe dall'alto; '#' = pieno
        static readonly Dictionary<char, string[]> Glifi = new Dictionary<char, string[]>
        {
            {'A', new[]{".###.","#...#","#...#","#####","#...#","#...#","#...#"}},
            {'B', new[]{"####.","#...#","#...#","####.","#...#","#...#","####."}},
            {'C', new[]{".####","#....","#....","#....","#....","#....",".####"}},
            {'D', new[]{"####.","#...#","#...#","#...#","#...#","#...#","####."}},
            {'E', new[]{"#####","#....","#....","####.","#....","#....","#####"}},
            {'F', new[]{"#####","#....","#....","####.","#....","#....","#...."}},
            {'G', new[]{".####","#....","#....","#..##","#...#","#...#",".###."}},
            {'H', new[]{"#...#","#...#","#...#","#####","#...#","#...#","#...#"}},
            {'I', new[]{"#####","..#..","..#..","..#..","..#..","..#..","#####"}},
            {'J', new[]{"..###","...#.","...#.","...#.","...#.","#..#.",".##.."}},
            {'K', new[]{"#...#","#..#.","#.#..","##...","#.#..","#..#.","#...#"}},
            {'L', new[]{"#....","#....","#....","#....","#....","#....","#####"}},
            {'M', new[]{"#...#","##.##","#.#.#","#.#.#","#...#","#...#","#...#"}},
            {'N', new[]{"#...#","##..#","#.#.#","#..##","#...#","#...#","#...#"}},
            {'O', new[]{".###.","#...#","#...#","#...#","#...#","#...#",".###."}},
            {'P', new[]{"####.","#...#","#...#","####.","#....","#....","#...."}},
            {'Q', new[]{".###.","#...#","#...#","#...#","#.#.#","#..#.",".##.#"}},
            {'R', new[]{"####.","#...#","#...#","####.","#.#..","#..#.","#...#"}},
            {'S', new[]{".####","#....","#....",".###.","....#","....#","####."}},
            {'T', new[]{"#####","..#..","..#..","..#..","..#..","..#..","..#.."}},
            {'U', new[]{"#...#","#...#","#...#","#...#","#...#","#...#",".###."}},
            {'V', new[]{"#...#","#...#","#...#","#...#","#...#",".#.#.","..#.."}},
            {'W', new[]{"#...#","#...#","#...#","#.#.#","#.#.#","##.##","#...#"}},
            {'X', new[]{"#...#","#...#",".#.#.","..#..",".#.#.","#...#","#...#"}},
            {'Y', new[]{"#...#","#...#",".#.#.","..#..","..#..","..#..","..#.."}},
            {'Z', new[]{"#####","....#","...#.","..#..",".#...","#....","#####"}},
            {'0', new[]{".###.","#...#","#..##","#.#.#","##..#","#...#",".###."}},
            {'1', new[]{"..#..",".##..","..#..","..#..","..#..","..#..",".###."}},
            {'2', new[]{".###.","#...#","....#","...#.","..#..",".#...","#####"}},
            {'3', new[]{"####.","....#","....#",".###.","....#","....#","####."}},
            {'4', new[]{"...#.","..##.",".#.#.","#..#.","#####","...#.","...#."}},
            {'5', new[]{"#####","#....","####.","....#","....#","#...#",".###."}},
            {'6', new[]{".###.","#....","#....","####.","#...#","#...#",".###."}},
            {'7', new[]{"#####","....#","...#.","..#..",".#...",".#...",".#..."}},
            {'8', new[]{".###.","#...#","#...#",".###.","#...#","#...#",".###."}},
            {'9', new[]{".###.","#...#","#...#",".####","....#","....#",".###."}},
            {'.', new[]{".....",".....",".....",".....",".....",".....","..#.."}},
            {',', new[]{".....",".....",".....",".....","..#..","..#..",".#..."}},
            {'\'', new[]{"..#..","..#..",".#...",".....",".....",".....","....."}},
            {'-', new[]{".....",".....",".....",".###.",".....",".....","....."}},
            {'!', new[]{"..#..","..#..","..#..","..#..","..#..",".....","..#.."}},
            {'?', new[]{".###.","#...#","....#","...#.","..#..",".....","..#.."}},
            {':', new[]{".....","..#..",".....",".....","..#..",".....","....."}},
            {'/', new[]{"....#","....#","...#.","..#..",".#...","#....","#...."}},
            {' ', new[]{".....",".....",".....",".....",".....",".....","....."}},
        };

        static int LarghezzaTesto(string t) { return t.Length * 6 - 1; }

        static void Stampa(float[] m, int w, int h, string t, int cx, int yTop, int px)
        {
            int tw = LarghezzaTesto(t) * px;
            int x0 = cx - tw / 2;
            for (int gi = 0; gi < t.Length; gi++)
            {
                string[] g;
                if (!Glifi.TryGetValue(char.ToUpperInvariant(t[gi]), out g)) g = Glifi[' '];
                for (int rr = 0; rr < 7; rr++)
                    for (int cc = 0; cc < 5; cc++)
                    {
                        if (g[rr][cc] != '#') continue;
                        for (int dy = 0; dy < px; dy++)
                            for (int dx = 0; dx < px; dx++)
                            {
                                int x = x0 + (gi * 6 + cc) * px + dx, y = yTop + rr * px + dy;
                                if (x >= 0 && x < w && y >= 0 && y < h) m[y * w + x] = 1f;
                            }
                    }
            }
        }

        static Material MaterialeTesto(string key, int w, int h, string[] righe, Color carta, Color inchiostro, bool manifesto, int seed)
        {
            var mask = new float[w * h];
            int margin = manifesto ? w / 12 : h / 6;
            // altezza di riga proporzionale: la prima riga e' il titolo
            int y = manifesto ? h / 12 : 0;
            if (manifesto)
            {
                for (int i = 0; i < righe.Length; i++)
                {
                    int px = Mathf.Clamp((w - 2 * margin) / LarghezzaTesto(righe[i]), 1, i == 0 ? 7 : 4);
                    Stampa(mask, w, h, righe[i], w / 2, y, px);
                    y += 9 * px + (i == 0 ? px * 2 : px);
                }
            }
            else
            {
                int px = Mathf.Max(1, Mathf.Min((w - 2 * margin) / LarghezzaTesto(righe[0]), (h - 2 * margin) / 7));
                Stampa(mask, w, h, righe[0], w / 2, (h - 7 * px) / 2, px);
            }
            return MaterialeDaMaschera(key, w, h, mask, carta, inchiostro, manifesto, seed, true, manifesto);
        }

        /// <summary>Carta invecchiata con macchie e bordi scuri, inchiostro dove la maschera vale 1. Usata da testi e disegni.</summary>
        static Material MaterialeDaMaschera(string key, int w, int h, float[] mask, Color carta, Color inchiostro, bool manifesto, int seed, bool cornice, bool piega)
        {
            int sd = seed;
            var tex = SaveGeneratedTexture((manifesto ? "T_Man_" : "T_Ins_") + key, w, h, (u, v) =>
            {
                int x = Mathf.RoundToInt(u * (w - 1)), yy = Mathf.RoundToInt((1f - v) * (h - 1));
                float n = ValueNoise(u * 9f + sd, v * 12f + sd) * 0.5f + ValueNoise(u * 40f, v * 40f + sd) * 0.5f;
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 14f);
                Color bg = carta * (0.78f + 0.22f * n) * (0.65f + 0.35f * edge);
                // macchie e piega
                float stain = Mathf.Clamp01(ValueNoise(u * 3f + 7f * sd, v * 3f) - 0.62f) * 1.8f;
                bg *= 1f - stain * 0.35f;
                if (piega && Mathf.Abs(v - 0.5f) < 0.004f) bg *= 0.82f;
                // cornice
                float dBordo = Mathf.Min(Mathf.Min(u, 1f - u) * w, Mathf.Min(v, 1f - v) * h);
                bool incorniciato = cornice && dBordo < (manifesto ? 7f : 5f) && dBordo > (manifesto ? 3f : 2f);
                float ink = mask[yy * w + x];
                // l'inchiostro si consuma a macchie
                ink *= 0.75f + 0.25f * ValueNoise(u * 60f, v * 60f);
                if (incorniciato) ink = Mathf.Max(ink, 0.9f);
                Color c = Color.Lerp(bg, inchiostro * (0.8f + 0.2f * n), ink);
                c.a = 1f;
                return c;
            });
            string path = "Assets/Art/Materials/MP_" + key + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            matCache["MP_" + key] = m;
            return m;
        }

        // ------------------------------------------------------------------ disegno di bambino (nessuna scritta)

        /// <summary>Segno a carboncino: dischi sovrapposti lungo il segmento, con un lieve tremito della mano.</summary>
        static void Tratto(float[] m, int w, int h, float x0, float y0, float x1, float y1, float spessore, int seed)
        {
            float len = Mathf.Max(1f, Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));
            int passi = Mathf.CeilToInt(len * 1.5f);
            for (int i = 0; i <= passi; i++)
            {
                float t = i / (float)passi;
                float cx = Mathf.Lerp(x0, x1, t) + (ValueNoise(t * 7f + seed, seed * 0.37f) - 0.5f) * 1.6f;
                float cy = Mathf.Lerp(y0, y1, t) + (ValueNoise(t * 7f + seed + 50f, seed * 0.71f) - 0.5f) * 1.6f;
                float r = spessore * (0.8f + 0.4f * ValueNoise(t * 13f + seed, 3f));
                int ix0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), ix1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r));
                int iy0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), iy1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r));
                for (int yy = iy0; yy <= iy1; yy++)
                    for (int xx = ix0; xx <= ix1; xx++)
                    {
                        float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                        if (d <= r) m[yy * w + xx] = Mathf.Max(m[yy * w + xx], 1f - 0.4f * d / r);
                    }
            }
        }

        static void CerchioTratto(float[] m, int w, int h, float cx, float cy, float r, float spessore, int seed)
        {
            int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Tratto(m, w, h, cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, spessore, seed + i);
            }
        }

        /// <summary>Il biglietto "Ricordo" del basso: un disegno di bambino a carboncino su carta povera (casetta, sole, treno col fumo), senza scritte.</summary>
        static Material MaterialeDisegnoBambino(string key, int w, int h, int seed)
        {
            var m = new float[w * h];
            float t = 1.6f;
            // sole in alto a destra, con i raggi
            CerchioTratto(m, w, h, 128, 24, 9, t, seed);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Tratto(m, w, h, 128 + Mathf.Cos(a) * 13, 24 + Mathf.Sin(a) * 13, 128 + Mathf.Cos(a) * 19, 24 + Mathf.Sin(a) * 19, t, seed + 40 + i);
            }
            // casetta a sinistra: muri, tetto, porta, finestra
            Tratto(m, w, h, 14, 90, 14, 55, t, seed + 1); Tratto(m, w, h, 14, 55, 52, 55, t, seed + 2);
            Tratto(m, w, h, 52, 55, 52, 90, t, seed + 3); Tratto(m, w, h, 10, 55, 33, 32, t, seed + 4);
            Tratto(m, w, h, 33, 32, 56, 55, t, seed + 5); Tratto(m, w, h, 14, 90, 52, 90, t, seed + 6);
            Tratto(m, w, h, 28, 90, 28, 70, t, seed + 7); Tratto(m, w, h, 28, 70, 38, 70, t, seed + 8); Tratto(m, w, h, 38, 70, 38, 90, t, seed + 9);
            Tratto(m, w, h, 42, 62, 48, 62, t, seed + 10); Tratto(m, w, h, 42, 62, 42, 68, t, seed + 11);
            // terra
            Tratto(m, w, h, 4, 96, 156, 98, t, seed + 12);
            // treno a destra: locomotiva, ciminiera, ruote, fumo
            Tratto(m, w, h, 78, 92, 78, 68, t, seed + 13); Tratto(m, w, h, 78, 68, 140, 68, t, seed + 14);
            Tratto(m, w, h, 140, 68, 140, 92, t, seed + 15); Tratto(m, w, h, 78, 82, 140, 82, t, seed + 16);
            Tratto(m, w, h, 92, 68, 92, 56, t, seed + 17); Tratto(m, w, h, 88, 56, 97, 56, t, seed + 18);
            CerchioTratto(m, w, h, 90, 94, 5, t, seed + 19); CerchioTratto(m, w, h, 130, 94, 5, t, seed + 20);
            CerchioTratto(m, w, h, 96, 46, 4, t * 0.8f, seed + 21); CerchioTratto(m, w, h, 104, 38, 6, t * 0.8f, seed + 22);
            Tratto(m, w, h, 140, 75, 152, 75, t, seed + 23);
            // il carboncino e' grigio scuro, la carta povera e' grigiastra
            return MaterialeDaMaschera(key, w, h, m, new Color(0.8f, 0.74f, 0.6f), new Color(0.13f, 0.12f, 0.11f), true, seed, false, false);
        }

        // ------------------------------------------------------------------ testi d'ambiente da JSON

        [Serializable]
        sealed class TestoAmbiente
        {
            public string chiave, gruppo, tipo; // tipo: manifesto | insegna
            public int w, h, seed;
            public string[] righe;
            public float[] carta, inchiostro;
        }

        [Serializable]
        sealed class TestiAmbienteFile { public TestoAmbiente[] testi; }

        const string PercorsoTestiAmbiente = "Assets/_Game/Resources/Data/testi_ambiente.json";

        /// <summary>
        /// Crea (o aggiorna) texture e materiali dei testi d'ambiente (manifesti, avvisi, graffiti, santini, calendario, insegne) di un gruppo
        /// di <c>testi_ambiente.json</c>: i testi narrativi stanno nel JSON, non nel codice. Le chiavi diventano materiali "MP_&lt;chiave&gt;".
        /// </summary>
        static void PreparaTestiAmbiente(string gruppo)
        {
            var file = JsonUtility.FromJson<TestiAmbienteFile>(System.IO.File.ReadAllText(PercorsoTestiAmbiente));
            int n = 0;
            foreach (var t in file.testi)
            {
                if (t.gruppo != gruppo) continue;
                var carta = new Color(t.carta[0], t.carta[1], t.carta[2]);
                var ink = new Color(t.inchiostro[0], t.inchiostro[1], t.inchiostro[2]);
                MaterialeTesto(t.chiave, t.w, t.h, t.righe, carta, ink, t.tipo == "manifesto", t.seed);
                n++;
            }
            if (n == 0) Debug.LogWarning("[SceneBuilder] Nessun testo d'ambiente per il gruppo '" + gruppo + "' in " + PercorsoTestiAmbiente);
        }

        static GameObject Manifesto(Transform env, string key, Vector3 pos, Vector3 n, float larg, float alt, string nome)
        {
            var go = Box(nome, env, pos + n * (0.0075f + 0.004f), new Vector3(larg, alt, 0.015f), "MP_" + key,
                Quaternion.LookRotation(n, Vector3.up).eulerAngles, false);
            return go;
        }

        /// <summary>Materiali di manifesti e insegne del quartiere (testi in testi_ambiente.json, gruppo "quartiere"): da chiamare prima di costruire le facciate (le insegne delle porte li usano).</summary>
        static void PreparaMaterialiTesti()
        {
            PreparaTestiAmbiente("quartiere");
        }

        static void TestiEManifesti(Transform env, System.Random r)
        {
            // insegne sopra i bassi aperti
            string[] nomi = { "Salumeria", "Barbiere", "Calzolaio", "Latteria", "Ferramenta", "Trattoria" };
            int bi = 0;
            foreach (var f in feats)
            {
                if (f.tipo != "basso") continue;
                var n = f.n;
                string key = f.interno == 2 && bi == 0 ? "Salumeria" : nomi[bi % nomi.Length];
                var go = Box("Insegna_Bottega_" + key, env, f.wp + n * 0.05f + Vector3.up * 2.77f, new Vector3(1.7f, 0.25f, 0.04f), "MP_" + key,
                    Quaternion.LookRotation(n, Vector3.up).eulerAngles, false);
                bi++;
            }

            Vector3 up = Vector3.up;
            int muriManifesti = 0; // i manifesti stanno su muri ciechi a coordinate fisse: se il muro non c'e' lo si segnala
            foreach (var f in feats)
            {
                if (f.tipo != "cieco") continue;
                Vector3 n = f.n, al = Vector3.Cross(n, up);
                if (Mathf.Abs(f.wp.x + 7f) < 0.1f && Mathf.Abs(f.wp.z + 28f) < 0.1f)
                {   // muro della bancarella, piazzetta della fontanella
                    muriManifesti |= 1;
                    Manifesto(env, "Ricostruiamo", f.wp + al * -1.9f + up * 1.7f, n, 0.62f, 0.93f, "Manifesto_Ricostruiamo");
                    Manifesto(env, "Acqua", f.wp + al * 1.9f + up * 1.7f, n, 0.62f, 0.93f, "Manifesto_Acqua");
                }
                else if (Mathf.Abs(f.wp.x - 14f) < 0.1f && Mathf.Abs(f.wp.z + 10f) < 0.1f)
                {   // bacheca con quattro manifesti
                    muriManifesti |= 2;
                    var tavola = Box("Cartello_Bacheca", env, f.wp + n * 0.03f + up * 1.75f, new Vector3(2.3f, 1.3f, 0.05f), "M_LegnoVernicePelata", Quaternion.LookRotation(n, up).eulerAngles, false);
                    string[] k4 = { "Viveri", "Lavoro", "Festa", "Scuola" };
                    for (int i = 0; i < 4; i++)
                        Manifesto(env, k4[i], f.wp + al * (-0.85f + i * 0.57f) + up * 1.75f + n * 0.04f, n, 0.5f, 0.78f, "Manifesto_" + k4[i]);
                }
                else if (Mathf.Abs(f.wp.x + 29f) < 0.1f && Mathf.Abs(f.wp.z + 30f) < 0.1f)
                {
                    muriManifesti |= 4;
                    Manifesto(env, "Affitto", f.wp + al * 0.0f + up * 1.9f, n, 0.55f, 0.82f, "Manifesto_Affitto");
                }
                else if (Mathf.Abs(f.wp.x + 14f) < 0.1f && Mathf.Abs(f.wp.z - 8f) < 0.1f || Mathf.Abs(f.wp.x - 12f) < 0.1f && Mathf.Abs(f.wp.z - 14f) < 0.1f)
                {   // edicole votive piccole
                    Shrine(env, f.wp, f.n);
                }
            }
            if ((muriManifesti & 1) == 0) Debug.LogWarning("[Quartiere] Muro cieco (-7, -28) non trovato: niente manifesti Ricostruiamo/Acqua.");
            if ((muriManifesti & 2) == 0) Debug.LogWarning("[Quartiere] Muro cieco (14, -10) non trovato: niente bacheca dei manifesti.");
            if ((muriManifesti & 4) == 0) Debug.LogWarning("[Quartiere] Muro cieco (-29, -30) non trovato: niente manifesto Affitto.");
            // cartello di pericolo nel lotto bombardato
            var palo = Box("Cartello_Palo", env, PT(15.6f, -14.9f, 0.8f), new Vector3(0.08f, 1.6f, 0.08f), "M_LegnoVernicePelata", new Vector3(0f, 0f, 0f), true);
            Mob(palo);
            Manifesto(env, "Crollo", PT(15.6f, -14.9f, 1.25f) + new Vector3(0f, 0f, -0.045f), Vector3.back, 0.5f, 0.75f, "Cartello_Crollo");
        }
    }
}
