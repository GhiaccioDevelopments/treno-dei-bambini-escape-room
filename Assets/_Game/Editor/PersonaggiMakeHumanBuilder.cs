using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Amerigo MakeHuman 1.3 (CC0, scheletro "game_engine"): importa i due FBX di ArtSource/Characters/makehuman,
    /// configura Humanoid, materiali URP/Lit e prefab con lo stesso AnimatorController di v5/v6 (PersonaggiBuilder).
    /// Sono i modelli in gioco (PersonaggiBuilder.UsaAmerigoMakeHuman): bambino 1,0 m nei flashback, adulto 1,75 m nel presente;
    /// v5/v6 restano come riserva. Menu: Treno/Personaggi/Importa Amerigo MakeHuman (poi Treno/Costruisci tutte le scene).
    /// </summary>
    public static partial class PersonaggiBuilder
    {
        sealed class MhSlot
        {
            public string Slot;       // nome del materiale/mesh nell'FBX (senza "Mesh")
            public string Albedo;     // file in Textures (senza .png)
            public string Normal;     // opzionale
            public string Tipo;       // Pelle | Vestito | Capelli | Ciglia | Occhi | Denti
        }

        sealed class MhPers
        {
            public string Sorgente;   // cartella in ArtSource
            public string FbxSorgente;
            public string Nome;       // AmerigoMH_Bambino / AmerigoMH_Adulto
            public float Scala;       // globalScale (unità FBX = decimetri)
            public MhSlot[] Slots;
            public string Folder { get { return "Assets/Art/Characters/" + Nome; } }
            public string Fbx { get { return Folder + "/" + Nome + ".fbx"; } }
            public string Prefab { get { return Folder + "/" + Nome + ".prefab"; } }
            public string TexF { get { return Folder + "/Textures"; } }
            public string MatF { get { return Folder + "/Materials"; } }
        }

        // L'FBX MakeHuman è in decimetri (1 unità = 10 cm). Bambino misurato 10,08 u = 1,01 m con scala 0,1.
        // Adulto misurato 18,75 u (suola-capelli) = 1,88 m con 0,1: scala 0,0933 per ~1,75 m.
        static readonly MhPers MhBambino = new MhPers
        {
            Sorgente = "ArtSource/Characters/makehuman/Amerigo_bambino", FbxSorgente = "Amerigo_bambino.fbx",
            Nome = "AmerigoMH_Bambino", Scala = 0.1f,
            Slots = new[] {
                new MhSlot { Slot = "amerigo_bambino", Albedo = "amerigo_bambino_pelle_diffuse", Tipo = "Pelle" },
                new MhSlot { Slot = "male_worksuit01", Albedo = "amerigo_bambino_salopette_diffuse", Normal = "male_worksuit01_normal", Tipo = "Vestito" },
                new MhSlot { Slot = "short02", Albedo = "amerigo_bambino_capelli_diffuse", Tipo = "Capelli" },
                new MhSlot { Slot = "eyebrow001", Albedo = "eyebrow001", Tipo = "Ciglia" },
                new MhSlot { Slot = "eyelashes01", Albedo = "eyelashes01", Tipo = "Ciglia" },
                new MhSlot { Slot = "high-poly", Albedo = "brown_eye_cut", Tipo = "Occhi" },
                new MhSlot { Slot = "teeth_base", Albedo = "teeth", Tipo = "Denti" },
            }
        };
        static readonly MhPers MhAdulto = new MhPers
        {
            Sorgente = "ArtSource/Characters/makehuman/Amerigo_adulto", FbxSorgente = "Amerigo_adulto.fbx",
            Nome = "AmerigoMH_Adulto", Scala = 0.0933f,
            Slots = new[] {
                new MhSlot { Slot = "amerigo_adulto", Albedo = "middleage_lightskinned_male_diffuse", Tipo = "Pelle" },
                new MhSlot { Slot = "male_elegantsuit01", Albedo = "male_elegantsuit01_diffuse", Tipo = "Vestito" },
                new MhSlot { Slot = "shoes03", Albedo = "shoes03_diffuse", Tipo = "Vestito" },
                new MhSlot { Slot = "short04", Albedo = "amerigo_adulto_capelli_diffuse", Tipo = "Capelli" },
                new MhSlot { Slot = "eyebrow002", Albedo = "eyebrow002", Tipo = "Ciglia" },
                new MhSlot { Slot = "eyelashes01", Albedo = "eyelashes01", Tipo = "Ciglia" },
                new MhSlot { Slot = "high-poly", Albedo = "brown_eye_cut", Tipo = "Occhi" },
                new MhSlot { Slot = "teeth_base", Albedo = "teeth", Tipo = "Denti" },
            }
        };

        [MenuItem("Treno/Personaggi/Importa Amerigo MakeHuman")]
        public static void ImportaAmerigoMakeHuman()
        {
            bool okB = ImportaMh(MhBambino);
            bool okA = ImportaMh(MhAdulto);
            AssetDatabase.SaveAssets();
            if (okB && okA)
                Debug.Log("[Personaggi] Amerigo MakeHuman importato (bambino e adulto). Prefab: " + MhBambino.Prefab + ", " + MhAdulto.Prefab);
            else
                Debug.LogError("[Personaggi] Importazione Amerigo MakeHuman NON riuscita (bambino " + (okB ? "ok" : "ERRORE")
                    + ", adulto " + (okA ? "ok" : "ERRORE") + "): vedi gli errori sopra.");
        }

        /// <summary>Mappa esplicita ossa "game_engine" (stile Unreal) -> HumanBodyBones, indipendente dall'auto-mappatura di Unity.</summary>
        static readonly string[,] MhMappaOssa =
        {
            { "Hips", "pelvis" }, { "Spine", "spine_01" }, { "Chest", "spine_02" }, { "UpperChest", "spine_03" },
            { "Neck", "neck_01" }, { "Head", "head" },
            { "LeftShoulder", "clavicle_l" }, { "LeftUpperArm", "upperarm_l" }, { "LeftLowerArm", "lowerarm_l" }, { "LeftHand", "hand_l" },
            { "RightShoulder", "clavicle_r" }, { "RightUpperArm", "upperarm_r" }, { "RightLowerArm", "lowerarm_r" }, { "RightHand", "hand_r" },
            { "LeftUpperLeg", "thigh_l" }, { "LeftLowerLeg", "calf_l" }, { "LeftFoot", "foot_l" }, { "LeftToes", "ball_l" },
            { "RightUpperLeg", "thigh_r" }, { "RightLowerLeg", "calf_r" }, { "RightFoot", "foot_r" }, { "RightToes", "ball_r" },
        };
        static readonly string[] MhDita = { "Thumb", "Index", "Middle", "Ring", "Little" };
        static readonly string[] MhDitaOsso = { "thumb", "index", "middle", "ring", "pinky" };
        static readonly string[] MhFalangi = { "Proximal", "Intermediate", "Distal" };

        /// <summary>Costruisce la mappa Humanoid esplicita, tenendo solo le ossa presenti nello scheletro dell'FBX.</summary>
        static HumanBone[] CreaMappaOsseMh(HumanDescription desc)
        {
            var presenti = new HashSet<string>();
            foreach (var sk in desc.skeleton) presenti.Add(sk.name);
            var lista = new List<HumanBone>();
            System.Action<string, string> aggiungi = (umano, osso) =>
            {
                if (!presenti.Contains(osso)) { Debug.LogWarning("[Personaggi] Osso '" + osso + "' (" + umano + ") non presente nello scheletro."); return; }
                var hb = new HumanBone { humanName = umano, boneName = osso };
                hb.limit.useDefaultValues = true;
                lista.Add(hb);
            };
            for (int i = 0; i < MhMappaOssa.GetLength(0); i++) aggiungi(MhMappaOssa[i, 0], MhMappaOssa[i, 1]);
            foreach (var lato in new[] { "Left", "Right" })
                for (int d = 0; d < MhDita.Length; d++)
                    for (int f = 0; f < MhFalangi.Length; f++)
                    {
                        string osso = MhDitaOsso[d] + "_0" + (f + 1) + "_" + (lato == "Left" ? "l" : "r");
                        string umano = lato + " " + MhDita[d] + " " + MhFalangi[f];
                        if (presenti.Contains(osso)) aggiungi(umano, osso);
                    }
            return lista.ToArray();
        }

        static bool ImportaMh(MhPers p)
        {
            string srcFbx = p.Sorgente + "/" + p.FbxSorgente;
            if (!File.Exists(srcFbx)) { Debug.LogError("[Personaggi] Manca " + srcFbx); return false; }
            if (EPuntatoreLfs(srcFbx)) { Debug.LogError("[Personaggi] " + srcFbx + " è un puntatore Git LFS: esegui 'git lfs pull'."); return false; }
            Directory.CreateDirectory(p.TexF);
            File.Copy(srcFbx, p.Fbx, true);
            // si copiano solo le texture usate dagli slot; le altre (es. brown_eye.png, sorgente del ritaglio degli occhi) restano in ArtSource
            string srcTex = p.Sorgente + "/textures";
            if (!Directory.Exists(srcTex)) { Debug.LogError("[Personaggi] Manca la cartella delle texture " + srcTex); return false; }
            var usate = new HashSet<string>();
            foreach (var s in p.Slots)
            {
                if (s.Albedo != null) usate.Add(s.Albedo + ".png");
                if (s.Normal != null) usate.Add(s.Normal + ".png");
            }
            usate.Remove("brown_eye_cut.png"); // generata da CreaOcchiTagliati
            foreach (var nome in usate)
            {
                string f = srcTex + "/" + nome;
                if (!File.Exists(f)) { Debug.LogError("[Personaggi] Texture mancante in ArtSource: " + f); return false; }
                File.Copy(f, p.TexF + "/" + nome, true);
            }
            // pulizia: texture rimaste in Assets da importazioni precedenti e non più usate
            var daTenere = new HashSet<string>(usate) { "brown_eye_cut.png" };
            foreach (var f in Directory.GetFiles(p.TexF, "*.png"))
                if (!daTenere.Contains(Path.GetFileName(f))) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            AssetDatabase.Refresh();

            if (!CreaOcchiTagliati(p)) return false;
            ConfiguraTextureMh(p);
            var imp = AssetImporter.GetAtPath(p.Fbx) as ModelImporter;
            if (imp == null) { Debug.LogError("[Personaggi] " + p.Nome + ": nessun ModelImporter per " + p.Fbx); return false; }
            // la humanDescription (posizioni delle ossa) si rigenera solo ripassando da Generic: se la scala cambia con un
            // avatar Humanoid già creato resta alla scala vecchia (ossa x10) e Unity segnala "Avatar Rig Configuration mis-match"
            // le normali esportate da MakeHuman/Blender sono incoerenti (puntinatura nera su vestiti e pelle): si ricalcolano
            imp.importNormals = ModelImporterNormals.Calculate;
            imp.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            imp.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.globalScale = p.Scala;
            imp.SaveAndReimport();
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            // mappa Humanoid esplicita (riproducibile, non dipende dall'auto-mappatura)
            var desc = imp.humanDescription;
            desc.human = CreaMappaOsseMh(desc);
            imp.humanDescription = desc;
            imp.SaveAndReimport();

            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(p.Fbx);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError("[Personaggi] " + p.Nome + ": avatar Humanoid NON valido, prefab non salvato.");
                return false;
            }
            var mancanti = new List<string>();
            var mappate = new HashSet<string>();
            foreach (var h in avatar.humanDescription.human) mappate.Add(h.humanName);
            for (int i = 0; i < HumanTrait.BoneName.Length; i++)
                if (HumanTrait.RequiredBone(i) && !mappate.Contains(HumanTrait.BoneName[i])) mancanti.Add(HumanTrait.BoneName[i]);
            Debug.Log("[Personaggi] " + p.Nome + ": avatar valido=" + (avatar.isValid && avatar.isHuman)
                + ", ossa mappate=" + mappate.Count + ", ossa obbligatorie mancanti=" + (mancanti.Count == 0 ? "nessuna" : string.Join(",", mancanti)));
            if (mancanti.Count > 0) { Debug.LogError("[Personaggi] " + p.Nome + ": mancano ossa obbligatorie, prefab non salvato."); return false; }

            var mats = CreaMaterialiMh(p);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(p.Fbx);
            if (model == null) { Debug.LogError("[Personaggi] " + p.Nome + ": modello non caricabile da " + p.Fbx); return false; }
            // istanza in una preview scene: la scena aperta dall'utente non viene toccata
            var prev = EditorSceneManager.NewPreviewScene();
            try
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, prev);
                inst.name = p.Nome;
                var anim = inst.GetComponent<Animator>();
                if (anim == null) anim = inst.AddComponent<Animator>();
                anim.avatar = avatar;
                anim.applyRootMotion = false;
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AmerigoController);
                var tutti = new List<Renderer>();
                var dettaglio = new List<Renderer>(); // parti che spariscono nel LOD1: occhi e denti
                foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string slot = r.name.EndsWith("Mesh") ? r.name.Substring(0, r.name.Length - 4) : r.name;
                    Material m;
                    if (!mats.TryGetValue(slot, out m)) { Debug.LogWarning("[Personaggi] " + p.Nome + ": mesh '" + r.name + "' senza materiale"); continue; }
                    var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < arr.Length; i++) arr[i] = m;
                    r.sharedMaterials = arr;
                    r.updateWhenOffscreen = false;
                    bool piccolo = slot.StartsWith("teeth") || slot.StartsWith("eye") || slot == "high-poly";
                    r.shadowCastingMode = piccolo ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                    tutti.Add(r);
                    if (slot.StartsWith("teeth")) dettaglio.Add(r);
                }
                // LOD: le mesh MakeHuman hanno un solo livello di dettaglio (nessuna decimazione disponibile qui): LOD0 completo,
                // LOD1 senza i denti (7k poligoni), culling sotto il 3% dello schermo.
                var lod1 = tutti.FindAll(r => !dettaglio.Contains(r));
                var lg = inst.GetComponent<LODGroup>();
                if (lg == null) lg = inst.AddComponent<LODGroup>();
                lg.SetLODs(new[] { new LOD(0.2f, tutti.ToArray()), new LOD(0.03f, lod1.ToArray()) });
                lg.fadeMode = LODFadeMode.None;
                lg.RecalculateBounds();
                // piedi sul pavimento: l'origine dell'FBX non coincide con la suola (es. adulto con scarpe ~2,6 cm sotto lo zero).
                // La quota più bassa delle mesh (posa di riposo) diventa lo zero: l'offset sta nel prefab (il costruttore di scena lo rispetta).
                inst.transform.localPosition = Vector3.zero;
                float minY = float.MaxValue;
                foreach (var r in tutti)
                {
                    // quota reale dei vertici nella posa di riposo (r.bounds è gonfiato dalle ossa e non è affidabile)
                    var smr = r as SkinnedMeshRenderer;
                    if (smr == null) continue;
                    var baked = new Mesh();
                    smr.BakeMesh(baked, false);
                    var m2w = smr.transform.localToWorldMatrix;
                    foreach (var v in baked.vertices) minY = Mathf.Min(minY, m2w.MultiplyPoint3x4(v).y);
                    Object.DestroyImmediate(baked);
                }
                if (minY < float.MaxValue && Mathf.Abs(minY) > 0.001f && Mathf.Abs(minY) < 0.3f)
                {
                    inst.transform.localPosition = new Vector3(0f, -minY, 0f);
                    Debug.Log("[Personaggi] " + p.Nome + ": offset verticale del modello " + (-minY * 100f).ToString("0.0") + " cm (suola a quota " + (minY * 100f).ToString("0.0") + " cm)");
                }
                PrefabUtility.SaveAsPrefabAsset(inst, p.Prefab);
            }
            finally { EditorSceneManager.ClosePreviewScene(prev); }
            return true;
        }

        /// <summary>
        /// La mesh degli occhi MakeHuman ha anche la cornea, mappata sul cerchio azzurro in basso a destra di brown_eye.png
        /// (nel viewer MakeHuman è trasparente): in Unity coprirebbe l'iride. Copia della texture con quel cerchio a alpha 0
        /// (Alpha Clipping sul materiale degli occhi). L'originale resta intatto.
        /// </summary>
        static bool CreaOcchiTagliati(MhPers p)
        {
            string src = p.Sorgente + "/textures/brown_eye.png", dst = p.TexF + "/brown_eye_cut.png";
            if (!File.Exists(src)) { Debug.LogError("[Personaggi] Manca " + src); return false; }
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!t.LoadImage(File.ReadAllBytes(src))) { Object.DestroyImmediate(t); Debug.LogError("[Personaggi] " + src + " non valida."); return false; }
            int w = t.width, h = t.height;
            var px = t.GetPixels32();
            float cx = 0.935f * w, cy = 0.065f * h, r = 0.085f * w; // origine in basso nel buffer
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    var c = px[y * w + x];
                    c.a = dx * dx + dy * dy < r * r ? (byte)0 : (byte)255;
                    px[y * w + x] = c;
                }
            var o = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            o.SetPixels32(px);
            File.WriteAllBytes(dst, o.EncodeToPNG());
            Object.DestroyImmediate(t); Object.DestroyImmediate(o);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            return true;
        }

        static void ConfiguraTextureMh(MhPers p)
        {
            foreach (var s in p.Slots)
            {
                foreach (var nome in new[] { s.Albedo, s.Normal })
                {
                    if (nome == null) continue;
                    string path = p.TexF + "/" + nome + ".png";
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null) { Debug.LogWarning("[Personaggi] Texture mancante " + path); continue; }
                    bool normal = nome == s.Normal;
                    ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    ti.sRGBTexture = !normal;
                    bool alpha = s.Tipo == "Capelli" || s.Tipo == "Ciglia" || s.Tipo == "Occhi";
                    ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                    ti.alphaIsTransparency = alpha;
                    ti.mipmapEnabled = true;
                    ti.mipMapsPreserveCoverage = alpha;
                    ti.alphaTestReferenceValue = 0.5f;
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.maxTextureSize = (s.Tipo == "Pelle" || s.Tipo == "Vestito") ? 2048 : 1024;
                    ti.textureCompression = alpha ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                    ti.SaveAndReimport();
                }
            }
        }

        static Dictionary<string, Material> CreaMaterialiMh(MhPers p)
        {
            Directory.CreateDirectory(p.MatF);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var res = new Dictionary<string, Material>();
            foreach (var s in p.Slots)
            {
                string path = p.MatF + "/" + p.Nome + "_" + s.Slot + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p.TexF + "/" + s.Albedo + ".png"));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Metallic", 0f);
                float liscio = s.Tipo == "Occhi" ? 0.85f : s.Tipo == "Denti" ? 0.4f : s.Tipo == "Pelle" ? 0.25f : s.Tipo == "Vestito" ? 0.1f : 0.2f;
                m.SetFloat("_Smoothness", liscio);
                if (s.Normal != null)
                {
                    m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p.TexF + "/" + s.Normal + ".png"));
                    m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
                }
                else
                {
                    m.SetTexture("_BumpMap", null);
                    m.DisableKeyword("_NORMALMAP");
                }
                if (s.Tipo == "Capelli" || s.Tipo == "Ciglia" || s.Tipo == "Occhi")
                {
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", s.Tipo == "Occhi" ? 2f : 0f); m.SetFloat("_Surface", 0f);
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    m.SetFloat("_AlphaClip", 0f); m.DisableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 2f); m.SetFloat("_Surface", 0f);
                    m.renderQueue = -1;
                    m.SetOverrideTag("RenderType", "Opaque");
                }
                EditorUtility.SetDirty(m);
                res[s.Slot] = m;
            }
            AssetDatabase.SaveAssets();
            return res;
        }
    }
}
