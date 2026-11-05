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

        // soglie del blend tree: walkSpeed/runSpeed = 1.9/4.0
        const float WalkThreshold = 0.475f;

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

            // se il controller esiste lo ricostruiamo da zero (generato, quindi riproducibile)
            if (File.Exists(AmerigoController)) AssetDatabase.DeleteAsset(AmerigoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(AmerigoController);
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
            if (File.Exists(AmerigoFbx))
            {
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AmerigoFbx);
                if (avatar != null && imp.sourceAvatar != avatar) { imp.sourceAvatar = avatar; changed = true; }
            }
            bool loop = nome == "Idle" || nome == "Idle2" || nome == "Walk" || nome == "Run";
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                if (c.loopTime != loop) { c.loopTime = loop; changed = true; }
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
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
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AmerigoController);
            var prefabPath = AmerigoFolder + "/Amerigo.prefab";
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
        public static void ConfiguraImportAmerigo()
        {
            if (!File.Exists(AmerigoFbx)) { Debug.LogError("[Personaggi] Manca " + AmerigoFbx); return; }
            ConfiguraTextureAmerigo();
            var imp = AssetImporter.GetAtPath(AmerigoFbx) as ModelImporter;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AmerigoFbx);
            Debug.Log("[Personaggi] Amerigo.fbx Humanoid, avatar valido: " + (avatar != null && avatar.isValid && avatar.isHuman));

            var mats = CreaMaterialiAmerigo();

            // prefab con Animator, materiali URP e LODGroup (LOD0/1/2)
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AmerigoFbx);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = "Amerigo";
            var anim = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            var lods = new Renderer[3];
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int k = r.name.EndsWith("LOD0") ? 0 : r.name.EndsWith("LOD1") ? 1 : r.name.EndsWith("LOD2") ? 2 : -1;
                if (k < 0) continue;
                lods[k] = r;
                // 4 slot dell'FBX nell'ordine Skin, Face, Clothes, Hair = ordine dei materiali generati
                var slots = new Material[4];
                for (int i = 0; i < 4; i++) slots[i] = mats[i];
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
            PrefabUtility.SaveAsPrefabAsset(inst, AmerigoFolder + "/Amerigo.prefab");
            Object.DestroyImmediate(inst);
            AggiornaAnimazioni();
        }

        const string TexFolder = AmerigoFolder + "/Textures";
        const string MatFolder = AmerigoFolder + "/Materials";

        static void ConfiguraTextureAmerigo()
        {
            foreach (var f in Directory.GetFiles(TexFolder, "*.png"))
            {
                string path = f.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string n = Path.GetFileNameWithoutExtension(path);
                bool normal = n.EndsWith("_normal");
                bool lineare = normal || n.EndsWith("_roughness") || n.EndsWith("_ao") || n.EndsWith("_metalgloss");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !lineare;
                bool conAlpha = n == "Amerigo_Hair_albedo" || n.EndsWith("_metalgloss");
                ti.alphaSource = conAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.alphaIsTransparency = n == "Amerigo_Hair_albedo";
                ti.mipmapEnabled = true;
                ti.maxTextureSize = 1024;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }
        }

        // mappa Metallic (R=0) + Smoothness (A = 1 - roughness) per Clothes, generata dalla roughness
        static Texture2D CreaMetalGlossClothes()
        {
            string dst = TexFolder + "/Amerigo_Clothes_metalgloss.png";
            string src = TexFolder + "/Amerigo_Clothes_roughness.png";
            var rough = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            rough.LoadImage(File.ReadAllBytes(src));
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

        static Texture2D Tex(string nome) { return AssetDatabase.LoadAssetAtPath<Texture2D>(TexFolder + "/" + nome + ".png"); }

        static Material[] CreaMaterialiAmerigo()
        {
            Directory.CreateDirectory(MatFolder);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var metalGloss = CreaMetalGlossClothes();
            var res = new List<Material>();
            // nome, albedo, normal, occlusion, metalgloss, smoothness costante
            var def = new[] {
                new object[]{"Skin", "Amerigo_Skin_albedo", null, null, null, 0.25f},
                new object[]{"Face", "Amerigo_Face_albedo", null, null, null, 0.25f},
                new object[]{"Clothes", "Amerigo_Clothes_albedo", "Amerigo_Clothes_normal", "Amerigo_Clothes_ao", metalGloss, 0.5f},
                new object[]{"Hair", "Amerigo_Hair_albedo", "Amerigo_Hair_normal", null, null, 0.3f},
            };
            foreach (var d in def)
            {
                string nome = (string)d[0];
                string path = MatFolder + "/Amerigo_" + nome + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                m.name = "Amerigo_" + nome;
                m.SetTexture("_BaseMap", Tex((string)d[1]));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Metallic", 0f);
                if (d[2] != null) { m.SetTexture("_BumpMap", Tex((string)d[2])); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
                if (d[3] != null) { m.SetTexture("_OcclusionMap", Tex((string)d[3])); m.SetFloat("_OcclusionStrength", 1f); }
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
                res.Add(m);
            }
            AssetDatabase.SaveAssets();
            return res.ToArray();
        }

        /// <summary>Prefab del corpo di Amerigo se presente (usato da SceneBuilder.BuildPlayer), altrimenti null.</summary>
        public static GameObject LoadAmerigoPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AmerigoFolder + "/Amerigo.prefab");
        }
    }
}
