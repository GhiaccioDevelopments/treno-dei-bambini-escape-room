using System.Collections;
using TrenoBambini.Audio;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrenoBambini.Core
{
    /// <summary>
    /// Sistema audio del gioco (R002). Volume generale (AudioListener.volume) + quattro canali (Musica, Ambiente, Effetti, Voci),
    /// ognuno con slider salvato in SaveData e livello di base preso da audio.json. Crea MusicPlayer, AmbientPlayer, SfxPlayer e
    /// SceneAudioDirector. Ducking: mentre parla una voce la musica e l'ambiente si abbassano con rampe morbide.
    /// In WebGL niente audio prima del primo clic/tocco/tasto (politica di autoplay dei browser).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource voice;
        float musicDuck = 1f, ambientDuck = 1f;
        SceneAudioDirector director;

        public MusicPlayer Music { get; private set; }
        public AmbientPlayer Ambient { get; private set; }
        public SfxPlayer Sfx { get; private set; }

        /// <summary>Superficie dei passi della scena corrente (pietra | legno | ovattato), impostata da SceneAudioDirector.</summary>
        public string Surface = "pietra";

        /// <summary>True in WebGL finché l'utente non ha cliccato/toccato: fino ad allora tutto resta muto.</summary>
        public bool WaitingForGesture { get; private set; }

        public float MusicDuck { get { return musicDuck; } }
        public float AmbientDuck { get { return ambientDuck; } }

        public float Volume { get { return AudioListener.volume; } }
        public bool VoicePlaying { get { return voice != null && voice.isPlaying; } }

        void Awake()
        {
            I = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;

            Music = gameObject.AddComponent<MusicPlayer>();
            Ambient = gameObject.AddComponent<AmbientPlayer>();
            Sfx = gameObject.AddComponent<SfxPlayer>();
            WaitingForGesture = Application.platform == RuntimePlatform.WebGLPlayer && !Application.isBatchMode;
            director = gameObject.AddComponent<SceneAudioDirector>();
        }

        void Start()
        {
            if (GameManager.I != null) AudioListener.volume = Mathf.Clamp01(GameManager.I.Save.volume);
            if (WaitingForGesture) StartCoroutine(HintGesture());
        }

        void OnDestroy() { if (I == this) I = null; }

        IEnumerator HintGesture()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            if (WaitingForGesture && UIRoot.I != null) UIRoot.I.Toast("Tocca o clicca per attivare l'audio.", 6f);
        }

        void Update()
        {
            if (WaitingForGesture && GestureHappened())
            {
                WaitingForGesture = false;
                if (director != null) director.Reapply();
            }

            var v = GameData.Audio.volumi;
            float dt = Time.unscaledDeltaTime;
            bool talking = VoicePlaying;
            musicDuck = Mathf.MoveTowards(musicDuck, talking ? Mathf.Clamp01(v.duckingMusica) : 1f, dt * 1.6f);
            ambientDuck = Mathf.MoveTowards(ambientDuck, talking ? Mathf.Clamp01(v.duckingAmbiente) : 1f, dt * 1.6f);
            if (voice != null) voice.volume = ChannelGain(AudioChannel.Voci);
            UISfx.Volume = ChannelGain(AudioChannel.Effetti);
        }

        static bool GestureHappened()
        {
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            return false;
        }

        // ------------------------------------------------------------ volumi
        public void SetVolume(float v)
        {
            v = Mathf.Clamp01(v);
            AudioListener.volume = v;
            if (GameManager.I != null) GameManager.I.Save.volume = v;
        }

        /// <summary>Valore dello slider del canale (0..1).</summary>
        public float GetChannel(AudioChannel ch)
        {
            if (GameManager.I == null) return 1f;
            var s = GameManager.I.Save;
            switch (ch)
            {
                case AudioChannel.Musica: return s.volMusica;
                case AudioChannel.Ambiente: return s.volAmbiente;
                case AudioChannel.Effetti: return s.volEffetti;
                default: return s.volVoci;
            }
        }

        public void SetChannel(AudioChannel ch, float value)
        {
            value = Mathf.Clamp01(value);
            if (GameManager.I == null) return;
            var s = GameManager.I.Save;
            switch (ch)
            {
                case AudioChannel.Musica: s.volMusica = value; break;
                case AudioChannel.Ambiente: s.volAmbiente = value; break;
                case AudioChannel.Effetti: s.volEffetti = value; break;
                default: s.volVoci = value; break;
            }
        }

        /// <summary>Guadagno finale del canale: slider x livello di base da audio.json (il volume generale è AudioListener.volume).</summary>
        public float ChannelGain(AudioChannel ch)
        {
            var b = GameData.Audio.volumi;
            float baseLevel;
            switch (ch)
            {
                case AudioChannel.Musica: baseLevel = b.musica; break;
                case AudioChannel.Ambiente: baseLevel = b.ambiente; break;
                case AudioChannel.Effetti: baseLevel = b.effetti; break;
                default: baseLevel = b.voci; break;
            }
            return Mathf.Clamp01(GetChannel(ch)) * baseLevel;
        }

        // ------------------------------------------------------------ voci
        public void PlayVoice(AudioClip clip)
        {
            if (clip == null || voice == null) return;
            voice.Stop();
            voice.clip = clip;
            voice.volume = ChannelGain(AudioChannel.Voci);
            voice.Play();
        }

        public void StopVoice()
        {
            if (voice != null) voice.Stop();
        }

        // ------------------------------------------------------------ eventi di gioco (audio.json "eventi")
        /// <summary>Esegue un evento di audio.json (stinger e/o effetto). Non fa nulla se l'evento o l'audio mancano.</summary>
        public static void Fire(string eventId)
        {
            Run(eventId, false, Vector3.zero);
        }

        /// <summary>Come Fire, ma l'effetto è 3D nel mondo (porte, oggetti).</summary>
        public static void FireAt(string eventId, Vector3 position)
        {
            Run(eventId, true, position);
        }

        static void Run(string eventId, bool at, Vector3 pos)
        {
            if (I == null || string.IsNullOrEmpty(eventId)) return;
            var list = GameData.Audio.eventi;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.id != eventId) continue;
                if (!string.IsNullOrEmpty(e.stinger)) I.Music.Stinger(e.stinger);
                if (!string.IsNullOrEmpty(e.sfx))
                {
                    if (at) I.Sfx.PlayAt(e.sfx, pos); else I.Sfx.Play(e.sfx);
                }
                return;
            }
        }
    }
}
