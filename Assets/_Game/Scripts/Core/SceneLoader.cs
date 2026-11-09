using System.Collections;
using TrenoBambini.Player;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Core
{
    public enum TransitionStyle { Fade, Ricordo }

    /// <summary>Caricamento scene con transizione e posizionamento del player sullo SpawnPoint.</summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader I { get; private set; }
        public bool IsLoading { get; private set; }

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        /// <summary>
        /// preRivela (R025, filmato iniziale): coroutine eseguita a scena caricata e giocatore posizionato, PRIMA che la transizione riveli la scena
        /// (serve a sistemare la camera mentre lo schermo e' ancora coperto).
        /// </summary>
        public void Load(string scena, string spawnId, TransitionStyle stile = TransitionStyle.Fade, System.Func<IEnumerator> preRivela = null)
        {
            if (IsLoading) return;
            if (string.IsNullOrEmpty(scena) || !Application.CanStreamedLevelBeLoaded(scena))
            {
                Debug.LogWarning("Scena non caricabile (manca nei Build Settings?): " + scena);
                if (UIRoot.I != null) UIRoot.I.Toast("Questo luogo non è ancora raggiungibile.");
                return;
            }
            StartCoroutine(LoadRoutine(scena, spawnId, stile, preRivela));
        }

        IEnumerator LoadRoutine(string scena, string spawnId, TransitionStyle stile, System.Func<IEnumerator> preRivela)
        {
            IsLoading = true;
            GameManager.I.PushBlock("caricamento");
            Fader fader = UIRoot.I != null ? UIRoot.I.Fader : null;

            // audio: uscita dal ricordo (proiettore che si ferma) oppure cambio scena normale (audio.json "eventi")
            if (SceneManager.GetActiveScene().name.Contains("_FB_")) AudioManager.Fire("ricordo_esce");
            else if (stile == TransitionStyle.Ricordo) AudioManager.Fire("ricordo_entra");
            else AudioManager.Fire("cambio_scena");

            if (fader != null) yield return fader.Out(stile);
            if (GameManager.MenuBackdrop) yield return MenuBackdrop.Congeda(); // R023: schermo nero, il vicolo del menu si scarica prima del caricamento vero
            // i decal escono dal DecalEntityManager di URP uno alla volta, prima che la scena sparisca: scaricare la scena con i decal ancora attivi
            // faceva andare in crash il DecalDrawSystem (player Windows, preset con decal)
            foreach (var d in FindObjectsByType<UnityEngine.Rendering.Universal.DecalProjector>(FindObjectsInactive.Exclude)) d.enabled = false;
            yield return null; yield return null;
            GameManager.MenuBackdrop = false; // R023: da qui lo sfondo del menu non conta piu' (niente pellicola del menu nella scena che arriva)
            if (UIRoot.I != null) { UIRoot.I.HidePrompt(); UIRoot.I.SetHudCounter(""); }

            var op = SceneManager.LoadSceneAsync(scena);
            while (op != null && !op.isDone) yield return null;
            yield return null; // lascia eseguire Awake/Start della nuova scena

            PlacePlayer(spawnId);
            GameManager.I.OnSceneEntered(scena, spawnId);
            yield return null;
            if (preRivela != null) yield return preRivela();

            if (fader != null) yield return fader.In(stile);
            GameManager.I.PopBlock("caricamento");
            IsLoading = false;
        }

        static void PlacePlayer(string spawnId)
        {
            if (string.IsNullOrEmpty(spawnId)) return;

            var points = FindObjectsByType<SpawnPoint>();
            SpawnPoint chosen = null;
            for (int i = 0; i < points.Length; i++)
                if (points[i].id == spawnId) { chosen = points[i]; break; }
            if (chosen == null && points.Length > 0) chosen = points[0];
            if (chosen == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(chosen.transform.position, chosen.transform.rotation);
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();

            var tpc = player.GetComponent<ThirdPersonController>();
            if (tpc != null) tpc.ResetMotion();
            var rig = FindAnyObjectByType<CameraRig>();
            if (rig != null) rig.Snap();
        }
    }
}
