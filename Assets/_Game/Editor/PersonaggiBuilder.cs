using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Personaggi (Fase 2.5): AnimatorController di Amerigo (blend tree idle/cammina/corri su Speed) e collegamento
    /// delle clip Mixamo (Assets/Art/Animations/Amerigo/). Senza clip usa segnaposto in Assets/Art/Animations/Amerigo/Segnaposto.
    /// Menu: Treno/Personaggi/...
    /// </summary>
    public static class PersonaggiBuilder
    {
        public const string AmerigoFolder = "Assets/Art/Characters/Amerigo";
        public const string AmerigoFbx = AmerigoFolder + "/Amerigo.fbx";
        public const string AmerigoController = AmerigoFolder + "/Amerigo.controller";
        public const string AnimFolder = "Assets/Art/Animations/Amerigo";
        const float RootHeightPlaceholder = 0.55f; // altezza anca di un bambino di 1,2 m (unità avatar)
        const string PlaceholderFolder = AnimFolder + "/Segnaposto";

        // soglie del blend tree: Speed01 mappato in ThirdPersonController (costante condivisa col runtime)
        const float WalkThreshold = TrenoBambini.Player.ThirdPersonController.WalkBlendThreshold;
        const string LfsMarker = "version https://git-lfs";

        [MenuItem("Treno/Personaggi/Aggiorna animazioni")]
        public static void AggiornaAnimazioni()
        {
            Directory.CreateDirectory(AmerigoFolder);
            Directory.CreateDirectory(AnimFolder);
            AssetDatabase.Refresh();

            var idle = Find("Idle");
            var walk = Find("Walk");
            var run = Find("Run");
            var found = new List<string>();
            var missing = new List<string>();
            idle = idle ?? Placeholder("Idle", found, missing, true);
            walk = walk ?? Placeholder("Walk", found, missing, true);
            run = run ?? Placeholder("Run", found, missing, true);
            if (Find("Idle") != null) found.Add("Idle");
            if (Find("Walk") != null) found.Add("Walk");
            if (Find("Run") != null) found.Add("Run");

            // il controller è generato (riproducibile): se esiste lo svuotiamo e lo ricostruiamo NELLO STESSO asset,
            // così il GUID resta stabile e i prefab/scene che lo referenziano non perdono il collegamento
            var ctrl = File.Exists(AmerigoController) ? AssetDatabase.LoadAssetAtPath<AnimatorController>(AmerigoController) : null;
            if (ctrl != null)
            {
                ctrl.layers = new AnimatorControllerLayer[0];
                ctrl.parameters = new AnimatorControllerParameter[0];
                // sotto-asset orfani (state machine, stati, blend tree della versione precedente)
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(AmerigoController))
                    if (sub != null && sub != ctrl) Object.DestroyImmediate(sub, true);
                ctrl.AddLayer("Base Layer");
            }
            else ctrl = AnimatorController.CreateAnimatorControllerAtPath(AmerigoController);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            var ps = ctrl.parameters;
            for (int i = 0; i < ps.Length; i++) if (ps[i].name == "MotionSpeed") ps[i].defaultFloat = 1f;
            ctrl.parameters = ps;

            var sm = ctrl.layers[0].stateMachine;
            BlendTree tree;
            var state = ctrl.CreateBlendTreeInController("Locomozione", out tree, 0);
            tree.blendParameter = "Speed";
            tree.blendType = BlendTreeType.Simple1D;
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, WalkThreshold);
            tree.AddChild(run, 1f);
            state.speedParameterActive = true;
            state.speedParameter = "MotionSpeed";
            sm.defaultState = state;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // eventuali altre clip (non ancora usate dal gioco): solo segnalate
            foreach (var extra in new[] { "Idle2", "TurnL", "TurnR", "PickUp", "Interact" })
                if (Find(extra) == null) missing.Add(extra); else found.Add(extra);

            Debug.Log("[Personaggi] AnimatorController Amerigo aggiornato. Clip Mixamo trovate: " + string.Join(", ", found)
                + " | mancanti/segnaposto: " + string.Join(", ", missing));
            AssignControllerToPrefab();
        }

        // cerca Amerigo_<nome>.fbx e ne prende la prima clip non di preview
        static AnimationClip Find(string nome)
        {
            string path = AnimFolder + "/Amerigo_" + nome + ".fbx";
            if (!File.Exists(path)) return null;
            ConfigureClipImport(path, nome);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var c = o as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        // le clip Mixamo si importano Humanoid; loop per idle/cammina/corri
        static void ConfigureClipImport(string path, string nome)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; changed = true; }
            // le clip Mixamo hanno lo scheletro mixamorig:*: l'avatar è creato dal loro modello (Copy From Other fallirebbe
            // con "No human bone found"); il retargeting Humanoid le applica comunque ad Amerigo.
            if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            bool loop = nome == "Idle" || nome == "Idle2" || nome == "Walk" || nome == "Run";
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                if (c.loopTime != loop) { c.loopTime = loop; changed = true; }
                if (!c.lockRootRotation || !c.lockRootHeightY || !c.lockRootPositionXZ || !c.keepOriginalPositionY
                    || !c.keepOriginalOrientation || !c.keepOriginalPositionXZ) changed = true;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true; // rotazione, altezza e XZ: Original
            }
            imp.clipAnimations = clips;
            if (changed) imp.SaveAndReimport();
        }

        static AnimationClip Placeholder(string nome, List<string> found, List<string> missing, bool loop)
        {
            Directory.CreateDirectory(PlaceholderFolder);
            string path = PlaceholderFolder + "/Segnaposto_" + nome + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Segnaposto_" + nome, frameRate = 30f };
                // clip umanoide in posa neutra (muscoli a 0, radice a altezza anca): l'avatar resta in piedi,
                // nessuna animazione fittizia. Una curva generica farebbe collassare lo scheletro Humanoid.
                var tipo = typeof(Animator);
                foreach (var m in UnityEngine.HumanTrait.MuscleName)
                    clip.SetCurve("", tipo, m, AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootT.x", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootT.y", AnimationCurve.Constant(0f, 1f, RootHeightPlaceholder));
                clip.SetCurve("", tipo, "RootT.z", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootQ.x", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootQ.y", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootQ.z", AnimationCurve.Constant(0f, 1f, 0f));
                clip.SetCurve("", tipo, "RootQ.w", AnimationCurve.Constant(0f, 1f, 1f));
                var s = AnimationUtility.GetAnimationClipSettings(clip);
                s.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, s);
                AssetDatabase.CreateAsset(clip, path);
            }
            missing.Add(nome + " (segnaposto)");
            return clip;
        }

        static void AssignControllerToPrefab()
        {
            AssignControllerToPrefab(Bambino);
            AssignControllerToPrefab(Adulto);
        }

        /// <summary>Descrizione di uno dei due Amerigo (stesso schema di import; cartella e prefisso dei nomi diversi).</summary>
        sealed class Pers
        {
            public string Nome;            // prefisso di FBX, texture, materiali, prefab
            public string Folder;
            public bool HairBande;         // atlas capelli a 16 bande orizzontali (adulto: 8 ramato in basso, 8 castano in alto) invece che a due metà in U
            public bool VarianteCapelli;   // il bambino usa l'albedo capelli ricolorata (HairSplit); anche l'adulto (16 bande orizzontali)
            public string Fbx { get { return Folder + "/" + Nome + ".fbx"; } }
            public string Prefab { get { return Folder + "/" + Nome + ".prefab"; } }
            public string TexF { get { return Folder + "/Textures"; } }
            public string MatF { get { return Folder + "/Materials"; } }
            public string HairAlbedo { get { return VarianteCapelli ? Nome + "_HairSplit_albedo" : Nome + "_Hair_albedo"; } }
        }

        public const string AdultoFolder = "Assets/Art/Characters/AmerigoAdulto";
        static readonly Pers Bambino = new Pers { Nome = "Amerigo", Folder = AmerigoFolder, VarianteCapelli = true };
        static readonly Pers Adulto = new Pers { Nome = "AmerigoAdulto", Folder = AdultoFolder, VarianteCapelli = true, HairBande = true };

        static void AssignControllerToPrefab(Pers p)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AmerigoController);
            var prefabPath = p.Prefab;
            if (!File.Exists(prefabPath) || ctrl == null) return;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ------------------------------------------------------------------ import FBX di Amerigo

        [MenuItem("Treno/Personaggi/Configura import Amerigo")]
        public static void ConfiguraImportAmerigo() { ConfiguraImport(Bambino); }

        [MenuItem("Treno/Personaggi/Configura import Amerigo adulto")]
        public static void ConfiguraImportAmerigoAdulto() { ConfiguraImport(Adulto); }

        static void ConfiguraImport(Pers p)
        {
            if (!File.Exists(p.Fbx)) { Debug.LogError("[Personaggi] Manca " + p.Fbx); return; }
            if (EPuntatoreLfs(p.Fbx)) { Debug.LogError("[Personaggi] " + p.Fbx + " è un puntatore Git LFS, non il file vero: esegui 'git lfs pull'."); return; }
            if (Directory.Exists(p.TexF))
                foreach (var f in Directory.GetFiles(p.TexF, "*.png"))
                    if (EPuntatoreLfs(f)) { Debug.LogError("[Personaggi] " + f.Replace("\\", "/") + " è un puntatore Git LFS, non il file vero: esegui 'git lfs pull'."); return; }
            if (p.VarianteCapelli) CreaVarianteCapelli(p);
            ConfiguraTexture(p);
            var imp = AssetImporter.GetAtPath(p.Fbx) as ModelImporter;
            var slotNomi = LeggiNomiSlot(p, imp);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(p.Fbx);
            Debug.Log("[Personaggi] " + p.Nome + ".fbx Humanoid, avatar valido: " + (avatar != null && avatar.isValid && avatar.isHuman));

            var mats = CreaMateriali(p);

            // prefab con Animator, materiali URP e LODGroup (LOD0/1/2)
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(p.Fbx);
            if (model == null) { Debug.LogError("[Personaggi] Impossibile caricare il GameObject di " + p.Fbx); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
            inst.name = p.Nome;
            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            var lods = new Renderer[3];
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int k = r.name.EndsWith("LOD0") ? 0 : r.name.EndsWith("LOD1") ? 1 : r.name.EndsWith("LOD2") ? 2 : -1;
                if (k < 0) continue;
                lods[k] = r;
                // materiali assegnati per NOME dello slot dell'FBX (Skin/Face/Clothes/Hair), mai per ordine fisso
                var slots = new Material[slotNomi.Length];
                for (int i = 0; i < slots.Length; i++) slots[i] = TrovaMateriale(mats, slotNomi[i], p, i);
                r.sharedMaterials = slots;
                r.updateWhenOffscreen = false;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            var lg = inst.GetComponent<LODGroup>();
            if (lg == null) lg = inst.AddComponent<LODGroup>();
            var lodArr = new LOD[3];
            float[] soglie = { 0.45f, 0.18f, 0.04f };
            for (int i = 0; i < 3; i++) lodArr[i] = new LOD(soglie[i], lods[i] != null ? new[] { lods[i] } : new Renderer[0]);
            lg.SetLODs(lodArr);
            lg.fadeMode = LODFadeMode.None;
            lg.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(inst, p.Prefab);
            }
            finally { Object.DestroyImmediate(inst); }
            if (p == Bambino) AggiornaAnimazioni();      // rigenera il controller e lo assegna a entrambi i prefab
            else AssignControllerToPrefab(p);            // l'adulto usa lo stesso controller del bambino
        }

        static readonly string[] OrdineSlot = { "Skin", "Face", "Clothes", "Hair" };

        /// <summary>Nome slot normalizzato: senza suffisso ".NNN" e senza prefisso del personaggio (es. "AmerigoAdulto_Hair.001" -> "Hair").</summary>
        static string NormalizzaSlot(string nm, Pers p)
        {
            if (string.IsNullOrEmpty(nm)) return "";
            nm = System.Text.RegularExpressions.Regex.Replace(nm, @"\.\d+$", "");
            foreach (var pre in new[] { p.Nome + "_", "AmerigoAdulto_", "Amerigo_" })
                if (nm.StartsWith(pre)) { nm = nm.Substring(pre.Length); break; }
            return nm;
        }

        static Material TrovaMateriale(Dictionary<string, Material> mats, string slotNome, Pers p, int indice)
        {
            Material m;
            if (mats.TryGetValue(NormalizzaSlot(slotNome, p), out m) && m != null) return m;
            Material fb = null;
            if (indice < OrdineSlot.Length) mats.TryGetValue(OrdineSlot[indice], out fb);
            Debug.LogWarning("[Personaggi] " + p.Nome + ": slot materiale '" + slotNome + "' (indice " + indice
                + ") non riconosciuto; uso per ordine " + (indice < OrdineSlot.Length ? OrdineSlot[indice] : "(nessuno)"));
            return fb;
        }

        // vero se il file è un puntatore Git LFS non scaricato (testo "version https://git-lfs...")
        static bool EPuntatoreLfs(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var buf = new byte[LfsMarker.Length];
                    int n = fs.Read(buf, 0, buf.Length);
                    return n == buf.Length && System.Text.Encoding.ASCII.GetString(buf) == LfsMarker;
                }
            }
            catch (System.Exception) { return false; }
        }

        static double Satur(Color32 c)
        {
            int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return mx > 0 ? (mx - mn) / (double)mx : 0.0;
        }

        /// <summary>
        /// Nomi degli slot materiale dell'FBX nell'ordine reale dei sottomesh (es. Skin, Face, Clothes, Hair), letti
        /// importando temporaneamente i materiali dell'FBX: l'ordine cambia da un modello all'altro, quindi mai fisso.
        /// </summary>
        static string[] LeggiNomiSlot(Pers p, ModelImporter imp)
        {
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();
            var g = AssetDatabase.LoadAssetAtPath<GameObject>(p.Fbx);
            string[] res = null;
            foreach (var r in g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!r.name.EndsWith("LOD0")) continue;
                var ms = r.sharedMaterials;
                res = new string[ms.Length];
                for (int i = 0; i < ms.Length; i++)
                {
                    string nm = ms[i] != null ? ms[i].name : "";
                    res[i] = nm.StartsWith(p.Nome + "_") ? nm.Substring(p.Nome.Length + 1) : nm;
                }
                break;
            }
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            Debug.Log("[Personaggi] " + p.Nome + " slot materiali FBX: " + (res == null ? "?" : string.Join(", ", res)));
            return res ?? new[] { "Skin", "Face", "Clothes", "Hair" };
        }

        static void ConfiguraTexture(Pers p)
        {
            if (!Directory.Exists(p.TexF)) { Debug.LogWarning("[Personaggi] Manca la cartella texture " + p.TexF); return; }
            foreach (var f in Directory.GetFiles(p.TexF, "*.png"))
            {
                string path = f.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                bool normal = n.EndsWith("_normal");
                bool lineare = normal || n.EndsWith("_roughness") || n.EndsWith("_ao") || n.EndsWith("_metalgloss");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !lineare;
                bool capelli = n == p.Nome + "_Hair_albedo" || n == p.Nome + "_HairSplit_albedo";
                bool conAlpha = capelli || n.EndsWith("_metalgloss");
                ti.alphaSource = conAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.alphaIsTransparency = capelli;
                ti.mipmapEnabled = true;
                // capelli: il bordo tra le due metà dell'atlas non deve sbavare nei mip, e le ciocche (alpha clip) restano piene
                ti.mipMapsPreserveCoverage = capelli;
                ti.alphaTestReferenceValue = 0.5f;
                // bambino: atlas a due metà (Clamp). Adulto: atlas a 16 bande orizzontali, il wrap U resta Repeat
                ti.wrapMode = capelli && p.VarianteCapelli && !p.HairBande ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                ti.maxTextureSize = 1024;
                ti.textureCompression = capelli ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
        }


        /// <summary>
        /// Variante dell'albedo dei capelli con divisione molto netta, leggibile anche sotto LUT seppia e luce calda
        /// (che azzerano la tinta e lasciano solo la luminanza): metà sinistra dell'atlas (u<0.5, lato sinistro del
        /// personaggio) = castano scuro quasi neutro, metà destra (lato destro del personaggio) = rosso ramato naturale. Il dettaglio delle ciocche (luminanza relativa e alpha)
        /// viene dall'albedo originale; l'originale resta intatto.
        /// </summary>
        static void CreaVarianteCapelli(Pers p)
        {
            string src = p.TexF + "/" + p.Nome + "_Hair_albedo.png";
            string dst = p.TexF + "/" + p.Nome + "_HairSplit_albedo.png";
            if (!File.Exists(src)) return;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            bool okImg = t.LoadImage(File.ReadAllBytes(src));
            int w = t.width, h = t.height;
            if (!okImg || w <= 2 || h <= 2)
            {
                Object.DestroyImmediate(t);
                Debug.LogError("[Personaggi] " + src + " non è un'immagine valida (puntatore LFS? esegui 'git lfs pull'): variante capelli non creata.");
                return;
            }
            var px = t.GetPixels32();
            // metà-colore: bambino = metà in U (x < w/2); adulto = 16 bande orizzontali, le 8 in basso del buffer
            // (origine in basso, y < h/2) sono il ramato, le 8 in alto il castano (misurato sul colore medio per banda)
            System.Func<int, int, int> meta = (xx, yy) => p.HairBande ? (yy < h / 2 ? 0 : 1) : (xx < w / 2 ? 0 : 1);
            double[] somma = new double[2]; int[] n = new int[2];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.a < 128) continue;
                    int m = meta(x, y);
                    somma[m] += 0.299 * c.r + 0.587 * c.g + 0.114 * c.b; n[m]++;
                }
            double[] media = { n[0] > 0 ? somma[0] / n[0] : 1.0, n[1] > 0 ? somma[1] / n[1] : 1.0 };
            // adulto: saturazione di riferimento = prima banda di ogni metà; nelle bande successive l'albedo originale
            // ingrigisce (tempie) e la variante preserva quel grigio crescente
            double[] satRef = { 1, 1 };
            if (p.HairBande)
                for (int k = 0; k < 2; k++)
                {
                    double s = 0; int cnt = 0;
                    for (int y = k * (h / 2); y < k * (h / 2) + h / 16; y++)
                        for (int x = 0; x < w; x++)
                        {
                            var c = px[y * w + x]; if (c.a < 128) continue;
                            s += Satur(c); cnt++;
                        }
                    satRef[k] = Mathf.Max(0.001f, (float)(s / Mathf.Max(1, cnt)));
                }
            // base in sRGB. Nell'atlas esportato da Blender la metà sinistra (x < w/2) è il ramato
            // (lato destro di Amerigo) e la metà destra il castano (lato sinistro): stesso ordine qui.
            double[][] baseCol = { new[] { 184.0, 90.0, 46.0 }, new[] { 42.0, 33.0, 28.0 } };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    int m = meta(x, y);
                    double rel = (0.299 * c.r + 0.587 * c.g + 0.114 * c.b) / media[m];
                    rel = 1.0 + (rel - 1.0) * 0.8; // un po' meno contrasto interno per non sporcare la divisione
                    double r = baseCol[m][0] * rel, g = baseCol[m][1] * rel, b = baseCol[m][2] * rel;
                    if (p.HairBande)
                    {
                        // grigio alle tempie: dove l'originale perde saturazione il colore vira a un grigio chiaro
                        double keep = System.Math.Min(1.0, Satur(c) / satRef[m]);
                        double grigio = (0.299 * r + 0.587 * g + 0.114 * b) * 1.6 + 30.0;
                        r = grigio + (r - grigio) * keep; g = grigio + (g - grigio) * keep; b = grigio + (b - grigio) * keep;
                    }
                    px[y * w + x] = new Color32(
                        (byte)Mathf.Clamp((float)r, 0, 255), (byte)Mathf.Clamp((float)g, 0, 255), (byte)Mathf.Clamp((float)b, 0, 255), c.a);
                }
            var o = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            o.SetPixels32(px);
            File.WriteAllBytes(dst, o.EncodeToPNG());
            Object.DestroyImmediate(t); Object.DestroyImmediate(o);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
        }

        // mappa Metallic (R=0) + Smoothness (A = 1 - roughness) per Clothes, generata dalla roughness
        static Texture2D CreaMetalGlossClothes(Pers p)
        {
            string dst = p.TexF + "/" + p.Nome + "_Clothes_metalgloss.png";
            string src = p.TexF + "/" + p.Nome + "_Clothes_roughness.png";
            if (!File.Exists(src)) { Debug.LogWarning("[Personaggi] Manca " + src + ": niente mappa metalgloss per i vestiti."); return null; }
            var rough = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            bool okImg = rough.LoadImage(File.ReadAllBytes(src));
            if (!okImg || rough.width <= 2 || rough.height <= 2)
            {
                Object.DestroyImmediate(rough);
                Debug.LogError("[Personaggi] " + src + " non è un'immagine valida (puntatore LFS? esegui 'git lfs pull'): metalgloss non creata.");
                return null;
            }
            var px = rough.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, (byte)(255 - px[i].r));
            var o = new Texture2D(rough.width, rough.height, TextureFormat.RGBA32, false, true);
            o.SetPixels32(px);
            File.WriteAllBytes(dst, o.EncodeToPNG());
            Object.DestroyImmediate(rough); Object.DestroyImmediate(o);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
            ti.sRGBTexture = false; ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.maxTextureSize = 1024; ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
        }

        static Texture2D Tex(Pers p, string nome) { return AssetDatabase.LoadAssetAtPath<Texture2D>(p.TexF + "/" + nome + ".png"); }

        static Dictionary<string, Material> CreaMateriali(Pers p)
        {
            Directory.CreateDirectory(p.MatF);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var metalGloss = CreaMetalGlossClothes(p);
            var res = new Dictionary<string, Material>();
            // nome, albedo, normal, occlusion, metalgloss, smoothness costante
            var tutti = new Dictionary<string, object[]> {
                { "Skin", new object[]{"Skin", p.Nome + "_Skin_albedo", null, null, null, 0.25f} },
                { "Face", new object[]{"Face", p.Nome + "_Face_albedo", null, null, null, 0.25f} },
                { "Clothes", new object[]{"Clothes", p.Nome + "_Clothes_albedo", p.Nome + "_Clothes_normal", p.Nome + "_Clothes_ao", metalGloss, 0.5f} },
                { "Hair", new object[]{"Hair", p.HairAlbedo, p.Nome + "_Hair_normal", null, null, 0.3f} },
            };
            foreach (var slot in new[] { "Skin", "Face", "Clothes", "Hair" })
            {
                var d = tutti[slot];
                string nome = (string)d[0];
                string path = p.MatF + "/" + p.Nome + "_" + nome + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                m.name = p.Nome + "_" + nome;
                m.SetTexture("_BaseMap", Tex(p, (string)d[1]));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Metallic", 0f);
                if (d[2] != null) { m.SetTexture("_BumpMap", Tex(p, (string)d[2])); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
                if (d[3] != null) { m.SetTexture("_OcclusionMap", Tex(p, (string)d[3])); m.SetFloat("_OcclusionStrength", 1f); }
                if (d[4] != null)
                {
                    m.SetTexture("_MetallicGlossMap", (Texture)d[4]); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 0f);
                }
                else m.SetFloat("_Smoothness", (float)d[5]);
                if (nome == "Hair")
                {
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 0f); m.SetFloat("_Surface", 0f);
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                }
                EditorUtility.SetDirty(m);
                res[nome] = m;
            }
            AssetDatabase.SaveAssets();
            return res;
        }

        /// <summary>Prefab del corpo di Amerigo se presente (usato da SceneBuilder.BuildPlayer), altrimenti null.</summary>
        public static GameObject LoadAmerigoPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Bambino.Prefab);
        }

        /// <summary>Prefab di Amerigo adulto (presente a Napoli, hub) se presente, altrimenti null.</summary>
        public static GameObject LoadAmerigoAdultoPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Adulto.Prefab);
        }
    }
}
