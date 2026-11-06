using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrenoBambini.Player
{
    /// <summary>
    /// Nasconde i muri che stanno tra la camera e il player, così negli interni piccoli la scena resta leggibile.
    /// Agisce solo sui collider del layer "VistaLibera" (assegnato dal builder solo a Muro*, tranne MuroInvisibile*; le porte non si nascondono mai):
    /// la camera (CinemachineDeoccluder) li ignora e questo componente li rende invisibili.
    /// I renderer passano a "solo ombra" (non a disattivati): la stanza resta coerente con la luce e l'ombra del muro.
    /// Un ritardo di rilascio (hysteresis) evita lampeggi ai bordi.
    /// </summary>
    public class WallFader : MonoBehaviour
    {
        public const string LayerName = "VistaLibera";
        /// <summary>Layer delle porte: la camera le attraversa ma non vengono mai nascoste.</summary>
        public const string LayerPorte = "PorteVista";

        public Transform target;      // punto mirato sul player (Punto camera)
        public float castRadius = 0.35f;
        public float releaseDelay = 0.2f;

        int mask;
        Transform camT;
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly Collider[] overlaps = new Collider[16];
        readonly Dictionary<Renderer, float> hidden = new Dictionary<Renderer, float>();
        readonly Dictionary<Renderer, ShadowCastingMode> original = new Dictionary<Renderer, ShadowCastingMode>();
        readonly List<Renderer> rendBuf = new List<Renderer>();
        readonly List<Renderer> release = new List<Renderer>();

        void Awake() { mask = LayerMask.GetMask(LayerName); }

        void OnDisable() { RestoreAll(); }

        void LateUpdate()
        {
            if (mask == 0 || target == null) return;
            if (camT == null)
            {
                var c = Camera.main;
                if (c == null) return;
                camT = c.transform;
            }

            Vector3 cam = camT.position;
            Vector3 tgt = target.position;
            Vector3 dir = tgt - cam;
            float dist = dir.magnitude;
            if (dist > 0.01f)
            {
                dir /= dist;
                Collect(Physics.SphereCastNonAlloc(cam, castRadius, dir, hits, dist, mask, QueryTriggerInteraction.Ignore), false);
                // la camera potrebbe già trovarsi dentro un collider (non rilevato dallo spherecast): si controlla anche il ritorno
                Collect(Physics.SphereCastNonAlloc(tgt, castRadius, -dir, hits, dist + 0.6f, mask, QueryTriggerInteraction.Ignore), true);
            }
            int n = Physics.OverlapSphereNonAlloc(cam, 0.6f, overlaps, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                // il soffitto degli interni si nasconde solo se la camera e' davvero sopra la sua faccia inferiore:
                // con la camera sotto la volta (anche a meno di 60 cm) resta visibile
                if (overlaps[i].name.Contains("Soffitto") && cam.y < overlaps[i].bounds.min.y) continue;
                Hide(overlaps[i]);
            }

            // renderer distrutti (cambio scena, rebuild): via anche da 'original'
            if (original.Count > hidden.Count)
            {
                release.Clear();
                foreach (var kv in original) if (kv.Key == null) release.Add(kv.Key);
                for (int i = 0; i < release.Count; i++) original.Remove(release[i]);
            }

            release.Clear();
            float now = Time.unscaledTime;
            foreach (var kv in hidden)
                if (kv.Key == null || now - kv.Value > releaseDelay) release.Add(kv.Key);
            for (int i = 0; i < release.Count; i++)
            {
                var r = release[i];
                if (r != null && original.TryGetValue(r, out var mode)) r.shadowCastingMode = mode;
                hidden.Remove(r);
                if (r != null) original.Remove(r);
            }
        }

        // fromTarget: il cast parte dal player; i colpi a distanza 0 sono collider già toccati alla partenza
        // (muro addossato al giocatore), non stanno tra camera e giocatore: si scartano.
        void Collect(int n, bool fromTarget)
        {
            for (int i = 0; i < n; i++)
            {
                if (fromTarget && hits[i].distance <= 0.0001f) continue;
                var col = hits[i].collider;
                if (col != null && camT != null && col.name.Contains("Soffitto") && camT.position.y < col.bounds.min.y) continue; // camera sotto la volta
                Hide(col);
            }
        }

        void Hide(Collider c)
        {
            if (c == null) return;
            c.GetComponentsInChildren(rendBuf);
            float now = Time.unscaledTime;
            for (int k = 0; k < rendBuf.Count; k++)
            {
                var r = rendBuf[k];
                if (!original.ContainsKey(r)) original[r] = r.shadowCastingMode;
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                hidden[r] = now;
            }
        }

        void RestoreAll()
        {
            foreach (var kv in original)
                if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            original.Clear();
            hidden.Clear();
        }
    }
}
