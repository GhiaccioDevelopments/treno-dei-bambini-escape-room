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
            bool pressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (!pressed) return false;
            var es = EventSystem.current;
            return !(es != null && es.IsPointerOverGameObject());
        }
    }
}
