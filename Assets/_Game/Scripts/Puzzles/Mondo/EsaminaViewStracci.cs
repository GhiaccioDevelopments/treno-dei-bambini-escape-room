using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R012 (capitolo "Stracci"): i meccanismi nuovi della vista ravvicinata.
    ///  - tessuti: si prova ogni pezzo di stoffa (controluce, filo, spessore) e lo si etichetta (lana, cotone, lino, scarto); come la deduzione ma con le prove.
    ///  - bilancia: bilancia a piatti con pesi di ottone e un chiodo incastrato sotto il piatto; si pesano le ceste fino all'equilibrio.
    ///  - monete: monete vere sul banco, si paga la somma che spetta.
    ///  - colonne: abbina a tre colonne (oggetto, mestiere del 1946, diritto di oggi) con l'interfaccia.
    /// Tutti i testi stanno nel JSON (EnigmaDef.strumenti, passanti, righe, parti.msg). Il codice non contiene testi narrativi.
    /// </summary>
    public partial class EsaminaView
    {
        const int MaxMano = 6;

        static bool HaUno(string richiede)
        {
            foreach (var t in (richiede ?? "").Split(',')) if (MondoStato.Ha(t.Trim())) return true;
            return false;
        }

        /// <summary>L'oggetto in mano e' uno di quelli ammessi (richiede puo' essere un elenco separato da virgole).</summary>
        static bool Accetta(string richiede, string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            foreach (var t in (richiede ?? "").Split(',')) if (t.Trim() == token) return true;
            return false;
        }

        string S(int i) { return def.strumenti != null && i < def.strumenti.Count ? def.strumenti[i] : ""; }

        // ------------------------------------------------------------------ costruzione
        string strumento = "controluce";
        readonly List<Button> strumentiBtn = new List<Button>();
        readonly Dictionary<string, Transform> origGenitore = new Dictionary<string, Transform>();

        void BuildStracci(string mec)
        {
            if (mec == "tessuti") BuildTessuti();
            else if (mec == "bilancia") BuildBilancia();
            else if (mec == "monete") BuildMonete();
        }

        void BuildStracciUi(string mec)
        {
            if (mec == "tessuti") BuildTessutiUi();
            else if (mec == "monete") BuildMoneteUi();
            else if (mec == "colonne") BuildColonneUi();
        }

        /// <summary>Azioni dei pezzi dei nuovi meccanismi. true = gestita.</summary>
        bool UsaStracci(Run r)
        {
            string mec = Meccanismo(def);
            switch ((r.d.azione ?? "").ToLowerInvariant())
            {
                case "passante":
                    if (mec != "tessuti") return false;
                    ProvaTessuto(Mathf.RoundToInt(r.d.valore), null);
                    return true;
                case "peso": TogglePeso(r); return true;
                case "chiodo": ToglieChiodo(r); return true;
                case "moneta": ToggleMoneta(r); return true;
            }
            return false;
        }

        public bool ConfermaStracci()
        {
            if (solved) return false;
            string mec = Meccanismo(def);
            bool esitoCappotto;
            if (ConfermaCappotto(mec, out esitoCappotto)) return esitoCappotto; // R014: Appunta (finestrino), Rispondi (dialogo)
            bool esitoVolantino;
            if (ConfermaVolantino(mec, out esitoVolantino)) return esitoVolantino; // R013: Scrivi la matrice (ciclostile), Timbra (datario)
            if (mec == "monete") return PagaMonete();
            if (mec == "colonne") return ConfermaColonne();
            return false;
        }

        // ------------------------------------------------------------------ tessuti
        void BuildTessuti()
        {
            int n = def.passanti.Count;
            assegnati = new int[n];
            int perRiga = 5;
            float passo = 0.5f;
            MondoFab.Pezzo(new ParteDef { id = "pedana", forma = "cubo", materiale = "legno", colore = "#4a3623", x = 0f, y = -0.03f, z = 0.2f, sx = perRiga * passo + 0.2f, sy = 0.04f, sz = Mathf.CeilToInt(n / (float)perRiga) * 0.62f + 0.2f }, objRoot, false);
            for (int i = 0; i < n; i++)
            {
                var p = def.passanti[i];
                int col = i % perRiga, riga = i / perRiga;
                float cx = (col - (perRiga - 1) * 0.5f) * passo, cz = 0.45f - riga * 0.62f;
                var gr = new GameObject("coppia_" + p.id);
                gr.transform.SetParent(objRoot, false);
                gr.transform.localPosition = new Vector3(cx, 0f, cz);
                var bc = gr.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.04f, 0f); bc.size = new Vector3(0.46f, 0.12f, 0.4f);
                MondoFab.Pezzo(new ParteDef { id = "stoffa", forma = "cubo", materiale = "stoffa", colore = p.colore, x = 0f, y = 0.02f, z = 0f, ry = (i * 11) % 9 - 4f, sx = 0.4f, sy = 0.025f, sz = 0.32f }, gr.transform, false);
                MondoFab.Pezzo(new ParteDef { id = "orlo", forma = "cubo", materiale = "stoffa", colore = p.colore, x = 0.03f, y = 0.04f, z = 0.04f, ry = 14f, sx = 0.3f, sy = 0.012f, sz = 0.2f }, gr.transform, false);
                MondoFab.Pezzo(new ParteDef { id = "filo", forma = "cubo", materiale = "stoffa", colore = "#d8cdb0", x = 0.23f, y = 0.03f, z = -0.06f, sx = 0.14f, sy = 0.008f, sz = 0.008f }, gr.transform, false);
                var t = new GameObject("Etichetta"); var tm = t.AddComponent<TextMeshPro>();
                tm.text = "?"; tm.fontSize = 1.0f; tm.fontStyle = FontStyles.Bold; tm.color = new Color(0.95f, 0.88f, 0.7f); tm.alignment = TextAlignmentOptions.Center; tm.rectTransform.sizeDelta = new Vector2(0.5f, 0.14f);
                t.transform.SetParent(objRoot, false); t.transform.localPosition = new Vector3(cx, -0.04f, cz - 0.3f);
                t.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
                etichette.Add(tm);
                run["pass_" + p.id] = new Run { d = new ParteDef { id = "pass_" + p.id, clic = true, azione = "passante", valore = i }, go = gr, visibile = true, pos0 = gr.transform.localPosition, rot0 = gr.transform.localRotation };
            }
        }

        void BuildTessutiUi()
        {
            BuildDeduzioneUi(); // elenco delle categorie e pulsante Conferma
            for (int k = 0; k < 4; k++)
            {
                string id = k == 0 ? "controluce" : k == 1 ? "filo" : k == 2 ? "spessore" : "etichetta";
                string kk = id;
                var b = UIFactory.MakeButton(ui, S(k), () => ImpostaStrumento(kk), new Vector2(250f, 54f), ButtonStyle.Tag, 22f);
                b.gameObject.name = "Strumento_" + id;
                UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f + k * 262f, -250f), new Vector2(250f, 54f));
                strumentiBtn.Add(b);
            }
            ImpostaStrumento("controluce");
        }

        public void ImpostaStrumento(string id)
        {
            strumento = id;
            string[] ids = { "controluce", "filo", "spessore", "etichetta" };
            for (int k = 0; k < strumentiBtn.Count && k < 4; k++) UIFactory.SetLabel(strumentiBtn[k], (ids[k] == id ? "> " : "") + S(k));
        }

        public string Strumento { get { return strumento; } }

        /// <summary>Prova un pezzo con lo strumento scelto (null = quello attivo; "etichetta" cambia l'etichetta).</summary>
        public void ProvaTessuto(int i, string quale)
        {
            if (assegnati == null || i < 0 || i >= def.passanti.Count) return;
            string s = quale ?? strumento;
            var p = def.passanti[i];
            string txt = s == "controluce" ? p.controluce : s == "filo" ? p.filo : s == "spessore" ? p.spessore : null;
            if (txt == null) { CiclaPassante(i); return; }
            Info(txt);
            UISfx.Play(Sfx.Tick, 0.5f);
            Run r;
            if (run.TryGetValue("pass_" + p.id, out r) && r.go != null) StartCoroutine(ImpulsoTessuto(r, s));
        }

        IEnumerator ImpulsoTessuto(Run r, string s)
        {
            var st = r.go.transform.Find("stoffa");
            var filo = r.go.transform.Find("filo");
            if (mpb == null) mpb = new MaterialPropertyBlock();
            var rend = st != null ? st.GetComponent<Renderer>() : null;
            Vector3 p0 = r.pos0, filo0 = filo != null ? filo.localScale : Vector3.one, f0 = filo != null ? filo.localPosition : Vector3.zero;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.9f)
            {
                if (r.go == null) yield break;
                float w = Mathf.Sin(k * Mathf.PI);
                if (s == "controluce")
                {
                    r.go.transform.localPosition = p0 + new Vector3(0f, 0.12f * w, 0f);
                    if (rend != null) { rend.GetPropertyBlock(mpb); mpb.SetColor(Emis, new Color(0.9f, 0.7f, 0.3f) * (0.9f * w)); rend.SetPropertyBlock(mpb); }
                }
                else if (s == "filo" && filo != null)
                {
                    filo.localScale = new Vector3(filo0.x * (1f + 1.4f * w), filo0.y, filo0.z);
                    filo.localPosition = f0 + new Vector3(0.1f * w, 0f, 0f);
                }
                else if (st != null) st.localScale = new Vector3(0.4f, 0.025f * (1f + 2.5f * w), 0.32f);
                yield return null;
            }
            if (r.go == null) yield break;
            r.go.transform.localPosition = p0;
            if (rend != null) { rend.GetPropertyBlock(mpb); mpb.SetColor(Emis, Color.black); rend.SetPropertyBlock(mpb); }
            if (filo != null) { filo.localScale = filo0; filo.localPosition = f0; }
            if (st != null) st.localScale = new Vector3(0.4f, 0.025f, 0.32f);
        }

        // ------------------------------------------------------------------ bilancia
        bool chiodoPresente;
        float angoloBil, angoloBilObiettivo;
        Transform trave, piattoSx, piattoDx;
        readonly List<Run> caricoSx = new List<Run>();
        readonly List<Run> pesiDx = new List<Run>();
        public float SbilanciamentoGrammi { get; private set; }
        public bool ChiodoPresente { get { return chiodoPresente; } }

        void BuildBilancia()
        {
            Run r;
            trave = run.TryGetValue("trave", out r) ? r.go.transform : null;
            piattoSx = run.TryGetValue("piatto_sx", out r) ? r.go.transform : null;
            piattoDx = run.TryGetValue("piatto_dx", out r) ? r.go.transform : null;
            chiodoPresente = run.ContainsKey("chiodo");
            foreach (var kv in run) if (kv.Value.go != null) origGenitore[kv.Key] = kv.Value.go.transform.parent;
            SbilanciamentoGrammi = Peso();
            angoloBilObiettivo = Mathf.Clamp(SbilanciamentoGrammi / 25f, -14f, 14f);
            angoloBil = angoloBilObiettivo;
            StartCoroutine(AnimaBilancia());
        }

        IEnumerator AnimaBilancia()
        {
            while (!closed && trave != null)
            {
                angoloBil = Mathf.MoveTowards(angoloBil, angoloBilObiettivo, 45f * Time.unscaledDeltaTime);
                trave.localRotation = Quaternion.Euler(0f, 0f, angoloBil);
                if (piattoSx != null) piattoSx.localRotation = Quaternion.Euler(0f, 0f, -angoloBil);
                if (piattoDx != null) piattoDx.localRotation = Quaternion.Euler(0f, 0f, -angoloBil);
                yield return null;
            }
        }

        /// <summary>Grammi a sinistra (con il chiodo, se c'e') meno i grammi a destra: positivo = pende a sinistra.</summary>
        float Peso()
        {
            float sx = chiodoPresente ? def.squilibrio : 0f, dx = 0f;
            foreach (var c in caricoSx) sx += c.d.valore;
            foreach (var c in pesiDx) dx += c.d.valore;
            return sx - dx;
        }

        void TogglePeso(Run r)
        {
            if (Meccanismo(def) != "bilancia" || r.go == null) return;
            bool sinistra = r.d.gruppo == "sx";
            var pan = sinistra ? piattoSx : piattoDx;
            if (pan == null) return;
            if (r.stato) Riponi(r);
            else
            {
                if (sinistra) while (caricoSx.Count > 0) Riponi(caricoSx[0]); // un solo carico per volta sul piatto del peso da misurare
                int k = sinistra ? 0 : pesiDx.Count;
                r.go.transform.SetParent(pan, false);
                r.go.transform.localRotation = Quaternion.identity;
                r.go.transform.localPosition = new Vector3(sinistra ? 0f : (k - 1.5f) * 0.075f, sinistra ? 0.07f : 0.03f, 0f);
                r.stato = true;
                (sinistra ? caricoSx : pesiDx).Add(r);
                UISfx.Play(Sfx.ClickBrass, 0.4f);
            }
            Pesa();
        }

        void Riponi(Run r)
        {
            Transform g;
            r.go.transform.SetParent(origGenitore.TryGetValue(r.d.id, out g) && g != null ? g : objRoot, false);
            r.go.transform.localPosition = r.pos0;
            r.go.transform.localRotation = r.rot0;
            r.stato = false;
            caricoSx.Remove(r); pesiDx.Remove(r);
        }

        void Pesa()
        {
            float diff = Peso();
            SbilanciamentoGrammi = diff;
            angoloBilObiettivo = Mathf.Clamp(diff / 25f, -14f, 14f);
            if (caricoSx.Count == 0 || pesiDx.Count == 0) return;
            var c = caricoSx[0];
            if (Mathf.Abs(diff) < 0.5f)
            {
                if (chiodoPresente) { Info(S(3)); return; }
                Info(string.Format(S(1), c.d.msg, c.d.valore));
                if (!string.IsNullOrEmpty(c.d.evento) && c.d.evento.StartsWith("pesata")) Registra(c.d.evento);
            }
            else if (c.d.id == "campione" && chiodoPresente && Mathf.Abs(diff - def.squilibrio) < 0.5f) Info(S(0)); // il peso campione contro il peso giusto: pende, la bilancia sbaglia
        }

        void ToglieChiodo(Run r)
        {
            if (!chiodoPresente) return;
            chiodoPresente = false;
            StartCoroutine(Sparisci(r));
            UISfx.Play(Sfx.Chime, 0.4f);
            Info(!string.IsNullOrEmpty(r.d.msg) ? r.d.msg : S(2));
            Registra("chiodo_tolto");
            Pesa();
        }

        // ------------------------------------------------------------------ monete
        readonly List<Run> sulBanco = new List<Run>();
        TextMeshPro totaleTesto;

        void BuildMonete()
        {
            Run r;
            if (run.TryGetValue("totale", out r)) totaleTesto = r.go.GetComponent<TextMeshPro>();
            AggiornaTotale();
        }

        void BuildMoneteUi()
        {
            var ok = UIFactory.MakeButton(ui, S(0), () => ConfermaStracci(), new Vector2(240f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(240f, 64f));
            ok.gameObject.name = "PagaMonete";
        }

        public int TotaleBanco { get { int t = 0; foreach (var c in sulBanco) t += Mathf.RoundToInt(c.d.valore); return t; } }

        void AggiornaTotale()
        {
            if (totaleTesto != null) totaleTesto.text = string.Format(S(1), TotaleBanco);
        }

        void ToggleMoneta(Run r)
        {
            if (Meccanismo(def) != "monete" || r.go == null) return;
            Run banco;
            if (!run.TryGetValue("banco", out banco)) return;
            if (r.stato) { sulBanco.Remove(r); r.stato = false; StartCoroutine(Sposta(r.go.transform, r.pos0, 0.2f)); }
            else
            {
                int k = sulBanco.Count;
                Vector3 b = banco.pos0;
                Vector3 dest = new Vector3(b.x + (k % 8 - 3.5f) * 0.12f, b.y + 0.012f, b.z + (k / 8 - 1f) * 0.12f);
                sulBanco.Add(r); r.stato = true;
                StartCoroutine(Sposta(r.go.transform, dest, 0.2f));
                UISfx.Play(Sfx.Tick, 0.5f);
            }
            AggiornaTotale();
        }

        bool PagaMonete()
        {
            int target;
            int.TryParse(def.codice, out target);
            int t = TotaleBanco;
            if (t == target) { Solved(); return true; }
            Wrong(string.Format(t < target ? S(2) : S(3), t));
            return false;
        }

        // ------------------------------------------------------------------ colonne
        int[] colMest, colDir;
        readonly List<Button> colMestBtn = new List<Button>(), colDirBtn = new List<Button>();

        void BuildColonneUi()
        {
            int n = def.righe.Count;
            colMest = new int[n]; colDir = new int[n];
            var area = UIFactory.MakePanel(ui, "Colonne", new Color(0.07f, 0.05f, 0.035f, 0.80f), false);
            UIFactory.Place(area.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(1230f, 80f + n * 66f));
            for (int c = 0; c < 3; c++)
            {
                var h = UIFactory.MakeText(area.rectTransform, S(c), 22, UITheme.OttoneLuce, TextAlignmentOptions.Center, FontStyles.Bold);
                UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f + (c == 0 ? 0f : c == 1 ? 350f : 770f), -8f), new Vector2(c == 0 ? 330f : c == 1 ? 400f : 440f, 36f));
            }
            for (int i = 0; i < n; i++)
            {
                int ii = i;
                float y = -52f - i * 66f;
                var lab = UIFactory.MakeText(area.rectTransform, def.righe[i].oggetto, 22, UITheme.CartaChiara, TextAlignmentOptions.Left);
                UIFactory.Place(lab.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, y), new Vector2(320f, 58f));
                lab.enableAutoSizing = true; lab.fontSizeMin = 13; lab.fontSizeMax = 22;
                var bm = UIFactory.MakeButton(area.rectTransform, "?", () => CiclaColonna(ii, 1, 1), new Vector2(400f, 58f), ButtonStyle.Tag, 21f);
                UIFactory.Place((RectTransform)bm.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(350f, y), new Vector2(400f, 58f));
                bm.gameObject.name = "Mestiere_" + i;
                var bd = UIFactory.MakeButton(area.rectTransform, "?", () => CiclaColonna(ii, 2, 1), new Vector2(440f, 58f), ButtonStyle.Tag, 19f);
                UIFactory.Place((RectTransform)bd.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(770f, y), new Vector2(440f, 58f));
                bd.gameObject.name = "Diritto_" + i;
                colMestBtn.Add(bm); colDirBtn.Add(bd);
            }
            var ok = UIFactory.MakeButton(ui, T.btnConferma, () => ConfermaStracci(), new Vector2(240f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(240f, 64f));
            ok.gameObject.name = "ConfermaColonne";
        }

        void CiclaColonna(int riga, int colonna, int delta)
        {
            var lista = colonna == 1 ? def.mestieri : def.diritti;
            var v = colonna == 1 ? colMest : colDir;
            int m = lista.Count + 1;
            v[riga] = ((v[riga] + delta) % m + m) % m;
            UIFactory.SetLabel(colonna == 1 ? colMestBtn[riga] : colDirBtn[riga], v[riga] == 0 ? "?" : lista[v[riga] - 1]);
            UISfx.Play(Sfx.Tick, 0.5f);
        }

        /// <summary>Prova: imposta una riga (indici 0-based delle liste; -1 = nessuna scelta).</summary>
        public void ImpostaRiga(int riga, int mestiere, int diritto)
        {
            if (colMest == null) return;
            colMest[riga] = mestiere + 1; colDir[riga] = diritto + 1;
            UIFactory.SetLabel(colMestBtn[riga], mestiere < 0 ? "?" : def.mestieri[mestiere]);
            UIFactory.SetLabel(colDirBtn[riga], diritto < 0 ? "?" : def.diritti[diritto]);
        }

        bool ConfermaColonne()
        {
            if (colMest == null) return false;
            int n = def.righe.Count, m = 0, d = 0;
            for (int i = 0; i < n; i++)
            {
                if (colMest[i] > 0 && def.mestieri[colMest[i] - 1] == def.righe[i].mestiere) m++;
                if (colDir[i] > 0 && def.diritti[colDir[i] - 1] == def.righe[i].diritto) d++;
            }
            if (m == n && d == n) { Solved(); return true; }
            Wrong(string.Format(S(3), m, d, n));
            return false;
        }
    }
}
