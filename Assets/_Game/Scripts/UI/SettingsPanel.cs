using System;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R008: menu Impostazioni a schede (Generale, Ombre, Luci, Effetti, Meteo, Dettaglio, Accessibilita'), costruito da codice nello stile della UI d'epoca.
    /// Ogni voce ha una riga di spiegazione, si applica subito (vedi <see cref="GraphicsPresets"/>) e si salva. Le voci non supportate su WebGL non compaiono.
    /// Si apre dalla Pausa e dal Titolo; Esc chiude.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        const float CardW = 1060f, CardH = 670f, RowH = 78f, ViewW = 960f;

        GameObject view;
        PanelFx fx;
        RectTransform viewport, content;
        ScrollRect scroll;
        readonly List<Button> tabs = new List<Button>();
        readonly List<Riga> righe = new List<Riga>();
        string gruppo = "Generale";
        bool built;
        GameObject ripristinaBtn;   // vale solo per la grafica: nella scheda Gioco resta nascosto

        /// <summary>Una riga del menu: etichetta, spiegazione, controllo a frecce.</summary>
        sealed class Riga
        {
            public string nome, spiega;
            public Func<string> testo;       // testo del valore attuale
            public Action<int> passo;        // +1 / -1
            public TextMeshProUGUI valore;
            public Func<bool> visibile;
        }

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            var veil = UIFactory.MakePanel(transform, "Vista", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            view = veil.gameObject;
            fx = view.AddComponent<PanelFx>();
            fx.entry = PanelEntry.Pop;
            view.SetActive(false);
        }

        // ---------------------------------------------------------------- costruzione

        void Build()
        {
            built = true;
            var veil = (RectTransform)view.transform;
            var card = UIFactory.MakePaper(veil, "Scheda", Color.white, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW, CardH));
            fx.card = card.rectTransform;
            UIFactory.MakeBrassFrame(card.rectTransform, 8f);

            var title = UIFactory.MakeText(card.rectTransform, "Impostazioni", 40, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(700f, 52f));

            // schede
            var tabRow = UIFactory.NewRect("Schede", card.rectTransform);
            UIFactory.Place(tabRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(ViewW + 40f, 56f));
            var h = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false; h.childControlHeight = false; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            float tw = (ViewW + 40f - 4f * (GraphicsOptions.Gruppi.Length - 1)) / GraphicsOptions.Gruppi.Length;
            foreach (var g in GraphicsOptions.Gruppi)
            {
                string gg = g;
                var b = UIFactory.MakeButton(tabRow, g, () => SelezionaScheda(gg), new Vector2(tw, 56f), ButtonStyle.Tag, 21f);
                tabs.Add(b);
            }

            // elenco scorrevole
            viewport = UIFactory.NewRect("Vista", card.rectTransform);
            viewport.anchorMin = new Vector2(0.5f, 0f); viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = new Vector2(-ViewW * 0.5f, 92f); viewport.offsetMax = new Vector2(ViewW * 0.5f, -140f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vimg = viewport.gameObject.AddComponent<Image>(); // area cliccabile per la rotella e il trascinamento
            vimg.color = new Color(1f, 1f, 1f, 0.001f);
            content = UIFactory.NewRect("Voci", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(-24f, 100f);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

            // barra di scorrimento a leva
            var track = UIFactory.MakePanel(card.rectTransform, "Binario", new Color(0.16f, 0.1f, 0.06f, 0.35f), true);
            track.sprite = UIArt.PaperDark(); track.type = Image.Type.Sliced;
            track.rectTransform.anchorMin = new Vector2(0.5f, 0f); track.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.rectTransform.offsetMin = new Vector2(ViewW * 0.5f + 6f, 92f); track.rectTransform.offsetMax = new Vector2(ViewW * 0.5f + 26f, -140f);
            var slideArea = UIFactory.NewRect("Area", track.rectTransform);
            UIFactory.Stretch(slideArea, 0f, 0f, 0f, 0f);
            var handle = UIFactory.MakePanel(slideArea, "Maniglia", Color.white, true);
            handle.sprite = UIArt.Brass(); handle.type = Image.Type.Sliced;
            handle.rectTransform.anchorMin = Vector2.zero; handle.rectTransform.anchorMax = Vector2.one;
            handle.rectTransform.offsetMin = Vector2.zero; handle.rectTransform.offsetMax = Vector2.zero;
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle.rectTransform; sb.targetGraphic = handle; sb.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0f;

            // piede
            var reset = UIFactory.MakeButton(card.rectTransform, "Ripristina predefiniti", OnRipristina, new Vector2(380f, 60f), ButtonStyle.Tag, 25f);
            ripristinaBtn = reset.gameObject;
            UIFactory.Place((RectTransform)reset.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 20f), new Vector2(380f, 60f));
            var close = UIFactory.MakeButton(card.rectTransform, "Chiudi", Hide, new Vector2(300f, 60f), ButtonStyle.Seal, 28f, true);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 20f), new Vector2(300f, 60f));
        }

        // ---------------------------------------------------------------- righe

        void RiempiScheda()
        {
            righe.Clear();
            UIFactory.Clear(content);
            if (gruppo == "Generale") RigaPreset();
            foreach (var d in GraphicsOptions.Defs)
            {
                if (d.gruppo != gruppo || !GraphicsPresets.Disponibile(d.id)) continue;
                var def = d;
                var r = new Riga
                {
                    nome = def.nome,
                    spiega = def.spiega + (string.IsNullOrEmpty(def.nota) ? "" : " " + def.nota),
                    testo = () => GraphicsPresets.TestoOpzione(def.id, GraphicsPresets.Get(def.id)),
                    passo = dir =>
                    {
                        int n = GraphicsPresets.NumOpzioni(def.id);
                        GraphicsPresets.Set(def.id, ((GraphicsPresets.Get(def.id) + dir) % n + n) % n);
                    }
                };
                righe.Add(r);
            }
            if (gruppo == "Gioco") RigheMappa();
            if (gruppo == "Accessibilità") { RigaEffettiRidotti(); RigheEnigmi(); }
            float y = 0f;
            foreach (var r in righe) { CostruisciRiga(r, y); y += RowH + 4f; }
            content.sizeDelta = new Vector2(-24f, y + 8f);
            content.anchoredPosition = Vector2.zero;
            AggiornaValori();
            AggiornaSchede();
            if (ripristinaBtn != null) ripristinaBtn.SetActive(gruppo != "Gioco");
        }

        void RigaPreset()
        {
            righe.Add(new Riga
            {
                nome = "Preset grafico",
                spiega = "Un livello già pronto: riscrive tutte le voci. Se ne cambi una, diventa «Personalizzata». Alta e Ultra non ci sono su WebGL.",
                testo = () => GraphicsPresets.DisplayName,
                passo = dir =>
                {
                    int cur = Mathf.Max(0, GraphicsPresets.Current);
                    for (int s = 1; s <= GraphicsPresets.Names.Length; s++)
                    {
                        int p = ((cur + dir * s) % 4 + 4) % 4;
                        if (System.Array.IndexOf(QualitySettings.names, GraphicsPresets.Names[p]) < 0) continue;
                        // da "Personalizzata" il primo passo riporta al preset di base
                        GraphicsPresets.Apply(GraphicsPresets.Custom && s == 1 ? cur : p);
                        return;
                    }
                }
            });
        }

        /// <summary>R009: "Enigmi semplificati" (pannelli 2D, utile alla LIM e per l'accessibilita') e "Evidenzia oggetti interattivi" (brillio leggero, spento di default).</summary>
        void RigheEnigmi()
        {
            righe.Insert(1, new Riga
            {
                nome = GameData.UiMenu.enigmiImp.semplificatiNome,
                spiega = GameData.UiMenu.enigmiImp.semplificatiSpiega,
                testo = () => TrenoBambini.Core.GameManager.I != null && TrenoBambini.Core.GameManager.I.Save.enigmiSemplificati ? GameData.UiMenu.enigmiImp.si : GameData.UiMenu.enigmiImp.no,
                passo = dir => { var g = TrenoBambini.Core.GameManager.I; if (g != null) { g.Save.enigmiSemplificati = !g.Save.enigmiSemplificati; g.SaveGame(); } }
            });
            righe.Insert(2, new Riga
            {
                nome = GameData.UiMenu.enigmiImp.evidenziaNome,
                spiega = GameData.UiMenu.enigmiImp.evidenziaSpiega,
                testo = () => TrenoBambini.Core.GameManager.I != null && TrenoBambini.Core.GameManager.I.Save.evidenziaInterattivi ? GameData.UiMenu.enigmiImp.si : GameData.UiMenu.enigmiImp.no,
                passo = dir => { var g = TrenoBambini.Core.GameManager.I; if (g != null) { g.Save.evidenziaInterattivi = !g.Save.evidenziaInterattivi; g.SaveGame(); } }
            });
            righe.Add(new Riga // R028: lettura automatica delle battute del Narratore (sì di default)
            {
                nome = GameData.UiMenu.voce.autoNome,
                spiega = GameData.UiMenu.voce.autoSpiega,
                testo = () => TrenoBambini.Core.GameManager.I != null && TrenoBambini.Core.GameManager.I.Save.narratoreManuale ? GameData.UiMenu.enigmiImp.no : GameData.UiMenu.enigmiImp.si,
                passo = dir => { var g = TrenoBambini.Core.GameManager.I; if (g != null) { g.Save.narratoreManuale = !g.Save.narratoreManuale; g.SaveGame(); } }
            });
            righe.Insert(3, new Riga // R026: biglietto "Cosa fare / Cosa ottieni" all'apertura degli enigmi (acceso di default)
            {
                nome = GameData.UiMenu.enigmiImp.spiegazioneNome,
                spiega = GameData.UiMenu.enigmiImp.spiegazioneSpiega,
                testo = () => TrenoBambini.Core.GameManager.I != null && TrenoBambini.Core.GameManager.I.Save.nascondiSpiegazione ? GameData.UiMenu.enigmiImp.no : GameData.UiMenu.enigmiImp.si,
                passo = dir => { var g = TrenoBambini.Core.GameManager.I; if (g != null) { g.Save.nascondiSpiegazione = !g.Save.nascondiSpiegazione; g.SaveGame(); } }
            });
        }

        /// <summary>R019: aiuto della mappa (Guidata / Normale / Esploratore), minimappa accesa o spenta, nord fisso. Testi in ui_menu.json (mappaImp).</summary>
        void RigheMappa()
        {
            var T = GameData.UiMenu.mappaImp;
            Func<SaveData> sd = () => TrenoBambini.Core.GameManager.I != null ? TrenoBambini.Core.GameManager.I.Save : null;
            Action salva = () => { if (TrenoBambini.Core.GameManager.I != null) TrenoBambini.Core.GameManager.I.SaveGame(); };
            righe.Add(new Riga
            {
                nome = T.aiutoNome, spiega = T.aiutoSpiega,
                testo = () => { var s = sd(); int a = s != null ? s.aiutoMappa : 1; return a == 0 ? T.guidata : (a == 2 ? T.esploratore : T.normale); },
                passo = dir => { var s = sd(); if (s == null) return; s.aiutoMappa = ((s.aiutoMappa + dir) % 3 + 3) % 3; salva(); }
            });
            righe.Add(new Riga
            {
                nome = T.minimappaNome, spiega = T.minimappaSpiega,
                testo = () => { var s = sd(); return s != null && s.minimappaNascosta ? T.no : T.si; },
                passo = dir => { var s = sd(); if (s == null) return; s.minimappaNascosta = !s.minimappaNascosta; salva(); }
            });
            righe.Add(new Riga
            {
                nome = T.nordFissoNome, spiega = T.nordFissoSpiega,
                testo = () => { var s = sd(); return s != null && s.nordFisso ? T.si : T.no; },
                passo = dir => { var s = sd(); if (s == null) return; s.nordFisso = !s.nordFisso; salva(); }
            });
        }

        void RigaEffettiRidotti()
        {
            righe.Insert(0, new Riga
            {
                nome = "Effetti ridotti dell'interfaccia",
                spiega = "Meno tremolii, particelle e animazioni nei menu e nelle scene. Utile su PC lenti o per chi si affatica.",
                testo = () => UITheme.ReduceEffects ? "Sì" : "No",
                passo = dir => { if (UIRoot.I != null) UIRoot.I.SetReduceEffects(!UITheme.ReduceEffects); }
            });
        }

        void CostruisciRiga(Riga r, float y)
        {
            var row = UIFactory.NewRect("Riga", content);
            row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f); row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -y); row.sizeDelta = new Vector2(0f, RowH);
            var bg = UIFactory.MakePanel(row, "Fondo", new Color(0.55f, 0.4f, 0.2f, 0.07f), false);
            UIFactory.Stretch(bg.rectTransform);
            var rule = UIFactory.MakePanel(row, "Filetto", new Color(UITheme.Ottone.r, UITheme.Ottone.g, UITheme.Ottone.b, 0.35f), false);
            rule.sprite = UIArt.White();
            rule.rectTransform.anchorMin = new Vector2(0f, 0f); rule.rectTransform.anchorMax = new Vector2(1f, 0f);
            rule.rectTransform.pivot = new Vector2(0.5f, 0f); rule.rectTransform.sizeDelta = new Vector2(-16f, 1.5f);

            var nome = UIFactory.MakeText(row, r.nome, 26, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            nome.rectTransform.anchorMin = new Vector2(0f, 1f); nome.rectTransform.anchorMax = new Vector2(0f, 1f); nome.rectTransform.pivot = new Vector2(0f, 1f);
            nome.rectTransform.anchoredPosition = new Vector2(14f, -6f); nome.rectTransform.sizeDelta = new Vector2(560f, 32f);
            var sp = UIFactory.MakeText(row, r.spiega, 18, UITheme.Inchiostro, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            sp.rectTransform.anchorMin = new Vector2(0f, 1f); sp.rectTransform.anchorMax = new Vector2(0f, 1f); sp.rectTransform.pivot = new Vector2(0f, 1f);
            sp.rectTransform.anchoredPosition = new Vector2(14f, -36f); sp.rectTransform.sizeDelta = new Vector2(545f, 40f);
            sp.enableAutoSizing = true; sp.fontSizeMin = 13f; sp.fontSizeMax = 18f;

            // controllo: ‹ valore ›
            var ctl = UIFactory.NewRect("Controllo", row);
            ctl.anchorMin = ctl.anchorMax = new Vector2(1f, 0.5f); ctl.pivot = new Vector2(1f, 0.5f);
            ctl.anchoredPosition = new Vector2(-12f, 0f); ctl.sizeDelta = new Vector2(330f, 60f);
            var left = UIFactory.MakeButton(ctl, "‹", () => { r.passo(-1); AggiornaValori(); }, new Vector2(60f, 56f), ButtonStyle.Brass, 34f);
            UIFactory.Place((RectTransform)left.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(60f, 56f));
            var right = UIFactory.MakeButton(ctl, "›", () => { r.passo(1); AggiornaValori(); }, new Vector2(60f, 56f), ButtonStyle.Brass, 34f);
            UIFactory.Place((RectTransform)right.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(60f, 56f));
            r.valore = UIFactory.MakeText(ctl, "", 24, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(r.valore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(196f, 56f));
            r.valore.enableAutoSizing = true; r.valore.fontSizeMin = 14f; r.valore.fontSizeMax = 24f;
        }

        void AggiornaValori()
        {
            foreach (var r in righe) if (r.valore != null) r.valore.text = r.testo();
        }

        void SelezionaScheda(string g)
        {
            gruppo = g;
            RiempiScheda();
        }

        void AggiornaSchede()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                bool sel = GraphicsOptions.Gruppi[i] == gruppo;
                UIFactory.SetBg(tabs[i], sel ? Color.white : new Color(0.72f, 0.66f, 0.56f, 1f));
            }
        }

        // ---------------------------------------------------------------- azioni

        void OnRipristina()
        {
            if (UIRoot.I == null) { GraphicsPresets.RipristinaPredefiniti(); RiempiScheda(); return; }
            UIRoot.I.ShowConfirm("Ripristina predefiniti", "Tutte le impostazioni grafiche tornano ai valori consigliati per questo computer (preset " +
                GraphicsPresets.NameOf(GraphicsPresets.DefaultLevel()) + "). Vuoi continuare?", "Sì, ripristina", "Annulla", () =>
                {
                    GraphicsPresets.RipristinaPredefiniti();
                    RiempiScheda();
                    UIRoot.I.Toast("Impostazioni ripristinate.");
                });
        }

        public void Show()
        {
            if (IsOpen) return;
            if (!built) Build();
            IsOpen = true;
            transform.SetAsLastSibling();
            RiempiScheda();
            fx.Show();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            fx.Hide();
            if (UIRoot.I != null) UIRoot.I.SalvaSeInCorso();
        }

        /// <summary>Per le prove: apre la scheda indicata.</summary>
        public void ShowScheda(string g)
        {
            Show();
            SelezionaScheda(g);
        }
    }
}
