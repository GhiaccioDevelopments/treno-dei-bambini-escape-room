using TrenoBambini.Core;
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
    ///  - mouse: SOLO tenendo premuto il tasto DESTRO (il sinistro resta all'interazione con il click, vedi PlayerInteractor);
    ///    durante il trascinamento il cursore è bloccato e nascosto, al rilascio torna libero;
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
        bool dragging;

        void Awake()
        {
            if (vcam == null) vcam = GetComponent<CinemachineCamera>();
            if (orbit == null) orbit = GetComponent<CinemachineOrbitalFollow>();
        }

        void Start()
        {
            Snap();
#if UNITY_WEBGL && !UNITY_EDITOR
            TrenoDisableContextMenu(); // niente menu contestuale del browser sul tasto destro
#endif
        }

        void OnDisable() { SetDragging(false); MoveYaw = float.NaN; }

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
            MoveYaw = orbit.HorizontalAxis.Value;
            bool blocked = GameManager.I != null && GameManager.I.InputBlocked;
            if (blocked) { camTouch = -1; pendingTouch = -1; SetDragging(false); return; }

            float sens = CameraSettings.Sensibilita;
            float ySign = CameraSettings.InvertiY ? -1f : 1f;
            float dYaw = 0f, dPitch = 0f; // gradi; dPitch > 0 = guarda in su

            var mouse = Mouse.current;
            bool rmb = mouse != null && mouse.rightButton.isPressed;
            SetDragging(rmb);
            if (rmb)
            {
                Vector2 d = mouse.delta.ReadValue();
                dYaw += d.x * mouseDegPerPixel * sens;
                dPitch += d.y * mouseDegPerPixel * sens * ySign;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 s = gp.rightStick.ReadValue();
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

        void SetDragging(bool on)
        {
            if (dragging == on) return;
            dragging = on;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: nessun blocco del cursore (pointer lock fragile nel browser), si usa solo il delta del mouse
            return;
#else
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
#endif
        }
    }
}
