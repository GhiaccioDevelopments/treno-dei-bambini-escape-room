using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TrenoBambini.Core;
using TrenoBambini.Player;
using TrenoBambini.Puzzles;
using TrenoBambini.UI;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Costruisce le 5 scene (Fase 2.3: ambienti realistici). Gli ambienti stanno in SceneBuilderAmbienti.cs, il kit in SceneBuilderKit.cs.
    /// Menu: Treno/Costruisci tutte le scene
    /// </summary>
    public static partial class SceneBuilder
    {
        const string ScenesFolder = "Assets/_Game/Scenes";
        static readonly string[] SceneNames =
        {
            "00_Boot", "01_Titolo", "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe"
        };

        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        // punti di parete (x,z) occupati da porte: nessuna finestra al piano terra vicino
        static readonly List<Vector3> doorWallPoints = new List<Vector3>();

        // ------------------------------------------------------------------ entry point

        [MenuItem("Treno/Costruisci tutte le scene")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                ArtGenerator.EnsureMaterials();
                matCache.Clear();
                ArtGenerator.EnsureFolder(ScenesFolder);
                PrepareKitFolder();

                EditorUtility.DisplayProgressBar("Costruisci scene", SceneNames[0], 0.05f);
                BuildBoot();
                EditorUtility.DisplayProgressBar("Costruisci scene", SceneNames[1], 0.2f);
                BuildTitolo();
                EditorUtility.DisplayProgressBar("Costruisci scene", SceneNames[2], 0.4f);
                BuildHub();
                EditorUtility.DisplayProgressBar("Costruisci scene", SceneNames[3], 0.65f);
                BuildBasso();
                EditorUtility.DisplayProgressBar("Costruisci scene", SceneNames[4], 0.85f);
                BuildFlashback();

                SetBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // look d'epoca (profili, nebbia, luci, probe); il bake resta un menu a parte: Treno/Look/Bake luci di tutte le scene
                EditorUtility.DisplayProgressBar("Costruisci scene", "Look d'epoca", 0.95f);
                LookBuilder.ApplyLookToScenes();
                Debug.LogWarning("[SceneBuilder] Luci da rifare: Treno ▸ Look ▸ Bake luci di tutte le scene");
                EditorSceneManager.OpenScene(ScenePath(SceneNames[0]), OpenSceneMode.Single);
                ReportKit();
                Debug.Log("[SceneBuilder] Costruite le 5 scene e aggiornati i Build Settings.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static string ScenePath(string name) { return ScenesFolder + "/" + name + ".unity"; }

        static void SetBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var n in SceneNames) list.Add(new EditorBuildSettingsScene(ScenePath(n), true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ helper generici

        static Material Mat(string name)
        {
            Material m;
            if (matCache.TryGetValue(name, out m) && m != null) return m;
            string mapped;
            m = SpecialMat(name);
            if (m != null) { }
            else if (name.StartsWith("M_")) m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + name + ".mat");
            else if (LegacyToPbr.TryGetValue(name, out mapped)) m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + mapped + ".mat");
            else m = ArtGenerator.GetMaterial(name);
            if (m == null) Debug.LogError("[SceneBuilder] Materiale mancante: " + name);
            matCache[name] = m;
            return m;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                               string mat, Vector3? euler = null, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = euler ?? Vector3.zero;
            t.localScale = scale;
            if (!string.IsNullOrEmpty(mat)) go.GetComponent<Renderer>().sharedMaterial = Mat(mat);
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, string mat,
                              Vector3? euler = null, bool collider = true)
        {
            return Prim(PrimitiveType.Cube, name, parent, pos, scale, mat, euler, collider);
        }

        // cilindro con diametro d e altezza h
        static GameObject Cyl(string name, Transform parent, Vector3 pos, float d, float h, string mat,
                              Vector3? euler = null, bool collider = true)
        {
            return Prim(PrimitiveType.Cylinder, name, parent, pos, new Vector3(d, h * 0.5f, d), mat, euler, collider);
        }

        static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        static Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            return scene;
        }

        static void SaveScene(Scene scene, string name)
        {
            if (!EditorSceneManager.SaveScene(scene, ScenePath(name)))
                Debug.LogError("[SceneBuilder] Salvataggio fallito: " + name);
        }

        static void SetStatic(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
        }

        static void Ambient(Color sky, Color equator, Color ground)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
        }

        static Light Sun(Color color, float intensity, Vector3 euler)
        {
            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.8f;
            RenderSettings.sun = l;
            return l;
        }

        static Camera MakeCamera(string name, Vector3 pos, Quaternion rot, Color background, bool mainTag)
        {
            var go = new GameObject(name);
            if (mainTag) go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(pos, rot);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 150f;
            cam.fieldOfView = 60f;
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true; // voluto: post-processing del look d'epoca su tutte le camere (anche Boot, innocuo)
            return cam;
        }

        static SpawnPoint Spawn(string id, Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject("SpawnPoint_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var sp = go.AddComponent<SpawnPoint>();
            sp.id = id;
            return sp;
        }

        static void InvisibleWall(string name, Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }

        // Amerigo: radice senza mesh (CharacterController), corpo capsula 1.2 m + coppola
        static void BuildAmerigoModel(Transform root)
        {
            Prim(PrimitiveType.Capsule, "Corpo", root, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.6f, 0.5f), "Amerigo", null, false);
            Cyl("Coppola", root, new Vector3(0f, 1.17f, 0f), 0.36f, 0.07f, "Legno", null, false);
            Box("Visiera", root, new Vector3(0f, 1.14f, 0.2f), new Vector3(0.22f, 0.025f, 0.14f), "Legno", null, false);
            Box("Naso", root, new Vector3(0f, 1.0f, 0.24f), new Vector3(0.06f, 0.06f, 0.08f), "Intonaco", null, false);
            Box("Sciarpa", root, new Vector3(0f, 0.86f, 0f), new Vector3(0.38f, 0.07f, 0.38f), "Tessuto1", null, false);
        }

        static GameObject BuildPlayer(Vector3 pos, float yaw, Camera cam, CamProfilo profilo = CamProfilo.Esterno, bool adulto = false)
        {
            // se manca il prefab dell'adulto si ripiega sul bambino: capsula, camera e velocità restano coerenti col modello usato
            var prefabAdulto = adulto ? PersonaggiBuilder.LoadAmerigoAdultoPrefab() : null;
            if (adulto && prefabAdulto == null)
                Debug.LogWarning("[SceneBuilder] Prefab di Amerigo adulto mancante: uso il bambino (esegui Treno > Personaggi > Configura import Amerigo adulto).");
            bool usaAdulto = adulto && prefabAdulto != null;
            if (!usaAdulto && profilo == CamProfilo.InternoPiccoloAdulto) profilo = CamProfilo.InternoPiccolo;
            var p = new GameObject("Amerigo");
            p.tag = "Player";
            p.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            // modello in gioco: prefab MakeHuman (bambino 1,0 m, adulto 1,75 m) oppure v5/v6 di riserva (bambino 1,2 m, adulto 1,78 m)
            var amerigoPrefab = usaAdulto ? prefabAdulto : PersonaggiBuilder.LoadAmerigoPrefab();
            bool mh = PersonaggiBuilder.PrefabEMakeHuman(amerigoPrefab);
            // bambino di ~7 anni: capsula 1.0 m (MakeHuman) o 1.2 m (v5 / segnaposto), raggio 20-22 cm. Adulto (presente, hub): 1.75 m, raggio 28 cm
            float altBambino = amerigoPrefab != null && !mh ? 1.2f : (amerigoPrefab == null ? 1.2f : PersonaggiBuilder.AltezzaBambino);
            var cc = p.AddComponent<CharacterController>();
            cc.height = usaAdulto ? 1.75f : altBambino;
            cc.radius = usaAdulto ? 0.28f : (altBambino < 1.1f ? 0.2f : 0.22f);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            cc.stepOffset = 0.2f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.03f;
            // punto di montaggio del modello: la 2.5 mette qui l'FBX di Amerigo (con Animator); per ora il segnaposto
            var modello = new GameObject(ThirdPersonController.ModelloName).transform;
            modello.SetParent(p.transform, false);
            if (amerigoPrefab != null)
            {
                // modello definitivo (2.5): prefab FBX Humanoid con Animator; nessun collider sul modello
                var corpo = (GameObject)PrefabUtility.InstantiatePrefab(amerigoPrefab, modello);
                corpo.name = "Amerigo_Mesh";
                // MakeHuman: l'offset verticale del prefab (suola a quota 0) lo calcola il menu di import; in più il CharacterController
                // resta sospeso di skinWidth (3 cm) sopra il pavimento: si abbassa il modello della stessa quota, così le suole toccano terra.
                // v5/v6 (riserva): posizione zero come prima.
                corpo.transform.localPosition = mh ? amerigoPrefab.transform.localPosition + Vector3.down * cc.skinWidth : Vector3.zero;
                corpo.transform.localRotation = Quaternion.identity;
            }
            else BuildAmerigoModel(modello);
            var tpc = p.AddComponent<ThirdPersonController>();
            // velocità di gioco e velocità di riferimento delle clip Mixamo (misurate) per personaggio: i piedi non scivolano
            // MakeHuman: velocità dei piedi appoggiati misurate in Play (Animator.Update a passo fisso, mediana dei fotogrammi con il piede a terra).
            // Cammino (clip Mixamo Walk): adulto 1,70 m/s, bambino 0,89. Corsa (R004, clip Mixamo "Slow Run"): adulto 3,4 m/s, bambino 1,7 m/s;
            // v6/v5 di riserva: 3,4 e 2,15. Moltiplicatore di riproduzione a velocità di corsa: adulto 0,94x, bambino 1,29x (runSpeed 2,2: a 2,7 sarebbe 1,6x).
            if (usaAdulto && mh) { tpc.walkSpeed = 1.4f; tpc.runSpeed = 3.2f; tpc.clipWalkSpeed = 1.7f; tpc.clipRunSpeed = 3.4f; }
            else if (usaAdulto) { tpc.walkSpeed = 1.4f; tpc.runSpeed = 3.2f; tpc.clipWalkSpeed = 1.2f; tpc.clipRunSpeed = 3.4f; }
            else if (mh) { tpc.walkSpeed = 1.1f; tpc.runSpeed = 2.2f; tpc.clipWalkSpeed = 0.89f; tpc.clipRunSpeed = 1.7f; tpc.maxAnimMultiplier = 3f; }
            else { tpc.walkSpeed = 1.1f; tpc.runSpeed = 2.7f; tpc.clipWalkSpeed = 0.46f; tpc.clipRunSpeed = 2.15f; }
            p.AddComponent<PlayerInteractor>();
            // punto mirato dalla camera (petto/testa: bambino MakeHuman 1,0 m a 0,85 m; v5 1,2 m a 1,0 m)
            var punto = new GameObject("PuntoCamera").transform;
            punto.SetParent(p.transform, false);
            punto.localPosition = new Vector3(0f, usaAdulto ? 1.45f : (mh ? 0.85f : 1.0f), 0f);

            if (cam != null) BuildCameraRig(cam, p.transform, punto, yaw, profilo, !usaAdulto && mh ? altBambino / 1.2f : 1f);
            AssegnaLayerVistaLibera(p.scene);
            return p;
        }

        // ------------------------------------------------------------------ 00_Boot

        static void BuildBoot()
        {
            var scene = NewScene();
            Ambient(Color.black, Color.black, Color.black);
            MakeCamera("Main Camera", new Vector3(0f, 1f, -10f), Quaternion.identity, Color.black, true);
            new GameObject("BootLoader").AddComponent<BootLoader>();
            SaveScene(scene, "00_Boot");
        }

        // ------------------------------------------------------------------ 01_Titolo

        static void BuildTitolo()
        {
            var scene = NewScene();
            kitScene = "Titolo"; kitIndex = 0;
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            ResetSpazio();
            Ambient(new Color(0.80f, 0.72f, 0.58f), new Color(0.56f, 0.48f, 0.38f), new Color(0.30f, 0.25f, 0.19f));
            Sun(new Color(1f, 0.90f, 0.72f), 1.1f, new Vector3(48f, -28f, 0f));

            var env = new GameObject("Diorama").transform;
            Street("Selciato", env, -4.5f, 4.5f, -7f, 7f, 0.15f);

            // punti da tenere liberi da balconi: ancoraggi dei fili e lampione
            foreach (var z in new[] { -4.5f, -0.5f, 3.5f, 5.5f }) { noBalcone.Add(new Vector3(-1.6f, 0f, z)); noBalcone.Add(new Vector3(1.6f, 0f, z)); }
            noBalcone.Add(new Vector3(1.2f, 0f, -1.5f));

            Building(env, -1, 1.6f, -7f, -1f, 2.5f, 3, "Tufo", r, 1.8f);
            Building(env, -1, 1.6f, -1f, 2.5f, 2.5f, 4, "Intonaco", r, 1.8f);
            Building(env, -1, 1.6f, 2.5f, 7f, 2.5f, 3, "TufoScuro", r, 1.8f);
            Building(env, 1, 1.6f, -7f, 0.5f, 2.5f, 4, "Intonaco", r, 1.8f);
            Building(env, 1, 1.6f, 0.5f, 7f, 2.5f, 3, "Tufo", r, 1.8f);
            Ground("FondoVicolo", env, new Vector3(-4f, 0f, 7f), new Vector3(4f, 10f, 8f), "M_IntonacoRustico05", 2.5f);

            // porta e scarpe
            Box("PortaDiorama", env, new Vector3(1.5f, 1.0f, -3f), new Vector3(0.2f, 2.0f, 1.0f), "Legno");
            Box("PortaDiorama", env, new Vector3(-1.5f, 1.0f, 1.5f), new Vector3(0.2f, 2.0f, 1.0f), "Legno");
            // fili nella fascia libera tra i piani (y 6.1-6.55), a ridosso delle facciate
            Clothesline(env, r, -4.5f, 6.3f, 6.2f, -1.6f, 1.6f, 4);
            Clothesline(env, r, -0.5f, 6.45f, 6.3f, -1.6f, 1.6f, 3);
            Clothesline(env, r, 3.5f, 6.2f, 6.15f, -1.6f, 1.6f, 4);
            Clothesline(env, r, 5.5f, 6.5f, 6.35f, -1.6f, 1.6f, 3);
            Lamp(env, new Vector3(1.2f, 0f, -1.5f));
            Cart(env, new Vector3(-0.9f, 0f, 4.2f));
            Bucket(env, new Vector3(1.2f, 0f, 2.2f));
            Rubble(env, r, new Vector3(0.2f, 0f, 5.8f), 5, 0.4f, 1.3f);
            RisolviSpazio("01_Titolo");
            AssestaMacerie();
            SetStatic(env.gameObject);

            // Amerigo di spalle nel diorama (solo scenografia)
            var am = new GameObject("AmerigoDiorama").transform;
            am.position = new Vector3(0f, 0f, -2.2f);
            BuildAmerigoModel(am);

            var cam = MakeCamera("Main Camera", new Vector3(0f, 2.3f, -9.5f), Quaternion.identity, new Color(0.90f, 0.80f, 0.60f), true);
            cam.fieldOfView = 42f;
            cam.transform.LookAt(new Vector3(0f, 3.2f, 0f));

            new GameObject("TitleScreen").AddComponent<TitleScreen>();
            SaveScene(scene, "01_Titolo");
        }

    }
}
