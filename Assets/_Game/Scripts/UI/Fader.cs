using System.Collections;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Transizioni a schermo intero.
    /// Fade: iride che si chiude fino al nero (cambio scena normale).
    /// Ricordo: la pellicola "brucia" dal centro verso i bordi (iride dal bordo bruciato), virata seppia, grana e graffi, suono di proiettore.
    /// Nessuno shader: solo sprite e tween (leggero per WebGL). Con "Effetti ridotti" diventa una dissolvenza breve.
    /// In più mantiene una grana leggera + vignetta seppia durante i flashback (SetFlashbackLook).
    /// </summary>
    public class Fader : MonoBehaviour
    {
        static readonly Color Seppia = new Color(0.36f, 0.25f, 0.14f, 1f);
        const float IrisOpen = 1.7f;
        const float IrisClosed = 0.015f;
        const float IrisSize = 2600f;

        Image overlay, vignette, irisImg;
        RectTransform iris;
        RawImage grain;
        Image lookVignette;
        RawImage lookGrain;
        float nextGrain;
        bool flashbackLook;
        public bool Busy { get; private set; }

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);

            // look del flashback (sotto le transizioni)
            lookVignette = UIFactory.MakePanel(transform, "LookVignetta", new Color(0.3f, 0.19f, 0.09f, 0.55f), false);
            lookVignette.sprite = UIArt.Vignette(Color.white);
            UIFactory.Stretch(lookVignette.rectTransform);
            lookGrain = MakeGrain("LookGrana", 0.1f);
            lookVignette.gameObject.SetActive(false);
            lookGrain.gameObject.SetActive(false);

            // iride con 4 pannelli pieni attorno (restano coprenti anche quando l'iride si rimpicciolisce)
            iris = UIFactory.NewRect("Iride", transform);
            iris.anchorMin = iris.anchorMax = new Vector2(0.5f, 0.5f);
            iris.sizeDelta = new Vector2(IrisSize, IrisSize);
            irisImg = iris.gameObject.AddComponent<Image>();
            irisImg.raycastTarget = false;
            const float big = IrisSize * 300f;
            for (int k = 0; k < 4; k++)
            {
                var p = UIFactory.MakePanel(iris, "Bordo" + k, new Color(0.02f, 0.015f, 0.01f, 1f), false);
                p.sprite = UIArt.White();
                var r = p.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                float half = IrisSize * 0.5f - 1f;
                switch (k)
                {
                    case 0: r.pivot = new Vector2(0.5f, 0f); r.anchoredPosition = new Vector2(0f, half); r.sizeDelta = new Vector2(big, big); break;
                    case 1: r.pivot = new Vector2(0.5f, 1f); r.anchoredPosition = new Vector2(0f, -half); r.sizeDelta = new Vector2(big, big); break;
                    case 2: r.pivot = new Vector2(1f, 0.5f); r.anchoredPosition = new Vector2(-half, 0f); r.sizeDelta = new Vector2(big, IrisSize); break;
                    default: r.pivot = new Vector2(0f, 0.5f); r.anchoredPosition = new Vector2(half, 0f); r.sizeDelta = new Vector2(big, IrisSize); break;
                }
            }
            iris.gameObject.SetActive(false);

            overlay = UIFactory.MakePanel(transform, "Overlay", new Color(0f, 0f, 0f, 0f), false);
            UIFactory.Stretch(overlay.rectTransform);

            vignette = UIFactory.MakePanel(transform, "Vignetta", new Color(1f, 1f, 1f, 0f), false);
            UIFactory.Stretch(vignette.rectTransform);
            vignette.sprite = UIArt.Vignette(new Color(0.2f, 0.1f, 0.03f));
            vignette.enabled = true;

            grain = MakeGrain("Grana", 0f);
        }

        RawImage MakeGrain(string name, float alpha)
        {
            var go = UIFactory.NewRect(name, transform);
            UIFactory.Stretch(go);
            var raw = go.gameObject.AddComponent<RawImage>();
            raw.texture = UIArt.Grain();
            raw.color = new Color(1f, 0.95f, 0.85f, alpha);
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 10f, 5.625f);
            return raw;
        }

        /// <summary>Grana leggera e vignetta seppia sopra l'HUD mentre si è dentro un flashback.</summary>
        public void SetFlashbackLook(bool on)
        {
            flashbackLook = on;
            lookVignette.gameObject.SetActive(on);
            lookGrain.gameObject.SetActive(on && !UITheme.ReduceEffects);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            if (t < nextGrain) return;
            nextGrain = t + 0.07f; // ~14 fps: effetto pellicola, costo trascurabile
            var r = new Rect(Random.value, Random.value, 10f, 5.625f);
            if (grain.color.a > 0.001f) grain.uvRect = r;
            if (flashbackLook && !UITheme.ReduceEffects)
            {
                if (!lookGrain.gameObject.activeSelf) lookGrain.gameObject.SetActive(true);
                lookGrain.uvRect = r;
            }
        }

        public IEnumerator Out(TransitionStyle stile) { return Run(stile, 0f, 1f); }
        public IEnumerator In(TransitionStyle stile) { return Run(stile, 1f, 0f); }

        IEnumerator Run(TransitionStyle stile, float from, float to)
        {
            Busy = true;
            bool ricordo = stile == TransitionStyle.Ricordo;
            bool reduced = UITheme.ReduceEffects;
            float dur = reduced ? 0.25f : (ricordo ? 1.2f : 0.5f);
            overlay.raycastTarget = true;
            if (ricordo && !reduced && from < to) UISfx.Play(Sfx.Projector, 0.7f, 0f);

            irisImg.sprite = UIArt.Iris(ricordo);
            irisImg.color = Color.white;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Lerp(from, to, UITween.Evaluate(Ease.InOutCubic, t / dur));
                Apply(k, ricordo, reduced);
                yield return null;
            }
            Apply(to, ricordo, reduced);
            overlay.raycastTarget = to > 0.01f;
            Busy = false;
        }

        // c: 0 = scena visibile, 1 = schermo coperto
        void Apply(float c, bool ricordo, bool reduced)
        {
            Color baseCol = ricordo ? Seppia : Color.black;
            if (reduced)
            {
                baseCol.a = c;
                overlay.color = baseCol;
                iris.gameObject.SetActive(false);
                vignette.color = new Color(1f, 1f, 1f, 0f);
                grain.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            bool irisOn = c > 0.001f && c < 0.999f;
            if (iris.gameObject.activeSelf != irisOn) iris.gameObject.SetActive(irisOn);
            if (irisOn)
            {
                float sc = Mathf.Lerp(IrisOpen, IrisClosed, c);
                iris.localScale = new Vector3(sc, sc, 1f);
            }

            float end = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, c));
            baseCol.a = ricordo ? c * 0.45f + end * 0.55f : end;
            overlay.color = baseCol;
            vignette.color = new Color(1f, 1f, 1f, ricordo ? c : 0f);
            grain.color = new Color(1f, 0.95f, 0.85f, ricordo ? 0.35f * Mathf.Sin(Mathf.Clamp01(c) * Mathf.PI) + 0.1f * c : 0f);
        }
    }
}
