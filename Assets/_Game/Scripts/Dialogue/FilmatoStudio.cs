using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TrenoBambini.Dialogue
{
    /// <summary>
    /// R025: lo "studio" degli inserti del filmato iniziale. Come la vista ravvicinata degli enigmi (EsaminaView): un palco buio in alto, lontano da
    /// ogni scena, con i pezzi costruiti dal JSON (forme, materiali, modelli veri di Resources/Modelli/Enigmi), due luci proprie e una camera che
    /// gira piano attorno. Esiste solo mentre l'inserto e' in onda; la camera principale resta spenta nel frattempo. Nessun asset in piu'.
    /// </summary>
    public class FilmatoStudio
    {
        static readonly Vector3 StagePos = new Vector3(0f, 3300f, 0f);

        Transform stage, pivot;
        Camera cam, mainCam;
        FilmatoInserto ins;
        Vector3 centro;
        float dist, yaw0;
        float inizio;

        public bool Aperto { get { return stage != null; } }
        public Camera Camera { get { return cam; } }

        public void Apri(FilmatoInserto inserto, Camera principale)
        {
            Chiudi();
            ins = inserto;
            mainCam = principale;
            if (mainCam != null) mainCam.enabled = false;

            stage = new GameObject("FilmatoStudio").transform;
            stage.position = StagePos;
            pivot = new GameObject("Pezzi").transform;
            pivot.SetParent(stage, false);
            var def = new EnigmaDef { parti = ins.parti };
            MondoFab.Oggetto(def, pivot, false);

            bool lampada = ins.luce == "lampada";
            var lk = new GameObject("Luce");
            lk.transform.SetParent(stage, false);
            lk.transform.localPosition = new Vector3(-1.2f, 1.6f, -1.8f);
            lk.transform.localRotation = Quaternion.Euler(38f, 32f, 0f);
            var l = lk.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = lampada ? new Color(1f, 0.72f, 0.42f) : new Color(1f, 0.86f, 0.64f);
            l.intensity = lampada ? 1.25f : 1.0f;
            l.shadows = LightShadows.None;
            var lf = new GameObject("Riempimento");
            lf.transform.SetParent(stage, false);
            lf.transform.localPosition = new Vector3(1.0f, 0.4f, -1.6f);
            var pl = lf.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(0.8f, 0.68f, 0.52f);
            pl.intensity = 0.9f; pl.range = 6f; pl.shadows = LightShadows.None;

            var cg = new GameObject("StudioCam");
            cg.transform.SetParent(stage, false);
            cam = cg.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.045f, 0.035f, 0.028f);
            cam.fieldOfView = ins.fov > 1f ? ins.fov : 32f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 30f;
            cam.depth = (mainCam != null ? mainCam.depth : 0f) + 5f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var b = MondoFab.Limiti(pivot);
            centro = b.center;
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float asp = Mathf.Max(1.2f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float dv = b.extents.y / tanV, dh = b.extents.x / (tanV * asp);
            dist = (Mathf.Max(dv * 1.5f, dh * 1.3f, 0.35f) + b.extents.z) * Mathf.Max(0.2f, ins.distanza);
            yaw0 = -18f;
            inizio = Time.unscaledTime;
            Aggiorna();
        }

        /// <summary>Un passo dell'orbita lenta attorno ai pezzi (da chiamare ogni frame finche' l'inserto e' in onda).</summary>
        public void Aggiorna()
        {
            if (cam == null) return;
            float t = Time.unscaledTime - inizio;
            var rot = Quaternion.Euler(ins.inclina, yaw0 + ins.giraAlSecondo * t, 0f);
            cam.transform.rotation = rot;
            cam.transform.position = centro + rot * new Vector3(0f, 0f, -dist) + Vector3.up * ins.alto * 0.2f;
        }

        public void Chiudi()
        {
            if (stage != null) Object.Destroy(stage.gameObject);
            stage = null; pivot = null; cam = null;
            if (mainCam != null) mainCam.enabled = true;
            mainCam = null;
        }
    }
}
