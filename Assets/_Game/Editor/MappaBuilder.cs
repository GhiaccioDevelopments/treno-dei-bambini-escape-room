using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TrenoBambini.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R019: genera le piante per la minimappa (menu Treno ▸ Mappa ▸ Genera piante).
    /// Per ogni scena apre la scena, legge i collider (terreno, muri, arredi) dall'alto, li disegna come inchiostro su carta invecchiata
    /// (muri a tratteggio, contorni tremolanti, scalini a righe, glifi dei punti d'interesse da mappe.json) e salva un JPG in
    /// Resources/Mappe/&lt;scena&gt;.jpg, piu' la striscia dei glifi (Resources/Mappe/glifi.png). L'area coperta viene scritta in mappe.json.
    /// A runtime non c'e' nessuna camera in piu': la minimappa mostra soltanto questa immagine.
    /// </summary>
    public static class MappaBuilder
    {
        const string CartellaMappe = "Assets/_Game/Resources/Mappe";
        const string PercorsoJson = "Assets/_Game/Resources/Data/mappe.json";
        const int Ss = 2;            // supersampling
        public static readonly string[] OrdineGlifi = { "porta", "fontana", "edicola", "pozzo", "lavatoio", "uscita", "stazione", "bottega" };

        sealed class Cfg
        {
            public string scena, tema;
            public string[] terra = { "Selciato", "Pavimento" };
            public string[] muri = { "Casa", "Muro" };
            public bool arredo;     // disegna anche i mobili (interni)
            public float margine = 8f;
            public float ppm = 8f;
        }

        static readonly Cfg[] Scene =
        {
            new Cfg { scena = "10_Hub_Vicolo", tema = "normale", margine = 10f, ppm = 8f },
            new Cfg { scena = "20_Int_BassoAntonietta", tema = "normale", arredo = true, margine = 3f, ppm = 24f },
            new Cfg { scena = "30_FB_Scarpe", tema = "ricordo", margine = 10f, ppm = 8f },
        };

        struct Rett { public float x0, x1, z0, z1, top, bottom; public string nome; }

        [MenuItem("Treno/Mappa/Genera piante")]
        public static void GeneraMenu() { Genera(); }

        public static string Genera()
        {
            var log = new StringBuilder();
            string scenaIniziale = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Genera piante: la scena aperta ha modifiche non salvate. Salva e riprova.");
                return "scena sporca";
            }
            Directory.CreateDirectory(CartellaMappe);
            string json = File.ReadAllText(PercorsoJson, Encoding.UTF8);
            var mappe = JsonUtility.FromJson<MappeFile>(json.TrimStart('﻿')) ?? new MappeFile();

            try
            {
                foreach (var c in Scene)
                {
                    string path = "Assets/_Game/Scenes/" + c.scena + ".unity";
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    Physics.SyncTransforms();
                    var def = mappe.scene.Find(s => s.scena == c.scena);
                    var punti = def != null ? def.punti : new List<MappaPuntoDef>();
                    float x0, z0, x1, z1;
                    string r = Disegna(c, punti, out x0, out z0, out x1, out z1);
                    log.AppendLine(c.scena + ": " + r);
                    json = ScriviArea(json, c, x0, z0, x1, z1);
                }
                File.WriteAllText(PercorsoJson, json, new UTF8Encoding(false));
                ScriviGlifi();
            }
            finally
            {
                if (!string.IsNullOrEmpty(scenaIniziale)) EditorSceneManager.OpenScene(scenaIniziale, OpenSceneMode.Single);
            }
            AssetDatabase.Refresh();
            ImportaTexture();
            Debug.Log("[Mappa] Piante generate.\n" + log);
            return log.ToString();
        }

        // ---------------------------------------------------------------- lettura della scena
        static bool Inizia(string nome, string[] prefissi)
        {
            foreach (var p in prefissi) if (nome.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        static string Fmt(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        static string ScriviArea(string json, Cfg c, float x0, float z0, float x1, float z1)
        {
            int i = json.IndexOf("\"scena\": \"" + c.scena + "\"", StringComparison.Ordinal);
            if (i < 0) { Debug.LogWarning("[Mappa] mappe.json non ha la scena " + c.scena + ": area non scritta."); return json; }
            json = Sostituisci(json, i, "ax0", x0); json = Sostituisci(json, i, "az0", z0);
            json = Sostituisci(json, i, "ax1", x1); json = Sostituisci(json, i, "az1", z1);
            json = Sostituisci(json, i, "ppm", c.ppm);
            return json;
        }

        static string Sostituisci(string json, int da, string campo, float v)
        {
            var m = new Regex("\"" + campo + "\"\\s*:\\s*-?[0-9.eE+-]+").Match(json, da);
            if (!m.Success) { Debug.LogWarning("[Mappa] campo mancante in mappe.json: " + campo); return json; }
            return json.Substring(0, m.Index) + "\"" + campo + "\": " + Fmt(v) + json.Substring(m.Index + m.Length);
        }

        static string Disegna(Cfg c, List<MappaPuntoDef> punti, out float oax0, out float oaz0, out float oax1, out float oaz1)
        {
            float ax0, az0, ax1, az1;
            var terra = new List<Rett>(); var muri = new List<Rett>(); var scale = new List<Rett>(); var arredi = new List<Rett>();
            foreach (var col in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (col.isTrigger) continue;
                var b = col.bounds;
                var r = new Rett { x0 = b.min.x, x1 = b.max.x, z0 = b.min.z, z1 = b.max.z, top = b.max.y, bottom = b.min.y, nome = col.name };
                if (Inizia(col.name, c.terra))
                {
                    if (col.name.Contains("Scalinata")) scale.Add(r);
                    terra.Add(r);
                }
                else if (Inizia(col.name, c.muri)) muri.Add(r);
                else if (c.arredo) arredi.Add(r);
            }
            // i muri veri: tengono la base vicino al terreno (le architravi, i ponti e i soffitti non chiudono la strada)
            var muriVeri = new List<Rett>();
            foreach (var m in muri)
            {
                if (m.nome.Contains("Ponte") || m.nome.Contains("Soffitto") || m.nome.Contains("Vetro")) continue;
                float cx = (m.x0 + m.x1) * 0.5f, cz = (m.z0 + m.z1) * 0.5f;
                float base0 = 0f;
                foreach (var t in terra)
                    if (!t.nome.Contains("Scalinata") && cx >= t.x0 && cx <= t.x1 && cz >= t.z0 && cz <= t.z1) base0 = Mathf.Max(base0, t.top);
                if (m.bottom - base0 > 1.5f) continue;
                muriVeri.Add(m);
            }

            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var t in terra) { minX = Mathf.Min(minX, t.x0); maxX = Mathf.Max(maxX, t.x1); minZ = Mathf.Min(minZ, t.z0); maxZ = Mathf.Max(maxZ, t.z1); }
            foreach (var m in muriVeri) { minX = Mathf.Min(minX, m.x0); maxX = Mathf.Max(maxX, m.x1); minZ = Mathf.Min(minZ, m.z0); maxZ = Mathf.Max(maxZ, m.z1); }
            foreach (var p in punti) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            if (minX > maxX) { oax0 = oaz0 = oax1 = oaz1 = 0f; return "nessun collider trovato"; }
            ax0 = Mathf.Floor(minX - c.margine); az0 = Mathf.Floor(minZ - c.margine);
            ax1 = Mathf.Ceil(maxX + c.margine); az1 = Mathf.Ceil(maxZ + c.margine);
            oax0 = ax0; oaz0 = az0; oax1 = ax1; oaz1 = az1;

            float R = c.ppm * Ss;
            int W = Mathf.CeilToInt((ax1 - ax0) * R), H = Mathf.CeilToInt((az1 - az0) * R);
            // classi: 0 vuoto, 1 aperto, 2 muro, 3 arredo, 4 riga di scalino
            var cls = new byte[W * H];
            Func<float, float, int> ix = (x, a) => Mathf.Clamp(Mathf.FloorToInt((x - a) * R), 0, int.MaxValue);
            Action<Rett, byte, bool> riempi = (r, v, soloVuoto) =>
            {
                int a = Mathf.Max(0, ix(r.x0, ax0)), b = Mathf.Min(W - 1, ix(r.x1, ax0)), d = Mathf.Max(0, ix(r.z0, az0)), e = Mathf.Min(H - 1, ix(r.z1, az0));
                for (int y = d; y <= e; y++) for (int x = a; x <= b; x++) { int k = y * W + x; if (!soloVuoto || cls[k] == 0) cls[k] = v; }
            };
            foreach (var t in terra) riempi(t, 1, false);
            foreach (var a in arredi)
            {
                float dx = a.x1 - a.x0, dz = a.z1 - a.z0;
                if (Mathf.Max(dx, dz) < 0.4f || a.bottom > 1.2f || a.nome.StartsWith("Amerigo") || a.nome.StartsWith("Porta") || a.nome.StartsWith("Scarpe")) continue;
                riempi(a, 3, false);
            }
            foreach (var m in muriVeri) riempi(m, 2, false);
            // righe degli scalini: sul bordo basso di ogni gradino
            foreach (var s in scale)
            {
                var r = s;
                if (s.x1 - s.x0 >= s.z1 - s.z0) r.z1 = r.z0 + 1.1f / R; else r.x1 = r.x0 + 1.1f / R;
                riempi(r, 4, false);
            }

            // ----- colori
            bool ricordo = c.tema == "ricordo";
            var carta = ricordo ? new Color(0.80f, 0.66f, 0.43f) : new Color(0.93f, 0.87f, 0.73f);
            var macchia = ricordo ? new Color(0.50f, 0.32f, 0.14f) : new Color(0.70f, 0.55f, 0.32f);
            var inchiostro = ricordo ? new Color(0.22f, 0.12f, 0.05f) : new Color(0.17f, 0.12f, 0.09f);
            float seed = c.scena.GetHashCode() & 0xFFFF;

            var pix = new Color[(W / Ss) * (H / Ss)];
            int fw = W / Ss, fh = H / Ss;
            float eWall = 0.13f * R;     // spessore del contorno (px del campione)
            Func<float, float, byte> classe = (x, y) =>
            {
                int xi = Mathf.Clamp((int)x, 0, W - 1), yi = Mathf.Clamp((int)y, 0, H - 1);
                return cls[yi * W + xi];
            };
            for (int fy = 0; fy < fh; fy++)
            {
                for (int fx = 0; fx < fw; fx++)
                {
                    Color acc = Color.black;
                    for (int sy = 0; sy < Ss; sy++)
                    {
                        for (int sx = 0; sx < Ss; sx++)
                        {
                            float px = fx * Ss + sx + 0.5f, py = fy * Ss + sy + 0.5f;
                            float wx = ax0 + px / R, wz = az0 + py / R;
                            // contorni a mano: il campione viene spostato un poco
                            float ox = (Mathf.PerlinNoise(wx * 0.55f + seed, wz * 0.55f) - 0.5f) * 0.30f * R + (Mathf.PerlinNoise(wx * 3.1f, wz * 3.1f + seed) - 0.5f) * 0.10f * R;
                            float oy = (Mathf.PerlinNoise(wx * 0.55f, wz * 0.55f + seed + 91f) - 0.5f) * 0.30f * R + (Mathf.PerlinNoise(wx * 3.1f + seed, wz * 3.1f + 17f) - 0.5f) * 0.10f * R;
                            byte k = classe(px + ox, py + oy);

                            // carta: grana, macchie, bordo scurito
                            float n1 = Mathf.PerlinNoise(wx * 0.9f + seed, wz * 0.9f);
                            float n2 = Mathf.PerlinNoise(wx * 7f, wz * 7f + seed);
                            float n3 = Mathf.PerlinNoise(wx * 0.11f + seed * 0.3f, wz * 0.11f + 37f);
                            Color col = carta * (0.94f + 0.10f * n1 + 0.04f * n2);
                            col.a = 1f;
                            if (n3 > 0.55f) col = Color.Lerp(col, macchia, (n3 - 0.55f) * (ricordo ? 1.7f : 1.0f));
                            float dEdge = Mathf.Min(Mathf.Min(wx - ax0, ax1 - wx), Mathf.Min(wz - az0, az1 - wz));
                            float v = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dEdge / (ricordo ? 14f : 9f)));
                            col = Color.Lerp(col * (ricordo ? 0.62f : 0.80f), col, v);

                            if (k == 1 || k == 3 || k == 4) col = Color.Lerp(col, new Color(Mathf.Min(1f, carta.r * 1.17f), Mathf.Min(1f, carta.g * 1.17f), Mathf.Min(1f, carta.b * 1.17f)), 0.55f);
                            float ink = 0f;
                            if (k == 2)
                            {
                                // muro: lavis di inchiostro e tratteggio obliquo; contorno pieno
                                col = Color.Lerp(col, inchiostro, 0.16f);
                                float h = (wx + wz) / 0.55f;
                                if (h - Mathf.Floor(h) < 0.20f) ink = 0.55f;
                                bool bordo = classe(px + ox + eWall, py + oy) != 2 || classe(px + ox - eWall, py + oy) != 2 || classe(px + ox, py + oy + eWall) != 2 || classe(px + ox, py + oy - eWall) != 2;
                                if (bordo) ink = 0.95f;
                            }
                            else if (k == 3)
                            {
                                bool bordo = classe(px + ox + eWall * 0.6f, py + oy) != 3 || classe(px + ox - eWall * 0.6f, py + oy) != 3 || classe(px + ox, py + oy + eWall * 0.6f) != 3 || classe(px + ox, py + oy - eWall * 0.6f) != 3;
                                col = Color.Lerp(col, inchiostro, 0.07f);
                                if (bordo) ink = 0.70f;
                            }
                            else if (k == 4) ink = 0.80f;
                            else if (k == 1)
                            {
                                // il terreno aperto confina con il vuoto: filetto sottile (marciapiede)
                                bool bordo = classe(px + ox + eWall, py + oy) == 0 || classe(px + ox - eWall, py + oy) == 0 || classe(px + ox, py + oy + eWall) == 0 || classe(px + ox, py + oy - eWall) == 0;
                                if (bordo) ink = 0.50f;
                            }
                            if (ink > 0f) col = Color.Lerp(col, inchiostro, ink * (0.85f + 0.15f * n2));
                            acc += col;
                        }
                    }
                    var f = acc / (Ss * Ss); f.a = 1f;
                    pix[fy * fw + fx] = f;
                }
            }

            // ----- glifi dei punti d'interesse
            var tela = new Tela(pix, fw, fh);
            foreach (var p in punti)
            {
                float gx = (p.x - ax0) * c.ppm, gy = (p.z - az0) * c.ppm;
                Glifo(tela, p.glifo, gx, gy, c.ppm >= 16f ? 1.3f : 1f, inchiostro);
            }

            var tex = new Texture2D(fw, fh, TextureFormat.RGB24, false);
            tex.SetPixels(pix);
            tex.Apply();
            string file = Path.Combine(CartellaMappe, c.scena + ".jpg");
            File.WriteAllBytes(file, tex.EncodeToJPG(90));
            UnityEngine.Object.DestroyImmediate(tex);
            return fw + "x" + fh + " px, area x " + ax0 + ".." + ax1 + " z " + az0 + ".." + az1 + ", " + (new FileInfo(file).Length / 1024) + " KB, muri " + muriVeri.Count + ", terreno " + terra.Count + ", arredi " + arredi.Count + ", glifi " + punti.Count;
        }

        // ---------------------------------------------------------------- glifi
        sealed class Tela
        {
            public Color[] p; public int w, h;
            public Tela(Color[] p, int w, int h) { this.p = p; this.w = w; this.h = h; }
            public void Mix(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= w || y >= h || a <= 0f) return;
                var o = p[y * w + x];
                p[y * w + x] = new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), Mathf.Max(o.a, a));
            }
        }

        static float Cop(float d) { return Mathf.Clamp01(0.5f - d); }   // copertura di un pixel a distanza d dal bordo

        static void Disco(Tela t, float cx, float cy, float r, float sp, Color c, bool pieno)
        {
            int a = Mathf.FloorToInt(cx - r - sp - 2), b = Mathf.CeilToInt(cx + r + sp + 2), d = Mathf.FloorToInt(cy - r - sp - 2), e = Mathf.CeilToInt(cy + r + sp + 2);
            for (int y = d; y <= e; y++) for (int x = a; x <= b; x++)
                {
                    float dist = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float cov = pieno ? Cop(dist - r) : Cop(Mathf.Abs(dist - r) - sp * 0.5f);
                    t.Mix(x, y, c, cov);
                }
        }

        static void Segmento(Tela t, float x0, float y0, float x1, float y1, float sp, Color c)
        {
            int a = Mathf.FloorToInt(Mathf.Min(x0, x1) - sp - 2), b = Mathf.CeilToInt(Mathf.Max(x0, x1) + sp + 2), d = Mathf.FloorToInt(Mathf.Min(y0, y1) - sp - 2), e = Mathf.CeilToInt(Mathf.Max(y0, y1) + sp + 2);
            var A = new Vector2(x0, y0); var B = new Vector2(x1, y1); var AB = B - A; float l2 = Mathf.Max(0.0001f, AB.sqrMagnitude);
            for (int y = d; y <= e; y++) for (int x = a; x <= b; x++)
                {
                    var P = new Vector2(x + 0.5f, y + 0.5f);
                    float k = Mathf.Clamp01(Vector2.Dot(P - A, AB) / l2);
                    t.Mix(x, y, c, Cop((P - (A + AB * k)).magnitude - sp * 0.5f));
                }
        }

        static void Rettangolo(Tela t, float cx, float cy, float hw, float hh, float sp, Color c, bool pieno)
        {
            if (pieno)
            {
                for (int y = Mathf.FloorToInt(cy - hh - 1); y <= Mathf.CeilToInt(cy + hh + 1); y++)
                    for (int x = Mathf.FloorToInt(cx - hw - 1); x <= Mathf.CeilToInt(cx + hw + 1); x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - cx) - hw, dy = Mathf.Abs(y + 0.5f - cy) - hh;
                        t.Mix(x, y, c, Cop(Mathf.Max(dx, dy)));
                    }
                return;
            }
            Segmento(t, cx - hw, cy - hh, cx + hw, cy - hh, sp, c); Segmento(t, cx + hw, cy - hh, cx + hw, cy + hh, sp, c);
            Segmento(t, cx + hw, cy + hh, cx - hw, cy + hh, sp, c); Segmento(t, cx - hw, cy + hh, cx - hw, cy - hh, sp, c);
        }

        static readonly Color Rosso = new Color(0.52f, 0.13f, 0.10f);

        /// <summary>Disegna un glifo d'epoca (inchiostro) centrato in (cx, cy) pixel. s = scala.</summary>
        static void Glifo(Tela t, string tipo, float cx, float cy, float s, Color ink)
        {
            float u = s;
            switch (tipo)
            {
                case "porta":
                    Rettangolo(t, cx, cy, 5f * u, 7.5f * u, 1.7f * u, ink, false);
                    Rettangolo(t, cx, cy + 0.5f * u, 2.6f * u, 5f * u, 1f, ink, true);
                    Disco(t, cx + 1.4f * u, cy + 0.5f * u, 0.9f * u, 1f, new Color(0.9f, 0.82f, 0.62f), true);
                    break;
                case "fontana":
                    Disco(t, cx, cy, 8f * u, 1.8f * u, ink, false);
                    Disco(t, cx, cy, 3f * u, 1f, ink, true);
                    for (int i = 0; i < 4; i++) { float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f; Segmento(t, cx + Mathf.Cos(a) * 4.5f * u, cy + Mathf.Sin(a) * 4.5f * u, cx + Mathf.Cos(a) * 7f * u, cy + Mathf.Sin(a) * 7f * u, 1.2f * u, ink); }
                    break;
                case "edicola":
                    Rettangolo(t, cx, cy, 6.5f * u, 6.5f * u, 1.7f * u, Rosso, false);
                    Segmento(t, cx, cy - 4f * u, cx, cy + 4f * u, 1.6f * u, Rosso);
                    Segmento(t, cx - 3f * u, cy + 1f * u, cx + 3f * u, cy + 1f * u, 1.6f * u, Rosso);
                    break;
                case "pozzo":
                    Disco(t, cx, cy, 6.5f * u, 2.6f * u, ink, false);
                    Disco(t, cx, cy, 2.4f * u, 1.4f * u, ink, false);
                    break;
                case "lavatoio":
                    Rettangolo(t, cx, cy, 9f * u, 5f * u, 1.7f * u, ink, false);
                    for (int row = -1; row <= 1; row += 2)
                        for (int i = -7; i < 7; i++)
                            Segmento(t, cx + i * 1.2f * u, cy + row * 1.6f * u + Mathf.Sin(i) * 0.7f * u, cx + (i + 1) * 1.2f * u, cy + row * 1.6f * u + Mathf.Sin(i + 1) * 0.7f * u, 1f, ink);
                    break;
                case "uscita":
                    Segmento(t, cx - 8f * u, cy, cx + 6f * u, cy, 2.2f * u, Rosso);
                    Segmento(t, cx + 7f * u, cy, cx + 1f * u, cy + 5.5f * u, 2.2f * u, Rosso);
                    Segmento(t, cx + 7f * u, cy, cx + 1f * u, cy - 5.5f * u, 2.2f * u, Rosso);
                    break;
                case "stazione":
                    Rettangolo(t, cx - 1f * u, cy, 9f * u, 4.5f * u, 1.6f * u, ink, false);
                    Rettangolo(t, cx + 5f * u, cy - 1f * u, 3.8f * u, 3.4f * u, 1f, ink, true);
                    Rettangolo(t, cx - 6f * u, cy - 7f * u, 1.2f * u, 3f * u, 1f, ink, true);
                    Disco(t, cx - 5f * u, cy + 6f * u, 2.2f * u, 1.4f * u, ink, false);
                    Disco(t, cx + 3f * u, cy + 6f * u, 2.2f * u, 1.4f * u, ink, false);
                    break;
                case "bottega":
                    Rettangolo(t, cx, cy + 2.5f * u, 6f * u, 4.5f * u, 1.6f * u, ink, false);
                    for (int i = -3; i <= 3; i++) Segmento(t, cx + i * 2f * u, cy - 5.5f * u, cx + i * 2.4f * u, cy - 2f * u, 1.5f * u, i % 2 == 0 ? ink : Rosso);
                    Segmento(t, cx - 7f * u, cy - 2f * u, cx + 7f * u, cy - 2f * u, 1.5f * u, ink);
                    break;
            }
        }

        static void ScriviGlifi()
        {
            int n = OrdineGlifi.Length, cella = 48;
            var pix = new Color[n * cella * cella];
            for (int i = 0; i < pix.Length; i++) pix[i] = new Color(0f, 0f, 0f, 0f);
            var t = new Tela(pix, n * cella, cella);
            var ink = new Color(0.17f, 0.12f, 0.09f);
            for (int i = 0; i < n; i++) Glifo(t, OrdineGlifi[i], i * cella + cella * 0.5f, cella * 0.5f, 2.0f, ink);
            // il colore sotto lo zero di alpha deve restare quello dell'inchiostro (niente aloni scuri nei bordi): premoltiplicazione inversa
            for (int i = 0; i < pix.Length; i++) { var c = pix[i]; if (c.a > 0f) { c.r = Mathf.Clamp01(c.r); c.g = Mathf.Clamp01(c.g); c.b = Mathf.Clamp01(c.b); } else c = new Color(ink.r, ink.g, ink.b, 0f); pix[i] = c; }
            var tex = new Texture2D(n * cella, cella, TextureFormat.RGBA32, false);
            tex.SetPixels(pix);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(CartellaMappe, "glifi.png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void ImportaTexture()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CartellaMappe }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool glifi = path.EndsWith("glifi.png");
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.alphaSource = glifi ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                imp.alphaIsTransparency = glifi;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = true;
                imp.isReadable = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = 2048;
                imp.textureCompression = glifi ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
        }
    }
}
