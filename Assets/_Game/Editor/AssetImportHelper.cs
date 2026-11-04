using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Fase 2.2: importa i set PBR (Assets/Art/Textures/SurfaceManifest.json), gli HDRI e i modelli
    /// (Assets/Art/Models/ModelManifest.json), impacchetta le mappe (metallic R + smoothness A) e crea i
    /// materiali URP Lit in Assets/Art/Materials.
    /// Rilanciabile: i materiali vengono aggiornati sul posto. Le texture dei modelli si rigenerano solo se la cache
    /// dei download (EditorPrefs "Treno.AssetCache") e' disponibile; altrimenti si riusano quelle gia' nel progetto.
    /// Menu: Treno/Asset/Importa asset 2.2
    /// </summary>
    public static class AssetImportHelper
    {
        const string TexRoot = "Assets/Art/Textures";
        const string HdriRoot = "Assets/Art/HDRI";
        const string ModelRoot = "Assets/Art/Models";
        public const string MatRoot = "Assets/Art/Materials";

        // leggera desaturazione / tinta seppia applicata al colore base (i file originali restano intatti)
        static readonly Color EpochTint = new Color(0.95f, 0.91f, 0.84f, 1f);

        [Serializable] class SurfaceEntry { public string name; public string src; public string res; }
        [Serializable] class SurfaceList { public List<SurfaceEntry> surfaces = new List<SurfaceEntry>(); }
        [Serializable]
        class ModelMat
        {
            public string name, @base, mr, normal, emissive, alphaMode;
            public float[] baseFactor, emissiveFactor;
            public float metal = 1f, rough = 1f;
            public bool doubleSided;
        }
        [Serializable] class ModelEntry { public string name, fbx; public List<ModelMat> materials = new List<ModelMat>(); }
        [Serializable] class ModelList { public List<ModelEntry> models = new List<ModelEntry>(); }

        [MenuItem("Treno/Asset/Importa asset 2.2")]
        public static void ImportAll()
        {
            try
            {
                EnsureFolder(MatRoot);
                ImportSurfaces();
                ImportHdris();
                ImportModels();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[AssetImportHelper] Import completato.");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ================================================================== superfici PBR

        static void ImportSurfaces()
        {
            var list = JsonUtility.FromJson<SurfaceList>(File.ReadAllText(TexRoot + "/SurfaceManifest.json"));
            for (int i = 0; i < list.surfaces.Count; i++)
            {
                var s = list.surfaces[i];
                EditorUtility.DisplayProgressBar("Superfici", s.name, (float)i / list.surfaces.Count);
                string d = TexRoot + "/" + s.name;
                string alb = d + "/" + s.name + "_Albedo.jpg";
                string nor = d + "/" + s.name + "_Normal.jpg";
                string rough = d + "/" + s.name + "_Rough.jpg";
                string ms = d + "/" + s.name + "_MS.png";
                string wet = d + "/" + s.name + "_MS_Bagnato.png";
                bool road = s.name.StartsWith("Basoli");
                int max = s.res == "2k" ? 2048 : 1024;

                if (File.Exists(rough))
                {
                    var rt = LoadRaw(rough);
                    WriteMs(rt, ms, null, 0f, 1f, 1f, 1024);
                    if (road) WriteMs(rt, wet, null, 0f, 0.38f, 1f, 1024); // bagnato: roughness ridotta
                    UnityEngine.Object.DestroyImmediate(rt);
                    AssetDatabase.DeleteAsset(rough);
                }
                AssetDatabase.ImportAsset(ms);
                if (road) AssetDatabase.ImportAsset(wet);
                SetTex(alb, TexKind.Color, max);
                SetTex(nor, TexKind.Normal, max);
                SetTex(ms, TexKind.Data, 1024);
                if (road) SetTex(wet, TexKind.Data, 1024);
                string aoP = d + "/" + s.name + "_AO.jpg";
                if (File.Exists(aoP)) SetTex(aoP, TexKind.Data, 1024); else aoP = null;
                var ao = aoP != null ? Load(aoP) : null;

                MakeLit(MatRoot + "/M_" + s.name + ".mat", Load(alb), Load(nor), Load(ms), EpochTint, null, Color.black, "OPAQUE", false, 1f, ao);
                if (road)
                    MakeLit(MatRoot + "/M_" + s.name + "_Bagnato.mat", Load(alb), Load(nor), Load(wet), EpochTint, null, Color.black, "OPAQUE", false, 1f, ao);
            }
        }

        // ================================================================== HDRI

        static void ImportHdris()
        {
            foreach (var dir in Directory.GetDirectories(HdriRoot))
            {
                foreach (var f in Directory.GetFiles(dir, "*.hdr"))
                {
                    string p = f.Replace('\\', '/');
                    AssetDatabase.ImportAsset(p);
                    var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                    imp.textureShape = TextureImporterShape.TextureCube;
                    imp.sRGBTexture = false;
                    imp.mipmapEnabled = true;
                    imp.maxTextureSize = 1024;
                    imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.SaveAndReimport();
                    string name = Path.GetFileNameWithoutExtension(p).Replace("_1k", "");
                    var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(p);
                    string matPath = MatRoot + "/M_Sky_" + name + ".mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (m == null)
                    {
                        m = new Material(Shader.Find("Skybox/Cubemap"));
                        AssetDatabase.CreateAsset(m, matPath);
                    }
                    if (cube != null) m.SetTexture("_Tex", cube);
                    EditorUtility.SetDirty(m);
                }
            }
        }

        // ================================================================== modelli

        const string CachePref = "Treno.AssetCache";

        /// <summary>
        /// Cartella cache dei download (contiene "polyhaven/" e "sketchfab/"), da EditorPrefs "Treno.AssetCache".
        /// Serve solo per rigenerare le texture dei modelli; senza cache si riusano quelle gia presenti nel progetto.
        /// </summary>
        static string CacheRoot()
        {
            string c = EditorPrefs.GetString(CachePref, "");
            if (string.IsNullOrEmpty(c) || !Directory.Exists(c))
            {
                Debug.LogWarning("[AssetImportHelper] Cache dei download non impostata o inesistente. Per rigenerare le texture dei modelli " +
                                 "imposta EditorPrefs \"" + CachePref + "\" alla cartella che contiene polyhaven/ e sketchfab/ " +
                                 "(es. EditorPrefs.SetString(\"" + CachePref + "\", \"D:/asset-cache\")). Uso le texture gia presenti in Assets/Art/Models.");
                return null;
            }
            return c;
        }

        static string Src(string cache, string rel)
        {
            if (cache == null || string.IsNullOrEmpty(rel)) return null;
            string p = Path.Combine(cache, rel);
            return File.Exists(p) ? p : null;
        }

        static string Existing(string p) { return File.Exists(p) ? p : null; }

        static void ImportModels()
        {
            var list = JsonUtility.FromJson<ModelList>(File.ReadAllText(ModelRoot + "/ModelManifest.json"));
            string cache = CacheRoot();
            for (int i = 0; i < list.models.Count; i++)
            {
                var e = list.models[i];
                EditorUtility.DisplayProgressBar("Modelli", e.name, (float)i / list.models.Count);
                string td = ModelRoot + "/" + e.name + "/Textures";
                EnsureFolder(td);
                var mats = new Dictionary<string, Material>();
                foreach (var m in e.materials)
                {
                    string key = Sanitize(m.name);
                    string pre = td + "/" + e.name + "_" + key;
                    bool opaque = m.alphaMode == "OPAQUE";
                    bool png = !opaque;
                    string srcBase = Src(cache, m.@base), srcNor = Src(cache, m.normal), srcMr = Src(cache, m.mr), srcEm = Src(cache, m.emissive);

                    // sorgente in cache -> rigenera; altrimenti riusa il file gia nel progetto (se manca l'albedo salta il materiale)
                    string albP = srcBase != null ? SaveResized(srcBase, 1024, pre + "_Albedo", png) : Existing(pre + "_Albedo" + (png ? ".png" : ".jpg"));
                    string norP = srcNor != null ? SaveResized(srcNor, 1024, pre + "_Normal", false) : Existing(pre + "_Normal.jpg");
                    string emP = srcEm != null ? SaveResized(srcEm, 512, pre + "_Emissive", false) : Existing(pre + "_Emissive.jpg");
                    string msP = Existing(pre + "_MS.png"), aoP = Existing(pre + "_AO.jpg");
                    if (srcMr != null)
                    {
                        var t = LoadRaw(srcMr);
                        msP = pre + "_MS.png";
                        WriteMs(t, msP, m.mr, 1f, 1f, 1f, 1024, true, m.metal, m.rough);
                        // Poly Haven: la mappa ARM ha l'occlusion nel canale R
                        if (Path.GetFileName(srcMr).Contains("_arm_")) aoP = WriteAo(t, pre + "_AO.jpg");
                        UnityEngine.Object.DestroyImmediate(t);
                    }
                    if (albP == null)
                    {
                        Debug.LogWarning("[AssetImportHelper] Materiale saltato (nessuna texture base ne in cache ne nel progetto): " + e.name + "/" + m.name);
                        continue;
                    }
                    foreach (var p in new[] { albP, norP, msP, emP, aoP })
                        if (p != null) AssetDatabase.ImportAsset(p);
                    SetTex(albP, TexKind.Color, 1024, png);
                    if (norP != null) SetTex(norP, TexKind.Normal, 1024);
                    if (msP != null) SetTex(msP, TexKind.Data, 1024);
                    if (aoP != null) SetTex(aoP, TexKind.Data, 1024);
                    if (emP != null) SetTex(emP, TexKind.Color, 512);

                    var bf = m.baseFactor != null && m.baseFactor.Length >= 4 ? new Color(m.baseFactor[0], m.baseFactor[1], m.baseFactor[2], m.baseFactor[3]) : Color.white;
                    var ef = m.emissiveFactor != null && m.emissiveFactor.Length >= 3 ? new Color(m.emissiveFactor[0], m.emissiveFactor[1], m.emissiveFactor[2], 1f) : Color.black;
                    bool hasEm = emP != null;
                    var tint = new Color(EpochTint.r * bf.r, EpochTint.g * bf.g, EpochTint.b * bf.b, bf.a);
                    var mat = MakeLit(MatRoot + "/M_" + e.name + (e.materials.Count > 1 ? "_" + key : "") + ".mat",
                        Load(albP), norP != null ? Load(norP) : null, msP != null ? Load(msP) : null,
                        tint, hasEm ? Load(emP) : null, hasEm ? (ef == Color.black ? Color.white : ef) : Color.black, m.alphaMode, m.doubleSided,
                        msP == null ? 1f - Mathf.Clamp01(m.rough) : 1f, aoP != null ? Load(aoP) : null);
                    mats[m.name] = mat;
                }

                string fbx = e.fbx;
                AssetDatabase.ImportAsset(fbx);
                var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
                mi.globalScale = 0.01f; // l'FBX di Blender esce in centimetri: 1 unita Unity = 1 m
                mi.useFileScale = false;
                mi.generateSecondaryUV = true;
                mi.importCameras = false;
                mi.importLights = false;
                mi.importBlendShapes = false;
                mi.isReadable = false;
                mi.meshCompression = ModelImporterMeshCompression.Off;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                foreach (var kv in mats)
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport();
            }
        }

        /// <summary>Legge, ridimensiona (max px) e salva come JPG/PNG liberando le texture temporanee.</summary>
        static string SaveResized(string srcFile, int max, string pathNoExt, bool png)
        {
            var t = LoadRaw(srcFile);
            var r = Resize(t, max);
            string p = Save(r, pathNoExt, png);
            if (r != t) UnityEngine.Object.DestroyImmediate(r);
            UnityEngine.Object.DestroyImmediate(t);
            return p;
        }

        /// <summary>Occlusion dal canale R di una mappa ARM, salvata come grigio JPG.</summary>
        static string WriteAo(Texture2D arm, string path)
        {
            var t = Resize(arm, 1024);
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++) { byte v = px[i].r; px[i] = new Color32(v, v, v, 255); }
            var o = new Texture2D(t.width, t.height, TextureFormat.RGB24, false, true);
            o.SetPixels32(px);
            o.Apply();
            File.WriteAllBytes(path, o.EncodeToJPG(90));
            UnityEngine.Object.DestroyImmediate(o);
            if (t != arm) UnityEngine.Object.DestroyImmediate(t);
            return path;
        }

        // ================================================================== util texture

        enum TexKind { Color, Normal, Data }

        static void SetTex(string path, TexKind kind, int maxSize, bool alpha = false)
        {
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[AssetImportHelper] Importer mancante: " + path); return; }
            imp.textureType = kind == TexKind.Normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = kind == TexKind.Color;
            imp.alphaSource = (kind == TexKind.Data || alpha) ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.alphaIsTransparency = alpha;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.crunchedCompression = false;
            imp.isReadable = false;
            imp.SaveAndReimport();
        }

        static Texture2D Load(string path) { return AssetDatabase.LoadAssetAtPath<Texture2D>(path); }

        /// <summary>Carica i byte del file senza conversione colore (texture marcata lineare = copia pura).</summary>
        static Texture2D LoadRaw(string path)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(t, File.ReadAllBytes(path), false))
                Debug.LogError("[AssetImportHelper] Immagine illeggibile: " + path);
            return t;
        }

        static Texture2D Resize(Texture2D src, int max)
        {
            int w = src.width, h = src.height;
            if (Mathf.Max(w, h) <= max) return src;
            float k = (float)max / Mathf.Max(w, h);
            int nw = Mathf.Max(4, Mathf.RoundToInt(w * k)), nh = Mathf.Max(4, Mathf.RoundToInt(h * k));
            var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var o = new Texture2D(nw, nh, TextureFormat.RGBA32, false, true);
            o.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
            o.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return o;
        }

        /// <summary>Salva la texture come JPG (o PNG se serve l'alfa); ritorna il percorso asset.</summary>
        static string Save(Texture2D t, string pathNoExt, bool png)
        {
            string p = pathNoExt + (png ? ".png" : ".jpg");
            File.WriteAllBytes(p, png ? t.EncodeToPNG() : t.EncodeToJPG(92));
            return p;
        }

        /// <summary>
        /// Scrive la mappa metallic(R)/smoothness(A). Sorgente "roughness" semplice (canale R) oppure glTF
        /// metallicRoughness/ARM (G = roughness, B = metallic).
        /// </summary>
        static void WriteMs(Texture2D src, string outPath, string srcPath, float metalConst, float roughMul, float unused, int max,
                            bool packed = false, float metalFactor = 1f, float roughFactor = 1f)
        {
            var t = Resize(src, max);
            var px = t.GetPixels32();
            var o = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                float rough = (packed ? px[i].g : px[i].r) / 255f;
                float metal = packed ? px[i].b / 255f * metalFactor : metalConst;
                rough = Mathf.Clamp01(rough * (packed ? roughFactor : 1f) * roughMul);
                byte a = (byte)Mathf.RoundToInt((1f - rough) * 255f);
                byte m = (byte)Mathf.RoundToInt(Mathf.Clamp01(metal) * 255f);
                o[i] = new Color32(m, m, m, a);
            }
            var r = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false, true);
            r.SetPixels32(o);
            r.Apply();
            File.WriteAllBytes(outPath, r.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(r);
            if (t != src) UnityEngine.Object.DestroyImmediate(t);
        }

        // ================================================================== util materiali

        public static Material MakeLit(string path, Texture2D albedo, Texture2D normal, Texture2D ms, Color tint,
                                       Texture2D emission, Color emissionColor, string alphaMode, bool doubleSided,
                                       float smoothnessScalar = 1f, Texture2D occlusion = null)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", tint);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            else m.DisableKeyword("_NORMALMAP");
            if (ms != null)
            {
                m.SetTexture("_MetallicGlossMap", ms);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_Metallic", 0f);
            }
            else
            {
                m.DisableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", smoothnessScalar);
                m.SetFloat("_Metallic", 0f);
            }
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            if (occlusion != null)
            {
                m.SetTexture("_OcclusionMap", occlusion);
                m.SetFloat("_OcclusionStrength", 1f);
                m.EnableKeyword("_OCCLUSIONMAP");
            }
            else m.DisableKeyword("_OCCLUSIONMAP");
            m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            if (emission != null)
            {
                m.SetTexture("_EmissionMap", emission);
                m.SetColor("_EmissionColor", emissionColor);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
            }

            m.SetFloat("_AlphaClip", 0f);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_SrcBlend", 1f);
            m.SetFloat("_DstBlend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.SetOverrideTag("RenderType", "Opaque");
            m.renderQueue = -1;
            if (alphaMode == "MASK")
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = 2450;
            }
            else if (alphaMode == "BLEND")
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", 5f);
                m.SetFloat("_DstBlend", 10f);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_').Replace('.', '_');
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
