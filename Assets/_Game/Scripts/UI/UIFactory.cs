using System.IO;
using TMPro;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Helper statici per costruire la UI da codice con stile coerente (carta seppia / inchiostro).</summary>
    public static class UIFactory
    {
        public static readonly Color Carta = new Color32(0xE8, 0xD9, 0xB5, 0xFF);
        public static readonly Color CartaScura = new Color32(0xD2, 0xBE, 0x93, 0xFF);
        public static readonly Color Inchiostro = new Color32(0x3B, 0x2A, 0x1A, 0xFF);
        public static readonly Color Accento = new Color32(0x8C, 0x3B, 0x2A, 0xFF);
        public static readonly Color Verde = new Color32(0x3F, 0x6E, 0x3A, 0xFF);
        public static readonly Color Marrone = new Color32(0x5A, 0x40, 0x2A, 0xFF);
        public static readonly Color Velo = new Color(0.1f, 0.07f, 0.04f, 0.75f);

        static Sprite circle;
        static bool fontWarned;

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static Image MakePanel(Transform parent, string name, Color color, bool raycast = true)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Immagine con sprite; se lo sprite manca usa il colore pieno di riserva.</summary>
        public static Image MakeImage(Transform parent, string name, Sprite sprite, Color fallback)
        {
            var img = MakePanel(parent, name, sprite != null ? Color.white : fallback, false);
            img.sprite = sprite;
            img.preserveAspect = true;
            return img;
        }

        public static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            if (!fontWarned && TMP_Settings.defaultFontAsset == null)
            {
                fontWarned = true;
                Debug.LogWarning("TMP: nessun font di default. Importa le TMP Essential Resources (Window > TextMeshPro).");
            }
            var rt = NewRect("Testo", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;
        }

        /// <summary>Compatibilità con le chiamate esistenti: il colore di sfondo sceglie il materiale del pulsante
        /// (nessuno/Accento = targa d'ottone, Marrone = cartellino, altro = cartoncino tinto).</summary>
        public static Button MakeButton(Transform parent, string label, UnityAction onClick, Vector2 size,
            Color? bg = null, Color? fg = null, float fontSize = 26f)
        {
            ButtonStyle style = ButtonStyle.Card;
            Color tint = bg ?? Accento;
            if (!bg.HasValue || Near(tint, Accento)) style = ButtonStyle.Brass;
            else if (Near(tint, Marrone)) style = ButtonStyle.Tag;
            Color text = fg ?? Carta;
            if ((style == ButtonStyle.Brass || style == ButtonStyle.Tag) && (!fg.HasValue || Near(fg.Value, Carta))) text = Inchiostro;
            return Build(parent, label, onClick, size, style, style == ButtonStyle.Card ? tint : Color.white, text, fontSize, false);
        }

        /// <summary>Pulsante con materiale esplicito. recommended = "respira" lentamente (pulsante consigliato).</summary>
        public static Button MakeButton(Transform parent, string label, UnityAction onClick, Vector2 size,
            ButtonStyle style, float fontSize = 26f, bool recommended = false)
        {
            Color text = style == ButtonStyle.Seal ? Carta : Inchiostro;
            return Build(parent, label, onClick, size, style, Color.white, text, fontSize, recommended);
        }

        static Button Build(Transform parent, string label, UnityAction onClick, Vector2 size, ButtonStyle style,
            Color tint, Color textColor, float fontSize, bool recommended)
        {
            size.y = Mathf.Max(size.y, 56f); // minimo adatto a touch/LIM
            var img = MakePanel(parent, "Pulsante", tint, true);
            switch (style)
            {
                case ButtonStyle.Brass: img.sprite = UIArt.Brass(); img.type = Image.Type.Sliced; break;
                case ButtonStyle.Seal: img.sprite = UIArt.Seal(); img.type = Image.Type.Sliced; break;
                case ButtonStyle.Tag:
                case ButtonStyle.Card: img.sprite = UIArt.Tag(); img.type = Image.Type.Sliced; break;
                default: img.color = new Color(1f, 1f, 1f, 0.001f); break; // Ink: nessuna base, resta cliccabile
            }
            img.rectTransform.sizeDelta = size;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.04f, 1f, 1f);
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.82f, 0.8f, 0.78f, 1f);
            cb.disabledColor = new Color(0.62f, 0.6f, 0.56f, 0.7f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            var fx = img.gameObject.AddComponent<ButtonFx>();
            fx.style = style;
            fx.recommended = recommended;

            var t = MakeText(img.rectTransform, label, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(t.rectTransform, 14f, 6f, 14f, 6f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 14f;
            t.fontSizeMax = fontSize;

            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static void SetLabel(Button b, string s)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = s;
        }

        public static void SetBg(Button b, Color c)
        {
            if (b.targetGraphic != null) b.targetGraphic.color = c;
        }

        /// <summary>Slider "a leva d'ottone": scanalatura scura, riempimento d'ottone, maniglia a targa.</summary>
        public static Slider MakeSlider(Transform parent, float value, UnityAction<float> onChange, Vector2 size)
        {
            var root = NewRect("Slider", parent);
            root.sizeDelta = size;

            var bgImg = MakePanel(root, "Fondo", new Color(0.16f, 0.1f, 0.06f, 0.75f), true);
            bgImg.sprite = UIArt.PaperDark();
            bgImg.type = Image.Type.Sliced;
            bgImg.rectTransform.anchorMin = new Vector2(0f, 0.32f);
            bgImg.rectTransform.anchorMax = new Vector2(1f, 0.68f);
            bgImg.rectTransform.offsetMin = Vector2.zero;
            bgImg.rectTransform.offsetMax = Vector2.zero;

            var fillArea = NewRect("AreaRiempimento", root);
            fillArea.anchorMin = new Vector2(0f, 0.32f);
            fillArea.anchorMax = new Vector2(1f, 0.68f);
            fillArea.offsetMin = new Vector2(10f, 0f);
            fillArea.offsetMax = new Vector2(-10f, 0f);
            var fill = MakePanel(fillArea, "Riempimento", UITheme.Ottone, false);
            fill.sprite = UIArt.White();
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            var slideArea = NewRect("AreaCursore", root);
            Stretch(slideArea, 18f, 0f, 18f, 0f);
            var handle = MakePanel(slideArea, "Cursore", Color.white, true);
            handle.sprite = UIArt.Brass();
            handle.type = Image.Type.Sliced;
            handle.rectTransform.anchorMin = new Vector2(0f, 0.04f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.96f);
            handle.rectTransform.sizeDelta = new Vector2(34f, 0f);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            s.onValueChanged.AddListener(SliderTick);
            return s;
        }

        static float lastTick;
        static void SliderTick(float v)
        {
            if (Time.unscaledTime - lastTick < 0.07f) return;
            lastTick = Time.unscaledTime;
            UISfx.Play(Sfx.Tick, 0.4f);
        }

        /// <summary>Pannello di carta invecchiata (9-slice). tint = Color.white per il colore naturale.</summary>
        public static Image MakePaper(Transform parent, string name, Color tint, bool raycast = true)
        {
            var img = MakePanel(parent, name, tint, raycast);
            img.sprite = UIArt.Paper();
            img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>Cornice d'ottone (centro trasparente) intorno a un pannello; sporge di "margin" px.</summary>
        public static Image MakeBrassFrame(RectTransform around, float margin = 10f)
        {
            var img = MakePanel(around, "CorniceOttone", Color.white, false);
            img.sprite = UIArt.BrassFrame();
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            Stretch(img.rectTransform, -margin, -margin, -margin, -margin);
            return img;
        }

        /// <summary>Cerca Resources/Sprites/&lt;cartella&gt;/&lt;nome&gt; (estensione ignorata). Null se manca.</summary>
        public static Sprite LoadSprite(string folder, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string n = Path.GetFileNameWithoutExtension(name);
            return Resources.Load<Sprite>("Sprites/" + folder + "/" + n);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Object.Destroy(c);
            }
        }

        public static VerticalLayoutGroup AddVLayout(GameObject go, float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = false;
            v.childControlHeight = false;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            return v;
        }

        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((r - d) * 255f / 1.5f), 0, 255);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }
    }
}
