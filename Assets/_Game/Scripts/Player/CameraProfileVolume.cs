using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.Player
{
    /// <summary>
    /// R018: volume (BoxCollider trigger, allineato agli assi) che attiva un profilo camera per una stanza: scala dell'orbita
    /// (altezze e raggi degli anelli rispetto al punto mirato), campo visivo e limiti dell'inclinazione. CameraRig cerca ogni
    /// fotogramma il volume piu' piccolo che contiene il giocatore e passa al suo profilo con una transizione morbida; fuori da
    /// tutti i volumi torna al profilo della scena. Con 'dissolviMuri' il volume e' un interno: vedi la regola in CameraGuard. Creato dai generatori delle scene (SceneBuilderBasso).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CameraProfileVolume : MonoBehaviour
    {
        public static readonly List<CameraProfileVolume> Tutti = new List<CameraProfileVolume>();

        [Tooltip("Moltiplicatore di altezze e raggi dell'orbita (1 = profilo della scena).")]
        public float scalaOrbita = 1f;
        [Tooltip("Campo visivo in gradi; 0 = quello del profilo della scena.")]
        public float fov;
        [Tooltip("Limiti dell'asse verticale (-1 anello basso, +1 anello alto).")]
        public float pitchMin = -1f;
        public float pitchMax = 1f;

        [Tooltip("Interno: i muri, le pareti e le porte tra camera e giocatore si dissolvono (MuroDissolvibile) invece di spingere la camera. Soffitto e pavimento restano solidi.")]
        public bool dissolviMuri;

        BoxCollider box;

        void OnEnable() { box = GetComponent<BoxCollider>(); box.isTrigger = true; if (!Tutti.Contains(this)) Tutti.Add(this); }
        void OnDisable() { Tutti.Remove(this); }

        public bool Contiene(Vector3 p) { return box != null && box.bounds.Contains(p); }
        public float Volume() { var s = box != null ? box.bounds.size : Vector3.one; return s.x * s.y * s.z; }

        /// <summary>Il volume piu' piccolo che contiene il punto, o null.</summary>
        public static CameraProfileVolume Trova(Vector3 p)
        {
            CameraProfileVolume best = null; float bv = float.MaxValue;
            for (int i = 0; i < Tutti.Count; i++)
            {
                var v = Tutti[i];
                if (v == null || !v.Contiene(p)) continue;
                float vol = v.Volume();
                if (vol < bv) { bv = vol; best = v; }
            }
            return best;
        }
    }
}
