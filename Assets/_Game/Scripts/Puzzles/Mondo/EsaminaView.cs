using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R009/R020: vista ravvicinata di un oggetto d'epoca (alla The Room). Una camera propria inquadra, lontano dal mondo, la copia ingrandita
    /// dell'oggetto: si ruota trascinando, si zooma con la rotella, si usa cliccando le parti (cassetti, coperchi, rotelle, fogli, assi).
    /// Meccanismi: oggetto (azioni sui pezzi e sequenza di eventi), ruote (lucchetto/calendario a cifre), deduzione (scarpe dei passanti), documenti (leggere e indicare la riga).
    /// Errore = messaggio gentile e riprova; Indizio a tre livelli; Esc per uscire. Con tastiera: Tab sceglie la parte, Invio la usa, frecce ruotano.
    /// </summary>
    public class EsaminaView : MonoBehaviour
    {
        public static EsaminaView Current { get; private set; }

        class Run { public ParteDef d; public GameObject go; public bool stato; public Vector3 pos0; public Quaternion rot0; public bool visibile; public int cifra; public bool premuta, segnata; }

        EnigmaDef def;
        Action onSolved;
        Camera cam, mainCam;
        Transform stage, pivot, objRoot;
        readonly List<GameObject> extra = new List<GameObject>();
        readonly Dictionary<string, Run> run = new Dictionary<string, Run>();
        readonly List<Run> ruoteRun = new List<Run>();
        readonly HashSet<string> fatti = new HashSet<string>();
        int prog, hintLevel, focus = -1;
        bool solved, closed, dragging, startUi;
        float yaw, pitch, dist, distBase;
        Vector2 downPos;
        string inMano, inventarioMsg = "";
        Run hover;

        // UI
        RectTransform ui;
        TextMeshProUGUI feedback, manoLabel;
        RectTransform feedbackPlate;
        Button hintBtn, closeBtn, actionBtn, consegnaBtn;
        RectTransform manoRow;
        readonly List<Button> manoBtns = new List<Button>();
        PanelFx fx;
        Action actionCb;

        // deduzione
        int[] assegnati;
        readonly List<TextMeshPro> etichette = new List<TextMeshPro>();
        // documenti
        int qDoc;

        /// <summary>Preset Bassa e WebGL: la vista ravvicinata spegne SMAA e ombre morbide della sua luce.</summary>
        static bool QualitaBassa { get { return Application.platform == RuntimePlatform.WebGLPlayer || GraphicsPresets.Current == 0; } }
        static EnigmiTesti T { get { return GameData.TestiEnigmi; } }
        public EnigmaDef Def { get { return def; } }
        public bool Risolto { get { return solved; } }
        public bool Chiusa { get { return closed; } }
        public string Mano { get { return inMano; } }
        public string CodiceRuote { get { return Codice(); } }

        static readonly Vector3 StagePos = new Vector3(0f, 3000f, 0f);

        public static EsaminaView Apri(EnigmaDef d, Action risolto)
        {
            if (Current != null || UIRoot.I == null) return null;
            var go = new GameObject("EsaminaView");
            var v = go.AddComponent<EsaminaView>();
            v.Init(d, risolto);
            Current = v;
            return v;
        }

        // ------------------------------------------------------------------ costruzione
        void Init(EnigmaDef d, Action risolto)
        {
            def = d;
            onSolved = risolto;
            if (GameManager.I != null) GameManager.I.PushBlock("esamina");
            UIRoot.I.PushModal();
            UIRoot.I.HidePrompt();

            mainCam = Camera.main;
            if (mainCam != null) mainCam.enabled = false;

            stage = new GameObject("EsaminaStage").transform;
            stage.position = StagePos;
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(stage, false);
            objRoot = new GameObject("Oggetto").transform;
            objRoot.SetParent(pivot, false);

            BuildScene();
            BuildCamera();
            BuildUi();
            Inquadra();
            fx.Show();
            AggiornaMano();
            Info(string.IsNullOrEmpty(def.consegna) ? "" : def.consegna);
        }

        void BuildScene()
        {
            var mappa = Meccanismo(def) == "deduzione" ? new Dictionary<string, GameObject>() : MondoFab.Oggetto(def, objRoot, true);
            foreach (var kv in mappa)
            {
                var pd = def.parti.Find(p => p.id == kv.Key);
                var r = new Run { d = pd, go = kv.Value, pos0 = kv.Value.transform.localPosition, rot0 = kv.Value.transform.localRotation, visibile = !pd.nascosta };
                kv.Value.SetActive(r.visibile);
                run[kv.Key] = r;
            }
            string mec = Meccanismo(def);
            if (mec == "ruote") BuildRuote();
            if (mec == "deduzione") BuildDeduzione();

            // luci: una calda dall'alto a sinistra e una di riempimento, solo per questa vista
            var lk = new GameObject("Luce");
            lk.transform.SetParent(stage, false);
            lk.transform.localPosition = new Vector3(-1.2f, 1.6f, -1.8f);
            lk.transform.localRotation = Quaternion.Euler(38f, 32f, 0f);
            var l = lk.AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(1f, 0.86f, 0.64f); l.intensity = 1.0f; l.shadows = QualitaBassa ? LightShadows.None : LightShadows.Soft;
            var lf = new GameObject("Riempimento");
            lf.transform.SetParent(stage, false);
            lf.transform.localPosition = new Vector3(1.0f, 0.4f, -1.6f);
            var pl = lf.AddComponent<Light>();
            pl.type = LightType.Point; pl.color = new Color(0.8f, 0.68f, 0.52f); pl.intensity = 0.9f; pl.range = 6f; pl.shadows = LightShadows.None;
        }

        public static string Meccanismo(EnigmaDef d)
        {
            if (!string.IsNullOrEmpty(d.meccanismo)) return d.meccanismo.ToLowerInvariant();
            switch ((d.tipo ?? "").ToLowerInvariant())
            {
                case "lucchetto": return "ruote";
                case "ordina": return "ordina";
                case "nascosto": return "oggetto";
                default: return "oggetto";
            }
        }

        void BuildCamera()
        {
            var cg = new GameObject("EsaminaCam");
            cg.transform.SetParent(stage, false);
            cam = cg.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.045f, 0.035f, 0.028f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 30f;
            cam.depth = (mainCam != null ? mainCam.depth : 0f) + 5f;
            var ad = cam.GetUniversalAdditionalCameraData();
            ad.renderPostProcessing = true;
            ad.antialiasing = QualitaBassa ? AntialiasingMode.None : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        Vector3 centro;
        float tanVista = 0.3f;

        void Inquadra()
        {
            // i pezzi nascosti (rivelati piu' tardi) contano nell'inquadratura: li accendo un istante per misurare
            var spenti = new List<GameObject>();
            foreach (var kv in run) if (kv.Value.go != null && !kv.Value.go.activeSelf) { spenti.Add(kv.Value.go); kv.Value.go.SetActive(true); }
            var b = MondoFab.Limiti(objRoot);
            foreach (var g in spenti) g.SetActive(false);
            centro = b.center;
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float asp = Mathf.Max(1.2f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float dv = b.extents.y / tanV, dh = b.extents.x / (tanV * asp);
            // i pannelli occupano un po' di spazio in alto e in basso: tolgo margine
            distBase = Mathf.Max(dv * 1.8f, dh * 1.35f, 0.5f) + b.extents.z + 0.1f;
            tanVista = tanV;
            if (def.distanza > 0.1f) distBase *= def.distanza / 1.6f;
            dist = distBase;
            yaw = 0f; pitch = def.inclina;
            AggiornaCamera();
        }

        void AggiornaCamera()
        {
            // l'oggetto sta fermo e la camera orbita attorno al suo centro (davanti = -z)
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.rotation = rot;
            cam.transform.position = centro + rot * new Vector3(0f, 0f, -dist);
            // l'oggetto sta nella fascia libera fra il titolo e il messaggio: sposto l'inquadratura un po' in basso
            cam.transform.position -= cam.transform.up * (0.09f * 2f * dist * tanVista);
        }

        void BuildUi()
        {
            ui = UIFactory.NewRect("Esamina", UIRoot.I.ModalLayer);
            UIFactory.Stretch(ui);
            fx = ui.gameObject.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Pop;
            var raycastCatcher = UIFactory.MakePanel(ui, "Fondo", new Color(0f, 0f, 0f, 0f), true);
            UIFactory.Stretch(raycastCatcher.rectTransform);

            // titolo in alto a sinistra (la consegna sta nel messaggio in basso e si rilegge con il pulsante "Consegna")
            var plate = UIFactory.MakePanel(ui, "Targa", new Color(0.07f, 0.05f, 0.035f, 0.78f), false);
            UIFactory.Place(plate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -118f), new Vector2(760f, 66f));
            var title = UIFactory.MakeText(plate.rectTransform, def.titolo ?? "", 34, UITheme.OttoneLuce, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Stretch(title.rectTransform, 22f, 6f, 22f, 6f);
            title.enableAutoSizing = true; title.fontSizeMin = 20; title.fontSizeMax = 34;

            // feedback in basso
            var fb = UIFactory.MakePanel(ui, "FeedbackTarga", new Color(0.07f, 0.05f, 0.035f, 0.82f), false);
            UIFactory.Place(fb.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(1180f, 120f));
            feedbackPlate = fb.rectTransform;
            feedback = UIFactory.MakeText(fb.rectTransform, "", 23, UITheme.CartaChiara, TextAlignmentOptions.Center);
            UIFactory.Stretch(feedback.rectTransform, 22f, 6f, 22f, 6f);
            feedback.enableAutoSizing = true; feedback.fontSizeMin = 13; feedback.fontSizeMax = 23;

            bool hasHints = def.indizi != null && def.indizi.Count > 0;
            hintBtn = UIFactory.MakeButton(ui, T.btnIndizio, () => Indizio(), new Vector2(200f, 64f), ButtonStyle.Tag, 26f, hasHints);
            UIFactory.Place((RectTransform)hintBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(200f, 64f));
            var lens = UIFactory.MakeImage(hintBtn.transform, "Lente", UIArt.IconLens(), Color.white);
            UIFactory.Place(lens.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(38f, 38f));
            UIFactory.Stretch(hintBtn.GetComponentInChildren<TextMeshProUGUI>().rectTransform, 50f, 6f, 10f, 6f);
            closeBtn = UIFactory.MakeButton(ui, T.btnChiudi, () => Chiudi(), new Vector2(200f, 64f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)closeBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(244f, 24f), new Vector2(200f, 64f));
            consegnaBtn = UIFactory.MakeButton(ui, T.btnConsegna, () => { if (!solved) Info(def.consegna ?? ""); }, new Vector2(220f, 64f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)consegnaBtn.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(460f, 24f), new Vector2(220f, 64f));
            actionBtn = UIFactory.MakeButton(ui, T.btnAvanti, () => { var cb = actionCb; if (cb != null) cb(); }, new Vector2(280f, 64f), ButtonStyle.Seal, 28f, true);
            UIFactory.Place((RectTransform)actionBtn.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(280f, 64f));
            actionBtn.gameObject.SetActive(false);

            var guida = UIFactory.MakeText(ui, UIRoot.TouchMode ? T.guidaTouch : T.guida, 17,
                new Color(UITheme.CartaChiara.r, UITheme.CartaChiara.g, UITheme.CartaChiara.b, 0.7f), TextAlignmentOptions.Center);
            UIFactory.Place(guida.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(1100f, 28f));

            // oggetti in mano (inventario attivo)
            var mr = UIFactory.NewRect("Mano", ui);
            manoRow = mr;
            UIFactory.Place(mr, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -96f), new Vector2(420f, 220f));
            var vl = mr.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6f; vl.childAlignment = TextAnchor.UpperRight; vl.childControlWidth = false; vl.childControlHeight = false; vl.childForceExpandWidth = false; vl.childForceExpandHeight = false;
            manoLabel = UIFactory.MakeText(mr, "", 20, UITheme.OttoneLuce, TextAlignmentOptions.Right, FontStyles.Italic);
            manoLabel.rectTransform.sizeDelta = new Vector2(400f, 30f);

            string mec = Meccanismo(def);
            if (mec == "deduzione") BuildDeduzioneUi();
            if (mec == "documenti") BuildDocumentiUi();
        }

        // ------------------------------------------------------------------ ruote
        void BuildRuote()
        {
            var rd = def.ruote ?? new RuotaDef();
            int n = Mathf.Max(1, rd.n);
            var mat = MondoFab.Mat("ottone", rd.colore);
            var matRib = MondoFab.Mat("metallo", "#2b2824");
            for (int i = 0; i < n; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                g.name = "ruota" + i;
                g.transform.SetParent(objRoot, false);
                g.transform.localPosition = new Vector3(rd.x + (i - (n - 1) * 0.5f) * rd.passo, rd.y, rd.z);
                g.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                g.transform.localScale = new Vector3(rd.raggio * 2f, rd.larghezza * 0.5f, rd.raggio * 2f);
                g.GetComponent<Renderer>().sharedMaterial = mat;
                var col = g.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var bc = g.AddComponent<BoxCollider>();
                // il cilindro e' ruotato: l'asse locale y e' la larghezza della ruota
                bc.size = new Vector3(1f, 1f, 1f);
                var t = new GameObject("Cifra");
                var tm = t.AddComponent<TextMeshPro>();
                tm.text = "0";
                tm.fontSize = rd.raggio * 14f;
                tm.fontStyle = FontStyles.Bold;
                tm.color = new Color(0.09f, 0.06f, 0.03f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.rectTransform.sizeDelta = new Vector2(rd.larghezza * 1.5f, rd.raggio * 1.6f);
                t.transform.SetParent(objRoot, false);
                t.transform.localPosition = new Vector3(g.transform.localPosition.x, rd.y, rd.z - rd.raggio - 0.002f);
                var su = new GameObject("Su"); var sm = su.AddComponent<TextMeshPro>();
                sm.text = "+"; sm.fontSize = rd.raggio * 9f; sm.color = new Color(0.2f, 0.15f, 0.08f); sm.alignment = TextAlignmentOptions.Center; sm.rectTransform.sizeDelta = new Vector2(0.1f, 0.06f);
                su.transform.SetParent(objRoot, false); su.transform.localPosition = new Vector3(g.transform.localPosition.x, rd.y + rd.raggio * 0.72f, rd.z - rd.raggio * 0.7f - 0.002f);
                su.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                var gi = new GameObject("Giu"); var gm = gi.AddComponent<TextMeshPro>();
                gm.text = "-"; gm.fontSize = rd.raggio * 9f; gm.color = new Color(0.2f, 0.15f, 0.08f); gm.alignment = TextAlignmentOptions.Center; gm.rectTransform.sizeDelta = new Vector2(0.1f, 0.06f);
                gi.transform.SetParent(objRoot, false); gi.transform.localPosition = new Vector3(g.transform.localPosition.x, rd.y - rd.raggio * 0.72f, rd.z - rd.raggio * 0.7f - 0.002f);
                gi.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
                var r = new Run { d = new ParteDef { id = "ruota" + i, clic = true, azione = "ruota_cifra" }, go = g, pos0 = g.transform.localPosition, rot0 = g.transform.localRotation, visibile = true };
                run[r.d.id] = r;
                ruoteRun.Add(r);
                r.go.GetComponent<Renderer>().sharedMaterial = mat;
                tm.name = "Cifra" + i;
                t.name = "Cifra" + i;
            }
        }

        TextMeshPro CifraTesto(int i)
        {
            var t = objRoot.Find("Cifra" + i);
            return t != null ? t.GetComponent<TextMeshPro>() : null;
        }

        string Codice()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ruoteRun.Count; i++) sb.Append(ruoteRun[i].cifra);
            return sb.ToString();
        }

        void GiraRuota(Run r, int delta)
        {
            r.cifra = ((r.cifra + delta) % 10 + 10) % 10;
            int i = ruoteRun.IndexOf(r);
            var tm = CifraTesto(i);
            if (tm != null) tm.text = r.cifra.ToString();
            StartCoroutine(Scatto(r.go.transform, delta));
            UISfx.Play(Sfx.Tick, 0.5f);
        }

        IEnumerator Scatto(Transform t, int delta)
        {
            Quaternion q0 = t.localRotation, q1 = q0 * Quaternion.Euler(0f, delta * 36f, 0f);
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.12f)
            {
                if (t == null) yield break;
                t.localRotation = Quaternion.Slerp(q0, q1, k);
                yield return null;
            }
            if (t != null) t.localRotation = q1;
        }

        public void ImpostaRuote(string cifre)
        {
            for (int i = 0; i < ruoteRun.Count && i < cifre.Length; i++)
            {
                ruoteRun[i].cifra = cifre[i] - '0';
                var tm = CifraTesto(i);
                if (tm != null) tm.text = ruoteRun[i].cifra.ToString();
            }
        }

        // ------------------------------------------------------------------ deduzione
        void BuildDeduzione()
        {
            int n = def.passanti.Count;
            assegnati = new int[n];
            float passo = 0.62f;
            MondoFab.Pezzo(new ParteDef { id = "pedana", forma = "cubo", materiale = "pietra", colore = "#5d554a", x = 0f, y = -0.03f, z = 0.0f, sx = n * passo + 0.1f, sy = 0.05f, sz = 0.7f }, objRoot, false);
            for (int i = 0; i < n; i++)
            {
                var p = def.passanti[i];
                float cx = (i - (n - 1) * 0.5f) * passo;
                var coppia = new GameObject("coppia_" + p.id);
                coppia.transform.SetParent(objRoot, false);
                coppia.transform.localPosition = new Vector3(cx, 0f, 0f);
                var bc = coppia.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.12f, 0f); bc.size = new Vector3(0.58f, 0.3f, 0.62f);
                MondoFab.Scarpa(new ParteDef { id = "sx", testo = p.stile, colore = "", x = 0f, y = 0f, z = 0.12f, ry = -8f, sx = 1.9f, sy = 1.9f, sz = 1.9f }, coppia.transform, false);
                MondoFab.Scarpa(new ParteDef { id = "dx", testo = p.stile, colore = "", x = 0.03f, y = 0f, z = -0.12f, ry = 6f, sx = 1.9f, sy = 1.9f, sz = 1.9f }, coppia.transform, false);
                var t = new GameObject("Etichetta"); var tm = t.AddComponent<TextMeshPro>();
                tm.text = "?"; tm.fontSize = 1.1f; tm.fontStyle = FontStyles.Bold; tm.color = new Color(0.95f, 0.88f, 0.7f); tm.alignment = TextAlignmentOptions.Center; tm.rectTransform.sizeDelta = new Vector2(0.6f, 0.14f);
                t.transform.SetParent(objRoot, false); t.transform.localPosition = new Vector3(cx, -0.07f, -0.42f);
                t.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
                etichette.Add(tm);
                run["pass_" + p.id] = new Run { d = new ParteDef { id = "pass_" + p.id, clic = true, azione = "passante", valore = i }, go = coppia, visibile = true, pos0 = coppia.transform.localPosition, rot0 = coppia.transform.localRotation };
            }
        }

        void BuildDeduzioneUi()
        {
            // elenco dei mestieri in una riga sotto il titolo (non copre le scarpe)
            var lista = UIFactory.MakePanel(ui, "Mestieri", new Color(0.07f, 0.05f, 0.035f, 0.78f), false);
            UIFactory.Place(lista.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -190f), new Vector2(1224f, 48f));
            var t = UIFactory.MakeText(lista.rectTransform, "<b>" + T.mestieri + "</b>  " + string.Join("  ·  ", def.mestieri), 22, UITheme.CartaChiara, TextAlignmentOptions.Left);
            UIFactory.Stretch(t.rectTransform, 18f, 4f, 14f, 4f);
            t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 22;
            var ok = UIFactory.MakeButton(ui, T.btnConferma, () => Conferma(), new Vector2(240f, 64f), ButtonStyle.Brass, 26f, true);
            UIFactory.Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(240f, 64f));
            ok.gameObject.name = "ConfermaDeduzione";
        }

        void CiclaPassante(int i, int delta = 1)
        {
            int m = def.mestieri.Count + 1;
            assegnati[i] = ((assegnati[i] + delta) % m + m) % m;
            etichette[i].text = assegnati[i] == 0 ? "?" : def.mestieri[assegnati[i] - 1];
            Info(def.passanti[i].descrizione + " Che mestiere fa?");
            UISfx.Play(Sfx.Tick, 0.5f);
        }

        public void AssegnaPassante(int i, int mestiereIdx) { assegnati[i] = mestiereIdx + 1; etichette[i].text = def.mestieri[mestiereIdx]; }

        /// <summary>Conferma a gruppi di 3: dice quante sono giuste in ciascun gruppo, mai quali.</summary>
        public bool Conferma()
        {
            if (solved || assegnati == null) return false;
            int n = def.passanti.Count, giuste = 0;
            var gruppi = new List<string>();
            for (int g = 0; g * 3 < n; g++)
            {
                int ok = 0, tot = 0;
                for (int i = g * 3; i < Mathf.Min(n, g * 3 + 3); i++)
                {
                    tot++;
                    int idx = assegnati[i] - 1;
                    if (idx >= 0 && def.mestieri[idx] == def.passanti[i].mestiere) { ok++; giuste++; }
                }
                gruppi.Add(string.Format(T.gruppoFmt, g + 1, ok, tot));
            }
            if (giuste == n) { Registra("deduzione"); return true; }
            Wrong(string.Format(T.deduzioneErrore, string.Join("; ", gruppi)));
            return false;
        }

        // ------------------------------------------------------------------ documenti
        RectTransform docArea;
        readonly List<Button> righeDoc = new List<Button>();
        int rigaSel = -1;

        void BuildDocumentiUi()
        {
            docArea = UIFactory.NewRect("Documenti", ui);
            UIFactory.Place(docArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1220f, 330f));
            var hl = docArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 18f; hl.childAlignment = TextAnchor.UpperCenter; hl.childControlWidth = true; hl.childControlHeight = false; hl.childForceExpandWidth = true; hl.childForceExpandHeight = false;
            for (int d = 0; d < def.documenti.Count; d++)
            {
                var doc = def.documenti[d];
                var card = UIFactory.MakePaper(docArea, "Doc_" + doc.id, Color.white, true);
                card.gameObject.AddComponent<LayoutElement>().minHeight = 320f;
                var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(12, 12, 8, 8); v.spacing = 3f; v.childControlWidth = true; v.childControlHeight = false; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
                var t = UIFactory.MakeText(card.rectTransform, doc.titolo, 22, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
                t.rectTransform.sizeDelta = new Vector2(100f, 34f);
                for (int r = 0; r < doc.righe.Count; r++)
                {
                    int dd = d, rr = r;
                    var b = UIFactory.MakeButton(card.rectTransform, doc.righe[r], () => RispondiDoc(def.documenti[dd].id, rr), new Vector2(100f, 56f), new Color(0.82f, 0.72f, 0.5f, 0.35f), UITheme.Inchiostro, 17);
                    b.gameObject.name = "Riga_" + doc.id + "_" + r;
                    righeDoc.Add(b);
                    var le = b.gameObject.AddComponent<LayoutElement>(); le.minHeight = 40f; le.preferredHeight = 56f;
                    var lt = b.GetComponentInChildren<TextMeshProUGUI>(); lt.enableAutoSizing = true; lt.fontSizeMin = 11; lt.fontSizeMax = 17; lt.alignment = TextAlignmentOptions.Left;
                }
            }
            MostraDomandaDoc();
        }

        void SpostaRiga(int dir)
        {
            if (righeDoc.Count == 0) return;
            rigaSel = rigaSel < 0 ? (dir > 0 ? 0 : righeDoc.Count - 1) : ((rigaSel + dir) % righeDoc.Count + righeDoc.Count) % righeDoc.Count;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(righeDoc[rigaSel].gameObject);
            UISfx.Play(Sfx.Tick, 0.4f);
        }

        void PremiRiga()
        {
            if (rigaSel >= 0 && rigaSel < righeDoc.Count) righeDoc[rigaSel].onClick.Invoke();
        }

        public int RigaSelezionata { get { return rigaSel; } }
        public void SpostaRigaProva(int dir) { SpostaRiga(dir); }
        public void PremiRigaProva() { PremiRiga(); }

        void MostraDomandaDoc()
        {
            var q = def.domande[qDoc];
            Info(string.Format(T.domandaFmt, qDoc + 1, def.domande.Count, q.testo));
        }

        public bool RispondiDoc(string docId, int riga)
        {
            if (solved) return false;
            var q = def.domande[qDoc];
            if (q.documento == docId && q.riga == riga)
            {
                UISfx.Play(Sfx.Chime, 0.5f);
                string ok = T.esatto + " " + (q.spiegazione ?? "");
                if (qDoc >= def.domande.Count - 1) { Registra("documenti"); return true; }
                qDoc++;
                SetFeedback(ok + "\n" + T.prossima + " " + def.domande[qDoc].testo, UITheme.CartaChiara);
                return true;
            }
            Wrong(!string.IsNullOrEmpty(q.erroreMsg) ? q.erroreMsg : null);
            return false;
        }

        // ------------------------------------------------------------------ input
        void Update()
        {
            if (closed) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { if (solved) Finito(); else Chiudi(); return; }
                bool docs = Meccanismo(def) == "documenti";
                if (!docs && kb.leftArrowKey.isPressed) yaw -= 70f * Time.unscaledDeltaTime;
                if (!docs && kb.rightArrowKey.isPressed) yaw += 70f * Time.unscaledDeltaTime;
                if (!docs && kb.upArrowKey.isPressed) pitch = Mathf.Clamp(pitch + 50f * Time.unscaledDeltaTime, -35f, 45f);
                if (!docs && kb.downArrowKey.isPressed) pitch = Mathf.Clamp(pitch - 50f * Time.unscaledDeltaTime, -35f, 45f);
                if (!docs && kb.tabKey.wasPressedThisFrame) CiclaFocus(kb.shiftKey.isPressed ? -1 : 1);
                bool invio = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                if (solved) { if (invio && actionBtn.gameObject.activeSelf) { Finito(); return; } }
                else if (Meccanismo(def) == "documenti")
                {
                    // frecce, Tab e Invio scorrono le righe dei fogli
                    if (kb.tabKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) SpostaRiga(kb.shiftKey.isPressed ? -1 : 1);
                    if (kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) SpostaRiga(-1);
                    if (invio) PremiRiga();
                }
                else
                {
                    if (kb.cKey.wasPressedThisFrame && assegnati != null) Conferma();
                    if (invio) { if (hover == null && assegnati != null) Conferma(); else UsaFocus(); }
                }
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.buttonEast.wasPressedThisFrame) { if (solved) Finito(); else Chiudi(); return; }
                if (solved) { if (gp.buttonSouth.wasPressedThisFrame && actionBtn.gameObject.activeSelf) { Finito(); return; } }
                else
                {
                    Vector2 st = gp.rightStick.ReadValue();
                    yaw += st.x * 90f * Time.unscaledDeltaTime;
                    pitch = Mathf.Clamp(pitch + st.y * 60f * Time.unscaledDeltaTime, -35f, 45f);
                    if (Meccanismo(def) == "documenti")
                    {
                        if (gp.dpad.down.wasPressedThisFrame || gp.dpad.right.wasPressedThisFrame) SpostaRiga(1);
                        if (gp.dpad.up.wasPressedThisFrame || gp.dpad.left.wasPressedThisFrame) SpostaRiga(-1);
                        if (gp.buttonSouth.wasPressedThisFrame) PremiRiga();
                    }
                    else
                    {
                        if (gp.dpad.right.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame) CiclaFocus(1);
                        if (gp.dpad.left.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame) CiclaFocus(-1);
                        if (gp.buttonSouth.wasPressedThisFrame) UsaFocus();
                        if (gp.buttonNorth.wasPressedThisFrame && assegnati != null) Conferma();
                    }
                }
            }
            yaw = Mathf.Clamp(yaw, -110f, 110f);
            var m = Mouse.current;
            if (m != null)
            {
                float sc = m.scroll.ReadValue().y;
                if (Mathf.Abs(sc) > 0.01f) dist = Mathf.Clamp(dist - Mathf.Sign(sc) * distBase * 0.08f, distBase * 0.45f, distBase * 1.4f);
            }
            var ptr = Pointer.current;
            if (ptr != null)
            {
                Vector2 pos = ptr.position.ReadValue();
                bool over = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && SopraUiVero();
                if (ptr.press.wasPressedThisFrame) { downPos = pos; dragging = false; startUi = over; }
                if (ptr.press.isPressed && !startUi)
                {
                    Vector2 d = pos - downPos;
                    if (!dragging && d.magnitude > 9f) dragging = true;
                    if (dragging)
                    {
                        Vector2 delta = ptr.delta.ReadValue();
                        yaw -= delta.x * 0.25f; pitch = Mathf.Clamp(pitch + delta.y * 0.2f, -35f, 45f);
                    }
                }
                if (ptr.press.wasReleasedThisFrame && !dragging && !startUi && !over) ClickSchermo(pos);
                if (!ptr.press.isPressed) AggiornaHover(pos, over);
            }
            AggiornaCamera();
        }

        /// <summary>Il fondo trasparente intercetta i raggi UI ovunque: conta solo se sotto il puntatore c'e' un vero pulsante o pannello (non il fondo).</summary>
        bool SopraUiVero()
        {
            var es = EventSystem.current;
            var ped = new PointerEventData(es) { position = Pointer.current.position.ReadValue() };
            var res = new List<RaycastResult>();
            es.RaycastAll(ped, res);
            for (int i = 0; i < res.Count; i++)
            {
                var g = res[i].gameObject;
                if (g == null || !g.transform.IsChildOf(ui)) continue;
                if (g.name == "Fondo") continue;
                if (g.GetComponentInParent<Button>() != null) return true;
            }
            return false;
        }

        Run Colpito(Vector2 screen, out RaycastHit hit)
        {
            hit = default(RaycastHit);
            if (cam == null) return null;
            Ray ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out hit, 30f, ~0, QueryTriggerInteraction.Collide)) return null;
            if (!hit.collider.transform.IsChildOf(objRoot)) return null;
            Transform t = hit.collider.transform;
            while (t != null && t != objRoot)
            {
                foreach (var kv in run) if (kv.Value.go != null && kv.Value.go.transform == t && kv.Value.d.clic && kv.Value.go.activeSelf) return kv.Value;
                t = t.parent;
            }
            return null;
        }

        void AggiornaHover(Vector2 pos, bool over)
        {
            RaycastHit h;
            var r = over || solved ? null : Colpito(pos, out h);
            if (r == hover) return;
            Evidenzia(hover, false);
            hover = r;
            Evidenzia(hover, true);
        }

        static readonly int Emis = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb;

        void Evidenzia(Run r, bool on)
        {
            if (r == null || r.go == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var rend in r.go.GetComponentsInChildren<Renderer>())
            {
                if (rend is MeshRenderer == false) continue;
                rend.GetPropertyBlock(mpb);
                mpb.SetColor(Emis, on || r.segnata ? (r.segnata ? new Color(0.45f, 0.36f, 0.08f) : new Color(0.30f, 0.20f, 0.07f)) : Color.black);
                rend.SetPropertyBlock(mpb);
            }
        }

        void ClickSchermo(Vector2 pos)
        {
            if (solved) return;
            RaycastHit h;
            var r = Colpito(pos, out h);
            if (r == null) return;
            Usa(r, h);
        }

        List<Run> Cliccabili()
        {
            var l = new List<Run>();
            foreach (var kv in run) if (kv.Value.d.clic && kv.Value.go != null && kv.Value.go.activeSelf) l.Add(kv.Value);
            l.Sort((a, b) => string.CompareOrdinal(a.d.id, b.d.id));
            return l;
        }

        void CiclaFocus(int dir)
        {
            var l = Cliccabili();
            if (l.Count == 0) return;
            Evidenzia(hover, false);
            focus = ((focus + dir) % l.Count + l.Count) % l.Count;
            hover = l[focus];
            Evidenzia(hover, true);
        }

        void UsaFocus()
        {
            if (hover != null && !solved) Usa(hover, default(RaycastHit));
        }

        // ------------------------------------------------------------------ API di prova e di gioco
        public string[] PartiCliccabili()
        {
            var l = Cliccabili();
            var s = new string[l.Count];
            for (int i = 0; i < l.Count; i++) s[i] = l[i].d.id;
            return s;
        }

        public bool Clicca(string id)
        {
            Run r;
            if (!run.TryGetValue(id, out r) || solved || r.go == null || !r.go.activeSelf) return false;
            Usa(r, default(RaycastHit));
            return true;
        }

        /// <summary>Posizione sullo schermo (pixel) del centro di una parte: serve alla prova con il mouse virtuale.</summary>
        public bool PosizioneSchermo(string id, out Vector2 pos)
        {
            pos = Vector2.zero;
            Run r;
            if (!run.TryGetValue(id, out r) || r.go == null) return false;
            var rend = r.go.GetComponentInChildren<Renderer>();
            Vector3 w = rend != null ? rend.bounds.center : r.go.transform.position;
            var sp = cam.WorldToScreenPoint(w);
            pos = new Vector2(sp.x, sp.y);
            return sp.z > 0f;
        }

        public bool Cliccabile(string id) { Run r; return run.TryGetValue(id, out r) && r.go != null && r.go.activeSelf; }

        public void ScegliMano(string token)
        {
            if (!MondoStato.Ha(token)) return;
            inMano = token;
            AggiornaMano();
            Info(string.Format(T.inMano, MondoStato.Nome(token)));
        }

        // ------------------------------------------------------------------ azioni sui pezzi
        void Usa(Run r, RaycastHit hit)
        {
            var d = r.d;
            if (d.azione == "ruota_cifra")
            {
                int dir = 1;
                if (hit.collider != null)
                {
                    Vector3 wp = objRoot.InverseTransformPoint(hit.point);
                    dir = wp.y >= r.pos0.y ? 1 : -1;
                }
                GiraRuota(r, dir);
                return;
            }
            if (d.azione == "passante")
            {
                int i = Mathf.RoundToInt(d.valore);
                int dir = 1;
                if (hit.collider != null && objRoot.InverseTransformPoint(hit.point).x < r.pos0.x - 0.0f && false) dir = -1;
                CiclaPassante(i, dir);
                return;
            }

            if (!string.IsNullOrEmpty(d.richiede))
            {
                // inventario attivo: serve l'oggetto giusto in mano
                if (!MondoStato.Ha(d.richiede))
                {
                    Info(!string.IsNullOrEmpty(d.msg) ? d.msg : T.serveQualcosa);
                    return;
                }
                if (inMano != d.richiede)
                {
                    if (!string.IsNullOrEmpty(inMano) && d.azione == "usa") { Wrong(string.Format(T.nonCalzaNome, MondoStato.Nome(inMano))); return; }
                    if (string.IsNullOrEmpty(inMano) && d.azione == "usa") { Info(T.prendiPrimaGiusto); return; }
                }
            }
            else if (d.azione == "usa" && string.IsNullOrEmpty(inMano))
            {
                Info(T.prendiPrima);
                return;
            }

            switch ((d.azione ?? "").ToLowerInvariant())
            {
                case "leggi":
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg);
                    Registra(Ev(d));
                    break;
                case "gira":
                {
                    var t = Target(r);
                    t.stato = !t.stato;
                    StartCoroutine(Ruota(t.go.transform, t.rot0 * Quaternion.AngleAxis(t.stato ? 180f : 0f, Asse(d.asse)), 0.5f));
                    if (!string.IsNullOrEmpty(d.msg)) Info(t.stato ? d.msg : "");
                    Rivela(d.dopo, t.stato);
                    if (t.stato) Registra(Ev(d));
                    break;
                }
                case "ruota":
                {
                    var t = Target(r);
                    float cur = Mathf.DeltaAngle(0f, t.go.transform.localEulerAngles.z);
                    if (Mathf.Abs(cur) < 0.6f) { Info(d.msg); break; }
                    float next = Mathf.MoveTowards(cur, 0f, Mathf.Abs(d.valore));
                    StartCoroutine(Ruota(t.go.transform, Quaternion.Euler(t.go.transform.localEulerAngles.x, t.go.transform.localEulerAngles.y, next), 0.25f));
                    if (Mathf.Abs(next) < 0.6f)
                    {
                        Info(string.IsNullOrEmpty(d.msg) ? T.dritta : d.msg);
                        Rivela(d.dopo, true);
                        Registra(Ev(d));
                    }
                    else Info(T.quasiDritta);
                    break;
                }
                case "rivela":
                case "nascondi":
                    StartCoroutine(Sparisci(r));
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg);
                    Rivela(d.dopo, true);
                    Registra(Ev(d));
                    break;
                case "segna":
                    if (!r.stato)
                    {
                        r.stato = true;
                        r.segnata = true;
                        Evidenzia(r, true);
                        UISfx.Play(Sfx.Chime, 0.35f);
                        Rivela(d.dopo, true);
                        Registra(Ev(d));
                    }
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg);
                    break;
                case "muovi":
                {
                    var t = Target(r);
                    t.stato = !t.stato;
                    StartCoroutine(Sposta(t.go.transform, t.pos0 + (t.stato ? Dir(d.asse) * d.valore : Vector3.zero), 0.4f));
                    if (!string.IsNullOrEmpty(d.msg)) Info(t.stato ? d.msg : "");
                    Rivela(d.dopo, t.stato);
                    if (t.stato) Registra(Ev(d));
                    break;
                }
                case "premi":
                    if (r.premuta) break;
                    r.premuta = true;
                    StartCoroutine(Sposta(r.go.transform, r.pos0 - Dir(d.asse) * 0.02f, 0.12f));
                    UISfx.Play(Sfx.Chime, 0.35f);
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg);
                    Rivela(d.dopo, true);
                    Registra(Ev(d));
                    break;
                case "prendi":
                    StartCoroutine(Sparisci(r));
                    if (!string.IsNullOrEmpty(d.produce)) { MondoStato.Dai(d.produce); AggiornaMano(); inventarioMsg = " (" + string.Format(T.quadernoOggettoFmt, MondoStato.Nome(d.produce)) + ")"; }
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg + inventarioMsg);
                    inventarioMsg = "";
                    Rivela(d.dopo, true);
                    Registra(Ev(d));
                    break;
                case "usa":
                    if (!string.IsNullOrEmpty(d.richiede) && inMano != d.richiede) { Wrong(T.nonCalza); break; }
                    UISfx.Play(Sfx.Chime, 0.4f);
                    if (!string.IsNullOrEmpty(d.richiede) && d.richiede == inMano) { fatti.Add("usato_" + d.richiede); }
                    inMano = null;
                    AggiornaMano();
                    Info(string.IsNullOrEmpty(d.msg) ? T.calza : d.msg);
                    Rivela(d.dopo, true);
                    r.stato = true;
                    Registra(Ev(d));
                    break;
                case "verifica":
                    if (Codice() == (def.codice ?? "")) { Info(string.IsNullOrEmpty(d.msg) ? T.lucchettoApre : d.msg); Rivela(d.dopo, true); Registra(Ev(d)); }
                    else Wrong(string.IsNullOrEmpty(def.erroreMsg) ? T.lucchettoNo : def.erroreMsg);
                    break;
                case "errore":
                    Wrong(string.IsNullOrEmpty(d.msg) ? T.erroreGenerico : d.msg);
                    break;
                default:
                    if (!string.IsNullOrEmpty(d.msg)) Info(d.msg);
                    break;
            }
        }

        Run Target(Run r)
        {
            Run t;
            if (!string.IsNullOrEmpty(r.d.bersaglio) && run.TryGetValue(r.d.bersaglio, out t)) return t;
            return r;
        }

        static string Ev(ParteDef d) { return string.IsNullOrEmpty(d.evento) ? d.id : d.evento; }

        static Vector3 Dir(string a) { a = (a ?? "y").ToLowerInvariant(); return a == "x" ? Vector3.right : (a == "z" ? Vector3.forward : (a == "-x" ? Vector3.left : (a == "-y" ? Vector3.down : (a == "-z" ? Vector3.back : Vector3.up)))); }
        static Vector3 Asse(string a) { return Dir(a); }

        void Rivela(string dopo, bool mostra)
        {
            if (string.IsNullOrEmpty(dopo)) return;
            foreach (var raw in dopo.Split(','))
            {
                string k = raw.Trim();
                if (k.Length == 0) continue;
                bool nascondi = k.StartsWith("-");
                if (nascondi) k = k.Substring(1);
                foreach (var kv in run)
                {
                    bool corrisponde = k.StartsWith("g:") ? kv.Value.d.gruppo == k.Substring(2) : kv.Key == k;
                    if (!corrisponde || kv.Value.go == null) continue;
                    bool vis = nascondi ? !mostra : mostra;
                    kv.Value.visibile = vis;
                    kv.Value.go.SetActive(vis);
                }
            }
        }

        IEnumerator Ruota(Transform t, Quaternion to, float dur)
        {
            Quaternion from = t.localRotation;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / dur)
            {
                if (t == null) yield break;
                t.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            if (t != null) t.localRotation = to;
        }

        IEnumerator Sposta(Transform t, Vector3 to, float dur)
        {
            Vector3 from = t.localPosition;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / dur)
            {
                if (t == null) yield break;
                t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            if (t != null) t.localPosition = to;
        }

        IEnumerator Sparisci(Run r)
        {
            var col0 = r.go.GetComponentInChildren<Collider>();
            if (col0 != null) col0.enabled = false;
            Vector3 from = r.go.transform.localScale, p0 = r.go.transform.localPosition;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.28f)
            {
                if (r.go == null) yield break;
                r.go.transform.localScale = from * (1f - k * 0.9f);
                r.go.transform.localPosition = p0 + new Vector3(0f, 0.05f * k, -0.12f * k);
                yield return null;
            }
            if (r.go != null) { r.go.SetActive(false); r.go.transform.localScale = from; r.go.transform.localPosition = p0; r.visibile = false; var c = r.go.GetComponentInChildren<Collider>(true); if (c != null) c.enabled = true; }
        }

        // ------------------------------------------------------------------ sequenza e soluzione
        void RivelaQuando(string ev)
        {
            foreach (var kv in run)
                if (kv.Value.d.quando == ev && kv.Value.go != null) { kv.Value.visibile = true; kv.Value.go.SetActive(true); }
        }

        void Registra(string ev)
        {
            if (solved) return;
            var sol = def.soluzione ?? new List<string>();
            if (!def.ordinata || (prog < sol.Count && ev == sol[prog])) RivelaQuando(ev);
            string mec = Meccanismo(def);
            if (sol.Count == 0 && (mec == "deduzione" || mec == "documenti" || mec == "ruote")) { if (ev == "deduzione" || ev == "documenti") { Solved(); } return; }
            if (def.ordinata)
            {
                if (prog < sol.Count && ev == sol[prog]) { prog++; if (prog >= sol.Count) Solved(); }
                else if (sol.Contains(ev))
                {
                    prog = 0;
                    ResetPremute();
                    Wrong(T.erroreOrdine);
                }
                return;
            }
            fatti.Add(ev);
            for (int i = 0; i < sol.Count; i++) if (!fatti.Contains(sol[i])) return;
            Solved();
        }

        void ResetPremute()
        {
            foreach (var kv in run)
                if (kv.Value.premuta && kv.Value.go != null)
                {
                    kv.Value.premuta = false;
                    StartCoroutine(Sposta(kv.Value.go.transform, kv.Value.pos0, 0.2f));
                }
        }

        public int Progresso { get { return def.ordinata ? prog : fatti.Count; } }

        // ------------------------------------------------------------------ messaggi, indizi, chiusura
        void SetFeedback(string text, Color color)
        {
            if (feedback == null) return;
            feedback.color = color;
            Typewriter.Play(feedback, text ?? "", 110f, false);
        }

        void Info(string text) { SetFeedback(text, UITheme.CartaChiara); }

        void Wrong(string specific)
        {
            string m = !string.IsNullOrEmpty(specific) ? specific : (!string.IsNullOrEmpty(def.erroreMsg) ? def.erroreMsg : T.erroreGenerico);
            SetFeedback(m, new Color(0.95f, 0.72f, 0.6f));
            UISfx.Play(Sfx.SoftError, 0.5f);
            if (ui != null) UIFx.Wobble((RectTransform)feedback.transform.parent, 0.8f, 0.5f);
        }

        public string UltimoMessaggio { get { return feedback != null ? feedback.text : ""; } }
        public int LivelloIndizio { get { return hintLevel; } }

        public void Indizio()
        {
            if (solved) return;
            if (def.indizi == null || def.indizi.Count == 0) { Info(T.nessunIndizio); return; }
            hintLevel = Mathf.Min(hintLevel + 1, def.indizi.Count);
            UISfx.Play(Sfx.Sparkle, 0.4f);
            Info(string.Format(T.indizioFmt, hintLevel, def.indizi.Count, def.indizi[hintLevel - 1]));
        }

        void AggiornaMano()
        {
            if (manoRow == null) return;
            foreach (var b in manoBtns) if (b != null) Destroy(b.gameObject);
            manoBtns.Clear();
            var candidati = new List<string>();
            foreach (var kv in run)
            {
                var rq = kv.Value.d.richiede;
                if (!string.IsNullOrEmpty(rq) && MondoStato.Ha(rq) && !candidati.Contains(rq) && !fatti.Contains("usato_" + rq)) candidati.Add(rq);
            }
            candidati.Sort();
            manoLabel.text = candidati.Count > 0 ? T.manoLabel : "";
            foreach (var tok in candidati)
            {
                string t2 = tok;
                var b = UIFactory.MakeButton(manoRow, (inMano == tok ? "> " : "") + MondoStato.Nome(tok), () => ScegliMano(t2), new Vector2(380f, 56f), ButtonStyle.Tag, 22f);
                b.gameObject.name = "Mano_" + tok;
                manoBtns.Add(b);
            }
            if (candidati.Count == 1 && string.IsNullOrEmpty(inMano)) inMano = candidati[0];
        }

        void Solved()
        {
            if (solved) return;
            solved = true;
            AudioManager.Fire("enigma_risolto");
            RivelaQuando("risolto");
            hintBtn.gameObject.SetActive(false);
            closeBtn.gameObject.SetActive(false);
            consegnaBtn.gameObject.SetActive(false);
            Evidenzia(hover, false);
            string t = def.spiegazione ?? "";
            if (!string.IsNullOrEmpty(def.scheda)) t += (t.Length > 0 ? "\n" : "") + def.scheda;
            if (!string.IsNullOrEmpty(def.temaCivico)) t += (t.Length > 0 ? "\n" : "") + T.educazione + " " + def.temaCivico;
            if (t.Length == 0) t = T.completo;
            string text = t;
            PuzzlePanel.SlamStamp(ui, "RISOLTO", UITheme.Oro, new Vector2(330f, 110f), new Vector2(0f, 200f), -9f, 54f, () =>
            {
                if (closed) return;
                feedbackPlate.sizeDelta = new Vector2(1180f, 250f);
                SetFeedback(text, new Color(0.78f, 0.95f, 0.72f));
                actionCb = Finito;
                UIFactory.SetLabel(actionBtn, T.btnAvanti);
                actionBtn.gameObject.SetActive(true);
            });
        }

        public void Finito()
        {
            var cb = onSolved;
            onSolved = null;
            Chiudi();
            if (cb != null) cb();
        }

        public void Chiudi()
        {
            if (closed) return;
            closed = true;
            if (Current == this) Current = null;
            if (mainCam != null) mainCam.enabled = true;
            if (GameManager.I != null) GameManager.I.PopBlock("esamina");
            if (UIRoot.I != null) UIRoot.I.PopModal();
            if (ui != null) { if (fx != null) fx.Hide(() => { if (ui != null) Destroy(ui.gameObject); }); else Destroy(ui.gameObject); }
            if (stage != null) Destroy(stage.gameObject);
            Destroy(gameObject, 0.5f);
        }

        void OnDestroy()
        {
            if (!closed)
            {
                closed = true;
                if (Current == this) Current = null;
                if (mainCam != null) mainCam.enabled = true;
                if (GameManager.I != null) GameManager.I.PopBlock("esamina");
                if (UIRoot.I != null) UIRoot.I.PopModal();
                if (stage != null) Destroy(stage.gameObject);
            }
        }
    }
}
