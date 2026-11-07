using TrenoBambini.Core;
using TrenoBambini.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace TrenoBambini.Player
{
    /// <summary>
    /// Pilota la CinemachineCamera in terza persona (CinemachineOrbitalFollow a 3 anelli).
    /// Input di rotazione:
    ///  - mouse (R005): con il cursore bloccato la visuale segue il mouse senza clic (CursorManager decide quando bloccarlo);
    ///    in alternativa, o se nelle impostazioni si sceglie il "tasto destro", trascinando con il tasto DESTRO premuto;
    ///  - gamepad: stick destro;
    ///  - touch: trascinamento in qualsiasi punto della metà destra dello schermo non coperto dalla UI.
    /// Con input bloccato (dialoghi, pausa, inventario, enigmi) la camera non ruota e il cursore è libero.
    /// Sensibilità e inversione Y sono in CameraSettings (PlayerPrefs).
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public CinemachineCamera vcam;
        public CinemachineOrbitalFollow orbit;
        public Transform target;
        [Tooltip("Rotazione extra (gradi) della camera rispetto al player quando fa Snap (es. basso: 180 = camera in diagonale dal lato nord-ovest, la porta d'uscita si vede).")]
        public float snapYawOffset;

        [Header("Mouse / touch (gradi per pixel)")]
        public float mouseDegPerPixel = 0.12f;
        public float touchDegPerPixel = 0.2f;
        [Header("Gamepad (gradi al secondo)")]
        public float padYawSpeed = 140f;
        public float padPitchSpeed = 70f;
        [Tooltip("Gradi di inclinazione corrispondenti a 1 unità dell'asse verticale a 3 anelli (-1..1).")]
        public float degreesPerPitchUnit = 35f;

        /// <summary>
        /// Direzione "avanti" del movimento (gradi di yaw): è l'asse orbitale della camera, senza la rotazione extra
        /// del composer (spalla). Float.NaN se non c'è nessuna camera attiva.
        /// </summary>
        public static float MoveYaw { get; private set; } = float.NaN;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void TrenoDisableContextMenu();
#endif

        int camTouch = -1;
        int pendingTouch = -1;
        int pendingFrame;

        // R018: profili per stanza (CameraProfileVolume) e piani di taglio coerenti con la nebbia
        public const float NearClip = 0.12f;
        Unity.Cinemachine.Cinemachine3OrbitRig.Settings orbiteBase;
        float fovBase;
        Vector2 rangeBase;
        float scalaCorr = 1f, scalaVel, fovCorr, fovVel, pMinCorr, pMinVel, pMaxCorr, pMaxVel;
        bool profiloInit;

        void Awake()
        {
            if (vcam == null) vcam = GetComponent<CinemachineCamera>();
            if (orbit == null) orbit = GetComponent<CinemachineOrbitalFollow>();
        }

        void Start()
        {
            Snap();
            ImpostaPianiDiTaglio();
#if UNITY_WEBGL && !UNITY_EDITOR
            TrenoDisableContextMenu(); // niente menu contestuale del browser sul tasto destro
#endif
        }

        void OnDisable() { MoveYaw = float.NaN; }

        /// <summary>
        /// Near fisso a 0,12 m. Far = distanza a cui la nebbia esponenziale copre ormai il 95% (3 / densita'), tra 40 e 150 m:
        /// oltre non si vede nulla, quindi non si spreca precisione di profondita' (meno z-fighting a distanza).
        /// </summary>
        void ImpostaPianiDiTaglio()
        {
            if (vcam == null) return;
            var l = vcam.Lens;
            l.NearClipPlane = NearClip;
            float far = 150f;
            if (RenderSettings.fog && RenderSettings.fogMode != FogMode.Linear && RenderSettings.fogDensity > 0.0001f)
                far = Mathf.Clamp(3f / RenderSettings.fogDensity, 40f, 150f);
            l.FarClipPlane = far;
            vcam.Lens = l;
        }

        void CatturaProfiloBase()
        {
            orbiteBase = orbit.Orbits;
            fovBase = vcam != null ? vcam.Lens.FieldOfView : 52f;
            rangeBase = orbit.VerticalAxis.Range;
            fovCorr = fovBase; scalaCorr = 1f; pMinCorr = rangeBase.x; pMaxCorr = rangeBase.y;
            profiloInit = true;
        }

        /// <summary>Transizione morbida verso il profilo del volume che contiene il giocatore (o verso quello della scena).</summary>
        void AggiornaProfilo()
        {
            if (!profiloInit) CatturaProfiloBase();
            var tr = target != null ? target : transform;
            var vol = CameraProfileVolume.Trova(tr.position);
            float scalaT = vol != null ? vol.scalaOrbita : 1f;
            float fovT = vol != null && vol.fov > 0f ? vol.fov : fovBase;
            float minT = vol != null ? Mathf.Max(vol.pitchMin, rangeBase.x) : rangeBase.x;
            float maxT = vol != null ? Mathf.Min(vol.pitchMax, rangeBase.y) : rangeBase.y;
            float dt = Time.unscaledDeltaTime;
            const float t = 0.5f;
            float s0 = scalaCorr, f0 = fovCorr, a0 = pMinCorr, b0 = pMaxCorr;
            scalaCorr = Mathf.SmoothDamp(scalaCorr, scalaT, ref scalaVel, t, Mathf.Infinity, dt);
            fovCorr = Mathf.SmoothDamp(fovCorr, fovT, ref fovVel, t, Mathf.Infinity, dt);
            pMinCorr = Mathf.SmoothDamp(pMinCorr, minT, ref pMinVel, t, Mathf.Infinity, dt);
            pMaxCorr = Mathf.SmoothDamp(pMaxCorr, maxT, ref pMaxVel, t, Mathf.Infinity, dt);
            if (!Mathf.Approximately(s0, scalaCorr))
            {
                var ob = orbiteBase;
                ob.Top = new Unity.Cinemachine.Cinemachine3OrbitRig.Orbit { Height = ob.Top.Height * scalaCorr, Radius = ob.Top.Radius * scalaCorr };
                ob.Center = new Unity.Cinemachine.Cinemachine3OrbitRig.Orbit { Height = ob.Center.Height * scalaCorr, Radius = ob.Center.Radius * scalaCorr };
                ob.Bottom = new Unity.Cinemachine.Cinemachine3OrbitRig.Orbit { Height = ob.Bottom.Height * scalaCorr, Radius = ob.Bottom.Radius * scalaCorr };
                orbit.Orbits = ob;
            }
            if (vcam != null && !Mathf.Approximately(f0, fovCorr)) { var l = vcam.Lens; l.FieldOfView = fovCorr; vcam.Lens = l; }
            if (!Mathf.Approximately(a0, pMinCorr) || !Mathf.Approximately(b0, pMaxCorr))
            {
                var v = orbit.VerticalAxis;
                v.Range = new Vector2(pMinCorr, pMaxCorr);
                v.Value = Mathf.Clamp(v.Value, v.Range.x, v.Range.y);
                orbit.VerticalAxis = v;
            }
        }

        /// <summary>Riporta la camera dietro al player e azzera lo smorzamento (dopo cambio scena o teletrasporto).</summary>
        public void Snap()
        {
            if (orbit == null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            Transform root = p != null ? p.transform : target;
            if (root == null) return;
            orbit.HorizontalAxis.Value = Mathf.DeltaAngle(0f, root.eulerAngles.y + snapYawOffset);
            orbit.VerticalAxis.Value = orbit.VerticalAxis.Center;
            if (vcam != null) vcam.PreviousStateIsValid = false;
        }

        void Update()
        {
            if (orbit == null) return;
            AggiornaProfilo();
            MoveYaw = orbit.HorizontalAxis.Value;
            bool blocked = GameManager.I != null && GameManager.I.InputBlocked;
            if (blocked) { camTouch = -1; pendingTouch = -1; return; }

            float sens = CameraSettings.Sensibilita;
            float ySign = CameraSettings.InvertiY ? -1f : 1f;
            float dYaw = 0f, dPitch = 0f; // gradi; dPitch > 0 = guarda in su

            var mouse = Mouse.current;
            bool rmb = mouse != null && mouse.rightButton.isPressed && !UIRoot.TouchMode;
            if (mouse != null && (rmb || CursorManager.MouseLookActive))
            {
                Vector2 d = mouse.delta.ReadValue();
                if (float.IsNaN(d.x) || float.IsNaN(d.y)) d = Vector2.zero;
                dYaw += d.x * mouseDegPerPixel * sens;
                dPitch += d.y * mouseDegPerPixel * sens * ySign;
            }

            var gp = InputHelper.ActiveGamepad;
            if (gp != null)
            {
                Vector2 s = InputHelper.SafeStick(gp.rightStick.ReadValue());
                if (s.sqrMagnitude > 0.04f)
                {
                    dYaw += s.x * padYawSpeed * sens * Time.unscaledDeltaTime;
                    dPitch += s.y * padPitchSpeed * sens * ySign * Time.unscaledDeltaTime;
                }
            }

            var ts = Touchscreen.current;
            if (pendingTouch != -1 && Time.frameCount > pendingFrame + 2) pendingTouch = -1; // tocco finito prima di essere deciso
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    int id = t.touchId.ReadValue();
                    // il tocco si decide al frame DOPO la pressione: l'EventSystem ha già deciso se è sopra un pulsante UI
                    if (t.press.wasPressedThisFrame && camTouch == -1 && pendingTouch == -1)
                    {
                        pendingTouch = id;
                        pendingFrame = Time.frameCount;
                    }
                    if (id == pendingTouch && Time.frameCount > pendingFrame)
                    {
                        pendingTouch = -1;
                        if (t.isInProgress)
                        {
                            Vector2 pos = t.position.ReadValue();
                            var es = EventSystem.current;
                            bool overUi = es != null && (es.IsPointerOverGameObject(id) || es.IsPointerOverGameObject());
                            if (pos.x > Screen.width * 0.5f && !overUi) camTouch = id;
                        }
                    }
                    if (id == camTouch)
                    {
                        if (t.isInProgress)
                        {
                            Vector2 d = t.delta.ReadValue();
                            dYaw += d.x * touchDegPerPixel * sens;
                            dPitch += d.y * touchDegPerPixel * sens * ySign;
                        }
                        else camTouch = -1;
                    }
                }
            }

            if (dYaw != 0f || dPitch != 0f)
            {
                // angolo positivo = lo sguardo gira verso destra (mouse a destra)
                orbit.HorizontalAxis.Value = Mathf.DeltaAngle(0f, orbit.HorizontalAxis.Value + dYaw);
                var v = orbit.VerticalAxis;
                // asse verticale: -1 anello basso (camera bassa, guarda in su), +1 anello alto (guarda dall'alto)
                v.Value = Mathf.Clamp(v.Value - dPitch / degreesPerPitchUnit, v.Range.x, v.Range.y);
                orbit.VerticalAxis = v;
            }
        }
    }
}
