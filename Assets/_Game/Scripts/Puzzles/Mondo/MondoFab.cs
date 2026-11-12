using System.Collections.Generic;
using TMPro;
using TrenoBambini.Data;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R009/R020: costruisce gli oggetti d'epoca degli enigmi (baule, cornice, calendario, cassetta, scarpe...) da pezzi di primitive con
    /// materiali URP Lit a texture procedurali (grana di legno, carta, stoffa, metallo graffiato). Nessun file: tutto nasce dal JSON.
    /// La stessa funzione costruisce l'oggetto nel mondo e il suo gemello nella vista ravvicinata.
    /// </summary>
    public static class MondoFab
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static Shader lit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { cache.Clear(); textures.Clear(); lit = null; tinte.Clear(); fonts.Clear(); }

        struct Prop { public Color col; public float smooth, metal; public string tex; public Prop(Color c, float s, float m, string t) { col = c; smooth = s; metal = m; tex = t; } }

        static Prop Preset(string m)
        {
            switch ((m ?? "").ToLowerInvariant())
            {
                case "metallo": return new Prop(new Color(0.42f, 0.42f, 0.43f), 0.4f, 0.4f, "graffi");
                case "ottone": return new Prop(new Color(0.7f, 0.52f, 0.22f), 0.45f, 0.5f, "graffi");
                case "argento": return new Prop(new Color(0.8f, 0.8f, 0.78f), 0.5f, 0.45f, "graffi");
                case "carta": return new Prop(new Color(0.85f, 0.77f, 0.58f), 0.08f, 0f, "carta");
                case "stoffa": return new Prop(new Color(0.5f, 0.42f, 0.33f), 0.04f, 0f, "trama");
                case "cuoio": return new Prop(new Color(0.27f, 0.15f, 0.09f), 0.3f, 0f, "carta");
                case "pietra": return new Prop(new Color(0.45f, 0.42f, 0.38f), 0.1f, 0f, "carta");
                case "gesso": return new Prop(new Color(0.88f, 0.87f, 0.82f), 0.05f, 0f, "carta");
                case "vetro": return new Prop(new Color(0.45f, 0.55f, 0.55f), 0.9f, 0f, "");
                case "cera": return new Prop(new Color(0.85f, 0.78f, 0.6f), 0.35f, 0f, "");
                case "quadri": return new Prop(new Color(0.6f, 0.28f, 0.2f), 0.04f, 0f, "quadri");
                default: return new Prop(new Color(0.38f, 0.25f, 0.15f), 0.22f, 0f, "legno");
            }
        }

        static Texture2D Tex(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            Texture2D t;
            if (textures.TryGetValue(kind, out t)) return t;
            const int n = 128;
            t = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Mondo_" + kind, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n, g;
                    switch (kind)
                    {
                        case "legno":
                            g = 0.72f + 0.2f * Mathf.PerlinNoise(u * 3f, v * 40f) + 0.08f * Mathf.PerlinNoise(u * 17f, v * 90f);
                            g -= 0.12f * Mathf.Pow(Mathf.PerlinNoise(u * 6f + 4f, v * 7f), 3f);
                            break;
                        case "trama":
                            g = 0.78f + 0.16f * Mathf.Sin(u * Mathf.PI * 2f * 32f) * Mathf.Sin(v * Mathf.PI * 2f * 32f) + 0.06f * Mathf.PerlinNoise(u * 9f, v * 9f);
                            break;
                        case "graffi":
                            g = 0.8f + 0.1f * Mathf.PerlinNoise(u * 6f, v * 6f) - 0.1f * Mathf.Clamp01(Mathf.PerlinNoise(u * 70f, v * 3f) - 0.62f) * 4f;
                            break;
                        case "quadri":
                            g = (((x / 16) + (y / 16)) % 2 == 0) ? 0.95f : 0.45f;
                            g *= 0.92f + 0.08f * Mathf.PerlinNoise(u * 20f, v * 20f);
                            break;
                        default: // carta: macchie morbide
                            g = 0.86f + 0.14f * Mathf.PerlinNoise(u * 5f + 2f, v * 5f) - 0.06f * Mathf.PerlinNoise(u * 40f, v * 40f);
                            break;
                    }
                    byte b = (byte)Mathf.Clamp(g * 255f, 0f, 255f);
                    px[y * n + x] = new Color32(b, b, b, 255);
                }
            t.SetPixels32(px);
            t.Apply(true, true);
            textures[kind] = t;
            return t;
        }

        public static Color Hex(string s, Color def)
        {
            Color c;
            if (!string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out c)) return c;
            return def;
        }

        public static Material Mat(string materiale, string colore)
        {
            string key = (materiale ?? "") + "|" + (colore ?? "");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            var p = Preset(materiale);
            var c = Hex(colore, p.col);
            // il materiale di riferimento in Resources porta nella build le varianti URP Lit con _EMISSION (altrimenti l'evidenziazione sparirebbe nel player)
            var rif = Resources.Load<Material>("Materiali/MP_Rif");
            m = rif != null ? new Material(rif) : new Material(lit);
            m.name = "MP_" + key;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", p.smooth);
            m.SetFloat("_Metallic", p.metal);
            var tex = Tex(p.tex);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            // R011: "tex:<nome>" = texture disegnata (Resources/Texture/Segreti/<nome>.png, generata da Treno > Segreti > Genera texture dei pezzi)
            if (!string.IsNullOrEmpty(materiale) && materiale.StartsWith("tex:", System.StringComparison.Ordinal))
            {
                var dis = Resources.Load<Texture2D>("Texture/Segreti/" + materiale.Substring(4));
                if (dis != null) { m.SetTexture("_BaseMap", dis); m.SetColor("_BaseColor", string.IsNullOrEmpty(colore) ? Color.white : c); m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Metallic", 0f); }
            }
            m.EnableKeyword("_EMISSION");
            cache[key] = m;
            return m;
        }

        public static Font DefaultFont { get { return null; } }

        static GameObject Forma(string forma, out bool conCollider)
        {
            conCollider = true;
            PrimitiveType t;
            switch ((forma ?? "").ToLowerInvariant())
            {
                case "cilindro": t = PrimitiveType.Cylinder; break;
                case "sfera": t = PrimitiveType.Sphere; break;
                case "capsula": t = PrimitiveType.Capsule; break;
                default: t = PrimitiveType.Cube; break;
            }
            return GameObject.CreatePrimitive(t);
        }

        static readonly Dictionary<string, Material> tinte = new Dictionary<string, Material>();
        static readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();

        /// <summary>Caratteri d'epoca (Resources/Fonts): "" = stampa (Old Standard), "mano" = scrittura a mano (Cedarville Cursive). Null = font predefinito di TMP.</summary>
        public static TMP_FontAsset Font(string tipo)
        {
            string k = tipo == "mano" ? "Fonts/Mano SDF" : "Fonts/Stampa SDF";
            TMP_FontAsset f;
            if (!fonts.TryGetValue(k, out f) || f == null) { f = Resources.Load<TMP_FontAsset>(k); fonts[k] = f; }
            return f;
        }

        static void Carattere(TextMeshPro tm, string tipo)
        {
            var f = Font(tipo);
            if (f != null) tm.font = f;
        }

        /// <summary>
        /// Materiale di uno slot del modello: ME_*/MX_* sono materiali propri con texture (restano), MT_* si tingono col colore della parte
        /// (copia in cache per colore), tutto il resto prende il materiale della parte.
        /// </summary>
        static Material Slot(Material proprio, Material parte)
        {
            if (proprio == null) return parte;
            string n = proprio.name;
            if (n.StartsWith("ME_") || n.StartsWith("MX_")) return proprio;
            if (n.StartsWith("MT_") && parte != null)
            {
                var c = parte.HasProperty("_BaseColor") ? parte.GetColor("_BaseColor") : Color.white;
                string key = n + "|" + ColorUtility.ToHtmlStringRGB(c);
                Material m;
                if (!tinte.TryGetValue(key, out m) || m == null)
                {
                    m = new Material(proprio) { name = n + "_" + ColorUtility.ToHtmlStringRGB(c) };
                    m.SetColor("_BaseColor", c);
                    tinte[key] = m;
                }
                return m;
            }
            return parte;
        }

        /// <summary>
        /// Istanzia un modello vero di Resources/Modelli/Enigmi, senza collider. Ritorna null se il prefab non esiste: allora chi chiama lascia visibile
        /// la propria forma di base (cubo, cilindro... della parte, o le scarpe da primitive), che e' solo un segnaposto. EnigmiSelfTest controlla che
        /// tutti i modelli citati nei JSON si carichino, quindi nel gioco questo caso non deve capitare.
        /// </summary>
        public static GameObject Modello(string nome, Transform parent, Material mat, Vector3 pos, Vector3 rot, Vector3 scala)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            var pf = Resources.Load<GameObject>("Modelli/Enigmi/" + nome);
            if (pf == null) return null;
            var g = Object.Instantiate(pf, parent);
            g.name = "Modello_" + nome;
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(rot);
            g.transform.localScale = scala;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = Slot(ms[i], mat);
                r.sharedMaterials = ms;
            }
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return g;
        }

        /// <summary>Modello della parte: misura vera (msx..) e centro (mox..) se dati, altrimenti riempie la scatola della parte. 'scala' = scala del GameObject della parte.</summary>
        static GameObject ModelloParte(ParteDef d, Transform go, Material mat, Vector3 scala)
        {
            float sx = Mathf.Max(1e-4f, Mathf.Abs(scala.x)), sy = Mathf.Max(1e-4f, Mathf.Abs(scala.y)), sz = Mathf.Max(1e-4f, Mathf.Abs(scala.z));
            var dim = new Vector3(d.msx > 0f ? d.msx : d.sx, d.msy > 0f ? d.msy : d.sy, d.msz > 0f ? d.msz : d.sz);
            return Modello(d.modello, go, mat, new Vector3(d.mox / sx, d.moy / sy, d.moz / sz), Vector3.zero, new Vector3(dim.x / sx, dim.y / sy, dim.z / sz));
        }

        /// <summary>Costruisce un singolo pezzo come figlio di parent. Con colliders = true il pezzo cliccabile ha il collider per i raggi.</summary>
        public static GameObject Pezzo(ParteDef d, Transform parent, bool colliders)
        {
            GameObject go;
            string f = (d.forma ?? "cubo").ToLowerInvariant();
            if (f == "testo")
            {
                go = new GameObject(d.id);
                var tm = go.AddComponent<TextMeshPro>();
                Carattere(tm, d.font);
                tm.text = d.testo ?? "";
                tm.fontSize = Mathf.Max(0.1f, d.dimensione * 10f);
                tm.color = Hex(d.colore, new Color(0.17f, 0.12f, 0.08f));
                tm.alignment = TextAlignmentOptions.Center;
                tm.textWrappingMode = TextWrappingModes.Normal;
                tm.richText = true;
                tm.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.05f, d.sx), Mathf.Max(0.05f, d.sy));
                if (!string.IsNullOrEmpty(d.asse) && d.asse == "sx") tm.alignment = TextAlignmentOptions.TopLeft;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(d.x, d.y, d.z);
                go.transform.localRotation = Quaternion.Euler(d.rx, d.ry, d.rz);
                if (colliders && d.clic)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = new Vector3(Mathf.Max(0.05f, d.sx), Mathf.Max(0.05f, d.sy), 0.02f);
                }
                return go;
            }
            if (f == "scarpa") return Scarpa(d, parent, colliders);
            if (f == "vuoto")
            {
                go = new GameObject(d.id);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(d.x, d.y, d.z);
                go.transform.localRotation = Quaternion.Euler(d.rx, d.ry, d.rz);
                if (colliders && d.clic)
                {
                    var bcv = go.AddComponent<BoxCollider>();
                    bcv.size = new Vector3(d.sx, d.sy, d.sz);
                }
                if (!string.IsNullOrEmpty(d.modello)) ModelloParte(d, go.transform, Mat(d.materiale, d.colore), Vector3.one); // gruppo con il suo modello vero
                return go;
            }

            bool col;
            go = Forma(f, out col);
            go.name = d.id;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(d.x, d.y, d.z);
            go.transform.localRotation = Quaternion.Euler(d.rx, d.ry, d.rz);
            go.transform.localScale = new Vector3(d.sx, d.sy, d.sz);
            var matParte = Mat(d.materiale, d.colore);
            go.GetComponent<Renderer>().sharedMaterial = matParte;
            if (!string.IsNullOrEmpty(d.modello) && ModelloParte(d, go.transform, matParte, go.transform.localScale) != null)
                go.GetComponent<Renderer>().enabled = false; // al posto del cubo si vede il modello (il cubo resta come collider)
            var c = go.GetComponent<Collider>();
            if (c != null && !(colliders && d.clic)) Object.Destroy(c);
            if (!string.IsNullOrEmpty(d.testo) && f != "testo")
            {
                // scritta sulla faccia frontale (-z)
                var tg = new GameObject("Scritta");
                var tm = tg.AddComponent<TextMeshPro>();
                Carattere(tm, d.font);
                tm.text = d.testo;
                tm.fontSize = Mathf.Max(0.1f, d.dimensione * 10f);
                tm.color = new Color(0.17f, 0.12f, 0.08f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.textWrappingMode = TextWrappingModes.Normal;
                tm.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.05f, d.sx * 0.92f), Mathf.Max(0.05f, d.sy * 0.92f));
                tg.transform.SetParent(go.transform, false);
                tg.transform.localPosition = new Vector3(0f, 0f, -0.505f);
                tg.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, d.sx), 1f / Mathf.Max(0.001f, d.sy), 1f / Mathf.Max(0.001f, d.sz));
                tm.rectTransform.sizeDelta = new Vector2(d.sx * 0.92f * 1f, d.sy * 0.92f);
            }
            return go;
        }

        /// <summary>Scarpa d'epoca da primitive. Lo stile e' nel campo testo: lucide, chiodate, zoccoli, militari, tacchi, risuolate, cartone, pezze. Lunga ~28 cm, punta verso +x.</summary>
        public static GameObject Scarpa(ParteDef d, Transform parent, bool colliders)
        {
            var root = new GameObject(d.id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(d.x, d.y, d.z);
            root.transform.localRotation = Quaternion.Euler(d.rx, d.ry, d.rz);
            root.transform.localScale = new Vector3(d.sx, d.sy, d.sz);
            string st = (d.testo ?? "").ToLowerInvariant();
            string tomaia = "cuoio", cT = d.colore;
            if (string.IsNullOrEmpty(cT))
            {
                switch (st)
                {
                    case "lucide": cT = "#16110e"; break;
                    case "chiodate": cT = "#4a3320"; break;
                    case "militari": cT = "#3a3022"; break;
                    case "tacchi": cT = "#2a1714"; break;
                    case "risuolate": cT = "#5a3a22"; break;
                    case "cartone": cT = "#6a5a40"; break;
                    default: cT = "#3a2616"; break;
                }
            }
            if (st == "zoccoli") { tomaia = "legno"; cT = string.IsNullOrEmpty(d.colore) ? "#6b4a2b" : d.colore; }
            if (st == "pezze") { tomaia = "stoffa"; cT = string.IsNullOrEmpty(d.colore) ? "#6d6252" : d.colore; }
            if (st == "matita") { tomaia = "gesso"; cT = string.IsNullOrEmpty(d.colore) ? "#55534f" : d.colore; }
            var mT = Mat(tomaia, cT);
            var mSuola = st == "matita" ? mT : Mat("cuoio", st == "risuolate" ? "#b07a45" : "#1c120b");
            var mLucido = st == "lucide" ? Mat("vetro", "#0e0b09") : mT;
            if (st == "matita" && Modello("segno_matita", root.transform, mT, new Vector3(0f, 0.065f, 0f), Vector3.zero, new Vector3(0.29f, 0.13f, 1f)) != null)
            {
                // scarpetta disegnata a matita sulla carta (decal), non una scarpa vera
                if (colliders && d.clic) { var bm = root.AddComponent<BoxCollider>(); bm.center = new Vector3(0f, 0.065f, 0f); bm.size = new Vector3(0.3f, 0.13f, 0.02f); }
                return root;
            }

            // R024: le cinque calzature da raccogliere hanno un modello vero ciascuna (Resources/Modelli/Enigmi/calz_<stile>), appoggiato a terra
            float lungCalz = LunghezzaCalzatura(st);
            if (lungCalz > 0f)
            {
                var mc = Modello("calz_" + st, root.transform, mT, Vector3.zero, Vector3.zero, Vector3.one * lungCalz);
                if (mc != null)
                {
                    AppoggiaATerra(mc, root.transform);
                    if (colliders && d.clic)
                    {
                        var bcc = root.AddComponent<BoxCollider>();
                        bcc.center = new Vector3(0f, 0.06f, 0f);
                        bcc.size = new Vector3(0.3f, 0.13f, 0.11f);
                    }
                    return root;
                }
            }

            Part(root, PrimitiveType.Cube, "suola", new Vector3(0f, 0.012f, 0f), new Vector3(0.29f, 0.025f, 0.1f), mSuola);
            if (st == "zoccoli")
            {
                Part(root, PrimitiveType.Cube, "blocco", new Vector3(-0.01f, 0.05f, 0f), new Vector3(0.26f, 0.05f, 0.095f), mT);
                Part(root, PrimitiveType.Sphere, "punta", new Vector3(0.1f, 0.05f, 0f), new Vector3(0.12f, 0.06f, 0.095f), mT);
                Part(root, PrimitiveType.Cube, "fascia", new Vector3(-0.02f, 0.085f, 0f), new Vector3(0.1f, 0.035f, 0.1f), Mat("cuoio", "#2c1b10"));
            }
            else if (st == "pezze")
            {
                Part(root, PrimitiveType.Sphere, "pezza1", new Vector3(0.0f, 0.04f, 0f), new Vector3(0.24f, 0.07f, 0.11f), mT);
                Part(root, PrimitiveType.Cylinder, "legaccio", new Vector3(-0.03f, 0.06f, 0f), new Vector3(0.1f, 0.012f, 0.1f), Mat("stoffa", "#8b7b5f"));
            }
            else
            {
                float alto = (st == "militari") ? 0.17f : (st == "tacchi" ? 0.07f : 0.1f);
                // modello vero della scarpa (lunghezza 1 su X, alto 0,45, largo 0,51): lunghezza 0,29 m, altezza e larghezza dal tipo di scarpa
                float ky = (st == "militari") ? 0.95f : (st == "tacchi" ? 0.62f : 0.78f);
                var mod = Modello("scarpa", root.transform, mLucido, new Vector3(0.0f, 0.06f + 0.29f * 0.45f * ky * 0.5f, 0f), Vector3.zero, new Vector3(0.29f, 0.29f * ky, 0.2f));
                if (mod == null)
                {
                    Part(root, PrimitiveType.Cube, "tomaia", new Vector3(-0.045f, 0.025f + alto * 0.5f, 0f), new Vector3(0.15f, alto, 0.088f), mLucido);
                    Part(root, PrimitiveType.Sphere, "punta", new Vector3(0.095f, 0.05f, 0f), new Vector3(0.12f, 0.07f, 0.092f), mLucido);
                    Part(root, PrimitiveType.Cube, "ponte", new Vector3(0.03f, 0.065f, 0f), new Vector3(0.11f, 0.05f, 0.085f), mLucido);
                }
                if (st == "tacchi")
                {
                    root.transform.localScale = new Vector3(d.sx, d.sy, d.sz);
                    Part(root, PrimitiveType.Cube, "tacco", new Vector3(-0.115f, -0.03f, 0f), new Vector3(0.03f, 0.07f, 0.04f), Mat("cuoio", "#120c09"));
                }
                if (st == "chiodate")
                {
                    for (int i = 0; i < 7; i++)
                        Part(root, PrimitiveType.Sphere, "chiodo" + i, new Vector3(-0.11f + i * 0.035f, -0.001f, (i % 2 == 0) ? 0.03f : -0.03f), new Vector3(0.012f, 0.008f, 0.012f), Mat("metallo", "#8c8c8c"));
                    Part(root, PrimitiveType.Sphere, "calce", new Vector3(0.0f, 0.1f, -0.04f), new Vector3(0.12f, 0.03f, 0.012f), Mat("gesso", "#ddd8cc"));
                }
                if (st == "militari")
                    Part(root, PrimitiveType.Cylinder, "laccio", new Vector3(0.0f, 0.12f, 0f), new Vector3(0.012f, 0.1f, 0.012f), Mat("cuoio", "#1b140d")).transform.localRotation = Quaternion.Euler(0, 0, 90);
                if (st == "risuolate")
                    Part(root, PrimitiveType.Cube, "pezza", new Vector3(0.06f, 0.03f, 0f), new Vector3(0.1f, 0.012f, 0.082f), Mat("cuoio", "#c18a52"));
            }
            if (colliders && d.clic)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.06f, 0f);
                bc.size = new Vector3(0.3f, 0.13f, 0.11f);
            }
            return root;
        }

        /// <summary>Lunghezza (m) del modello (misura x dei FBX in modelli_misure.json) della calzatura per lo stile dato; 0 = nessun modello dedicato (stile da primitive o dalla scarpa R023).</summary>
        public static float LunghezzaCalzatura(string stile)
        {
            switch (stile)
            {
                case "zoccoli": return 0.275f;
                case "militari": return 0.306f;
                case "cartone": return 0.356f;
                case "tacchi": return 0.248f;
                case "pezze": return 0.247f;
                default: return 0f;
            }
        }

        /// <summary>Porta il modello con il fondo sul piano della radice e il centro sopra la sua posizione (i modelli sono centrati nel loro cubo).</summary>
        static void AppoggiaATerra(GameObject modello, Transform radice)
        {
            bool primo = true; Bounds b = new Bounds();
            foreach (var r in modello.GetComponentsInChildren<Renderer>())
            {
                if (primo) { b = r.bounds; primo = false; } else b.Encapsulate(r.bounds);
            }
            if (primo) return;
            var p = radice.position;
            modello.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
        }

        static GameObject Part(GameObject root, PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(root.transform, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            var c = g.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            return g;
        }

        /// <summary>Crea l'insieme dei pezzi di un enigma sotto root. Ritorna i pezzi per id.</summary>
        public static Dictionary<string, GameObject> Oggetto(EnigmaDef def, Transform root, bool colliders)
        {
            var map = new Dictionary<string, GameObject>();
            if (def.parti == null) return map;
            for (int i = 0; i < def.parti.Count; i++)
            {
                var p = def.parti[i];
                if (string.IsNullOrEmpty(p.id)) p.id = "parte" + i;
                var g = Pezzo(p, root, colliders);
                map[p.id] = g;
            }
            // genitori: il figlio mantiene la posizione locale rispetto al genitore
            for (int i = 0; i < def.parti.Count; i++)
            {
                var p = def.parti[i];
                GameObject gen;
                if (!string.IsNullOrEmpty(p.genitore) && map.TryGetValue(p.genitore, out gen))
                {
                    var t = map[p.id].transform;
                    var lp = t.localPosition; var lr = t.localRotation;
                    t.SetParent(gen.transform, false);
                    t.localPosition = lp; t.localRotation = lr;
                }
            }
            return map;
        }

        /// <summary>Limiti complessivi dei renderer sotto t (spazio mondo).</summary>
        public static Bounds Limiti(Transform t, bool soloVisibili = true)
        {
            var rs = t.GetComponentsInChildren<Renderer>(!soloVisibili);
            bool first = true;
            var b = new Bounds(t.position, Vector3.zero);
            for (int i = 0; i < rs.Length; i++)
            {
                if (first) { b = rs[i].bounds; first = false; } else b.Encapsulate(rs[i].bounds);
            }
            return b;
        }
    }
}
