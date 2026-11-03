using TrenoBambini.Core;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrenoBambini.Player
{
    /// <summary>Movimento in terza persona relativo alla camera (WASD/frecce, joystick virtuale, gamepad).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        public float walkSpeed = 3.2f;
        public float runSpeed = 5f;
        public float turnSpeed = 12f;
        public float gravity = -20f;

        CharacterController cc;
        Transform cam;
        float vy;

        void Awake() { cc = GetComponent<CharacterController>(); }

        void Update()
        {
            if (cc == null || !cc.enabled) return;

            Vector2 input = Vector2.zero;
            bool run = false;
            bool blocked = GameManager.I != null && GameManager.I.InputBlocked;
            if (!blocked)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                    run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                }
                input += VirtualJoystick.Value;
                var gp = Gamepad.current;
                if (gp != null) input += gp.leftStick.ReadValue();
                input = Vector2.ClampMagnitude(input, 1f);
            }

            if (cam == null)
            {
                var c = Camera.main;
                if (c != null) cam = c.transform;
            }

            Vector3 fwd = Vector3.forward, right = Vector3.right;
            if (cam != null)
            {
                fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            }

            Vector3 dir = fwd * input.y + right * input.x;
            Vector3 move = dir * (run ? runSpeed : walkSpeed);

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += gravity * Time.deltaTime;
            move.y = vy;
            cc.Move(move * Time.deltaTime);

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }
    }
}
