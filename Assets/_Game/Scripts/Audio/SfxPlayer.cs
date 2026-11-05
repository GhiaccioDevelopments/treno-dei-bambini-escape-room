using TrenoBambini.Core;
using TrenoBambini.Data;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Effetti: pool di AudioSource (2D e 3D), varianti dello stesso id (file o sintesi), pitch ±5% e volume ±10%, limite di voci.
    /// Gli id ui_* passano di qui verso i suoni dell'interfaccia (R001).
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        const int Voices2D = 10;
        const int Voices3D = 6;

        AudioSource[] pool2D, pool3D;
        int n2D, n3D;

        void Awake()
        {
            pool2D = new AudioSource[Voices2D];
            for (int i = 0; i < Voices2D; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f;
                pool2D[i] = s;
            }
            pool3D = new AudioSource[Voices3D];
            for (int i = 0; i < Voices3D; i++)
            {
                var go = new GameObject("Sfx3D_" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 1.5f; s.maxDistance = 22f;
                pool3D[i] = s;
            }
        }

        float Gain()
        {
            var am = AudioManager.I;
            return am != null ? am.ChannelGain(AudioChannel.Effetti) : 0.9f;
        }

        bool Blocked { get { return AudioManager.I != null && AudioManager.I.WaitingForGesture; } }

        /// <summary>Effetto 2D (passi di Amerigo, UI, stinger brevi). pan -1..1.</summary>
        public void Play(string id, float vol = 1f, float pitchVar = 0.05f, float pan = 0f)
        {
            if (Blocked) return;
            var clip = AudioLibrary.GetSfx(id);
            if (clip == null) return;
            var s = pool2D[n2D]; n2D = (n2D + 1) % Voices2D;
            s.clip = clip;
            s.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            s.panStereo = pan;
            s.volume = Mathf.Clamp01(vol * Random.Range(0.9f, 1.1f) * Gain());
            s.Play();
        }

        /// <summary>Effetto 3D nel mondo (porte, oggetti).</summary>
        public void PlayAt(string id, Vector3 position, float vol = 1f, float pitchVar = 0.05f)
        {
            if (Blocked) return;
            var clip = AudioLibrary.GetSfx(id);
            if (clip == null) return;
            var s = pool3D[n3D]; n3D = (n3D + 1) % Voices3D;
            s.transform.position = position;
            s.clip = clip;
            s.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            s.volume = Mathf.Clamp01(vol * Random.Range(0.9f, 1.1f) * Gain());
            s.Play();
        }
    }
}
