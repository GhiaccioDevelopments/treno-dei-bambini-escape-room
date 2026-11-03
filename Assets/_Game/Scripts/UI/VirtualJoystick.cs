using UnityEngine;
using UnityEngine.EventSystems;

namespace TrenoBambini.UI
{
    /// <summary>Joystick virtuale per touch/LIM. Il valore è letto da ThirdPersonController.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static Vector2 Value { get; private set; }

        public RectTransform background;
        public RectTransform knob;
        public float radius = 90f;

        public void OnPointerDown(PointerEventData e) { OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            if (background == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out local)) return;
            Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
            Value = v;
            if (knob != null) knob.anchoredPosition = v * radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }
    }
}
