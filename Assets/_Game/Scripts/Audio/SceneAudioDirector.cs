using System.Collections;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Legge audio.json a ogni cambio scena: avvia musica (con crossfade) e ambienti, imposta la superficie dei passi e
    /// aggiunge FootstepAudio al player. Niente è cablato nel codice: quale musica suona dove sta in Resources/Data/audio.json.
    /// </summary>
    public class SceneAudioDirector : MonoBehaviour
    {
        string appliedScene = "";

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }

        void Start() { Apply(SceneManager.GetActiveScene().name); }

        void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive && GameManager.MenuBackdrop) return; // R023: vicolo come sfondo del menu, la musica resta quella del titolo
            Apply(s.name);
        }

        /// <summary>Riapplica la scena corrente (es. dopo il primo gesto in WebGL).</summary>
        public void Reapply() { Apply(SceneManager.GetActiveScene().name); }

        void Apply(string scene)
        {
            var am = AudioManager.I;
            if (am == null) return;
            appliedScene = scene;
            AudioSceneDef def = null;
            var list = GameData.Audio.scene;
            for (int i = 0; i < list.Count; i++) if (list[i].scena == scene) { def = list[i]; break; }

            if (def == null)
            {
                // scena senza voce in audio.json (es. Boot): nessun cambio di musica, passi su pietra
                am.Surface = "pietra";
                return;
            }
            am.Surface = string.IsNullOrEmpty(def.superficie) ? "pietra" : def.superficie;
            StartCoroutine(AttachFootsteps());
            if (am.WaitingForGesture) return; // WebGL: si parte al primo clic/tocco (vedi AudioManager)
            am.Music.Play(def.musica, 2.5f);
            am.Ambient.Set(def.ambiente, def.sparsi, def.intervallo);
        }

        IEnumerator AttachFootsteps()
        {
            yield return null; // il player può essere creato in Start() della scena
            var p = FindAnyObjectByType<ThirdPersonController>();
            if (p != null && p.GetComponent<FootstepAudio>() == null) p.gameObject.AddComponent<FootstepAudio>();
        }
    }
}
