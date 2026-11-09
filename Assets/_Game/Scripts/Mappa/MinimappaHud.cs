using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Mappa
{
    /// <summary>
    /// R019: minimappa in alto a sinistra. Una piccola pianta d'epoca (carta e inchiostro, generata dall'editor) in una cornice d'ottone:
    /// ruota con la camera (opzione: nord fisso), il giocatore e' la freccetta al centro, i nomi dei vicoli compaiono quando si e' vicini,
    /// il prossimo obiettivo e' una X rossa a matita (o una freccia sul bordo se e' fuori). Sotto: la targa della strada dove ci si trova
    /// e la riga dell'obiettivo. Nessuna camera in piu': e' solo un'immagine che si muove (peso zero).
    /// Cliccandola si apre la mappa grande (come il tasto M).
    /// </summary>
    public class MinimappaHud : MonoBehaviour
    {
        public static MinimappaHud I { get; private set; }

        /// <summary>Per le prove di prestazioni: -senzaminimappa spegne la minimappa (solo editor e build di sviluppo).</summary>
        public static bool Disabilitata
        {
            get
            {
#if UNITY_EDITOR || DEBUG
                foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-senzaminimappa") return true;
#endif
                return false;
            }
        }

        const float Diametro = 216f, DiamMaschera = 188f, Raggio = DiamMaschera * 0.5f;

        RectTransform gruppo, mondo, pianta, targa, rigaObiettivo;
        RawImage piantaImg;
        Image freccia, croce, bordo, anelloImg;
        TextMeshProUGUI nord, targaTesto, obiettivoTesto;
        CanvasGroup targaGruppo, obiettivoGruppo, gruppoAlpha;
        MappaScenaDef def;
        Texture2D tex;
        string scenaCorrente = "";
        float scala = 4.8f;
        List<EtichettaStrada> etichette = new List<EtichettaStrada>();
        Vector2[] posEt = new Vector2[0]; float[] chiaveEt = new float[0]; bool[] candEt = new bool[0]; bool[] sceltoEt = new bool[0];
        ThirdPersonController giocatore;
        string nomeTarga = "";
        float obiettivoAlpha, targaAlpha;

        // ---- stato leggibile dalle prove
        public bool Visibile { get; private set; }
        public Vector2 PosizioneCroce { get { return croce != null ? croce.rectTransform.anchoredPosition : Vector2.zero; } }
        public bool CroceVisibile { get { return croce != null && croce.gameObject.activeSelf; } }
        public bool FrecciaBordoVisibile { get { return bordo != null && bordo.gameObject.activeSelf; } }
        public string TestoObiettivo { get { return obiettivoGruppo != null && obiettivoGruppo.alpha > 0.5f && obiettivoTesto != null ? obiettivoTesto.text : ""; } }
        public string NomeStrada { get { return nomeTarga; } }
        public int EtichetteVisibili { get { int n = 0; for (int i = 0; i < etichette.Count; i++) if (etichette[i].rt.gameObject.activeSelf) n++; return n; } }
        public RectTransform Gruppo { get { return gruppo; } }

        public static MinimappaHud Create(RectTransform parent)
        {
            // il componente sta su una radice sempre attiva; il gruppo figlio si accende e si spegne
            var radice = UIFactory.NewRect("MinimappaRadice", parent);
            UIFactory.Stretch(radice);
            var h = radice.gameObject.AddComponent<MinimappaHud>();
            h.gruppo = UIFactory.NewRect("Minimappa", radice);
            h.Build();
            I = h;
            return h;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Build()
        {
            UIFactory.Place(gruppo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(Diametro + 90f, Diametro + 130f));
            gruppo.gameObject.AddComponent<Canvas>();           // layer proprio: la pianta che ruota non fa rifare il resto dell'HUD
            gruppo.gameObject.AddComponent<GraphicRaycaster>();
            gruppoAlpha = gruppo.gameObject.AddComponent<CanvasGroup>();

            // anello d'ottone + fondo scuro
            anelloImg = UIFactory.MakePanel(gruppo, "Anello", UITheme.Ottone, true);
            anelloImg.sprite = UIFactory.Circle();
            UIFactory.Place(anelloImg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(Diametro, Diametro));
            var btn = anelloImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = anelloImg; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { if (UIRoot.I != null) UIRoot.I.ToggleMappa(); });
            var scuro = UIFactory.MakePanel(anelloImg.rectTransform, "Bordo", new Color(0.16f, 0.1f, 0.06f, 1f), false);
            scuro.sprite = UIFactory.Circle();
            UIFactory.Place(scuro.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(DiamMaschera + 8f, DiamMaschera + 8f));

            // maschera circolare
            var maschera = UIFactory.MakePanel(anelloImg.rectTransform, "Maschera", Color.white, false);
            maschera.sprite = UIFactory.Circle();
            UIFactory.Place(maschera.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(DiamMaschera, DiamMaschera));
            var mask = maschera.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            mondo = UIFactory.NewRect("Mondo", maschera.rectTransform);
            mondo.anchorMin = mondo.anchorMax = new Vector2(0.5f, 0.5f);
            mondo.sizeDelta = Vector2.zero;
            var fondo = UIFactory.MakePanel(mondo, "Carta", UITheme.CartaScura, false);
            UIFactory.Place(fondo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));
            pianta = UIFactory.NewRect("Pianta", mondo);
            pianta.anchorMin = pianta.anchorMax = new Vector2(0.5f, 0.5f);
            piantaImg = pianta.gameObject.AddComponent<RawImage>();
            piantaImg.raycastTarget = false;

            // cartellino del giocatore, segno dell'obiettivo e freccia sul bordo (non ruotano con la mappa)
            freccia = UIFactory.MakeImage(maschera.rectTransform, "Giocatore", MappaArt.Freccia(), Color.white);
            UIFactory.Place(freccia.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            croce = UIFactory.MakeImage(maschera.rectTransform, "Obiettivo", MappaArt.Croce(), Color.white);
            UIFactory.Place(croce.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            bordo = UIFactory.MakeImage(maschera.rectTransform, "ObiettivoBordo", MappaArt.Bordo(), Color.white);
            UIFactory.Place(bordo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            croce.gameObject.SetActive(false); bordo.gameObject.SetActive(false);

            // piccola N sull'anello d'ottone
            nord = UIFactory.MakeText(anelloImg.rectTransform, GameData.Mappe.testi.nord, 15, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            nord.rectTransform.anchorMin = nord.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            nord.rectTransform.sizeDelta = new Vector2(22f, 20f);

            // targa del nome della strada, sotto la mappa
            var tg = UIFactory.MakePanel(gruppo, "Targa", Color.white, false);
            tg.sprite = UIArt.Brass(); tg.type = Image.Type.Sliced;
            targa = tg.rectTransform;
            UIFactory.Place(targa, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -Diametro - 4f), new Vector2(Diametro + 20f, 34f));
            targaTesto = UIFactory.MakeText(targa, "", 20, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Stretch(targaTesto.rectTransform, 10f, 2f, 10f, 2f);
            targaTesto.enableAutoSizing = true; targaTesto.fontSizeMin = 12f; targaTesto.fontSizeMax = 20f;
            targaGruppo = targa.gameObject.AddComponent<CanvasGroup>();
            targaGruppo.alpha = 0f;

            // riga dell'obiettivo
            var ro = UIFactory.MakePanel(gruppo, "RigaObiettivo", Color.white, false);
            ro.sprite = UIArt.Tag(); ro.type = Image.Type.Sliced;
            rigaObiettivo = ro.rectTransform;
            UIFactory.Place(rigaObiettivo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -Diametro - 44f), new Vector2(Diametro + 70f, 62f));
            obiettivoTesto = UIFactory.MakeText(rigaObiettivo, "", 19, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Italic | FontStyles.Bold);
            UIFactory.Stretch(obiettivoTesto.rectTransform, 12f, 4f, 12f, 4f);
            obiettivoTesto.enableAutoSizing = true; obiettivoTesto.fontSizeMin = 12f; obiettivoTesto.fontSizeMax = 19f;
            obiettivoGruppo = rigaObiettivo.gameObject.AddComponent<CanvasGroup>();
            obiettivoGruppo.alpha = 0f;

            gruppo.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- scena
        void Carica(string scena)
        {
            scenaCorrente = scena;
            def = GameData.MappaScena(scena);
            tex = def != null ? Resources.Load<Texture2D>("Mappe/" + scena) : null;
            for (int i = 0; i < etichette.Count; i++) if (etichette[i].rt != null) Destroy(etichette[i].rt.gameObject);
            etichette.Clear();
            giocatore = null;
            nomeTarga = "";
            if (def == null || tex == null) return;
            piantaImg.texture = tex;
            float raggio = def.raggio > 0f ? def.raggio : GameData.Mappe.parametri.raggioVista;
            scala = Raggio / Mathf.Max(3f, raggio);
            pianta.sizeDelta = new Vector2((def.ax1 - def.ax0) * scala, (def.az1 - def.az0) * scala);
            etichette = MappaUi.CreaEtichette(mondo, def, 15f);
            int n = etichette.Count;
            posEt = new Vector2[n]; chiaveEt = new float[n]; candEt = new bool[n]; sceltoEt = new bool[n];
        }

        void Update()
        {
            var gm = GameManager.I;
            var ui = UIRoot.I;
            if (gm == null || ui == null) return;

            bool attivo = !gm.InputBlocked && Time.timeScale > 0f;
            Obiettivi.Tick(Time.unscaledDeltaTime, attivo);

            string scena = SceneManager.GetActiveScene().name;
            if (scena != scenaCorrente) Carica(scena);

            var save = gm.Save;
            bool voglio = def != null && tex != null && !save.minimappaNascosta && !Disabilitata && !ui.MenuScene;
            if (gruppo.gameObject.activeSelf != voglio) gruppo.gameObject.SetActive(voglio);
            Visibile = voglio;
            if (!voglio) return;

            if (giocatore == null) giocatore = FindAnyObjectByType<ThirdPersonController>();
            if (giocatore == null) return;

            // la minimappa si fa piu' discreta quando un pannello la copre o il gioco e' fermo
            Vector3 p = giocatore.transform.position;
            float yaw = 0f;
            var cam = Camera.main;
            if (!save.nordFisso && cam != null) yaw = cam.transform.eulerAngles.y;
            float cx = (def.ax0 + def.ax1) * 0.5f, cz = (def.az0 + def.az1) * 0.5f;

            mondo.localRotation = Quaternion.Euler(0f, 0f, yaw);
            pianta.anchoredPosition = new Vector2(-(p.x - cx) * scala, -(p.z - cz) * scala);
            freccia.rectTransform.localRotation = Quaternion.Euler(0f, 0f, yaw - giocatore.transform.eulerAngles.y);
            Vector2 n = MappaUi.Ruota(new Vector2(0f, 1f), yaw) * (Diametro * 0.5f - 7f);
            nord.rectTransform.anchoredPosition = n;
            nord.rectTransform.localRotation = Quaternion.identity;

            AggiornaEtichette(p, yaw);
            AggiornaObiettivo(p, yaw, scena);
            AggiornaTarga(p);
        }

        void AggiornaEtichette(Vector3 p, float yaw)
        {
            float rn = GameData.Mappe.parametri.raggioNome;
            for (int i = 0; i < etichette.Count; i++)
            {
                var e = etichette[i];
                float dx = e.x - p.x, dz = e.z - p.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                Vector2 s = MappaUi.Ruota(new Vector2(dx, dz) * scala, yaw);
                posEt[i] = s; chiaveEt[i] = d;
                candEt[i] = d <= rn && s.magnitude < Raggio - e.h * 0.5f - 2f;
            }
            MappaUi.Seleziona(etichette, posEt, chiaveEt, candEt, sceltoEt, 3f);
            for (int i = 0; i < etichette.Count; i++)
            {
                var e = etichette[i];
                bool on = sceltoEt[i];
                if (e.rt.gameObject.activeSelf != on) e.rt.gameObject.SetActive(on);
                if (!on) continue;
                MappaUi.Misura(e);
                float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rn * 0.65f, rn, chiaveEt[i]));
                e.gruppo.alpha = Mathf.MoveTowards(e.gruppo.alpha, a, Time.unscaledDeltaTime * 4f);
                e.rt.anchoredPosition = new Vector2((e.x - p.x) * scala, (e.z - p.z) * scala);
                e.rt.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
        }

        void AggiornaObiettivo(Vector3 p, float yaw, string scena)
        {
            var ob = Obiettivi.Corrente();
            bool mostra = ob != null && Obiettivi.Mostrato;
            var punto = mostra ? Obiettivi.Punto(ob, scena) : null;

            // riga dell'obiettivo
            string testo = mostra ? Obiettivi.Testo(ob, scena) : "";
            if (obiettivoTesto.text != testo && !string.IsNullOrEmpty(testo)) obiettivoTesto.text = testo;
            obiettivoAlpha = Mathf.MoveTowards(obiettivoAlpha, string.IsNullOrEmpty(testo) ? 0f : 1f, Time.unscaledDeltaTime * 3f);
            obiettivoGruppo.alpha = obiettivoAlpha;

            if (punto == null) { if (croce.gameObject.activeSelf) croce.gameObject.SetActive(false); if (bordo.gameObject.activeSelf) bordo.gameObject.SetActive(false); return; }

            Vector2 v = MappaUi.Ruota(new Vector2(punto.x - p.x, punto.z - p.z) * scala, yaw);
            float pulsa = UITheme.ReduceEffects ? 1f : 1f + 0.10f * Mathf.Sin(Time.unscaledTime * 3.2f);
            if (v.magnitude <= Raggio - 14f)
            {
                croce.gameObject.SetActive(true); bordo.gameObject.SetActive(false);
                croce.rectTransform.anchoredPosition = v;
                croce.rectTransform.localScale = Vector3.one * pulsa;
            }
            else
            {
                bordo.gameObject.SetActive(true); croce.gameObject.SetActive(false);
                Vector2 d = v.normalized;
                bordo.rectTransform.anchoredPosition = d * (Raggio - 12f);
                bordo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                bordo.rectTransform.localScale = Vector3.one * pulsa;
            }
        }

        void AggiornaTarga(Vector3 p)
        {
            var s = MappaUi.StradaIn(def, p.x, p.z);
            string nome = s != null ? s.nome : "";
            if (nome != nomeTarga)
            {
                nomeTarga = nome;
                if (!string.IsNullOrEmpty(nome)) targaTesto.text = nome;
            }
            targaAlpha = Mathf.MoveTowards(targaAlpha, string.IsNullOrEmpty(nomeTarga) ? 0f : 1f, Time.unscaledDeltaTime * 3f);
            targaGruppo.alpha = targaAlpha;
        }
    }
}
