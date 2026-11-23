using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using TrenoBambini.Data;
using TrenoBambini.Dialogue;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Strumenti per il doppiaggio: libreria voci ed export del copione.
    /// </summary>
    public static class VoiceTools
    {
        const string VociFolder = "Assets/Audio/Voci";
        const string ResourcesFolder = "Assets/_Game/Resources";
        const string LibraryPath = "Assets/_Game/Resources/VoiceLibrary.asset";
        const string DataFolder = "Assets/_Game/Resources/Data";

        static readonly string[] AudioExtensions = { ".ogg", ".wav", ".mp3" };

        [MenuItem("Treno/Doppiaggio/Aggiorna libreria voci")]
        public static void UpdateLibrary()
        {
            ArtGenerator.EnsureFolder(VociFolder);
            ArtGenerator.EnsureFolder(ResourcesFolder);

            var lib = AssetDatabase.LoadAssetAtPath<VoiceLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<VoiceLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var found = new SortedDictionary<string, AudioClip>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { VociFolder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (Array.IndexOf(AudioExtensions, ext) < 0) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;
                string id = Path.GetFileNameWithoutExtension(path);
                if (found.ContainsKey(id))
                {
                    Debug.LogWarning("[VoiceTools] ID duplicato '" + id + "' (ignorato " + path + ").");
                    continue;
                }
                found[id] = clip;
            }

            lib.entries = new List<VoiceLibrary.Entry>();
            foreach (var kv in found)
                lib.entries.Add(new VoiceLibrary.Entry { id = kv.Key, clip = kv.Value });

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log("[VoiceTools] Libreria voci aggiornata: " + lib.entries.Count + " clip (" + LibraryPath + ").");
        }

        [MenuItem("Treno/Doppiaggio/Esporta copione CSV")]
        public static void ExportCsv()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string dataDir = Path.Combine(projectRoot, DataFolder);
            if (!Directory.Exists(dataDir))
            {
                Debug.LogError("[VoiceTools] Cartella dati non trovata: " + DataFolder);
                return;
            }

            // nomi personaggi
            var nomi = new Dictionary<string, string>();
            string persPath = Path.Combine(dataDir, "personaggi.json");
            if (File.Exists(persPath))
            {
                var pf = JsonUtility.FromJson<PersonaggiFile>(File.ReadAllText(persPath, Encoding.UTF8));
                if (pf != null && pf.personaggi != null)
                    foreach (var pd in pf.personaggi)
                        if (pd != null && !string.IsNullOrEmpty(pd.id))
                            nomi[pd.id] = !string.IsNullOrEmpty(pd.nomeCopione) ? pd.nomeCopione : (string.IsNullOrEmpty(pd.nome) ? pd.id : pd.nome);
            }

            // file audio esistenti (id -> nome file con estensione)
            var esistenti = new Dictionary<string, string>();
            string vociDir = Path.Combine(projectRoot, VociFolder);
            if (Directory.Exists(vociDir))
                foreach (var f in Directory.GetFiles(vociDir))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (Array.IndexOf(AudioExtensions, ext) < 0) continue;
                    esistenti[Path.GetFileNameWithoutExtension(f)] = Path.GetFileName(f);
                }

            var sb = new StringBuilder();
            sb.Append("ID;Personaggio;Testo;Emozione;FileAudio\r\n");
            int righe = 0;
            var files = Directory.GetFiles(dataDir, "cutscene_*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var cs = JsonUtility.FromJson<CutsceneDef>(File.ReadAllText(file, Encoding.UTF8));
                if (cs == null || cs.battute == null)
                {
                    Debug.LogWarning("[VoiceTools] Cutscene non valida: " + Path.GetFileName(file));
                    continue;
                }
                foreach (var b in cs.battute)
                {
                    if (b == null) continue;
                    string pers = b.personaggio ?? "";
                    string nome;
                    if (nomi.TryGetValue(pers, out nome)) pers = nome;

                    string audio = b.audio;
                    if (string.IsNullOrEmpty(audio))
                    {
                        string existing;
                        audio = esistenti.TryGetValue(b.id ?? "", out existing) ? existing : (b.id ?? "") + ".ogg";
                    }
                    else if (string.IsNullOrEmpty(Path.GetExtension(audio)))
                    {
                        string existing;
                        audio = esistenti.TryGetValue(audio, out existing) ? existing : audio + ".ogg";
                    }

                    sb.Append(Q(b.id)).Append(';')
                      .Append(Q(pers)).Append(';')
                      .Append(Q(b.testo)).Append(';')
                      .Append(Q(b.emozione)).Append(';')
                      .Append(Q(audio)).Append("\r\n");
                    righe++;
                }
            }

            string outDir = Path.Combine(projectRoot, "Doppiaggio");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "copione.csv");
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
            Debug.Log("[VoiceTools] Copione esportato: " + righe + " battute in " + outPath);
            EditorUtility.RevealInFinder(outPath);
        }

        static string Q(string s)
        {
            if (s == null) s = "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
