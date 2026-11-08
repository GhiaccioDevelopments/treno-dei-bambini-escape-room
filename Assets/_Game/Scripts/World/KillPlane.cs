using System.Collections;
using UnityEngine;
using TrenoBambini.Core;
using TrenoBambini.Player;
using TrenoBambini.UI;

namespace TrenoBambini.World
{
    /// <summary>
    /// R017: piano di recupero. Se il giocatore scende sotto la quota 'sogliaY' (caduto da un buco residuo della mappa) la schermata si
    /// oscura con la dissolvenza di gioco (Fader), il giocatore torna allo SpawnPoint piu' vicino e la schermata si riapre.
    /// </summary>
    public class KillPlane : MonoBehaviour
    {
        public float sogliaY = -6f;
        bool inCorso, disattivato;

        void Update()
        {
            if (inCorso || disattivato) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.transform.position.y < sogliaY) StartCoroutine(Recupera(player));
        }

        IEnumerator Recupera(GameObject player)
        {
            inCorso = true;
            if (GameManager.I != null) GameManager.I.PushBlock("kill-plane");
            Fader fader = UIRoot.I != null ? UIRoot.I.Fader : null;
            if (fader != null) yield return fader.Out(TransitionStyle.Fade);

            SpawnPoint scelto = null;
            float best = float.MaxValue;
            var pos = player.transform.position;
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude))
            {
                float d = new Vector2(sp.transform.position.x - pos.x, sp.transform.position.z - pos.z).sqrMagnitude;
                if (d < best) { best = d; scelto = sp; }
            }
            if (scelto == null)
            {
                // nessun punto di ritorno: niente teletrasporto, e il piano si disattiva (altrimenti ripartirebbe a ogni frame)
                Debug.LogWarning("[KillPlane] Nessuno SpawnPoint nella scena: recupero impossibile, piano disattivato.");
                disattivato = true;
                if (fader != null) yield return fader.In(TransitionStyle.Fade);
                if (GameManager.I != null) GameManager.I.PopBlock("kill-plane");
                inCorso = false;
                yield break;
            }
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.SetPositionAndRotation(scelto.transform.position + Vector3.up * 0.05f, scelto.transform.rotation);
                if (cc != null) cc.enabled = true;
                Physics.SyncTransforms();
                var tpc = player.GetComponent<ThirdPersonController>();
                if (tpc != null) tpc.ResetMotion();
                var rig = FindAnyObjectByType<CameraRig>();
                if (rig != null) rig.Snap();
            }
            yield return null;
            if (fader != null) yield return fader.In(TransitionStyle.Fade);
            if (GameManager.I != null) GameManager.I.PopBlock("kill-plane");
            inCorso = false;
        }
    }
}
