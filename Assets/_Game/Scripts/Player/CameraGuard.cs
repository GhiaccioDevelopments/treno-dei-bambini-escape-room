using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrenoBambini.Player
{
    /// <summary>
    /// R018: rete di sicurezza della camera, dopo il CinemachineDeoccluder (stesso stadio Body, componente successivo).
    ///
    /// REGOLA UNICA (vale per CinemachineDeoccluder, CameraGuard, WallFader e MuroDissolvibile):
    ///  ESTERNI = LA CAMERA SI AVVICINA; INTERNI = I MURI TRA CAMERA E AMERIGO SI DISSOLVONO; SOFFITTI E PAVIMENTI SEMPRE SOLIDI.
    ///  1. Esterni (hub, flashback, fuori dai volumi CameraProfileVolume con 'dissolviMuri'): la camera NON attraversa mai niente di solido
    ///     (edifici, muri, balconi, porte, props con un lato oltre 'MinLatoCollider'); il Deoccluder la avvicina con smorzamento e
    ///     CameraGuard copre i casi che il Deoccluder non vede (giocatore addossato a un muro: lo spherecast parte gia' dentro il collider).
    ///  2. Interni (volumi con 'dissolviMuri', cioe' le stanze del basso): muri, pareti divisorie, stipiti e porte (layer VistaLibera e PorteVista)
    ///     NON spingono la camera: WallFader li dissolve con un dithering di 0,25 s (LODGroup con cross-fade, vedi MuroDissolvibile).
    ///     Soffitti e pavimenti restano solidi (collidono sempre, nessun dissolvimento). CameraRig sceglie la modalita' in base al volume.
    ///  3. Sempre: la camera sta sopra il suolo (quota minima dai piedi e dal terreno sotto di lei), a distanza minima dal giocatore, e in vista
    ///     stretta alza l'inclinazione ("da sopra la spalla") ma mai oltre ElevazioneMax (50 gradi). Il rilascio dopo un ostacolo e' morbido
    ///     ('velocitaRilascio'), l'avvicinamento immediato. Se la camera e' troppo vicina il giocatore si nasconde (solo ombra, con isteresi).
    /// </summary>
    public class CameraGuard : CinemachineExtension
    {
        /// <summary>Lato minimo (m) della bounding box oltre il quale un collider conta per la camera.</summary>
        public const float MinLatoCollider = 0.6f;

        [Tooltip("Punto mirato sul giocatore (Punto camera).")]
        public Transform target;
        [Tooltip("Layer contro cui la camera collide: tutto.")]
        public LayerMask collideAgainst = ~0;
        public float margine = 0.22f;
        public float raggio = 0.18f;
        [Tooltip("Distanza minima dal punto mirato quando c'e' un ostacolo molto vicino.")]
        public float distanzaMinima = 0.12f;
        [Tooltip("Altezza minima della camera sopra i piedi del giocatore e sopra il suolo sotto di lei.")]
        public float quotaMinima = 0.45f;
        [Tooltip("Metri al secondo con cui la camera torna alla posa voluta dopo un ostacolo.")]
        public float velocitaRilascio = 5f;
        /// <summary>Sotto questa distanza (m) dal punto mirato il giocatore si nasconde; sopra MostraOltre riappare (isteresi).</summary>
        const float NascondiSotto = 0.6f;
        const float MostraOltre = 0.8f;
        /// <summary>Inclinazione massima (gradi) con cui la camera si alza in vista stretta, a passi di 12 gradi.</summary>
        const int PassiAlzata = 4;
        /// <summary>Elevazione massima (gradi) della direzione target-camera: la camera non va mai a picco.</summary>
        public const float ElevazioneMax = 50f;

        class Extra : VcamExtraStateBase { public Vector3 correzione; public bool valida; }

        static readonly RaycastHit[] hits = new RaycastHit[24];
        static readonly Collider[] ovl = new Collider[16];
        Transform playerRoot;
        readonly List<Renderer> rendererGiocatore = new List<Renderer>();
        readonly Dictionary<Renderer, ShadowCastingMode> originale = new Dictionary<Renderer, ShadowCastingMode>();
        bool nascosto;
        float prossimaScansione;
        Transform camT;

        /// <summary>Distanza camera-giocatore dopo la correzione (informativo, per la prova automatica).</summary>
        public float UltimaDistanza { get; private set; }

        /// <summary>Quota minima (m) di un ostacolo basso perche' conti per la camera (sotto: sta sotto la quota minima della camera).</summary>
        public const float MinAltezzaCollider = 0.4f;

        /// <summary>
        /// Un ostacolo conta per la camera se ha un lato di almeno MinLatoCollider e (e' alto almeno MinAltezzaCollider oppure e' una lastra
        /// larga almeno 3 m: pavimenti, tetti, soffitti). Materassi, rotoli e props bassi e piccoli non contano.
        /// </summary>
        public static bool ContaDimensioni(Vector3 s)
        {
            float orizz = Mathf.Max(s.x, s.z);
            return Mathf.Max(orizz, s.y) >= MinLatoCollider && (s.y >= MinAltezzaCollider || orizz >= 3f);
        }

        public static bool Conta(Collider c)
        {
            return c != null && !c.isTrigger && ContaDimensioni(c.bounds.size);
        }

        Transform PlayerRoot()
        {
            if (playerRoot == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerRoot = p.transform;
                else if (target != null) playerRoot = target.root;
            }
            return playerRoot;
        }

        /// <summary>Distanza libera (m) lungo 'dir' da 'origine', al massimo 'dist'; dist se non c'e' niente di solido.</summary>
        float Libera(Vector3 origine, Vector3 dir, float dist)
        {
            float libera = dist;
            var root = PlayerRoot();
            int n = Physics.RaycastNonAlloc(origine, dir, hits, dist + margine, collideAgainst, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (!Conta(c) || (root != null && c.transform.IsChildOf(root))) continue;
                libera = Mathf.Min(libera, hits[i].distance - margine);
            }
            n = Physics.SphereCastNonAlloc(origine, raggio, dir, hits, dist, collideAgainst, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (hits[i].distance <= 0.0001f) continue; // gia' toccato alla partenza: lo copre il raggio sottile
                if (!Conta(c) || (root != null && c.transform.IsChildOf(root))) continue;
                libera = Mathf.Min(libera, hits[i].distance - 0.05f);
            }
            return Mathf.Max(libera, distanzaMinima);
        }

        bool Dentro(Vector3 p)
        {
            var root = PlayerRoot();
            int n = Physics.OverlapSphereNonAlloc(p, 0.14f, ovl, collideAgainst, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = ovl[i];
                if (!Conta(c) || (root != null && c.transform.IsChildOf(root))) continue;
                return true;
            }
            return false;
        }

        /// <summary>Posa corretta: sempre su un punto senza ostacoli solidi tra il target e la camera.</summary>
        Vector3 Risolvi(Vector3 T, Vector3 cam)
        {
            Vector3 d = cam - T;
            float dist = d.magnitude;
            if (dist < 0.01f) return cam;
            Vector3 dir = d / dist;
            float a = Libera(T, dir, dist);
            Vector3 res = a >= dist ? cam : T + dir * a;
            if (a < Mathf.Min(dist, 1.3f))
            {
                // vista stretta: provo ad alzare l'inclinazione (sopra la spalla) finche' lo spazio libero basta
                Vector3 destra = Vector3.Cross(Vector3.up, dir);
                if (destra.sqrMagnitude < 1e-4f) destra = Vector3.right;
                destra.Normalize();
                float miglior = a, migliorPunti = a; Vector3 migliorDir = dir;
                float serve = Mathf.Min(dist, 2.2f);
                for (int k = 1; k <= PassiAlzata; k++)
                {
                    Vector3 d2 = Quaternion.AngleAxis(-12f * k, destra) * dir;
                    if (Mathf.Asin(Mathf.Clamp(d2.y, -1f, 1f)) * Mathf.Rad2Deg > ElevazioneMax) break;
                    float a2 = Libera(T, d2, dist);
                    float punti = Mathf.Min(a2, dist) - 0.06f * k; // a parita' di spazio si preferisce l'inclinazione piu' bassa
                    if (punti > migliorPunti) { migliorPunti = punti; miglior = a2; migliorDir = d2; }
                    if (a2 >= serve) break;
                }
                res = T + migliorDir * Mathf.Min(miglior, dist);
            }
            // quota minima: sopra i piedi del giocatore e sopra il suolo sotto la camera
            var root = PlayerRoot();
            float piedi = root != null ? root.position.y : T.y - 1.2f;
            float minY = piedi + quotaMinima;
            if (Physics.Raycast(new Vector3(res.x, Mathf.Max(T.y, res.y), res.z), Vector3.down, out var gh, Mathf.Max(T.y, res.y) - piedi + 4f, collideAgainst, QueryTriggerInteraction.Ignore)
                && !(root != null && gh.collider.transform.IsChildOf(root)) && Conta(gh.collider))
                minY = Mathf.Max(minY, gh.point.y + 0.3f);
            if (res.y < minY)
            {
                res.y = minY;
                Vector3 dd = res - T;
                float dl = dd.magnitude;
                if (dl > 0.01f) { float a3 = Libera(T, dd / dl, dl); if (a3 < dl) res = T + dd / dl * a3; }
            }
            // ultima verifica: se e' ancora dentro qualcosa si avvicina al target a piccoli passi
            for (int i = 0; i < 12 && Dentro(res); i++) res = Vector3.MoveTowards(res, T, 0.15f);
            if (Dentro(res)) res = T; // ostacolo attaccato al giocatore: la camera sta sul punto mirato (il giocatore si nasconde)
            return res;
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body || target == null) return;
            var extra = GetExtraState<Extra>(vcam);
            if (!vcam.PreviousStateIsValid || deltaTime < 0f) extra.valida = false;

            Vector3 T = target.position;
            Vector3 cam = state.GetCorrectedPosition();
            Vector3 voluta = Risolvi(T, cam);
            Vector3 delta = voluta - cam;

            if (extra.valida)
            {
                // rilascio morbido: la correzione puo' ridursi solo a 'velocitaRilascio' m/s; l'aumento e' immediato
                float nuova = delta.magnitude, vecchia = extra.correzione.magnitude;
                if (nuova < vecchia)
                {
                    Vector3 morbido = Vector3.MoveTowards(extra.correzione, delta, velocitaRilascio * Mathf.Max(deltaTime, 0f));
                    Vector3 cand = cam + morbido;
                    // la posa morbida vale solo se resta libera
                    Vector3 dc = cand - T; float l = dc.magnitude;
                    if (l > 0.01f && Libera(T, dc / l, l) >= l - 0.001f && !Dentro(cand)) delta = morbido;
                }
            }
            extra.correzione = delta;
            extra.valida = true;
            state.PositionCorrection += delta;
            UltimaDistanza = (cam + delta - T).magnitude;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            var root = PlayerRoot();
            if (root == null) return;
            if (camT == null) { var c = Camera.main; if (c != null) camT = c.transform; }
            if (camT == null || target == null) return;
            // il giocatore si nasconde solo se la camera in uso e' questa (non durante le cutscene o con altre camere del Brain)
            var brain = camT.GetComponent<CinemachineBrain>();
            bool attiva = brain != null && ReferenceEquals(brain.ActiveVirtualCamera, ComponentOwner);
            float d = Vector3.Distance(camT.position, target.position);
            bool vuoleNascosto = attiva && (nascosto ? d < MostraOltre : d < NascondiSotto);
            if (vuoleNascosto == nascosto) return;
            nascosto = vuoleNascosto;
            if (nascosto && Time.unscaledTime >= prossimaScansione)
            {
                root.GetComponentsInChildren(true, rendererGiocatore);
                prossimaScansione = Time.unscaledTime + 1f;
            }
            foreach (var r in rendererGiocatore)
            {
                if (r == null) continue;
                if (nascosto) { if (!originale.ContainsKey(r)) originale[r] = r.shadowCastingMode; r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
                else if (originale.TryGetValue(r, out var m)) r.shadowCastingMode = m;
            }
            if (!nascosto) originale.Clear();
        }

        /// <summary>
        /// ORDINE DELLE ESTENSIONI: CameraGuard deve stare DOPO CinemachineDeoccluder sulla stessa vcam (le estensioni girano nell'ordine dei
        /// componenti), altrimenti il Deoccluder rimetterebbe la camera dentro i muri. Lo mette cosi' SceneBuilderCamera; qui si controlla.
        /// </summary>
        void Start()
        {
            var ext = GetComponents<CinemachineExtension>();
            int iDeo = -1, iGuard = -1;
            for (int i = 0; i < ext.Length; i++) { if (ext[i] is CinemachineDeoccluder) iDeo = i; if (ext[i] == this) iGuard = i; }
            if (iDeo >= 0 && iGuard < iDeo)
                Debug.LogWarning("[CameraGuard] Ordine sbagliato: CameraGuard deve stare dopo CinemachineDeoccluder sulla stessa camera (vedi SceneBuilderCamera).", this);
        }

        void OnDisable()
        {
            if (nascosto) foreach (var kv in originale) if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            originale.Clear(); nascosto = false;
        }
    }
}
