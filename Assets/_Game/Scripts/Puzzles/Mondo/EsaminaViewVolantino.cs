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
    /// R013 (capitolo "Il volantino del treno"): i meccanismi nuovi della vista ravvicinata.
    ///  - bacheca: bacheca di sughero con le dicerie (a sinistra) e i documenti (a destra). Si tocca una voce, poi il foglio che le risponde: lo spago li collega.
    ///    Alla Obra Dinn, quando due collegamenti sono giusti lo spago diventa rosso e resta fissato (non si dice quali sono sbagliati). Poi si gira il cartellino di ogni voce: vera o falsa.
    ///  - ciclostile: meccanismo fisico a fasi. Si scelgono tre frasi per la matrice (chiare e verificabili), si stende la matrice sul tamburo, si inchiostra,
    ///    si gira la manovella con ritmo regolare: troppo veloce il foglio sbava e si ricomincia.
    ///  - datario: timbro a nastri girevoli (giorno, mese, anno); si timbrano in ordine le date della linea del tempo, l'ultima e' il 2 giugno 1946.
    /// Tutti i testi stanno nel JSON (EnigmaDef.strumenti, collegamenti, frasi, date, mesi, anni, parti.msg/errore). Il codice non contiene testi narrativi.
    /// </summary>
    public partial class EsaminaView
    {
        void BuildVolantino(string mec)
        {
            if (mec == "bacheca") BuildBacheca();
            else if (mec == "ciclostile") BuildCiclostile();
            else if (mec == "datario") BuildDatario();
        }

        void BuildVolantinoUi(string mec)
        {
            if (mec == "ciclostile") BuildCiclostileUi();
            else if (mec == "datario") BuildDatarioUi();
        }

        /// <summary>Azioni dei pezzi dei meccanismi R013. true = gestita.</summary>
        bool UsaVolantino(Run r, RaycastHit hit)
        {
            string mec = Meccanismo(def);
            string az = (r.d.azione ?? "").ToLowerInvariant();
            if (mec == "bacheca")
            {
                if (az == "diceria") { ScegliDiceria(Mathf.RoundToInt(r.d.valore)); return true; }
                if (az == "documento") { ToccaDocumento(r); return true; }
                if (az == "verdetto") { GiraVerdetto(Mathf.RoundToInt(r.d.valore), 1); return true; }
            }
            else if (mec == "ciclostile")
            {
                if (az == "fase") { FaseCiclostile(r); return true; }
                if (az == "manovella") { if (faseCic < 3) Info(!string.IsNullOrEmpty(r.d.errore) ? r.d.errore : S(3)); else GiraManovellaAl(Time.unscaledTime); return true; }
            }
            else if (mec == "datario")
            {
                if (az == "nastro")
                {
                    int dir = 1;
                    if (hit.collider != null)
                    {
                        float yHit = objRoot.InverseTransformPoint(hit.point).y, yC = objRoot.InverseTransformPoint(r.go.transform.position).y;
                        dir = yHit >= yC ? 1 : -1;
                    }
                    GiraNastro(Mathf.RoundToInt(r.d.valore), dir);
                    return true;
                }
                if (az == "timbra") { Timbra(); return true; }
            }
            return false;
        }

        /// <summary>Tasto C, gamepad Nord e pulsante a schermo: Scrivi la matrice (ciclostile), Timbra (datario). true = il meccanismo e' di R013.</summary>
        bool ConfermaVolantino(string mec, out bool esito)
        {
            esito = false;
            if (mec == "ciclostile") { esito = faseCic == 0 && ComponiMatrice(); return true; }
            if (mec == "datario") { esito = Timbra(); return true; }
            if (mec == "bacheca") { esito = false; return true; }
            return false;
        }

        static TextMeshPro ScrittaDi(GameObject g)
        {
            if (g == null) return null;
            var t = g.transform.Find("Scritta");
            return t != null ? t.GetComponent<TextMeshPro>() : g.GetComponent<TextMeshPro>();
        }

        Run CercaRun(string azione, int valore)
        {
            foreach (var kv in run)
                if ((kv.Value.d.azione ?? "").ToLowerInvariant() == azione && Mathf.RoundToInt(kv.Value.d.valore) == valore) return kv.Value;
            return null;
        }

        // ------------------------------------------------------------------ bacheca
        int dicSel = -1;
        string[] colleg;
        bool[] fissato;
        int[] verdetti;
        LineRenderer[] fili;
        Material matFilo, matFiloFisso;

        void BuildBacheca()
        {
            int n = def.collegamenti != null ? def.collegamenti.Count : 0;
            colleg = new string[n]; fissato = new bool[n]; verdetti = new int[n]; fili = new LineRenderer[n];
            matFilo = MondoFab.Mat("stoffa", "#c9b98a");       // spago chiaro: collegamento da verificare
            matFiloFisso = MondoFab.Mat("stoffa", "#a3261c");  // spago rosso: collegamento confermato
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("Filo_" + i);
                g.transform.SetParent(objRoot, false);
                var lr = g.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = lr.endWidth = 0.006f;
                lr.numCapVertices = 2;
                lr.sharedMaterial = matFilo;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                g.SetActive(false); // spento finche' non c'e' un collegamento: cosi' non entra nell'inquadratura (MondoFab.Limiti conta i renderer attivi)
                fili[i] = lr;
                AggiornaVerdetto(i);
            }
        }

        /// <summary>Punto dello spago su un cartoncino: bordo destro (voci) o sinistro (fogli), davanti alla faccia frontale.</summary>
        static Vector3 Spillo(GameObject g, bool destra)
        {
            var rend = g != null ? g.GetComponentInChildren<Renderer>() : null;
            if (rend == null) return g != null ? g.transform.position : Vector3.zero;
            var b = rend.bounds;
            return new Vector3(destra ? b.max.x - 0.015f : b.min.x + 0.015f, b.center.y + b.extents.y * 0.55f, b.min.z - 0.012f);
        }

        void DisegnaFilo(int i)
        {
            var lr = fili[i];
            Run dr = CercaRun("diceria", i), doc;
            if (string.IsNullOrEmpty(colleg[i]) || dr == null || !run.TryGetValue(colleg[i], out doc)) { lr.gameObject.SetActive(false); return; }
            lr.SetPosition(0, Spillo(dr.go, true));
            lr.SetPosition(1, Spillo(doc.go, false));
            lr.sharedMaterial = fissato[i] ? matFiloFisso : matFilo;
            lr.startWidth = lr.endWidth = fissato[i] ? 0.008f : 0.006f;
            lr.gameObject.SetActive(true);
        }

        void ScegliDiceria(int i)
        {
            if (colleg == null || i < 0 || i >= colleg.Length) return;
            var precedente = dicSel >= 0 ? CercaRun("diceria", dicSel) : null;
            if (precedente != null) { precedente.segnata = false; Evidenzia(precedente, false); }
            var r = CercaRun("diceria", i);
            if (fissato[i]) { dicSel = -1; Info((r != null && !string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(3)); return; }
            dicSel = i;
            if (r != null) { r.segnata = true; Evidenzia(r, true); }
            UISfx.Play(Sfx.Tick, 0.5f);
            Info((r != null && !string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(0));
        }

        void ToccaDocumento(Run doc)
        {
            if (dicSel < 0) { Info(!string.IsNullOrEmpty(doc.d.msg) ? doc.d.msg : S(9)); return; } // senza una voce scelta il foglio si legge e basta
            Collega(dicSel, doc.d.id, doc.d.msg);
        }

        string UltimoEsito = "";

        /// <summary>Collega la voce i al foglio docId (sostituisce il collegamento precedente se non e' fissato). Poi fissa a coppie i collegamenti giusti.</summary>
        public bool Collega(int i, string docId, string prefisso = null)
        {
            if (solved || colleg == null || i < 0 || i >= colleg.Length || fissato[i] || !run.ContainsKey(docId)) return false;
            colleg[i] = docId;
            var r = CercaRun("diceria", i);
            if (r != null) { r.segnata = false; Evidenzia(r, false); }
            dicSel = -1;
            UISfx.Play(Sfx.ClickBrass, 0.4f);
            UltimoEsito = S(1);
            FissaACoppie();
            for (int k = 0; k < colleg.Length; k++) DisegnaFilo(k);
            Info((!string.IsNullOrEmpty(prefisso) ? prefisso + "\n" : "") + UltimoEsito);
            ControllaBacheca();
            return true;
        }

        bool Giusto(int i) { return !string.IsNullOrEmpty(colleg[i]) && colleg[i] == def.collegamenti[i].a; }

        void FissaACoppie()
        {
            int n = colleg.Length;
            while (true)
            {
                var giusti = new List<int>();
                int liberi = 0;
                for (int i = 0; i < n; i++) { if (fissato[i]) continue; liberi++; if (Giusto(i)) giusti.Add(i); }
                // due giusti si fissano insieme (non si dice quali erano sbagliati); l'ultimo rimasto, se giusto, si fissa da solo
                if (giusti.Count >= 2) { fissato[giusti[0]] = true; fissato[giusti[1]] = true; }
                else if (giusti.Count == 1 && liberi == 1) fissato[giusti[0]] = true;
                else break;
                UISfx.Play(Sfx.Chime, 0.5f);
                UltimoEsito = S(2);
            }
        }

        public int FiliFissati { get { int c = 0; if (fissato != null) foreach (var f in fissato) if (f) c++; return c; } }
        public int FiliTesi { get { int c = 0; if (colleg != null) foreach (var f in colleg) if (!string.IsNullOrEmpty(f)) c++; return c; } }

        void GiraVerdetto(int i, int delta)
        {
            if (verdetti == null || i < 0 || i >= verdetti.Length || solved) return;
            verdetti[i] = ((verdetti[i] + delta) % 3 + 3) % 3;
            AggiornaVerdetto(i);
            UISfx.Play(Sfx.Tick, 0.5f);
            ControllaBacheca();
        }

        /// <summary>Prova: imposta il verdetto della voce i (0 = ?, 1 = vera, 2 = falsa).</summary>
        public void ImpostaVerdetto(int i, int v)
        {
            if (verdetti == null || i < 0 || i >= verdetti.Length || solved) return;
            verdetti[i] = Mathf.Clamp(v, 0, 2);
            AggiornaVerdetto(i);
            ControllaBacheca();
        }

        void AggiornaVerdetto(int i)
        {
            var r = CercaRun("verdetto", i);
            var tm = r != null ? ScrittaDi(r.go) : null;
            if (tm != null) tm.text = S(5 + verdetti[i]);
        }

        void ControllaBacheca()
        {
            if (solved || fissato == null) return;
            int n = fissato.Length, sbagliati = 0, vuoti = 0;
            for (int i = 0; i < n; i++)
            {
                if (!fissato[i]) return; // prima tutti i fili
                int atteso = (def.collegamenti[i].verdetto ?? "").ToLowerInvariant() == "vera" ? 1 : 2;
                if (verdetti[i] == 0) vuoti++;
                else if (verdetti[i] != atteso) sbagliati++;
            }
            if (sbagliati == 0 && vuoti == 0) { Solved(); return; }
            if (vuoti > 0) { UltimoEsito = S(4); Info(S(4)); return; }
            Wrong(string.Format(S(8), sbagliati));
        }

        // ------------------------------------------------------------------ ciclostile
        int faseCic, giriFatti;
        float ultimoGiro = -99f;
        bool[] scelte;
        RectTransform frasiPanel;
        readonly List<Button> frasiBtn = new List<Button>();
        Button componiBtn;

        public int FaseStampa { get { return faseCic; } }
        public int GiriFatti { get { return giriFatti; } }

        void BuildCiclostile()
        {
            scelte = new bool[def.frasi != null ? def.frasi.Count : 0];
            faseCic = 0; giriFatti = 0;
        }

        void BuildCiclostileUi()
        {
            int n = scelte.Length;
            int righe = (n + 1) / 2;
            var area = UIFactory.MakePanel(ui, "Frasi", new Color(0.07f, 0.05f, 0.035f, 0.82f), false);
            frasiPanel = area.rectTransform;
            UIFactory.Place(frasiPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(1230f, 20f + righe * 66f));
            for (int i = 0; i < n; i++)
            {
                int ii = i;
                var b = UIFactory.MakeButton(frasiPanel, "", () => ScegliFrase(ii), new Vector2(600f, 58f), ButtonStyle.Tag, 19f);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f + (i % 2) * 610f, -10f - (i / 2) * 66f), new Vector2(600f, 58f));
                b.gameObject.name = "Frase_" + i;
                var lt = b.GetComponentInChildren<TextMeshProUGUI>();
                if (lt != null) { lt.enableAutoSizing = true; lt.fontSizeMin = 12; lt.fontSizeMax = 19; }
                frasiBtn.Add(b);
                EtichettaFrase(i);
            }
            componiBtn = UIFactory.MakeButton(ui, S(0), () => ConfermaStracci(), new Vector2(300f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)componiBtn.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(300f, 64f));
            componiBtn.gameObject.name = "ComponiMatrice";
        }

        void EtichettaFrase(int i)
        {
            if (i < 0 || i >= frasiBtn.Count) return;
            UIFactory.SetLabel(frasiBtn[i], (scelte[i] ? "[X]  " : "[  ]  ") + def.frasi[i].testo);
        }

        /// <summary>Sceglie o toglie una frase per la matrice (solo nella prima fase).</summary>
        public void ScegliFrase(int i)
        {
            if (faseCic != 0 || scelte == null || i < 0 || i >= scelte.Length || solved) return;
            scelte[i] = !scelte[i];
            EtichettaFrase(i);
            UISfx.Play(Sfx.Tick, 0.5f);
        }

        /// <summary>Scrivi la matrice: tre frasi, tutte chiare e verificabili. Errore = la spiegazione della prima frase non adatta (mai un giudizio politico).</summary>
        public bool ComponiMatrice()
        {
            if (solved || faseCic != 0 || scelte == null) return false;
            int c = 0, prima = -1;
            for (int i = 0; i < scelte.Length; i++)
            {
                if (!scelte[i]) continue;
                c++;
                if (!def.frasi[i].giusta && prima < 0) prima = i;
            }
            if (c != 3) { Wrong(S(1)); return false; }
            if (prima >= 0) { Wrong(def.frasi[prima].motivo); return false; }
            faseCic = 1;
            if (frasiPanel != null) frasiPanel.gameObject.SetActive(false);
            if (componiBtn != null) componiBtn.gameObject.SetActive(false);
            Rivela("g:scritta", true);
            UISfx.Play(Sfx.Chime, 0.5f);
            Info(S(2));
            return true;
        }

        void FaseCiclostile(Run r)
        {
            int f = Mathf.RoundToInt(r.d.valore);
            if (faseCic < f) { Info(!string.IsNullOrEmpty(r.d.errore) ? r.d.errore : S(3)); return; }
            if (faseCic > f) { Info(S(5)); return; }
            faseCic++;
            if (!string.IsNullOrEmpty(r.d.msg)) Info(r.d.msg);
            Rivela(r.d.dopo, true);
            UISfx.Play(Sfx.Chime, 0.4f);
        }

        /// <summary>Un giro di manovella all'istante t (secondi): troppo vicino al precedente = il foglio sbava e si ricomincia. Pubblico per la prova.</summary>
        public bool GiraManovellaAl(float t)
        {
            if (solved || faseCic < 3) return false;
            float dt = t - ultimoGiro;
            ultimoGiro = t;
            Run tam;
            if (run.TryGetValue("tamburo", out tam) && tam.go != null) StartCoroutine(GiroTamburo(tam));
            Run man;
            if (run.TryGetValue("manovella", out man) && man.go != null) StartCoroutine(GiroTamburo(man));
            if (giriFatti > 0 && dt < def.ritmo)
            {
                giriFatti = 0;
                Rivela("g:fogli", false);
                Rivela("macchia", true);
                Wrong(S(4));
                return false;
            }
            Rivela("macchia", false);
            giriFatti++;
            Rivela("foglio_" + giriFatti, true);
            UISfx.Play(Sfx.Tick, 0.6f);
            Info(string.Format(S(6), giriFatti, def.giri));
            if (giriFatti >= Mathf.Max(1, def.giri)) Solved();
            return true;
        }

        IEnumerator GiroTamburo(Run r)
        {
            var t = r.go.transform;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.35f)
            {
                if (t == null) yield break;
                t.localRotation = r.rot0 * Quaternion.AngleAxis(360f * Mathf.SmoothStep(0f, 1f, k), Vector3.up);
                yield return null;
            }
            if (t != null) t.localRotation = r.rot0;
        }

        // ------------------------------------------------------------------ datario
        int giorno = 1, mese, annoIdx, timbrati;
        readonly TextMeshPro[] nastroTesto = new TextMeshPro[3];

        public int Timbrati { get { return timbrati; } }
        public string DataCorrente { get { return giorno.ToString("00") + "-" + (mese + 1).ToString("00") + "-" + Anno(annoIdx); } }

        string Anno(int i) { return def.anni != null && i >= 0 && i < def.anni.Count ? def.anni[i] : ""; }
        int NAnni { get { return def.anni != null && def.anni.Count > 0 ? def.anni.Count : 1; } }

        void BuildDatario()
        {
            giorno = 1; mese = 0; annoIdx = 0; timbrati = 0;
            var font = MondoFab.Font("");
            for (int k = 0; k < 3; k++)
            {
                var r = CercaRun("nastro", k);
                if (r == null || r.go == null) continue;
                var s = r.go.transform.localScale;
                float sx = Mathf.Max(0.001f, s.x), sy = Mathf.Max(0.001f, s.y), sz = Mathf.Max(0.001f, s.z);
                var g = new GameObject("Valore");
                var tm = g.AddComponent<TextMeshPro>();
                if (font != null) tm.font = font; else tm.fontStyle = FontStyles.Bold;
                tm.fontSize = sy * 6f;
                tm.color = new Color(0.08f, 0.06f, 0.04f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.textWrappingMode = TextWrappingModes.NoWrap;
                tm.rectTransform.sizeDelta = new Vector2(sx * 0.95f, sy * 0.95f);
                g.transform.SetParent(r.go.transform, false);
                g.transform.localPosition = new Vector3(0f, 0f, -0.52f);
                g.transform.localScale = new Vector3(1f / sx, 1f / sy, 1f / sz);
                nastroTesto[k] = tm;
            }
            AggiornaNastri();
        }

        void BuildDatarioUi()
        {
            var ok = UIFactory.MakeButton(ui, S(0), () => ConfermaStracci(), new Vector2(240f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(240f, 64f));
            ok.gameObject.name = "Timbra";
        }

        void AggiornaNastri()
        {
            if (nastroTesto[0] != null) nastroTesto[0].text = giorno.ToString("00");
            if (nastroTesto[1] != null) nastroTesto[1].text = def.mesi != null && mese < def.mesi.Count ? def.mesi[mese] : (mese + 1).ToString("00");
            if (nastroTesto[2] != null) nastroTesto[2].text = Anno(annoIdx);
        }

        void GiraNastro(int k, int dir)
        {
            if (solved) return;
            if (k == 0) giorno = ((giorno - 1 + dir) % 31 + 31) % 31 + 1;
            else if (k == 1) mese = ((mese + dir) % 12 + 12) % 12;
            else annoIdx = ((annoIdx + dir) % NAnni + NAnni) % NAnni;
            AggiornaNastri();
            UISfx.Play(Sfx.Tick, 0.5f);
        }

        /// <summary>Prova: imposta il datario (giorno 1-31, mese 1-12, anno come scritto sul nastro).</summary>
        public void ImpostaData(int g, int m, int a)
        {
            giorno = Mathf.Clamp(g, 1, 31);
            mese = Mathf.Clamp(m - 1, 0, 11);
            int i = def.anni != null ? def.anni.IndexOf(a.ToString()) : -1;
            annoIdx = i >= 0 ? i : 0;
            AggiornaNastri();
        }

        bool DataUguale(DataTimbroDef d) { return d != null && d.giorno == giorno && d.mese == mese + 1 && d.anno.ToString() == Anno(annoIdx); }

        /// <summary>Timbra la data del datario sulla linea del tempo: deve essere la prossima in ordine. Ultima data = enigma risolto.</summary>
        public bool Timbra()
        {
            if (solved || def.date == null || def.date.Count == 0) return false;
            Run manico;
            if (run.TryGetValue("timbro", out manico) && manico.go != null) StartCoroutine(Timbrata(manico));
            var atteso = def.date[timbrati];
            if (DataUguale(atteso))
            {
                timbrati++;
                if (!string.IsNullOrEmpty(atteso.parte)) Rivela(atteso.parte, true);
                UISfx.Play(Sfx.ClickBrass, 0.6f);
                if (timbrati >= def.date.Count) { Solved(); return true; }
                Info(string.Format(S(1), atteso.etichetta));
                return true;
            }
            for (int i = 0; i < def.date.Count; i++)
                if (i != timbrati && DataUguale(def.date[i])) { Wrong(i < timbrati ? S(4) : S(2)); return false; }
            Wrong(S(3));
            return false;
        }

        IEnumerator Timbrata(Run r)
        {
            var t = r.go.transform;
            Vector3 p0 = r.pos0, giu = p0 + Vector3.down * 0.06f;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.25f)
            {
                if (t == null) yield break;
                t.localPosition = Vector3.Lerp(p0, giu, Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (t != null) t.localPosition = p0;
        }
    }
}
