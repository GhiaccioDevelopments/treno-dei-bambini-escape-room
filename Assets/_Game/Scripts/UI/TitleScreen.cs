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
    /// R023: schermata del titolo (scena 01_Titolo). Sfondo vivo = vicolo dell'hub all'imbrunire (<see cref="MenuBackdrop"/>).
    /// Le voci stanno su una locandina appuntata a sinistra, la voce scelta e' sottolineata a matita rossa; "Continua" mostra un foglietto
    /// con l'anteprima del salvataggio. Sequenza d'apertura (nero, locandina, titolo che si imprime lettera per lettera, voci a cascata)
    /// saltabile con qualunque tasto, clic o tocco. Sottomenu Ricordi e Crediti con la carta che scorre; conferma di "Nuova partita" su un foglietto con timbro.
    /// Mouse, tastiera, gamepad e touch passano dall'EventSystem (le voci sono Button). Testi in Resources/Data/ui_menu.json.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        enum Stato { Caricamento, Intro, Menu, Conferma, Sottomenu, Lancio }

        sealed class Beat
        {
            public float t0, dur;
            public Ease ease = Ease.OutCubic;
            public Action<float> apply;
            public Action onStart;
            public bool started;
        }

        /// <summary>Una voce del menu: Button con testo; passando il mouse o con la selezione da tastiera/gamepad prende il fuoco.</summary>
        sealed class MenuRow : MonoBehaviour, IPointerEnterHandler, ISelectHandler
        {
            public TitleScreen owner;
            public string id;
            public TextMeshProUGUI label;
            public RectTransform rt;
            public void OnPointerEnter(PointerEventData e) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject); }
            public void OnSelect(BaseEventData e) { owner.Focus(this); }
        }

        GameObject root;
        RectTransform rootRt, poster, listRt, bar, preview, hint, premi;
        Image veilNero;
        MenuBackdrop backdrop;
        UiMenuFile T;
        Stato stato = Stato.Caricamento;
        readonly List<Beat> beats = new List<Beat>();
        readonly List<MenuRow> rows = new List<MenuRow>();
        float introT, introTotal;
        MenuRow focus;
        GameObject ultimoFuoco;
        RectTransform pannelloAttivo, ricordiPanel, creditiPanel, confermaRoot;
        ScrollRect creditiScroll;
        bool reduced;

        void Start()
        {
            if (UIRoot.I == null) { Debug.LogWarning("UIRoot non disponibile."); return; }
            T = GameData.UiMenu;
            reduced = UITheme.ReduceEffects || GraphicsPresets.ReduceMotion;
            Build();
            StartCoroutine(Avvio());
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (backdrop != null) Destroy(backdrop);
        }

        IEnumerator Avvio()
        {
            backdrop = gameObject.AddComponent<MenuBackdrop>();
            yield return backdrop.Run(rootRt);
            // lo sfondo fermo va dietro a tutto, ma sopra il nero va il resto
            veilNero.rectTransform.SetAsLastSibling();
            stato = Stato.Intro;
            introT = 0f;
            if (reduced) introT = Mathf.Max(0f, introTotal - 0.6f);
        }

        // ------------------------------------------------------------------ costruzione
        void Build()
        {
            rootRt = UIFactory.NewRect("Titolo", UIRoot.I.ScreenLayer);
            UIFactory.Stretch(rootRt);
            root = rootRt.gameObject;

            // fascia scura morbida sotto la locandina (leggibilita'), calda e non grigia
            var ombra = UIFactory.MakePanel(rootRt, "Ombra", new Color(0.07f, 0.035f, 0.015f, 0.34f), false);
            ombra.sprite = UIArt.Glow();
            UIFactory.Place(ombra.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-80f, 0f), new Vector2(900f, 1000f));

            BuildPoster();
            BuildPreview();

            hint = MakeHint();
            premi = UIFactory.MakeText(rootRt, T.premiUnTasto, 22, new Color(0.96f, 0.9f, 0.75f, 0f), TextAlignmentOptions.Center, FontStyles.Italic).rectTransform;
            UIFactory.Place(premi, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(500f, 34f));

            veilNero = UIFactory.MakePanel(rootRt, "Nero", Color.black, true);
            UIFactory.Stretch(veilNero.rectTransform);

            BuildIntro();
        }

        RectTransform MakeHint()
        {
            var h = UIFactory.MakeText(rootRt, T.aiutoMenu, 18, new Color(0.96f, 0.9f, 0.75f, 0f), TextAlignmentOptions.BottomRight);
            UIFactory.Place(h.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(620f, 28f));
            if (UIRoot.TouchMode || Application.isMobilePlatform) h.text = "";
            return h.rectTransform;
        }

        void BuildPoster()
        {
            var p = UIFactory.MakePaper(rootRt, "Locandina", new Color(1f, 0.96f, 0.86f, 1f), true);
            poster = p.rectTransform;
            UIFactory.Place(poster, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, -4f), new Vector2(470f, 650f));
            poster.localRotation = Quaternion.Euler(0f, 0f, -1.4f);
            poster.gameObject.AddComponent<CanvasGroup>();

            foreach (float x in new[] { -190f, 190f })
            {
                var pin = UIFactory.MakePanel(poster, "Spilla", UITheme.Ottone, false);
                pin.sprite = UIFactory.Circle();
                UIFactory.Place(pin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -22f), new Vector2(18f, 18f));
            }

            // titolo stampato, lettera per lettera
            var titleBox = UIFactory.NewRect("TitoloStampa", poster);
            titleBox.anchorMin = titleBox.anchorMax = new Vector2(0.5f, 1f);
            titleBox.pivot = new Vector2(0.5f, 1f);
            titleBox.anchoredPosition = new Vector2(0f, -34f);
            titleBox.sizeDelta = new Vector2(440f, 190f);
            titleBox.gameObject.AddComponent<RectMask2D>();
            float t = 1.7f;
            float e1 = StampLine(titleBox, T.titolo1, 0f, 74f, UITheme.Inchiostro, t);
            float e2 = StampLine(titleBox, T.titolo2, -82f, 74f, UITheme.Ceralacca, t + 0.45f);
            float endTime = Mathf.Max(e1, e2);

            var shine = UIFactory.MakePanel(titleBox, "Luce", new Color(1f, 0.9f, 0.55f, 0.5f), false);
            shine.sprite = UIArt.White();
            var sr = shine.rectTransform;
            sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(0f, 1f); sr.pivot = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(60f, 40f); sr.localRotation = Quaternion.Euler(0f, 0f, 16f);
            shine.gameObject.SetActive(false);
            beats.Add(new Beat
            {
                t0 = endTime + 0.1f, dur = 0.9f, ease = Ease.InOutCubic,
                onStart = () => { if (!reduced) { shine.gameObject.SetActive(true); UISfx.Play(Sfx.Sparkle, 0.5f); } },
                apply = v => { if (sr == null) return; sr.anchoredPosition = new Vector2(Mathf.Lerp(-70f, 520f, v), 0f); if (v >= 1f) shine.gameObject.SetActive(false); }
            });

            var rule = UIFactory.MakePanel(poster, "Filetto", UITheme.Inchiostro, false);
            UIFactory.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(360f, 3f));
            var sub = UIFactory.MakeText(poster, T.sottotitolo, 30, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(440f, 42f));
            var subCg = sub.gameObject.AddComponent<CanvasGroup>(); subCg.alpha = 0f;
            var ruleCg = rule.gameObject.AddComponent<CanvasGroup>(); ruleCg.alpha = 0f;
            beats.Add(new Beat { t0 = endTime, dur = 0.6f, apply = v => { if (subCg != null) { subCg.alpha = v; ruleCg.alpha = v; } } });

            // voci
            listRt = UIFactory.NewRect("Voci", poster);
            listRt.anchorMin = listRt.anchorMax = new Vector2(0.5f, 1f); listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = new Vector2(0f, -302f); listRt.sizeDelta = new Vector2(400f, 330f);
            var barImg = UIFactory.MakePanel(listRt, "Matita", new Color(UITheme.Ceralacca.r, UITheme.Ceralacca.g, UITheme.Ceralacca.b, 0.92f), false);
            barImg.sprite = UIArt.Tag(); barImg.type = Image.Type.Sliced;
            bar = barImg.rectTransform;
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f); bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(380f, 50f);

            bool hasSave = GameManager.I != null && GameManager.I.HasSave;
            bool esci = Application.platform != RuntimePlatform.WebGLPlayer && !Application.isMobilePlatform;
            int idx = 0;
            foreach (var v in T.voci)
            {
                if (v.id == "continua" && !hasSave) continue;
                if (v.id == "esci" && !esci) continue;
                var row = MakeRow(v, idx);
                float delay = endTime + 0.2f + idx * 0.09f;
                var rrt = row.rt;
                rrt.localScale = Vector3.zero;
                beats.Add(new Beat
                {
                    t0 = delay, dur = 0.4f, ease = Ease.OutBack,
                    onStart = () => UISfx.Play(Sfx.ClickPaper, 0.25f),
                    apply = k => { if (rrt != null) rrt.localScale = Vector3.one * k; }
                });
                idx++;
            }
            bar.gameObject.SetActive(false);
            introTotal = Mathf.Max(introTotal, endTime + 0.2f + idx * 0.09f + 1.2f);
        }

        MenuRow MakeRow(UiVoce v, int index)
        {
            var r = UIFactory.NewRect("Voce_" + v.id, listRt);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -index * 54f); r.sizeDelta = new Vector2(380f, 50f);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var btn = r.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            var tx = UIFactory.MakeText(r, v.etichetta, 33, UITheme.Inchiostro, TextAlignmentOptions.Left, FontStyles.Normal);
            UIFactory.Stretch(tx.rectTransform, 26f, 0f, 0f, 0f);
            var row = r.gameObject.AddComponent<MenuRow>();
            row.owner = this; row.id = v.id; row.label = tx; row.rt = r;
            string id = v.id;
            btn.onClick.AddListener(() => Attiva(id));
            rows.Add(row);
            return row;
        }

        void BuildPreview()
        {
            var s = GameManager.I != null ? GameManager.I.Save : null;
            var card = UIFactory.MakePanel(rootRt, "Anteprima", Color.white, false);
            card.sprite = UIArt.Tag(); card.type = Image.Type.Sliced;
            preview = card.rectTransform;
            UIFactory.Place(preview, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(560f, 118f), new Vector2(360f, 170f));
            preview.localRotation = Quaternion.Euler(0f, 0f, 3f);
            var cg = card.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f;
            var h = UIFactory.MakeText(preview, T.anteprima.intestazione, 20, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -14f), new Vector2(300f, 28f));
            var b = UIFactory.MakeText(preview, s != null ? TestoAnteprima(s) : "", 22, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(b.rectTransform, 26f, 12f, 16f, 46f);
            b.enableAutoSizing = true; b.fontSizeMin = 13; b.fontSizeMax = 22;
            if (GameManager.I == null || !GameManager.I.HasSave) card.gameObject.SetActive(false);
        }

        string TestoAnteprima(SaveData s)
        {
            var sb = new StringBuilder();
            string ultimo = s.inventario.Count > 0 ? s.inventario[s.inventario.Count - 1] : null;
            var o = ultimo != null ? GameData.Oggetto(ultimo) : null;
            if (o != null) sb.AppendLine(o.nome);
            string luogoNome = null;
            foreach (var l in GameData.Luoghi) if (l.scena == s.scena) { luogoNome = l.nome; break; }
            if (luogoNome == null) foreach (var l in GameData.Luoghi) if (l.aperto) { luogoNome = l.nome; break; }
            if (luogoNome != null) sb.AppendLine(luogoNome);
            int n = s.inventario.Count;
            sb.AppendLine(n == 0 ? T.anteprima.nessunOggetto : n == 1 ? T.anteprima.oggetto1 : string.Format(T.anteprima.oggetti, n));
            int min = Mathf.FloorToInt(s.tempoGioco / 60f);
            sb.Append(min < 1 ? T.anteprima.tempoPoco : string.Format(T.anteprima.tempo, min));
            return sb.ToString();
        }

        // ------------------------------------------------------------------ sequenza d'apertura
        void BuildIntro()
        {
            // nero che si dissolve sul vicolo
            beats.Add(new Beat { t0 = 0.1f, dur = 1.6f, apply = v => { if (veilNero != null) { var c = veilNero.color; c.a = 1f - v; veilNero.color = c; veilNero.raycastTarget = v < 1f; } } });
            // locandina che entra da sinistra
            var cg = poster.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            beats.Add(new Beat
            {
                t0 = 0.8f, dur = 0.9f,
                onStart = () => UISfx.Play(Sfx.Whoosh, 0.3f),
                apply = v => { if (poster == null) return; cg.alpha = v; poster.anchoredPosition = new Vector2(Mathf.Lerp(22f, 62f, v), -4f); }
            });
            if (hint != null)
            {
                var ht = hint.GetComponent<TextMeshProUGUI>();
                beats.Add(new Beat { t0 = introTotalHint(), dur = 0.8f, apply = v => { if (ht != null) { var c = ht.color; c.a = 0.85f * v; ht.color = c; } } });
            }
            var pt = premi.GetComponent<TextMeshProUGUI>();
            beats.Add(new Beat
            {
                t0 = 1.2f, dur = 1.0f,
                apply = v =>
                {
                    if (pt == null) return;
                    var c = pt.color; c.a = (stato == Stato.Intro ? 0.7f : 0f) * Mathf.Sin(v * Mathf.PI * 0.5f) * (v >= 1f ? 0f : 1f); pt.color = c;
                }
            });
            // anteprima
            var pcg = preview.GetComponent<CanvasGroup>();
            beats.Add(new Beat { t0 = introTotal - 1f, dur = 0.5f, apply = v => { if (pcg != null) pcg.alpha = v * (focus != null && focus.id == "continua" ? 1f : 0f); } });
            // la durata di tutta la sequenza: dopo l'ultima beat
        }

        float introTotalHint() { return Mathf.Max(0f, introTotal - 0.8f); }

        float StampLine(RectTransform parent, string text, float y, float size, Color color, float start)
        {
            var row = UIFactory.NewRect("Riga", parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, y);
            row.sizeDelta = new Vector2(440f, size * 1.15f);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.spacing = 0f;

            float t = start;
            for (int i = 0; i < text.Length; i++)
            {
                string ch = text.Substring(i, 1);
                var slot = UIFactory.NewRect("Lettera", row);
                var letter = UIFactory.MakeText(slot, ch, size, color, TextAlignmentOptions.Center, FontStyles.Bold);
                letter.textWrappingMode = TextWrappingModes.NoWrap;
                letter.rectTransform.anchorMin = letter.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                letter.rectTransform.sizeDelta = new Vector2(size, size * 1.15f);
                float w = ch == " " ? size * 0.32f : letter.GetPreferredValues(ch).x + 2f;
                var le = slot.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = w; le.preferredHeight = size * 1.15f;
                if (ch == " ") { t += 0.12f; continue; }

                var cg = letter.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                var lrt = letter.rectTransform;
                bool red = reduced;
                beats.Add(new Beat
                {
                    t0 = t, dur = red ? 0.2f : 0.5f, ease = Ease.Linear,
                    onStart = () => UISfx.Play(Sfx.TypeKey, 0.5f, 0.1f),
                    apply = v =>
                    {
                        if (lrt == null) return;
                        if (red || v >= 1f) { cg.alpha = v >= 1f ? 1f : v; lrt.anchoredPosition = Vector2.zero; lrt.localScale = Vector3.one; return; }
                        if (v < 0.7f)
                        {
                            float k = UITween.Evaluate(Ease.InCubic, v / 0.7f);
                            lrt.anchoredPosition = new Vector2(0f, Mathf.Lerp(90f, 0f, k));
                            float s = Mathf.Lerp(1.7f, 1f, k);
                            lrt.localScale = new Vector3(s, s, 1f);
                            cg.alpha = Mathf.Clamp01(k * 1.6f);
                        }
                        else
                        {
                            float k = (v - 0.7f) / 0.3f;
                            lrt.anchoredPosition = Vector2.zero;
                            lrt.localScale = new Vector3(1f + 0.12f * Mathf.Sin(k * Mathf.PI), 1f - 0.16f * Mathf.Sin(k * Mathf.PI), 1f);
                            cg.alpha = 1f;
                        }
                    }
                });
                t += 0.065f;
            }
            return t + 0.5f;
        }

        void RunIntro(bool skip)
        {
            if (skip) introT = introTotal + 1f; else introT += Time.unscaledDeltaTime;
            foreach (var b in beats)
            {
                if (introT < b.t0) continue;
                if (!b.started) { b.started = true; if (!skip && b.onStart != null) b.onStart(); }
                float p = b.dur <= 0f ? 1f : Mathf.Clamp01((introT - b.t0) / b.dur);
                b.apply(skip ? 1f : UITween.Evaluate(b.ease, p));
            }
            if (introT >= introTotal)
            {
                if (skip) UISfx.Play(Sfx.ClickPaper, 0.4f);
                FineIntro();
            }
        }

        void FineIntro()
        {
            // stato finale esatto di ogni elemento, anche per le beat fuori tempo
            foreach (var b in beats) b.apply(1f);
            beats.Clear();
            stato = Stato.Menu;
            if (veilNero != null) veilNero.gameObject.SetActive(false);
            var pt = premi.GetComponent<TextMeshProUGUI>(); if (pt != null) pt.text = "";
            if (hint != null) { var ht = hint.GetComponent<TextMeshProUGUI>(); if (ht != null) { var c = ht.color; c.a = 0.85f; ht.color = c; } }
            if (rows.Count > 0) Seleziona(rows[0].gameObject);
        }

        static bool QualunqueInput()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var m = Mouse.current;
            if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame
                              || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame)) return true;
            return false;
        }

        // ------------------------------------------------------------------ fuoco e azioni
        static void Seleziona(GameObject go) { if (EventSystem.current != null && go != null) EventSystem.current.SetSelectedGameObject(go); }

        void Focus(MenuRow r)
        {
            if (r == null || stato == Stato.Intro) return;
            bool cambia = focus != r;
            focus = r;
            ultimoFuoco = r.gameObject;
            foreach (var x in rows) x.label.color = x == r ? UITheme.CartaChiara : UITheme.Inchiostro;
            bar.gameObject.SetActive(true);
            var to = new Vector2(0f, r.rt.anchoredPosition.y);
            if (cambia)
            {
                UITween.Cancel(bar);
                if (reduced || bar.anchoredPosition == Vector2.zero && !bar.gameObject.activeSelf) bar.anchoredPosition = to;
                else UITween.Move(bar, to, 0.14f, Ease.OutCubic);
                UISfx.Play(Sfx.Hover, 0.3f);
            }
            else bar.anchoredPosition = to;
            if (preview != null && GameManager.I != null && GameManager.I.HasSave)
            {
                var cg = preview.GetComponent<CanvasGroup>();
                bool on = r.id == "continua" && (pannelloAttivo == null);
                UITween.Alpha(cg, on ? 1f : 0f, 0.2f);
            }
        }

        void Attiva(string id)
        {
            if (stato != Stato.Menu) return;
            switch (id)
            {
                case "continua": Lancia(() => GameManager.I.Continue()); break;
                case "nuova":
                    if (GameManager.I != null && GameManager.I.HasSave) MostraConferma();
                    else Lancia(() => GameManager.I.NewGame());
                    break;
                case "ricordi": if (ricordiPanel == null) ricordiPanel = BuildRicordi(); Sottomenu(ricordiPanel); break;
                case "impostazioni": if (UIRoot.I != null && UIRoot.I.Settings != null) UIRoot.I.Settings.Show(); break;
                case "crediti": if (creditiPanel == null) creditiPanel = BuildCrediti(); Sottomenu(creditiPanel); break;
                case "esci":
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        void Lancia(Action avvia)
        {
            if (GameManager.I == null) return;
            stato = Stato.Lancio;
            UISfx.Play(Sfx.ClickSeal, 0.6f);
            var cg = poster.GetComponent<CanvasGroup>();
            UITween.Alpha(cg, 0f, UITheme.Dur(0.35f), Ease.InCubic);
            UITween.Move(poster, poster.anchoredPosition + new Vector2(-40f, 0f), UITheme.Dur(0.35f), Ease.InCubic);
            if (preview != null) UITween.Alpha(preview.GetComponent<CanvasGroup>(), 0f, 0.2f);
            avvia();
        }

        // ------------------------------------------------------------------ sottomenu
        void Sottomenu(RectTransform pannello)
        {
            stato = Stato.Sottomenu;
            pannelloAttivo = pannello;
            var cgMain = poster.GetComponent<CanvasGroup>();
            cgMain.interactable = false; cgMain.blocksRaycasts = false;
            UITween.Alpha(cgMain, 0f, UITheme.Dur(0.25f), Ease.InCubic);
            UITween.Move(poster, new Vector2(22f, -4f), UITheme.Dur(0.25f), Ease.InCubic);
            if (preview != null) UITween.Alpha(preview.GetComponent<CanvasGroup>(), 0f, 0.15f);
            Entra(pannello);
            UISfx.Play(Sfx.Whoosh, 0.3f);
        }

        void Entra(RectTransform p)
        {
            p.gameObject.SetActive(true);
            var cg = p.GetComponent<CanvasGroup>();
            cg.alpha = 0f; cg.interactable = true; cg.blocksRaycasts = true;
            p.anchoredPosition = new Vector2(120f, -4f);
            UITween.Alpha(cg, 1f, UITheme.Dur(0.35f), Ease.OutCubic, null, UITheme.Dur(0.15f));
            UITween.Move(p, new Vector2(62f, -4f), UITheme.Dur(0.4f), Ease.OutCubic, () =>
            {
                var back = p.GetComponentInChildren<Button>();
                if (back != null) Seleziona(back.gameObject);
            }, UITheme.Dur(0.15f));
        }

        void Indietro()
        {
            if (stato == Stato.Conferma) { ChiudiConferma(); return; }
            if (stato != Stato.Sottomenu || pannelloAttivo == null) return;
            var p = pannelloAttivo;
            pannelloAttivo = null;
            var cgP = p.GetComponent<CanvasGroup>();
            cgP.interactable = false; cgP.blocksRaycasts = false;
            UITween.Alpha(cgP, 0f, UITheme.Dur(0.25f), Ease.InCubic, () => p.gameObject.SetActive(false));
            UITween.Move(p, new Vector2(120f, -4f), UITheme.Dur(0.25f), Ease.InCubic);
            var cgMain = poster.GetComponent<CanvasGroup>();
            UITween.Alpha(cgMain, 1f, UITheme.Dur(0.3f), Ease.OutCubic, null, UITheme.Dur(0.1f));
            UITween.Move(poster, new Vector2(62f, -4f), UITheme.Dur(0.35f), Ease.OutCubic, () =>
            {
                cgMain.interactable = true; cgMain.blocksRaycasts = true;
                stato = Stato.Menu;
                Seleziona(ultimoFuoco != null ? ultimoFuoco : (rows.Count > 0 ? rows[0].gameObject : null));
                if (focus != null) Focus(focus);
            }, UITheme.Dur(0.1f));
            UISfx.Play(Sfx.ClickPaper, 0.4f);
        }

        RectTransform NewSheet(string name, float w, float h)
        {
            var p = UIFactory.MakePaper(rootRt, name, new Color(1f, 0.96f, 0.86f, 1f), true);
            var rt = p.rectTransform;
            UIFactory.Place(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, -4f), new Vector2(w, h));
            rt.localRotation = Quaternion.Euler(0f, 0f, -0.8f);
            p.gameObject.AddComponent<CanvasGroup>();
            foreach (float x in new[] { -w / 2f + 36f, w / 2f - 36f })
            {
                var pin = UIFactory.MakePanel(rt, "Spilla", UITheme.Ottone, false);
                pin.sprite = UIFactory.Circle();
                UIFactory.Place(pin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -22f), new Vector2(18f, 18f));
            }
            rt.gameObject.SetActive(false);
            return rt;
        }

        Button BackButton(RectTransform parent, string label)
        {
            var b = UIFactory.MakeButton(parent, label, Indietro, new Vector2(240f, 56f), ButtonStyle.Seal, 26f);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(240f, 56f));
            return b;
        }

        ScrollRect MakeScroll(RectTransform parent, Vector2 offMin, Vector2 offMax, out RectTransform content)
        {
            var sv = UIFactory.NewRect("Scorri", parent);
            sv.anchorMin = Vector2.zero; sv.anchorMax = Vector2.one;
            sv.offsetMin = offMin; sv.offsetMax = offMax;
            var viewport = UIFactory.NewRect("Vista", sv);
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vimg = viewport.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0);
            content = UIFactory.NewRect("Contenuto", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.spacing = 10f; vl.padding = new RectOffset(10, 6, 4, 12);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.viewport = viewport; sr.content = content; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;
            return sr;
        }

        RectTransform BuildRicordi()
        {
            var rt = NewSheet("Ricordi", 700f, 640f);
            var title = UIFactory.MakeText(rt, T.ricordi.titolo, 52, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 66f));
            RectTransform content;
            MakeScroll(rt, new Vector2(40f, 100f), new Vector2(-40f, -118f), out content);
            var s = GameManager.I != null ? GameManager.I.Save : null;
            int n = 0;
            foreach (var o in GameData.Oggetti)
            {
                if (string.IsNullOrEmpty(o.flashback)) continue;
                n++;
                bool aperto = s != null && s.flashbackCompletati.Contains(o.flashback);
                var f = aperto ? GameData.Flashback(o.flashback) : null;
                var card = UIFactory.MakePanel(content, "Ricordo_" + o.id, Color.white, false);
                card.sprite = UIArt.Tag(); card.type = Image.Type.Sliced;
                var le = card.gameObject.AddComponent<LayoutElement>(); le.minHeight = 150f; le.preferredHeight = 150f;
                if (aperto)
                {
                    var icon = UIFactory.MakeImage(card.rectTransform, "Icona", UIFactory.LoadSprite("oggetti", o.icona), UITheme.Inchiostro);
                    UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(96f, 96f));
                }
                else
                {
                    var q = UIFactory.MakeText(card.rectTransform, "?", 90, new Color(UITheme.Inchiostro.r, UITheme.Inchiostro.g, UITheme.Inchiostro.b, 0.4f), TextAlignmentOptions.Center, FontStyles.Bold);
                    UIFactory.Place(q.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(96f, 110f));
                }
                var nome = UIFactory.MakeText(card.rectTransform, aperto ? (f != null && !string.IsNullOrEmpty(f.titolo) ? f.titolo : o.nome) : T.ricordi.bloccato, 30,
                    aperto ? UITheme.Ceralacca : UITheme.Inchiostro, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                nome.rectTransform.anchorMin = new Vector2(0f, 1f); nome.rectTransform.anchorMax = new Vector2(1f, 1f); nome.rectTransform.pivot = new Vector2(0f, 1f);
                nome.rectTransform.offsetMin = new Vector2(150f, -52f); nome.rectTransform.offsetMax = new Vector2(-24f, -12f);
                string luogo = null;
                var l = GameData.Luogo(o.luogo);
                if (l != null) luogo = l.nome;
                string corpo = aperto ? (f != null ? f.introduzione : o.descrizione) : T.ricordi.bloccatoNota;
                if (luogo != null) corpo += "\n" + T.ricordi.luogo + ": " + luogo + (aperto ? "  -  " + T.ricordi.rivissuto : "");
                var body = UIFactory.MakeText(card.rectTransform, corpo, 20, UITheme.Inchiostro, TextAlignmentOptions.TopLeft, aperto ? FontStyles.Normal : FontStyles.Italic);
                body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
                body.rectTransform.offsetMin = new Vector2(150f, 12f); body.rectTransform.offsetMax = new Vector2(-24f, -56f);
                body.enableAutoSizing = true; body.fontSizeMin = 12; body.fontSizeMax = 20;
            }
            if (n == 0)
            {
                var vuoto = UIFactory.MakeText(content, T.ricordi.nessuno, 24, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
                vuoto.gameObject.AddComponent<LayoutElement>().minHeight = 80f;
            }
            BackButton(rt, T.ricordi.indietro);
            return rt;
        }

        RectTransform BuildCrediti()
        {
            var rt = NewSheet("Crediti", 700f, 640f);
            var title = UIFactory.MakeText(rt, T.crediti.titolo, 52, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 66f));
            RectTransform content;
            creditiScroll = MakeScroll(rt, new Vector2(46f, 100f), new Vector2(-46f, -118f), out content);
            Testo(content, T.crediti.intro, 22, UITheme.Inchiostro, FontStyles.Normal);
            foreach (var r in T.crediti.squadra) Testo(content, r, 22, UITheme.InchiostroBlu, FontStyles.Bold);
            Testo(content, T.crediti.assetTitolo, 26, UITheme.Ceralacca, FontStyles.Bold | FontStyles.Italic);
            var sb = new StringBuilder();
            foreach (var c in GameData.Crediti.voci)
                sb.Append("<b>").Append(c.asset).Append("</b>  -  ").Append(c.autore).Append("  (").Append(c.licenza).Append(")\n");
            Testo(content, sb.ToString(), 17, UITheme.Inchiostro, FontStyles.Normal);
            BackButton(rt, T.crediti.indietro);
            return rt;
        }

        static void Testo(RectTransform parent, string s, float size, Color col, FontStyles st)
        {
            var t = UIFactory.MakeText(parent, s, size, col, TextAlignmentOptions.TopLeft, st);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        // ------------------------------------------------------------------ conferma "Nuova partita" su un foglietto con timbro
        void MostraConferma()
        {
            stato = Stato.Conferma;
            if (preview != null) UITween.Alpha(preview.GetComponent<CanvasGroup>(), 0f, 0.15f);
            var main = poster.GetComponent<CanvasGroup>();
            main.interactable = false;
            confermaRoot = UIFactory.NewRect("Conferma", rootRt);
            UIFactory.Stretch(confermaRoot);
            var velo = UIFactory.MakePanel(confermaRoot, "Velo", new Color(0.05f, 0.03f, 0.015f, 0.55f), true);
            UIFactory.Stretch(velo.rectTransform);
            var vcg = velo.gameObject.AddComponent<CanvasGroup>(); vcg.alpha = 0f;
            UITween.Alpha(vcg, 1f, UITheme.Dur(0.25f));

            var foglio = UIFactory.MakePaper(confermaRoot, "Foglietto", new Color(1f, 0.97f, 0.88f, 1f), true);
            var fr = foglio.rectTransform;
            UIFactory.Place(fr, new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 330f));
            fr.localRotation = Quaternion.Euler(0f, 0f, 2f);
            fr.localScale = Vector3.one * 0.8f;
            var fcg = foglio.gameObject.AddComponent<CanvasGroup>(); fcg.alpha = 0f;
            UITween.Alpha(fcg, 1f, UITheme.Dur(0.2f));
            UITween.Scale(fr, Vector3.one, UITheme.Dur(0.35f), Ease.OutBack);

            var tt = UIFactory.MakeText(fr, T.conferma.titolo, 38, UITheme.Inchiostro, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(tt.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -26f), new Vector2(300f, 50f));
            var body = UIFactory.MakeText(fr, T.conferma.testo, 24, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(body.rectTransform, 34f, 100f, 34f, 92f);
            body.enableAutoSizing = true; body.fontSizeMin = 16; body.fontSizeMax = 24;

            var stamp = UIFactory.MakePanel(fr, "Timbro", new Color(UITheme.Ceralacca.r, UITheme.Ceralacca.g, UITheme.Ceralacca.b, 0.85f), false);
            stamp.sprite = UIArt.StampFrame(); stamp.type = Image.Type.Sliced;
            UIFactory.Place(stamp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(190f, 56f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            var st = UIFactory.MakeText(stamp.rectTransform, T.conferma.timbro, 22, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(st.rectTransform);
            stamp.rectTransform.localScale = Vector3.one * 2.2f;
            var scg = stamp.gameObject.AddComponent<CanvasGroup>(); scg.alpha = 0f;
            UITween.Alpha(scg, 1f, 0.1f, Ease.Linear, null, UITheme.Dur(0.35f));
            UITween.Scale(stamp.rectTransform, Vector3.one, UITheme.Dur(0.18f), Ease.InCubic, () => UISfx.Play(Sfx.Stamp, 0.7f), UITheme.Dur(0.35f));

            var si = UIFactory.MakeButton(fr, T.conferma.si, () => { ChiudiConferma(false); Lancia(() => GameManager.I.NewGame()); }, new Vector2(220f, 58f), ButtonStyle.Seal, 24f);
            UIFactory.Place((RectTransform)si.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 24f), new Vector2(220f, 58f));
            var no = UIFactory.MakeButton(fr, T.conferma.no, () => ChiudiConferma(), new Vector2(200f, 58f), ButtonStyle.Tag, 24f);
            UIFactory.Place((RectTransform)no.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(200f, 58f));
            Seleziona(no.gameObject);
            UISfx.Play(Sfx.TagDrop, 0.6f);
        }

        void ChiudiConferma(bool ripristina = true)
        {
            if (confermaRoot == null) return;
            var r = confermaRoot.gameObject;
            confermaRoot = null;
            Destroy(r);
            if (!ripristina) return;
            stato = Stato.Menu;
            poster.GetComponent<CanvasGroup>().interactable = true;
            Seleziona(ultimoFuoco);
            if (focus != null) Focus(focus);
        }

        // ------------------------------------------------------------------ ciclo
        void Update()
        {
            if (stato == Stato.Caricamento || UIRoot.I == null) return;
            if (stato == Stato.Intro)
            {
                bool skip = introT > 0.15f && QualunqueInput();
                RunIntro(skip);
                return;
            }
            if (UIRoot.I.Settings != null && UIRoot.I.Settings.IsOpen) return; // R008: le Impostazioni gestiscono i propri tasti
            if (stato == Stato.Lancio) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool indietro = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (indietro && (stato == Stato.Sottomenu || stato == Stato.Conferma)) Indietro();

            // il fuoco non deve mai sparire (clic su un punto vuoto): torna sull'ultima voce
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                if (stato == Stato.Menu) Seleziona(ultimoFuoco);
                else if (stato == Stato.Sottomenu && pannelloAttivo != null)
                {
                    var b = pannelloAttivo.GetComponentInChildren<Button>();
                    if (b != null) Seleziona(b.gameObject);
                }
            }

            // il menu principale risponde anche a W/S
            if (stato == Stato.Menu && kb != null && focus != null)
            {
                int i = rows.IndexOf(focus);
                if (kb.wKey.wasPressedThisFrame) Seleziona(rows[(i - 1 + rows.Count) % rows.Count].gameObject);
                else if (kb.sKey.wasPressedThisFrame) Seleziona(rows[(i + 1) % rows.Count].gameObject);
            }

            // scorrimento dei crediti con tastiera e stick
            if (stato == Stato.Sottomenu && pannelloAttivo == creditiPanel && creditiScroll != null)
            {
                float dir = 0f;
                if (kb != null) dir = (kb.upArrowKey.isPressed || kb.wKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed || kb.sKey.isPressed ? 1f : 0f);
                if (gp != null) dir += InputHelper.SafeStick(gp.leftStick.ReadValue()).y;
                if (Mathf.Abs(dir) > 0.1f)
                {
                    float extra = Mathf.Max(1f, creditiScroll.content.rect.height - creditiScroll.viewport.rect.height);
                    creditiScroll.verticalNormalizedPosition = Mathf.Clamp01(creditiScroll.verticalNormalizedPosition + dir * Time.unscaledDeltaTime * 700f / extra);
                }
            }
        }
    }
}
