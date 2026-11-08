using System.Collections.Generic;
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
    /// Gestisce la grafica del gioco: preset (Bassa/Media/Alta/Ultra) e singole voci (R008, vedi <see cref="GraphicsOptions"/>).
    /// Scegliere un preset riscrive tutte le voci legate al preset; cambiare una voce porta a "Personalizzata". Tutto e' salvato in PlayerPrefs
    /// (<c>treno_grafica</c> = preset di base 0..3, <c>treno_grafica_pers</c> = personalizzata, <c>treno_g_&lt;chiave&gt;</c> = valore di ogni voce) e applicato subito.
    /// Le impostazioni del render pipeline si applicano a una COPIA a runtime dell'asset URP del livello (gli asset su disco non vengono modificati);
    /// le funzioni dei renderer (SSAO, decal, pellicola) si accendono/spengono e nell'editor sono rimesse a posto all'uscita dal Play.
    /// Tasto di debug F9 (solo editor/development build) = preset successivo.
    /// </summary>
    public static class GraphicsPresets
    {
        public const string PrefKey = "treno_grafica";
        const string CustomKey = "treno_grafica_pers";
        const string VoceKey = "treno_g_";
        const string GrainKey = "treno_grafica_grana";   // chiavi del 2.7, lette solo per migrare i salvataggi vecchi
        const string BloomKey = "treno_grafica_bloom";

        public static readonly string[] Names = { "Bassa", "Media", "Alta", "Ultra" };

        /// <summary>Preset di base 0..3 (resta quello di partenza anche quando e' "Personalizzata").</summary>
        public static int Current { get; private set; } = -1;
        public static bool Custom { get; private set; }
        /// <summary>Cresce a ogni modifica: chi dipende dalla grafica (meteo, dettagli) ricontrolla quando cambia.</summary>
        public static int Version { get; private set; }
        public static event System.Action<int> Changed;

        static readonly int N = GraphicsOptions.Defs.Length;
        static int[] _v = new int[GraphicsOptions.Defs.Length];
        static bool _loaded;

        /// <summary>Nome da mostrare: il preset oppure "Personalizzata".</summary>
        public static string DisplayName { get { return Custom ? "Personalizzata" : NameOf(Current); } }

        public static VoceDef Def(Voce v) { return GraphicsOptions.Defs[(int)v]; }

        static bool IsWeb { get { return Application.platform == RuntimePlatform.WebGLPlayer; } }
        /// <summary>Per le prove: simula WebGL (voci nascoste) anche altrove.</summary>
        public static bool SimulaWebGL;
        public static bool Web { get { return IsWeb || SimulaWebGL; } }

        /// <summary>La voce si mostra su questa piattaforma?</summary>
        public static bool Disponibile(Voce v)
        {
            var d = Def(v);
            if (d.soloDesktop && Web) return false;
            return true;
        }

        /// <summary>Quante opzioni ha la voce su questa piattaforma.</summary>
        public static int NumOpzioni(Voce v)
        {
            var d = Def(v);
            if (v == Voce.Risoluzione) return Mathf.Max(1, Risoluzioni().Count);
            return (Web && d.maxWeb > 0) ? d.maxWeb : d.opzioni.Length;
        }

        public static string TestoOpzione(Voce v, int i)
        {
            if (v == Voce.Risoluzione)
            {
                var r = Risoluzioni();
                if (r.Count == 0) return Screen.width + "x" + Screen.height;
                i = Mathf.Clamp(i, 0, r.Count - 1);
                return r[i].x + " x " + r[i].y;
            }
            var d = Def(v);
            return d.opzioni[Mathf.Clamp(i, 0, d.opzioni.Length - 1)];
        }

        // ------------------------------------------------------------------ valori

        /// <summary>Valore (indice di opzione) della voce.</summary>
        public static int Get(Voce v)
        {
            EnsureLoaded();
            if (v == Voce.Risoluzione) return RisoluzioneIndice();
            if (v == Voce.SchermoIntero) return Screen.fullScreen ? 1 : 0;
            return Mathf.Clamp(_v[(int)v], 0, NumOpzioni(v) - 1);
        }

        /// <summary>Imposta una voce: la salva, la applica subito e (se legata al preset) passa a "Personalizzata".</summary>
        public static void Set(Voce v, int valore)
        {
            EnsureLoaded();
            var d = Def(v);
            valore = Mathf.Clamp(valore, 0, NumOpzioni(v) - 1);
            if (v == Voce.Risoluzione) { ImpostaRisoluzione(valore); Notify(); return; }
            if (v == Voce.SchermoIntero) { ImpostaSchermoIntero(valore != 0); Notify(); return; }
            if (_v[(int)v] == valore) return;
            _v[(int)v] = valore;
            if (d.legataAlPreset) Custom = true;
            Save();
            ApplyAll();
        }

        static void Notify()
        {
            Version++;
            if (Changed != null) Changed(Current);
        }

        /// <summary>Grana pellicola attiva (compatibilita' con il 2.7).</summary>
        public static bool FilmGrainEnabled
        {
            get { return Get(Voce.Grana) != 0; }
            set { Set(Voce.Grana, value ? 1 : 0); }
        }

        /// <summary>Bagliore (bloom) attivo (compatibilita' con il 2.7).</summary>
        public static bool BloomEnabled
        {
            get { return Get(Voce.Bloom) != 0; }
            set { Set(Voce.Bloom, value ? 1 : 0); }
        }

        /// <summary>"Riduci effetti di movimento" (accessibilita').</summary>
        public static bool ReduceMotion { get { return Get(Voce.RiduciMovimento) != 0; } }

        static bool _cutsceneDof;

        /// <summary>R007: profondita' di campo accesa solo durante le cutscene (se la voce e' attiva). In gioco resta spenta.</summary>
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
            Custom = false;
            Version = 0;
            _cutsceneDof = false;
            _loaded = false;
            _v = new int[GraphicsOptions.Defs.Length];
            _clone = null; _cloneIndex = -1; _risoluzioni = null;
            _origFeatureActive.Clear(); _origDecalDist.Clear(); _origPipelines.Clear();
            _fogScene = null; _lastLevel = -1;
            ScenaDettagli.NuovaScena();
        }

        // ------------------------------------------------------------------ preset

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

        static void CopiaPreset(int tier)
        {
            for (int i = 0; i < N; i++)
            {
                var d = GraphicsOptions.Defs[i];
                if (d.legataAlPreset) _v[i] = d.preset[Mathf.Clamp(tier, 0, 3)];
            }
        }

        /// <summary>Imposta il preset (valore dell'enum 0..3, NON l'indice di piattaforma): riscrive tutte le voci legate al preset, lo salva e aggiorna camere e volumi.</summary>
        public static void Apply(int level, bool save = true)
        {
            if (QualitySettings.names.Length == 0) return;
            _loaded = true;
            int effective;
            ResolveLevel(level, out effective);
            Current = effective;
            Custom = false;
            CopiaPreset(effective);
            if (save) Save();
            ApplyAll();
        }

        public static void Apply(GraphicsPreset preset, bool save = true) { Apply((int)preset, save); }

        /// <summary>Tutto al minimo (misure e prove): preset Bassa con ogni voce al valore piu' economico. La pioggia resta leggera.</summary>
        public static void ApplyMinimum(bool save = false)
        {
            Apply(0, false);
            var minimi = new Dictionary<Voce, int>
            {
                { Voce.VSync, 0 }, { Voce.LimiteFps, 0 }, { Voce.ScalaRender, 0 }, { Voce.OmbreQualita, 0 }, { Voce.OmbreDistanza, 0 }, { Voce.OmbreCascate, 0 },
                { Voce.OmbreMorbide, 0 }, { Voce.OmbreLuci, 0 }, { Voce.LuciSecondarie, 0 }, { Voce.LuciMax, 0 }, { Voce.Riflessi, 0 }, { Voce.Antialiasing, 0 },
                { Voce.Ssao, 0 }, { Voce.Bloom, 0 }, { Voce.Grana, 0 }, { Voce.Vignetta, 0 }, { Voce.ProfonditaCampo, 0 }, { Voce.SfocaturaMovimento, 0 },
                { Voce.Decal, 0 }, { Voce.Nebbia, 0 }, { Voce.Pioggia, 1 }, { Voce.Spruzzi, 0 }, { Voce.Bagnato, 0 }, { Voce.Texture, 2 }, { Voce.Anisotropico, 0 },
                { Voce.Lod, 0 }, { Voce.Dettagli, 0 }
            };
            foreach (var kv in minimi) _v[(int)kv.Key] = kv.Value;
            Custom = true;
            if (save) Save();
            ApplyAll();
        }

        /// <summary>"Ripristina predefiniti": preset di default della piattaforma, schermo e contatore FPS compresi (non la risoluzione).</summary>
        public static void RipristinaPredefiniti()
        {
            EnsureLoaded();
            _v[(int)Voce.ContatoreFps] = 0;
            _v[(int)Voce.RiduciMovimento] = 0;
            Apply(DefaultLevel(), true);
        }

        public static string NameOf(int level)
        {
            return (level >= 0 && level < Names.Length) ? Names[level] : level.ToString();
        }

        /// <summary>Preset successivo tra quelli disponibili sulla piattaforma (ciclico).</summary>
        public static void Next()
        {
            EnsureLoaded();
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

        // ------------------------------------------------------------------ salvataggio

        static void EnsureLoaded()
        {
            if (_loaded) return;
            Load();
        }

        static void Load()
        {
            _loaded = true;
            int e;
            ResolveLevel(PlayerPrefs.GetInt(PrefKey, DefaultLevel()), out e);
            Current = e;
            CopiaPreset(e);
            // il valore dei campi non legati al preset ha sempre un default neutro
            Custom = PlayerPrefs.GetInt(CustomKey, 0) != 0;
            bool nuovo = PlayerPrefs.HasKey(VoceKey + "versione");
            if (!nuovo)
            {
                // salvataggio del 2.7 (solo preset + grana + bloom): migra, grana o bloom spenti = personalizzata
                if (PlayerPrefs.GetInt(GrainKey, 1) == 0) { _v[(int)Voce.Grana] = 0; Custom = true; }
                if (PlayerPrefs.GetInt(BloomKey, 1) == 0) { _v[(int)Voce.Bloom] = 0; Custom = true; }
            }
            else
            {
                for (int i = 0; i < N; i++)
                {
                    var d = GraphicsOptions.Defs[i];
                    if (d.id == Voce.Risoluzione || d.id == Voce.SchermoIntero) continue;
                    if (d.legataAlPreset && !Custom) continue;
                    _v[i] = PlayerPrefs.GetInt(VoceKey + d.chiave, _v[i]);
                }
            }
            // le voci non legate al preset (contatore FPS, accessibilita') si leggono sempre
            foreach (var d in GraphicsOptions.Defs)
                if (!d.legataAlPreset && d.id != Voce.Risoluzione && d.id != Voce.SchermoIntero)
                    _v[(int)d.id] = PlayerPrefs.GetInt(VoceKey + d.chiave, 0);
        }

        static void Save()
        {
            PlayerPrefs.SetInt(PrefKey, Mathf.Max(0, Current)); // enum, indipendente dagli indici della piattaforma
            PlayerPrefs.SetInt(CustomKey, Custom ? 1 : 0);
            PlayerPrefs.SetInt(VoceKey + "versione", 1);
            for (int i = 0; i < N; i++)
            {
                var d = GraphicsOptions.Defs[i];
                if (d.id == Voce.Risoluzione || d.id == Voce.SchermoIntero) continue;
                PlayerPrefs.SetInt(VoceKey + d.chiave, _v[i]);
            }
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------ schermo

        static List<Vector2Int> _risoluzioni;

        /// <summary>Risoluzioni disponibili (senza frequenze duplicate, almeno 800 px di larghezza), in ordine crescente.</summary>
        public static List<Vector2Int> Risoluzioni()
        {
            if (_risoluzioni != null) return _risoluzioni;
            var l = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 800 && !l.Contains(v)) l.Add(v);
            }
            if (l.Count == 0) l.Add(new Vector2Int(Screen.width, Screen.height));
            _risoluzioni = l;
            return l;
        }

        static int RisoluzioneIndice()
        {
            var l = Risoluzioni();
            int best = 0; long bd = long.MaxValue;
            for (int i = 0; i < l.Count; i++)
            {
                long d = Mathf.Abs(l[i].x - Screen.width) + Mathf.Abs(l[i].y - Screen.height);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        static void ImpostaRisoluzione(int idx)
        {
            var l = Risoluzioni();
            var r = l[Mathf.Clamp(idx, 0, l.Count - 1)];
            var mode = Screen.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Application.isEditor) { Debug.Log("[GraphicsPresets] Risoluzione " + r.x + "x" + r.y + " (ignorata nell'editor)"); return; }
            Screen.SetResolution(r.x, r.y, mode);
            PlayerPrefs.SetInt(VoceKey + "schermo_w", r.x); PlayerPrefs.SetInt(VoceKey + "schermo_h", r.y);
            PlayerPrefs.Save();
        }

        static void ImpostaSchermoIntero(bool pieno)
        {
            if (Application.isEditor) { Debug.Log("[GraphicsPresets] Schermo intero " + pieno + " (ignorato nell'editor)"); return; }
            if (pieno)
            {
                PlayerPrefs.SetInt(VoceKey + "finestra_w", Screen.width); PlayerPrefs.SetInt(VoceKey + "finestra_h", Screen.height);
                var m = Screen.currentResolution;
                Screen.SetResolution(m.width, m.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                int w = PlayerPrefs.GetInt(VoceKey + "finestra_w", 1280), h = PlayerPrefs.GetInt(VoceKey + "finestra_h", 720);
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
            PlayerPrefs.SetInt(VoceKey + "schermo", pieno ? 1 : 0);
            PlayerPrefs.Save();
        }

        static void RipristinaSchermoSalvato()
        {
            if (Application.isEditor || Web) return;
            if (!PlayerPrefs.HasKey(VoceKey + "schermo")) return;
            if (PlayerPrefs.GetInt(VoceKey + "schermo", 0) != 0)
            {
                var m = Screen.currentResolution;
                Screen.SetResolution(m.width, m.height, FullScreenMode.FullScreenWindow);
            }
            else if (PlayerPrefs.HasKey(VoceKey + "schermo_w"))
                Screen.SetResolution(PlayerPrefs.GetInt(VoceKey + "schermo_w"), PlayerPrefs.GetInt(VoceKey + "schermo_h"), FullScreenMode.Windowed);
        }

        // ------------------------------------------------------------------ applicazione

        static void ApplyAll()
        {
            ApplyPipeline();
            ApplyQualitySettings();
            ApplyToScene();
            Version++;
            if (Changed != null) Changed(Current);
        }

        // --- render pipeline: copia a runtime dell'asset del livello + funzioni dei renderer

        static UniversalRenderPipelineAsset _clone;
        static int _cloneIndex = -1;
        static int _lastLevel = -1;
        static readonly Dictionary<int, UnityEngine.Rendering.RenderPipelineAsset> _origPipelines = new Dictionary<int, UnityEngine.Rendering.RenderPipelineAsset>();
        static readonly Dictionary<ScriptableRendererFeature, bool> _origFeatureActive = new Dictionary<ScriptableRendererFeature, bool>();
        static readonly Dictionary<ScriptableRendererFeature, float> _origDecalDist = new Dictionary<ScriptableRendererFeature, float>();

        static void SetField(object o, string name, object value)
        {
            var f = o.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(o, value);
            else Debug.LogWarning("[GraphicsPresets] Campo URP non trovato: " + name);
        }

        /// <summary>Livello di QualitySettings da usare: il preset di base; Bassa passa a Media se serve un renderer con SSAO o decal (il renderer "leggero" non li ha).</summary>
        static int PipelineTier()
        {
            int tier = Mathf.Max(0, Current);
            if (tier == (int)GraphicsPreset.Bassa && !Web && (_v[(int)Voce.Ssao] != 0 || _v[(int)Voce.Decal] != 0) && IsAvailable((int)GraphicsPreset.Media))
                tier = (int)GraphicsPreset.Media;
            return tier;
        }

        static void ApplyPipeline()
        {
            if (QualitySettings.names.Length == 0) return;
            int eff;
            int index = ResolveLevel(PipelineTier(), out eff);
            bool livelloCambiato = QualitySettings.GetQualityLevel() != index;
            if (livelloCambiato) QualitySettings.SetQualityLevel(index, true);

            if (!_origPipelines.ContainsKey(index)) _origPipelines[index] = QualitySettings.GetRenderPipelineAssetAt(index);
            var src = _origPipelines[index] as UniversalRenderPipelineAsset;
            if (src == null) return; // pipeline non URP: nessuna regolazione
            if (_clone == null || _cloneIndex != index)
            {
                if (_clone != null) Object.Destroy(_clone);
                _clone = Object.Instantiate(src);
                _clone.hideFlags = HideFlags.HideAndDontSave;
                _clone.name = src.name + "_runtime";
                _cloneIndex = index;
                QualitySettings.renderPipeline = _clone;
            }
            else if (QualitySettings.renderPipeline != _clone) QualitySettings.renderPipeline = _clone;
            _lastLevel = index;

            var rp = _clone;
            int ombre = _v[(int)Voce.OmbreQualita];
            int aa = Mathf.Clamp(_v[(int)Voce.Antialiasing], 0, NumOpzioni(Voce.Antialiasing) - 1);
            rp.renderScale = GraphicsOptions.Scale[Mathf.Clamp(_v[(int)Voce.ScalaRender], 0, GraphicsOptions.Scale.Length - 1)];
            rp.msaaSampleCount = aa == 3 ? 2 : aa == 4 ? 4 : aa == 5 ? 8 : 1;
            rp.shadowDistance = ombre == 0 ? 0f : GraphicsOptions.DistanzaOmbre[Mathf.Clamp(_v[(int)Voce.OmbreDistanza], 0, 4)];
            rp.shadowCascadeCount = _v[(int)Voce.OmbreCascate] + 1;
            if (ombre > 0) rp.mainLightShadowmapResolution = GraphicsOptions.RisOmbre[ombre];
            SetField(rp, "m_MainLightShadowsSupported", ombre > 0);
            SetField(rp, "m_SoftShadowsSupported", _v[(int)Voce.OmbreMorbide] != 0);
            SetField(rp, "m_AdditionalLightShadowsSupported", _v[(int)Voce.OmbreLuci] != 0 && ombre > 0 && !Web);
            SetField(rp, "m_AdditionalLightsRenderingMode", _v[(int)Voce.LuciSecondarie] == 0 ? LightRenderingMode.Disabled : LightRenderingMode.PerPixel);
            rp.maxAdditionalLightsCount = GraphicsOptions.MaxLuci[Mathf.Clamp(_v[(int)Voce.LuciMax], 0, 3)];
            int rif = _v[(int)Voce.Riflessi];
            SetField(rp, "m_ReflectionProbeBlending", rif >= 1);
            SetField(rp, "m_ReflectionProbeBoxProjection", rif >= 2);

            ApplyFeatures(rp);
        }

        static IEnumerable<ScriptableRendererData> RendererDati(UniversalRenderPipelineAsset rp)
        {
            var campo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lista = campo != null ? campo.GetValue(rp) as ScriptableRendererData[] : null;
            if (lista == null) yield break;
            foreach (var rd in lista) if (rd != null) yield return rd;
        }

        /// <summary>SetActive solo se cambia (a raffica faceva andare in crash il DecalDrawSystem di URP) e ricordando il valore originale per l'editor.</summary>
        static void ImpostaFeature(ScriptableRendererFeature f, bool acceso)
        {
            if (f == null) return;
            if (!_origFeatureActive.ContainsKey(f)) _origFeatureActive[f] = f.isActive;
            if (f.isActive != acceso) f.SetActive(acceso);
        }

        static void ApplyFeatures(UniversalRenderPipelineAsset rp)
        {
            int decal = _v[(int)Voce.Decal];
            bool ssao = _v[(int)Voce.Ssao] != 0;
            foreach (var rd in RendererDati(rp))
                foreach (var f in rd.rendererFeatures)
                {
                    if (f == null) continue;
                    string tipo = f.GetType().Name;
                    if (tipo == "ScreenSpaceAmbientOcclusion") ImpostaFeature(f, ssao);
                    else if (tipo == "DecalRendererFeature")
                    {
                        ImpostaFeature(f, decal != 0);
                        ImpostaDistanzaDecal(f, decal == 1);
                    }
                }
        }

        static void ImpostaDistanzaDecal(ScriptableRendererFeature f, bool vicini)
        {
            try
            {
                var campo = f.GetType().GetField("m_Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (campo == null) return;
                var s = campo.GetValue(f);
                var md = s.GetType().GetField("maxDrawDistance");
                if (md == null) return;
                if (!_origDecalDist.ContainsKey(f)) _origDecalDist[f] = (float)md.GetValue(s);
                float voluta = vicini ? 15f : _origDecalDist[f];
                if (!Mathf.Approximately((float)md.GetValue(s), voluta)) md.SetValue(s, voluta);
            }
            catch (System.Exception e) { Debug.LogWarning("[GraphicsPresets] Distanza dei decal non regolabile: " + e.Message); }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void RegistraRipristinoEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += st =>
            {
                if (st != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
                // rimette negli asset i valori originali (nessun diff nei file del progetto)
                foreach (var kv in _origFeatureActive) if (kv.Key != null && kv.Key.isActive != kv.Value) kv.Key.SetActive(kv.Value);
                foreach (var kv in _origDecalDist) if (kv.Key != null) ImpostaDistanzaDecalAsset(kv.Key, kv.Value);
                int corrente = QualitySettings.GetQualityLevel();
                foreach (var kv in _origPipelines) { QualitySettings.SetQualityLevel(kv.Key, false); QualitySettings.renderPipeline = kv.Value; }
                QualitySettings.SetQualityLevel(corrente, false);
                _origFeatureActive.Clear(); _origDecalDist.Clear(); _origPipelines.Clear();
            };
        }

        static void ImpostaDistanzaDecalAsset(ScriptableRendererFeature f, float v)
        {
            var campo = f.GetType().GetField("m_Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var s = campo != null ? campo.GetValue(f) : null;
            var md = s != null ? s.GetType().GetField("maxDrawDistance") : null;
            if (md != null) md.SetValue(s, v);
        }
#endif

        // --- QualitySettings: sincronia, limite FPS, texture, filtro, LOD

        static bool _benchmarkLibero;

        /// <summary>Il benchmark tiene sincronia e limite FPS spenti.</summary>
        public static void BenchmarkSenzaLimiti(bool v) { _benchmarkLibero = v; ApplyQualitySettings(); }

        static void ApplyQualitySettings()
        {
            QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(_v[(int)Voce.Texture], 0, 2);
            QualitySettings.anisotropicFiltering = _v[(int)Voce.Anisotropico] == 0 ? AnisotropicFiltering.Disable
                : _v[(int)Voce.Anisotropico] == 1 ? AnisotropicFiltering.Enable : AnisotropicFiltering.ForceEnable;
            QualitySettings.lodBias = GraphicsOptions.LodBias[Mathf.Clamp(_v[(int)Voce.Lod], 0, 3)];
            if (_benchmarkLibero) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; return; }
            if (Web) { Application.targetFrameRate = -1; return; } // WebGL: il browser decide
            QualitySettings.vSyncCount = _v[(int)Voce.VSync] != 0 ? 1 : 0;
            Application.targetFrameRate = GraphicsOptions.Limiti[Mathf.Clamp(_v[(int)Voce.LimiteFps], 0, 3)];
        }

        // ------------------------------------------------------------------ camere e volumi

        /// <summary>R007 t3: la pellicola dei ricordi (gate weave, polvere, graffi) e' accesa solo nel ricordo 1946 (scena 30_FB_Scarpe); piu' forte a Alta/Ultra.</summary>
        static void ApplyPellicola()
        {
            bool ricordo = SceneManager.GetActiveScene().name.StartsWith("30_");
            float q = Current == (int)GraphicsPreset.Media ? 0.6f : 1f;
            Shader.SetGlobalFloat("_TrenoPellicola", ricordo ? 1f : 0f);
            Shader.SetGlobalFloat("_TrenoPellicolaQ", q);
            Shader.SetGlobalFloat("_TrenoPellicolaMov", ReduceMotion ? 1f : 0f); // R008: niente tremolio ne' sfarfallio
            ImpostaPassoPellicola(ricordo); // nell'editor fuori da Play resta acceso: non si modifica l'asset del renderer
        }

        /// <summary>Accende/spegne il passo a schermo intero "PellicolaRicordi" del renderer corrente: spento = nessuna copia del colore e nessun blit.</summary>
        static void ImpostaPassoPellicola(bool acceso)
        {
            try
            {
                var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (rp == null) return;
                foreach (var rd in RendererDati(rp))
                    foreach (var f in rd.rendererFeatures)
                        if (f != null && f.name == "PellicolaRicordi") ImpostaFeature(f, acceso);
            }
            catch (System.Exception e) { Debug.LogWarning("[GraphicsPresets] Passo pellicola non regolabile: " + e.Message); }
        }

        static string _fogScene;
        static bool _fogOriginale;

        static void ApplyFog()
        {
            string scena = SceneManager.GetActiveScene().name;
            if (_fogScene != scena) { _fogScene = scena; _fogOriginale = RenderSettings.fog; }
            RenderSettings.fog = _fogOriginale && _v[(int)Voce.Nebbia] != 0;
        }

        static void ApplyToScene()
        {
            ApplyPellicola();
            ApplyAntiAliasing();
            ApplyPostSwitches();
            ApplyFog();
            ScenaDettagli.Applica(_v[(int)Voce.Dettagli]);
        }

        /// <summary>FXAA/SMAA sulla camera; MSAA e' nell'asset URP (vedi ApplyPipeline).</summary>
        static void ApplyAntiAliasing()
        {
            int aa = _v[(int)Voce.Antialiasing];
            var mode = aa == 1 ? AntialiasingMode.FastApproximateAntialiasing : aa == 2 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null) continue;
                data.renderPostProcessing = true;
                data.antialiasing = mode;
                if (mode == AntialiasingMode.SubpixelMorphologicalAntiAliasing) data.antialiasingQuality = AntialiasingQuality.Medium;
            }
        }

        /// <summary>
        /// Post-processing (R007 t3, pellicola + R008). Ogni effetto e' una voce; la forza di grana e aberrazione e' scalata dal preset di base
        /// (grana 0,7 / 0,8 / 1 / 1,1; aberrazione 0 / 0,5 / 1 / 1,2) e il filtro del bloom e' di alta qualita' da Alta in su.
        /// La profondita' di campo e' predisposta nei profili ma accesa solo con <see cref="CutsceneDepthOfField"/>.
        /// </summary>
        static void ApplyPostSwitches()
        {
            bool grain = _v[(int)Voce.Grana] != 0;
            bool bloom = _v[(int)Voce.Bloom] != 0;
            bool vignetta = _v[(int)Voce.Vignetta] != 0;
            bool mblur = _v[(int)Voce.SfocaturaMovimento] != 0 && !ReduceMotion && !Web;
            float grainK = Current == (int)GraphicsPreset.Bassa ? 0.7f : Current == (int)GraphicsPreset.Media ? 0.8f : Current == (int)GraphicsPreset.Alta ? 1f : 1.1f;
            float caK = Current == (int)GraphicsPreset.Media ? 0.5f : Current == (int)GraphicsPreset.Alta ? 1f : Current == (int)GraphicsPreset.Ultra ? 1.2f : 0.5f;
            bool hq = Current >= (int)GraphicsPreset.Alta;
            bool dof = _v[(int)Voce.ProfonditaCampo] != 0 && _cutsceneDof;
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
                if (profile.TryGet(out vgn)) vgn.active = vignetta;
                ChromaticAberration cab;
                if (profile.TryGet(out cab))
                {
                    cab.active = vignetta;
                    ChromaticAberration bca;
                    if (vol.sharedProfile != null && vol.sharedProfile.TryGet(out bca)) cab.intensity.Override(bca.intensity.value * caK);
                }
                Bloom bl;
                if (profile.TryGet(out bl)) { bl.active = bloom; bl.highQualityFiltering.Override(hq); }
                DepthOfField df;
                if (profile.TryGet(out df)) df.active = dof;

                MotionBlur mb;
                if (!profile.TryGet(out mb))
                {
                    if (!mblur) continue; // niente componente = niente costo
                    mb = profile.Add<MotionBlur>(true);
                    mb.mode.Override(MotionBlurMode.CameraOnly);
                    mb.quality.Override(MotionBlurQuality.Low);
                    mb.intensity.Override(0.35f);
                    mb.clamp.Override(0.06f);
                }
                mb.active = mblur;
            }
        }

        // ------------------------------------------------------------------ avvio e debug

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            EnsureLoaded();
            ApplyAll();
            RipristinaSchermoSalvato();

            var go = new GameObject("GraphicsPresetsDriver");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Driver>();
        }

        sealed class Driver : MonoBehaviour
        {
            float acc; int frames; float fps;
            GUIStyle stile;

            void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

            void OnSceneLoaded(Scene s, LoadSceneMode m)
            {
                _fogScene = null; // nuova scena: la nebbia originale si rilegge
                ScenaDettagli.NuovaScena();
                ApplyToScene();
            }

            void Update()
            {
                acc += Time.unscaledDeltaTime; frames++;
                if (acc >= 0.5f) { fps = frames / acc; acc = 0f; frames = 0; }

                if (!Application.isEditor && !Debug.isDebugBuild) return; // F9 solo editor / development build
                var kb = Keyboard.current;
                if (kb == null || !kb.f9Key.wasPressedThisFrame) return;
                Next();
                string msg = "Grafica: " + NameOf(Current);
                Debug.Log("[GraphicsPresets] " + msg);
                if (UIRoot.I != null) UIRoot.I.Toast(msg, 1.5f);
            }

            void OnGUI()
            {
                if (_v[(int)Voce.ContatoreFps] == 0) return;
                if (stile == null)
                {
                    stile = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                    stile.normal.textColor = new Color(0.95f, 0.9f, 0.7f, 0.95f);
                }
                var r = new Rect(10f, 6f, 200f, 26f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), "FPS " + fps.ToString("0"), new GUIStyle(stile) { normal = { textColor = new Color(0f, 0f, 0f, 0.7f) } });
                GUI.Label(r, "FPS " + fps.ToString("0"), stile);
            }
        }
    }
}
