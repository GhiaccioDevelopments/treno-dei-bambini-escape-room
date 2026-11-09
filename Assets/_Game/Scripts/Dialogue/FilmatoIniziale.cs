using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Audio;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Mappa;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.Dialogue
{
    /// <summary>
    /// R025: filmato iniziale della nuova partita, in-engine (niente video). Un regista a capitoli guidato dai JSON: il copione e gli ID del doppiaggio
    /// stanno in cutscene_intro.json, le inquadrature (camera, attore, inserti) in filmato_intro.json. Usa le scene vere (10_Hub_Vicolo per il presente,
    /// 30_FB_Scarpe per il 1946) e una CinemachineCamera di regia con priorita' alta che alla fine si fonde nella camera di gioco (CameraRig).
    /// Sottotitoli sempre attivi; senza audio il testo avanza a tempo di lettura (campo durata) e con clic/E/Spazio; con audio segue la clip.
    /// Si salta tenendo premuto (Esc, Spazio, E, Invio, clic, tocco, B/A/Start del gamepad).
    /// </summary>
    public class FilmatoIniziale : MonoBehaviour
    {
        public static bool InCorso { get; private set; }
        public bool IsPlaying { get { return InCorso; } }
        const string IdCutscene = "intro";
        const float AltezzaBande = 56f;

        // ---- stato leggibile dalle prove
        public int CapitoloIndice { get; private set; } = -1;
        public string BattutaId { get; private set; } = "";
        public string ControlloMostrato { get; private set; } = "";
        public string DispositivoControlli { get; private set; } = "";
        public string SottotitoloTesto { get { return testoLbl != null ? testoLbl.text : ""; } }
        public bool SottotitoloVisibile { get { return subGruppo != null && subGruppo.alpha > 0.5f; } }
        public bool InsertoAttivo { get { return studio != null && studio.Aperto; } }
        public bool Concluso { get; private set; }
        public bool Saltato { get; private set; }
        public int BattuteMostrate { get; private set; }
        public int CapitoliVisti { get; private set; }
        public string ScenaCapitolo { get; private set; } = "";
        public bool ModoRivedi { get; private set; }
        public float Tenuta { get { return tenuta; } }
        public bool TargaSaltaVisibile { get { return saltaGruppo != null && saltaGruppo.alpha > 0.3f; } }
        public bool ConsegnaFatta { get; private set; }
        public bool BattutaInCorso { get; private set; }
        public float InizioBattuta { get; private set; }

        FilmatoFile F { get { return GameData.Filmato; } }

        // ---- interfaccia
        RectTransform view;
        Image nero, tinta, velo;
        RectTransform barSu, barGiu;
        CanvasGroup titoloGruppo; TextMeshProUGUI titoloTesto;
        CanvasGroup subGruppo; TextMeshProUGUI nomeLbl, testoLbl; GameObject targhettaNome; RectTransform subBox;
        Image ritratto; CanvasGroup ritrattoGruppo; RectTransform ritrattoRt;
        CanvasGroup ctrlGruppo; RectTransform ctrlRt, ctrlTasti; TextMeshProUGUI ctrlTitolo;
        CanvasGroup saltaGruppo; TextMeshProUGUI saltaLbl; Image saltaBarra;
        RectTransform cartaRt; CanvasGroup cartaGruppo; TextMeshProUGUI cartaTitolo, cartaCorpo; Image cartaImg;
        RectTransform piantaRt; CanvasGroup piantaGruppo; RawImage piantaImg; Image piantaCroce; TextMeshProUGUI piantaTit;
        RectTransform indizioRt; CanvasGroup indizioGruppo;
        RectTransform erroreRt; CanvasGroup erroreGruppo; TextMeshProUGUI erroreTit, erroreTxt;

        // ---- regia
        FilmatoStudio studio = new FilmatoStudio();
        Camera cam;
        CinemachineBrain brain;
        CinemachineCamera vcam;
        CinemachineBlendDefinition blendOrig;
        Transform player;
        ThirdPersonController tpc;
        CharacterController cc;
        PlayerInteractor pint;
        CameraRig rig;
        Animator anim;
        Coroutine camRoutine, attoreRoutine, tintaRoutine, titoloRoutine;
        bool skip, tapAvanti;
        float tenuta, ultimoInput = -100f;
        float guardiaFinoA;
        Vector3 shakeOff;

        // rivedi: dove tornare
        string retScena, retSpawn;
        Vector3 retPos; float retYaw;
        bool retValido;

        static readonly int HSpeed = Animator.StringToHash("Speed");
        static readonly int HGrounded = Animator.StringToHash("Grounded");

        void Awake() { Build(); }

        // ================================================================== costruzione dell'interfaccia
        static void NoRay(GameObject g)
        {
            foreach (var gr in g.GetComponentsInChildren<Graphic>(true)) gr.raycastTarget = false;
        }

        void Build()
        {
            var self = (RectTransform)transform;
            UIFactory.Stretch(self);
            view = UIFactory.NewRect("Filmato", self);
            UIFactory.Stretch(view);

            tinta = UIFactory.MakePanel(view, "Tinta", new Color(0f, 0f, 0f, 0f), false);
            UIFactory.Stretch(tinta.rectTransform);
            velo = UIFactory.MakePanel(view, "Velo", new Color(0.04f, 0.03f, 0.02f, 0f), false);
            UIFactory.Stretch(velo.rectTransform);

            BuildInserti();

            barSu = MakeBar("BandaSopra", true);
            barGiu = MakeBar("BandaSotto", false);

            // titolo del capitolo, sulla banda in alto
            var tr = UIFactory.MakeText(view, "", 28, UITheme.OttoneLuce, TextAlignmentOptions.Left, FontStyles.Italic);
            UIFactory.Place(tr.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -10f), new Vector2(720f, 40f));
            titoloTesto = tr;
            titoloGruppo = tr.gameObject.AddComponent<CanvasGroup>();
            titoloGruppo.alpha = 0f;

            // ritratto per i personaggi senza modello 3D
            var rp = UIFactory.MakePanel(view, "Ritratto", Color.white, false);
            UIFactory.Place(rp.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, AltezzaBande + 22f), new Vector2(230f, 230f));
            rp.preserveAspect = true;
            ritratto = rp; ritrattoRt = rp.rectTransform;
            UIFactory.MakeBrassFrame(ritrattoRt, 6f);
            ritrattoGruppo = rp.gameObject.AddComponent<CanvasGroup>();
            ritrattoGruppo.alpha = 0f;

            // sottotitoli: lettera su carta con targhetta d'ottone del nome
            var box = UIFactory.MakePaper(view, "Sottotitolo", new Color(1f, 1f, 1f, 0.96f), false);
            subBox = box.rectTransform;
            UIFactory.Place(subBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, AltezzaBande + 18f), new Vector2(900f, 132f));
            subGruppo = box.gameObject.AddComponent<CanvasGroup>();
            subGruppo.alpha = 0f;
            var plate = UIFactory.MakePanel(subBox, "Targhetta", Color.white, false);
            plate.sprite = UIArt.Brass(); plate.type = Image.Type.Sliced;
            plate.rectTransform.anchorMin = plate.rectTransform.anchorMax = new Vector2(0f, 1f);
            plate.rectTransform.pivot = new Vector2(0f, 0.5f);
            plate.rectTransform.anchoredPosition = new Vector2(26f, 0f);
            plate.rectTransform.sizeDelta = new Vector2(280f, 44f);
            nomeLbl = UIFactory.MakeText(plate.rectTransform, "", 24, UIFactory.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(nomeLbl.rectTransform, 12f, 2f, 12f, 2f);
            nomeLbl.enableAutoSizing = true; nomeLbl.fontSizeMin = 14; nomeLbl.fontSizeMax = 24;
            targhettaNome = plate.gameObject;
            testoLbl = UIFactory.MakeText(subBox, "", 26, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(testoLbl.rectTransform, 34f, 14f, 34f, 30f);
            testoLbl.enableAutoSizing = true; testoLbl.fontSizeMin = 16; testoLbl.fontSizeMax = 26;

            // targhetta dei controlli (etichetta di carta d'epoca, piccola, in basso a destra sopra i sottotitoli)
            var ct = UIFactory.MakePanel(view, "TarghettaControlli", Color.white, false);
            ct.sprite = UIArt.Tag(); ct.type = Image.Type.Sliced;
            ctrlRt = ct.rectTransform;
            UIFactory.Place(ctrlRt, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(450f, AltezzaBande + 160f), new Vector2(380f, 74f));
            ctrlGruppo = ct.gameObject.AddComponent<CanvasGroup>();
            ctrlGruppo.alpha = 0f;
            ctrlTitolo = UIFactory.MakeText(ctrlRt, "", 19, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(ctrlTitolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(352f, 24f));
            ctrlTasti = UIFactory.NewRect("Tasti", ctrlRt);
            UIFactory.Place(ctrlTasti, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 8f), new Vector2(356f, 40f));
            var hl = ctrlTasti.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 6f; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

            // targhetta "tieni premuto per saltare" in alto a destra, con la barra che si riempie
            var sk = UIFactory.MakePanel(view, "TarghettaSalta", Color.white, false);
            sk.sprite = UIArt.Tag(); sk.type = Image.Type.Sliced;
            var skRt = sk.rectTransform;
            UIFactory.Place(skRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -(AltezzaBande + 12f)), new Vector2(360f, 66f));
            saltaGruppo = sk.gameObject.AddComponent<CanvasGroup>();
            saltaGruppo.alpha = 0f;
            saltaLbl = UIFactory.MakeText(skRt, "", 18, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(saltaLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(332f, 24f));
            var barraFondo = UIFactory.MakePanel(skRt, "BarraFondo", new Color(0.23f, 0.16f, 0.1f, 0.35f), false);
            barraFondo.sprite = UIArt.White();
            UIFactory.Place(barraFondo.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 12f), new Vector2(332f, 8f));
            saltaBarra = UIFactory.MakePanel(barraFondo.rectTransform, "Barra", UITheme.Ceralacca, false);
            saltaBarra.sprite = UIArt.White();
            saltaBarra.type = Image.Type.Filled; saltaBarra.fillMethod = Image.FillMethod.Horizontal; saltaBarra.fillAmount = 0f;
            UIFactory.Stretch(saltaBarra.rectTransform);

            // nero di dissolvenza (sopra tutto)
            nero = UIFactory.MakePanel(view, "Nero", Color.black, false);
            UIFactory.Stretch(nero.rectTransform);

            NoRay(view.gameObject);
            view.gameObject.SetActive(false);
        }

        RectTransform MakeBar(string nome, bool sopra)
        {
            var bar = UIFactory.MakePanel(view, nome, Color.black, false);
            var r = bar.rectTransform;
            r.anchorMin = new Vector2(0f, sopra ? 1f : 0f); r.anchorMax = new Vector2(1f, sopra ? 1f : 0f);
            r.pivot = new Vector2(0.5f, sopra ? 1f : 0f);
            r.sizeDelta = new Vector2(0f, AltezzaBande);
            return r;
        }

        void BuildInserti()
        {
            // foglio d'epoca
            var cp = UIFactory.MakePaper(view, "Carta", Color.white, false);
            cartaRt = cp.rectTransform; cartaImg = cp;
            UIFactory.Place(cartaRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(640f, 420f));
            cartaGruppo = cp.gameObject.AddComponent<CanvasGroup>();
            cartaGruppo.alpha = 0f;
            cartaTitolo = UIFactory.MakeText(cartaRt, "", 36, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(cartaTitolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -26f), new Vector2(580f, 120f));
            var fs = MondoFab.Font("");
            if (fs != null) cartaTitolo.font = fs;
            cartaTitolo.enableAutoSizing = true; cartaTitolo.fontSizeMin = 20; cartaTitolo.fontSizeMax = 36;
            cartaCorpo = UIFactory.MakeText(cartaRt, "", 28, UIFactory.Inchiostro, TextAlignmentOptions.Top);
            UIFactory.Stretch(cartaCorpo.rectTransform, 36f, 30f, 36f, 150f);
            if (fs != null) cartaCorpo.font = fs;
            cartaCorpo.enableAutoSizing = true; cartaCorpo.fontSizeMin = 16; cartaCorpo.fontSizeMax = 28;

            // pianta del quartiere
            var pp = UIFactory.MakePaper(view, "Pianta", Color.white, false);
            piantaRt = pp.rectTransform;
            UIFactory.Place(piantaRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(520f, 560f));
            piantaGruppo = pp.gameObject.AddComponent<CanvasGroup>();
            piantaGruppo.alpha = 0f;
            var ri = UIFactory.NewRect("PiantaImg", piantaRt);
            UIFactory.Stretch(ri, 22f, 22f, 22f, 52f);
            piantaImg = ri.gameObject.AddComponent<RawImage>();
            piantaCroce = UIFactory.MakeImage(ri, "Croce", MappaArt.Croce(), Color.red);
            piantaCroce.rectTransform.anchorMin = piantaCroce.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            piantaCroce.rectTransform.sizeDelta = new Vector2(54f, 54f);
            piantaCroce.gameObject.SetActive(false);
            piantaTit = UIFactory.MakeText(piantaRt, "", 26, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(piantaTit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(480f, 36f));

            // pulsante Indizio evidenziato (finto: solo da guardare)
            var ib = UIFactory.MakeButton(view, "Indizio", () => { }, new Vector2(240f, 70f), ButtonStyle.Brass, 30f, true);
            indizioRt = (RectTransform)ib.transform;
            UIFactory.Place(indizioRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, AltezzaBande + 190f), new Vector2(240f, 70f));
            indizioGruppo = ib.gameObject.AddComponent<CanvasGroup>();
            indizioGruppo.alpha = 0f;

            // biglietto gentile dell'errore
            var eb = UIFactory.MakePaper(view, "Errore", Color.white, false);
            erroreRt = eb.rectTransform;
            UIFactory.Place(erroreRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(620f, 190f));
            erroreGruppo = eb.gameObject.AddComponent<CanvasGroup>();
            erroreGruppo.alpha = 0f;
            erroreTit = UIFactory.MakeText(erroreRt, "", 30, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(erroreTit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(560f, 40f));
            erroreTxt = UIFactory.MakeText(erroreRt, "", 24, UIFactory.Inchiostro, TextAlignmentOptions.Top);
            UIFactory.Stretch(erroreTxt.rectTransform, 30f, 14f, 30f, 64f);
            erroreTxt.enableAutoSizing = true; erroreTxt.fontSizeMin = 14; erroreTxt.fontSizeMax = 24;
        }

        // ================================================================== avvio
        public void Avvia(bool rivedi, Action fine)
        {
            if (InCorso || GameManager.I == null || SceneLoader.I == null || SceneLoader.I.IsLoading) { if (fine != null) fine(); return; }
            var def = GameData.Cutscene(IdCutscene);
            if (def == null || def.battute == null || def.battute.Count == 0 || F.capitoli.Count == 0) { Debug.LogWarning("Filmato iniziale non disponibile."); if (fine != null) fine(); return; }
            StartCoroutine(Run(def, rivedi, fine));
        }

        IEnumerator Run(CutsceneDef def, bool rivedi, Action fine)
        {
            InCorso = true; Concluso = false; Saltato = false; ConsegnaFatta = false; ModoRivedi = rivedi;
            BattuteMostrate = 0; CapitoliVisti = 0; CapitoloIndice = -1; BattutaId = ""; ControlloMostrato = "";
            skip = false; tenuta = 0f;
            var gm = GameManager.I;
            gm.PushBlock("filmato");
            if (UIRoot.I != null) UIRoot.I.HidePrompt();
            Memorizza(rivedi);

            view.gameObject.SetActive(true);
            view.SetAsLastSibling();
            nero.color = new Color(0f, 0f, 0f, 0f);
            subGruppo.alpha = 0f; ritrattoGruppo.alpha = 0f; ctrlGruppo.alpha = 0f; saltaGruppo.alpha = 0f; titoloGruppo.alpha = 0f;
            NascondiInserti();
            barSu.anchoredPosition = new Vector2(0f, AltezzaBande); barGiu.anchoredPosition = new Vector2(0f, -AltezzaBande);
            UITween.Move(barSu, Vector2.zero, 0.6f, Ease.OutCubic);
            UITween.Move(barGiu, Vector2.zero, 0.6f, Ease.OutCubic);
            MostraTargaSalta(0f);

            for (int ci = 0; ci < F.capitoli.Count && !skip; ci++)
            {
                yield return Capitolo(def, ci);
                CapitoliVisti = ci + 1;
            }
            Saltato = skip;
            yield return Chiudi(rivedi);

            if (!rivedi)
            {
                var s = gm.Save;
                s.filmatoVisto = true;
                if (!s.cutsceneViste.Contains(IdCutscene)) s.cutsceneViste.Add(IdCutscene);
                gm.SaveGame();
                Obiettivi.Forza(F.parametri.obiettivoForzatoSecondi);
            }
            Concluso = true;
            InCorso = false;
            gm.PopBlock("filmato");
            if (fine != null) fine();
        }

        void Memorizza(bool rivedi)
        {
            retValido = false;
            var s = GameManager.I.Save;
            retScena = s.scena; retSpawn = s.spawn;
            if (!rivedi) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { retPos = p.transform.position; retYaw = p.transform.eulerAngles.y; retValido = true; }
            retScena = SceneManager.GetActiveScene().name;
        }

        // ================================================================== capitolo
        IEnumerator Capitolo(CutsceneDef def, int ci)
        {
            var cap = F.capitoli[ci];
            CapitoloIndice = ci;
            Debug.Log("[Filmato] capitolo " + (ci + 1) + " " + cap.id + " (scena " + cap.scena + ")");
            ScenaCapitolo = cap.scena;
            var righe = new List<BattutaDef>();
            foreach (var b in def.battute) if (b != null && b.capitolo == capId(def, ci)) righe.Add(b);
            if (righe.Count == 0) yield break;
            CapitoloDef cd = ci < def.capitoli.Count ? def.capitoli[ci] : null;
            float pausa = cd != null ? cd.pausaSenzaTesto : 2f;
            var shots = cap.inquadrature;
            var durate = DurateInquadrature(shots, righe, pausa);

            // scena
            bool cambia = ScenaDa(cap.scena);
            if (cambia)
            {
                var stile = cap.stile == "ricordo" ? TransitionStyle.Ricordo : TransitionStyle.Fade;
                SceneLoader.I.Load(cap.scena, "inizio", stile, () => PreparaScena(ci, durate));
                float g = 0f;
                while ((SceneLoader.I.IsLoading || g < 0.1f) && g < 120f) { g += Time.unscaledDeltaTime; yield return null; }
            }
            else
            {
                yield return FadeNero(1f, 0.8f);
                yield return PreparaScena(ci, durate);
            }
            if (!PreparatoOk) { skip = true; yield break; }

            // titolo del capitolo e dissolvenza in apertura
            if (cd != null && !string.IsNullOrEmpty(cd.titolo)) MostraTitolo(cd.titolo);
            yield return FadeNero(0f, 1.0f);

            // pre-roll senza testo: la camera si muove gia'
            float t = 0f;
            while (t < pausa && !skip) { t += Time.unscaledDeltaTime; yield return null; }

            int shotCorrente = 0;
            for (int li = 0; li < righe.Count && !skip; li++)
            {
                var b = righe[li];
                int s = li == 0 ? 0 : TrovaInquadratura(shots, b.id);
                if (s > 0 && s != shotCorrente)
                {
                    shotCorrente = s;
                    yield return CambiaInquadratura(shots[s], durate[s]);
                }
                ProgressoTinta(li, righe.Count);
                yield return Battuta(b, righe, li);
                BattuteMostrate++;
                if (li < righe.Count - 1) yield return AspettaSec(F.parametri.pausaFraBattute);
            }

            // fine capitolo
            SpegniSottotitoli();
            if (camRoutine != null) { StopCoroutine(camRoutine); camRoutine = null; }
            if (attoreRoutine != null) { StopCoroutine(attoreRoutine); attoreRoutine = null; }
            if (ci < F.capitoli.Count - 1 && !skip)
            {
                string prossima = F.capitoli[ci + 1].scena;
                if (prossima == cap.scena) { yield return FadeNero(1f, 0.8f); ChiudiStudio(); }
            }
            else if (!skip)
            {
                yield return null;
            }
        }

        /// <summary>Serve caricare "scena"? Si': se e' un'altra, oppure se si e' nel menu (anche col vicolo dell'hub come sfondo vivo: e' una scena senza giocatore).</summary>
        static bool ScenaDa(string scena)
        {
            string attiva = SceneManager.GetActiveScene().name;
            return attiva != scena || GameManager.MenuBackdrop || GameManager.IsMenuScene(attiva);
        }

        static string capId(CutsceneDef def, int ci)
        {
            return ci < def.capitoli.Count ? def.capitoli[ci].id : "";
        }

        List<float> DurateInquadrature(List<FilmatoInquadratura> shots, List<BattutaDef> righe, float pausa)
        {
            var d = new List<float>();
            for (int k = 0; k < shots.Count; k++) d.Add(k == 0 ? pausa : 0f);
            int cur = 0;
            for (int i = 0; i < righe.Count; i++)
            {
                int s = TrovaInquadratura(shots, righe[i].id);
                if (s > 0) cur = s;
                d[cur] += Mathf.Max(2f, righe[i].durata) + F.parametri.pausaFraBattute;
            }
            return d;
        }

        static int TrovaInquadratura(List<FilmatoInquadratura> shots, string idBattuta)
        {
            for (int k = 1; k < shots.Count; k++) if (shots[k].da == idBattuta) return k;
            return -1;
        }

        // ================================================================== scena e camera
        bool PreparatoOk;

        /// <summary>A scena caricata, con lo schermo ancora coperto: trova il giocatore, crea la camera di regia, applica la prima inquadratura.</summary>
        IEnumerator PreparaScena(int ci, List<float> durate)
        {
            PreparatoOk = false;
            nero.color = Color.black;
            ChiudiStudio();
            TrovaScena();
            // la scena e' appena stata caricata: se Amerigo o la camera non ci sono ancora si aspetta qualche frame
            for (int tentativo = 0; tentativo < 180 && (player == null || cam == null); tentativo++) { yield return null; TrovaScena(); }
            if (player == null || cam == null)
            {
                Debug.LogWarning("Filmato: scena senza giocatore o camera (scena " + SceneManager.GetActiveScene().name + ", scene caricate " + SceneManager.sceneCount
                    + ", giocatore " + (player != null) + ", camera " + (cam != null) + ", telecamere " + Camera.allCamerasCount + ").");
                yield break;
            }
            if (!ModoRivedi) { var s = GameManager.I.Save; s.scena = F.finale.scena; s.spawn = "inizio"; }
            var cap = F.capitoli[ci];
            ImpostaTinta(cap, durate);
            Musica(cap.musica, 1.5f);
            yield return null;
            AvviaInquadratura(cap.inquadrature[0], durate[0]);
            yield return null;
            PreparatoOk = true;
        }

        void TrovaScena()
        {
            cam = Camera.main;
            brain = cam != null ? cam.GetComponent<CinemachineBrain>() : null;
            var p = GameObject.FindGameObjectWithTag("Player");
            player = p != null ? p.transform : null;
            tpc = p != null ? p.GetComponent<ThirdPersonController>() : null;
            cc = p != null ? p.GetComponent<CharacterController>() : null;
            pint = p != null ? p.GetComponent<PlayerInteractor>() : null;
            rig = FindAnyObjectByType<CameraRig>();
            anim = null;
            if (player != null)
            {
                var mod = player.Find(ThirdPersonController.ModelloName);
                if (mod != null) anim = mod.GetComponentInChildren<Animator>();
            }
            if (tpc != null) tpc.enabled = false;
            if (pint != null) pint.enabled = false;
            if (cc != null) cc.enabled = false;
            if (UIRoot.I != null) UIRoot.I.HidePrompt();
            AggancioBrain();
            if (vcam != null) Destroy(vcam.gameObject);
            var go = new GameObject("CM_Filmato");
            vcam = go.AddComponent<CinemachineCamera>();
            vcam.Priority = 500;
            var lens = vcam.Lens;
            lens.FieldOfView = 45f; lens.NearClipPlane = 0.45f; // vicino: la pioggia a pochi centimetri dalla camera non diventa una striscia gigante
            lens.FarClipPlane = RenderSettings.fog && RenderSettings.fogDensity > 0.0001f ? Mathf.Clamp(3f / RenderSettings.fogDensity, 40f, 150f) : 150f;
            vcam.Lens = lens;
            if (cam != null && player != null) go.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
        }

        Quaternion YawPlayer { get { return Quaternion.Euler(0f, player != null ? player.eulerAngles.y : 0f, 0f); } }

        void Musica(string id, float fade)
        {
            if (string.IsNullOrEmpty(id) || AudioManager.I == null) return;
            if (id == "-") AudioManager.I.Music.Stop(fade); else AudioManager.I.Music.Play(id, fade);
        }

        void AvviaInquadratura(FilmatoInquadratura s, float durata)
        {
            if (camRoutine != null) { StopCoroutine(camRoutine); camRoutine = null; }
            if (attoreRoutine != null && s.attore != null && s.attore.presente) { StopCoroutine(attoreRoutine); attoreRoutine = null; } // senza "attore" il cammino in corso continua
            NascondiInserti();
            ChiudiStudio();
            Musica(s.musica, 2f);
            ImpostaAttore(s.attore);
            if (s.consegna) { Consegna(); return; }
            if (s.inserto != null && s.inserto.tipo == "pezzi")
            {
                studio.Apri(s.inserto, cam);
                if (s.inserto.indizio == "si") StartCoroutine(MostraIndizio(durata));
                if (s.inserto.errore == "si") StartCoroutine(MostraErrore(durata));
                camRoutine = StartCoroutine(AnimaStudio());
                return;
            }
            if (s.inserto != null && s.inserto.tipo == "carta") StartCoroutine(MostraCarta(s.inserto, durata));
            if (s.inserto != null && s.inserto.tipo == "pianta") StartCoroutine(MostraPianta(s.inserto, durata));
            if (s.scossa > 0f) StartCoroutine(Scossa(s.scossa));
            camRoutine = StartCoroutine(AnimaCamera(s, Mathf.Max(1f, durata)));
        }

        IEnumerator CambiaInquadratura(FilmatoInquadratura s, float durata)
        {
            bool stacco = s.taglio == "stacco";
            // la tinta dell'inserto o della scena non cambia: solo un taglio morbido
            if (!stacco) yield return FadeNero(1f, 0.3f);
            SpegniSottotitoli();
            AvviaInquadratura(s, durata);
            yield return null;
            if (!stacco) yield return FadeNero(0f, 0.45f);
        }

        IEnumerator AnimaStudio()
        {
            while (studio.Aperto) { studio.Aggiorna(); yield return null; }
        }

        static Vector3 V(float[] a, Vector3 def)
        {
            return a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : def;
        }

        IEnumerator AnimaCamera(FilmatoInquadratura s, float durata)
        {
            if (vcam == null) yield break;
            bool segui = !string.IsNullOrEmpty(s.segui) && player != null;
            Vector3 p0 = V(s.p0, Vector3.zero), m0 = V(s.m0, Vector3.zero);
            Vector3 p1 = s.p1 != null && s.p1.Length >= 3 ? V(s.p1, p0) : p0;
            Vector3 m1 = s.m1 != null && s.m1.Length >= 3 ? V(s.m1, m0) : m0;
            float f0 = s.fov0 > 1f ? s.fov0 : 45f, f1 = s.fov1 > 1f ? s.fov1 : f0;
            float t = 0f;
            while (vcam != null)
            {
                float k = Mathf.Clamp01(t / durata);
                float e = s.ease == "lineare" ? k : Mathf.SmoothStep(0f, 1f, k);
                Vector3 p = Vector3.Lerp(p0, p1, e), m = Vector3.Lerp(m0, m1, e);
                if (segui)
                {
                    var q = YawPlayer;
                    p = player.position + q * p;
                    m = player.position + q * m;
                }
                p += shakeOff;
                var dir = m - p;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
                vcam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(dir, Vector3.up));
                var lens = vcam.Lens; lens.FieldOfView = Mathf.Lerp(f0, f1, e); vcam.Lens = lens;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator Scossa(float ampiezza)
        {
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float a = ampiezza * (1f - t / 0.5f);
                shakeOff = new Vector3(UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a));
                yield return null;
            }
            shakeOff = Vector3.zero;
        }

        // ---- attore (il giocatore: Amerigo adulto nell'hub, bambino nel ricordo)
        void ImpostaAttore(FilmatoAttore a)
        {
            if (player == null) return;
            var mod = player.Find(ThirdPersonController.ModelloName);
            if (a == null || !a.presente) return;
            if (mod != null) mod.gameObject.SetActive(!a.nascondi);
            if (a.pos != null && a.pos.Length >= 2) Piazza(a.pos[0], a.pos[1], a.yaw);
            else if (a.yaw < 9000f) player.rotation = Quaternion.Euler(0f, a.yaw, 0f);
            if (a.percorso != null && a.percorso.Count >= 2) attoreRoutine = StartCoroutine(Cammina(a));
            else ImpostaVelocitaAnim(0f);
        }

        float Suolo(float x, float z, float yDef)
        {
            Physics.SyncTransforms();
            // il suolo e' il piu' alto fra i selciati e i pavimenti sotto il punto (i tetti e i balconi non contano), anche se piu' in alto di dove era Amerigo
            var hits = Physics.RaycastAll(new Vector3(x, yDef + 8f, z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity, bestAny = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(player) || h.normal.y < 0.5f) continue;
                if (h.point.y > yDef + 4f) continue;
                if (h.point.y > bestAny) bestAny = h.point.y;
                if ((h.collider.name.StartsWith("Selciato") || h.collider.name.StartsWith("Pavimento")) && h.point.y > best) best = h.point.y;
            }
            if (float.IsNegativeInfinity(best)) best = bestAny;
            return float.IsNegativeInfinity(best) ? yDef : best + 0.02f;
        }

        void Piazza(float x, float z, float yaw)
        {
            var p = new Vector3(x, player.position.y, z);
            p.y = Suolo(x, z, player.position.y);
            player.position = p;
            if (yaw < 9000f) player.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void ImpostaVelocitaAnim(float v)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            anim.SetFloat(HSpeed, v, 0.1f, Time.unscaledDeltaTime);
            anim.SetBool(HGrounded, true);
        }

        IEnumerator Cammina(FilmatoAttore a)
        {
            float v = a.velocita > 0f ? a.velocita : F.parametri.velocitaCammino;
            for (int i = 0; i + 1 < a.percorso.Count && player != null; i += 2)
            {
                var meta = new Vector2(a.percorso[i], a.percorso[i + 1]);
                while (player != null)
                {
                    var qui = new Vector2(player.position.x, player.position.z);
                    var d = meta - qui;
                    float passo = v * Time.unscaledDeltaTime;
                    if (d.magnitude <= passo) { Piazza(meta.x, meta.y, 9999f); break; }
                    var dir = d.normalized;
                    var np = qui + dir * passo;
                    var rot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y), Vector3.up);
                    player.rotation = Quaternion.Slerp(player.rotation, rot, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                    float y = Suolo(np.x, np.y, player.position.y);
                    player.position = new Vector3(np.x, Mathf.Lerp(player.position.y, y, 0.35f), np.y);
                    ImpostaVelocitaAnim(ThirdPersonController.WalkBlendThreshold);
                    yield return null;
                }
            }
            for (int k = 0; k < 20; k++) { ImpostaVelocitaAnim(0f); yield return null; }
        }

        // ---- tinta e musica
        static Color Hex(string s, Color def)
        {
            Color c;
            return !string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out c) ? c : def;
        }

        void ImpostaTinta(FilmatoCapitolo cap, List<float> durate)
        {
            if (tintaRoutine != null) { StopCoroutine(tintaRoutine); tintaRoutine = null; }
            var da = Hex(cap.tinta, new Color(0f, 0f, 0f, 0f));
            tinta.color = da;
            tintaDa = da; tintaA = Hex(cap.tintaFine, da);
        }

        Color tintaDa, tintaA;

        /// <summary>La tinta del capitolo passa da quella iniziale a quella finale seguendo le battute (curva dolce: si sente solo verso la fine).</summary>
        void ProgressoTinta(int battuta, int totale)
        {
            if (tintaDa == tintaA) return;
            float k = Mathf.Clamp01((battuta + 1f) / Mathf.Max(1, totale));
            if (tintaRoutine != null) StopCoroutine(tintaRoutine);
            tintaRoutine = StartCoroutine(TintaVerso(Color.Lerp(tintaDa, tintaA, k * k), 2.5f));
        }

        IEnumerator TintaVerso(Color verso, float dur)
        {
            Color da = tinta.color;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime) { tinta.color = Color.Lerp(da, verso, t / dur); yield return null; }
            tinta.color = verso;
        }

        // ================================================================== battuta
        IEnumerator Battuta(BattutaDef b, List<BattutaDef> righe, int indice)
        {
            BattutaId = b.id;
            BattutaInCorso = true; InizioBattuta = Time.unscaledTime;
            var par = F.parametri;
            bool narratore = string.IsNullOrEmpty(b.personaggio) || b.personaggio == "narratore";
            var pd = GameData.Personaggio(b.personaggio);
            string nome = pd != null ? pd.nome : (narratore ? "" : b.personaggio);
            Color col = UIFactory.Accento;
            if (pd != null) col = Hex(pd.colore, col);
            nomeLbl.text = nome;
            nomeLbl.color = Color.Lerp(col, UIFactory.Inchiostro, 0.6f);
            targhettaNome.SetActive(!narratore && !string.IsNullOrEmpty(nome));
            testoLbl.fontStyle = narratore ? FontStyles.Italic : FontStyles.Normal;
            testoLbl.text = b.testo ?? "";
            testoLbl.maxVisibleCharacters = 0;
            testoLbl.ForceMeshUpdate();
            int totale = testoLbl.textInfo.characterCount;

            // ritratto solo per chi non ha un modello 3D
            bool conRitratto = !narratore && par.conRitratto.Contains(b.personaggio);
            if (conRitratto)
            {
                var spr = UIFactory.LoadSprite("ritratti", pd != null ? pd.ritratto : b.personaggio);
                if (spr != null) { ritratto.sprite = spr; ritratto.color = Color.white; }
            }
            UITween.Alpha(ritrattoGruppo, conRitratto ? 1f : 0f, 0.35f);
            if (conRitratto && b.azione != null && b.azione.StartsWith("entra_")) UISfx.Play(Sfx.Whoosh, 0.25f);
            UITween.Alpha(subGruppo, 1f, 0.3f);

            // targhetta del controllo, solo mentre dura la battuta che lo nomina
            if (!string.IsNullOrEmpty(b.controllo)) MostraControllo(b.controllo); else NascondiControllo();

            AudioClip clip = VoiceLibrary.Get(System.IO.Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(b.audio) ? b.id : b.audio));
            if (clip != null && AudioManager.I != null) AudioManager.I.PlayVoice(clip);
            float inizio = Time.unscaledTime;
            guardiaFinoA = inizio + par.guardiaSecondi;
            float finoA = clip != null ? inizio + clip.length + 0.4f : inizio + Mathf.Max(2f, b.durata);

            float mostrati = 0f; float nextKey = 0f;
            bool completo = false;
            while (!skip)
            {
                if (!completo)
                {
                    mostrati += par.velocitaTesto * Time.unscaledDeltaTime;
                    int vis = Mathf.Min(totale, Mathf.FloorToInt(mostrati));
                    if (vis != testoLbl.maxVisibleCharacters && Time.unscaledTime >= nextKey && clip == null) { nextKey = Time.unscaledTime + 0.08f; UISfx.Play(Sfx.TypeKey, 0.15f, 0.12f); }
                    testoLbl.maxVisibleCharacters = vis;
                    if (vis >= totale) { completo = true; testoLbl.maxVisibleCharacters = 99999; }
                }
                bool tap = Time.unscaledTime >= guardiaFinoA && TapPremuto();
                if (tap)
                {
                    if (!completo) { completo = true; testoLbl.maxVisibleCharacters = 99999; guardiaFinoA = Time.unscaledTime + 0.25f; }
                    else break;
                }
                if (completo && Time.unscaledTime >= finoA) break;
                yield return null;
            }
            testoLbl.maxVisibleCharacters = 99999;
            if (AudioManager.I != null) AudioManager.I.StopVoice();
            NascondiControllo();
            BattutaInCorso = false;
            // nell'ultima battuta il testo resta finche' non si chiude tutto
        }

        bool TapPremuto()
        {
            if (InputHelper.ConfirmPressed(true)) return true;
            var gp = InputHelper.ActiveGamepad;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
        }

        void SpegniSottotitoli()
        {
            UITween.Alpha(subGruppo, 0f, 0.25f);
            UITween.Alpha(ritrattoGruppo, 0f, 0.25f);
            NascondiControllo();
        }

        IEnumerator AspettaSec(float s)
        {
            float t = 0f;
            while (t < s && !skip) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // ---- dissolvenze e titolo
        public IEnumerator FadeNero(float a, float dur)
        {
            float da = nero.color.a;
            if (dur <= 0f || Mathf.Approximately(da, a)) { nero.color = new Color(0f, 0f, 0f, a); yield break; }
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                nero.color = new Color(0f, 0f, 0f, Mathf.Lerp(da, a, Mathf.SmoothStep(0f, 1f, t / dur)));
                yield return null;
            }
            nero.color = new Color(0f, 0f, 0f, a);
        }

        void MostraTitolo(string testo)
        {
            if (titoloRoutine != null) StopCoroutine(titoloRoutine);
            titoloRoutine = StartCoroutine(TitoloAnim(testo));
        }

        IEnumerator TitoloAnim(string testo)
        {
            titoloTesto.text = testo;
            yield return AspettaSec(1.2f);
            UITween.Alpha(titoloGruppo, 1f, 0.8f);
            yield return AspettaSec(3.6f);
            UITween.Alpha(titoloGruppo, 0f, 0.8f);
        }

        // ================================================================== controlli
        string ultimoDisp = "";

        /// <summary>Dispositivo in uso (tastiera + mouse, gamepad o touch): l'ultimo che ha dato un ingresso; la targhetta dei controlli si scrive per quello.</summary>
        public string DispositivoOra
        {
            get
            {
                if (UIRoot.TouchMode) return "touch";
                if (!string.IsNullOrEmpty(ultimoDisp)) return ultimoDisp;
                return InputHelper.ActiveGamepad != null ? "gamepad" : "tastiera";
            }
        }

        void AggiornaDispositivo()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) ultimoDisp = "tastiera";
            var ms = Mouse.current;
            if (ms != null && (ms.leftButton.wasPressedThisFrame || ms.delta.ReadValue().sqrMagnitude > 4f)) ultimoDisp = "tastiera";
            var gp = InputHelper.ActiveGamepad;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame
                || gp.startButton.wasPressedThisFrame || gp.dpad.ReadValue().sqrMagnitude > 0.25f || gp.leftStick.ReadValue().sqrMagnitude > 0.25f || gp.rightStick.ReadValue().sqrMagnitude > 0.25f)) ultimoDisp = "gamepad";
        }

        /// <summary>Tasti di una targhetta per un dispositivo, uniti da spazio (prove).</summary>
        public string TastiPer(string id, string disp)
        {
            var c = TrovaControllo(id);
            return c == null ? "" : string.Join(" ", Tasti(c, disp));
        }

        FilmatoControllo TrovaControllo(string id)
        {
            foreach (var c in F.controlli) if (c.id == id) return c;
            return null;
        }

        static List<string> Tasti(FilmatoControllo c, string disp)
        {
            return disp == "touch" ? c.touch : (disp == "gamepad" ? c.gamepad : c.tastiera);
        }

        void RiempiTasti(RectTransform contenitore, List<string> tasti)
        {
            for (int i = contenitore.childCount - 1; i >= 0; i--) Destroy(contenitore.GetChild(i).gameObject);
            foreach (var t in tasti)
            {
                var k = UIFactory.MakePanel(contenitore, "Tasto", Color.white, false);
                k.sprite = UIArt.KeyCap(); k.type = Image.Type.Sliced;
                var lab = UIFactory.MakeText(k.rectTransform, t, 20, UIFactory.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
                UIFactory.Stretch(lab.rectTransform, 6f, 2f, 6f, 2f);
                lab.enableAutoSizing = false;
                lab.ForceMeshUpdate();
                float w = Mathf.Max(40f, lab.preferredWidth + 22f);
                k.rectTransform.sizeDelta = new Vector2(w, 38f);
                NoRay(k.gameObject);
            }
        }

        void MostraControllo(string id)
        {
            var c = TrovaControllo(id);
            if (c == null) return;
            string disp = DispositivoOra;
            DispositivoControlli = disp;
            ControlloMostrato = id;
            if (id == "salta") { ultimoInput = Time.unscaledTime; return; } // la targhetta del salto e' quella in alto a destra
            ctrlTitolo.text = c.titolo;
            RiempiTasti(ctrlTasti, Tasti(c, disp));
            UITween.Alpha(ctrlGruppo, 1f, 0.35f);
            if (id == "salta") ultimoInput = Time.unscaledTime;
        }

        void NascondiControllo()
        {
            ControlloMostrato = "";
            UITween.Alpha(ctrlGruppo, 0f, 0.25f);
        }

        void AggiornaTestoSalta()
        {
            var c = TrovaControllo("salta");
            if (c == null) return;
            string t = c.titolo + "  <color=#7A1F1A><b>" + string.Join(" ", Tasti(c, DispositivoOra)) + "</b></color>";
            if (saltaLbl.text != t) saltaLbl.text = t;
        }

        void MostraTargaSalta(float alpha)
        {
            var c = TrovaControllo("salta");
            if (c == null) return;
            AggiornaTestoSalta();
            DispositivoControlli = DispositivoOra;
            saltaGruppo.alpha = alpha;
        }

        // ================================================================== salta (tieni premuto)
        bool TastoSaltaPremuto()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.isPressed || kb.spaceKey.isPressed || kb.eKey.isPressed || kb.enterKey.isPressed)) return true;
            var ms = Mouse.current;
            if (ms != null && ms.leftButton.isPressed) return true;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed) return true;
            var gp = InputHelper.ActiveGamepad;
            return gp != null && (gp.buttonEast.isPressed || gp.buttonSouth.isPressed || gp.startButton.isPressed);
        }

        void Update()
        {
            if (!InCorso) return;
            float dt = Time.unscaledDeltaTime;
            AggiornaDispositivo();
            bool premuto = TastoSaltaPremuto();
            if (premuto)
            {
                tenuta += dt;
                ultimoInput = Time.unscaledTime;
                AggiornaTestoSalta();
                if (tenuta >= F.parametri.tenutaSalta && !skip) skip = true;
            }
            else tenuta = Mathf.Max(0f, tenuta - dt * 2f);
            saltaBarra.fillAmount = Mathf.Clamp01(tenuta / Mathf.Max(0.1f, F.parametri.tenutaSalta));
            // la targhetta compare a ogni pressione e poi si ritira
            bool mostra = Time.unscaledTime - ultimoInput < 2.5f || ControlloMostrato == "salta";
            float target = mostra ? 1f : 0f;
            saltaGruppo.alpha = Mathf.MoveTowards(saltaGruppo.alpha, target, dt * 3f);
            if (saltaGruppo.alpha > 0.01f && Time.frameCount % 30 == 0) { DispositivoControlli = DispositivoOra; AggiornaTestoSalta(); }
        }

        // ================================================================== inserti UI
        void NascondiInserti()
        {
            cartaGruppo.alpha = 0f; piantaGruppo.alpha = 0f; indizioGruppo.alpha = 0f; erroreGruppo.alpha = 0f;
            velo.color = new Color(velo.color.r, velo.color.g, velo.color.b, 0f);
        }

        void ChiudiStudio() { if (studio != null && studio.Aperto) studio.Chiudi(); }

        FilmatoCarta TrovaCarta(string id)
        {
            foreach (var c in F.carte) if (c.id == id) return c;
            return null;
        }

        IEnumerator MostraCarta(FilmatoInserto ins, float durata)
        {
            var c = TrovaCarta(ins.carta);
            if (c == null) yield break;
            cartaTitolo.text = c.titolo;
            cartaCorpo.text = string.Join("\n", c.righe);
            cartaRt.sizeDelta = new Vector2(c.larghezza, c.altezza);
            cartaRt.localRotation = Quaternion.Euler(0f, 0f, c.inclina);
            cartaImg.color = c.stile == "scheda" ? new Color(0.95f, 0.9f, 0.78f, 1f) : Color.white;
            float a = 0f;
            velo.color = new Color(0.04f, 0.03f, 0.02f, 0.55f);
            for (float t = 0f; t < durata && cartaRt != null; t += Time.unscaledDeltaTime)
            {
                a = Mathf.Clamp01(t / 0.8f) * (1f - Mathf.Clamp01((t - (durata - 0.6f)) / 0.6f));
                cartaGruppo.alpha = a;
                cartaRt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.04f, t / durata);
                yield return null;
            }
            cartaGruppo.alpha = 0f;
        }

        IEnumerator MostraPianta(FilmatoInserto ins, float durata)
        {
            string scena = string.IsNullOrEmpty(ins.scena) ? SceneManager.GetActiveScene().name : ins.scena;
            var tex = Resources.Load<Texture2D>("Mappe/" + scena);
            var def = GameData.MappaScena(scena);
            if (tex == null || def == null) yield break;
            piantaImg.texture = tex;
            piantaTit.text = F.testi.piantaTitolo;
            piantaCroce.gameObject.SetActive(false);
            piantaRt.anchorMin = piantaRt.anchorMax = new Vector2(0.5f, 0.5f); piantaRt.pivot = new Vector2(0.5f, 0.5f);
            piantaRt.anchoredPosition = new Vector2(0f, 10f); piantaRt.localScale = Vector3.one * 0.92f;
            // la croce dell'obiettivo (nella scena corrente)
            var ob = Obiettivi.Corrente();
            var punto = ob != null ? Obiettivi.Punto(ob, scena) : null;
            if (punto == null) punto = Obiettivi.Punto(GameData.Mappe.obiettivi.Count > 0 ? GameData.Mappe.obiettivi[0] : null, scena);
            if (punto != null)
            {
                float u = Mathf.InverseLerp(def.ax0, def.ax1, punto.x), v = Mathf.InverseLerp(def.az0, def.az1, punto.z);
                piantaCroce.rectTransform.anchorMin = piantaCroce.rectTransform.anchorMax = new Vector2(u, v);
                piantaCroce.rectTransform.anchoredPosition = Vector2.zero;
            }
            velo.color = new Color(0.04f, 0.03f, 0.02f, 0.45f);
            bool croce = ins.croce == "si";
            float tCroce = Mathf.Min(durata * 0.55f, 4f);
            for (float t = 0f; t < durata && piantaRt != null; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Clamp01(t / 1.2f) * (1f - Mathf.Clamp01((t - (durata - 0.8f)) / 0.8f));
                piantaGruppo.alpha = a;
                piantaRt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.0f, Mathf.SmoothStep(0f, 1f, t / durata));
                if (croce && t >= tCroce && !piantaCroce.gameObject.activeSelf)
                {
                    piantaCroce.gameObject.SetActive(true);
                    piantaCroce.rectTransform.localScale = Vector3.zero;
                    UITween.Scale(piantaCroce.rectTransform, Vector3.one, 0.4f, Ease.OutBack);
                    UISfx.Play(Sfx.Stamp, 0.3f);
                }
                yield return null;
            }
            piantaGruppo.alpha = 0f;
        }

        IEnumerator MostraIndizio(float durata)
        {
            ((RectTransform)indizioRt).localScale = Vector3.one;
            var lab = indizioRt.GetComponentInChildren<TextMeshProUGUI>();
            if (lab != null) lab.text = F.testi.indizio;
            for (float t = 0f; t < durata && indizioRt != null; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Clamp01(t / 0.8f) * (1f - Mathf.Clamp01((t - (durata - 0.6f)) / 0.6f));
                indizioGruppo.alpha = a;
                indizioRt.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(t * 4f));
                yield return null;
            }
            indizioGruppo.alpha = 0f;
        }

        IEnumerator MostraErrore(float durata)
        {
            erroreTit.text = F.testi.erroreTitolo;
            erroreTxt.text = F.testi.erroreTesto;
            for (float t = 0f; t < durata && erroreRt != null; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Clamp01(t / 0.8f) * (1f - Mathf.Clamp01((t - (durata - 0.6f)) / 0.6f));
                erroreGruppo.alpha = a;
                yield return null;
            }
            erroreGruppo.alpha = 0f;
        }

        // ================================================================== consegna e chiusura
        /// <summary>La camera di regia passa il testimone alla camera di gioco: il Brain le fonde con una dissolvenza morbida.</summary>
        void Consegna()
        {
            if (ConsegnaFatta) return;
            ConsegnaFatta = true;
            if (camRoutine != null) { StopCoroutine(camRoutine); camRoutine = null; }
            if (rig != null) rig.Snap();
            if (brain != null) brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.8f);
            if (vcam != null) vcam.gameObject.SetActive(false); // il Brain fonde dalla regia alla camera di gioco; la regia si elimina in SistemaFine
        }

        IEnumerator Chiudi(bool rivedi)
        {
            // se si sta ancora caricando una scena (saltato durante una transizione) si aspetta: un secondo caricamento verrebbe ignorato
            float guardiaCarico = 0f;
            while (SceneLoader.I != null && SceneLoader.I.IsLoading && guardiaCarico < 60f) { guardiaCarico += Time.unscaledDeltaTime; yield return null; }
            if (camRoutine != null) { StopCoroutine(camRoutine); camRoutine = null; }
            if (attoreRoutine != null) { StopCoroutine(attoreRoutine); attoreRoutine = null; }
            SpegniSottotitoli();
            if (AudioManager.I != null) AudioManager.I.StopVoice();
            if (tintaRoutine != null) { StopCoroutine(tintaRoutine); tintaRoutine = null; }
            ChiudiStudio();
            NascondiInserti();
            tinta.color = new Color(0f, 0f, 0f, 0f);
            string finaleScena = F.finale.scena;
            string destinazione = rivedi && !string.IsNullOrEmpty(retScena) ? retScena : finaleScena;
            bool regolare = ConsegnaFatta && !skip && !rivedi;
            if (!regolare)
            {
                // dissolvenza e arrivo nella scena finale con il giocatore al suo posto
                if (ScenaDa(destinazione))
                {
                    SceneLoader.I.Load(destinazione, rivedi ? retSpawn : "inizio", TransitionStyle.Fade, () => PreparaFinale(rivedi, true));
                    float g = 0f;
                    while ((SceneLoader.I.IsLoading || g < 0.1f) && g < 120f) { g += Time.unscaledDeltaTime; yield return null; }
                }
                else
                {
                    yield return FadeNero(1f, 0.5f);
                    yield return PreparaFinale(rivedi, true);
                }
                nero.color = Color.black;
                SistemaFine(rivedi, true);
                yield return FadeNero(0f, 0.9f);
            }
            else
            {
                // fine regolare: la camera e' gia' passata al gioco; il Brain finisce la fusione
                yield return AspettaSecFisso(1.9f);
                SistemaFine(rivedi, false);
            }
            yield return ChiudiInterfaccia();
        }

        IEnumerator AspettaSecFisso(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        /// <summary>Rimette il giocatore al suo posto (finale del filmato, oppure il punto di prima se lo si e' rivisto da pausa) e gli ridà il controllo.</summary>
        IEnumerator PreparaFinale(bool rivedi, bool coperto)
        {
            nero.color = Color.black;
            TrovaScenaSenzaCamera();
            if (player != null)
            {
                string scena = SceneManager.GetActiveScene().name;
                if (rivedi && retValido && scena == retScena)
                {
                    player.position = retPos; player.rotation = Quaternion.Euler(0f, retYaw, 0f);
                }
                else if (!rivedi && F.finale.pos != null && F.finale.pos.Length >= 2 && scena == F.finale.scena)
                {
                    Piazza(F.finale.pos[0], F.finale.pos[1], F.finale.yaw);
                }
            }
            yield return null;
        }

        void TrovaScenaSenzaCamera()
        {
            cam = Camera.main;
            brain = cam != null ? cam.GetComponent<CinemachineBrain>() : null;
            var p = GameObject.FindGameObjectWithTag("Player");
            player = p != null ? p.transform : null;
            tpc = p != null ? p.GetComponent<ThirdPersonController>() : null;
            cc = p != null ? p.GetComponent<CharacterController>() : null;
            pint = p != null ? p.GetComponent<PlayerInteractor>() : null;
            rig = FindAnyObjectByType<CameraRig>();
            // il CharacterController annullerebbe lo spostamento: si spegne finche' Amerigo non e' al suo posto (lo riaccende SistemaFine)
            if (tpc != null) tpc.enabled = false;
            if (pint != null) pint.enabled = false;
            if (cc != null) cc.enabled = false;
            if (vcam != null) { Destroy(vcam.gameObject); vcam = null; }
            AggancioBrain();
        }

        CinemachineBrain brainAgganciato;

        /// <summary>Ricorda la fusione normale del Brain (una volta per Brain) e la porta a stacco netto per la regia.</summary>
        void AggancioBrain()
        {
            if (brain == null) return;
            if (brain != brainAgganciato) { brainAgganciato = brain; blendOrig = brain.DefaultBlend; }
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        void SistemaFine(bool rivedi, bool riposiziona)
        {
            // ridà il controllo
            if (player != null)
            {
                var mod = player.Find(ThirdPersonController.ModelloName);
                if (mod != null) mod.gameObject.SetActive(true);
            }
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
            if (tpc != null) { tpc.enabled = true; tpc.ResetMotion(); }
            if (pint != null) pint.enabled = true;
            if (rig != null && riposiziona) rig.Snap();
            if (brain != null && brain == brainAgganciato) brain.DefaultBlend = blendOrig;
            if (vcam != null) { Destroy(vcam.gameObject); vcam = null; }
            var gm = GameManager.I;
            var s = gm.Save;
            if (rivedi) { s.scena = retScena; s.spawn = retSpawn; }
            else { s.scena = F.finale.scena; s.spawn = "inizio"; }
            gm.SaveGame();
            // la musica e gli ambienti tornano quelli della scena
            var dir = FindAnyObjectByType<SceneAudioDirector>();
            if (dir != null) dir.Reapply();
        }

        IEnumerator ChiudiInterfaccia()
        {
            UITween.Move(barSu, new Vector2(0f, AltezzaBande), 0.5f, Ease.InCubic);
            UITween.Move(barGiu, new Vector2(0f, -AltezzaBande), 0.5f, Ease.InCubic);
            UITween.Alpha(saltaGruppo, 0f, 0.3f);
            yield return AspettaSecFisso(0.55f);
            view.gameObject.SetActive(false);
        }
    }
}
