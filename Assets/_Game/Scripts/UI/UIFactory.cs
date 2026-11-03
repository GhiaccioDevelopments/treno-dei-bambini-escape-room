using System.IO;
using TMPro;
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

        public static Button MakeButton(Transform parent, string label, UnityAction onClick, Vector2 size,
            Color? bg = null, Color? fg = null, float fontSize = 26f)
        {
            size.y = Mathf.Max(size.y, 56f); // minimo adatto a touch/LIM
            var img = MakePanel(parent, "Pulsante", bg ?? Accento, true);
            img.rectTransform.sizeDelta = size;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;

            var t = MakeText(img.rectTransform, label, fontSize, fg ?? Carta, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(t.rectTransform, 10f, 4f, 10f, 4f);
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

        public static Slider MakeSlider(Transform parent, float value, UnityAction<float> onChange, Vector2 size)
        {
            var root = NewRect("Slider", parent);
            root.sizeDelta = size;

            var bgImg = MakePanel(root, "Fondo", new Color(0.23f, 0.16f, 0.1f, 0.35f), true);
            bgImg.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            bgImg.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            bgImg.rectTransform.offsetMin = Vector2.zero;
            bgImg.rectTransform.offsetMax = Vector2.zero;

            var fillArea = NewRect("AreaRiempimento", root);
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            fillArea.offsetMin = new Vector2(10f, 0f);
            fillArea.offsetMax = new Vector2(-10f, 0f);
            var fill = MakePanel(fillArea, "Riempimento", Accento, false);
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            var slideArea = NewRect("AreaCursore", root);
            Stretch(slideArea, 14f, 0f, 14f, 0f);
            var handle = MakePanel(slideArea, "Cursore", Inchiostro, true);
            handle.sprite = Circle();
            handle.rectTransform.sizeDelta = new Vector2(size.y * 0.8f, 0f);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
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
