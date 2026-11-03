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
