using System;
using System.Collections;
using System.Text;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R016: titoli di coda a fine gioco. Fondo nero, testo che sale lento: titolo, righe del finale (ui_menu.json, blocco "finale"), squadra e asset con licenze
    /// (gli stessi dei Crediti del menu, da crediti.json). Si saltano tenendo premuto un tasto, il clic o il tocco per un secondo. Alla fine chiama "fine".
    /// Nessun testo narrativo nel codice.
    /// </summary>
    public class TitoliDiCoda : MonoBehaviour
    {
        public static bool InCorso { get; private set; }

        Action fine;
        RectTransform contenuto;
        TextMeshProUGUI aiuto;
        float tenuta;

        public static void Mostra(Action fine)
        {
            if (UIRoot.I == null) { if (fine != null) fine(); return; }
            var root = UIFactory.NewRect("TitoliDiCoda", UIRoot.I.ModalLayer);
            UIFactory.Stretch(root);
            var t = root.gameObject.AddComponent<TitoliDiCoda>();
            t.fine = fine;
            t.Costruisci(root);
        }

        void Costruisci(RectTransform root)
        {
            InCorso = true;
            UIRoot.I.PushModal();
            if (GameManager.I != null) GameManager.I.PushBlock("titoli");
            var nero = UIFactory.MakePanel(root, "Nero", Color.black, true);
            UIFactory.Stretch(nero.rectTransform);
            var m = GameData.UiMenu;
            var f = m.finale ?? new TrenoBambini.Data.UiFinale();
            contenuto = UIFactory.NewRect("Contenuto", root);
            contenuto.anchorMin = new Vector2(0.5f, 0f); contenuto.anchorMax = new Vector2(0.5f, 0f); contenuto.pivot = new Vector2(0.5f, 1f);
            contenuto.sizeDelta = new Vector2(1100f, 10f);
            contenuto.anchoredPosition = new Vector2(0f, -20f);
            var v = contenuto.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter; v.spacing = 26f; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            contenuto.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Riga(f.titolo, 64, UITheme.Oro, FontStyles.Bold);
            Riga(f.sottotitolo, 28, UITheme.CartaChiara, FontStyles.Italic);
            Riga(" ", 30, Color.white, FontStyles.Normal);
            foreach (var r in f.righe) Riga(r, 24, UITheme.CartaChiara, FontStyles.Normal);
            Riga(" ", 40, Color.white, FontStyles.Normal);
            Riga(m.crediti.intro, 22, UITheme.CartaChiara, FontStyles.Normal);
            foreach (var r in m.crediti.squadra) Riga(r, 26, UITheme.Oro, FontStyles.Bold);
            Riga(m.crediti.assetTitolo, 26, UITheme.Oro, FontStyles.Bold | FontStyles.Italic);
            var sb = new StringBuilder();
            foreach (var c in GameData.Crediti.voci) sb.Append(c.asset).Append("  -  ").Append(c.autore).Append("  (").Append(c.licenza).Append(")\n");
            Riga(sb.ToString(), 17, UITheme.CartaChiara, FontStyles.Normal);
            Riga(" ", 120, Color.white, FontStyles.Normal);
            Riga(f.fine, 54, UITheme.Oro, FontStyles.Bold);
            aiuto = UIFactory.MakeText(root, f.salta, 18, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.BottomRight, FontStyles.Italic);
            UIFactory.Place(aiuto.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 18f), new Vector2(600f, 40f));
            StartCoroutine(Scorri(f.velocita > 1f ? f.velocita : 55f));
        }

        void Riga(string s, float size, Color col, FontStyles st)
        {
            if (string.IsNullOrEmpty(s)) return;
            var t = UIFactory.MakeText(contenuto, s, size, col, TextAlignmentOptions.Top, st);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 1100f;
        }

        IEnumerator Scorri(float velocita)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contenuto);
            float h = contenuto.rect.height, schermo = ((RectTransform)transform).rect.height;
            float y = -20f, finale = h + schermo * 0.5f;
            while (y < finale)
            {
                y += velocita * Time.unscaledDeltaTime;
                contenuto.anchoredPosition = new Vector2(0f, y);
                if (Tenuto()) { tenuta += Time.unscaledDeltaTime; if (tenuta > 1f) break; } else tenuta = 0f;
                yield return null;
            }
            float t = 0f;
            while (t < 2.5f && !Tenuto()) { t += Time.unscaledDeltaTime; yield return null; }
            Chiudi();
        }

        static bool Tenuto()
        {
            var kb = Keyboard.current; var ms = Mouse.current; var ts = Touchscreen.current; var gp = Gamepad.current;
            return (kb != null && kb.anyKey.isPressed) || (ms != null && ms.leftButton.isPressed) || (ts != null && ts.primaryTouch.press.isPressed) ||
                   (gp != null && (gp.buttonSouth.isPressed || gp.startButton.isPressed));
        }

        void Chiudi()
        {
            InCorso = false;
            if (UIRoot.I != null) UIRoot.I.PopModal();
            if (GameManager.I != null) GameManager.I.PopBlock("titoli");
            var cb = fine;
            fine = null;
            Destroy(gameObject);
            if (cb != null) cb();
        }
    }
}
