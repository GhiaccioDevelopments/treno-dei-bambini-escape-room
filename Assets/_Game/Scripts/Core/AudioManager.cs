using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>Volume globale e sorgente per le voci delle cutscene.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        AudioSource voice;

        public float Volume { get { return AudioListener.volume; } }
        public bool VoicePlaying { get { return voice != null && voice.isPlaying; } }

        void Awake()
        {
            I = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
        }

        void Start()
        {
            if (GameManager.I != null) AudioListener.volume = Mathf.Clamp01(GameManager.I.Save.volume);
        }

        void OnDestroy() { if (I == this) I = null; }

        public void SetVolume(float v)
        {
            v = Mathf.Clamp01(v);
            AudioListener.volume = v;
            if (GameManager.I != null) GameManager.I.Save.volume = v;
        }

        public void PlayVoice(AudioClip clip)
        {
            if (clip == null || voice == null) return;
            voice.Stop();
            voice.clip = clip;
            voice.Play();
        }

        public void StopVoice()
        {
            if (voice != null) voice.Stop();
        }
    }
}
