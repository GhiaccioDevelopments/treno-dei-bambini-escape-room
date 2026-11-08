using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrenoBambini.Player
{
    /// <summary>
    /// R018 - REGOLA UNICA (dettagli in CameraGuard): ESTERNI = LA CAMERA SI AVVICINA; INTERNI = I MURI TRA CAMERA E AMERIGO SI DISSOLVONO;
    /// SOFFITTI E PAVIMENTI SEMPRE SOLIDI.
    /// WallFader lavora SOLO negli interni (CameraRig.Interno, cioe' dentro un CameraProfileVolume con 'dissolviMuri'): i collider dei layer
    /// "VistaLibera" (Muro*, tranne MuroInvisibile* e i soffitti) e "PorteVista" (Porta*) che stanno tra la camera e il giocatore, o attorno
    /// alla camera, vengono dissolti con MuroDissolvibile (dithering di 0,25 s). Un ritardo di rilascio evita lampeggi ai bordi.
    /// Negli esterni non fa nulla (la camera collide con tutto) e rimette tutto visibile. Se un muro non ha MuroDissolvibile ripiega sul
    /// vecchio metodo: i suoi renderer passano a "solo ombra".
    /// </summary>
    public class WallFader : MonoBehaviour
    {
        public const string LayerName = "VistaLibera";
        /// <summary>Layer delle porte: negli interni si dissolvono come i muri, negli esterni la camera ci collide.</summary>
        public const string LayerPorte = "PorteVista";

        public Transform target;      // punto mirato sul player (Punto camera)
        public float castRadius = 0.35f;
        public float releaseDelay = 0.25f;

        int mask;
        Transform camT;
        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly Collider[] overlaps = new Collider[16];
        // muro dissolvibile -> ultimo istante in cui serviva nasconderlo
        readonly Dictionary<MuroDissolvibile, float> nascosti = new Dictionary<MuroDissolvibile, float>();
        readonly Dictionary<Renderer, float> hidden = new Dictionary<Renderer, float>();
        readonly Dictionary<Renderer, ShadowCastingMode> original = new Dictionary<Renderer, ShadowCastingMode>();
        readonly Dictionary<Collider, MuroDissolvibile> cache = new Dictionary<Collider, MuroDissolvibile>();
        readonly List<Renderer> rendBuf = new List<Renderer>();
        readonly List<Renderer> release = new List<Renderer>();
        readonly List<MuroDissolvibile> rilascia = new List<MuroDissolvibile>();

        void Awake() { mask = LayerMask.GetMask(LayerName, LayerPorte); }

        void OnDisable() { RestoreAll(); }

        void LateUpdate()
        {
            if (mask == 0 || target == null) return;
            if (!CameraRig.Interno)
            {
                if (nascosti.Count > 0 || hidden.Count > 0) RestoreAll();
                return;
            }
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
                // la camera potrebbe gia' trovarsi dentro un collider (non rilevato dallo spherecast): si controlla anche il ritorno
                Collect(Physics.SphereCastNonAlloc(tgt, castRadius, -dir, hits, dist + 0.6f, mask, QueryTriggerInteraction.Ignore), true);
            }
            int n = Physics.OverlapSphereNonAlloc(cam, 0.6f, overlaps, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) Hide(overlaps[i]);

            float now = Time.unscaledTime;
            rilascia.Clear();
            foreach (var kv in nascosti)
                if (kv.Key == null || now - kv.Value > releaseDelay) rilascia.Add(kv.Key);
            for (int i = 0; i < rilascia.Count; i++)
            {
                if (rilascia[i] != null) rilascia[i].Dissolvi(false);
                nascosti.Remove(rilascia[i]);
            }

            // ripiego: renderer di muri senza MuroDissolvibile
            release.Clear();
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

        // fromTarget: il cast parte dal player; i colpi a distanza 0 sono collider gia' toccati alla partenza
        // (muro addossato al giocatore), non stanno tra camera e giocatore: si scartano.
        void Collect(int n, bool fromTarget)
        {
            for (int i = 0; i < n; i++)
            {
                if (fromTarget && hits[i].distance <= 0.0001f) continue;
                Hide(hits[i].collider);
            }
        }

        void Hide(Collider c)
        {
            if (c == null) return;
            float now = Time.unscaledTime;
            if (!cache.TryGetValue(c, out var m)) { m = c.GetComponentInParent<MuroDissolvibile>(); cache[c] = m; }
            if (m != null)
            {
                m.Dissolvi(true);
                nascosti[m] = now;
                return;
            }
            c.GetComponentsInChildren(rendBuf);
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
            foreach (var kv in nascosti) if (kv.Key != null) kv.Key.Dissolvi(false);
            nascosti.Clear();
            foreach (var kv in original)
                if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            original.Clear();
            hidden.Clear();
            cache.Clear();
        }
    }
}
