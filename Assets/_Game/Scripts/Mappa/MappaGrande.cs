using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Mappa
{
    /// <summary>
    /// R019: la mappa grande (tasto M, pulsante Mappa, tocco sulla minimappa, Select del gamepad). Un foglio di pianta aperto a schermo,
    /// nord in alto: nomi di tutti i vicoli (si ingrandisce con +/- o la rotella e si sposta trascinando), segni sulla pianta, legenda,
    /// la freccetta di dove ti trovi e il prossimo obiettivo secondo la modalita' di aiuto. Testi da mappe.json.
    /// </summary>
    public class MappaGrande : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public bool IsOpen { get; private set; }

        const float CardW = 1180f, CardH = 650f, VistaW = 500f, VistaH = 580f;

        GameObject view;
        PanelFx fx;
        bool built;
        MappaTesti T;

        RectTransform vista, mondo, pianta;
        RawImage piantaImg;
        TextMeshProUGUI titolo, sottotitolo, obiettivoIntestazione, obiettivoTesto, aiuto;
        Image freccia, croce;
        RectTransform legendaBox;
        readonly List<GameObject> legendaVoci = new List<GameObject>();
        List<EtichettaStrada> etichette = new List<EtichettaStrada>();
        Vector2[] posEt = new Vector2[0]; float[] chiaveEt = new float[0]; bool[] candEt = new bool[0]; bool[] sceltoEt = new bool[0];

        MappaScenaDef def;
        Texture2D tex;
        string scena = "";
        float scalaBase = 1f, zoom = 1f;
        Vector2 fuoco;
        ThirdPersonController giocatore;

        // lettura per le prove
        public float Zoom { get { return zoom; } }
        public int EtichetteVisibili { get { int n = 0; for (int i = 0; i < etichette.Count; i++) if (etichette[i].rt.gameObject.activeSelf) n++; return n; } }
        public int EtichetteTotali { get { return etichette.Count; } }
        public string TestoObiettivo { get { return obiettivoTesto != null ? obiettivoTesto.text : ""; } }
        public bool CroceVisibile { get { return croce != null && croce.gameObject.activeSelf; } }

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;
            fx = view.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Notebook;
            view.SetActive(false);
        }

        // ---------------------------------------------------------------- costruzione
        void Build()
        {
            built = true;
            T = GameData.Mappe.testi;
            var veil = (RectTransform)view.transform;
            var card = UIFactory.MakePaper(veil, "Foglio", Color.white, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW, CardH));
            fx.card = card.rectTransform;
            UIFactory.MakeBrassFrame(card.rectTransform, 8f);

            // cornice della pianta (bordo d'inchiostro) con la vista ritagliata
            var cornice = UIFactory.MakePanel(card.rectTransform, "CorniceMappa", new Color(0.17f, 0.11f, 0.07f, 1f), false);
            UIFactory.Place(cornice.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -4f), new Vector2(VistaW + 12f, VistaH + 12f));
            var vp = UIFactory.MakePanel(cornice.rectTransform, "Ritaglio", UITheme.CartaScura, true);
            UIFactory.Place(vp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VistaW, VistaH));
            vp.gameObject.AddComponent<RectMask2D>();
            vista = vp.rectTransform;
            var drag = vp.gameObject.AddComponent<MappaDrag>();
            drag.mappa = this;
            mondo = UIFactory.NewRect("Mondo", vista);
            mondo.anchorMin = mondo.anchorMax = new Vector2(0.5f, 0.5f);
            mondo.sizeDelta = Vector2.zero;
            pianta = UIFactory.NewRect("Pianta", mondo);
            pianta.anchorMin = pianta.anchorMax = new Vector2(0.5f, 0.5f);
            piantaImg = pianta.gameObject.AddComponent<RawImage>();
            piantaImg.raycastTarget = false;

            freccia = UIFactory.MakeImage(mondo, "Giocatore", MappaArt.Freccia(), Color.white);
            freccia.rectTransform.sizeDelta = new Vector2(30f, 30f);
            croce = UIFactory.MakeImage(mondo, "Obiettivo", MappaArt.Croce(), Color.white);
            croce.rectTransform.sizeDelta = new Vector2(44f, 44f);
            croce.gameObject.SetActive(false);

            // zoom
            var piu = UIFactory.MakeButton(vista, "+", () => CambiaZoom(1.35f), new Vector2(56f, 56f), ButtonStyle.Brass, 34f);
            UIFactory.Place((RectTransform)piu.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 72f), new Vector2(56f, 56f));
            var meno = UIFactory.MakeButton(vista, "−", () => CambiaZoom(1f / 1.35f), new Vector2(56f, 56f), ButtonStyle.Brass, 34f);
            UIFactory.Place((RectTransform)meno.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 10f), new Vector2(56f, 56f));

            // colonna di destra
            titolo = UIFactory.MakeText(card.rectTransform, T.titoloGrande, 40, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(titolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -24f), new Vector2(570f, 52f));
            sottotitolo = UIFactory.MakeText(card.rectTransform, "", 24, UITheme.InchiostroBlu, TextAlignmentOptions.Left, FontStyles.Italic);
            UIFactory.Place(sottotitolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -76f), new Vector2(570f, 34f));
            var filetto = UIFactory.MakePanel(card.rectTransform, "Filetto", new Color(UITheme.Ottone.r, UITheme.Ottone.g, UITheme.Ottone.b, 0.8f), false);
            filetto.sprite = UIArt.White();
            UIFactory.Place(filetto.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -114f), new Vector2(560f, 3f));

            obiettivoIntestazione = UIFactory.MakeText(card.rectTransform, T.obiettivoTitolo, 26, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(obiettivoIntestazione.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -126f), new Vector2(560f, 36f));
            obiettivoTesto = UIFactory.MakeText(card.rectTransform, "", 25, UITheme.Inchiostro, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            UIFactory.Place(obiettivoTesto.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -164f), new Vector2(560f, 96f));
            obiettivoTesto.enableAutoSizing = true; obiettivoTesto.fontSizeMin = 16f; obiettivoTesto.fontSizeMax = 25f;

            var legTitolo = UIFactory.MakeText(card.rectTransform, T.legendaTitolo, 26, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(legTitolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -276f), new Vector2(560f, 36f));
            legendaBox = UIFactory.NewRect("Legenda", card.rectTransform);
            UIFactory.Place(legendaBox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(580f, -318f), new Vector2(560f, 230f));

            aiuto = UIFactory.MakeText(card.rectTransform, "", 20, UITheme.InchiostroBlu, TextAlignmentOptions.BottomLeft, FontStyles.Italic);
            UIFactory.Place(aiuto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(580f, 26f), new Vector2(330f, 30f));
            var chiudi = UIFactory.MakeButton(card.rectTransform, T.chiudi, Hide, new Vector2(220f, 60f), ButtonStyle.Seal, 28f, true);
            UIFactory.Place((RectTransform)chiudi.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 24f), new Vector2(220f, 60f));
        }

        void CostruisciLegenda()
        {
            foreach (var g in legendaVoci) if (g != null) Destroy(g);
            legendaVoci.Clear();
            int i = 0;
            foreach (var v in T.legenda)
            {
                Sprite s = v.glifo == "tu" ? MappaArt.Freccia() : (v.glifo == "obiettivo" ? MappaArt.Croce() : MappaArt.Glifo(v.glifo));
                int col = i % 2, riga = i / 2;
                var row = UIFactory.NewRect("Voce", legendaBox);
                UIFactory.Place(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(col * 280f, -riga * 44f), new Vector2(276f, 42f));
                var im = UIFactory.MakeImage(row, "Segno", s, Color.clear);
                UIFactory.Place(im.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(38f, 38f));
                var t = UIFactory.MakeText(row, v.testo, 22, UITheme.Inchiostro, TextAlignmentOptions.Left);
                UIFactory.Stretch(t.rectTransform, 48f, 0f, 0f, 0f);
                t.enableAutoSizing = true; t.fontSizeMin = 14f; t.fontSizeMax = 22f;
                legendaVoci.Add(row.gameObject);
                i++;
            }
        }

        // ---------------------------------------------------------------- scena
        void CaricaScena(string nome)
        {
            scena = nome;
            def = GameData.MappaScena(nome);
            tex = def != null ? Resources.Load<Texture2D>("Mappe/" + nome) : null;
            for (int i = 0; i < etichette.Count; i++) if (etichette[i].rt != null) Destroy(etichette[i].rt.gameObject);
            etichette.Clear();
            giocatore = null;
            if (def == null || tex == null) return;
            piantaImg.texture = tex;
            float w = def.ax1 - def.ax0, h = def.az1 - def.az0;
            scalaBase = Mathf.Min(VistaW / w, VistaH / h);
            etichette = MappaUi.CreaEtichette(mondo, def, 14f);
            int n = etichette.Count;
            posEt = new Vector2[n]; chiaveEt = new float[n]; candEt = new bool[n]; sceltoEt = new bool[n];
            // freccia e croce restano sopra le etichette
            freccia.rectTransform.SetAsLastSibling();
            croce.rectTransform.SetAsLastSibling();
            sottotitolo.text = def.titolo ?? "";
        }

        public bool Disponibile { get { return GameData.MappaScena(SceneManager.GetActiveScene().name) != null; } }

        public void Show()
        {
            if (IsOpen) return;
            if (!built) Build();
            string s = SceneManager.GetActiveScene().name;
            if (s != scena || tex == null) CaricaScena(s);
            if (def == null || tex == null) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock("mappa");
            if (UIRoot.I != null) UIRoot.I.PushModal();
            CostruisciLegenda();
            zoom = 1f;
            giocatore = FindAnyObjectByType<ThirdPersonController>();
            fuoco = new Vector2((def.ax0 + def.ax1) * 0.5f, (def.az0 + def.az1) * 0.5f);
            aiuto.text = UIRoot.TouchMode ? T.aiutoTouch : T.aiuto;
            transform.SetAsLastSibling();
            fx.Show();
            Aggiorna();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            fx.Hide();
            if (GameManager.I != null) GameManager.I.PopBlock("mappa");
            if (UIRoot.I != null) UIRoot.I.PopModal();
        }

        // ---------------------------------------------------------------- zoom e trascinamento
        void CambiaZoom(float f)
        {
            zoom = Mathf.Clamp(zoom * f, 1f, 4.5f);
            LimitaFuoco();
        }

        void LimitaFuoco()
        {
            if (def == null) return;
            fuoco.x = Mathf.Clamp(fuoco.x, def.ax0, def.ax1);
            fuoco.y = Mathf.Clamp(fuoco.y, def.az0, def.az1);
            if (zoom <= 1.001f) fuoco = new Vector2((def.ax0 + def.ax1) * 0.5f, (def.az0 + def.az1) * 0.5f);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!IsOpen || def == null || zoom <= 1.001f) return;
            float scala = scalaBase * zoom;
            fuoco -= e.delta / scala / (UIRoot.I != null && UIRoot.I.Canvas != null ? UIRoot.I.Canvas.scaleFactor : 1f);
            LimitaFuoco();
        }

        public void OnScroll(PointerEventData e)
        {
            if (!IsOpen) return;
            CambiaZoom(e.scrollDelta.y > 0f ? 1.2f : 1f / 1.2f);
        }

        // ---------------------------------------------------------------- disegno
        void Update()
        {
            if (IsOpen) Aggiorna();
        }

        void Aggiorna()
        {
            if (def == null || tex == null) return;
            float scala = scalaBase * zoom;
            float cx = (def.ax0 + def.ax1) * 0.5f, cz = (def.az0 + def.az1) * 0.5f;
            mondo.anchoredPosition = new Vector2(-(fuoco.x - cx) * scala, -(fuoco.y - cz) * scala);
            pianta.sizeDelta = new Vector2((def.ax1 - def.ax0) * scala, (def.az1 - def.az0) * scala);

            // il giocatore
            if (giocatore == null) giocatore = FindAnyObjectByType<ThirdPersonController>();
            Vector3 p = giocatore != null ? giocatore.transform.position : new Vector3(cx, 0f, cz);
            bool ho = giocatore != null;
            if (freccia.gameObject.activeSelf != ho) freccia.gameObject.SetActive(ho);
            if (ho)
            {
                freccia.rectTransform.anchoredPosition = new Vector2((p.x - cx) * scala, (p.z - cz) * scala);
                freccia.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -giocatore.transform.eulerAngles.y);
            }

            // le etichette: quelle che entrano nella vista senza sovrapporsi
            float mx = VistaW * 0.5f, my = VistaH * 0.5f;
            for (int i = 0; i < etichette.Count; i++)
            {
                var e = etichette[i];
                Vector2 s = new Vector2((e.x - cx) * scala, (e.z - cz) * scala);
                Vector2 sulloSchermo = s + mondo.anchoredPosition;
                posEt[i] = s; chiaveEt[i] = ho ? Mathf.Abs(e.x - p.x) + Mathf.Abs(e.z - p.z) : 0f;
                candEt[i] = Mathf.Abs(sulloSchermo.x) < mx - e.w * 0.5f && Mathf.Abs(sulloSchermo.y) < my - e.h * 0.5f;
            }
            MappaUi.Seleziona(etichette, posEt, chiaveEt, candEt, sceltoEt, 4f);
            for (int i = 0; i < etichette.Count; i++)
            {
                var e = etichette[i];
                bool on = sceltoEt[i];
                if (e.rt.gameObject.activeSelf != on) e.rt.gameObject.SetActive(on);
                if (!on) continue;
                MappaUi.Misura(e);
                e.gruppo.alpha = Mathf.MoveTowards(e.gruppo.alpha, 1f, Time.unscaledDeltaTime * 6f);
                e.rt.anchoredPosition = posEt[i];
            }

            // obiettivo e testo a destra
            var ob = Obiettivi.Corrente();
            int modo = Obiettivi.Modo;
            string testo;
            ObiettivoPuntoDef punto = null;
            if (modo == Obiettivi.Esploratore) testo = T.aiutoSpento;
            else if (ob == null) testo = string.IsNullOrEmpty(T.capitoloFinito) ? T.nessunObiettivo : T.capitoloFinito;
            else if (Obiettivi.Mostrato) { testo = Obiettivi.Testo(ob, scena); punto = Obiettivi.Punto(ob, scena); }
            else testo = T.obiettivoNascosto;
            if (obiettivoTesto.text != testo) obiettivoTesto.text = testo ?? "";
            obiettivoIntestazione.gameObject.SetActive(modo != Obiettivi.Esploratore);

            bool mostraCroce = punto != null;
            if (croce.gameObject.activeSelf != mostraCroce) croce.gameObject.SetActive(mostraCroce);
            if (mostraCroce)
            {
                croce.rectTransform.anchoredPosition = new Vector2((punto.x - cx) * scala, (punto.z - cz) * scala);
                croce.rectTransform.localScale = Vector3.one * (UITheme.ReduceEffects ? 1f : 1f + 0.10f * Mathf.Sin(Time.unscaledTime * 3.2f));
            }
        }
    }

    /// <summary>Raccoglie trascinamento e rotella sul foglio della mappa e li passa a MappaGrande.</summary>
    public class MappaDrag : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public MappaGrande mappa;
        public void OnDrag(PointerEventData e) { if (mappa != null) mappa.OnDrag(e); }
        public void OnScroll(PointerEventData e) { if (mappa != null) mappa.OnScroll(e); }
    }
}
