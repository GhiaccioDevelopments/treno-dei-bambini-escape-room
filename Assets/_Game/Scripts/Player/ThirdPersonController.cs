using TrenoBambini.Core;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrenoBambini.Player
{
    /// <summary>
    /// Movimento in terza persona relativo alla camera per Amerigo (bambino 1.2 m o adulto 1.75 m: capsula e velocità
    /// sono impostate dal costruttore di scena).
    /// Camminata di default, corsa leggera con Shift (tastiera) o stick/joystick premuto a fondo (oltre 0.9).
    /// Accelerazione e decelerazione morbide, rotazione verso la direzione di movimento, gravità, gradini e pendenze
    /// (il corpo resta aderente al suolo in discesa). Nessun salto.
    /// Aggancio per l'Animator (sotto-fase 2.5): parametro Speed (0 = fermo, WalkBlendThreshold = camminata, 1 = corsa), MotionSpeed (moltiplicatore
    /// di riproduzione, default 1) e Grounded; l'Animator viene cercato nel figlio "Modello" (assente = nessun errore).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        public const string ModelloName = "Modello";
        /// <summary>Valore di Speed (blend tree) corrispondente alla camminata; letto anche dal builder dell'AnimatorController.</summary>
        public const float WalkBlendThreshold = 0.475f;

        [Header("Velocità (m/s)")]
        public float walkSpeed = 1.9f;
        public float runSpeed = 4.0f;
        [Tooltip("Velocità di avanzamento incorporata nelle clip Mixamo (m/s): serve a sincronizzare i piedi.")]
        public float clipWalkSpeed = 1.2f;
        public float clipRunSpeed = 1.9f;
        public float minAnimMultiplier = 0.8f;
        public float maxAnimMultiplier = 2.4f;
        public float acceleration = 7f;
        public float deceleration = 10f;
        [Header("Rotazione e fisica")]
        public float turnSpeed = 10f;
        public float gravity = -20f;
        [Tooltip("Soglia dell'input analogico oltre la quale scatta la corsa.")]
        public float analogRunThreshold = 0.9f;

        [Header("Animator (2.5)")]
        public Animator animator;
        public float dampSpeedParam = 0.1f;

        /// <summary>Ultimo input di movimento letto e stato di blocco (diagnosi F3).</summary>
        public Vector2 DebugInput { get; private set; }
        public bool DebugBlocked { get; private set; }
        public Vector3 DebugVelocity { get { return cc != null ? cc.velocity : Vector3.zero; } }
        public bool DebugControllerEnabled { get { return cc != null && cc.enabled; } }
        public float Speed01 { get; private set; }
        public float MotionSpeed { get; private set; } = 1f;
        public bool Grounded { get; private set; } = true;
        public float CurrentSpeed { get { return new Vector3(velocity.x, 0f, velocity.z).magnitude; } }

        static readonly int HSpeed = Animator.StringToHash("Speed");
        static readonly int HMotionSpeed = Animator.StringToHash("MotionSpeed");
        static readonly int HGrounded = Animator.StringToHash("Grounded");

        CharacterController cc;
        Transform cam;
        Vector3 velocity;   // orizzontale (x,z) + verticale (y)
        float nextAnimSearch;
        bool hasSpeed, hasMotion, hasGrounded, animChecked;

        void Awake() { cc = GetComponent<CharacterController>(); }

        /// <summary>Azzera la velocità (dopo un teletrasporto/cambio scena).</summary>
        public void ResetMotion() { velocity = Vector3.zero; Speed01 = 0f; }

        void Update()
        {
            if (cc == null || !cc.enabled) return;
            float dt = Time.deltaTime;

            Vector2 input = Vector2.zero;
            bool shift = false;
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
                    shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                }
                bool analog = false;
                Vector2 a = VirtualJoystick.Value;
                var gp = Gamepad.current;
                if (gp != null)
                {
                    // zona morta: un gamepad "fantasma" o con deriva (frequente nei browser) non deve annullare WASD
                    Vector2 g = gp.leftStick.ReadValue();
                    if (g.magnitude > 0.25f) a += g;
                }
                if (a.sqrMagnitude > 0.0001f) { input += a; analog = true; }
                input = Vector2.ClampMagnitude(input, 1f);
                // la corsa analogica si decide sulla leva
                if (analog && input.magnitude >= analogRunThreshold) shift = true;
            }

            DebugInput = input; DebugBlocked = blocked;

            if (cam == null)
            {
                var c = Camera.main;
                if (c != null) cam = c.transform;
            }

            Vector3 fwd = Vector3.forward, right = Vector3.right;
            if (!float.IsNaN(CameraRig.MoveYaw))
            {
                // asse della camera orbitale: l'inquadratura "sopra la spalla" non deve deviare il cammino
                var q = Quaternion.Euler(0f, CameraRig.MoveYaw, 0f);
                fwd = q * Vector3.forward;
                right = q * Vector3.right;
            }
            else if (cam != null)
            {
                fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            }

            Vector3 dir = fwd * input.y + right * input.x;
            float mag = Mathf.Clamp01(dir.magnitude);
            // velocità voluta: corsa con Shift/leva a fondo, altrimenti camminata proporzionale alla leva
            float targetSpeed = mag < 0.01f ? 0f : (shift ? runSpeed : walkSpeed * Mathf.Clamp01(mag / Mathf.Max(0.01f, analogRunThreshold)));
            Vector3 desired = mag < 0.01f ? Vector3.zero : dir.normalized * targetSpeed;

            Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
            if (blocked) { horiz = Vector3.zero; desired = Vector3.zero; } // dialoghi/pausa: fermo subito, resta solo la gravità
            float rate = desired.sqrMagnitude > horiz.sqrMagnitude ? acceleration : deceleration;
            if (!blocked) horiz = Vector3.MoveTowards(horiz, desired, rate * dt);

            // verticale: gravità, con piccola spinta a terra per restare incollato a pendenze e gradini
            Grounded = cc.isGrounded;
            if (Grounded && velocity.y < 0f) velocity.y = -3f;
            velocity.y += gravity * dt;

            Vector3 move = horiz;
            // in pendenza il movimento segue il piano del suolo (niente rimbalzi in discesa)
            if (Grounded && horiz.sqrMagnitude > 0.0001f &&
                Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var gh, 0.8f, ~0, QueryTriggerInteraction.Ignore) &&
                gh.normal.y > 0.5f && gh.normal.y < 0.999f)
            {
                move = Vector3.ProjectOnPlane(horiz, gh.normal);
                move *= horiz.magnitude / Mathf.Max(0.0001f, move.magnitude);
            }
            move.y += velocity.y;
            cc.Move(move * dt);

            velocity = new Vector3(horiz.x, velocity.y, horiz.z);
            // se ho urtato qualcosa la velocità effettiva è minore: la riallineo per non accumulare contro i muri
            Vector3 actual = cc.velocity;
            if (new Vector3(actual.x, 0f, actual.z).sqrMagnitude < horiz.sqrMagnitude * 0.25f)
                velocity = new Vector3(actual.x, velocity.y, actual.z);

            if (!blocked && horiz.sqrMagnitude > 0.04f)
            {
                Quaternion tr = Quaternion.LookRotation(new Vector3(horiz.x, 0f, horiz.z), Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, tr, 1f - Mathf.Exp(-turnSpeed * dt));
            }

            UpdateAnimator(dt);
        }

        void UpdateAnimator(float dt)
        {
            // parametro del blend tree: 0 = fermo, WalkThreshold (0.475) = camminata, 1 = corsa, indipendente dalle velocità di gioco
            float v = CurrentSpeed;
            float w = Mathf.Max(0.01f, walkSpeed), r = Mathf.Max(w + 0.01f, runSpeed);
            const float walkThreshold = WalkBlendThreshold;
            Speed01 = v <= w ? walkThreshold * v / w : Mathf.Lerp(walkThreshold, 1f, Mathf.Clamp01((v - w) / (r - w)));
            // moltiplicatore di riproduzione = velocità di gioco / velocità della clip (mix del blend tree), limitato
            float wRun = v <= w ? 0f : Mathf.Clamp01((v - w) / (r - w));
            float clipV = v <= w ? clipWalkSpeed * v / w : Mathf.Lerp(clipWalkSpeed, clipRunSpeed, wRun);
            float mult = clipV > 0.01f ? Mathf.Clamp(v / clipV, minAnimMultiplier, maxAnimMultiplier) : 1f;
            MotionSpeed = Mathf.Lerp(1f, mult, Mathf.Clamp01(v / 0.3f));
            // ricerca dell'Animator al massimo una volta al secondo; il controller può essere assegnato dopo
            if (animator == null)
            {
                if (Time.unscaledTime < nextAnimSearch) return;
                nextAnimSearch = Time.unscaledTime + 1f;
                var mod = transform.Find(ModelloName);
                if (mod == null) return;
                animator = mod.GetComponentInChildren<Animator>();
                animChecked = false;
                if (animator == null) return;
            }
            if (animator.runtimeAnimatorController == null) { animChecked = false; return; }
            if (!animChecked)
            {
                animChecked = true;
                hasSpeed = hasMotion = hasGrounded = false;
                foreach (var p in animator.parameters)
                {
                    if (p.nameHash == HSpeed) hasSpeed = true;
                    else if (p.nameHash == HMotionSpeed) hasMotion = true;
                    else if (p.nameHash == HGrounded) hasGrounded = true;
                }
            }
            if (hasSpeed) animator.SetFloat(HSpeed, Speed01, dampSpeedParam, dt);
            if (hasMotion) animator.SetFloat(HMotionSpeed, MotionSpeed);
            if (hasGrounded) animator.SetBool(HGrounded, Grounded);
        }
    }
}
