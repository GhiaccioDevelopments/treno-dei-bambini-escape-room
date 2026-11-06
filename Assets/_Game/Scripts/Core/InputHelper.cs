using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TrenoBambini.Core
{
    /// <summary>Piccole utilità per l'Input System condivise tra i moduli.</summary>
    public static class InputHelper
    {
        /// <summary>E / Spazio / Invio (e opzionalmente click o tap fuori dalla UI).</summary>
        public static bool ConfirmPressed(bool includePointer)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame
                || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) return true;
            return includePointer && PointerPressedOutsideUI();
        }

        public static bool PointerPressedOutsideUI()
        {
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !CursorManager.SwallowPointer;
            bool pressed = mouse
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (!pressed) return false;
            var es = EventSystem.current;
            return !(es != null && es.IsPointerOverGameObject());
        }

        // ---------------------------------------------------------------- gamepad "vero"
        static Gamepad trusted;
        static Gamepad watched;
        static Vector2 restL, restR;
        static int lastFrame = -1;

        /// <summary>
        /// Il gamepad corrente solo se è stato davvero usato (un tasto premuto o una leva mossa rispetto alla posizione di riposo
        /// letta all'inizio). Nei browser compaiono gamepad "fantasma" o con assi fermi a fondo scala (volanti, pedaliere,
        /// driver virtuali): finché non si usano non devono muovere Amerigo né la camera.
        /// </summary>
        public static Gamepad ActiveGamepad
        {
            get
            {
                var gp = Gamepad.current;
                if (gp == null) { trusted = null; watched = null; return null; }
                if (gp == trusted) return gp;
                if (Time.frameCount == lastFrame && gp == watched) return null;
                lastFrame = Time.frameCount;
                Vector2 l = SafeStick(gp.leftStick.ReadValue()), r = SafeStick(gp.rightStick.ReadValue());
                if (gp != watched) { watched = gp; restL = l; restR = r; return null; }
                bool moved = (l - restL).magnitude > 0.4f || (r - restR).magnitude > 0.4f;
                bool button = gp.buttonSouth.isPressed || gp.buttonEast.isPressed || gp.buttonWest.isPressed || gp.buttonNorth.isPressed
                    || gp.startButton.isPressed || gp.selectButton.isPressed || gp.dpad.ReadValue().sqrMagnitude > 0.25f
                    || gp.leftShoulder.isPressed || gp.rightShoulder.isPressed;
                if (moved || button) { trusted = gp; return gp; }
                return null;
            }
        }

        /// <summary>Valore di una leva senza NaN/infiniti (alcuni browser li riportano per assi non collegati).</summary>
        public static Vector2 SafeStick(Vector2 v)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y)) return Vector2.zero;
            return Vector2.ClampMagnitude(v, 1f);
        }
    }
}
