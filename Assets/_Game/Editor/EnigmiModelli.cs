using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R020: modelli veri degli enigmi. Dagli FBX di Assets/Art/Models/Enigmi (generati da ArtSource/Models/Enigmi/genera_enigmi_v2.py, texture da texture_enigmi.py):
    /// 1) imposta l'importazione delle texture (normal map, mappe lineari, dimensione massima);
    /// 2) crea i materiali URP Lit ME_* / MT_* / MX_* in Resources/Materiali/Enigmi (PBR: albedo, normal, metallo+occlusione+levigatezza);
    /// 3) importa gli FBX rimappando gli slot sui materiali per nome, crea i prefab in Resources/Modelli/Enigmi (dove MondoFab li cerca, ParteDef.modello).
    /// Menu: Treno > Enigmi > Crea prefab dei modelli. I font d'epoca: Treno > Enigmi > Crea font d'epoca.
    /// </summary>
    public static class EnigmiModelli
    {
        const string Fbx = "Assets/Art/Models/Enigmi";
        const string Dest = "Assets/_Game/Resources/Modelli/Enigmi";
        const string Mats = "Assets/_Game/Resources/Materiali/Enigmi";
        const string TexE = "Assets/Art/Textures/Enigmi/";
        const string TexP = "Assets/Art/Textures/";

        class Def
        {
            public string a, n, ms, ao; public Color tinta = Color.white; public float smooth = 0.5f, metal = 0f, bump = 1f, cutoff = -1f;
            public bool vetro; public Vector2 tile = Vector2.one;
        }

        static Def D(string a, string n = null, string ms = null, string ao = null, float smooth = 0.5f, float metal = 0f, float r = 1, float g = 1, float b = 1, float bump = 1f, float cutoff = -1f)
        {
            return new Def { a = a, n = n, ms = ms, ao = ao, smooth = smooth, metal = metal, tinta = new Color(r, g, b), bump = bump, cutoff = cutoff };
        }

        // Materiali dei modelli degli enigmi (parametri, non testi). ms = R metallo, G occlusione, A levigatezza; ao separata per le texture Poly Haven.
        static readonly Dictionary<string, Def> Materiali = new Dictionary<string, Def>
        {
            { "ME_ExVoto", D(TexE + "T_ExVoto_A.png", TexE + "T_ExVoto_N.jpg", TexE + "T_ExVoto_MS.png", null, 1f, 1f, bump: 1.2f, cutoff: 0.45f) },
            { "ME_LegnoNoce", D(TexE + "T_LegnoNoce_A.jpg", TexE + "T_LegnoNoce_N.jpg", null, null, 0.38f) },
            { "ME_LegnoChiaro", D(TexE + "T_LegnoChiaro_A.jpg", TexE + "T_LegnoChiaro_N.jpg", null, null, 0.28f) },
            { "ME_Ottone", D(TexE + "T_Ottone_A.jpg", TexE + "T_Ottone_N.jpg", TexE + "T_Ottone_MS.png", null, 1f, 1f) },
            { "ME_Damasco", D(TexE + "T_Damasco_A.jpg", TexE + "T_Damasco_N.jpg", TexE + "T_Damasco_MS.png", null, 1f, 0f) },
            { "ME_Marmo", D(TexE + "T_Marmo_A.jpg", null, null, null, 0.45f) },
            { "ME_VetroRosso", D(null, null, null, null, 0.88f, 0f, 0.32f, 0.04f, 0.03f) },
            { "ME_Cera", D(null, null, null, null, 0.3f, 0f, 0.78f, 0.70f, 0.55f) },
            { "ME_Latta", D(TexE + "T_Latta_A.jpg", TexE + "T_Latta_N.jpg", TexE + "T_Latta_MS.png", null, 1f, 1f) },
            { "ME_Calendario", D(TexE + "T_Calendario_A.jpg", TexE + "T_Calendario_N.jpg", null, null, 0.12f) },
            // decal a matita: trasparenza come il vetro (stessa variante dello shader, sicura nella build)
            { "ME_SegnoMatita", new Def { a = TexE + "T_SegnoMatita_A.png", smooth = 0.35f, tinta = Color.white, vetro = true } },
            { "MT_Panno", D(TexE + "T_Panno_A.jpg", TexE + "T_Panno_N.jpg", null, null, 0.06f) },
            { "ME_Toppe", D(TexE + "T_Toppe_A.png", TexE + "T_Toppe_N.jpg", null, null, 0.06f, cutoff: 0.5f) },
            { "ME_Coperta", D(TexE + "T_Coperta_A.jpg", TexE + "T_Coperta_N.jpg", null, null, 0.05f) },
            { "ME_Fodera", D(TexE + "T_Fodera_A.jpg", null, null, null, 0.15f) },
            { "ME_AssiIncise", D(TexE + "T_AssiIncise_A.jpg", TexE + "T_AssiIncise_N.jpg", TexE + "T_AssiIncise_MS.png", null, 1f, 0f) },
            { "ME_Foto", D(TexE + "T_Foto_A.jpg", null, null, null, 0.55f) },
            { "MT_Cuoio", D(TexE + "T_CuoioTinta_A.jpg", TexP + "Scarpe/T_Cuoio_Scarpe_Normale.png", null, null, 0.42f) },
            { "ME_Suola", D(TexP + "Scarpe/T_Cuoio_Scarpe_Albedo.png", TexP + "Scarpe/T_Cuoio_Scarpe_Normale.png", null, null, 0.25f, 0f, 0.35f, 0.3f, 0.27f) },
            { "ME_Lacci", D(TexP + "TessutoIuta/TessutoIuta_Albedo.jpg", null, null, null, 0.1f, 0f, 0.25f, 0.2f, 0.16f) },
            // texture CC0 Poly Haven gia' nel progetto (CREDITS.md)
            { "ME_LegnoBaule", D(TexP + "AssiStagionate/AssiStagionate_Albedo.jpg", TexP + "AssiStagionate/AssiStagionate_Normal.jpg", TexP + "AssiStagionate/AssiStagionate_MS.png", TexP + "AssiStagionate/AssiStagionate_AO.jpg", 0.8f, 0f, 0.55f, 0.42f, 0.33f) },
            { "ME_LegnoBanco", D(TexP + "AssiConsunte/AssiConsunte_Albedo.jpg", TexP + "AssiConsunte/AssiConsunte_Normal.jpg", TexP + "AssiConsunte/AssiConsunte_MS.png", TexP + "AssiConsunte/AssiConsunte_AO.jpg", 0.8f, 0f, 0.8f, 0.68f, 0.56f) },
            { "ME_Ferro", D(TexP + "FerroRuggine02/FerroRuggine02_Albedo.jpg", TexP + "FerroRuggine02/FerroRuggine02_Normal.jpg", TexP + "FerroRuggine02/FerroRuggine02_MS.png", TexP + "FerroRuggine02/FerroRuggine02_AO.jpg", 0.9f, 0.6f, 0.45f, 0.42f, 0.4f) },
            { "ME_CuoioCinghia", D(TexP + "Scarpe/T_Cuoio_Scarpe_Albedo.png", TexP + "Scarpe/T_Cuoio_Scarpe_Normale.png", null, null, 0.35f, 0f, 0.8f, 0.62f, 0.48f) },
            { "ME_Lino", D(TexP + "TessutoLino/TessutoLino_Albedo.jpg", TexP + "TessutoLino/TessutoLino_Normal.jpg", TexP + "TessutoLino/TessutoLino_MS.png", TexP + "TessutoLino/TessutoLino_AO.jpg", 0.6f, 0f, 0.92f, 0.86f, 0.76f) },
            { "ME_Iuta", D(TexP + "TessutoIuta/TessutoIuta_Albedo.jpg", TexP + "TessutoIuta/TessutoIuta_Normal.jpg", TexP + "TessutoIuta/TessutoIuta_MS.png", TexP + "TessutoIuta/TessutoIuta_AO.jpg", 0.6f) },
            { "MX_Vetro", new Def { smooth = 0.93f, tinta = new Color(0.62f, 0.66f, 0.62f, 0.14f), vetro = true } },
        };

        static void ImpostaTexture(string path, bool normale, bool lineare, int max)
        {
            if (!path.StartsWith(TexE)) return;   // le texture condivise del progetto restano come sono
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool cambia = false;
            var tipo = normale ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != tipo) { ti.textureType = tipo; cambia = true; }
            if (!normale && ti.sRGBTexture == lineare) { ti.sRGBTexture = !lineare; cambia = true; }
            if (ti.maxTextureSize > max) { ti.maxTextureSize = max; cambia = true; }
            if (path.EndsWith("_A.png") && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; cambia = true; }
            if (path.StartsWith(TexE) && ti.textureCompression != TextureImporterCompression.CompressedHQ) { ti.textureCompression = TextureImporterCompression.CompressedHQ; cambia = true; }
            if (cambia) ti.SaveAndReimport();
        }

        static Texture2D T(string p) { return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(p); }

        static Material CreaMateriale(string nome, Def d)
        {
            string path = Mats + "/" + nome + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            m.shaderKeywords = new string[0];
            m.SetColor("_BaseColor", d.tinta);
            m.SetTexture("_BaseMap", T(d.a));
            var n = T(d.n);
            m.SetTexture("_BumpMap", n); m.SetFloat("_BumpScale", d.bump);
            if (n != null) m.EnableKeyword("_NORMALMAP");
            var ms = T(d.ms);
            m.SetTexture("_MetallicGlossMap", ms);
            if (ms != null) m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Metallic", d.metal); m.SetFloat("_Smoothness", d.smooth); m.SetFloat("_SmoothnessTextureChannel", 0f);
            var ao = T(d.ao) ?? ms;   // nelle mappe MS proprie l'occlusione e' nel verde
            m.SetTexture("_OcclusionMap", ao); m.SetFloat("_OcclusionStrength", 1f);
            if (ao != null) m.EnableKeyword("_OCCLUSIONMAP");
            // evidenziazione degli oggetti cliccabili: emissione accesa ma nera (MaterialPropertyBlock in EsaminaView)
            m.SetColor("_EmissionColor", Color.black); m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.SetFloat("_Surface", 0f); m.SetFloat("_Blend", 0f); m.SetFloat("_ZWrite", 1f); m.SetFloat("_Cull", 2f);
            m.SetFloat("_SrcBlend", 1f); m.SetFloat("_DstBlend", 0f); m.SetFloat("_SrcBlendAlpha", 1f); m.SetFloat("_DstBlendAlpha", 0f);
            m.SetOverrideTag("RenderType", "Opaque"); m.renderQueue = -1;
            m.SetFloat("_AlphaClip", 0f);
            if (d.cutoff >= 0f)
            {
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", d.cutoff); m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);   // lamine sottili e decal: visibili da tutte e due le parti
                m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450;
            }
            if (d.vetro)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", 1f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_SrcBlendAlpha", 1f); m.SetFloat("_DstBlendAlpha", 10f);
                m.SetFloat("_BlendModePreserveSpecular", 1f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
                m.SetShaderPassEnabled("ShadowCaster", false);
                if (d.a != null) m.renderQueue = 3001;
            }
            else m.SetShaderPassEnabled("ShadowCaster", true);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Cartella(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Cartella(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [MenuItem("Treno/Enigmi/Crea prefab dei modelli")]
        public static void CreaPrefab()
        {
            Cartella(Dest); Cartella(Mats);
            // 1) texture
            foreach (var d in Materiali.Values)
            {
                if (!string.IsNullOrEmpty(d.a)) ImpostaTexture(d.a, false, false, 1024);
                if (!string.IsNullOrEmpty(d.n)) ImpostaTexture(d.n, true, true, 1024);
                if (!string.IsNullOrEmpty(d.ms)) ImpostaTexture(d.ms, false, true, 512);
                if (!string.IsNullOrEmpty(d.ao)) ImpostaTexture(d.ao, false, true, 1024);
            }
            // 2) materiali
            var mats = new Dictionary<string, Material>();
            foreach (var kv in Materiali) mats[kv.Key] = CreaMateriale(kv.Key, kv.Value);
            AssetDatabase.SaveAssets();
            // 3) modelli e prefab
            int n = 0;
            var vivi = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Fbx }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string nome = Path.GetFileNameWithoutExtension(path);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp != null)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                    imp.addCollider = false;
                    imp.importCameras = false; imp.importLights = false; imp.importAnimation = false;
                    imp.animationType = ModelImporterAnimationType.None;
                    imp.meshCompression = ModelImporterMeshCompression.Medium;
                    imp.importNormals = ModelImporterNormals.Import;
                    imp.importTangents = ModelImporterTangents.CalculateMikk;
                    imp.isReadable = false;
                    foreach (var kv in mats) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                    imp.SaveAndReimport();
                }
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (fbx == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var sm = r.sharedMaterials;
                    for (int i = 0; i < sm.Length; i++)
                    {
                        if (sm[i] == null) continue;
                        Material m;
                        if (mats.TryGetValue(sm[i].name, out m)) sm[i] = m;
                        else Debug.LogWarning("[Enigmi] " + nome + ": materiale senza definizione " + sm[i].name);
                    }
                    r.sharedMaterials = sm;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                var lg = inst.GetComponent<LODGroup>();
                if (lg != null)
                {
                    var lods = lg.GetLODs();
                    if (lods.Length == 2) { lods[0].screenRelativeTransitionHeight = 0.35f; lods[1].screenRelativeTransitionHeight = 0.02f; lg.SetLODs(lods); }
                }
                PrefabUtility.SaveAsPrefabAsset(inst, Dest + "/" + nome + ".prefab");
                Object.DestroyImmediate(inst);
                vivi.Add(nome);
                n++;
            }
            // prefab di modelli che non esistono piu'
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Dest }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!vivi.Contains(Path.GetFileNameWithoutExtension(p))) AssetDatabase.DeleteAsset(p);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Enigmi] Prefab dei modelli creati: " + n + ", materiali: " + mats.Count);
        }

        [MenuItem("Treno/Enigmi/Crea font d'epoca")]
        public static void CreaFont()
        {
            Cartella("Assets/_Game/Resources/Fonts");
            // tutti i caratteri usati nei JSON del gioco, piu' l'alfabeto di base
            var set = new HashSet<char>();
            for (char c = ' '; c <= '~'; c++) set.Add(c);
            foreach (var c in "àèéìòùÀÈÉÌÒÙ«»·’‘“”–—…°") set.Add(c);
            foreach (var f in Directory.GetFiles("Assets/_Game/Resources/Data", "*.json"))
                foreach (var c in File.ReadAllText(f)) if (!char.IsControl(c)) set.Add(c);
            string caratteri = new string(set.ToArray());
            Font("Assets/Art/Fonts/OldStandard-Bold.ttf", "Assets/_Game/Resources/Fonts/Stampa SDF.asset", caratteri);
            Font("Assets/Art/Fonts/Cedarville-Cursive.ttf", "Assets/_Game/Resources/Fonts/Mano SDF.asset", caratteri);
            AssetDatabase.SaveAssets();
        }

        static void Font(string ttf, string dest, string caratteri)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null) { Debug.LogError("[Enigmi] font mancante: " + ttf); return; }
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(dest) != null) AssetDatabase.DeleteAsset(dest);
            var fa = TMP_FontAsset.CreateFontAsset(font, 72, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fa.name = Path.GetFileNameWithoutExtension(dest);
            string mancanti;
            fa.TryAddCharacters(caratteri, out mancanti);
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fa, dest);
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            Debug.Log("[Enigmi] Font " + fa.name + ": " + fa.characterTable.Count + " caratteri" + (string.IsNullOrEmpty(mancanti) ? "" : " (mancanti: " + mancanti.Length + ")"));
        }
    }
}
