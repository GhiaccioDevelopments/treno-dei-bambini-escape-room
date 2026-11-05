using TrenoBambini.Core;
using UnityEngine;

namespace TrenoBambini.Audio
{
    public enum AudioChannel { Musica, Ambiente, Effetti, Voci }

    /// <summary>
    /// Musica: due AudioSource per il crossfade tra brani in loop, "stinger" (frammenti brevi) sopra il brano,
    /// abbassamento in pausa e ducking sotto le voci (il fattore arriva da AudioManager.MusicDuck).
    /// Niente DSP in tempo reale (WebGL): solo volumi.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        class Voice
        {
            public AudioSource src;
            public float level, target, speed = 1f;
        }

        Voice[] v;
        int cur;
        AudioSource stingerSrc;
        string currentId = "";
        float stingerDuck = 1f;
        float stingerUntil;
        bool paused;
        float pauseFactor = 1f;

        public string CurrentId { get { return currentId; } }

        void Awake()
        {
            v = new Voice[2];
            for (int i = 0; i < 2; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f;
                v[i] = new Voice { src = s };
            }
            stingerSrc = gameObject.AddComponent<AudioSource>();
            stingerSrc.playOnAwake = false; stingerSrc.loop = false; stingerSrc.spatialBlend = 0f;
        }

        /// <summary>Suona il brano "id" con crossfade. id vuoto = silenzio (dissolvenza in uscita).</summary>
        public void Play(string id, float fade = 2.5f)
        {
            id = id ?? "";
            if (id == currentId) return;
            currentId = id;
            // il vecchio brano esce
            var old = v[cur];
            old.target = 0f; old.speed = 1f / Mathf.Max(0.05f, fade);
            if (id.Length == 0) return;

            string asked = id;
            AudioLibrary.GetMusic(this, id, clip =>
            {
                if (clip == null || currentId != asked) return; // nel frattempo la scena è cambiata
                var next = v[1 - cur];
                if (next.src.isPlaying && next.target > 0f && next.src.clip == clip) return;
                // il nuovo entra sull'altra voce; quella che era corrente esce
                v[cur].target = 0f; v[cur].speed = 1f / Mathf.Max(0.05f, fade);
                cur = 1 - cur;
                var nv = v[cur];
                nv.src.clip = clip;
                nv.level = 0f; nv.target = 1f; nv.speed = 1f / Mathf.Max(0.05f, fade);
                nv.src.time = 0f;
                nv.src.Play();
            });
        }

        public void Stop(float fade = 1.5f) { Play("", fade); }

        /// <summary>Frammento musicale breve sopra il brano corrente (la musica si abbassa mentre suona).</summary>
        public void Stinger(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            AudioLibrary.GetMusic(this, id, clip =>
            {
                if (clip == null) return;
                stingerSrc.Stop();
                stingerSrc.clip = clip;
                stingerSrc.volume = StingerVolume();
                stingerSrc.Play();
                stingerUntil = Time.unscaledTime + clip.length;
            });
        }

        public void SetPaused(bool value) { paused = value; }

        float Gain()
        {
            var am = AudioManager.I;
            return am != null ? am.ChannelGain(AudioChannel.Musica) : 0.6f;
        }

        // lo stinger rispetta attesa del primo clic (WebGL), pausa e ducking delle voci come la musica
        float StingerVolume()
        {
            var am = AudioManager.I;
            if (am != null && am.WaitingForGesture) return 0f;
            return 0.9f * Gain() * (am != null ? am.MusicDuck : 1f) * pauseFactor;
        }

        void Update()
        {
            var am = AudioManager.I;
            bool blocked = am != null && am.WaitingForGesture;
            float master = Gain() * (am != null ? am.MusicDuck : 1f);
            pauseFactor = Mathf.MoveTowards(pauseFactor, paused ? 0.3f : 1f, Time.unscaledDeltaTime * 2f);
            bool stinging = Time.unscaledTime < stingerUntil;
            stingerDuck = Mathf.MoveTowards(stingerDuck, stinging ? 0.45f : 1f, Time.unscaledDeltaTime * 1.5f);
            if (stingerSrc.isPlaying) stingerSrc.volume = StingerVolume();

            for (int i = 0; i < 2; i++)
            {
                var x = v[i];
                x.level = Mathf.MoveTowards(x.level, x.target, Time.unscaledDeltaTime * x.speed);
                x.src.volume = blocked ? 0f : x.level * master * pauseFactor * stingerDuck;
                if (x.target <= 0f && x.level <= 0f && x.src.isPlaying) x.src.Stop();
            }
        }
    }
}
