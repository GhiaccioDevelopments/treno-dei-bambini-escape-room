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
    /// Costruisce le 5 scene della Fase 1 con primitive low-poly.
    /// Menu: Treno/Costruisci tutte le scene
    /// </summary>
    public static class SceneBuilder
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
                EditorSceneManager.OpenScene(ScenePath(SceneNames[0]), OpenSceneMode.Single);
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
            m = ArtGenerator.GetMaterial(name);
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
            go.AddComponent<UniversalAdditionalCameraData>();
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

        static GameObject BuildPlayer(Vector3 pos, float yaw, Camera cam, bool interno = false)
        {
            var p = new GameObject("Amerigo");
            p.tag = "Player";
            p.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var cc = p.AddComponent<CharacterController>();
            cc.height = 1.2f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.6f, 0f);
            cc.stepOffset = 0.25f;
            cc.slopeLimit = 45f;
            BuildAmerigoModel(p.transform);
            p.AddComponent<ThirdPersonController>();
            p.AddComponent<PlayerInteractor>();

            if (cam != null)
            {
                var follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.target = p.transform;
                // Negli interni piccoli la camera sta più in alto e più vicina, guardando dall'alto
                if (interno)
                {
                    follow.distance = 3.4f;
                    follow.height = 3.0f;
                    follow.minPitch = 15f;
                }
                cam.transform.position = pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, follow.height, -follow.distance);
                cam.transform.LookAt(pos + Vector3.up * 1f);
            }
            return p;
        }

        // ------------------------------------------------------------------ facciate

        static void Building(Transform parent, int side, float innerAbs, float z0, float z1, float depth,
                             int floors, string wallMat, System.Random r, float winStep)
        {
            // side: -1 = edificio a sinistra (x<0), +1 = a destra
            float innerX = side * innerAbs;
            float n = -side; // normale della facciata verso il vicolo
            float len = z1 - z0;
            float zc = (z0 + z1) * 0.5f;
            float h = floors * 3f;
            float cx = side * (innerAbs + depth * 0.5f);

            Box("Casa", parent, new Vector3(cx, h * 0.5f, zc), new Vector3(depth, h, len), wallMat);
            Box("Cornicione", parent, new Vector3(cx, h + 0.15f, zc), new Vector3(depth + 0.3f, 0.3f, len + 0.2f), "TufoScuro", null, false);
            Box("Zoccolo", parent, new Vector3(innerX + n * 0.06f, 0.4f, zc), new Vector3(0.12f, 0.8f, len), "TufoScuro");

            for (float z = z0 + winStep * 0.6f; z <= z1 - 0.8f; z += winStep)
            {
                for (int f = 0; f < floors; f++)
                {
                    if (f == 0 && NearDoor(innerX, z)) continue;
                    float y = f * 3f + 1.8f;
                    Box("Finestra", parent, new Vector3(innerX + n * 0.02f, y, z), new Vector3(0.12f, 1.3f, 0.9f), "Finestra", null, false);
                    Box("Davanzale", parent, new Vector3(innerX + n * 0.12f, y - 0.7f, z), new Vector3(0.25f, 0.08f, 1.1f), "Legno", null, f == 0);
                    Box("Architrave", parent, new Vector3(innerX + n * 0.08f, y + 0.7f, z), new Vector3(0.16f, 0.1f, 1.1f), "TufoScuro", null, false);
                    if (r.NextDouble() < 0.4)
                    {
                        Box("Persiana", parent, new Vector3(innerX + n * 0.1f, y, z - 0.62f), new Vector3(0.05f, 1.3f, 0.3f), "Legno", null, false);
                        Box("Persiana", parent, new Vector3(innerX + n * 0.1f, y, z + 0.62f), new Vector3(0.05f, 1.3f, 0.3f), "Legno", null, false);
                    }
                    if (f >= 1 && r.NextDouble() < 0.25)
                    {
                        float by = y - 0.75f;
                        Box("Balcone", parent, new Vector3(innerX + n * 0.4f, by, z), new Vector3(0.8f, 0.08f, 1.5f), "Ferro", null, false);
                        Box("Ringhiera", parent, new Vector3(innerX + n * 0.78f, by + 0.35f, z), new Vector3(0.04f, 0.7f, 1.5f), "Ferro", null, false);
                        Box("Ringhiera", parent, new Vector3(innerX + n * 0.4f, by + 0.35f, z - 0.74f), new Vector3(0.8f, 0.7f, 0.04f), "Ferro", null, false);
                        Box("Ringhiera", parent, new Vector3(innerX + n * 0.4f, by + 0.35f, z + 0.74f), new Vector3(0.8f, 0.7f, 0.04f), "Ferro", null, false);
                    }
                }
            }

            // volumi sul tetto
            if (len > 3f)
            {
                float rz = z0 + 1f + (float)r.NextDouble() * (len - 2f);
                Box("Comignolo", parent, new Vector3(cx + side * 0.5f, h + 0.9f, rz), new Vector3(0.6f, 1.2f, 0.6f), "TufoScuro", null, false);
                if (r.NextDouble() < 0.5)
                    Box("Terrazzino", parent, new Vector3(cx, h + 0.8f, zc), new Vector3(depth * 0.4f, 1.0f, Mathf.Min(len * 0.4f, 3f)), "Intonaco", null, false);
            }
        }

        static bool NearDoor(float innerX, float z)
        {
            foreach (var p in doorWallPoints)
                if (Mathf.Abs(p.x - innerX) < 0.01f && Mathf.Abs(p.z - z) < 1.7f) return true;
            return false;
        }

        // porta con cornice e cartello; wallPoint = punto sul muro a terra, normal = verso il vicolo
        static Door MakeDoor(Transform doorsParent, Transform envParent, string id, Vector3 wallPoint,
                             Vector3 normal, string signMat)
        {
            var euler = Quaternion.LookRotation(normal).eulerAngles;
            Box("CornicePorta_" + id, envParent, wallPoint + normal * 0.04f + Vector3.up * 1.25f, new Vector3(1.6f, 2.5f, 0.08f), "TufoScuro", euler, false);
            var door = Box("Porta_" + id, doorsParent, wallPoint + normal * 0.12f + Vector3.up * 1.1f, new Vector3(1.1f, 2.2f, 0.2f), "Legno", euler);
            Box("Maniglia_" + id, doorsParent, wallPoint + normal * 0.26f + Vector3.up * 1.05f + Quaternion.Euler(euler) * Vector3.right * 0.38f,
                new Vector3(0.07f, 0.07f, 0.07f), "Ferro", euler, false);
            Box("Cartello_" + id, envParent, wallPoint + normal * 0.1f + Vector3.up * 2.85f, new Vector3(1.5f, 0.45f, 0.1f), signMat, euler, false);
            doorWallPoints.Add(wallPoint);
            var d = door.AddComponent<Door>();
            d.luogoId = id;
            return d;
        }

        // ------------------------------------------------------------------ props

        static void Clothesline(Transform parent, System.Random r, float z, float yL, float yR, float xL, float xR, int cloths)
        {
            float len = xR - xL;
            float ang = Mathf.Atan2(yR - yL, len) * Mathf.Rad2Deg;
            Box("Filo", parent, new Vector3((xL + xR) * 0.5f, (yL + yR) * 0.5f, z), new Vector3(len, 0.02f, 0.02f), "Ferro", new Vector3(0f, 0f, ang), false);
            string[] mats = { "Tessuto1", "Tessuto2", "Tessuto3" };
            for (int k = 1; k <= cloths; k++)
            {
                float t = k / (cloths + 1f) + ((float)r.NextDouble() - 0.5f) * 0.05f;
                float w = 0.45f + (float)r.NextDouble() * 0.35f;
                float hh = 0.6f + (float)r.NextDouble() * 0.4f;
                float cy = Mathf.Lerp(yL, yR, t) - hh * 0.5f - 0.02f;
                Box("Panno", parent, new Vector3(Mathf.Lerp(xL, xR, t), cy, z), new Vector3(w, hh, 0.02f), mats[r.Next(mats.Length)],
                    new Vector3(0f, (float)(r.NextDouble() - 0.5) * 20f, (float)(r.NextDouble() - 0.5) * 8f), false);
            }
        }

        static void Rubble(Transform parent, System.Random r, Vector3 center, int count, float radius)
        {
            string[] mats = { "Tufo", "TufoScuro", "Intonaco" };
            for (int i = 0; i < count; i++)
            {
                float s = 0.3f + (float)r.NextDouble() * 0.8f;
                var off = new Vector3((float)(r.NextDouble() * 2 - 1) * radius, 0f, (float)(r.NextDouble() * 2 - 1) * radius);
                float stack = i > count / 2 ? 0.45f : 0f;
                Box("Macerie", parent, center + off + new Vector3(0f, s * 0.35f + stack, 0f), new Vector3(s, s * 0.7f, s * (0.7f + (float)r.NextDouble() * 0.5f)),
                    mats[r.Next(mats.Length)], new Vector3((float)r.NextDouble() * 30f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 30f));
            }
            Box("TraveSpezzata", parent, center + new Vector3(0.2f, 0.7f, 0.1f), new Vector3(0.14f, 0.14f, 1.8f), "Legno", new Vector3(18f, 35f, 8f));
        }

        static void Cart(Transform parent, Vector3 pos)
        {
            var root = Empty("Carretto", parent, pos);
            Box("Pianale", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(1.0f, 0.12f, 1.7f), "Legno");
            Box("SpondaSx", root.transform, new Vector3(-0.5f, 0.9f, 0f), new Vector3(0.06f, 0.3f, 1.7f), "Legno");
            Box("SpondaDx", root.transform, new Vector3(0.5f, 0.9f, 0f), new Vector3(0.06f, 0.3f, 1.7f), "Legno");
            Box("SpondaFondo", root.transform, new Vector3(0f, 0.9f, -0.84f), new Vector3(1.0f, 0.3f, 0.06f), "Legno");
            Cyl("RuotaSx", root.transform, new Vector3(-0.58f, 0.4f, 0f), 0.8f, 0.07f, "Legno", new Vector3(0f, 0f, 90f));
            Cyl("RuotaDx", root.transform, new Vector3(0.58f, 0.4f, 0f), 0.8f, 0.07f, "Legno", new Vector3(0f, 0f, 90f));
            Box("Timone", root.transform, new Vector3(-0.3f, 0.55f, 1.6f), new Vector3(0.05f, 0.05f, 1.4f), "Legno", new Vector3(8f, 0f, 0f), false);
            Box("Timone", root.transform, new Vector3(0.3f, 0.55f, 1.6f), new Vector3(0.05f, 0.05f, 1.4f), "Legno", new Vector3(8f, 0f, 0f), false);
            Box("Cassa", root.transform, new Vector3(0.1f, 1.0f, -0.2f), new Vector3(0.5f, 0.4f, 0.5f), "TufoScuro", new Vector3(0f, 20f, 0f));
            Box("Sacco", root.transform, new Vector3(-0.15f, 0.95f, 0.45f), new Vector3(0.5f, 0.35f, 0.4f), "Tessuto3", new Vector3(0f, -15f, 0f));
        }

        static void Bucket(Transform parent, Vector3 pos)
        {
            Cyl("Secchio", parent, pos + new Vector3(0f, 0.17f, 0f), 0.32f, 0.34f, "Ferro");
        }

        static void Lamp(Transform parent, Vector3 pos)
        {
            Cyl("PaloLampione", parent, pos + new Vector3(0f, 2.0f, 0f), 0.12f, 4f, "Ferro");
            Cyl("BaseLampione", parent, pos + new Vector3(0f, 0.15f, 0f), 0.3f, 0.3f, "Ferro");
            Box("BraccioLampione", parent, pos + new Vector3(0.25f, 3.95f, 0f), new Vector3(0.6f, 0.06f, 0.06f), "Ferro", null, false);
            Prim(PrimitiveType.Sphere, "LuceLampione", parent, pos + new Vector3(0.5f, 3.8f, 0f), new Vector3(0.32f, 0.32f, 0.32f), "Glow", null, false);
            Box("CappelloLampione", parent, pos + new Vector3(0.5f, 4.02f, 0f), new Vector3(0.42f, 0.06f, 0.42f), "Ferro", null, false);
        }

        // edicola votiva incassata sul muro; n = normale verso il vicolo
        static void Shrine(Transform parent, Vector3 wallPoint, Vector3 normal)
        {
            var e = Quaternion.LookRotation(normal).eulerAngles;
            Box("EdicolaCorpo", parent, wallPoint + normal * 0.15f + Vector3.up * 1.8f, new Vector3(0.9f, 1.2f, 0.3f), "Intonaco", e);
            Box("EdicolaNicchia", parent, wallPoint + normal * 0.31f + Vector3.up * 1.85f, new Vector3(0.6f, 0.8f, 0.04f), "Finestra", e, false);
            Box("EdicolaMensola", parent, wallPoint + normal * 0.25f + Vector3.up * 1.12f, new Vector3(1.0f, 0.08f, 0.5f), "Legno", e);
            Box("EdicolaStatuetta", parent, wallPoint + normal * 0.3f + Vector3.up * 1.95f, new Vector3(0.14f, 0.4f, 0.1f), "Cera", e, false);
            Cyl("EdicolaCero", parent, wallPoint + normal * 0.33f + Vector3.up * 1.25f + Quaternion.Euler(e) * Vector3.right * 0.2f, 0.06f, 0.18f, "Cera", null, false);
            Prim(PrimitiveType.Sphere, "EdicolaLumino", parent, wallPoint + normal * 0.33f + Vector3.up * 1.4f + Quaternion.Euler(e) * Vector3.right * 0.2f,
                new Vector3(0.07f, 0.1f, 0.07f), "Glow", null, false);
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
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            Ambient(new Color(0.80f, 0.72f, 0.58f), new Color(0.56f, 0.48f, 0.38f), new Color(0.30f, 0.25f, 0.19f));
            Sun(new Color(1f, 0.90f, 0.72f), 1.1f, new Vector3(48f, -28f, 0f));

            var env = new GameObject("Diorama").transform;
            Box("Selciato", env, new Vector3(0f, -0.15f, 0f), new Vector3(9f, 0.3f, 14f), "Ciottoli");
            Box("Canaletta", env, new Vector3(0f, 0.01f, 0f), new Vector3(0.3f, 0.02f, 13f), "TufoScuro", null, false);

            Building(env, -1, 1.6f, -7f, -1f, 2.5f, 3, "Tufo", r, 1.8f);
            Building(env, -1, 1.6f, -1f, 2.5f, 2.5f, 4, "Intonaco", r, 1.8f);
            Building(env, -1, 1.6f, 2.5f, 7f, 2.5f, 3, "TufoScuro", r, 1.8f);
            Building(env, 1, 1.6f, -7f, 0.5f, 2.5f, 4, "Intonaco", r, 1.8f);
            Building(env, 1, 1.6f, 0.5f, 7f, 2.5f, 3, "Tufo", r, 1.8f);
            Box("FondoVicolo", env, new Vector3(0f, 5f, 7.5f), new Vector3(8f, 10f, 1f), "Tufo");

            // porta e scarpe
            Box("PortaDiorama", env, new Vector3(1.5f, 1.0f, -3f), new Vector3(0.2f, 2.0f, 1.0f), "Legno");
            Box("PortaDiorama", env, new Vector3(-1.5f, 1.0f, 1.5f), new Vector3(0.2f, 2.0f, 1.0f), "Legno");
            Clothesline(env, r, -4.5f, 5.6f, 5.2f, -1.55f, 1.55f, 4);
            Clothesline(env, r, -0.5f, 6.4f, 6.8f, -1.55f, 1.55f, 3);
            Clothesline(env, r, 3.5f, 5.2f, 5.0f, -1.55f, 1.55f, 4);
            Clothesline(env, r, 5.5f, 7.0f, 6.6f, -1.55f, 1.55f, 3);
            Lamp(env, new Vector3(1.2f, 0f, -1.5f));
            Cart(env, new Vector3(-0.9f, 0f, 4.2f));
            Bucket(env, new Vector3(1.2f, 0f, 2.2f));
            Rubble(env, r, new Vector3(0.6f, 0f, 5.8f), 6, 0.6f);
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

        // ------------------------------------------------------------------ 10_Hub_Vicolo

        static float HubInnerLeft(float z) { return (z >= -6f && z <= 2f) ? 5f : 3.5f; }
        static float HubInnerRight(float z) { return (z >= 8f && z <= 14f) ? 5f : 3.5f; }

        static void BuildHub()
        {
            var scene = NewScene();
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            Ambient(new Color(0.78f, 0.70f, 0.58f), new Color(0.55f, 0.47f, 0.38f), new Color(0.30f, 0.24f, 0.18f));
            Sun(new Color(1f, 0.90f, 0.72f), 1.0f, new Vector3(52f, -32f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.76f, 0.60f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 80f;

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // pavimento in ciottoli
            Box("Selciato", env, new Vector3(0f, -0.25f, 0f), new Vector3(14f, 0.5f, 50f), "Ciottoli");
            Box("Canaletta", env, new Vector3(0f, 0.012f, 0f), new Vector3(0.4f, 0.02f, 40f), "TufoScuro", null, false);

            // porte (registrate prima delle case per evitare finestre sopra)
            var basso = MakeDoor(porte, env, "basso_antonietta", new Vector3(-5f, 0f, -2f), Vector3.right, "Tessuto1");
            basso.targetScene = "20_Int_BassoAntonietta";
            basso.targetSpawn = "ingresso";
            MakeDoor(porte, env, "casa_capa_e_fierro", new Vector3(3.5f, 0f, -9f), Vector3.left, "Tessuto2");
            MakeDoor(porte, env, "sede_partito", new Vector3(5f, 0f, 11f), Vector3.left, "Tessuto1");
            MakeDoor(porte, env, "stazione", new Vector3(0f, 0f, 20f), Vector3.back, "Tessuto3");
            MakeDoor(porte, env, "vicolo_stracci", new Vector3(0f, 0f, -20f), Vector3.forward, "Tessuto2");

            // case lato sinistro
            Building(env, -1, 3.5f, -24f, -6f, 6f, 3, "Tufo", r, 2.6f);
            Building(env, -1, 5.0f, -6f, 2f, 6f, 2, "Intonaco", r, 2.6f);
            Building(env, -1, 3.5f, 2f, 10f, 6f, 4, "TufoScuro", r, 2.6f);
            Building(env, -1, 3.5f, 10f, 18f, 6f, 3, "Intonaco", r, 2.6f);
            Building(env, -1, 3.5f, 18f, 24f, 6f, 4, "Tufo", r, 2.6f);
            // case lato destro
            Building(env, 1, 3.5f, -24f, -6f, 6f, 3, "Intonaco", r, 2.6f);
            Building(env, 1, 3.5f, -6f, 8f, 6f, 4, "Tufo", r, 2.6f);
            Building(env, 1, 5.0f, 8f, 14f, 6f, 3, "TufoScuro", r, 2.6f);
            Building(env, 1, 3.5f, 14f, 24f, 6f, 3, "Intonaco", r, 2.6f);
            // chiusure del vicolo
            Box("ChiusuraNord", env, new Vector3(0f, 6f, 22f), new Vector3(14f, 12f, 4f), "Tufo");
            Box("ChiusuraSud", env, new Vector3(0f, 6f, -22f), new Vector3(14f, 12f, 4f), "TufoScuro");

            // panni stesi
            float[] lz = { -17f, -12f, -3.5f, 1.0f, 5.5f, 12.5f, 16.5f };
            for (int i = 0; i < lz.Length; i++)
            {
                float z = lz[i];
                float yL = 5.0f + (i % 3) * 0.9f;
                float yR = yL - 0.35f - (float)r.NextDouble() * 0.4f;
                Clothesline(env, r, z, yL, yR, -HubInnerLeft(z) + 0.05f, HubInnerRight(z) - 0.05f, 4 + (i % 3));
            }

            // oggetti di scena
            Rubble(env, r, new Vector3(2.1f, 0f, -16.5f), 10, 0.9f);
            Cart(env, new Vector3(3.6f, 0f, 9.5f));
            Bucket(env, new Vector3(3.0f, 0f, -8.0f));
            Bucket(env, new Vector3(-3.4f, 0f, 5.5f));
            Bucket(env, new Vector3(-4.4f, 0f, -3.4f));
            Lamp(env, new Vector3(2.7f, 0f, -5.5f));
            Lamp(env, new Vector3(-2.7f, 0f, 14f));
            Shrine(env, new Vector3(-3.5f, 0f, 6.5f), Vector3.right);
            Box("PanchinaTufo", env, new Vector3(-3.0f, 0.25f, 12.5f), new Vector3(0.5f, 0.5f, 1.8f), "Tufo");
            Box("CassaLegno", env, new Vector3(3.1f, 0.3f, 16.5f), new Vector3(0.7f, 0.6f, 0.7f), "Legno", new Vector3(0f, 25f, 0f));
            Box("CassaLegno", env, new Vector3(3.1f, 0.9f, 16.5f), new Vector3(0.5f, 0.4f, 0.5f), "Legno", new Vector3(0f, -10f, 0f));
            SetStatic(env.gameObject);

            // muri invisibili ai bordi
            var inv = new GameObject("MuriInvisibili").transform;
            InvisibleWall("MuroInvisibile_Ovest", inv, new Vector3(-5.8f, 4f, 0f), new Vector3(1f, 8f, 44f));
            InvisibleWall("MuroInvisibile_Est", inv, new Vector3(5.8f, 4f, 0f), new Vector3(1f, 8f, 44f));
            InvisibleWall("MuroInvisibile_Nord", inv, new Vector3(0f, 4f, 20.5f), new Vector3(14f, 8f, 1f));
            InvisibleWall("MuroInvisibile_Sud", inv, new Vector3(0f, 4f, -20.5f), new Vector3(14f, 8f, 1f));

            // spawn
            Spawn("inizio", new Vector3(0f, 0f, -13f), 0f, spawns);
            Spawn("da_basso_antonietta", new Vector3(-3.7f, 0f, -2f), 90f, spawns);

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.85f, 0.76f, 0.60f), true);
            BuildPlayer(new Vector3(0f, 0.05f, -13f), 0f, cam);
            SaveScene(scene, "10_Hub_Vicolo");
        }

        // ------------------------------------------------------------------ 20_Int_BassoAntonietta

        static void Chair(Transform parent, Vector3 pos, float yaw)
        {
            var root = Empty("Sedia", parent, pos);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Seduta", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.05f, 0.42f), "Legno");
            Box("Schienale", root.transform, new Vector3(0f, 0.78f, -0.19f), new Vector3(0.42f, 0.6f, 0.04f), "Legno");
            float o = 0.18f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("Gamba", root.transform, new Vector3(sx * o, 0.22f, sz * o), new Vector3(0.05f, 0.44f, 0.05f), "Legno", null, false);
        }

        static void BuildBasso()
        {
            var scene = NewScene();
            var r = new System.Random(1947);
            doorWallPoints.Clear();
            Ambient(new Color(0.62f, 0.52f, 0.40f), new Color(0.42f, 0.34f, 0.26f), new Color(0.24f, 0.19f, 0.14f));
            Sun(new Color(1f, 0.88f, 0.68f), 0.6f, new Vector3(55f, -25f, 0f));

            var env = new GameObject("Ambiente").transform;
            var props = new GameObject("Arredo").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // stanza 6 x 5 m, x in [-3,3], z in [-2.5,2.5], muri alti 3 m
            Box("Pavimento", env, new Vector3(0f, -0.1f, 0f), new Vector3(6.4f, 0.2f, 5.4f), "Legno");
            Box("MuroNord", env, new Vector3(0f, 1.5f, 2.6f), new Vector3(6.4f, 3f, 0.2f), "Intonaco");
            Box("MuroSud", env, new Vector3(0f, 1.5f, -2.6f), new Vector3(6.4f, 3f, 0.2f), "Intonaco");
            Box("MuroOvest", env, new Vector3(-3.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 5.4f), "Intonaco");
            Box("MuroEst", env, new Vector3(3.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 5.4f), "Intonaco");
            Box("ZoccoloNord", env, new Vector3(0f, 0.2f, 2.45f), new Vector3(6f, 0.4f, 0.1f), "TufoScuro", null, false);
            Box("ZoccoloEst", env, new Vector3(2.95f, 0.2f, 0f), new Vector3(0.1f, 0.4f, 5f), "TufoScuro", null, false);
            Box("ZoccoloOvest", env, new Vector3(-2.95f, 0.2f, 0f), new Vector3(0.1f, 0.4f, 5f), "TufoScuro", null, false);
            // travi a vista (senza soffitto, visibili solo come bordo)
            Box("Trave", env, new Vector3(0f, 3.05f, 0f), new Vector3(6.4f, 0.12f, 0.15f), "Legno", null, false);
            // finestrella chiusa sul muro nord
            Box("FinestraNord", env, new Vector3(1.6f, 1.9f, 2.47f), new Vector3(0.7f, 0.9f, 0.06f), "Finestra", null, false);
            Box("TelaioFinestra", env, new Vector3(1.6f, 1.9f, 2.45f), new Vector3(0.85f, 1.05f, 0.04f), "Legno", null, false);
            SetStatic(env.gameObject);

            // letto grande (testata al muro nord, lato ovest)
            var bed = Empty("Letto", props, new Vector3(-1.95f, 0f, 1.2f)).transform;
            Box("TelaioLetto", bed, new Vector3(0f, 0.3f, 0f), new Vector3(1.8f, 0.2f, 2.5f), "Legno");
            Box("Materasso", bed, new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 0.22f, 2.4f), "Tessuto3");
            Box("Coperta", bed, new Vector3(0f, 0.63f, -0.35f), new Vector3(1.72f, 0.06f, 1.6f), "Tessuto1", null, false);
            Box("Cuscino", bed, new Vector3(0f, 0.68f, 0.95f), new Vector3(0.9f, 0.14f, 0.4f), "Tessuto3", null, false);
            Box("Testata", bed, new Vector3(0f, 0.7f, 1.2f), new Vector3(1.8f, 1.0f, 0.1f), "Legno");
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("PiedeLetto", bed, new Vector3(sx * 0.85f, 0.1f, sz * 1.15f), new Vector3(0.1f, 0.2f, 0.1f), "Legno", null, false);

            // tavolo e sedie
            var table = Empty("Tavolo", props, new Vector3(1.6f, 0f, 0.9f)).transform;
            Box("PianoTavolo", table, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.06f, 0.8f), "Legno");
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("GambaTavolo", table, new Vector3(sx * 0.54f, 0.36f, sz * 0.34f), new Vector3(0.07f, 0.72f, 0.07f), "Legno", null, false);
            Cyl("Brocca", table, new Vector3(-0.2f, 0.9f, 0.05f), 0.14f, 0.24f, "TufoScuro", null, false);
            Cyl("Piatto", table, new Vector3(0.25f, 0.79f, -0.1f), 0.28f, 0.02f, "Cera", null, false);
            Chair(props, new Vector3(0.75f, 0f, 0.9f), 90f);
            Chair(props, new Vector3(2.45f, 0f, 0.9f), -90f);

            // braciere
            var br = Empty("Braciere", props, new Vector3(-0.1f, 0f, -0.5f)).transform;
            Cyl("CiotolaBraciere", br, new Vector3(0f, 0.4f, 0f), 0.55f, 0.14f, "Ferro");
            Cyl("PiedeBraciere", br, new Vector3(0f, 0.2f, 0f), 0.1f, 0.4f, "Ferro", null, false);
            Cyl("BaseBraciere", br, new Vector3(0f, 0.03f, 0f), 0.4f, 0.06f, "Ferro", null, false);
            Cyl("Brace", br, new Vector3(0f, 0.46f, 0f), 0.45f, 0.03f, "Glow", null, false);

            // cassapanca
            var chest = Empty("Cassapanca", props, new Vector3(2.6f, 0f, -0.6f)).transform;
            Box("CorpoCassapanca", chest, new Vector3(0f, 0.28f, 0f), new Vector3(0.5f, 0.56f, 1.2f), "Legno");
            Box("CoperchioCassapanca", chest, new Vector3(0f, 0.58f, 0f), new Vector3(0.54f, 0.06f, 1.24f), "Legno", null, false);
            Box("SerraturaCassapanca", chest, new Vector3(-0.27f, 0.45f, 0f), new Vector3(0.03f, 0.1f, 0.1f), "Ferro", null, false);

            // altarino con lumino
            var alt = Empty("Altarino", props, new Vector3(0.2f, 0f, 2.3f)).transform;
            Box("MensolaAltarino", alt, new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 0.06f, 0.3f), "Legno");
            Box("SostegnoAltarino", alt, new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 1.0f, 0.2f), "TufoScuro");
            Box("NicchiaAltarino", alt, new Vector3(0f, 1.5f, 0.1f), new Vector3(0.55f, 0.8f, 0.1f), "Finestra", null, false);
            Box("StatuettaAltarino", alt, new Vector3(0f, 1.3f, 0.02f), new Vector3(0.12f, 0.36f, 0.08f), "Cera", null, false);
            Cyl("CeroAltarino", alt, new Vector3(0.22f, 1.1f, -0.02f), 0.06f, 0.14f, "Cera", null, false);
            Prim(PrimitiveType.Sphere, "LuminoAltarino", alt, new Vector3(0.22f, 1.22f, -0.02f), new Vector3(0.07f, 0.1f, 0.07f), "Glow", null, false);

            // tenda divisoria con bastone
            Cyl("BastoneTenda", props, new Vector3(-0.95f, 2.45f, 1.65f), 0.04f, 1.6f, "Ferro", new Vector3(90f, 0f, 0f), false);
            Box("Tenda", props, new Vector3(-0.95f, 1.25f, 1.65f), new Vector3(0.04f, 2.3f, 1.5f), "Tessuto2", null, false);

            // panni appesi
            Clothesline(props, r, 0.3f, 2.5f, 2.4f, -0.8f, 2.9f, 3);

            // porta d'uscita (muro sud, facciata interna a z=-2.5)
            var door = Box("PortaUscita", props, new Vector3(0f, 1.05f, -2.35f), new Vector3(1.1f, 2.1f, 0.2f), "Legno");
            Box("MuroCorniceUscita", env, new Vector3(0f, 1.15f, -2.47f), new Vector3(1.4f, 2.3f, 0.06f), "TufoScuro"); // "Muro*": la camera lo rende trasparente
            var d = door.AddComponent<Door>();
            d.targetScene = "10_Hub_Vicolo";
            d.targetSpawn = "da_basso_antonietta";

            // le scarpe (due blocchi a forma di scarpa) vicino alla porta
            var shoes = Empty("Scarpe", props, new Vector3(1.3f, 0f, -2.0f));
            BuildShoe(shoes.transform, new Vector3(-0.1f, 0f, 0f), 8f);
            BuildShoe(shoes.transform, new Vector3(0.12f, 0f, 0.03f), -4f);
            var bc = shoes.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.08f, 0f);
            bc.size = new Vector3(0.5f, 0.18f, 0.42f);
            shoes.AddComponent<Glow>();
            var pick = shoes.AddComponent<ItemPickup>();
            pick.oggettoId = "scarpe";

            // luce calda (una sola Point Light)
            var lgo = new GameObject("Luce Calda");
            lgo.transform.position = new Vector3(0.3f, 2.4f, 0.2f);
            var pl = lgo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.70f, 0.40f);
            pl.intensity = 4f;
            pl.range = 9f;
            pl.shadows = LightShadows.None;

            Spawn("ingresso", new Vector3(0f, 0f, -1.7f), 0f, spawns);
            Spawn("oggetto_scarpe", new Vector3(0.7f, 0f, -1.4f), 90f, spawns);

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.10f, 0.07f, 0.05f), true);
            BuildPlayer(new Vector3(0f, 0.05f, -1.7f), 0f, cam, true);
            SaveScene(scene, "20_Int_BassoAntonietta");
        }

        // scarpa: suola + tomaia + puntale (blocchi), lunga ~0.28 m
        static void BuildShoe(Transform parent, Vector3 pos, float yaw)
        {
            var s = Empty("Scarpa", parent, pos);
            s.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box("Suola", s.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.11f, 0.04f, 0.28f), "Ferro", null, false);
            Box("Tallone", s.transform, new Vector3(0f, 0.08f, -0.08f), new Vector3(0.1f, 0.08f, 0.12f), "Legno", null, false);
            Box("Tomaia", s.transform, new Vector3(0f, 0.06f, 0.04f), new Vector3(0.1f, 0.06f, 0.16f), "Legno", null, false);
            Box("Punta", s.transform, new Vector3(0f, 0.05f, 0.11f), new Vector3(0.1f, 0.04f, 0.08f), "Legno", null, false);
        }

        // ------------------------------------------------------------------ 30_FB_Scarpe

        static void BuildFlashback()
        {
            var scene = NewScene();
            doorWallPoints.Clear();
            Color sepiaFog = new Color(0.62f, 0.50f, 0.34f);
            Ambient(new Color(0.72f, 0.60f, 0.42f), new Color(0.52f, 0.42f, 0.30f), new Color(0.28f, 0.22f, 0.15f));
            Sun(new Color(1f, 0.86f, 0.62f), 0.9f, new Vector3(55f, -40f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sepiaFog;
            RenderSettings.fogStartDistance = 4f;
            RenderSettings.fogEndDistance = 22f;

            var env = new GameObject("Ambiente").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // stanza 10 x 10 m, muri alti 4 m
            Box("Pavimento", env, new Vector3(0f, -0.1f, 0f), new Vector3(10.4f, 0.2f, 10.4f), "Ricordo");
            Box("MuroNord", env, new Vector3(0f, 2f, 5.1f), new Vector3(10.4f, 4f, 0.2f), "Ricordo");
            Box("MuroSud", env, new Vector3(0f, 2f, -5.1f), new Vector3(10.4f, 4f, 0.2f), "Ricordo");
            Box("MuroOvest", env, new Vector3(-5.1f, 2f, 0f), new Vector3(0.2f, 4f, 10.4f), "Ricordo");
            Box("MuroEst", env, new Vector3(5.1f, 2f, 0f), new Vector3(0.2f, 4f, 10.4f), "Ricordo");
            // pilastri e zoccolo
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box("Pilastro", env, new Vector3(sx * 4.75f, 2f, sz * 4.75f), new Vector3(0.5f, 4f, 0.5f), "Ricordo");
            Box("FasciaNord", env, new Vector3(0f, 0.3f, 4.95f), new Vector3(9f, 0.6f, 0.1f), "TufoScuro", null, false);
            Box("FasciaSud", env, new Vector3(0f, 0.3f, -4.95f), new Vector3(9f, 0.6f, 0.1f), "TufoScuro", null, false);
            Box("FasciaOvest", env, new Vector3(-4.95f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 9f), "TufoScuro", null, false);
            Box("FasciaEst", env, new Vector3(4.95f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 9f), "TufoScuro", null, false);
            // finestra di luce sbiadita sul muro ovest
            Box("FinestraRicordo", env, new Vector3(-4.97f, 2.1f, 0f), new Vector3(0.06f, 2.0f, 2.6f), "Glow", null, false);
            Box("TelaioFinestraRicordo", env, new Vector3(-4.95f, 2.1f, 0f), new Vector3(0.05f, 2.2f, 0.1f), "Legno", null, false);
            Box("TelaioFinestraRicordo", env, new Vector3(-4.95f, 2.1f, 0f), new Vector3(0.05f, 0.1f, 2.8f), "Legno", null, false);
            // arredi leggeri lungo i muri (centro libero per gli enigmi)
            Box("Cassa", env, new Vector3(-4.3f, 0.3f, -4.2f), new Vector3(0.8f, 0.6f, 0.8f), "Legno", new Vector3(0f, 15f, 0f));
            Box("Cassa", env, new Vector3(-3.4f, 0.2f, -4.4f), new Vector3(0.5f, 0.4f, 0.5f), "Legno", new Vector3(0f, -20f, 0f));
            Box("Mensola", env, new Vector3(2.5f, 0.45f, 4.6f), new Vector3(2.0f, 0.9f, 0.5f), "Legno");
            Box("Panca", env, new Vector3(-4.5f, 0.25f, 3.4f), new Vector3(0.5f, 0.5f, 1.8f), "Legno");
            Box("Sgabello", env, new Vector3(4.3f, 0.25f, -3.6f), new Vector3(0.45f, 0.5f, 0.45f), "Legno");
            SetStatic(env.gameObject);

            // uscita: porta luminosa sul muro est
            var exitRoot = new GameObject("Uscita").transform;
            Box("CorniceUscita", exitRoot, new Vector3(4.96f, 1.25f, 0f), new Vector3(0.06f, 2.5f, 1.6f), "Legno", null, false);
            var exit = Box("EscapeExit", exitRoot, new Vector3(4.88f, 1.1f, 0f), new Vector3(0.2f, 2.2f, 1.2f), "Glow");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_scarpe";

            Spawn("inizio", new Vector3(0f, 0f, 0f), 0f, spawns);

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, sepiaFog, true);
            BuildPlayer(new Vector3(0f, 0.05f, 0f), 0f, cam);
            SaveScene(scene, "30_FB_Scarpe");
        }
    }
}
