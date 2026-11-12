using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Dialogue
{
    /// <summary>Cutscene a dialoghi: sfondo, ritratti sx/dx, testo a macchina da scrivere, voci opzionali.</summary>
    public class CutscenePlayer : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        const float CharsPerSecond = 45f;

        GameObject view;
        RectTransform scene;       // contenitore che può "scuotersi"
        RectTransform bgRect;
        Image bg, imgL, imgR, black;
        RectTransform frameL, frameR, barTop, barBottom, arrow;
        GameObject namePlate;
        float nextKey;
        RectTransform rtL, rtR;
        TextMeshProUGUI nameLbl, textLbl, hintLbl;
        float guardUntil;
        bool skip;

        // stato dei ritratti
        readonly Dictionary<string, string> sideOf = new Dictionary<string, string>();
        int sideCounter;
        bool shownL, shownR;
        Coroutine zoomRoutine;

        static readonly Vector2 RestL = new Vector2(40f, 250f);
        static readonly Vector2 RestR = new Vector2(-40f, 250f);
        const float BarH = 52f;

        void Awake() { Build(); }

        void Build()
        {
            var self = (RectTransform)transform;
            UIFactory.Stretch(self);
            view = UIFactory.NewRect("View", self).gameObject;
            var viewRt = (RectTransform)view.transform;
            UIFactory.Stretch(viewRt);

            // sfondo nero di base (copre il gioco) e contenitore scuotibile
            UIFactory.MakePanel(viewRt, "Nero", Color.black, false);
            scene = UIFactory.NewRect("Scena", viewRt);
            UIFactory.Stretch(scene);

            var mask = UIFactory.NewRect("Maschera", scene);
            UIFactory.Stretch(mask);
            mask.gameObject.AddComponent<RectMask2D>();
            bg = UIFactory.MakePanel(mask, "Sfondo", new Color(0.2f, 0.14f, 0.09f), false);
            bgRect = bg.rectTransform;
            UIFactory.Stretch(bgRect);
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            imgL = MakePortrait(scene, "RitrattoSx", new Vector2(0f, 0f), new Vector2(0f, 0f), RestL);
            imgR = MakePortrait(scene, "RitrattoDx", new Vector2(1f, 0f), new Vector2(1f, 0f), RestR);
            rtL = imgL.rectTransform; rtR = imgR.rectTransform;
            frameL = UIFactory.MakeBrassFrame(rtL, 6f).rectTransform; frameR = UIFactory.MakeBrassFrame(rtR, 6f).rectTransform;
            frameL.gameObject.SetActive(false); frameR.gameObject.SetActive(false);

            // box di testo: lettera su carta, targhetta d'ottone con il nome
            var box = UIFactory.MakePaper(scene, "Box", new Color(1f, 1f, 1f, 0.97f), false);
            var boxRt = box.rectTransform;
            boxRt.anchorMin = new Vector2(0f, 0f); boxRt.anchorMax = new Vector2(1f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.offsetMin = new Vector2(40f, BarH + 14f); boxRt.offsetMax = new Vector2(-40f, BarH + 194f);
            var plate = UIFactory.MakePanel(boxRt, "Targhetta", Color.white, false);
            plate.sprite = UIArt.Brass(); plate.type = Image.Type.Sliced;
            plate.rectTransform.anchorMin = plate.rectTransform.anchorMax = new Vector2(0f, 1f);
            plate.rectTransform.pivot = new Vector2(0f, 0.5f);
            plate.rectTransform.anchoredPosition = new Vector2(30f, 0f);
            plate.rectTransform.sizeDelta = new Vector2(330f, 52f);
            nameLbl = UIFactory.MakeText(plate.rectTransform, "", 26, UIFactory.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(nameLbl.rectTransform, 12f, 2f, 12f, 2f);
            nameLbl.enableAutoSizing = true; nameLbl.fontSizeMin = 14; nameLbl.fontSizeMax = 26;
            namePlate = plate.gameObject;
            textLbl = UIFactory.MakeText(boxRt, "", 26, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(textLbl.rectTransform, 34f, 30f, 34f, 40f);
            textLbl.enableAutoSizing = true; textLbl.fontSizeMin = 16; textLbl.fontSizeMax = 26;
            hintLbl = UIFactory.MakeText(boxRt, "Premi E, Spazio o clicca per continuare", 16, new Color(0.23f, 0.16f, 0.1f, 0.6f), TextAlignmentOptions.BottomLeft);
            UIFactory.Stretch(hintLbl.rectTransform, 34f, 6f, 90f, 0f);
            var arrowImg = UIFactory.MakePanel(boxRt, "Continua", UITheme.Ceralacca, false);
            arrowImg.sprite = UIArt.Diamond();
            arrow = arrowImg.rectTransform;
            arrow.anchorMin = arrow.anchorMax = new Vector2(1f, 0f);
            arrow.pivot = new Vector2(1f, 0f);
            arrow.anchoredPosition = new Vector2(-28f, 12f);
            arrow.sizeDelta = new Vector2(30f, 30f);
            arrow.gameObject.SetActive(false);

            black = UIFactory.MakePanel(viewRt, "Dissolvenza", new Color(0f, 0f, 0f, 0f), false);
            UIFactory.Stretch(black.rectTransform);

            // bande cinematografiche (sopra tutto tranne il pulsante Salta)
            barTop = MakeBar(viewRt, true);
            barBottom = MakeBar(viewRt, false);

            // pulsante Salta (fuori dal contenitore scuotibile)
            var skipBtn = UIFactory.MakeButton(viewRt, "Salta", () => { skip = true; }, new Vector2(150f, 56f), ButtonStyle.Tag, 24f);
            UIFactory.Place(skipBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(150f, 56f));

            view.SetActive(false);
        }

        static RectTransform MakeBar(RectTransform parent, bool top)
        {
            var bar = UIFactory.MakePanel(parent, top ? "BandaSopra" : "BandaSotto", Color.black, false);
            var r = bar.rectTransform;
            r.anchorMin = new Vector2(0f, top ? 1f : 0f); r.anchorMax = new Vector2(1f, top ? 1f : 0f);
            r.pivot = new Vector2(0.5f, top ? 1f : 0f);
            r.sizeDelta = new Vector2(0f, BarH);
            return r;
        }

        void SetPortrait(Image img, bool visible)
        {
            img.enabled = visible;
            var f = img == imgL ? frameL : frameR;
            if (f != null) f.gameObject.SetActive(visible);
        }

        static Image MakePortrait(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var img = UIFactory.MakePanel(parent, name, Color.white, false);
            UIFactory.Place(img.rectTransform, anchor, pivot, pos, new Vector2(440f, 440f));
            img.preserveAspect = true;
            img.enabled = false;
            return img;
        }

        /// <summary>R012: capitolo = solo le battute con quel campo "capitolo" (es. "ingresso" e "chiusura" di cutscene_stracci); vuoto = tutte.</summary>
        public void Play(string cutsceneId, Action onDone, string capitolo = null)
        {
            var def = GameData.Cutscene(cutsceneId);
            if (def != null && !string.IsNullOrEmpty(capitolo))
            {
                var parte = new CutsceneDef { id = def.id + "#" + capitolo, titolo = def.titolo, sfondo = def.sfondo };
                foreach (var b in def.battute) if (b != null && b.capitolo == capitolo) parte.battute.Add(b);
                def = parte;
            }
            if (IsPlaying || def == null || def.battute == null || def.battute.Count == 0)
            {
                if (IsPlaying) Debug.LogWarning("Cutscene già in corso, ignoro: " + cutsceneId);
                if (onDone != null) onDone();
                return;
            }
            StartCoroutine(Run(def, onDone));
        }

        IEnumerator Run(CutsceneDef def, Action onDone)
        {
            IsPlaying = true;
            skip = false;
            guardUntil = Time.unscaledTime + 0.3f;
            GameManager.I.PushBlock("cutscene");
            if (UIRoot.I != null) UIRoot.I.HidePrompt();

            sideOf.Clear(); sideCounter = 0; shownL = shownR = false;
            SetPortrait(imgL, false); SetPortrait(imgR, false);
            scene.anchoredPosition = Vector2.zero;
            black.color = new Color(0f, 0f, 0f, 0f);
            SetBackground(def.sfondo);
            view.SetActive(true);
            // le bande nere scendono
            barTop.anchoredPosition = new Vector2(0f, UITheme.ReduceEffects ? 0f : BarH);
            barBottom.anchoredPosition = new Vector2(0f, UITheme.ReduceEffects ? 0f : -BarH);
            UITween.Move(barTop, Vector2.zero, 0.5f, Ease.OutCubic);
            UITween.Move(barBottom, Vector2.zero, 0.5f, Ease.OutCubic);

            for (int i = 0; i < def.battute.Count && !skip; i++)
                yield return PlayLine(def.battute[i]);

            if (AudioManager.I != null) AudioManager.I.StopVoice();
            if (zoomRoutine != null) { StopCoroutine(zoomRoutine); zoomRoutine = null; }
            UITween.Cancel(arrow);
            view.SetActive(false);
            IsPlaying = false;
            GameManager.I.PopBlock("cutscene");

            var s = GameManager.I.Save;
            if (!string.IsNullOrEmpty(def.id) && !s.cutsceneViste.Contains(def.id)) s.cutsceneViste.Add(def.id);
            GameManager.I.SaveGame();

            if (onDone != null) onDone();
        }

        IEnumerator PlayLine(BattutaDef b)
        {
            string azione = b.azione ?? "";

            if (azione == "dissolvenza")
            {
                yield return Fade(black, 0f, 1f, 0.4f);
                if (!string.IsNullOrEmpty(b.sfondo)) SetBackground(b.sfondo);
                yield return Fade(black, 1f, 0f, 0.4f);
            }
            else if (!string.IsNullOrEmpty(b.sfondo)) SetBackground(b.sfondo);

            // personaggio e lato
            var p = GameData.Personaggio(b.personaggio);
            bool narratore = string.IsNullOrEmpty(b.personaggio) || b.personaggio == "narratore";
            string side = null;
            if (!narratore)
            {
                if (azione == "entra_sx") side = "sx";
                else if (azione == "entra_dx") side = "dx";
                else if (b.posizione == "sx" || b.posizione == "dx") side = b.posizione;
                else if (!sideOf.TryGetValue(b.personaggio, out side)) side = (sideCounter++ % 2 == 0) ? "sx" : "dx";
                sideOf[b.personaggio] = side;
            }

            string nomeMostrato = p != null ? p.nome : (narratore ? "" : b.personaggio);
            Color colNome = UIFactory.Accento;
            if (p != null && !string.IsNullOrEmpty(p.colore)) { Color c; if (ColorUtility.TryParseHtmlString(p.colore, out c)) colNome = c; }
            nameLbl.text = nomeMostrato;
            nameLbl.color = Color.Lerp(colNome, UIFactory.Inchiostro, 0.6f);
            namePlate.SetActive(!string.IsNullOrEmpty(nomeMostrato));

            // ritratti
            if (!narratore)
            {
                Image img = side == "sx" ? imgL : imgR;
                Sprite spr = UIFactory.LoadSprite("ritratti", p != null ? p.ritratto : b.personaggio);
                if (spr != null)
                {
                    img.sprite = spr;
                    img.color = Color.white;
                    SetPortrait(img, true);
                    bool wasShown = side == "sx" ? shownL : shownR;
                    if (side == "sx") shownL = true; else shownR = true;
                    if (azione.StartsWith("entra_") || !wasShown) SlideIn(img, side == "sx");
                }
                Dim(imgL, img == imgL);
                Dim(imgR, img == imgR);
            }
            else { Dim(imgL, false); Dim(imgR, false); }

            if (azione == "zoom") { if (zoomRoutine != null) StopCoroutine(zoomRoutine); zoomRoutine = StartCoroutine(Zoom()); }
            if (azione == "scuoti") { UIFx.Shake(scene, 14f, 0.4f); UISfx.Play(Sfx.Stamp, 0.4f); }

            // testo
            textLbl.text = b.testo ?? "";
            textLbl.maxVisibleCharacters = 0;
            hintLbl.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            textLbl.ForceMeshUpdate();
            int total = textLbl.textInfo.characterCount;

            AudioClip clip = VoiceLibrary.Get(System.IO.Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(b.audio) ? b.id : b.audio));
            if (clip != null && AudioManager.I != null) AudioManager.I.PlayVoice(clip);
            float clipEnd = clip != null ? Time.unscaledTime + clip.length + 0.4f : 0f;

            float shown = 0f;
            while (shown < total && !skip)
            {
                if (Time.unscaledTime >= guardUntil && InputHelper.ConfirmPressed(true)) break;
                shown += CharsPerSecond * Time.unscaledDeltaTime;
                int vis = Mathf.Min(total, Mathf.FloorToInt(shown));
                if (vis != textLbl.maxVisibleCharacters && Time.unscaledTime >= nextKey) { nextKey = Time.unscaledTime + 0.075f; UISfx.Play(Sfx.TypeKey, 0.2f, 0.12f); }
                textLbl.maxVisibleCharacters = vis;
                yield return null;
            }
            textLbl.maxVisibleCharacters = 99999;
            if (skip) yield break;
            yield return null;

            hintLbl.gameObject.SetActive(true);
            ShowArrow();
            while (!skip)
            {
                if (Time.unscaledTime >= guardUntil && InputHelper.ConfirmPressed(true)) break;
                if (clip != null && Time.unscaledTime >= clipEnd && !(AudioManager.I != null && AudioManager.I.VoicePlaying)) break;
                yield return null;
            }
            if (AudioManager.I != null) AudioManager.I.StopVoice();

            if (azione == "esce" && !narratore && !skip)
            {
                Image img = side == "sx" ? imgL : imgR;
                if (side == "sx") shownL = false; else shownR = false;
                yield return Fade(img, 1f, 0f, 0.35f);
                SetPortrait(img, false);
            }
        }

        void SetBackground(string nome)
        {
            if (zoomRoutine != null) { StopCoroutine(zoomRoutine); zoomRoutine = null; }
            bgRect.localScale = Vector3.one;
            if (string.IsNullOrEmpty(nome)) return;
            Sprite s = UIFactory.LoadSprite("sfondi", nome);
            if (s != null) { bg.sprite = s; bg.color = Color.white; }
            else { bg.sprite = null; bg.color = new Color(0.2f, 0.14f, 0.09f); }
        }

        static void Dim(Image img, bool active)
        {
            if (!img.enabled) return;
            float a = active ? 1f : 0.5f;
            img.color = new Color(a, a, a, 1f);
        }

        void SlideIn(Image img, bool left)
        {
            var rt = img.rectTransform;
            Vector2 rest = left ? RestL : RestR;
            UITween.Cancel(rt);
            rt.anchoredPosition = UITheme.ReduceEffects ? rest : rest + new Vector2(left ? -520f : 520f, 0f);
            UITween.Move(rt, rest, 0.5f, Ease.OutBack);
            UISfx.Play(Sfx.Whoosh, 0.3f);
        }

        void ShowArrow()
        {
            arrow.gameObject.SetActive(true);
            UITween.Cancel(arrow);
            if (UITheme.ReduceEffects) return;
            UITween.Run(arrow, 0.9f, Ease.Linear, v => { if (arrow != null) arrow.anchoredPosition = new Vector2(-28f, 12f + Mathf.Sin(v * Mathf.PI * 2f) * 5f); }, null, 0f, true);
        }

        IEnumerator Zoom()
        {
            const float dur = 6f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                bgRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t / dur);
                yield return null;
            }
            bgRect.localScale = Vector3.one * 1.15f;
        }

        static IEnumerator Fade(Image img, float from, float to, float dur)
        {
            Color c = img.color;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(from, to, t / dur);
                img.color = c;
                yield return null;
            }
            c.a = to;
            img.color = c;
        }
    }
}
