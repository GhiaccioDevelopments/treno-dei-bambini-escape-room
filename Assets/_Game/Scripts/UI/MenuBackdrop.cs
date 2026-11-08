using System.Collections;
using System.Collections.Generic;
using TrenoBambini.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R023: sfondo vivo del menu iniziale = il vicolo dell'hub all'imbrunire (la scena 10_Hub_Vicolo, caricata in additivo, senza giocatore).
    /// Luce calda e bassa, nebbia, finestre accese che tremolano, panni stesi che ondeggiano, fumo lontano di una locomotiva,
    /// carrellata lenta con una CinemachineCamera, pellicola di R007 (passo a schermo intero, intensita' ridotta).
    /// Nessun bake: luci e volume si creano a runtime e spariscono con la scena. Su WebGL e a grafica Bassa non si carica la scena:
    /// si mostra un fotogramma fisso (Resources/Menu/sfondo_vicolo) con un lentissimo zoom.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour
    {
        const string SceneHub = "10_Hub_Vicolo";
        static readonly Vector3 PosPartenza = new Vector3(0.2f, 0f, -20f);
        static readonly Vector3 PuntoGuardato = new Vector3(-4.2f, 2.6f, 6f);

        public bool Ready { get; private set; }
        public bool Live { get; private set; }

        Camera titleCam, hubCam;
        CinemachineCamera vcam;
        Vector3 basePos;
        Quaternion baseRot;
        readonly List<Transform> panni = new List<Transform>();
        readonly List<Quaternion> panniRot = new List<Quaternion>();
        readonly List<Light> luci = new List<Light>();
        RectTransform still;
        bool fermo;

        /// <summary>Vero se la scena dell'hub e' troppo pesante: WebGL o preset Bassa.</summary>
        public static bool Leggero { get { return GraphicsPresets.Web || GraphicsPresets.Current == (int)GraphicsPreset.Bassa; } }

        void OnDestroy()
        {
            GameManager.MenuBackdrop = false;
            if (titleCam != null) titleCam.enabled = true;
        }

        public IEnumerator Run(RectTransform stillParent)
        {
            fermo = GraphicsPresets.ReduceMotion || Fx.UITheme.ReduceEffects;
            titleCam = Camera.main;
            // il profilo di post-processing della scena del titolo scurirebbe i bordi di grigio sopra al vicolo: si spegne (il look lo da' il Volume del vicolo)
            // il vecchio diorama della scena del titolo (case, luce direzionale, probe, Volume) si spegne: la scena e' solo camera + schermata
            foreach (var go in gameObject.scene.GetRootGameObjects())
                if (go != gameObject && go.GetComponent<Camera>() == null) go.SetActive(false);
            if (!Leggero && Application.CanStreamedLevelBeLoaded(SceneHub))
            {
                GameManager.MenuBackdrop = true;
                var op = SceneManager.LoadSceneAsync(SceneHub, LoadSceneMode.Additive);
                while (op != null && !op.isDone) yield return null;
                yield return null;
                var hub = SceneManager.GetSceneByName(SceneHub);
                if (hub.IsValid() && hub.isLoaded)
                {
                    SceneManager.SetActiveScene(hub);
                    Isola(hub);
                    Imbrunire();
                    Live = true;
                }
            }
            if (!Live) CostruisciFermo(stillParent);
            Ready = true;
        }

        // ------------------------------------------------------------------ sfondo vivo
        void Isola(Scene hub)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.gameObject.scene != hub) continue;
                if (mb.GetType().Namespace == "TrenoBambini.Player") mb.enabled = false;
            }
            foreach (var p in FindObjectsByType<Player.ThirdPersonController>(FindObjectsInactive.Include))
                if (p.gameObject.scene == hub) p.gameObject.SetActive(false);

            // le pozzanghere a quest'ora riflettono il buio e sembrano buchi neri: nel menu si tolgono
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.gameObject.scene == hub && (t.name == "Pozzanghera" || t.name == "Pozzanghere")) t.gameObject.SetActive(false);

            foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (c.gameObject.scene == hub && c.CompareTag("MainCamera")) { hubCam = c; break; }
            if (hubCam == null) return;
            var al = hubCam.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
            if (titleCam != null) titleCam.enabled = false;
            var brain = hubCam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = hubCam.gameObject.AddComponent<CinemachineBrain>();
            brain.enabled = true;

            // posizione di partenza: occhi a 1,7 m sopra il selciato
            var p0 = PosPartenza;
            p0.y = Suolo(p0) + 1.7f;
            basePos = p0;
            baseRot = Quaternion.LookRotation(PuntoGuardato - p0);

            var vgo = new GameObject("VcamMenu");
            vgo.transform.SetPositionAndRotation(basePos, baseRot);
            vcam = vgo.AddComponent<CinemachineCamera>();
            vcam.Priority = 1000;
            var lens = vcam.Lens;
            lens.FieldOfView = 58f;
            vcam.Lens = lens;
            hubCam.transform.SetPositionAndRotation(basePos, baseRot);

            // panni stesi
            var amb = GameObject.Find("Ambiente");
            if (amb != null)
                foreach (Transform t in amb.transform)
                    if (t.name.StartsWith("Panno_") && !t.name.Contains("_LOD") && t.gameObject.scene == hub) { panni.Add(t); panniRot.Add(t.localRotation); }
        }

        static float Suolo(Vector3 p)
        {
            Physics.SyncTransforms();
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 100f))
                if (h.collider.name.StartsWith("Selciato_")) return h.point.y;
            return 0f;
        }

        void Imbrunire()
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null)
            {
                sun.color = new Color(1f, 0.55f, 0.32f);
                sun.intensity *= 0.5f;
                sun.transform.rotation = Quaternion.Euler(7f, 62f, 0f);
            }
            RenderSettings.ambientIntensity = 0.4f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.30f, 0.20f, 0.15f);
            RenderSettings.fogDensity = 0.013f;

            var vgo = new GameObject("VolumeMenu");
            var vol = vgo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 90;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = prof.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.1f);
            ca.saturation.Override(-14f);
            ca.contrast.Override(22f);
            ca.colorFilter.Override(new Color(1f, 0.86f, 0.70f));
            var vg = prof.Add<Vignette>(true);
            vg.intensity.Override(0.36f); vg.smoothness.Override(0.65f);
            vol.sharedProfile = prof;

            // finestre accese: poche luci puntiformi, le prime due con sfarfallio di lampada a petrolio
            int n = GraphicsPresets.Current >= (int)GraphicsPreset.Alta ? 5 : 3;
            var fwd = (PuntoGuardato - basePos); fwd.y = 0f; fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            float[] d = { 6f, 11f, 17f, 24f, 32f };
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("FinestraAccesa_" + i);
                go.transform.position = basePos + fwd * d[i] + right * ((i % 2 == 0) ? -3.4f : 3.4f) + Vector3.up * (-0.1f + (i % 3) * 0.9f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point; l.range = 9f; l.intensity = 40f;
                l.color = new Color(1f, 0.62f, 0.28f); l.shadows = LightShadows.None;
                luci.Add(l);
            }
            CostruisciFumo(fwd);
        }

        void CostruisciFumo(Vector3 fwd)
        {
            var mat = Resources.Load<Material>("Menu/M_FumoMenu");
            if (mat == null) return;
            mat.SetTexture("_BaseMap", Fx.UIArt.Smoke().texture);
            var go = new GameObject("FumoLontano");
            go.transform.position = basePos + fwd * 70f + Vector3.up * 6f + Vector3.Cross(Vector3.up, fwd) * 12f;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 14f; main.startSpeed = 0.9f; main.startSize = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startColor = new Color(0.55f, 0.46f, 0.40f, 0.30f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 2.5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 1.5f;
            go.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 1.2f; vel.y = 0f; vel.z = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = mat;
            rend.sortingFudge = 10f;
            if (!fermo) ps.Simulate(14f, true, true); // parte gia' con il pennacchio formato
            ps.Play();
        }

        void Update()
        {
            if (!Live || vcam == null) return;
            float t = Time.unscaledTime;
            if (!fermo)
            {
                // carrellata lenta in avanti e ritorno, con leggero sbandamento
                float k = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t * 0.04f, 1f)) * 3.4f;
                var f = baseRot * Vector3.forward; f.y = 0f; f.Normalize();
                vcam.transform.position = basePos + f * k + (baseRot * Vector3.right) * (Mathf.Sin(t * 0.17f) * 0.18f);
                vcam.transform.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.11f) * 0.8f, 0f);
                for (int i = 0; i < panni.Count; i++)
                {
                    if (panni[i] == null) continue;
                    float a = Mathf.Sin(t * 0.9f + i * 1.7f) * 2.4f + Mathf.Sin(t * 2.3f + i) * 0.8f;
                    panni[i].localRotation = panniRot[i] * Quaternion.Euler(0f, 0f, a);
                }
            }
            for (int i = 0; i < luci.Count && i < 2; i++)
                if (luci[i] != null) luci[i].intensity = 40f + (Mathf.PerlinNoise(t * 5f + i * 9f, 3.1f) - 0.5f) * 16f;
        }

        // ------------------------------------------------------------------ sfondo fermo (WebGL / Bassa)
        void CostruisciFermo(RectTransform parent)
        {
            var go = new GameObject("SfondoFermo", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tex = Resources.Load<Texture2D>("Menu/sfondo_vicolo");
            var raw = go.AddComponent<RawImage>();
            raw.raycastTarget = false;
            if (tex != null) raw.texture = tex; else raw.color = new Color(0.1f, 0.06f, 0.04f, 1f);
            // riempie lo schermo mantenendo le proporzioni
            var fit = go.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = tex != null ? tex.width / (float)tex.height : 16f / 9f;
            still = rt;
            StartCoroutine(ZoomLento());
        }

        IEnumerator ZoomLento()
        {
            while (still != null)
            {
                if (!fermo)
                {
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(Time.unscaledTime * 0.03f, 1f));
                    still.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.08f, k);
                }
                yield return null;
            }
        }

#if UNITY_EDITOR
        /// <summary>Solo editor: salva il fotogramma dello sfondo (senza UI) per la versione leggera. Vedi Treno > R023 > Cattura sfondo fermo.</summary>
        public IEnumerator CatturaFermo(string path)
        {
            var layer = UIRoot.I != null ? UIRoot.I.ScreenLayer : null;
            if (layer != null) layer.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var rtex = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(shot, rtex);
            var prev = RenderTexture.active;
            RenderTexture.active = rtex;
            var outTex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            outTex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            outTex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rtex);
            System.IO.File.WriteAllBytes(path, outTex.EncodeToJPG(82));
            Destroy(shot); Destroy(outTex);
            if (layer != null) layer.gameObject.SetActive(true);
            Debug.Log("[R023] Sfondo fermo salvato: " + path);
        }
#endif
    }
}
