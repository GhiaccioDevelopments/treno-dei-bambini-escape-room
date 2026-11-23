using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R014 (capitolo "Il cappotto"): i meccanismi nuovi della vista ravvicinata.
    ///  - cartellini: deduzione su figure vive. I bambini della fila (e il riflesso di Amerigo nel vetro) si muovono appena; i cartellini sul tavolo hanno solo il numero.
    ///    Dal registro (numero, eta', segni particolari) si capisce di chi e' ogni cartellino: si tocca il cartellino, poi il bambino, e glielo si appende al collo.
    ///    Quando tutti hanno un cartellino si dice quanti sono sbagliati (non quali).
    ///  - cappotti: abbinare per taglia e dettagli. Si tocca un cappotto del mucchio e poi un bambino: se e' giusto lo indossa, altrimenti si dice perche' (troppo grande,
    ///    troppo piccolo, misura giusta ma numero di un altro).
    ///  - finestrino: enigma in movimento. Dietro il finestrino scorrono campi, pali e cartelli; quando passa il cartello di una citta' lo si appunta sulla carta
    ///    (pulsante o tasto C). Se una citta' passa senza essere appuntata il controllore la ripete (nessuna penalita'). La luce va dal giorno alla notte.
    ///  - dialogo: scelte nei dialoghi. Ogni bambino dice la sua paura; Amerigo sceglie come rispondere. Una risposta che non aiuta non e' un errore: il bambino resta triste.
    /// Tutti i testi stanno nel JSON (EnigmaDef.strumenti, figure, fermate, parti.msg). Il codice non contiene testi narrativi.
    /// </summary>
    public partial class EsaminaView
    {
        void BuildCappotto(string mec)
        {
            if (mec != "cartellini" && mec != "cappotti" && mec != "finestrino" && mec != "dialogo") return;
            // i pezzi del gruppo "vista" stanno solo nella vista ravvicinata (nel mondo ci sono quelli veri: pareti, panche, finestrino; o i bambini stanno altrove)
            Rivela("g:vista", true);
            if (mec == "cartellini" || mec == "cappotti") BuildFigure();
            else if (mec == "finestrino") BuildFinestrino();
            else if (mec == "dialogo") BuildDialogo();
        }

        void BuildCappottoUi(string mec)
        {
            if (mec == "finestrino") BuildFinestrinoUi();
            else if (mec == "dialogo") BuildDialogoUi();
        }

        /// <summary>Azioni dei pezzi dei meccanismi R014. true = gestita.</summary>
        bool UsaCappotto(Run r, RaycastHit hit)
        {
            string mec = Meccanismo(def);
            string az = (r.d.azione ?? "").ToLowerInvariant();
            if (mec == "cartellini")
            {
                if (az == "cartellino") { ToccaCartellino(r); return true; }
                if (az == "figura") { ToccaFiguraCartellini(Mathf.RoundToInt(r.d.valore)); return true; }
            }
            else if (mec == "cappotti")
            {
                if (az == "cappotto") { ToccaCappotto(r); return true; }
                if (az == "figura") { ToccaFiguraCappotti(Mathf.RoundToInt(r.d.valore)); return true; }
            }
            else if (mec == "finestrino")
            {
                if (az == "cartello") { Appunta(); return true; }
                if (az == "arrivo") { Arriva(); return true; }
            }
            else if (mec == "dialogo")
            {
                if (az == "figura") { ScegliBambino(Mathf.RoundToInt(r.d.valore)); return true; }
            }
            return false;
        }

        /// <summary>Tasto C, gamepad Nord e pulsante a schermo: Appunta (finestrino), Rispondi (dialogo). true = il meccanismo e' di R014.</summary>
        bool ConfermaCappotto(string mec, out bool esito)
        {
            esito = false;
            if (mec == "finestrino") { esito = Appunta(); return true; }
            if (mec == "dialogo") { esito = figSel >= 0 && Rispondi(figSel, rispSel); return true; }
            if (mec == "cartellini" || mec == "cappotti") return true;
            return false;
        }

        /// <summary>Tasti del dialogo (1, 2, 3 scelgono la risposta; frecce su/giu' del gamepad la evidenziano). Chiamato da Update.</summary>
        void TastiCappotto()
        {
            if (solved || def == null || Meccanismo(def) != "dialogo" || figSel < 0) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Rispondi(figSel, 0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Rispondi(figSel, 1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Rispondi(figSel, 2);
            }
            var gp = Gamepad.current;
            if (gp != null && figSel >= 0)
            {
                int n = NRisposte(figSel);
                if (n > 0 && gp.dpad.down.wasPressedThisFrame) { rispSel = (rispSel + 1) % n; EtichetteRisposte(); }
                if (n > 0 && gp.dpad.up.wasPressedThisFrame) { rispSel = (rispSel - 1 + n) % n; EtichetteRisposte(); }
            }
        }

        /// <summary>Oggetto della latta o gettone degli enigmi (il volantino e' nella latta, non fra i gettoni).</summary>
        static bool HaOggetto(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (MondoStato.Ha(id)) return true;
            return GameManager.I != null && GameManager.I.Save.inventario.Contains(id);
        }

        FiguraDef Figura(int j) { return def.figure != null && j >= 0 && j < def.figure.Count ? def.figure[j] : null; }
        int NFigure { get { return def.figure != null ? def.figure.Count : 0; } }

        // ------------------------------------------------------------------ figure vive (cartellini, cappotti, dialogo)
        void BuildFigure()
        {
            // le figure ci sono sempre nella vista ravvicinata, anche se nel mondo stanno nascoste (cappotti: i bambini sono alla fila del Comitato)
            for (int j = 0; j < NFigure; j++)
            {
                var r = CercaRun("figura", j);
                if (r == null || r.go == null) continue;
                r.visibile = true;
                r.go.SetActive(true);
            }
            appesi.Clear(); vestiti.Clear(); selezionato = null;
            StartCoroutine(VitaFigure());
        }

        /// <summary>Le figure respirano e si guardano intorno (poco: restano facili da cliccare).</summary>
        IEnumerator VitaFigure()
        {
            var figure = new List<Run>();
            for (int j = 0; j < NFigure; j++) { var r = CercaRun("figura", j); if (r != null && r.go != null) figure.Add(r); }
            float t0 = Time.unscaledTime;
            while (!closed)
            {
                float t = Time.unscaledTime - t0;
                for (int i = 0; i < figure.Count; i++)
                {
                    var r = figure[i];
                    if (r.go == null) continue;
                    float f = i * 1.9f;
                    r.go.transform.localRotation = r.rot0 * Quaternion.Euler(0f, 5f * Mathf.Sin(t * 0.45f + f) + 2f * Mathf.Sin(t * 1.3f + f * 2f), 0f);
                    r.go.transform.localPosition = r.pos0 + new Vector3(0f, 0.004f * Mathf.Sin(t * 1.6f + f), 0f);
                }
                yield return null;
            }
        }

        string selezionato;                                       // cartellino o cappotto in mano (id della parte)
        readonly Dictionary<int, string> appesi = new Dictionary<int, string>();   // figura -> cartellino appeso
        readonly HashSet<int> vestiti = new HashSet<int>();                        // figure con il cappotto giusto

        public int CartelliniAppesi { get { return appesi.Count; } }
        public int Vestiti { get { return vestiti.Count; } }

        void Seleziona(Run r)
        {
            Run prima;
            if (selezionato != null && run.TryGetValue(selezionato, out prima)) { prima.segnata = false; Evidenzia(prima, false); }
            selezionato = r != null ? r.d.id : null;
            if (r != null) { r.segnata = true; Evidenzia(r, true); UISfx.Play(Sfx.Tick, 0.5f); }
        }

        /// <summary>Rimette un pezzo (cartellino o cappotto) al suo posto sul tavolo.</summary>
        void APosto(Run r)
        {
            if (r == null || r.go == null) return;
            r.go.transform.SetParent(objRoot, false);
            r.go.transform.localPosition = r.pos0;
            r.go.transform.localRotation = r.rot0;
        }

        /// <summary>Mette un pezzo addosso alla figura j (ritto, faccia verso la camera) alla quota locale y.</summary>
        void Addosso(Run pezzo, int j, float y, float z)
        {
            var f = CercaRun("figura", j);
            if (pezzo == null || pezzo.go == null || f == null || f.go == null) return;
            pezzo.go.transform.SetParent(f.go.transform, false);
            pezzo.go.transform.localPosition = new Vector3(0f, y, z);
            pezzo.go.transform.localRotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ cartellini
        void ToccaCartellino(Run r)
        {
            if (solved) return;
            // un cartellino gia' appeso torna in mano (si puo' spostare)
            int da = -1;
            foreach (var kv in appesi) if (kv.Value == r.d.id) da = kv.Key;
            if (da >= 0) { appesi.Remove(da); APosto(r); }
            Seleziona(r);
            Info((!string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(0));
        }

        void ToccaFiguraCartellini(int j)
        {
            if (solved) return;
            var f = Figura(j);
            if (f == null) return;
            var fr = CercaRun("figura", j);
            if (selezionato == null)
            {
                Info((fr != null && !string.IsNullOrEmpty(fr.d.msg) ? fr.d.msg + "\n" : "") + S(1));
                return;
            }
            Appendi(j, selezionato);
        }

        /// <summary>Appende il cartellino alla figura j (quello che c'era torna sul tavolo). Quando tutte le figure hanno un cartellino, controlla. Pubblico per la prova.</summary>
        public bool Appendi(int j, string cartellinoId)
        {
            var f = Figura(j);
            Run c;
            if (solved || f == null || string.IsNullOrEmpty(cartellinoId) || !run.TryGetValue(cartellinoId, out c)) return false;
            // tolto da dove era
            int da = -1;
            foreach (var kv in appesi) if (kv.Value == cartellinoId) da = kv.Key;
            if (da >= 0) appesi.Remove(da);
            string vecchio;
            if (appesi.TryGetValue(j, out vecchio) && vecchio != cartellinoId) { Run rv; if (run.TryGetValue(vecchio, out rv)) APosto(rv); }
            appesi[j] = cartellinoId;
            c.segnata = false; Evidenzia(c, false);
            selezionato = null;
            Addosso(c, j, f.collo, -0.14f);
            UISfx.Play(Sfx.ClickBrass, 0.4f);
            Info(string.Format(S(3), f.nome));
            ControllaCartellini();
            return true;
        }

        void ControllaCartellini()
        {
            if (solved || appesi.Count < NFigure) return;
            int sbagliati = 0;
            for (int j = 0; j < NFigure; j++)
            {
                string a;
                if (!appesi.TryGetValue(j, out a) || a != Figura(j).cartellino) sbagliati++;
            }
            if (sbagliati == 0) { Solved(); return; }
            Wrong(string.Format(S(2), sbagliati));
        }

        // ------------------------------------------------------------------ cappotti
        void ToccaCappotto(Run r)
        {
            if (solved) return;
            foreach (var j in vestiti)
            {
                var f = Figura(j);
                if (f != null && f.cappotto == r.d.id) { Info(!string.IsNullOrEmpty(f.ok) ? f.ok : S(5)); return; }
            }
            Seleziona(r);
            Info((!string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(0));
        }

        void ToccaFiguraCappotti(int j)
        {
            if (solved) return;
            var f = Figura(j);
            if (f == null) return;
            if (vestiti.Contains(j)) { Info(!string.IsNullOrEmpty(f.ok) ? f.ok : S(5)); return; }
            if (selezionato == null)
            {
                var fr = CercaRun("figura", j);
                Info((fr != null && !string.IsNullOrEmpty(fr.d.msg) ? fr.d.msg + "\n" : "") + S(1));
                return;
            }
            Vesti(j, selezionato);
        }

        /// <summary>Prova il cappotto sulla figura j: giusto = lo indossa; altrimenti un messaggio gentile che dice perche'. Pubblico per la prova.</summary>
        public bool Vesti(int j, string cappottoId)
        {
            var f = Figura(j);
            Run c;
            if (solved || f == null || vestiti.Contains(j) || string.IsNullOrEmpty(cappottoId) || !run.TryGetValue(cappottoId, out c)) return false;
            c.segnata = false; Evidenzia(c, false);
            selezionato = null;
            if (cappottoId == f.cappotto)
            {
                vestiti.Add(j);
                Addosso(c, j, f.busto, -0.02f);
                Rivela(f.dopo, true);
                UISfx.Play(Sfx.Chime, 0.5f);
                Info(!string.IsNullOrEmpty(f.ok) ? f.ok : S(5));
                if (vestiti.Count >= NFigure) Solved();
                return true;
            }
            int t = Mathf.RoundToInt(c.d.valore);
            Wrong(t > f.taglia ? S(2) : t < f.taglia ? S(3) : S(4));
            return false;
        }

        // ------------------------------------------------------------------ finestrino
        Transform paesaggio, cartello, cartelloPalo;
        TextMeshPro cartelloTesto;
        readonly List<Transform> pali = new List<Transform>(), colline = new List<Transform>();
        Renderer cielo;
        float tempoViaggio;
        int fermataCorrente = -1, prossimaCitta;
        bool inVista, pendente;
        readonly List<int> ordineCitta = new List<int>();
        readonly HashSet<int> passate = new HashSet<int>();
        Rect vano = new Rect(-0.5f, 1.1f, 0.9f, 0.6f);
        float vanoZ;

        public int CittaAppuntate { get { return prossimaCitta; } }
        public int CittaDaAppuntare { get { return ordineCitta.Count; } }
        public string CartelloInVista { get { return inVista && fermataCorrente >= 0 ? def.fermate[fermataCorrente].testo : ""; } }
        public bool ControlloreHaRipetuto { get { return pendente; } }

        float Periodo { get { return def.periodo > 0.5f ? def.periodo : 7f; } }

        void BuildFinestrino()
        {
            ordineCitta.Clear(); passate.Clear();
            if (def.fermate != null) for (int i = 0; i < def.fermate.Count; i++) if (def.fermate[i].citta) ordineCitta.Add(i);
            prossimaCitta = 0; pendente = false; fermataCorrente = -1; tempoViaggio = 0f;
            // il vano del finestrino: parte "vano" (vuoto) con centro e misura dell'apertura
            Run v;
            if (run.TryGetValue("vano", out v))
            {
                vano = new Rect(v.d.x - v.d.sx * 0.5f, v.d.y - v.d.sy * 0.5f, v.d.sx, v.d.sy);
                vanoZ = v.d.z;
            }
            StartCoroutine(Viaggio()); // la cornice del finestrino e la parete (gruppo "vista") sono gia' accese da BuildCappotto
        }

        void BuildFinestrinoUi()
        {
            var b = UIFactory.MakeButton(ui, S(0), () => ConfermaStracci(), new Vector2(280f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)b.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(280f, 64f));
            b.gameObject.name = "Appunta";
        }

        GameObject Pezzetto(string id, string forma, string mat, string col, Vector3 p, Vector3 s, Transform padre)
        {
            var g = MondoFab.Pezzo(new ParteDef { id = id, forma = forma, materiale = mat, colore = col, x = p.x, y = p.y, z = p.z, sx = s.x, sy = s.y, sz = s.z }, padre, false);
            var rend = g.GetComponent<Renderer>();
            if (rend != null) { rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rend.receiveShadows = false; }
            return g;
        }

        /// <summary>Il paesaggio dietro il finestrino (creato al primo fotogramma, dopo l'inquadratura: non deve allargarla) e il viaggio.</summary>
        IEnumerator Viaggio()
        {
            yield return null;
            if (closed) yield break;
            float cx = vano.center.x, cy = vano.center.y, w = vano.width, h = vano.height;
            paesaggio = new GameObject("Paesaggio").transform;
            paesaggio.SetParent(objRoot, false);
            // cielo, campi, colline lontane, pali del telegrafo vicini: piu' sono vicini piu' corrono
            // (misure contenute: oltre il bordo della parete del vagone, gruppo "vista", gli strati non devono vedersi)
            cielo = Pezzetto("cielo", "cubo", "carta", "#b9c3c4", new Vector3(cx, cy + h * 0.2f, vanoZ + 1.6f), new Vector3(w * 2.2f, h * 1.6f, 0.02f), paesaggio).GetComponent<Renderer>();
            Pezzetto("campi", "cubo", "stoffa", "#6d6a45", new Vector3(cx, cy - h * 0.62f, vanoZ + 0.9f), new Vector3(w * 2.6f, 0.02f, 1.6f), paesaggio);
            for (int i = 0; i < 4; i++)
                colline.Add(Pezzetto("collina_" + i, "sfera", "stoffa", i % 2 == 0 ? "#5b5f44" : "#4e5640", new Vector3(cx, cy - h * 0.45f, vanoZ + 1.45f), new Vector3(w * (0.9f + 0.3f * i), h * 0.5f, 0.1f), paesaggio).transform);
            for (int i = 0; i < 3; i++)
                pali.Add(Pezzetto("palo_" + i, "cilindro", "legno", "#3a2c20", new Vector3(cx, cy, vanoZ + 0.35f), new Vector3(0.025f, h * 0.75f, 0.025f), paesaggio).transform);
            // il cartello della stazione (cliccabile): tavola chiara con il nome, su due paletti
            var cg = MondoFab.Pezzo(new ParteDef { id = "cartello", forma = "cubo", materiale = "legno", colore = "#e3dccb", x = cx, y = cy + h * 0.08f, z = vanoZ + 0.55f,
                sx = w * 0.62f, sy = h * 0.22f, sz = 0.02f, testo = " ", dimensione = h * 0.11f, clic = true, azione = "cartello" }, paesaggio, true);
            cartello = cg.transform;
            cartelloTesto = ScrittaDi(cg);
            cartelloPalo = Pezzetto("cartello_palo", "cubo", "legno", "#2d241b", new Vector3(cx, cy - h * 0.3f, vanoZ + 0.57f), new Vector3(0.025f, h * 0.55f, 0.02f), paesaggio).transform;
            run["cartello"] = new Run { d = new ParteDef { id = "cartello", clic = true, azione = "cartello" }, go = cg, visibile = true, pos0 = cg.transform.localPosition, rot0 = cg.transform.localRotation };
            Color giorno = MondoFab.Hex("#b9c3c4", Color.gray), sera = MondoFab.Hex("#c48a5c", Color.gray), notte = MondoFab.Hex("#1f2633", Color.black);
            var mpb = new MaterialPropertyBlock();
            Light luce = stage != null && stage.Find("Luce") != null ? stage.Find("Luce").GetComponent<Light>() : null;
            float luce0 = luce != null ? luce.intensity : 1f;
            float luceAttuale = 0f;
            while (!closed)
            {
                tempoViaggio += Time.unscaledDeltaTime;
                AggiornaViaggio();
                // pali e colline: scorrono verso sinistra (il treno va verso destra della carta, cioe' verso nord) e ricominciano
                float span = w * 1.6f;
                for (int i = 0; i < pali.Count; i++)
                {
                    float x = Mathf.Repeat(-tempoViaggio * 1.4f + i * span / pali.Count, span) - span * 0.5f;
                    pali[i].localPosition = new Vector3(cx + x, cy - h * 0.05f, vanoZ + 0.35f);
                }
                for (int i = 0; i < colline.Count; i++)
                {
                    float x = Mathf.Repeat(-tempoViaggio * 0.12f + i * span / colline.Count, span) - span * 0.5f;
                    colline[i].localPosition = new Vector3(cx + x, cy - h * 0.45f, vanoZ + 1.45f);
                }
                // luce: dal giorno alla notte con il viaggio (luce della fermata corrente)
                float bersaglio = fermataCorrente >= 0 ? Mathf.Clamp01(def.fermate[fermataCorrente].luce) : 0f;
                luceAttuale = Mathf.MoveTowards(luceAttuale, bersaglio, Time.unscaledDeltaTime * 0.25f);
                Color c = luceAttuale < 0.5f ? Color.Lerp(giorno, sera, luceAttuale * 2f) : Color.Lerp(sera, notte, (luceAttuale - 0.5f) * 2f);
                if (cielo != null)
                {
                    cielo.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", c);
                    cielo.SetPropertyBlock(mpb);
                }
                if (cam != null) cam.backgroundColor = Color.Lerp(new Color(0.045f, 0.035f, 0.028f), c * 0.25f, 0.5f);
                if (luce != null) luce.intensity = luce0 * Mathf.Lerp(1f, 0.55f, luceAttuale);
                yield return null;
            }
        }

        /// <summary>Posizione del cartello e fermata corrente dal tempo di viaggio: ogni fermata attraversa il finestrino in un periodo.</summary>
        void AggiornaViaggio()
        {
            int n = def.fermate != null ? def.fermate.Count : 0;
            if (n == 0) return;
            float per = Periodo;
            int k = Mathf.FloorToInt(tempoViaggio / per);
            float f = tempoViaggio / per - k;
            int idx = ((k % n) + n) % n;
            if (idx != fermataCorrente)
            {
                if (fermataCorrente >= 0) FermataPassata(fermataCorrente);
                fermataCorrente = idx;
                if (cartelloTesto != null) cartelloTesto.text = def.fermate[idx].testo ?? "";
            }
            float cx = vano.center.x, w = vano.width;
            float x = Mathf.Lerp(cx + w * 1.1f, cx - w * 1.1f, f);
            if (cartello != null) cartello.localPosition = new Vector3(x, cartello.localPosition.y, cartello.localPosition.z);
            if (cartelloPalo != null) cartelloPalo.localPosition = new Vector3(x, cartelloPalo.localPosition.y, cartelloPalo.localPosition.z);
            inVista = Mathf.Abs(x - cx) < w * 0.42f;
        }

        /// <summary>Un cartello e' uscito dal finestrino: se era la citta' da appuntare, il controllore la ripete (si puo' appuntare lo stesso).</summary>
        void FermataPassata(int i)
        {
            passate.Add(i);
            if (solved || prossimaCitta >= ordineCitta.Count || ordineCitta[prossimaCitta] != i || pendente) return;
            pendente = true;
            var fm = def.fermate[i];
            Info(!string.IsNullOrEmpty(fm.ripete) ? fm.ripete : S(4));
        }

        /// <summary>Prova: porta il cartello della fermata i al centro del finestrino.</summary>
        public void PortaCartello(int i)
        {
            int n = def.fermate != null ? def.fermate.Count : 0;
            if (n == 0 || i < 0 || i >= n) return;
            float per = Periodo;
            int giro = Mathf.FloorToInt(tempoViaggio / (per * n));
            float t = (giro * n + i + 0.5f) * per;
            if (t < tempoViaggio) t += per * n;
            tempoViaggio = t;
            AggiornaViaggio();
        }

        /// <summary>Appunta sulla carta la citta' del cartello che sta passando (o quella appena ripetuta dal controllore).</summary>
        public bool Appunta()
        {
            if (solved || def.fermate == null || def.fermate.Count == 0) return false;
            if (prossimaCitta >= ordineCitta.Count) { Info(S(5)); return false; }
            int atteso = ordineCitta[prossimaCitta];
            var fm = fermataCorrente >= 0 ? def.fermate[fermataCorrente] : null;
            if ((inVista && fm != null && fermataCorrente == atteso) || pendente)
            {
                var fa = def.fermate[atteso];
                prossimaCitta++;
                pendente = false;
                Rivela(fa.dopo, true);
                UISfx.Play(Sfx.Chime, 0.5f);
                Info(string.Format(S(1), fa.testo) + (prossimaCitta >= ordineCitta.Count ? "\n" + S(5) : ""));
                return true;
            }
            if (inVista && fm != null && !fm.citta) { Wrong(S(2)); return false; }
            if (inVista && fm != null) { Wrong(S(3)); return false; }
            Info(S(6));
            return false;
        }

        /// <summary>L'ultima citta' (gia' stampata sulla carta): si tocca quando tutte le altre sono appuntate.</summary>
        public bool Arriva()
        {
            if (solved) return false;
            if (prossimaCitta < ordineCitta.Count) { Info(S(7)); return false; }
            Solved();
            return true;
        }

        // ------------------------------------------------------------------ dialogo
        int figSel = -1, rispSel;
        bool[] calmi;
        RectTransform rispostePanel;
        readonly List<Button> risposteBtn = new List<Button>();

        public int Calmati { get { int c = 0; if (calmi != null) foreach (var b in calmi) if (b) c++; return c; } }
        public int BambinoScelto { get { return figSel; } }

        int NRisposte(int j) { var f = Figura(j); return f != null && f.risposte != null ? Mathf.Min(3, f.risposte.Count) : 0; }

        void BuildDialogo()
        {
            calmi = new bool[NFigure];
            figSel = -1; rispSel = 0;
            BuildFigure();
        }

        void BuildDialogoUi()
        {
            var area = UIFactory.MakePanel(ui, "Risposte", new Color(0.07f, 0.05f, 0.035f, 0.85f), false);
            rispostePanel = area.rectTransform;
            UIFactory.Place(rispostePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(1000f, 20f + 3 * 66f));
            for (int k = 0; k < 3; k++)
            {
                int kk = k;
                var b = UIFactory.MakeButton(rispostePanel, "", () => { if (figSel >= 0) Rispondi(figSel, kk); }, new Vector2(980f, 58f), ButtonStyle.Tag, 19f);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f - k * 66f), new Vector2(980f, 58f));
                b.gameObject.name = "Risposta_" + k;
                var lt = b.GetComponentInChildren<TextMeshProUGUI>();
                if (lt != null) { lt.enableAutoSizing = true; lt.fontSizeMin = 12; lt.fontSizeMax = 19; }
                risposteBtn.Add(b);
            }
            rispostePanel.gameObject.SetActive(false);
            var ok = UIFactory.MakeButton(ui, S(0), () => ConfermaStracci(), new Vector2(260f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(260f, 64f));
            ok.gameObject.name = "Rispondi";
        }

        void EtichetteRisposte()
        {
            var f = Figura(figSel);
            for (int k = 0; k < risposteBtn.Count; k++)
            {
                bool c = f != null && f.risposte != null && k < f.risposte.Count;
                risposteBtn[k].gameObject.SetActive(c);
                if (c) UIFactory.SetLabel(risposteBtn[k], (k == rispSel ? "> " : "") + (k + 1) + ".  " + f.risposte[k].testo);
            }
        }

        /// <summary>Sceglie il bambino con cui parlare: dice la sua paura e compaiono le risposte. Pubblico per la prova.</summary>
        public void ScegliBambino(int j)
        {
            if (solved || calmi == null || j < 0 || j >= calmi.Length) return;
            var f = Figura(j);
            if (calmi[j]) { Info(f != null ? f.ok : ""); return; }
            var prima = figSel >= 0 ? CercaRun("figura", figSel) : null;
            if (prima != null && !calmi[figSel]) { prima.segnata = false; Evidenzia(prima, false); }
            figSel = j; rispSel = 0;
            var r = CercaRun("figura", j);
            if (r != null) { r.segnata = true; Evidenzia(r, true); }
            UISfx.Play(Sfx.Tick, 0.5f);
            Info(f != null ? f.paura : "");
            if (rispostePanel != null) rispostePanel.gameObject.SetActive(true);
            EtichetteRisposte();
        }

        /// <summary>Amerigo risponde al bambino j con la risposta k. Una risposta che non aiuta non e' un errore: il bambino resta triste. Pubblico per la prova.</summary>
        public bool Rispondi(int j, int k)
        {
            if (solved || calmi == null || j < 0 || j >= calmi.Length) return false;
            var f = Figura(j);
            if (f == null || f.risposte == null || k < 0 || k >= f.risposte.Count) return false;
            if (figSel != j) ScegliBambino(j);
            if (calmi[j]) return false;
            var r = f.risposte[k];
            rispSel = k;
            if (!string.IsNullOrEmpty(r.richiede) && !HaOggetto(r.richiede)) { Info(S(2)); return false; }
            if (!r.giusta)
            {
                UISfx.Play(Sfx.Tick, 0.4f);
                SetFeedback(r.esito + "\n" + S(1), new Color(0.88f, 0.82f, 0.7f)); // il bambino resta triste: niente suono d'errore
                EtichetteRisposte();
                return false;
            }
            calmi[j] = true;
            var fr = CercaRun("figura", j);
            if (fr != null) { fr.segnata = true; Evidenzia(fr, true); }
            Rivela(f.dopo, true);
            UISfx.Play(Sfx.Chime, 0.5f);
            figSel = -1;
            if (rispostePanel != null) rispostePanel.gameObject.SetActive(false);
            if (Calmati >= calmi.Length) { Info(r.esito); Solved(); return true; }
            Info(r.esito + "\n" + S(3));
            return true;
        }
    }
}
