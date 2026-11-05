using System;
using System.Collections;
using System.Collections.Generic;
using TrenoBambini.UI.Fx;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Libreria audio (id -> clip), come VoiceLibrary ma per Musica / Ambienti / Effetti.
    /// L'asset Resources/AudioLibrary.asset è aggiornato dal menu Treno ▸ Audio ▸ Aggiorna libreria, che indicizza
    /// Assets/Audio/{Musica,Ambienti,Effetti}: l'id è il nome del file senza suffisso numerico
    /// (passo_pietra_01.wav e passo_pietra_02.wav -> id passo_pietra, due varianti).
    /// Se per un id non c'è nessun file, si usa il suono sintetizzato dal codice (SfxSynth / MusicSynth): il gioco suona anche
    /// senza file, e basta aggiungere una registrazione vera con lo stesso id per sostituire quella sintetica.
    /// </summary>
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string categoria; // Musica | Ambienti | Effetti
            public List<AudioClip> clips = new List<AudioClip>();
        }

        public List<Entry> entries = new List<Entry>();

        static AudioLibrary instance;
        static bool searched;
        static Dictionary<string, Entry> map;
        static readonly Dictionary<string, AudioClip> synthCache = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, bool> synthBuilding = new Dictionary<string, bool>();
        static int lastVariant = -1;

        /// <summary>True mentre un brano sintetizzato è in costruzione (il frame può calare: non è colpa del PC).</summary>
        public static bool IsBuilding { get { return synthBuilding.Count > 0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null; searched = false; map = null;
            synthCache.Clear(); synthBuilding.Clear();
        }

        static void Init()
        {
            if (searched) return;
            searched = true;
            instance = Resources.Load<AudioLibrary>("AudioLibrary");
            map = new Dictionary<string, Entry>();
            if (instance == null) return;
            for (int i = 0; i < instance.entries.Count; i++)
            {
                var e = instance.entries[i];
                if (e != null && !string.IsNullOrEmpty(e.id) && e.clips != null && e.clips.Count > 0) map[e.id] = e;
            }
        }

        /// <summary>Una variante a caso (diversa dalla precedente se possibile) tra i file con questo id. Null se non ci sono file.</summary>
        public static AudioClip PickFile(string id)
        {
            Init();
            Entry e;
            if (string.IsNullOrEmpty(id) || !map.TryGetValue(id, out e)) return null;
            int n = e.clips.Count;
            int k = n == 1 ? 0 : UnityEngine.Random.Range(0, n);
            if (n > 1 && k == lastVariant) k = (k + 1) % n;
            lastVariant = k;
            return e.clips[k];
        }

        public static bool HasFile(string id)
        {
            Init();
            return !string.IsNullOrEmpty(id) && map.ContainsKey(id);
        }

        // ------------------------------------------------------------ effetti e ambienti (sintesi veloce, sincrona)
        static readonly Dictionary<string, Sfx> uiMap = new Dictionary<string, Sfx>
        {
            { "ui_click_ottone", Sfx.ClickBrass }, { "ui_click_carta", Sfx.ClickPaper }, { "ui_timbro", Sfx.Stamp },
            { "ui_macchina_scrivere", Sfx.TypeKey }, { "ui_valigia_apri", Sfx.SuitcaseOpen }, { "ui_valigia_chiudi", Sfx.SuitcaseClose },
            { "ui_sigillo", Sfx.ClickSeal }, { "ui_hover", Sfx.Hover }
        };

        /// <summary>Clip per un effetto/ambiente: prima i file, poi la sintesi (con varianti _1.._n per gli id base come passo_pietra).</summary>
        public static AudioClip GetSfx(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var f = PickFile(id);
            if (f != null) return f;

            Sfx ui;
            if (uiMap.TryGetValue(id, out ui)) return UISfx.GetClip(ui);

            string key = id;
            if (!SfxSynth.Has(id))
            {
                int n = SfxSynth.VariantCount(id);
                if (n <= 1) return null;
                int k = UnityEngine.Random.Range(1, n + 1);
                if (k == lastVariant) k = k % n + 1;
                lastVariant = k;
                key = id + "_" + k;
            }
            AudioClip c;
            if (!synthCache.TryGetValue(key, out c) || c == null)
            {
                c = SfxSynth.Make(key);
                synthCache[key] = c;
            }
            return c;
        }

        // ------------------------------------------------------------ musica (la sintesi è lunga: asincrona)
        /// <summary>Chiede un brano: file se esiste, altrimenti sintetizza a pezzi (un po' per frame) e chiama done(clip).</summary>
        public static void GetMusic(MonoBehaviour host, string id, Action<AudioClip> done)
        {
            if (string.IsNullOrEmpty(id)) { done(null); return; }
            var f = PickFile(id);
            if (f != null) { done(f); return; }
            AudioClip c;
            if (synthCache.TryGetValue(id, out c) && c != null) { done(c); return; }
            if (!MusicSynth.IsMusicId(id)) { done(null); return; }
            host.StartCoroutine(BuildMusic(id, done));
        }

        static IEnumerator BuildMusic(string id, Action<AudioClip> done)
        {
            // più richieste dello stesso brano: aspetta quella già in corso
            while (synthBuilding.ContainsKey(id)) yield return null;
            AudioClip c;
            if (synthCache.TryGetValue(id, out c) && c != null) { done(c); yield break; }
            synthBuilding[id] = true;
            var e = MusicSynth.Render(id, clip => { synthCache[id] = clip; });
            while (e.MoveNext()) yield return null;
            synthBuilding.Remove(id);
            AudioClip result;
            synthCache.TryGetValue(id, out result);
            done(result);
        }
    }
}
