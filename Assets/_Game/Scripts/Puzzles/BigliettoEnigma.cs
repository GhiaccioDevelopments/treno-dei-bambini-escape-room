using System;
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
    /// R026: il biglietto d'apertura di un enigma. Un foglietto di carta d'epoca con due righe: "Cosa fare" (EnigmaDef.cosaFare) e "Cosa ottieni"
    /// (EnigmaDef.cosaOttieni). Compare all'apertura della vista ravvicinata (EsaminaView) e dei pannelli 2D (PuzzlePanel) se l'opzione "Mostra la spiegazione
    /// all'apertura" e' accesa (SaveData.nascondiSpiegazione = false, default); si chiude con un clic, un tasto o da solo dopo qualche secondo e si riapre con "?".
    /// Etichette in enigmi_mondo.json (testi: bigliettoFare, bigliettoOttieni, bigliettoChiudi, btnSpiega). Nessun testo narrativo nel codice.
    /// </summary>
    public class BigliettoEnigma : MonoBehaviour
    {
        public static BigliettoEnigma Aperto { get; private set; }

        float t0;
        CanvasGroup cg;

        public static bool Attivo { get { return GameManager.I == null || !GameManager.I.Save.nascondiSpiegazione; } }

        public static bool HaTesto(EnigmaDef d) { return d != null && (!string.IsNullOrEmpty(d.cosaFare) || !string.IsNullOrEmpty(d.cosaOttieni)); }

        /// <summary>Mostra il biglietto sopra parent (la vista o il pannello). Ritorna null se l'enigma non ha testi.</summary>
        public static BigliettoEnigma Mostra(RectTransform parent, EnigmaDef d)
        {
            if (parent == null || !HaTesto(d)) return null;
            if (Aperto != null) Aperto.Chiudi();
            var T = GameData.TestiEnigmi;
            var rt = UIFactory.NewRect("BigliettoEnigma", parent);
            UIFactory.Stretch(rt);
            var b = rt.gameObject.AddComponent<BigliettoEnigma>();
            b.cg = rt.gameObject.AddComponent<CanvasGroup>();
            var velo = UIFactory.MakePanel(rt, "Velo", new Color(0.05f, 0.03f, 0.015f, 0.35f), true);
            UIFactory.Stretch(velo.rectTransform);
            var chiudi = velo.gameObject.AddComponent<Button>();
            chiudi.onClick.AddListener(b.Chiudi);
            var carta = UIFactory.MakePanel(rt, "Carta", UITheme.CartaChiara, true);
            UIFactory.Place(carta.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(760f, 300f));
            carta.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
            carta.gameObject.AddComponent<Button>().onClick.AddListener(b.Chiudi);
            float y = -26f;
            y = Riga(carta.rectTransform, T.bigliettoFare, d.cosaFare, y);
            y = Riga(carta.rectTransform, T.bigliettoOttieni, d.cosaOttieni, y - 10f);
            var nota = UIFactory.MakeText(carta.rectTransform, T.bigliettoChiudi, 16, new Color(UITheme.Inchiostro.r, UITheme.Inchiostro.g, UITheme.Inchiostro.b, 0.6f),
                TextAlignmentOptions.BottomRight, FontStyles.Italic);
            UIFactory.Place(nota.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 10f), new Vector2(500f, 26f));
            b.t0 = Time.unscaledTime;
            Aperto = b;
            UISfx.Play(Sfx.ClickPaper, 0.5f);
            return b;
        }

        static float Riga(RectTransform carta, string etichetta, string testo, float y)
        {
            if (string.IsNullOrEmpty(testo)) return y;
            var e = UIFactory.MakeText(carta, etichetta, 20, UITheme.Ceralacca, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.SmallCaps);
            UIFactory.Place(e.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(700f, 28f));
            var t = UIFactory.MakeText(carta, testo, 24, UITheme.Inchiostro, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            UIFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, y - 30f), new Vector2(700f, 90f));
            t.enableAutoSizing = true; t.fontSizeMin = 15; t.fontSizeMax = 24;
            return y - 126f;
        }

        void Update()
        {
            float dt = Time.unscaledTime - t0;
            if (cg != null) cg.alpha = Mathf.Clamp01(dt / 0.2f);
            if (dt < 0.4f) return; // il clic che ha aperto l'enigma non lo chiude subito
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame)) || dt > 12f) Chiudi();
        }

        public void Chiudi()
        {
            if (Aperto == this) Aperto = null;
            if (this != null && gameObject != null) Destroy(gameObject);
        }

        void OnDestroy() { if (Aperto == this) Aperto = null; }
    }
}
