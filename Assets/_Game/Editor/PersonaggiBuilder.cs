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
                // curva neutra (1 s) sul pivot: l'avatar resta in posa, nessuna animazione fittizia
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, 0f));
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
            var imp = AssetImporter.GetAtPath(AmerigoFbx) as ModelImporter;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AmerigoFbx);
            Debug.Log("[Personaggi] Amerigo.fbx Humanoid, avatar valido: " + (avatar != null && avatar.isValid && avatar.isHuman));

            // prefab con Animator
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AmerigoFbx);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = "Amerigo";
            var anim = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(inst, AmerigoFolder + "/Amerigo.prefab");
            Object.DestroyImmediate(inst);
            AggiornaAnimazioni();
        }

        /// <summary>Prefab del corpo di Amerigo se presente (usato da SceneBuilder.BuildPlayer), altrimenti null.</summary>
        public static GameObject LoadAmerigoPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AmerigoFolder + "/Amerigo.prefab");
        }
    }
}
