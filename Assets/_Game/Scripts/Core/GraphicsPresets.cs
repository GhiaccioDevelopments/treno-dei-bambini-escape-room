using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TrenoBambini.UI;

namespace TrenoBambini.Core
{
    /// <summary>Preset grafici del gioco. L'indice corrisponde al livello in QualitySettings.</summary>
    public enum GraphicsPreset
    {
        Bassa = 0,
        Media = 1,
        Alta = 2,
        Ultra = 3
    }

    /// <summary>
    /// Gestisce i preset grafici (Bassa/Media/Alta/Ultra): applicazione, salvataggio in PlayerPrefs (<c>treno_grafica</c>),
    /// anti-aliasing per camera e interruttori di grana/bloom. Tasto di debug F9 (solo editor/development build) = preset successivo.
    /// Il menu Impostazioni (Fase 2.7) userà questa classe.
    /// </summary>
    public static class GraphicsPresets
    {
        public const string PrefKey = "treno_grafica";
        const string GrainKey = "treno_grafica_grana";
        const string BloomKey = "treno_grafica_bloom";

        public static readonly string[] Names = { "Bassa", "Media", "Alta", "Ultra" };

        public static int Current { get; private set; } = -1;
        public static event System.Action<int> Changed;

        /// <summary>Grana pellicola attiva (se il preset la consente).</summary>
        public static bool FilmGrainEnabled
        {
            get { return PlayerPrefs.GetInt(GrainKey, 1) != 0; }
            set { PlayerPrefs.SetInt(GrainKey, value ? 1 : 0); PlayerPrefs.Save(); ApplyPostSwitches(); }
        }

        /// <summary>Bagliore (bloom) attivo (se il preset lo consente).</summary>
        public static bool BloomEnabled
        {
            get { return PlayerPrefs.GetInt(BloomKey, 1) != 0; }
            set { PlayerPrefs.SetInt(BloomKey, value ? 1 : 0); PlayerPrefs.Save(); ApplyPostSwitches(); }
        }

        static bool _cutsceneDof;

        /// <summary>R007: profondita' di campo accesa solo durante le cutscene (da Media in su). In gioco resta spenta.</summary>
        public static bool CutsceneDepthOfField
        {
            get { return _cutsceneDof; }
            set { _cutsceneDof = value; ApplyPostSwitches(); }
        }

        /// <summary>Preset di default per la piattaforma: WebGL = Bassa, altrove = Alta.</summary>
        public static int DefaultLevel()
        {
            return Application.platform == RuntimePlatform.WebGLPlayer ? (int)GraphicsPreset.Bassa : (int)GraphicsPreset.Alta;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // domain reload disattivato: azzera lo stato statico a ogni avvio del play mode
            Changed = null;
            Current = -1;
            _cutsceneDof = false;
        }

        /// <summary>Indice in QualitySettings.names del preset (cercato per NOME); se manca (es. Alta/Ultra su WebGL)
        /// ripiega sul piu' alto disponibile minore o uguale. <paramref name="effective"/> e' il preset realmente usato.</summary>
        static int ResolveLevel(int preset, out int effective)
        {
            var names = QualitySettings.names;
            for (int p = Mathf.Clamp(preset, 0, Names.Length - 1); p >= 0; p--)
            {
                int idx = System.Array.IndexOf(names, Names[p]);
                if (idx >= 0) { effective = p; return idx; }
            }
            effective = 0;
            return 0;
        }

        static bool IsAvailable(int preset)
        {
            return System.Array.IndexOf(QualitySettings.names, Names[preset]) >= 0;
        }

        /// <summary>Imposta il preset (valore dell'enum 0..3, NON l'indice di piattaforma), lo salva e aggiorna camere e volumi.</summary>
        public static void Apply(int level, bool save = true)
        {
            if (QualitySettings.names.Length == 0) return;
            int effective;
            int index = ResolveLevel(level, out effective);
            QualitySettings.SetQualityLevel(index, true);
            Current = effective;
            if (save)
            {
                PlayerPrefs.SetInt(PrefKey, effective); // enum, indipendente dagli indici della piattaforma
                PlayerPrefs.Save();
            }
            ApplyToScene();
            if (Changed != null) Changed(level);
        }

        public static void Apply(GraphicsPreset preset, bool save = true) { Apply((int)preset, save); }

        public static string NameOf(int level)
        {
            return (level >= 0 && level < Names.Length) ? Names[level] : level.ToString();
        }

        /// <summary>Preset successivo tra quelli disponibili sulla piattaforma (ciclico).</summary>
        public static void Next()
        {
            int cur = Current;
            if (cur < 0)
            {
                int e;
                ResolveLevel(PlayerPrefs.GetInt(PrefKey, DefaultLevel()), out e);
                cur = e;
            }
            for (int step = 1; step <= Names.Length; step++)
            {
                int p = (cur + step) % Names.Length;
                if (IsAvailable(p)) { Apply(p); return; }
            }
        }

        // ------------------------------------------------------------------ camere e volumi

        /// <summary>R007 t3: la pellicola dei ricordi (gate weave, polvere, graffi) e' accesa solo nel ricordo 1946 (scena 30_FB_Scarpe); piu' forte a Alta/Ultra.</summary>
        static void ApplyPellicola()
        {
            bool ricordo = SceneManager.GetActiveScene().name.StartsWith("30_");
            float q = Current == (int)GraphicsPreset.Media ? 0.6f : 1f;
            Shader.SetGlobalFloat("_TrenoPellicola", ricordo ? 1f : 0f);
            Shader.SetGlobalFloat("_TrenoPellicolaQ", q);
            ImpostaPassoPellicola(ricordo); // nell'editor fuori da Play resta acceso: non si modifica l'asset del renderer
        }

        /// <summary>Accende/spegne il passo a schermo intero "PellicolaRicordi" del renderer corrente: spento = nessuna copia del colore e nessun blit.</summary>
        static void ImpostaPassoPellicola(bool acceso)
        {
            try
            {
                var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (rp == null) return;
                var campo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lista = campo != null ? campo.GetValue(rp) as ScriptableRendererData[] : null;
                if (lista == null) return;
                foreach (var rd in lista)
                {
                    if (rd == null) continue;
                    foreach (var f in rd.rendererFeatures)
                        if (f != null && f.name == "PellicolaRicordi" && f.isActive != acceso) f.SetActive(acceso); // solo se cambia: SetActive a raffica faceva andare in crash il DecalDrawSystem di URP
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[GraphicsPresets] Passo pellicola non regolabile: " + e.Message); }
        }

        static void ApplyToScene()
        {
            ApplyPellicola();
            ApplyAntiAliasing();
            ApplyPostSwitches();
        }

        /// <summary>Media = FXAA sulla camera; Bassa = nessuno; Alta/Ultra usano l'MSAA dell'asset URP.</summary>
        static void ApplyAntiAliasing()
        {
            var mode = Current == (int)GraphicsPreset.Media ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null) continue;
                data.renderPostProcessing = true;
                data.antialiasing = mode;
            }
        }

        /// <summary>
        /// Post-processing per preset (R007 t3, pellicola). Bassa (renderer Lite, WebGL): solo grana (scalata 0,7) e curva tonale, senza bloom, vignetta,
        /// aberrazione ne' SSAO; Media/Alta/Ultra: + alone (bloom), vignetta e aberrazione cromatica scalata per preset (0,5 / 1 / 1,2), grana 0,8 / 1 / 1,1;
        /// Alta/Ultra: bloom ad alta qualita' di filtro. Grana e bloom restano interruttori dell'utente.
        /// La profondita' di campo e' predisposta nei profili ma accesa solo con <see cref="CutsceneDepthOfField"/>.
        /// </summary>
        static void ApplyPostSwitches()
        {
            // R007 t3 (pellicola): Bassa = solo grana e curva tonale (renderer Lite, WebGL); Media 0,8 / Alta 1 / Ultra 1,1 sulla grana;
            // alone (bloom), vignetta e aberrazione solo da Media in su, aberrazione scalata per preset.
            bool lite = Current == (int)GraphicsPreset.Bassa;
            bool grain = FilmGrainEnabled;
            bool bloom = !lite && BloomEnabled;
            float grainK = Current == (int)GraphicsPreset.Bassa ? 0.7f : Current == (int)GraphicsPreset.Media ? 0.8f : Current == (int)GraphicsPreset.Alta ? 1f : 1.1f;
            float caK = Current == (int)GraphicsPreset.Media ? 0.5f : Current == (int)GraphicsPreset.Alta ? 1f : Current == (int)GraphicsPreset.Ultra ? 1.2f : 0f;
            bool hq = Current >= (int)GraphicsPreset.Alta;
            bool dof = !lite && _cutsceneDof;
            foreach (var vol in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
            {
                var profile = vol.profile; // istanza: non modifica l'asset
                if (profile == null) continue;
                FilmGrain fg;
                if (profile.TryGet(out fg))
                {
                    fg.active = grain;
                    FilmGrain bfg;
                    if (vol.sharedProfile != null && vol.sharedProfile.TryGet(out bfg)) fg.intensity.Override(Mathf.Clamp01(bfg.intensity.value * grainK));
                }
                Vignette vgn;
                if (profile.TryGet(out vgn)) vgn.active = !lite;
                ChromaticAberration cab;
                if (profile.TryGet(out cab))
                {
                    cab.active = !lite;
                    ChromaticAberration bca;
                    if (vol.sharedProfile != null && vol.sharedProfile.TryGet(out bca)) cab.intensity.Override(bca.intensity.value * caK);
                }
                Bloom bl;
                if (profile.TryGet(out bl)) { bl.active = bloom; bl.highQualityFiltering.Override(hq); }
                DepthOfField df;
                if (profile.TryGet(out df)) df.active = dof;
            }
        }

        // ------------------------------------------------------------------ avvio e debug

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            int level = PlayerPrefs.GetInt(PrefKey, DefaultLevel());
            Apply(level, false);

            var go = new GameObject("GraphicsPresetsDriver");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Driver>();
        }

        sealed class Driver : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

            void OnSceneLoaded(Scene s, LoadSceneMode m) { ApplyToScene(); }

            void Update()
            {
                if (!Application.isEditor && !Debug.isDebugBuild) return; // F9 solo editor / development build
                var kb = Keyboard.current;
                if (kb == null || !kb.f9Key.wasPressedThisFrame) return;
                Next();
                string msg = "Grafica: " + NameOf(Current);
                Debug.Log("[GraphicsPresets] " + msg);
                if (UIRoot.I != null) UIRoot.I.Toast(msg, 1.5f);
            }
        }
    }
}
