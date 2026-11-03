using TrenoBambini.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TrenoBambini.Player
{
    /// <summary>Camera in terza persona: orbita con tasto destro (mouse) o trascinamento nella metà destra (touch).</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float distance = 4.5f;
        public float height = 2.2f;      // altezza iniziale della camera rispetto ai piedi
        public float lookHeight = 1.1f;  // punto guardato sul player
        public float smooth = 10f;
        public float mouseSensitivity = 0.15f;
        public float touchSensitivity = 0.2f;
        public float minPitch = -5f;
        public float maxPitch = 65f;

        float yaw, pitch;
        Vector3 pivot;
        int camTouch = -1;
        float retargetTimer;
        bool initialized;

        void Start()
        {
            if (target == null) FindTarget();
            InitAngles();
            Snap();
        }

        void FindTarget()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        void InitAngles()
        {
            if (initialized) return;
            initialized = true;
            yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            pitch = Mathf.Asin(Mathf.Clamp((height - lookHeight) / Mathf.Max(0.1f, distance), -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Porta subito la camera dietro al target (dopo un teletrasporto).</summary>
        public void Snap()
        {
            if (target == null) FindTarget();
            if (target == null) return;
            InitAngles();
            yaw = target.eulerAngles.y;
            Apply(true);
        }

        void LateUpdate()
        {
            if (target == null)
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f) { retargetTimer = 0.5f; FindTarget(); }
                return;
            }
            InitAngles();
            HandleInput();
            Apply(false);
        }

        void HandleInput()
        {
            if (GameManager.I != null && GameManager.I.InputBlocked) { camTouch = -1; return; }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * mouseSensitivity;
                pitch -= d.y * mouseSensitivity;
            }

            var ts = Touchscreen.current;
            if (ts != null)
            {
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var t = touches[i];
                    int id = t.touchId.ReadValue();
                    if (t.press.wasPressedThisFrame && camTouch == -1)
                    {
                        Vector2 p = t.position.ReadValue();
                        var es = EventSystem.current;
                        bool overUi = es != null && es.IsPointerOverGameObject(id);
                        if (p.x > Screen.width * 0.5f && !overUi) camTouch = id;
                    }
                    if (id == camTouch)
                    {
                        if (t.isInProgress)
                        {
                            Vector2 d = t.delta.ReadValue();
                            yaw += d.x * touchSensitivity;
                            pitch -= d.y * touchSensitivity;
                        }
                        else camTouch = -1;
                    }
                }
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        void Apply(bool snap)
        {
            Vector3 pivotTarget = target.position + Vector3.up * lookHeight;
            pivot = snap ? pivotTarget : Vector3.Lerp(pivot, pivotTarget, 1f - Mathf.Exp(-smooth * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;
            // Collisione: si ignorano il player e i muri "Muro*" (che diventano trasparenti, vedi sotto)
            float d = distance;
            int n = Physics.SphereCastNonAlloc(pivot, 0.25f, dir, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c.transform.IsChildOf(target) || IsSeeThrough(c)) continue;
                if (hits[i].distance > 0f) d = Mathf.Min(d, Mathf.Max(0.6f, hits[i].distance));
            }

            transform.SetPositionAndRotation(pivot + dir * d, rot);
            UpdateSeeThrough(dir, d);
        }

        // ---- Muri che si nascondono quando stanno tra camera e player (interni piccoli) ----
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly System.Collections.Generic.List<Renderer> hidden = new System.Collections.Generic.List<Renderer>();
        readonly System.Collections.Generic.List<Renderer> nowHidden = new System.Collections.Generic.List<Renderer>();

        static bool IsSeeThrough(Collider c) => c.name.StartsWith("Muro") || c.name.StartsWith("Porta");

        void UpdateSeeThrough(Vector3 dir, float d)
        {
            nowHidden.Clear();
            // in entrambe le direzioni: un raycast non rileva il collider da cui parte
            CollectSeeThrough(Physics.RaycastNonAlloc(pivot, dir, hits, d + 0.5f, ~0, QueryTriggerInteraction.Ignore));
            Vector3 camPos = pivot + dir * (d + 0.5f);
            CollectSeeThrough(Physics.RaycastNonAlloc(camPos, -dir, hits, d + 0.5f, ~0, QueryTriggerInteraction.Ignore));
            for (int i = 0; i < hidden.Count; i++)
                if (hidden[i] != null && !nowHidden.Contains(hidden[i]))
                    hidden[i].enabled = true;
            hidden.Clear();
            hidden.AddRange(nowHidden);
        }

        void CollectSeeThrough(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (!IsSeeThrough(c)) continue;
                c.GetComponentsInChildren(rendBuf);
                for (int k = 0; k < rendBuf.Count; k++)
                {
                    var r = rendBuf[k];
                    r.enabled = false;
                    if (!nowHidden.Contains(r)) nowHidden.Add(r);
                }
            }
        }

        readonly System.Collections.Generic.List<Renderer> rendBuf = new System.Collections.Generic.List<Renderer>();
    }
}
