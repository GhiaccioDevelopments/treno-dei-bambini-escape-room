using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R023: inventario come una latta d'epoca aperta su un tavolo, ripresa in 3D davanti alla camera (prefab Resources/Inventario/StudioLatta),
    /// con gli oggetti raccolti come modelli 3D esaminabili (si girano trascinando, con lo stick destro o col tocco) e un quaderno di scuola
    /// a destra con quattro linguette: Oggetti, Ricordi, Quaderno (appunti e indizi dagli enigmi risolti, spazio per i segreti di R011) e Mappa (in attesa di R019).
    /// Il gioco resta fermo sotto (input bloccato) e la scena e' coperta dal set: apertura e chiusura con una dissolvenza in nero di circa 0,3 s.
    /// Ogni oggetto di oggetti.json puo' avere un modello (campo "modello", prefab in Resources/Modelli); senza modello compare la sua icona.
    /// Navigazione: mouse e tocco sui controlli, Q/E o LB/RB per le pagine, frecce/A/D o croce direzionale per gli oggetti, Esc/I per chiudere.
    /// Testi in Resources/Data/ui_menu.json.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        enum Pagina { Oggetti, Ricordi, Quaderno, Mappa }

        /// <summary>Anello di fuoco: si accende quando il controllo ha il fuoco (tastiera/gamepad) o il mouse sopra.</summary>
        sealed class Ring : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
        {
            public Image ring;
            public void OnSelect(BaseEventData e) { if (ring != null) ring.enabled = true; }
            public void OnDeselect(BaseEventData e) { if (ring != null) ring.enabled = false; }
            public void OnPointerEnter(PointerEventData e) { if (EventSystem.current != null && GetComponent<Selectable>() != null) EventSystem.current.SetSelectedGameObject(gameObject); }
        }

        GameObject view;
        CanvasGroup veil;
        Image veilImg;
        UiInventario T;
        UiRicordi R;
        bool busy;
        Pagina pagina = Pagina.Oggetti;
        int oggettoIdx;
        readonly List<OggettoDef> oggetti = new List<OggettoDef>();

        // UI
        RectTransform cover;
        readonly List<Button> tabs = new List<Button>();
        readonly List<RectTransform> tabRt = new List<RectTransform>();
        readonly List<CanvasGroup> pagine = new List<CanvasGroup>();
        RectTransform chooser;
        readonly List<RectTransform> chooserBtns = new List<RectTransform>();
        TextMeshProUGUI oggHeading, oggBody, oggTema, oggRicordo, helpText;
        RectTransform oggStamp;
        RectTransform ricordiContent, quadernoContent;
        TextMeshProUGUI mappaTitolo, mappaTesto;

        // 3D
        GameObject studio;
        Camera studioCam, mainCam;
        Transform punto;
        GameObject oggetto3d;
        float yaw, pitch, autoSpin;
        Light candela;
        float sunOld, ambOld, refOld; bool fogOld; Light sun;
        CanvasGroup hud; float hudAlpha;

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            T = GameData.UiMenu.inventario;
            R = GameData.UiMenu.ricordi;
            BuildUi();
            view.SetActive(false);
        }

        // ------------------------------------------------------------------ interfaccia
        void BuildUi()
        {
            var v = UIFactory.NewRect("VistaLatta", transform);
            UIFactory.Stretch(v);
            view = v.gameObject;

            var vig = UIFactory.MakePanel(v, "Vignetta", Color.white, false);
            vig.sprite = UIArt.Vignette(new Color(0.03f, 0.015f, 0.005f, 1f));
            UIFactory.Stretch(vig.rectTransform);

            // cartellino della latta, tasto Chiudi
            var tag = UIFactory.MakePanel(v, "Cartellino", Color.white, false);
            tag.sprite = UIArt.Brass(); tag.type = Image.Type.Sliced;
            UIFactory.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -36f), new Vector2(250f, 56f));
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 2f);
            var tt = UIFactory.MakeText(tag.rectTransform, T.titoloLatta, 30, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(tt.rectTransform);
            var close = UIFactory.MakeButton(v, T.chiudi, Hide, new Vector2(150f, 56f), ButtonStyle.Seal, 24f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(316f, -34f), new Vector2(150f, 56f));
            AddRing(close.gameObject);

            // scelta dell'oggetto (in basso a sinistra)
            chooser = UIFactory.NewRect("Scelta", v);
            chooser.anchorMin = chooser.anchorMax = new Vector2(0f, 0f); chooser.pivot = new Vector2(0f, 0f);
            chooser.anchoredPosition = new Vector2(46f, 52f); chooser.sizeDelta = new Vector2(640f, 64f);
            var hl = chooser.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10f; hl.childAlignment = TextAnchor.LowerLeft;
            hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;

            helpText = UIFactory.MakeText(v, "", 18, new Color(0.96f, 0.9f, 0.75f, 0.85f), TextAlignmentOptions.BottomLeft);
            UIFactory.Place(helpText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(46f, 16f), new Vector2(680f, 28f));

            // quaderno di scuola in piedi, a destra
            var cv = UIFactory.MakePanel(v, "Quaderno", Color.white, false);
            cv.sprite = UIArt.Cover(); cv.type = Image.Type.Sliced;
            cover = cv.rectTransform;
            UIFactory.Place(cover, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, -22f), new Vector2(520f, 590f));
            cover.localRotation = Quaternion.Euler(0f, 0f, 1.2f);

            // linguette (dietro la pagina)
            for (int i = 0; i < T.sezioni.Count; i++)
            {
                int idx = i;
                var b = UIFactory.MakeButton(cover, T.sezioni[i].etichetta, () => VaiA((Pagina)idx), new Vector2(112f, 44f), ButtonStyle.Tag, 20f);
                var r = (RectTransform)b.transform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 0f);
                r.sizeDelta = new Vector2(112f, 44f);
                r.anchoredPosition = new Vector2(30f + i * 118f, -8f);
                r.SetAsFirstSibling();
                AddRing(b.gameObject);
                tabs.Add(b); tabRt.Add(r);
            }

            var page = UIFactory.MakePanel(cover, "Pagina", Color.white, false);
            page.sprite = UIArt.Notebook(); page.type = Image.Type.Sliced;
            UIFactory.Stretch(page.rectTransform, 14f, 14f, 14f, 14f);
            var ruled = UIFactory.NewRect("Righe", page.rectTransform);
            UIFactory.Stretch(ruled, 16f, 16f, 16f, 16f);
            var rr = ruled.gameObject.AddComponent<RawImage>(); rr.texture = UIArt.Ruled(); rr.raycastTarget = false;
            rr.uvRect = new Rect(0f, 0f, 1f, 17f);

            BuildPaginaOggetti(page.rectTransform);
            BuildPaginaRicordi(page.rectTransform);
            BuildPaginaQuaderno(page.rectTransform);
            BuildPaginaMappa(page.rectTransform);

            // velo nero per la dissolvenza di apertura e chiusura (sopra a tutto)
            veilImg = UIFactory.MakePanel(transform, "Dissolvenza", Color.black, true);
            UIFactory.Stretch(veilImg.rectTransform);
            veil = veilImg.gameObject.AddComponent<CanvasGroup>();
            veil.alpha = 0f; veil.blocksRaycasts = false;
            veilImg.gameObject.SetActive(false);
        }

        void AddRing(GameObject go)
        {
            var r = UIFactory.MakePanel(go.transform, "Fuoco", UITheme.Ceralacca, false);
            r.sprite = UIArt.StampFrame(); r.type = Image.Type.Sliced; r.fillCenter = false;
            UIFactory.Stretch(r.rectTransform, -5f, -5f, -5f, -5f);
            r.enabled = false;
            go.AddComponent<Ring>().ring = r;
        }

        RectTransform NewPagina(RectTransform parent, string nome)
        {
            var r = UIFactory.NewRect(nome, parent);
            UIFactory.Stretch(r);
            var cg = r.gameObject.AddComponent<CanvasGroup>();
            pagine.Add(cg);
            return r;
        }

        void BuildPaginaOggetti(RectTransform page)
        {
            var p = NewPagina(page, "PaginaOggetti");
            oggHeading = UIFactory.MakeText(p, "", 40, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(oggHeading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(420f, 54f));
            // descrizione (testo dritto) e significato (corsivo, come una nota a margine) nello stesso riquadro
            oggBody = UIFactory.MakeText(p, "", 20, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            UIFactory.Stretch(oggBody.rectTransform, 40f, 200f, 30f, 78f);
            oggBody.enableAutoSizing = true; oggBody.fontSizeMin = 10; oggBody.fontSizeMax = 20;
            // ricordo collegato e suo stato
            oggRicordo = UIFactory.MakeText(p, "", 18, UITheme.Ceralacca, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            UIFactory.Place(oggRicordo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 144f), new Vector2(420f, 52f));
            oggRicordo.enableAutoSizing = true; oggRicordo.fontSizeMin = 11; oggRicordo.fontSizeMax = 18;
            var st = UIFactory.MakePanel(p, "TimbroTema", new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.85f), false);
            st.sprite = UIArt.StampFrame(); st.type = Image.Type.Sliced;
            oggStamp = st.rectTransform;
            UIFactory.Place(oggStamp, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(410f, 112f));
            oggStamp.localRotation = Quaternion.Euler(0f, 0f, -2f);
            var h = UIFactory.MakeText(oggStamp, T.etichettaTema.ToUpperInvariant(), 16, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(390f, 24f));
            oggTema = UIFactory.MakeText(oggStamp, "", 15, UITheme.InchiostroBlu, TextAlignmentOptions.Center);
            UIFactory.Stretch(oggTema.rectTransform, 14f, 8f, 14f, 34f);
            oggTema.enableAutoSizing = true; oggTema.fontSizeMin = 9; oggTema.fontSizeMax = 15;
        }

        RectTransform ScrollPagina(RectTransform p, out RectTransform content)
        {
            var sv = UIFactory.NewRect("Scorri", p);
            UIFactory.Stretch(sv, 36f, 28f, 30f, 28f);
            var vp = UIFactory.NewRect("Vista", sv);
            UIFactory.Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            vp.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            content = UIFactory.NewRect("Contenuto", vp);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.spacing = 8f; vl.padding = new RectOffset(10, 6, 4, 12);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.viewport = vp; sr.content = content; sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;
            sr.movementType = ScrollRect.MovementType.Clamped;
            return sv;
        }

        void BuildPaginaRicordi(RectTransform page)
        {
            var p = NewPagina(page, "PaginaRicordi");
            ScrollPagina(p, out ricordiContent);
        }

        void BuildPaginaQuaderno(RectTransform page)
        {
            var p = NewPagina(page, "PaginaQuaderno");
            ScrollPagina(p, out quadernoContent);
        }

        void BuildPaginaMappa(RectTransform page)
        {
            var p = NewPagina(page, "PaginaMappa");
            mappaTitolo = UIFactory.MakeText(p, T.mappa.titolo, 40, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(mappaTitolo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(420f, 54f));
            var frame = UIFactory.MakePanel(p, "Riquadro", new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.55f), false);
            frame.sprite = UIArt.DashedFrame(); frame.type = Image.Type.Sliced;
            UIFactory.Stretch(frame.rectTransform, 40f, 70f, 36f, 100f);
            mappaTesto = UIFactory.MakeText(frame.rectTransform, T.mappa.testo, 24, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Stretch(mappaTesto.rectTransform, 24f, 24f, 24f, 24f);
        }

        // ------------------------------------------------------------------ contenuti
        void RefreshOggetti()
        {
            oggetti.Clear();
            oggetti.AddRange(GameData.Oggetti);
            var save = GameManager.I != null ? GameManager.I.Save : null;
            UIFactory.Clear(chooser);
            chooserBtns.Clear();
            for (int i = 0; i < oggetti.Count; i++)
            {
                int idx = i;
                var o = oggetti[i];
                bool trovato = save != null && save.inventario.Contains(o.id);
                // gli oggetti da trovare si possono scegliere: la scheda dice dove cercarli (senza svelare il nome)
                var b = UIFactory.MakeButton(chooser, trovato ? o.nome : T.daTrovare, () => SelezionaOggetto(idx), new Vector2(trovato ? 200f : 150f, 56f),
                    trovato ? ButtonStyle.Tag : ButtonStyle.Card, 22f);
                ((RectTransform)b.transform).sizeDelta = new Vector2(trovato ? 200f : 150f, 56f);
                AddRing(b.gameObject);
                chooserBtns.Add((RectTransform)b.transform);
            }
            if (oggetti.Count == 0) oggettoIdx = -1;
            else
            {
                int first = oggetti.FindIndex(o => save != null && save.inventario.Contains(o.id));
                if (oggettoIdx < 0 || oggettoIdx >= oggetti.Count || !(save != null && save.inventario.Contains(oggetti[oggettoIdx].id))) oggettoIdx = first;
            }
            SelezionaOggetto(oggettoIdx, false);
        }

        void SelezionaOggetto(int idx, bool suono = true)
        {
            oggettoIdx = idx;
            OggettoDef o = idx >= 0 && idx < oggetti.Count ? oggetti[idx] : null;
            var save = GameManager.I != null ? GameManager.I.Save : null;
            bool trovato = o != null && save != null && save.inventario.Contains(o.id);
            if (o == null)
            {
                // la latta e' vuota
                oggHeading.text = "";
                oggBody.text = T.latteVuota ?? "";
                oggRicordo.text = "";
                oggTema.text = "";
                oggStamp.gameObject.SetActive(false);
            }
            else if (!trovato)
            {
                // da trovare: solo l'indizio sul luogo, nessun nome e nessun ricordo
                var luogo = GameData.Luogo(o.luogo);
                oggHeading.text = T.daTrovare;
                oggBody.text = luogo != null && !string.IsNullOrEmpty(luogo.nome) ? string.Format(T.cercaVicino, luogo.nome) : T.cercaGenerico;
                oggRicordo.text = "";
                oggTema.text = "";
                oggStamp.gameObject.SetActive(false);
            }
            else
            {
                oggHeading.text = o.nome;
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(o.descrizione)) sb.Append(o.descrizione);
                if (!string.IsNullOrEmpty(o.significato)) { if (sb.Length > 0) sb.Append("\n\n"); sb.Append("<i>").Append(o.significato).Append("</i>"); }
                oggBody.text = sb.ToString();
                oggRicordo.text = TestoRicordo(o, save);
                oggTema.text = o.temaCivico ?? "";
                oggStamp.gameObject.SetActive(!string.IsNullOrEmpty(o.temaCivico));
            }
            if (suono) UISfx.Play(Sfx.ClickPaper, 0.5f);
            // evidenzia l'oggetto scelto nella fila (sollevato)
            for (int i = 0; i < chooserBtns.Count; i++) chooserBtns[i].localScale = Vector3.one * (i == idx ? 1.08f : 1f);
            MostraOggetto3d(trovato ? o : null);
        }

        /// <summary>Riga "Ricordo: titolo - Rivissuto / Da rivivere" della scheda; vuota se l'oggetto non ha un ricordo.</summary>
        string TestoRicordo(OggettoDef o, SaveData save)
        {
            if (o == null || string.IsNullOrEmpty(o.flashback)) return "";
            var f = GameData.Flashback(o.flashback);
            if (f == null) return "<b>" + T.etichettaRicordo + ":</b> " + T.ricordoNonDisponibile;
            bool fatto = save != null && save.flashbackCompletati.Contains(o.flashback);
            string titolo = !string.IsNullOrEmpty(f.titolo) ? f.titolo : o.nome;
            return "<b>" + T.etichettaRicordo + ":</b> " + titolo + " - <i>" + (fatto ? T.ricordoStato : T.ricordoDaFare) + "</i>";
        }

        void MostraOggetto3d(OggettoDef o)
        {
            if (punto == null) return;
            if (oggetto3d != null) { Destroy(oggetto3d); oggetto3d = null; }
            if (o == null) return;
            GameObject prefab = string.IsNullOrEmpty(o.modello) ? null : Resources.Load<GameObject>("Modelli/" + o.modello);
            if (prefab != null)
            {
                oggetto3d = Instantiate(prefab, punto);
                oggetto3d.transform.localPosition = Vector3.zero;
                oggetto3d.transform.localRotation = Quaternion.identity;
                // centra il modello sul punto
                var rs = oggetto3d.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    // ogni modello entra in circa 30 cm, anche se e' stato fatto piu' grande o piu' piccolo
                    float ingombro = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (ingombro > 0.0001f) oggetto3d.transform.localScale *= 0.3f / ingombro;
                    b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    oggetto3d.transform.position += punto.position - b.center;
                }
            }
            else
            {
                // senza modello: l'icona su una cartolina in piedi
                var spr = UIFactory.LoadSprite("oggetti", o.icona);
                if (spr == null) return;
                oggetto3d = new GameObject("Icona_" + o.id);
                oggetto3d.transform.SetParent(punto, false);
                var sr = oggetto3d.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                float s = 0.22f / Mathf.Max(spr.bounds.size.x, spr.bounds.size.y);
                oggetto3d.transform.localScale = Vector3.one * s;
            }
            yaw = 25f; pitch = -10f; autoSpin = 0f;
        }

        void RefreshRicordi()
        {
            UIFactory.Clear(ricordiContent);
            var save = GameManager.I != null ? GameManager.I.Save : null;
            int n = 0;
            foreach (var o in GameData.Oggetti)
            {
                if (string.IsNullOrEmpty(o.flashback)) continue;
                n++;
                bool aperto = save != null && save.flashbackCompletati.Contains(o.flashback);
                // M5: se l'oggetto non e' stato trovato (e il ricordo non e' rivissuto) non si svelano titolo ne' descrizione, come nel sottomenu Ricordi del titolo
                bool trovato = save != null && save.inventario.Contains(o.id);
                bool visibile = aperto || trovato;
                var f = visibile ? GameData.Flashback(o.flashback) : null;
                var luogoDef = GameData.Luogo(o.luogo);
                var card = UIFactory.MakePanel(ricordiContent, "Ricordo_" + o.id, Color.white, false);
                card.sprite = UIArt.Tag(); card.type = Image.Type.Sliced;
                card.gameObject.AddComponent<LayoutElement>().minHeight = 210f;
                var title = UIFactory.MakeText(card.rectTransform, !visibile ? R.bloccato : f != null && !string.IsNullOrEmpty(f.titolo) ? f.titolo : o.nome, 26, UITheme.Ceralacca, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f); title.rectTransform.pivot = new Vector2(0f, 1f);
                title.rectTransform.offsetMin = new Vector2(24f, -50f); title.rectTransform.offsetMax = new Vector2(-24f, -10f);
                var stato = UIFactory.MakeText(card.rectTransform, aperto ? T.ricordoStato : trovato ? T.ricordoDaFare : T.daTrovare, 17, aperto ? UITheme.Esatto : UITheme.InchiostroBlu, TextAlignmentOptions.TopRight, FontStyles.Italic);
                stato.rectTransform.anchorMin = new Vector2(1f, 1f); stato.rectTransform.anchorMax = new Vector2(1f, 1f); stato.rectTransform.pivot = new Vector2(1f, 1f);
                stato.rectTransform.anchoredPosition = new Vector2(-18f, -50f); stato.rectTransform.sizeDelta = new Vector2(240f, 24f);
                var body = UIFactory.MakeText(card.rectTransform, !visibile ? ProssimoRicordo(luogoDef) : aperto && f != null ? f.introduzione : o.descrizione, 18, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
                body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
                body.rectTransform.offsetMin = new Vector2(24f, 10f); body.rectTransform.offsetMax = new Vector2(-24f, -76f);
                body.enableAutoSizing = true; body.fontSizeMin = 11; body.fontSizeMax = 20;
            }
            if (n == 0) NotaVuota(ricordiContent, T.ricordiVuoto);
        }

        string ProssimoRicordo(LuogoDef luogo)
        {
            string s = R.bloccatoNota ?? "";
            if (luogo != null && !string.IsNullOrEmpty(luogo.nome)) s += "\n" + R.luogo + ": " + luogo.nome;
            return s;
        }

        void RefreshQuaderno()
        {
            UIFactory.Clear(quadernoContent);
            var save = GameManager.I != null ? GameManager.I.Save : null;

            // appunti: la spiegazione degli enigmi risolti
            Intestazione(quadernoContent, T.quaderno.appunti);
            var appunti = new StringBuilder();
            var indizi = new StringBuilder();
            foreach (var o in GameData.Oggetti)
            {
                if (string.IsNullOrEmpty(o.flashback)) continue;
                var f = GameData.Flashback(o.flashback);
                if (f == null) continue;
                foreach (var e in f.enigmi)
                {
                    if (save == null || !save.enigmiRisolti.Contains(f.id + "/" + e.id)) continue;
                    if (!string.IsNullOrEmpty(e.spiegazione)) appunti.Append("<b>").Append(e.titolo).Append("</b>\n").Append(e.spiegazione).Append("\n\n");
                    if (e.indizi != null && e.indizi.Count > 0)
                    {
                        indizi.Append("<b>").Append(e.titolo).Append("</b>\n");
                        foreach (var i in e.indizi) indizi.Append("- ").Append(i).Append('\n');
                        indizi.Append('\n');
                    }
                }
            }
            if (appunti.Length > 0) Riga(quadernoContent, appunti.ToString().TrimEnd());
            else NotaVuota(quadernoContent, T.quaderno.appuntiVuoto);

            Intestazione(quadernoContent, T.quaderno.indizi);
            if (indizi.Length > 0) Riga(quadernoContent, indizi.ToString().TrimEnd());
            else NotaVuota(quadernoContent, T.quaderno.indiziVuoto);

            // segreti: R011 imposta una volta QuadernoSegreti.Provider (funzione che legge dal salvataggio i segreti trovati)
            Intestazione(quadernoContent, T.quaderno.segreti);
            var segreti = QuadernoSegreti.Trovati();
            if (segreti.Count == 0) NotaVuota(quadernoContent, T.quaderno.segretiVuoto);
            else foreach (var s in segreti) Riga(quadernoContent, "<b>" + s.titolo + "</b>\n" + s.testo);
        }

        static void Intestazione(RectTransform parent, string s)
        {
            var t = UIFactory.MakeText(parent, s, 30, UITheme.Ceralacca, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 40f;
        }

        static void Riga(RectTransform parent, string s)
        {
            var t = UIFactory.MakeText(parent, s, 19, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        static void NotaVuota(RectTransform parent, string s)
        {
            var t = UIFactory.MakeText(parent, s, 19, new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.7f), TextAlignmentOptions.TopLeft, FontStyles.Italic);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void VaiA(Pagina p, bool suono = true)
        {
            pagina = p;
            for (int i = 0; i < pagine.Count; i++)
            {
                bool on = i == (int)p;
                pagine[i].gameObject.SetActive(on);
                pagine[i].alpha = on ? 0f : 1f;
                if (on) UITween.Alpha(pagine[i], 1f, UITheme.Dur(0.18f));
            }
            // i contenuti si costruiscono a pagina accesa, cosi' il layout e i testi con ridimensionamento automatico hanno le misure giuste
            if (p == Pagina.Ricordi) RefreshRicordi();
            if (p == Pagina.Quaderno) RefreshQuaderno();
            if (p == Pagina.Ricordi || p == Pagina.Quaderno) UIFactory.RefreshText((RectTransform)pagine[(int)p].transform);
            for (int i = 0; i < tabRt.Count; i++)
            {
                bool on = i == (int)p;
                tabRt[i].anchoredPosition = new Vector2(tabRt[i].anchoredPosition.x, on ? -2f : -10f);
                var img = tabRt[i].GetComponent<Image>();
                if (img != null) img.color = on ? Color.white : new Color(0.78f, 0.72f, 0.58f, 1f);
            }
            if (suono) UISfx.Play(Sfx.ClickPaper, 0.5f);
        }

        // ------------------------------------------------------------------ apertura e chiusura
        public void Show()
        {
            if (IsOpen || busy) return;
            IsOpen = true;
            if (GameManager.I != null) GameManager.I.PushBlock("inventario");
            transform.SetAsLastSibling();
            StartCoroutine(Apri());
        }

        public void Hide()
        {
            if (!IsOpen || busy) return;
            StartCoroutine(Chiudi());
        }

        IEnumerator Dissolvi(float a, float durata)
        {
            veilImg.gameObject.SetActive(true);
            veil.blocksRaycasts = true;
            float da = veil.alpha, t = 0f;
            durata = UITheme.Dur(durata);
            while (t < durata)
            {
                t += Time.unscaledDeltaTime;
                veil.alpha = Mathf.Lerp(da, a, Mathf.Clamp01(t / durata));
                yield return null;
            }
            veil.alpha = a;
            if (a <= 0f) { veil.blocksRaycasts = false; veilImg.gameObject.SetActive(false); }
        }

        IEnumerator Apri()
        {
            busy = true;
            UISfx.Play(Sfx.SuitcaseOpen, 0.6f);
            yield return Dissolvi(1f, 0.13f);

            var prefab = Resources.Load<GameObject>("Inventario/StudioLatta");
            if (prefab != null)
            {
                studio = Instantiate(prefab, new Vector3(800f, 0f, 800f), Quaternion.identity);
                studioCam = studio.GetComponentInChildren<Camera>(true);
                punto = studio.transform.Find("PuntoOggetto");
                var luce = studio.transform.Find("Candela/LuceCandela");
                candela = luce != null ? luce.GetComponent<Light>() : null;
                mainCam = Camera.main;
                if (mainCam != null) mainCam.enabled = false;
                if (studioCam != null) studioCam.enabled = true;
                sun = RenderSettings.sun;
                if (sun != null) { sunOld = sun.intensity; sun.intensity = 0f; }
                ambOld = RenderSettings.ambientIntensity; RenderSettings.ambientIntensity = 0.2f;
                fogOld = RenderSettings.fog; RenderSettings.fog = false;
                refOld = RenderSettings.reflectionIntensity; RenderSettings.reflectionIntensity = 0.25f;
            }
            hud = UIRoot.I != null && UIRoot.I.HudLayer != null ? UIRoot.I.HudLayer.GetComponent<CanvasGroup>() : null;
            if (hud != null) { hudAlpha = hud.alpha; hud.alpha = 0f; hud.blocksRaycasts = false; }
            helpText.text = UIRoot.TouchMode ? T.aiutoTouch : T.aiuto;
            view.SetActive(true);
            RefreshOggetti();
            VaiA(Pagina.Oggetti, false);
            var first = tabs.Count > 0 ? tabs[0].gameObject : null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first);
            yield return Dissolvi(0f, 0.15f);
            busy = false;
        }

        IEnumerator Chiudi()
        {
            busy = true;
            UISfx.Play(Sfx.SuitcaseClose, 0.6f);
            yield return Dissolvi(1f, 0.13f);
            Ripristina();
            view.SetActive(false);
            IsOpen = false;
            if (GameManager.I != null) GameManager.I.PopBlock("inventario");
            yield return Dissolvi(0f, 0.15f);
            busy = false;
        }

        void Ripristina()
        {
            if (oggetto3d != null) { Destroy(oggetto3d); oggetto3d = null; }
            if (studio != null) { Destroy(studio); studio = null; }
            if (mainCam != null) mainCam.enabled = true;
            if (sun != null) sun.intensity = sunOld;
            if (studioCam != null || mainCam != null) { RenderSettings.ambientIntensity = ambOld; RenderSettings.fog = fogOld; RenderSettings.reflectionIntensity = refOld; }
            if (hud != null) { hud.alpha = hudAlpha; hud.blocksRaycasts = true; hud = null; }
            studioCam = null; mainCam = null; punto = null; sun = null; candela = null;
        }

        void OnDisable()
        {
            if (studio != null) { Ripristina(); }
        }

        // ------------------------------------------------------------------ ciclo
        void Update()
        {
            if (!IsOpen || busy) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;

            if (kb != null)
            {
                if (kb.eKey.wasPressedThisFrame) VaiA((Pagina)(((int)pagina + 1) % 4));
                else if (kb.qKey.wasPressedThisFrame) VaiA((Pagina)(((int)pagina + 3) % 4));
                if (pagina == Pagina.Oggetti)
                {
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Passa(1);
                    else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Passa(-1);
                }
            }
            if (gp != null)
            {
                if (gp.rightShoulder.wasPressedThisFrame) VaiA((Pagina)(((int)pagina + 1) % 4));
                else if (gp.leftShoulder.wasPressedThisFrame) VaiA((Pagina)(((int)pagina + 3) % 4));
                if (pagina == Pagina.Oggetti)
                {
                    if (gp.dpad.right.wasPressedThisFrame) Passa(1);
                    else if (gp.dpad.left.wasPressedThisFrame) Passa(-1);
                }
                if (gp.buttonEast.wasPressedThisFrame) Hide();
            }

            // il fuoco non sparisce mai
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && tabs.Count > 0)
                EventSystem.current.SetSelectedGameObject(tabs[(int)pagina].gameObject);

            // oggetto: si gira trascinando (mouse o dito) o con lo stick destro
            if (oggetto3d != null && punto != null)
            {
                var ptr = Pointer.current;
                bool trascina = ptr != null && ptr.press.isPressed && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject();
                bool fermo = UITheme.ReduceEffects || GraphicsPresets.ReduceMotion;
                if (trascina) { var d = ptr.delta.ReadValue(); yaw -= d.x * 0.4f; pitch = Mathf.Clamp(pitch + d.y * 0.3f, -60f, 60f); autoSpin = 0f; }
                else if (gp != null)
                {
                    var st = InputHelper.SafeStick(gp.rightStick.ReadValue());
                    if (st.sqrMagnitude > 0.01f) { yaw -= st.x * 180f * Time.unscaledDeltaTime; pitch = Mathf.Clamp(pitch + st.y * 120f * Time.unscaledDeltaTime, -60f, 60f); autoSpin = 0f; }
                }
                if (!trascina && !fermo) { autoSpin = Mathf.MoveTowards(autoSpin, 14f, 20f * Time.unscaledDeltaTime); yaw += autoSpin * Time.unscaledDeltaTime; }
                punto.localRotation = Quaternion.Euler(pitch, yaw, 0f);
                float bob = fermo ? 0f : Mathf.Sin(Time.unscaledTime * 1.2f) * 0.006f;
                punto.localPosition = new Vector3(-0.11f, 0.19f + bob, -0.04f);
            }
            if (candela != null && !UITheme.ReduceEffects) candela.intensity = 1.6f + Mathf.PerlinNoise(Time.unscaledTime * 6f, 3.1f) * 0.5f;
        }

        void Passa(int dir)
        {
            if (oggetti.Count == 0) return;
            var save = GameManager.I != null ? GameManager.I.Save : null;
            int n = oggetti.Count, i = oggettoIdx;
            // si passa anche sugli oggetti da trovare: la loro scheda dice dove cercarli
            i = (i + dir + n) % n;
            SelezionaOggetto(i);
        }
    }
}
