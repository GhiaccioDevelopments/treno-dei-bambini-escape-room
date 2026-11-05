using System.Collections.Generic;
using TrenoBambini.Core;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Ambienti: fino a 3 loop per scena con crossfade + suoni sparsi casuali (campane, gabbiani, carretto, venditore...)
    /// a intervalli casuali e posizione stereo casuale. Segue il canale Ambiente e il ducking sotto le voci.
    /// </summary>
    public class AmbientPlayer : MonoBehaviour
    {
        class Loop
        {
            public string id;
            public AudioSource src;
            public float level, target, speed = 0.5f;
        }

        readonly List<Loop> loops = new List<Loop>();
        readonly List<AudioSource> scatterPool = new List<AudioSource>();
        List<string> scatter = new List<string>();
        float minGap = 8f, maxGap = 20f;
        float nextScatter;
        string lastScatter = "";

        /// <summary>Imposta gli ambienti della scena. I loop non più presenti sfumano, quelli nuovi entrano.</summary>
        public void Set(List<string> loopIds, List<string> scatterIds, float[] interval)
        {
            scatter = scatterIds ?? new List<string>();
            if (interval != null && interval.Length >= 2) { minGap = Mathf.Max(2f, interval[0]); maxGap = Mathf.Max(minGap, interval[1]); }
            nextScatter = Time.unscaledTime + Random.Range(minGap * 0.4f, minGap);

            loopIds = loopIds ?? new List<string>();
            for (int i = 0; i < loops.Count; i++)
                if (!loopIds.Contains(loops[i].id)) { loops[i].target = 0f; loops[i].speed = 1f / 2f; }
            for (int i = 0; i < loopIds.Count; i++)
            {
                string id = loopIds[i];
                Loop existing = loops.Find(l => l.id == id);
                if (existing != null) { existing.target = 1f; existing.speed = 1f / 2f; continue; }
                var clip = AudioLibrary.GetSfx(id);
                if (clip == null) continue;
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.clip = clip;
                s.time = Random.value * clip.length * 0.5f;
                s.Play();
                loops.Add(new Loop { id = id, src = s, level = 0f, target = 1f, speed = 1f / 2.5f });
            }
        }

        public void Clear() { Set(null, null, null); }

        void Update()
        {
            var am = AudioManager.I;
            bool blocked = am != null && am.WaitingForGesture;
            float master = am != null ? am.ChannelGain(AudioChannel.Ambiente) * am.AmbientDuck : 0.7f;
            for (int i = loops.Count - 1; i >= 0; i--)
            {
                var l = loops[i];
                l.level = Mathf.MoveTowards(l.level, l.target, Time.unscaledDeltaTime * l.speed);
                l.src.volume = blocked ? 0f : l.level * master * 0.8f;
                if (l.target <= 0f && l.level <= 0f) { Destroy(l.src); loops.RemoveAt(i); }
            }

            if (blocked || scatter.Count == 0 || Time.unscaledTime < nextScatter) return;
            nextScatter = Time.unscaledTime + Random.Range(minGap, maxGap);
            string id = scatter[Random.Range(0, scatter.Count)];
            if (scatter.Count > 1 && id == lastScatter) id = scatter[(scatter.IndexOf(id) + 1) % scatter.Count];
            lastScatter = id;
            var clip = AudioLibrary.GetSfx(id);
            if (clip == null) return;
            var src = FreeScatterSource();
            src.clip = clip;
            src.panStereo = Random.Range(-0.8f, 0.8f);
            src.pitch = Random.Range(0.96f, 1.04f);
            src.volume = Random.Range(0.55f, 0.95f) * master;
            src.Play();
        }

        AudioSource FreeScatterSource()
        {
            for (int i = 0; i < scatterPool.Count; i++) if (!scatterPool[i].isPlaying) return scatterPool[i];
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = false; s.spatialBlend = 0f;
            scatterPool.Add(s);
            return s;
        }
    }
}
