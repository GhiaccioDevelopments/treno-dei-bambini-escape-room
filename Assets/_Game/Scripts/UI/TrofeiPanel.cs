using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R027: l'album dei trofei, aperto dal menu principale e dalla Pausa. Scelta di stile (coerente con R023, la latta e il quaderno): un **album di figurine**
    /// su carta, una figurina per trofeo. Sbloccata = figurina colorata con il titolo e la descrizione; bloccata = sagoma grigia con l'indizio; segreta e
    /// bloccata = sagoma con il testo "segreto" e nient'altro. Testi in trofei.json. Esc, Indietro o il tasto Est del gamepad chiudono.
    /// </summary>
    public class TrofeiPanel : MonoBehaviour
    {
        public static TrofeiPanel Aperto { get; private set; }
        System.Action alChiudere;
        float t0;

        public static void Mostra(System.Action alChiudere = null)
        {
            if (UIRoot.I == null) return;
            if (Aperto != null) return;
            var root = UIFactory.NewRect("Trofei", UIRoot.I.ModalLayer);
            UIFactory.Stretch(root);
            var p = root.gameObject.AddComponent<TrofeiPanel>();
            p.alChiudere = alChiudere;
            p.Costruisci(root);
        }

        void Costruisci(RectTransform root)
        {
            Aperto = this;
            t0 = Time.unscaledTime;
            UIRoot.I.PushModal();
            if (GameManager.I != null) TrenoBambini.Album.Trofei.Controlla(false);
            var T = GameData.Trofei.testi;
            var velo = UIFactory.MakePanel(root, "Velo", new Color(0.06f, 0.04f, 0.02f, 0.82f), true);
            UIFactory.Stretch(velo.rectTransform);
            var album = UIFactory.MakeImage(root, "Album", UIArt.Notebook(), Color.white);
            album.type = Image.Type.Sliced;
            UIFactory.Place(album.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f));
            var ar = album.rectTransform;
            var titolo = UIFactory.MakeText(ar, T.titolo, 46, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(titolo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 56f));
            var lista = GameData.Trofei.trofei;
            int fatti = 0; foreach (var t in lista) if (TrenoBambini.Album.Trofei.Sbloccato(t.id)) fatti++;
            var sotto = UIFactory.MakeText(ar, T.sottotitolo + "   " + string.Format(T.contatore, fatti, lista.Count), 20, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Place(sotto.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(1000f, 30f));

            // la griglia delle figurine, in un'area che scorre
            var vista = UIFactory.NewRect("Vista", ar);
            vista.anchorMin = Vector2.zero; vista.anchorMax = Vector2.one; vista.offsetMin = new Vector2(40f, 96f); vista.offsetMax = new Vector2(-40f, -124f);
            vista.gameObject.AddComponent<RectMask2D>();
            var contenuto = UIFactory.NewRect("Figurine", vista);
            contenuto.anchorMin = new Vector2(0f, 1f); contenuto.anchorMax = new Vector2(1f, 1f); contenuto.pivot = new Vector2(0.5f, 1f);
            var g = contenuto.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(352f, 150f); g.spacing = new Vector2(14f, 14f); g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
            contenuto.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = vista.gameObject.AddComponent<ScrollRect>();
            sr.content = contenuto; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f;
            foreach (var t in lista) Figurina(contenuto, t, TrenoBambini.Album.Trofei.Sbloccato(t.id), T);

            var b = UIFactory.MakeButton(ar, T.indietro, Chiudi, new Vector2(240f, 60f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(240f, 60f));
            UISfx.Play(Sfx.ClickPaper, 0.5f);
        }

        static void Figurina(RectTransform parent, TrofeoDef t, bool ok, TrofeiTesti T)
        {
            var carta = UIFactory.MakeImage(parent, "Figurina_" + t.id, UIArt.Tag(), ok ? Color.white : new Color(0.62f, 0.6f, 0.56f, 0.75f));
            carta.type = Image.Type.Sliced;
            var cr = carta.rectTransform;
            var medaglia = UIFactory.MakePanel(cr, "Medaglia", ok ? UITheme.Oro : new Color(0.3f, 0.28f, 0.25f, 0.5f), false);
            UIFactory.Place(medaglia.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(64f, 64f));
            var ic = UIFactory.MakeText(medaglia.rectTransform, ok ? (t.icona ?? "") : "?", 30, ok ? UITheme.Ceralacca : new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(ic.rectTransform);
            string nome = ok || !t.segreto ? t.titolo : T.segreto;
            string riga = ok ? t.descrizione : (t.segreto ? T.bloccato : t.indizio);
            var tn = UIFactory.MakeText(cr, nome, 22, ok ? UITheme.Inchiostro : new Color(0.2f, 0.18f, 0.15f, 0.8f), TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Place(tn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(92f, -12f), new Vector2(248f, 30f));
            tn.enableAutoSizing = true; tn.fontSizeMin = 14; tn.fontSizeMax = 22;
            var td = UIFactory.MakeText(cr, riga ?? "", 16, new Color(0.2f, 0.18f, 0.15f, ok ? 0.95f : 0.7f), TextAlignmentOptions.TopLeft, ok ? FontStyles.Normal : FontStyles.Italic);
            UIFactory.Place(td.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(92f, -44f), new Vector2(248f, 96f));
            td.enableAutoSizing = true; td.fontSizeMin = 11; td.fontSizeMax = 16;
            td.textWrappingMode = TextWrappingModes.Normal;
        }

        void Update()
        {
            if (Time.unscaledTime - t0 < 0.3f) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame)) Chiudi();
        }

        public void Chiudi()
        {
            if (Aperto != this) return;
            Aperto = null;
            if (UIRoot.I != null) UIRoot.I.PopModal();
            var cb = alChiudere;
            Destroy(gameObject);
            if (cb != null) cb();
        }
    }
}
