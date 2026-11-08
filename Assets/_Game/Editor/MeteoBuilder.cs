using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TrenoBambini.Core;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R007 tappa 3B e 4: materiali di pioggia/spruzzi/pozzanghera, componente MeteoController nelle scene (hub, basso, ricordo), decal URP di umidita',
    /// salnitro e macchie d'acqua alla base delle facciate (hub e ricordo). Chiamato da LookBuilder.ApplySceneLook; il feature Decal va nei renderer
    /// Media/Alta/Ultra (menu Treno/Look/Aggiungi decal ai renderer). Il renderer Lite (Bassa/WebGL) non disegna i decal.
    /// </summary>
    public static class MeteoBuilder
    {
        const string Mats = "Assets/Art/Materials";
        const string TexDecal = "Assets/Art/Textures/Decal";

        static Material Particelle(string nome)
        {
            string path = Mats + "/" + nome + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", Color.white);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Pozzanghera()
        {
            string path = Mats + "/M_Pozzanghera.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(0.13f, 0.14f, 0.16f, 1f));
            m.SetFloat("_Smoothness", 0.9f); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Surface", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Decal(string nome, string tex)
        {
            string path = Mats + "/" + nome + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find("Shader Graphs/Decal");
            if (sh == null) { Debug.LogWarning("[Meteo] Shader Graphs/Decal non trovato"); return null; }
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDecal + "/" + tex);
            if (t != null)
            {
                var imp = AssetImporter.GetAtPath(TexDecal + "/" + tex) as TextureImporter;
                if (imp != null && (imp.wrapMode != TextureWrapMode.Repeat || !imp.alphaIsTransparency || imp.maxTextureSize != 512))
                {
                    imp.wrapMode = TextureWrapMode.Repeat; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
                    imp.maxTextureSize = 512; imp.sRGBTexture = true; imp.mipmapEnabled = true;
                    imp.SaveAndReimport();
                    t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDecal + "/" + tex);
                }
                m.SetTexture("Base_Map", t);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Aggiunge (o rifa) il controllore del meteo nella scena. Il suo comportamento dipende dal campo meteo di luoghi.json.</summary>
        public static void AddMeteo(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == "Meteo") Object.DestroyImmediate(root);
            if (name != "10_Hub_Vicolo" && name != "20_Int_BassoAntonietta" && name != "30_FB_Scarpe") return;
            var go = new GameObject("Meteo");
            var mc = go.AddComponent<MeteoController>();
            mc.materialePioggia = Particelle("M_Pioggia");
            mc.materialeSpruzzo = Particelle("M_Spruzzo");
            mc.materialePozzanghera = Pozzanghera();
            mc.suoloBagnato = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/M_BasoliLavica_Bagnato.mat");
            if (name == "20_Int_BassoAntonietta") mc.cortileDaZ = 9.3f;
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        /// <summary>Decal di umidita'/salnitro e macchie d'acqua alla base delle facciate Casa_E_*/Casa_O_* (hub e ricordo). Uno per facciata, due varianti alternate.</summary>
        public static void AddDecals(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == "Decal_Muri") Object.DestroyImmediate(root);
            if (name != "10_Hub_Vicolo" && name != "30_FB_Scarpe") return;
            var mA = Decal("M_DecalUmido", "DecalUmido.png");
            var mB = Decal("M_DecalMacchie", "DecalMacchie.png");
            if (mA == null || mB == null) return;
            var padre = new GameObject("Decal_Muri");
            SceneManager.MoveGameObjectToScene(padre, scene);
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Ambiente") continue;
                foreach (Transform t in root.transform)
                {
                    bool est = t.name.StartsWith("Casa_E"), ovest = t.name.StartsWith("Casa_O");
                    if (!est && !ovest) continue;
                    Bounds b = new Bounds(); bool primo = true;
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (primo) { b = r.bounds; primo = false; } else b.Encapsulate(r.bounds); }
                    if (primo || b.size.z < 1.5f) continue;
                    float xf = est ? b.min.x : b.max.x;                    // la facciata guarda la strada (x=0)
                    float dirx = est ? 1f : -1f;                           // si proietta verso il muro
                    var g = new GameObject("Decal_" + t.name);
                    g.transform.SetParent(padre.transform, false);
                    g.transform.position = new Vector3(xf - dirx * 0.35f, 0.75f, b.center.z);
                    g.transform.rotation = Quaternion.LookRotation(new Vector3(dirx, 0f, 0f), Vector3.up);
                    var dp = g.AddComponent<DecalProjector>();
                    dp.material = (n % 2 == 0) ? mA : mB;
                    dp.size = new Vector3(Mathf.Min(b.size.z, 14f), 1.5f, 0.9f);
                    dp.pivot = new Vector3(0f, 0f, 0.45f);
                    dp.uvScale = new Vector2(Mathf.Min(b.size.z, 14f) / 5f, 1f);
                    dp.uvBias = new Vector2((n * 0.37f) % 1f, 0f);
                    dp.fadeFactor = 1f; dp.drawDistance = 28f; dp.startAngleFade = 70f; dp.endAngleFade = 90f;
                    n++;
                }
            }
            Debug.Log("[Meteo] " + name + ": " + n + " decal di umidita' alla base delle facciate.");
        }

        // inquadrature di riferimento (le stesse delle foto t1): scena, nome, camera, punto guardato
        static readonly object[][] Punti =
        {
            new object[] { "10_Hub_Vicolo", "hub_vico_salita", new Vector3(0f, 1.7f, -20f), new Vector3(0f, 2.2f, 6f) },
            new object[] { "10_Hub_Vicolo", "hub_edicola_votiva", new Vector3(0f, 1.7f, 28f), new Vector3(-7.2f, 5.2f, 37f) },
            new object[] { "10_Hub_Vicolo", "hub_piazzetta_fontanella", new Vector3(4f, 1.7f, -36f), new Vector3(0f, 2.5f, -15f) },
            new object[] { "20_Int_BassoAntonietta", "basso_stanza", new Vector3(-3.9f, 1.65f, -2.6f), new Vector3(1.5f, 1.3f, 2.5f) },
            new object[] { "20_Int_BassoAntonietta", "basso_cortile", new Vector3(3.6f, 2.6f, 8.6f), new Vector3(3.6f, 1.2f, 13f) },
            new object[] { "30_FB_Scarpe", "fb_via_bottega_calzolaio", new Vector3(-4.5f, 1.7f, -1f), new Vector3(-9f, 1.5f, -3.5f) },
            new object[] { "30_FB_Scarpe", "fb_via_piazzetta", new Vector3(3f, 1.7f, 20f), new Vector3(-1f, 1.6f, 32f) },
        };

        /// <summary>Foto finali t4_&lt;nome&gt;_&lt;preset&gt;.jpg con pioggia (anteprima nell'editor). NON salvare le scene dopo: le modifiche dell'anteprima sono solo temporanee.</summary>
        public static void FotoFinali(int preset, string suffisso)
        {
            foreach (var q in Punti)
            {
                string scena = (string)q[0];
                string pth = "Assets/_Game/Scenes/" + scena + ".unity";
                if (EditorSceneManager.GetActiveScene().path != pth) EditorSceneManager.OpenScene(pth, OpenSceneMode.Single);
                GraphicsPresets.Apply(preset, false);
                var pos = (Vector3)q[2];
                if (scena == "10_Hub_Vicolo")
                {
                    Physics.SyncTransforms();
                    foreach (var h in Physics.RaycastAll(new Vector3(pos.x, 60f, pos.z), Vector3.down, 100f))
                        if (h.collider.name.StartsWith("Selciato_")) { pos.y += h.point.y; break; }
                }
                var mc = Object.FindAnyObjectByType<MeteoController>();
                if (mc != null) mc.Anteprima(pos);
                var e = Quaternion.LookRotation((Vector3)q[3] - pos).eulerAngles;
                FotoDocumentazione.Scatta(scena, pos, e.y, e.x > 180f ? e.x - 360f : e.x, preset, "t4_" + (string)q[1] + "_" + suffisso);
            }
        }

        [MenuItem("Treno/Look/Aggiungi decal ai renderer")]
        public static void AddDecalFeature()
        {
            foreach (var n in new[] { "Media", "Alta", "Ultra" })
            {
                var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Quality/URP_Renderer_" + n + ".asset");
                if (rd == null) continue;
                bool ha = false;
                foreach (var f in rd.rendererFeatures) if (f is DecalRendererFeature) ha = true;
                if (ha) continue;
                var feat = ScriptableObject.CreateInstance<DecalRendererFeature>();
                feat.name = "Decal";
                AssetDatabase.AddObjectToAsset(feat, rd);
                var so = new SerializedObject(rd);
                var arr = so.FindProperty("m_RendererFeatures");
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = feat;
                var map = so.FindProperty("m_RendererFeatureMap");
                string guid; long id;
                if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out guid, out id))
                {
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rd);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Meteo] DecalRendererFeature nei renderer Media/Alta/Ultra.");
        }
    }
}
