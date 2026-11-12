using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>Base di tutto ciò con cui il player può interagire. Registro statico: niente Find per frame.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string prompt;
        public virtual bool CanInteract { get { return true; } }
        public virtual string GetPrompt() { return prompt; }
        /// <summary>R011: distanza aggiunta nella scelta del prompt piu' vicino: i segreti cedono il passo agli interattori principali.</summary>
        public virtual float Penalita { get { return 0f; } }
        /// <summary>Oggetto speciale (es. ricordo da vivere): il prompt ha bordo dorato e scintille.</summary>
        public virtual bool IsSpecial { get { return false; } }

        /// <summary>Punto nel mondo sopra l'oggetto dove ancorare il rombo dorato dell'indicatore.</summary>
        public Vector3 MarkerPoint
        {
            get
            {
                if (!colSearched) { col = GetComponentInChildren<Collider>(); colSearched = true; }
                if (col != null) { var b = col.bounds; return new Vector3(b.center.x, b.max.y + 0.35f, b.center.z); }
                return transform.position + Vector3.up * 1.8f;
            }
        }
        public abstract void Interact();

        Collider col;
        bool colSearched;

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>Punto dell'oggetto più vicino a p (usa i bounds del collider se c'è).</summary>
        public Vector3 ClosestPointTo(Vector3 p)
        {
            if (!colSearched) { col = GetComponentInChildren<Collider>(); colSearched = true; }
            if (col != null) return col.bounds.ClosestPoint(p);
            return transform.position;
        }
    }
}
