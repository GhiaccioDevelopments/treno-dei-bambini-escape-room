using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R022: impostazioni dell'importer delle clip Mixamo di Amerigo riesportate da Blender
    /// (ArtSource/Characters/riesporta_clip_gerarchia.py: movimento del bacino sull'osso pelvis, fotogramma 0 = T-pose di riposo).
    /// Humanoid con avatar proprio ricavato dalla T-pose del fotogramma 0 (mappa e posa rigenerate da Unity, senza il vecchio
    /// riferimento sbagliato), clip dal fotogramma 1, loop pulito, rotazione e altezza del bacino "Bake Into Pose".
    /// Menu: Treno ▸ Personaggi ▸ Applica importer clip Amerigo (idempotente).
    /// </summary>
    public static class ClipAmerigoImporter
    {
        // il nome della clip resta "mixamo.com" come in origine (nome interno del take Mixamo); il controller la referenzia con GUID del file + ID interno,
        // che cambia a ogni riesportazione: per questo dopo l'import viene ricollegata (RicollegaControllerCorsa).
        // percorso, nome clip, primo e ultimo fotogramma della corsa nel file riesportato
        static readonly (string path, string nome, int primo, int ultimo)[] Clip =
        {
            ("Assets/Art/Animations/Amerigo/Amerigo_Run.fbx", "mixamo.com", 1, 23),
        };

        [MenuItem("Treno/Personaggi/Applica importer clip Amerigo")]
        public static void Applica()
        {
            foreach (var c in Clip) Applica(c.path, c.nome, c.primo, c.ultimo);
        }

        static void Applica(string path, string nome, int primo, int ultimo)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogError("[ClipAmerigo] importer assente: " + path); return; }
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // mappa e posa vuote: Unity le rigenera dal fotogramma 0 (T-pose), come per un file Mixamo "with skin"
            imp.humanDescription = new HumanDescription
            {
                human = new HumanBone[0], skeleton = new SkeletonBone[0],
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
            imp.SaveAndReimport();

            var def = imp.defaultClipAnimations;
            if (def.Length == 0) { Debug.LogError("[ClipAmerigo] nessuna clip in " + path); return; }
            var c = def[0];
            c.name = nome; c.firstFrame = primo; c.lastFrame = ultimo;
            c.loopTime = true; c.loopPose = true;
            c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
            c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            imp.clipAnimations = new[] { c };
            imp.SaveAndReimport();
            RicollegaControllerCorsa(path);
            Debug.Log("[ClipAmerigo] " + path + ": Humanoid, clip " + nome + " " + primo + "-" + ultimo + ", loop e Bake Into Pose");
        }

        const string Controller = "Assets/Art/Characters/Amerigo/Amerigo.controller";

        /// <summary>Dopo la riesportazione l'ID interno della clip cambia: ricollega al blend tree Locomozione solo la voce di corsa (soglia 1) che ha perso il riferimento o punta a questo file.</summary>
        static void RicollegaControllerCorsa(string path)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__"));
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Controller);
            if (clip == null || ctrl == null) return;
            foreach (var st in ctrl.layers[0].stateMachine.states)
            {
                var bt = st.state.motion as UnityEditor.Animations.BlendTree;
                if (bt == null) continue;
                var ch = bt.children;
                for (int i = 0; i < ch.Length; i++)
                {
                    // solo la voce della corsa: soglia 1 e riferimento perso (null) oppure gia' verso un'altra clip di questo stesso file
                    bool voceCorsa = ch[i].threshold >= 0.99f;
                    bool daRicollegare = ch[i].motion == null || AssetDatabase.GetAssetPath(ch[i].motion) == path;
                    if (voceCorsa && daRicollegare) ch[i].motion = clip;
                }
                bt.children = ch;
                EditorUtility.SetDirty(bt);
            }
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
        }
    }
}
