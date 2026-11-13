using TrenoBambini.Core;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TrenoBambini.Player
{
    /// <summary>Trova l'Interactable più vicino davanti al player, mostra il prompt e gestisce E / click / pulsante.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public static PlayerInteractor Active { get; private set; }

        public float range = 1.6f;
        public float clickRange = 3f;

        public Interactable Current { get; private set; }

        string lastSource;
        string displayed;
        bool lastTouch;
        static readonly RaycastHit[] hits = new RaycastHit[24];

        void OnEnable() { Active = this; }
        void OnDisable()
        {
            if (Active == this) Active = null;
            if (UIRoot.I != null) UIRoot.I.HidePrompt();
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.InputBlocked)
            {
                Clear();
                return;
            }

            Current = FindBest();
            UpdatePrompt();

            if (Time.unscaledTime - gm.LastUnblockTime < 0.2f) return;

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) TriggerInteract();
            else TryClick();
        }

        /// <summary>Usato dal pulsante a schermo "Interagisci".</summary>
        public void TriggerInteract()
        {
            var gm = GameManager.I;
            if (gm == null || gm.InputBlocked || Time.unscaledTime - gm.LastUnblockTime < 0.2f) return;
            if (Current != null && Current.CanInteract)
            {
                if (UIRoot.I != null) UIRoot.I.PromptPressed();
                Current.Interact();
            }
        }

        Interactable FindBest()
        {
            Vector3 pos = transform.position;
            Vector3 fwd = transform.forward;
            Interactable best = null;
            float bestDist = float.MaxValue;
            var list = Interactable.All;
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (it == null || !it.CanInteract) continue;
                Vector3 p = it.ClosestPointTo(pos);
                Vector3 d = p - pos;
                if (Mathf.Abs(d.y) > 2.5f) continue;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > range) continue;
                if (dist > 0.6f && Vector3.Dot(fwd, d / dist) < 0f) continue;
                float score = dist + it.Penalita;
                if (score < bestDist) { bestDist = score; best = it; }
            }
            return best;
        }

        void UpdatePrompt()
        {
            if (UIRoot.I == null) return;
            if (Current == null) { Clear(); return; }
            string src = Current.GetPrompt();
            if (src != lastSource || lastTouch != UIRoot.TouchMode)
            {
                lastSource = src;
                lastTouch = UIRoot.TouchMode;
                displayed = (lastTouch ? "" : "[E] ") + src;
            }
            UIRoot.I.ShowPrompt(displayed, Current.IsSpecial);
            UIRoot.I.ShowMarker(Current.MarkerPoint);
        }

        void Clear()
        {
            Current = null;
            lastSource = null;
            if (UIRoot.I != null) UIRoot.I.HidePrompt();
        }

        void TryClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            // R005: il clic che serve a bloccare il cursore (o subito dopo) non attiva oggetti
            if (CursorManager.SwallowPointer) return;
            // con il cursore bloccato non si punta un oggetto preciso e il clic sinistro non fa nulla (decisione R005): si usa E
            if (CursorManager.I != null && CursorManager.I.Locked) return;
            var es = EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return;
            var cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            int n = Physics.RaycastNonAlloc(ray, hits, 60f, ~0, QueryTriggerInteraction.Collide);
            // Il colpo interattivo piu' vicino vince, a meno che un ostacolo solido gli stia davanti di piu' di 30 cm
            // (un battente incassato nella facciata non deve essere coperto dal collider dell'edificio).
            float intD = float.MaxValue, solidD = float.MaxValue;
            int bestI = -1;
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform)) continue; // ignora il player stesso
                if (col.name.StartsWith("Muro")) continue;          // muri resi trasparenti dalla camera
                if (col.GetComponentInParent<Interactable>() != null)
                {
                    if (hits[i].distance < intD) { intD = hits[i].distance; bestI = i; }
                }
                else if (hits[i].distance < solidD) solidD = hits[i].distance;
            }
            if (bestI < 0 || solidD < intD - 0.3f) return;

            var it = hits[bestI].collider.GetComponentInParent<Interactable>();
            if (it == null || !it.CanInteract) return;
            if (Vector3.Distance(transform.position, hits[bestI].point) > clickRange) return;
            it.Interact();
        }
    }
}
