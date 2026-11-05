using TMPro;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Schermata del titolo (scena 01_Titolo): Nuova partita, Continua, Crediti.
    /// Il titolo si "stampa" lettera per lettera come in tipografia, poi una passata di luce dorata; vapore del treno e polvere nella luce;
    /// i pulsanti entrano a cascata e "Continua" (se c'è un salvataggio) respira.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        GameObject root;

        void Start()
        {
            if (UIRoot.I == null) { Debug.LogWarning("UIRoot non disponibile."); return; }
            Build();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
        }

        void Build()
        {
            var rootRt = UIFactory.NewRect("Titolo", UIRoot.I.ScreenLayer);
            UIFactory.Stretch(rootRt);
            root = rootRt.gameObject;

            BuildAtmosphere(rootRt);

            var plate = UIFactory.MakePaper(rootRt, "Targa", new Color(1f, 1f, 1f, 0.95f), false);
            UIFactory.Place(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(820f, 650f));
            UIFactory.MakeBrassFrame(plate.rectTransform, 8f);

            // titolo stampato
            var titleBox = UIFactory.NewRect("TitoloStampa", plate.rectTransform);
            titleBox.anchorMin = titleBox.anchorMax = new Vector2(0.5f, 1f);
            titleBox.pivot = new Vector2(0.5f, 1f);
            titleBox.anchoredPosition = new Vector2(0f, -36f);
            titleBox.sizeDelta = new Vector2(740f, 230f);
            titleBox.gameObject.AddComponent<RectMask2D>();
            float t0 = 0.35f;
            float endTime = t0;
            endTime = Max(endTime, StampLine(titleBox, "Il treno", 0f, 96f, UITheme.Inchiostro, t0));
            endTime = Max(endTime, StampLine(titleBox, "dei bambini", -112f, 96f, UITheme.Ceralacca, t0 + 0.55f));

            // passata di luce dorata sul titolo
            var shine = UIFactory.MakePanel(titleBox, "Luce", new Color(1f, 0.9f, 0.55f, 0.55f), false);
            shine.sprite = UIArt.White();
            var sr = shine.rectTransform;
            sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(0f, 1f);
            sr.pivot = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(70f, 40f);
            sr.localRotation = Quaternion.Euler(0f, 0f, 16f);
            shine.gameObject.SetActive(false);
            UITween.After(titleBox, endTime + 0.2f, () =>
            {
                if (shine == null || UITheme.ReduceEffects) return;
                shine.gameObject.SetActive(true);
                UISfx.Play(Sfx.Sparkle, 0.5f);
                UITween.Run(sr, 0.9f, Ease.InOutCubic, v => { if (sr != null) sr.anchoredPosition = new Vector2(Mathf.Lerp(-80f, 820f, v), 0f); },
                    () => { if (shine != null) shine.gameObject.SetActive(false); });
            });

            var sub = UIFactory.MakeText(plate.rectTransform, "Napoli, 1946", 34, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -276f), new Vector2(640f, 48f));
            var subCg = sub.gameObject.AddComponent<CanvasGroup>();
            subCg.alpha = 0f;
            UITween.Alpha(subCg, 1f, 0.6f, Ease.OutCubic, null, endTime);

            // pulsanti a cascata
            bool hasSave = GameManager.I != null && GameManager.I.HasSave;
            var column = UIFactory.NewRect("Pulsanti", plate.rectTransform);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0f);
            column.pivot = new Vector2(0.5f, 0f);
            column.anchoredPosition = new Vector2(0f, 34f);
            column.sizeDelta = new Vector2(420f, 270f);
            var vl = UIFactory.AddVLayout(column.gameObject, 12f, TextAnchor.LowerCenter);

            var bNew = UIFactory.MakeButton(column, "Nuova partita", OnNew, new Vector2(400f, 72f), ButtonStyle.Brass, 30f, !hasSave);
            Cascade(bNew, endTime + 0.1f);
            if (hasSave)
            {
                var bCont = UIFactory.MakeButton(column, "Continua", OnContinue, new Vector2(400f, 72f), ButtonStyle.Seal, 30f, true);
                Cascade(bCont, endTime + 0.2f);
            }
            var bCred = UIFactory.MakeButton(column, "Crediti", OnCredits, new Vector2(400f, 72f), ButtonStyle.Tag, 30f);
            Cascade(bCred, endTime + 0.3f);
        }

        static float Max(float a, float b) { return a > b ? a : b; }

        static void Cascade(Button b, float delay)
        {
            var rt = (RectTransform)b.transform;
            rt.localScale = Vector3.zero;
            UITween.Scale(rt, Vector3.one, 0.4f, Ease.OutBack, null, delay);
            UITween.After(b, delay, () => UISfx.Play(Sfx.ClickPaper, 0.3f));
        }

        /// <summary>Una riga di titolo: ogni lettera cade, si imprime con un leggero schiacciamento. Ritorna il tempo di fine.</summary>
        float StampLine(RectTransform parent, string text, float y, float size, Color color, float start)
        {
            var row = UIFactory.NewRect("Riga", parent);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, y);
            row.sizeDelta = new Vector2(740f, size * 1.15f);
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

                if (ch != " ")
                {
                    var cg = letter.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    var lrt = letter.rectTransform;
                    float delay = t;
                    bool reduced = UITheme.ReduceEffects;
                    UITween.Run(lrt, reduced ? 0.2f : 0.5f, Ease.Linear, v =>
                    {
                        if (lrt == null) return;
                        if (reduced) { cg.alpha = v; return; }
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
                    }, () => { if (lrt != null) { lrt.localScale = Vector3.one; lrt.anchoredPosition = Vector2.zero; } }, delay);
                    UITween.After(lrt, delay + (reduced ? 0.1f : 0.35f), () => UISfx.Play(Sfx.TypeKey, 0.55f, 0.1f));
                    t += 0.065f;
                }
                else t += 0.12f;
            }
            return t + 0.5f;
        }

        /// <summary>Vapore del treno che sale lento, polvere nella luce, grana di pellicola leggera.</summary>
        void BuildAtmosphere(RectTransform parent)
        {
            var atm = UIFactory.NewRect("Atmosfera", parent);
            UIFactory.Stretch(atm);
            var cv = atm.gameObject.AddComponent<Canvas>(); // isolata: si anima di continuo
            cv.overrideSorting = false;

            if (!UITheme.ReduceEffects)
            {
                for (int i = 0; i < 5; i++)
                {
                    var puff = UIFactory.MakePanel(atm, "Vapore", new Color(0.95f, 0.9f, 0.8f, 0.13f), false);
                    puff.sprite = UIArt.Smoke();
                    var r = puff.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
                    float sz = Random.Range(520f, 760f);
                    r.sizeDelta = new Vector2(sz, sz);
                    float x = Mathf.Lerp(-520f, 520f, i / 4f) + Random.Range(-60f, 60f);
                    float dur = Random.Range(11f, 16f);
                    float offset = Random.value;
                    UITween.Run(r, dur, Ease.Linear, v =>
                    {
                        if (r == null) return;
                        float k = (v + offset) % 1f;
                        r.anchoredPosition = new Vector2(x + Mathf.Sin(k * 6f + offset * 9f) * 40f, Mathf.Lerp(-200f, 760f, k));
                        var c = puff.color; c.a = 0.13f * Mathf.Sin(k * Mathf.PI); puff.color = c;
                        float s = Mathf.Lerp(0.8f, 1.35f, k); r.localScale = new Vector3(s, s, 1f);
                    }, null, 0f, true);
                }
                for (int i = 0; i < 14; i++)
                {
                    var mote = UIFactory.MakePanel(atm, "Polvere", new Color(1f, 0.93f, 0.7f, 0.35f), false);
                    mote.sprite = UIArt.Glow();
                    var r = mote.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    float sz = Random.Range(6f, 16f);
                    r.sizeDelta = new Vector2(sz, sz);
                    Vector2 p0 = new Vector2(Random.Range(-620f, 620f), Random.Range(-340f, 340f));
                    float dur = Random.Range(14f, 24f), ph = Random.value;
                    UITween.Run(r, dur, Ease.Linear, v =>
                    {
                        if (r == null) return;
                        float k = (v + ph) % 1f;
                        r.anchoredPosition = p0 + new Vector2(Mathf.Sin(k * 6.28f) * 30f, k * 80f);
                        var c = mote.color; c.a = 0.35f * Mathf.Sin(k * Mathf.PI); mote.color = c;
                    }, null, 0f, true);
                }
            }

            var grainGo = UIFactory.NewRect("Grana", atm);
            UIFactory.Stretch(grainGo);
            var raw = grainGo.gameObject.AddComponent<RawImage>();
            raw.texture = UIArt.Grain();
            raw.color = new Color(1f, 0.95f, 0.85f, UITheme.ReduceEffects ? 0f : 0.1f);
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 10f, 5.625f);
            grainGo.gameObject.AddComponent<GrainJitter>().target = raw;
        }

        void OnNew()
        {
            if (GameManager.I == null) return;
            if (GameManager.I.HasSave)
                UIRoot.I.ShowConfirm("Nuova partita", "Esiste già un salvataggio. Se ricominci, verrà cancellato. Vuoi continuare?",
                    "Sì, ricomincia", "Annulla", () => GameManager.I.NewGame());
            else
                GameManager.I.NewGame();
        }

        void OnContinue()
        {
            if (GameManager.I != null) GameManager.I.Continue();
        }

        void OnCredits()
        {
            UIRoot.I.ShowMessage("Crediti",
                "Il treno dei bambini - Escape room\n\nGioco educativo di Educazione Civica ispirato al romanzo di Viola Ardone (2019) e al film di Cristina Comencini (2024).\n\nPersonaggi, dialoghi e grafica originali.\n\nGhiaccio Developments",
                null);
        }
    }

    /// <summary>Sposta a caso l'uvRect della grana ~12 volte al secondo (effetto pellicola).</summary>
    public class GrainJitter : MonoBehaviour
    {
        public RawImage target;
        float next;

        void Update()
        {
            if (target == null || Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.08f;
            target.uvRect = new Rect(Random.value, Random.value, 10f, 5.625f);
        }
    }
}
