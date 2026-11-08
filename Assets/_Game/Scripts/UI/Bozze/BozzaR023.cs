using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TrenoBambini.UI.Bozze
{
    public enum BozzaTipo { MenuA, MenuB, InventarioA, InventarioB }

    [System.Serializable] public class BozzaVoce { public string id, etichetta, nota; }
    [System.Serializable] public class BozzaAnteprima { public string intestazione, oggetti, tempo, oggettoId, luogoId; }
    [System.Serializable] public class BozzaInventarioTesti { public string titoloLatta, intestazioneQuaderno, etichettaTema, aiuto, daTrovare; }
    [System.Serializable]
    public class BozzaTesti
    {
        public string titolo1, titolo2, sottotitolo;
        public List<BozzaVoce> voci;
        public BozzaAnteprima anteprima;
        public List<string> sezioni;
        public BozzaInventarioTesti inventario;
    }

    /// <summary>
    /// R023 fase 1: BOZZE del menu iniziale e dell'inventario. Vive solo per le prove (menu Treno > R023 > Bozza ...): non sostituisce
    /// TitleScreen ne' InventoryPanel. Gira nella scena 10_Hub_Vicolo, prende la camera principale, la mette sul vicolo all'imbrunire
    /// (luce, nebbia e post-processing cambiati a runtime, nessun bake) oppure su un piccolo set da tavolo per la latta.
    /// Testi in Resources/Data/r023_bozze.json e oggetti.json.
    /// </summary>
    public class BozzaR023 : MonoBehaviour
    {
        public BozzaTipo tipo;
        public string capturaNome = "";   // se non vuoto, scatta lo screenshot dopo "capturaDopo" secondi
        public float capturaDopo = 6f;
        public bool Fatto { get; private set; }

        // asset assegnati dall'editor (le bozze non li caricano da Resources)
        public Material matLegno, matPanno, matCuoio, matSuola, matTerzo;
        public Mesh meshScarpa;

        BozzaTesti T;
        Camera cam;
        Canvas canvas;
        RectTransform root;
        readonly List<RectTransform> righe = new List<RectTransform>();
        readonly List<Graphic> righeTesto = new List<Graphic>();
        readonly List<TextMeshProUGUI> etichette = new List<TextMeshProUGUI>();
        RectTransform barra, anteprimaCard;
        int sel;
        Vector3 camBase; Quaternion camRot;
        Transform oggettoGira;
        float oggettoYaw;
        Light flamma;

        void Start() { StartCoroutine(Avvia()); }

        IEnumerator Avvia()
        {
            yield return null; yield return null;
            var ta = Resources.Load<TextAsset>("Data/r023_bozze");
            T = ta != null ? JsonUtility.FromJson<BozzaTesti>(ta.text) : new BozzaTesti();
            Isola();
            if (tipo == BozzaTipo.MenuA || tipo == BozzaTipo.MenuB) CostruisciMenu();
            else CostruisciInventario();
            if (!string.IsNullOrEmpty(capturaNome)) yield return Cattura();
        }

        // ------------------------------------------------------------------ preparazione scena
        void Isola()
        {
            cam = Camera.main;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb == this) continue;
                var ns = mb.GetType().Namespace;
                if (ns == "TrenoBambini.Player") mb.enabled = false;
            }
            foreach (var p in FindObjectsByType<TrenoBambini.Player.ThirdPersonController>(FindObjectsInactive.Include)) p.gameObject.SetActive(false);
            var brain = cam.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null) brain.enabled = false;
            
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            var cgo = new GameObject("BozzaCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1280f, 720f);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            root = (RectTransform)cgo.transform;
        }

        // ------------------------------------------------------------------ imbrunire sul vicolo
        void Imbrunire(float intensitaSole, float nebbia)
        {
            Light sun = RenderSettings.sun;
            if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null)
            {
                sun.color = new Color(1f, 0.55f, 0.32f);
                sun.intensity = intensitaSole * 0.55f;
                sun.transform.rotation = Quaternion.Euler(7f, 62f, 0f);
            }
            RenderSettings.ambientIntensity = 0.32f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.30f, 0.20f, 0.15f);
            RenderSettings.fogDensity = nebbia;

            var vgo = new GameObject("BozzaVolume");
            var vol = vgo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 90;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = prof.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.6f);
            ca.saturation.Override(-22f);
            ca.contrast.Override(14f);
            ca.colorFilter.Override(new Color(1f, 0.86f, 0.70f));
            var vg = prof.Add<Vignette>(true);
            vg.intensity.Override(0.42f); vg.smoothness.Override(0.6f);
            var fg = prof.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Medium1); fg.intensity.Override(0.55f); fg.response.Override(0.7f);
            var bl = prof.Add<Bloom>(true);
            bl.intensity.Override(1.1f); bl.threshold.Override(0.7f); bl.tint.Override(new Color(1f, 0.8f, 0.55f));
            vol.sharedProfile = prof;
        }

        void LuciFinestre(Vector3 origine, Vector3 avanti, Vector3 destra, float h)
        {
            // luci calde di finestre e lampade lungo il vicolo: poche luci dinamiche, nessun bake
            float[] d = { 6f, 11f, 17f, 24f, 32f };
            for (int i = 0; i < d.Length; i++)
            {
                var go = new GameObject("BozzaFinestra_" + i);
                go.transform.position = origine + avanti * d[i] + destra * ((i % 2 == 0) ? -3.4f : 3.4f) + Vector3.up * (h + (i % 3) * 0.9f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point; l.range = 9f; l.intensity = 40f;
                l.color = new Color(1f, 0.62f, 0.28f); l.shadows = LightShadows.None;
            }
        }

        float Suolo(Vector3 p)
        {
            Physics.SyncTransforms();
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 100f))
                if (h.collider.name.StartsWith("Selciato_")) return h.point.y;
            return 0f;
        }

        // ------------------------------------------------------------------ MENU
        void CostruisciMenu()
        {
            bool a = tipo == BozzaTipo.MenuA;
            Vector3 p; Vector3 guarda;
            if (a) { p = new Vector3(0.2f, 0f, -20f); guarda = new Vector3(-4.2f, 2.6f, 6f); }
            else { p = new Vector3(0.4f, 0f, -12f); guarda = new Vector3(0.1f, 2.5f, 6f); }
            p.y = Suolo(p) + (a ? 1.7f : 1.15f);
            camBase = p;
            camRot = Quaternion.LookRotation(guarda - p);
            cam.transform.SetPositionAndRotation(p, camRot);
            cam.fieldOfView = a ? 58f : 52f;
            Imbrunire(a ? 0.9f : 0.8f, a ? 0.016f : 0.013f);
            LuciFinestre(p, (new Vector3(guarda.x - p.x, 0f, guarda.z - p.z)).normalized, Vector3.Cross(Vector3.up, (new Vector3(guarda.x - p.x, 0f, guarda.z - p.z)).normalized), p.y + 1.6f);

            // pellicola: vignetta e grana
            var vig = UIFactory.MakePanel(root, "Vignetta", new Color(0.06f, 0.03f, 0.01f, 0.55f), false);
            vig.sprite = UIArt.Vignette(new Color(0.05f, 0.025f, 0.01f, 1f));
            vig.color = Color.white;
            UIFactory.Stretch(vig.rectTransform);
            if (a)
            {
                // fascia sinistra scura morbida sotto la locandina
                var sh = UIFactory.MakePanel(root, "Ombra", new Color(0f, 0f, 0f, 0.3f), false);
                sh.sprite = UIArt.Glow();
                UIFactory.Place(sh.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-60f, 0f), new Vector2(860f, 980f));
            }
            var grana = UIFactory.NewRect("Grana", root);
            UIFactory.Stretch(grana);
            var raw = grana.gameObject.AddComponent<RawImage>();
            raw.texture = UIArt.Grain(); raw.color = new Color(1f, 0.95f, 0.85f, 0.035f); raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 10f, 5.625f);
            grana.gameObject.AddComponent<GrainJitter>().target = raw;

            if (a) MenuLocandina(); else MenuBiglietti();
            Seleziona(0);
        }

        string TestoAnteprima()
        {
            var an = T.anteprima ?? new BozzaAnteprima();
            var o = GameData.Oggetto(an.oggettoId);
            var l = GameData.Luogo(an.luogoId);
            return (o != null ? o.nome : an.oggettoId) + "\n" + (l != null ? l.nome : an.luogoId) + "\n" + an.oggetti + "\n" + an.tempo;
        }

        void MenuLocandina()
        {
            var poster = UIFactory.MakePaper(root, "Locandina", new Color(1f, 0.96f, 0.86f, 1f), false);
            var prt = poster.rectTransform;
            UIFactory.Place(prt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(62f, -4f), new Vector2(470f, 650f));
            prt.localRotation = Quaternion.Euler(0f, 0f, -1.4f);
            var cg = poster.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f;
            UITween.Alpha(cg, 1f, 0.7f, Ease.OutCubic);
            UITween.Move(prt, prt.anchoredPosition, 0.8f, Ease.OutCubic);
            prt.anchoredPosition += new Vector2(-40f, 0f);
            UITween.Move(prt, new Vector2(62f, -4f), 0.8f, Ease.OutCubic);

            // spille d'ottone
            foreach (float x in new[] { -190f, 190f })
            {
                var pin = UIFactory.MakePanel(prt, "Spilla", UITheme.Ottone, false);
                pin.sprite = UIFactory.Circle();
                UIFactory.Place(pin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -22f), new Vector2(18f, 18f));
            }

            var t1 = UIFactory.MakeText(prt, T.titolo1, 74, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(t1.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(440f, 84f));
            var t2 = UIFactory.MakeText(prt, T.titolo2, 74, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(t2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(440f, 84f));
            var rule = UIFactory.MakePanel(prt, "Filetto", UITheme.Inchiostro, false);
            UIFactory.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(360f, 3f));
            var sub = UIFactory.MakeText(prt, T.sottotitolo, 30, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(440f, 42f));

            // voci a macchina da scrivere con matita rossa sulla voce scelta
            var lista = UIFactory.NewRect("Voci", prt);
            lista.anchorMin = lista.anchorMax = new Vector2(0.5f, 1f); lista.pivot = new Vector2(0.5f, 1f);
            lista.anchoredPosition = new Vector2(0f, -290f); lista.sizeDelta = new Vector2(400f, 340f);
            var bar = UIFactory.MakePanel(lista, "Matita", new Color(UITheme.Ceralacca.r, UITheme.Ceralacca.g, UITheme.Ceralacca.b, 0.92f), false);
            bar.sprite = UIArt.Tag(); bar.type = Image.Type.Sliced;
            barra = bar.rectTransform;
            barra.anchorMin = barra.anchorMax = new Vector2(0.5f, 1f); barra.pivot = new Vector2(0.5f, 1f);
            barra.sizeDelta = new Vector2(380f, 50f);
            for (int i = 0; i < T.voci.Count; i++)
            {
                var r = UIFactory.NewRect("Voce_" + T.voci[i].id, lista);
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -i * 54f); r.sizeDelta = new Vector2(380f, 50f);
                var tx = UIFactory.MakeText(r, T.voci[i].etichetta, 33, UITheme.Inchiostro, TextAlignmentOptions.Left, FontStyles.Normal);
                UIFactory.Stretch(tx.rectTransform, 26f, 0f, 0f, 0f);
                righe.Add(r); etichette.Add(tx);
            }

            // anteprima di Continua: foglietto appuntato accanto alla locandina
            var card = UIFactory.MakePanel(root, "Anteprima", Color.white, false);
            card.sprite = UIArt.Tag(); card.type = Image.Type.Sliced;
            anteprimaCard = card.rectTransform;
            UIFactory.Place(anteprimaCard, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(560f, 118f), new Vector2(360f, 170f));
            anteprimaCard.localRotation = Quaternion.Euler(0f, 0f, 3f);
            var h = UIFactory.MakeText(anteprimaCard, T.anteprima.intestazione, 20, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -14f), new Vector2(300f, 28f));
            var b = UIFactory.MakeText(anteprimaCard, TestoAnteprima(), 22, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(b.rectTransform, 26f, 12f, 16f, 46f);
            b.enableAutoSizing = true; b.fontSizeMin = 13; b.fontSizeMax = 22;
        }

        void MenuBiglietti()
        {
            // titolo in alto, su un velo scuro morbido
            var glow = UIFactory.MakePanel(root, "VeloTitolo", new Color(0f, 0f, 0f, 0.5f), false);
            glow.sprite = UIArt.Glow();
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 60f), new Vector2(1100f, 380f));
            var t = UIFactory.MakeText(root, T.titolo1 + " " + T.titolo2, 82, UITheme.CartaChiara, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(1100f, 100f));
            var s = UIFactory.MakeText(root, T.sottotitolo, 34, UITheme.OttoneLuce, TextAlignmentOptions.Center, FontStyles.Italic);
            UIFactory.Place(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(600f, 46f));

            // biglietti del treno allineati in basso
            int n = T.voci.Count;
            float w = 186f, gap = 8f, tot = n * w + (n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                var tk = UIFactory.MakePanel(root, "Biglietto_" + T.voci[i].id, Color.white, false);
                tk.sprite = UIArt.Ticket(); tk.type = Image.Type.Sliced;
                var r = tk.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f);
                r.sizeDelta = new Vector2(w, 104f);
                float x = -tot / 2f + w / 2f + i * (w + gap);
                r.anchoredPosition = new Vector2(x, 40f);
                r.localRotation = Quaternion.Euler(0f, 0f, ((i * 37) % 5 - 2) * 0.9f);
                var lab = UIFactory.MakeText(r, T.voci[i].etichetta, 25, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
                UIFactory.Stretch(lab.rectTransform, 26f, 30f, 6f, 8f);
                lab.enableAutoSizing = true; lab.fontSizeMin = 14; lab.fontSizeMax = 25;
                var nota = UIFactory.MakeText(r, T.voci[i].nota, 15, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Italic);
                UIFactory.Stretch(nota.rectTransform, 26f, 12f, 6f, 70f);
                righe.Add(r); etichette.Add(lab); righeTesto.Add(tk);
                r.localScale = Vector3.zero;
                UITween.Scale(r, Vector3.one, 0.4f, Ease.OutBack, null, 0.2f + i * 0.08f);
            }

            var card = UIFactory.MakePanel(root, "Anteprima", Color.white, false);
            card.sprite = UIArt.Ticket(); card.type = Image.Type.Sliced;
            anteprimaCard = card.rectTransform;
            anteprimaCard.anchorMin = anteprimaCard.anchorMax = new Vector2(0.5f, 0f); anteprimaCard.pivot = new Vector2(0.5f, 0f);
            anteprimaCard.sizeDelta = new Vector2(420f, 150f);
            anteprimaCard.anchoredPosition = new Vector2(-tot / 2f + 210f - 20f, 168f);
            anteprimaCard.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
            var h = UIFactory.MakeText(anteprimaCard, T.anteprima.intestazione, 19, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
            UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -14f), new Vector2(340f, 26f));
            var b = UIFactory.MakeText(anteprimaCard, TestoAnteprima(), 21, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(b.rectTransform, 38f, 14f, 14f, 44f);
            b.enableAutoSizing = true; b.fontSizeMin = 12; b.fontSizeMax = 21;
        }

        void Seleziona(int i)
        {
            if (T == null || T.voci == null || righe.Count == 0) return;
            sel = (i + righe.Count) % righe.Count;
            bool a = tipo == BozzaTipo.MenuA;
            for (int k = 0; k < righe.Count; k++)
            {
                bool on = k == sel;
                if (a) etichette[k].color = on ? UITheme.CartaChiara : UITheme.Inchiostro;
                else
                {
                    var r = righe[k];
                    float x = r.anchoredPosition.x;
                    r.anchoredPosition = new Vector2(x, on ? 62f : 40f);
                    r.localScale = Vector3.one * (on ? 1.07f : 1f);
                    if (righeTesto.Count > k) ((Image)righeTesto[k]).color = on ? Color.white : new Color(0.82f, 0.78f, 0.7f, 1f);
                }
            }
            if (a && barra != null) barra.anchoredPosition = new Vector2(0f, -sel * 54f);
            if (anteprimaCard != null) anteprimaCard.gameObject.SetActive(sel == 0);
        }

        // ------------------------------------------------------------------ INVENTARIO
        Material Lit(Color c, float metal, float smooth)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            return m;
        }

        GameObject Cubo(Transform parent, string nome, Vector3 pos, Vector3 scala, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nome;
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scala;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        Transform Scarpa(Transform set, Vector3 pos, float yaw)
        {
            var holder = new GameObject("Scarpa_3D").transform;
            holder.SetParent(set, false);
            var inner = new GameObject("Mesh").transform;
            inner.SetParent(holder, false);
            inner.gameObject.AddComponent<MeshFilter>().sharedMesh = meshScarpa;
            var mr = inner.gameObject.AddComponent<MeshRenderer>();
            var mats = new List<Material>();
            for (int k = 0; k < meshScarpa.subMeshCount; k++) mats.Add(k == 1 ? matSuola : matCuoio);
            mr.sharedMaterials = mats.ToArray();
            var bnd = meshScarpa.bounds;
            float sc = 0.2f / Mathf.Max(bnd.size.x, bnd.size.z, 0.0001f);
            inner.localScale = Vector3.one * sc;
            inner.localPosition = -bnd.center * sc;
            holder.localPosition = pos;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return holder;
        }

        void CostruisciInventario()
        {
            bool a = tipo == BozzaTipo.InventarioA;
            // luce del vicolo spenta: set da tavolo illuminato dalla lampada
            Light sun = RenderSettings.sun;
            if (sun != null) sun.intensity = 0f;
            RenderSettings.fog = false;
            RenderSettings.ambientIntensity = 0.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.022f, 0.014f);
            cam.fieldOfView = a ? 32f : 32f;

            var set = new GameObject("StudioLatta").transform;
            set.position = new Vector3(800f, 0f, 800f);
            var tavolo = Cubo(set, "Tavolo", new Vector3(0f, -0.03f, 0f), new Vector3(2.6f, 0.06f, 1.6f), matLegno != null ? matLegno : Lit(new Color(0.3f, 0.2f, 0.12f), 0f, 0.3f));
            var panno = Cubo(set, "Panno", new Vector3(a ? -0.1f : 0f, 0.002f, 0f), new Vector3(0.85f, 0.004f, 0.6f), matPanno != null ? matPanno : Lit(new Color(0.45f, 0.38f, 0.28f), 0f, 0.1f));
            panno.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);

            // la latta (smalto rosso scuro consumato, interno stagnato)
            var smalto = Lit(new Color(0.34f, 0.09f, 0.07f), 0.55f, 0.5f);
            var stagno = Lit(new Color(0.55f, 0.55f, 0.52f), 0.85f, 0.45f);
            var tin = new GameObject("Latta").transform;
            tin.SetParent(set, false);
            tin.localPosition = new Vector3(a ? -0.14f : 0f, 0f, a ? 0.02f : 0.12f);
            tin.localRotation = Quaternion.Euler(0f, a ? 8f : -4f, 0f);
            float L = 0.36f, D = 0.25f, H = 0.1f, s = 0.008f;
            Cubo(tin, "Fondo", new Vector3(0f, s / 2f, 0f), new Vector3(L, s, D), smalto);
            Cubo(tin, "Interno", new Vector3(0f, s + 0.001f, 0f), new Vector3(L - 2 * s, 0.002f, D - 2 * s), stagno);
            Cubo(tin, "ParetePosteriore", new Vector3(0f, H / 2f, D / 2f - s / 2f), new Vector3(L, H, s), smalto);
            Cubo(tin, "Davanti", new Vector3(0f, H / 2f, -D / 2f + s / 2f), new Vector3(L, H, s), smalto);
            Cubo(tin, "Sinistra", new Vector3(-L / 2f + s / 2f, H / 2f, 0f), new Vector3(s, H, D), smalto);
            Cubo(tin, "Destra", new Vector3(L / 2f - s / 2f, H / 2f, 0f), new Vector3(s, H, D), smalto);
            Cubo(tin, "OrloDavanti", new Vector3(0f, H - 0.002f, -D / 2f + s / 2f), new Vector3(L + 0.006f, 0.008f, s + 0.006f), stagno);
            Cubo(tin, "Imbottitura", new Vector3(0f, s + 0.012f, 0f), new Vector3(L - 2 * s - 0.01f, 0.02f, D - 2 * s - 0.01f), matPanno != null ? matPanno : Lit(new Color(0.4f, 0.3f, 0.2f), 0f, 0.1f));
            // coperchio aperto sul retro, inclinato
            var hinge = new GameObject("Cerniera").transform;
            hinge.SetParent(tin, false);
            hinge.localPosition = new Vector3(0f, H, D / 2f);
            hinge.localRotation = Quaternion.Euler(105f, 0f, 0f);
            Cubo(hinge, "Coperchio", new Vector3(0f, 0.004f, -D / 2f), new Vector3(L + 0.008f, s, D + 0.008f), smalto);
            Cubo(hinge, "InternoCoperchio", new Vector3(0f, -0.0008f, -D / 2f), new Vector3(L - 0.01f, 0.0015f, D - 0.01f), stagno);
            Cubo(hinge, "OrloCoperchio", new Vector3(0f, -0.01f, -0.004f), new Vector3(L + 0.008f, 0.022f, s), smalto);

            // le scarpe in 3D: una esaminabile (gira), una appoggiata nella latta
            if (meshScarpa != null)
            {
                oggettoGira = Scarpa(set, a ? new Vector3(-0.14f, 0.2f, 0.0f) : new Vector3(0.05f, 0.075f, 0.12f), 25f);
                oggettoYaw = 25f;
                Scarpa(set, a ? new Vector3(-0.2f, 0.045f, 0.0f) : new Vector3(-0.08f, 0.045f, 0.1f), a ? -80f : 100f);
                var fl = new GameObject("LucePerOggetto");
                fl.transform.SetParent(set, false);
                fl.transform.localPosition = new Vector3(-0.35f, 0.5f, -0.3f);
                var lf = fl.AddComponent<Light>(); lf.type = LightType.Point; lf.range = 1.6f; lf.intensity = 0.7f; lf.color = new Color(1f, 0.78f, 0.5f);
            }

            // candela sul tavolo: luce principale calda
            var cand = new GameObject("Candela").transform;
            cand.SetParent(set, false);
            cand.localPosition = new Vector3(a ? -0.7f : -0.85f, 0f, a ? 0.18f : 0.45f);
            var cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cil.GetComponent<Collider>());
            cil.transform.SetParent(cand, false);
            cil.transform.localPosition = new Vector3(0f, 0.05f, 0f); cil.transform.localScale = new Vector3(0.035f, 0.05f, 0.035f);
            cil.GetComponent<MeshRenderer>().sharedMaterial = Lit(new Color(0.85f, 0.78f, 0.6f), 0f, 0.35f);
            var fm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(fm.GetComponent<Collider>());
            fm.transform.SetParent(cand, false);
            fm.transform.localPosition = new Vector3(0f, 0.115f, 0f); fm.transform.localScale = new Vector3(0.012f, 0.022f, 0.012f);
            var fmat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); fmat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f));
            fm.GetComponent<MeshRenderer>().sharedMaterial = fmat;
            var lg = new GameObject("LuceCandela"); lg.transform.SetParent(cand, false); lg.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            flamma = lg.AddComponent<Light>();
            flamma.type = LightType.Point; flamma.range = 3.2f; flamma.intensity = 1.6f; flamma.color = new Color(1f, 0.66f, 0.3f); flamma.shadows = LightShadows.Soft;
            // lampada a spot dall'alto a sinistra
            var sp = new GameObject("LuceLampada"); sp.transform.SetParent(set, false);
            sp.transform.localPosition = new Vector3(-0.5f, 0.9f, -0.5f);
            sp.transform.LookAt(set.position + new Vector3(a ? -0.1f : 0f, 0.05f, 0.05f));
            var spl = sp.AddComponent<Light>(); spl.type = LightType.Spot; spl.spotAngle = 55f; spl.innerSpotAngle = 25f; spl.range = 3f;
            spl.intensity = 3.2f; spl.color = new Color(1f, 0.82f, 0.6f); spl.shadows = LightShadows.Soft;

            // camera
            Vector3 c0, look;
            if (a) { c0 = set.position + new Vector3(0.06f, 0.78f, -1.1f); look = set.position + new Vector3(0.06f, 0.05f, 0f); }
            else { c0 = set.position + new Vector3(0f, 1.45f, -0.55f); look = set.position + new Vector3(0f, 0f, 0.03f); }
            cam.transform.SetPositionAndRotation(c0, Quaternion.LookRotation(look - c0));
            camBase = c0; camRot = cam.transform.rotation;

            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var vgo = new GameObject("BozzaVolume"); var vol = vgo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 90;
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.3f); ca.saturation.Override(-8f); ca.colorFilter.Override(new Color(1f, 0.9f, 0.78f));
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.5f); vg.smoothness.Override(0.55f);
            var fg = prof.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Medium1); fg.intensity.Override(0.5f);
            var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.35f); bl.threshold.Override(1.2f);
            vol.sharedProfile = prof;

            InventarioUI(a);
        }

        void InventarioUI(bool a)
        {
            var vig = UIFactory.MakePanel(root, "Vignetta", Color.white, false);
            vig.sprite = UIArt.Vignette(new Color(0.03f, 0.015f, 0.005f, 1f));
            UIFactory.Stretch(vig.rectTransform);
            var grana = UIFactory.NewRect("Grana", root); UIFactory.Stretch(grana);
            var raw = grana.gameObject.AddComponent<RawImage>(); raw.texture = UIArt.Grain(); raw.color = new Color(1f, 0.95f, 0.85f, 0.06f); raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 10f, 5.625f); grana.gameObject.AddComponent<GrainJitter>().target = raw;

            var o = GameData.Oggetto("scarpe");
            string nome = o != null ? o.nome : "Le scarpe";
            string desc = o != null ? o.descrizione : "";
            string signif = o != null ? o.significato : "";
            string tema = o != null ? o.temaCivico : "";

            // cartellino della latta
            var tag = UIFactory.MakePanel(root, "Cartellino", Color.white, false);
            tag.sprite = UIArt.Brass(); tag.type = Image.Type.Sliced;
            UIFactory.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -36f), new Vector2(250f, 56f));
            tag.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 2f);
            var tt = UIFactory.MakeText(tag.rectTransform, T.inventario.titoloLatta, 30, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(tt.rectTransform);

            // nome dell'oggetto sotto la latta, come etichetta di carta
            var lab = UIFactory.MakePanel(root, "Etichetta", Color.white, false);
            lab.sprite = UIArt.Tag(); lab.type = Image.Type.Sliced;
            if (a) UIFactory.Place(lab.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(330f, 74f), new Vector2(280f, 54f));
            else UIFactory.Place(lab.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -120f), new Vector2(280f, 54f));
            lab.rectTransform.localRotation = Quaternion.Euler(0f, 0f, a ? -2f : 1.5f);
            var lt = UIFactory.MakeText(lab.rectTransform, nome, 28, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Stretch(lt.rectTransform, 12f, 0f, 12f, 0f);

            var aiuto = UIFactory.MakeText(root, T.inventario.aiuto, 19, new Color(0.96f, 0.9f, 0.75f, 0.85f), TextAlignmentOptions.BottomLeft);
            if (a) UIFactory.Place(aiuto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(46f, 18f), new Vector2(680f, 30f));
            else { aiuto.alignment = TextAlignmentOptions.TopRight; UIFactory.Place(aiuto.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(680f, 30f)); }

            // quaderno di scuola
            var cover = UIFactory.MakePanel(root, "Quaderno", Color.white, false);
            cover.sprite = UIArt.Cover(); cover.type = Image.Type.Sliced;
            var crt = cover.rectTransform;
            if (a) { UIFactory.Place(crt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, -22f), new Vector2(520f, 590f)); crt.localRotation = Quaternion.Euler(0f, 0f, 1.2f); }
            else { UIFactory.Place(crt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(1180f, 262f)); }

            var page = UIFactory.MakePanel(crt, "Pagina", Color.white, false);
            page.sprite = UIArt.Notebook(); page.type = Image.Type.Sliced;
            UIFactory.Stretch(page.rectTransform, 14f, 14f, 14f, 14f);
            var ruled = UIFactory.NewRect("Righe", page.rectTransform);
            UIFactory.Stretch(ruled, 16f, 16f, 16f, 16f);
            var rr = ruled.gameObject.AddComponent<RawImage>(); rr.texture = UIArt.Ruled(); rr.raycastTarget = false;
            rr.uvRect = new Rect(0f, 0f, 1f, a ? 17f : 7.5f);
            if (!a)
            {
                var spine = UIFactory.MakePanel(page.rectTransform, "Piega", new Color(0.35f, 0.28f, 0.18f, 0.35f), false);
                spine.sprite = UIArt.Glow();
                UIFactory.Place(spine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 300f));
            }

            // linguette delle sezioni
            for (int i = 0; i < T.sezioni.Count; i++)
            {
                var tb = UIFactory.MakePanel(crt, "Linguetta_" + i, Color.white, false);
                tb.sprite = UIArt.Tag(); tb.type = Image.Type.Sliced;
                bool on = i == 0;
                tb.color = on ? Color.white : new Color(0.8f, 0.74f, 0.6f, 1f);
                var r = tb.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 0f);
                r.sizeDelta = new Vector2(a ? 112f : 128f, 40f);
                r.anchoredPosition = new Vector2(30f + i * (a ? 118f : 134f), on ? -6f : -10f);
                var tx = UIFactory.MakeText(r, T.sezioni[i], 20, UITheme.Inchiostro, TextAlignmentOptions.Center, on ? FontStyles.Bold : FontStyles.Normal);
                UIFactory.Stretch(tx.rectTransform, 4f, 0f, 4f, 0f);
                r.SetAsFirstSibling();
            }
            page.rectTransform.SetAsLastSibling(); ruled.SetAsLastSibling();

            if (a)
            {
                var h = UIFactory.MakeText(page.rectTransform, nome, 40, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
                UIFactory.Place(h.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(54f, -26f), new Vector2(400f, 54f));
                var body = UIFactory.MakeText(page.rectTransform, signif, 22, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Italic);
                UIFactory.Stretch(body.rectTransform, 54f, 190f, 36f, 92f);
                body.enableAutoSizing = true; body.fontSizeMin = 13; body.fontSizeMax = 22;
                StampoTema(page.rectTransform, tema, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(410f, 158f));
            }
            else
            {
                var h1 = UIFactory.MakeText(page.rectTransform, nome, 32, UITheme.Ceralacca, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
                UIFactory.Place(h1.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -16f), new Vector2(480f, 42f));
                var left = UIFactory.MakeText(page.rectTransform, desc, 19, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Italic);
                UIFactory.Stretch(left.rectTransform, 50f, 14f, 640f - 14f + 50f - 50f, 62f);
                left.rectTransform.anchorMax = new Vector2(0.5f, 1f); left.rectTransform.offsetMax = new Vector2(-34f, -62f);
                left.enableAutoSizing = true; left.fontSizeMin = 11; left.fontSizeMax = 19;
                var right = UIFactory.MakeText(page.rectTransform, signif, 19, UITheme.InchiostroBlu, TextAlignmentOptions.TopLeft, FontStyles.Italic);
                right.rectTransform.anchorMin = new Vector2(0.5f, 0f); right.rectTransform.anchorMax = new Vector2(1f, 1f);
                right.rectTransform.offsetMin = new Vector2(40f, 14f); right.rectTransform.offsetMax = new Vector2(-40f, -22f);
                right.enableAutoSizing = true; right.fontSizeMin = 11; right.fontSizeMax = 19;
            }
        }

        void StampoTema(RectTransform page, string tema, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var st = UIFactory.MakePanel(page, "TimbroTema", new Color(UITheme.InchiostroBlu.r, UITheme.InchiostroBlu.g, UITheme.InchiostroBlu.b, 0.85f), false);
            st.sprite = UIArt.StampFrame(); st.type = Image.Type.Sliced;
            UIFactory.Place(st.rectTransform, anchor, new Vector2(0.5f, 0f), pos, size);
            st.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2f);
            var h = UIFactory.MakeText(st.rectTransform, T.inventario.etichettaTema.ToUpperInvariant(), 16, UITheme.InchiostroBlu, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(size.x - 20f, 24f));
            var b = UIFactory.MakeText(st.rectTransform, tema, 15, UITheme.InchiostroBlu, TextAlignmentOptions.Center);
            UIFactory.Stretch(b.rectTransform, 14f, 8f, 14f, 34f);
            b.enableAutoSizing = true; b.fontSizeMin = 9; b.fontSizeMax = 15;
        }

        // ------------------------------------------------------------------ ciclo
        void Update()
        {
            if (cam == null) return;
            if (UIRoot.I != null && UIRoot.I.HudLayer != null) { var hg = UIRoot.I.HudLayer.GetComponent<CanvasGroup>(); if (hg != null) hg.alpha = 0f; }
            if (UIRoot.I != null && UIRoot.I.Pause != null && UIRoot.I.Pause.IsOpen) UIRoot.I.Pause.Hide();
            float t = Time.time;
            if (tipo == BozzaTipo.MenuA)
            {
                // carrellata lenta in avanti con leggero sbandamento
                float k = Mathf.PingPong(t * 0.09f, 3.2f);
                var fwd = camRot * Vector3.forward; fwd.y = 0f; fwd.Normalize();
                cam.transform.position = camBase + fwd * k + (camRot * Vector3.right) * (Mathf.Sin(t * 0.17f) * 0.18f);
                cam.transform.rotation = camRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.11f) * 0.8f, 0f);
            }
            else if (tipo == BozzaTipo.MenuB)
            {
                cam.transform.position = camBase + new Vector3(0f, Mathf.Sin(t * 0.7f) * 0.008f, 0f);
            }
            else if (oggettoGira != null)
            {
                var m = Mouse.current;
                if (m != null && m.leftButton.isPressed) oggettoYaw -= m.delta.ReadValue().x * 0.4f;
                else oggettoYaw += Time.deltaTime * 14f;
                oggettoGira.localRotation = Quaternion.Euler(tipo == BozzaTipo.InventarioA ? -10f : 0f, oggettoYaw, 0f);
            }
            if (flamma != null) flamma.intensity = 1.6f + Mathf.PerlinNoise(t * 6f, 3.1f) * 0.5f;

            var kb = Keyboard.current;
            if (kb != null && (tipo == BozzaTipo.MenuA || tipo == BozzaTipo.MenuB))
            {
                bool giu = tipo == BozzaTipo.MenuA ? (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) : (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
                bool su = tipo == BozzaTipo.MenuA ? (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) : (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
                if (giu) { Seleziona(sel + 1); UISfx.Play(Sfx.ClickPaper, 0.4f); }
                if (su) { Seleziona(sel - 1); UISfx.Play(Sfx.ClickPaper, 0.4f); }
                if (kb.enterKey.wasPressedThisFrame && T.voci[sel].id == "impostazioni" && UIRoot.I != null && UIRoot.I.Settings != null) UIRoot.I.Settings.Show();
            }
        }

        // ------------------------------------------------------------------ screenshot 1280x720 JPG
        IEnumerator Cattura()
        {
            yield return new WaitForSeconds(capturaDopo);
            if (tipo == BozzaTipo.MenuA || tipo == BozzaTipo.MenuB) { }
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var rt = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(shot, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var outTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            outTex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            outTex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/richieste/media/R023/bozze"));
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, capturaNome + ".jpg"), outTex.EncodeToJPG(88));
            Debug.Log("[R023] Screenshot salvato: " + capturaNome + ".jpg (schermo " + shot.width + "x" + shot.height + ")");
            Destroy(shot); Destroy(outTex);
            Fatto = true;
        }
    }
}
