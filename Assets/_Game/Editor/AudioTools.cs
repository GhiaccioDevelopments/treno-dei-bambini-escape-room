using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrenoBambini.Audio;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Strumenti per l'audio (R002): libreria dei file (registrazioni vere che sostituiscono i suoni sintetizzati),
    /// esportazione del Tema di Amerigo in WAV e della partitura ABC.
    /// </summary>
    public static class AudioTools
    {
        const string AudioRoot = "Assets/Audio";
        const string ResourcesFolder = "Assets/_Game/Resources";
        const string LibraryPath = "Assets/_Game/Resources/AudioLibrary.asset";
        static readonly string[] Categorie = { "Musica", "Ambienti", "Effetti" };
        static readonly string[] Extensions = { ".ogg", ".wav", ".mp3" };

        [MenuItem("Treno/Audio/Aggiorna libreria")]
        public static void UpdateLibrary()
        {
            ArtGenerator.EnsureFolder(ResourcesFolder);
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var found = new SortedDictionary<string, AudioLibrary.Entry>(StringComparer.Ordinal);
            foreach (var cat in Categorie)
            {
                string folder = AudioRoot + "/" + cat;
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Array.IndexOf(Extensions, Path.GetExtension(path).ToLowerInvariant()) < 0) continue;
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null) continue;
                    string id = BaseId(Path.GetFileNameWithoutExtension(path));
                    AudioLibrary.Entry e;
                    if (!found.TryGetValue(id, out e))
                    {
                        e = new AudioLibrary.Entry { id = id, categoria = cat };
                        found[id] = e;
                    }
                    e.clips.Add(clip);
                }
            }

            lib.entries = new List<AudioLibrary.Entry>(found.Values);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[AudioTools] Libreria audio aggiornata: " + lib.entries.Count + " id (" + LibraryPath + ").");
        }

        /// <summary>passo_pietra_01 -> passo_pietra (il suffisso numerico indica le varianti).</summary>
        static string BaseId(string name)
        {
            int i = name.LastIndexOf('_');
            if (i > 0 && i < name.Length - 1)
            {
                bool digits = true;
                for (int k = i + 1; k < name.Length; k++) if (!char.IsDigit(name[k])) { digits = false; break; }
                if (digits) return name.Substring(0, i);
            }
            return name;
        }

        [MenuItem("Treno/Audio/Esporta tema di Amerigo (WAV e partitura ABC)")]
        public static void ExportTheme()
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ArtSource/Audio");
            Directory.CreateDirectory(dir);
            string[] ids = { MusicSynth.Solo, MusicSynth.Ricordo, MusicSynth.Scoperta, MusicSynth.Vicolo, MusicSynth.StingerRisolto };
            foreach (var id in ids)
            {
                var clip = MusicSynth.RenderSync(id);
                if (clip == null) continue;
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                File.WriteAllBytes(Path.Combine(dir, id + ".wav"), ToWav(data, clip.channels, clip.frequency));
            }
            File.WriteAllText(Path.Combine(dir, "tema_amerigo.abc"), MusicSynth.AbcText(), new UTF8Encoding(false));
            Debug.Log("[AudioTools] Esportato in " + dir + " (i WAV non vanno committati: usare la cartella Assets/Audio/Musica solo per registrazioni finali .ogg).");
        }

        static byte[] ToWav(float[] data, int channels, int rate)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int bytes = data.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
                w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                for (int i = 0; i < data.Length; i++) w.Write((short)(Mathf.Clamp(data[i], -1f, 1f) * 32767f));
                return ms.ToArray();
            }
        }
    }
}
