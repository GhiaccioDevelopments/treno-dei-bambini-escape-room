using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.Dialogue
{
    /// <summary>Libreria delle voci (id battuta → clip). Asset in Resources/VoiceLibrary.asset, aggiornato dall'editor.</summary>
    [CreateAssetMenu(fileName = "VoiceLibrary", menuName = "Treno/Voice Library")]
    public class VoiceLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public AudioClip clip;
        }

        public List<Entry> entries = new List<Entry>();

        static VoiceLibrary instance;
        static bool searched;
        Dictionary<string, AudioClip> map;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; searched = false; }

        public static AudioClip Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!searched)
            {
                searched = true;
                instance = Resources.Load<VoiceLibrary>("VoiceLibrary");
            }
            if (instance == null) return null;
            if (instance.map == null)
            {
                instance.map = new Dictionary<string, AudioClip>();
                for (int i = 0; i < instance.entries.Count; i++)
                {
                    var e = instance.entries[i];
                    if (e != null && !string.IsNullOrEmpty(e.id) && e.clip != null) instance.map[e.id] = e.clip;
                }
            }
            AudioClip c;
            return instance.map.TryGetValue(id, out c) ? c : null;
        }
    }
}
