using System.Collections;
using TrenoBambini.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Dissolvenza a schermo intero: nero (Fade) oppure seppia con vignetta e tempi lenti (Ricordo).</summary>
    public class Fader : MonoBehaviour
    {
        static readonly Color Seppia = new Color(0.36f, 0.25f, 0.14f, 1f);

        Image overlay, vignette;
        public bool Busy { get; private set; }

        void Awake()
        {
            UIFactory.Stretch((RectTransform)transform);
            overlay = UIFactory.MakePanel(transform, "Overlay", new Color(0f, 0f, 0f, 0f), false);
            UIFactory.Stretch(overlay.rectTransform);
            vignette = UIFactory.MakePanel(transform, "Vignetta", new Color(1f, 1f, 1f, 0f), false);
            UIFactory.Stretch(vignette.rectTransform);
            var spr = Resources.Load<Sprite>("Sprites/ui/vignetta");
            if (spr != null) vignette.sprite = spr;
            else vignette.enabled = false; // senza sprite niente vignetta
            vignette.type = Image.Type.Simple;
        }

        public IEnumerator Out(TransitionStyle stile) { return Run(stile, 0f, 1f); }
        public IEnumerator In(TransitionStyle stile) { return Run(stile, 1f, 0f); }

        IEnumerator Run(TransitionStyle stile, float from, float to)
        {
            Busy = true;
            bool ricordo = stile == TransitionStyle.Ricordo;
            float dur = ricordo ? 1.1f : 0.35f;
            Color baseCol = ricordo ? Seppia : Color.black;
            overlay.raycastTarget = true;

            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                Apply(baseCol, Mathf.Lerp(from, to, t / dur), ricordo);
                yield return null;
            }
            Apply(baseCol, to, ricordo);
            overlay.raycastTarget = to > 0.01f;
            Busy = false;
        }

        void Apply(Color baseCol, float a, bool ricordo)
        {
            baseCol.a = a;
            overlay.color = baseCol;
            if (vignette != null)
                vignette.color = new Color(1f, 1f, 1f, ricordo ? a : 0f);
        }
    }
}
