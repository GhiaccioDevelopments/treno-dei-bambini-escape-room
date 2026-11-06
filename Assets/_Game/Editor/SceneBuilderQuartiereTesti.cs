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
                if (manifesto && Mathf.Abs(v - 0.5f) < 0.004f) bg *= 0.82f;
                // cornice
                bool cornice = (Mathf.Min(Mathf.Min(u, 1f - u) * w, Mathf.Min(v, 1f - v) * h) < (manifesto ? 7f : 5f)) && (Mathf.Min(Mathf.Min(u, 1f - u) * w, Mathf.Min(v, 1f - v) * h) > (manifesto ? 3f : 2f));
                float ink = mask[yy * w + x];
                // l'inchiostro si consuma a macchie
                ink *= 0.75f + 0.25f * ValueNoise(u * 60f, v * 60f);
                if (cornice) ink = Mathf.Max(ink, 0.9f);
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

        static GameObject Manifesto(Transform env, string key, Vector3 pos, Vector3 n, float larg, float alt, string nome)
        {
            var go = Box(nome, env, pos + n * (0.0075f + 0.004f), new Vector3(larg, alt, 0.015f), "MP_" + key,
                Quaternion.LookRotation(n, Vector3.up).eulerAngles, false);
            return go;
        }

        /// <summary>Crea (o aggiorna) texture e materiali di manifesti e insegne: da chiamare prima di costruire le facciate (le insegne delle porte li usano).</summary>
        static void PreparaMaterialiTesti()
        {
            // manifesti (carta ingiallita, inchiostro nero o rosso scuro): testi originali
            var man = new Dictionary<string, string[]>
            {
                { "Ricostruiamo", new[] { "RICOSTRUIAMO", "INSIEME", "LA NOSTRA CITTA", "", "ASSEMBLEA IN PIAZZA", "SABATO ORE 16" } },
                { "Acqua", new[] { "AVVISO", "ACQUA ALLA", "FONTANELLA", "", "DALLE 6 ALLE 10", "PORTARE I SECCHI" } },
                { "Viveri", new[] { "AVVISO", "DISTRIBUZIONE", "DEI VIVERI", "", "OGNI MARTEDI", "PORTARE LA TESSERA" } },
                { "Affitto", new[] { "SI AFFITTA", "BASSO", "", "RIVOLGERSI", "AL PORTIERE", "DI SERA" } },
                { "Scuola", new[] { "PER LA SCUOLA", "PER I BAMBINI", "", "OFFERTE AL", "COMITATO DI VIA", "GRAZIE" } },
                { "Lavoro", new[] { "CERCASI", "MANOVALI", "", "PER LA", "RICOSTRUZIONE", "PAGA A GIORNATA" } },
                { "Festa", new[] { "FESTA DI", "QUARTIERE", "", "SABATO SERA", "MUSICA E BALLI", "PER TUTTI" } },
                { "Crollo", new[] { "PERICOLO", "CROLLO", "", "NON ENTRARE", "VIA LIBERA", "SOLO AI TECNICI" } },
            };
            int s = 3;
            foreach (var kv in man)
            {
                bool rosso = kv.Key == "Crollo" || kv.Key == "Lavoro";
                MaterialeTesto(kv.Key, 256, 384, kv.Value, new Color(0.88f, 0.8f, 0.62f), rosso ? new Color(0.45f, 0.08f, 0.05f) : new Color(0.1f, 0.08f, 0.06f), true, s++);
            }
            // insegne delle botteghe (tavole dipinte, lettere color crema)
            var ins = new Dictionary<string, string>
            {
                { "Salumeria", "SALUMERIA" }, { "Barbiere", "BARBIERE" }, { "Calzolaio", "CALZOLAIO" },
                { "Latteria", "LATTERIA" }, { "Ferramenta", "FERRAMENTA" }, { "Trattoria", "TRATTORIA" }, { "Sarto", "SARTO" }, { "Fruttivendolo", "FRUTTA E VERDURA" }
            };
            var colIns = new Dictionary<string, Color>
            {
                { "Salumeria", new Color(0.38f, 0.1f, 0.08f) }, { "Barbiere", new Color(0.12f, 0.22f, 0.3f) }, { "Calzolaio", new Color(0.22f, 0.15f, 0.08f) },
                { "Latteria", new Color(0.18f, 0.3f, 0.24f) }, { "Ferramenta", new Color(0.16f, 0.16f, 0.17f) }, { "Trattoria", new Color(0.4f, 0.22f, 0.08f) }, { "Sarto", new Color(0.2f, 0.2f, 0.3f) }, { "Fruttivendolo", new Color(0.22f, 0.3f, 0.14f) }
            };
            foreach (var kv in ins)
                MaterialeTesto(kv.Key, 512, 96, new[] { kv.Value }, colIns[kv.Key] * 1.0f, new Color(0.9f, 0.82f, 0.6f), false, s++);
            // insegne dei luoghi di luoghi.json
            MaterialeTesto("Stazione", 512, 96, new[] { "STAZIONE" }, new Color(0.16f, 0.2f, 0.17f), new Color(0.92f, 0.86f, 0.68f), false, s++);
            MaterialeTesto("Comitato", 512, 96, new[] { "SEDE DEL COMITATO" }, new Color(0.38f, 0.1f, 0.08f), new Color(0.92f, 0.86f, 0.68f), false, s++);
            MaterialeTesto("CapaFierro", 512, 96, new[] { "CAPA E FIERRO" }, new Color(0.12f, 0.22f, 0.3f), new Color(0.92f, 0.86f, 0.68f), false, s++);
            MaterialeTesto("Stracci", 512, 96, new[] { "VICOLO DEGLI STRACCI" }, new Color(0.22f, 0.15f, 0.08f), new Color(0.92f, 0.86f, 0.68f), false, s++);
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
            foreach (var f in feats)
            {
                if (f.tipo != "cieco") continue;
                Vector3 n = f.n, al = Vector3.Cross(n, up);
                if (Mathf.Abs(f.wp.x + 7f) < 0.1f && Mathf.Abs(f.wp.z + 28f) < 0.1f)
                {   // muro della bancarella, piazzetta della fontanella
                    Manifesto(env, "Ricostruiamo", f.wp + al * -1.9f + up * 1.7f, n, 0.62f, 0.93f, "Manifesto_Ricostruiamo");
                    Manifesto(env, "Acqua", f.wp + al * 1.9f + up * 1.7f, n, 0.62f, 0.93f, "Manifesto_Acqua");
                }
                else if (Mathf.Abs(f.wp.x - 14f) < 0.1f && Mathf.Abs(f.wp.z + 10f) < 0.1f)
                {   // bacheca con quattro manifesti
                    var tavola = Box("Cartello_Bacheca", env, f.wp + n * 0.03f + up * 1.75f, new Vector3(2.3f, 1.3f, 0.05f), "M_LegnoVernicePelata", Quaternion.LookRotation(n, up).eulerAngles, false);
                    string[] k4 = { "Viveri", "Lavoro", "Festa", "Scuola" };
                    for (int i = 0; i < 4; i++)
                        Manifesto(env, k4[i], f.wp + al * (-0.85f + i * 0.57f) + up * 1.75f + n * 0.04f, n, 0.5f, 0.78f, "Manifesto_" + k4[i]);
                }
                else if (Mathf.Abs(f.wp.x + 29f) < 0.1f && Mathf.Abs(f.wp.z + 30f) < 0.1f)
                {
                    Manifesto(env, "Affitto", f.wp + al * 0.0f + up * 1.9f, n, 0.55f, 0.82f, "Manifesto_Affitto");
                }
                else if (Mathf.Abs(f.wp.x + 14f) < 0.1f && Mathf.Abs(f.wp.z - 8f) < 0.1f || Mathf.Abs(f.wp.x - 12f) < 0.1f && Mathf.Abs(f.wp.z - 14f) < 0.1f)
                {   // edicole votive piccole
                    Shrine(env, f.wp, f.n);
                }
            }
            // cartello di pericolo nel lotto bombardato
            var palo = Box("Cartello_Palo", env, PT(15.6f, -14.9f, 0.8f), new Vector3(0.08f, 1.6f, 0.08f), "M_LegnoVernicePelata", new Vector3(0f, 0f, 0f), true);
            Mob(palo);
            Manifesto(env, "Crollo", PT(15.6f, -14.9f, 1.25f) + new Vector3(0f, 0f, -0.045f), Vector3.back, 0.5f, 0.75f, "Cartello_Crollo");
        }
    }
}
