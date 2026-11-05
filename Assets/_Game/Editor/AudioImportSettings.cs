using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Import dei file audio pre-generati (R002) in Assets/Audio/{Musica,Ambienti,Effetti}: mono, Vorbis, caricamento in background.
    /// Le opzioni sono applicate solo al primo import di ogni file (importSettingsMissing), così si possono ritoccare a mano.
    /// Dopo ogni importazione di file in quelle cartelle la libreria (Resources/AudioLibrary.asset) viene aggiornata da sola:
    /// senza libreria il gioco ripiega sulla sintesi.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        static bool InGameAudio(string path)
        {
            return path.StartsWith("Assets/Audio/Musica/") || path.StartsWith("Assets/Audio/Ambienti/") || path.StartsWith("Assets/Audio/Effetti/");
        }

        void OnPreprocessAudio()
        {
            if (!InGameAudio(assetPath) || !assetImporter.importSettingsMissing) return;
            var ai = (AudioImporter)assetImporter;
            bool breve = assetPath.StartsWith("Assets/Audio/Effetti/");
            ai.forceToMono = true;
            ai.loadInBackground = true;
            var s = ai.defaultSampleSettings;
            s.preloadAudioData = true;
            s.loadType = breve ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory; // WebGL: niente Streaming
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f; // i file sono già Vorbis a ~38 kbps: la ricodifica a questa qualità non li appesantisce
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; // 22050 Hz
            ai.defaultSampleSettings = s;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = false;
            foreach (var p in imported) if (InGameAudio(p) && Path.GetExtension(p).ToLowerInvariant() != ".meta") { touched = true; break; }
            if (!touched) foreach (var p in deleted) if (InGameAudio(p)) { touched = true; break; }
            if (touched) EditorApplication.delayCall += AudioTools.UpdateLibrary;
        }
    }
}
