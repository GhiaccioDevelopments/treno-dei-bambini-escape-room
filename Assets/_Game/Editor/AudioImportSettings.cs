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
    /// R029: le musiche vere (registrazioni, Assets/Audio/Musica) hanno un marker loro: stereo, Streaming sul PC (i brani durano minuti),
    /// CompressedInMemory per WebGL e per gli stinger brevi (< 512 KB, devono partire subito). Il normalize dell'importer va lasciato spento
    /// a mano nel .meta: i file sono gia' livellati a -18 LUFS (titoli -16) e il normalize di picco romperebbe la coerenza.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        const string Marker = "r002";
        const string MarkerMusica = "r029b"; // r029b: override WebGL alleggerito (gh-pages accetta file sotto i 100 MB)

        static bool InGameAudio(string path)
        {
            return path.StartsWith("Assets/Audio/Musica/") || path.StartsWith("Assets/Audio/Ambienti/") || path.StartsWith("Assets/Audio/Effetti/");
        }

        void OnPreprocessAudio()
        {
            if (!InGameAudio(assetPath) || assetImporter.userData == Marker || assetImporter.userData == MarkerMusica) return; // userData = marker: importSettingsMissing non è affidabile con i .meta appena creati
            var ai = (AudioImporter)assetImporter;
            if (assetPath.StartsWith("Assets/Audio/Musica/")) { ImpostaMusica(ai); return; }
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
            ai.userData = Marker;
        }

        // R029: brani veri in stereo; Streaming solo per i file lunghi e solo fuori da WebGL
        void ImpostaMusica(AudioImporter ai)
        {
            long dimensione = 0;
            try { dimensione = new FileInfo(assetPath).Length; } catch (IOException) { }
            ai.forceToMono = false;
            ai.loadInBackground = true;
            var s = ai.defaultSampleSettings;
            s.preloadAudioData = true;
            s.loadType = dimensione > 512 * 1024 ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ai.defaultSampleSettings = s;
            var w = s;
            w.loadType = AudioClipLoadType.CompressedInMemory; // WebGL: niente Streaming
            w.quality = 0.15f; // WebGL: musica piu' compressa e a 22 kHz, il PC resta a piena qualita'
            w.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            w.sampleRateOverride = 22050;
            ai.SetOverrideSampleSettings("WebGL", w);
            ai.userData = MarkerMusica;
        }

        /// <summary>Reimporta tutte le musiche con le impostazioni attuali (serve quando cambia il marker o l'override WebGL).</summary>
        [MenuItem("Treno/Audio/Reimporta musiche")]
        public static void ReimportaMusiche()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Musica" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var ai = AssetImporter.GetAtPath(p) as AudioImporter;
                if (ai == null) continue;
                ai.userData = ""; // toglie il marker: OnPreprocessAudio riapplica le impostazioni
                ai.SaveAndReimport();
                n++;
            }
            Debug.Log("[Audio] Musiche reimportate: " + n);
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
