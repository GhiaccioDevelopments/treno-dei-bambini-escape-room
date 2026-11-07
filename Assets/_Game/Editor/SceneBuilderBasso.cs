using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 3: il basso di Antonietta (20_Int_BassoAntonietta) come abitazione povera e sovraffollata:
    /// stanza principale, alcova col letto dietro una tenda, angolo cucina e cortiletto sul retro (circa 3,6 volte la superficie di prima).
    /// Pareti con InteriorWall, arredo con mesh del kit e modelli FBX gia' presenti; spazio libero risolto da RisolviSpazio.
    /// Menu per l'iterazione: Treno > Sviluppo > Costruisci solo basso e flashback.
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiBassoPath = "Docs/richieste/media/R006/punti_segreti_basso.md";

        [MenuItem("Treno/Sviluppo/Costruisci solo basso e flashback")]
        public static void BuildBassoFlashbackSolo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ArtGenerator.EnsureMaterials();
            matCache.Clear();
            ArtGenerator.EnsureFolder(ScenesFolder);
            ArtGenerator.EnsureFolder("Assets/_Game/Generated");
            ArtGenerator.EnsureFolder(KitBuilder.Folder);
            facadeCache.Clear();
            kitMeshes = 0; kitTris = 0; propCount = 0;
            EnsureLightMaterials();
            BuildBasso();
            BuildFlashback();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LookBuilder.ApplyLookToScene("20_Int_BassoAntonietta");
            LookBuilder.ApplyLookToScene("30_FB_Scarpe");
            Debug.Log("[SceneBuilder] Basso e flashback: " + kitMeshes + " mesh generate (" + kitTris + " triangoli), " + propCount + " pezzi.");
        }

        // ------------------------------------------------------------------ pezzi generati del basso

        static GameObject Pz(Transform parent, string name, string key, string[] mats, Action<KitBuilder> fill, Vector3 pos, float yaw, bool collider = true)
        {
            return Pezzo(parent, name, key, mats, fill, pos, yaw, 0.03f, collider);
        }

        static readonly string[] MatSedia = { "M_LegnoVernicePelata", "Paglia" };

        static GameObject Sedia(Transform parent, Vector3 pos, float yaw)
        {
            return Pz(parent, "SediaPaglia", "SediaPaglia", MatSedia, SediaPaglia, pos, yaw);
        }

        static void LettoFerro(KitBuilder kb)
        {
            // testata a +z; sub 0 ferro, 1 materasso, 2 coperta
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.7f : 0.66f;
                kb.Box(new Vector3(x, 0f, 0.94f), new Vector3(x + 0.04f, 1.12f, 0.98f), 0, 0.3f, KitBuilder.NY);
                kb.Box(new Vector3(x, 0f, -0.98f), new Vector3(x + 0.04f, 0.72f, -0.94f), 0, 0.3f, KitBuilder.NY);
                kb.Box(new Vector3(x, 0.3f, -0.94f), new Vector3(x + 0.04f, 0.34f, 0.94f), 0, 0.3f);
            }
            kb.Box(new Vector3(-0.7f, 1.0f, 0.95f), new Vector3(0.7f, 1.04f, 0.98f), 0, 0.3f);
            kb.Box(new Vector3(-0.7f, 0.5f, 0.95f), new Vector3(0.7f, 0.54f, 0.98f), 0, 0.3f);
            for (float x = -0.6f; x < 0.65f; x += 0.15f) kb.Box(new Vector3(x, 0.54f, 0.955f), new Vector3(x + 0.02f, 1.0f, 0.975f), 0, 0.3f, KitBuilder.NY | KitBuilder.PY);
            kb.Box(new Vector3(-0.7f, 0.5f, -0.98f), new Vector3(0.7f, 0.54f, -0.94f), 0, 0.3f);
            kb.Box(new Vector3(-0.66f, 0.34f, -0.93f), new Vector3(0.66f, 0.5f, 0.93f), 1, 1f, KitBuilder.NY);
            kb.Box(new Vector3(-0.67f, 0.5f, -0.9f), new Vector3(0.67f, 0.55f, 0.3f), 2, 1f);
            kb.Box(new Vector3(-0.32f, 0.5f, 0.55f), new Vector3(0.32f, 0.62f, 0.9f), 1, 0.6f);
        }

        static void MaterassoTerra(KitBuilder kb)
        {
            kb.Box(new Vector3(-0.4f, 0f, -0.9f), new Vector3(0.4f, 0.12f, 0.9f), 0, 1f, KitBuilder.NY);
            kb.Box(new Vector3(-0.41f, 0.12f, -0.9f), new Vector3(0.41f, 0.16f, 0.2f), 1, 1f);
            kb.Box(new Vector3(-0.25f, 0.12f, 0.45f), new Vector3(0.25f, 0.22f, 0.85f), 0, 0.6f);
        }

        static void PilaMaterassi(KitBuilder kb)
        {
            // due materassi arrotolati e legati, appoggiati al muro (dorso a -z)
            kb.Box(new Vector3(-0.5f, 0f, -0.14f), new Vector3(0.0f, 1.5f, 0.14f), 0, 0.8f, KitBuilder.NY);
            kb.Box(new Vector3(0.02f, 0f, -0.14f), new Vector3(0.5f, 1.35f, 0.14f), 1, 0.8f, KitBuilder.NY);
            foreach (var y in new[] { 0.3f, 0.75f, 1.2f })
                kb.Box(new Vector3(-0.51f, y, -0.15f), new Vector3(0.51f, y + 0.03f, 0.15f), 2, 0.3f, KitBuilder.NY | KitBuilder.PY);
        }

        static void Credenza(KitBuilder kb)
        {
            // fronte a +z; sub 0 legno, 1 ferro, 2 piatti
            kb.Box(new Vector3(-0.6f, 0f, -0.25f), new Vector3(0.6f, 0.92f, 0.25f), 0, 1f, KitBuilder.NY);
            kb.Box(new Vector3(-0.62f, 0.92f, -0.27f), new Vector3(0.62f, 0.96f, 0.28f), 0, 1f);
            kb.Box(new Vector3(-0.54f, 0.08f, 0.25f), new Vector3(-0.02f, 0.86f, 0.28f), 0, 1f);
            kb.Box(new Vector3(0.02f, 0.08f, 0.25f), new Vector3(0.54f, 0.86f, 0.28f), 0, 1f);
            kb.Box(new Vector3(-0.07f, 0.42f, 0.28f), new Vector3(-0.04f, 0.56f, 0.3f), 1, 0.2f);
            kb.Box(new Vector3(0.04f, 0.42f, 0.28f), new Vector3(0.07f, 0.56f, 0.3f), 1, 0.2f);
            kb.Box(new Vector3(-0.58f, 0.96f, -0.25f), new Vector3(-0.54f, 1.85f, 0.02f), 0, 1f);
            kb.Box(new Vector3(0.54f, 0.96f, -0.25f), new Vector3(0.58f, 1.85f, 0.02f), 0, 1f);
            kb.Box(new Vector3(-0.6f, 1.85f, -0.26f), new Vector3(0.6f, 1.9f, 0.04f), 0, 1f);
            kb.Box(new Vector3(-0.54f, 0.96f, -0.25f), new Vector3(0.54f, 1.85f, -0.22f), 0, 1f);
            kb.Box(new Vector3(-0.54f, 1.38f, -0.22f), new Vector3(0.54f, 1.41f, 0.0f), 0, 1f);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.45f + i * 0.28f;
                kb.Box(new Vector3(x, 1.41f, -0.2f), new Vector3(x + 0.2f, 1.7f, -0.17f), 2, 0.3f);
                kb.Box(new Vector3(x + 0.02f, 0.96f, -0.18f), new Vector3(x + 0.18f, 1.2f, -0.15f), 2, 0.3f);
            }
        }

        static void Fornello(KitBuilder kb)
        {
            // fronte a +z; sub 0 ferro, 1 brace
            kb.Box(new Vector3(-0.3f, 0f, -0.25f), new Vector3(0.3f, 0.8f, 0.25f), 0, 0.5f, KitBuilder.NY);
            kb.Box(new Vector3(-0.32f, 0.8f, -0.27f), new Vector3(0.32f, 0.84f, 0.27f), 0, 0.5f);
            kb.Box(new Vector3(-0.2f, 0.84f, -0.15f), new Vector3(0.2f, 0.86f, 0.15f), 1, 1f);
            kb.Box(new Vector3(-0.09f, 0.33f, 0.25f), new Vector3(0.09f, 0.45f, 0.265f), 1, 1f);
            kb.Box(new Vector3(-0.14f, 0.28f, 0.25f), new Vector3(0.14f, 0.5f, 0.255f), 0, 0.5f);
            kb.Tube(new Vector3(0f, 0.84f, -0.18f), new Vector3(0f, 2.9f, -0.18f), 0.055f, 6, 0, 0.5f);
        }

        static void BancoCalzolaio(KitBuilder kb)
        {
            // fronte (lato del calzolaio) a -z; sub 0 assi, 1 ferro, 2 legno, 3 pelli, 4 cuoio
            kb.Box(new Vector3(-0.8f, 0.82f, -0.35f), new Vector3(0.8f, 0.88f, 0.35f), 0, TileWood);
            foreach (var cx in new[] { -0.78f, 0.7f })
                foreach (var cz in new[] { -0.33f, 0.27f })
                    kb.Box(new Vector3(cx, 0f, cz), new Vector3(cx + 0.08f, 0.82f, cz + 0.08f), 2, TileWood, KitBuilder.NY);
            kb.Box(new Vector3(-0.78f, 0.28f, -0.33f), new Vector3(0.78f, 0.32f, 0.33f), 0, TileWood);
            kb.Box(new Vector3(-0.5f, 0.32f, -0.2f), new Vector3(0.1f, 0.42f, 0.2f), 3, 0.5f);
            kb.Box(new Vector3(0.2f, 0.32f, -0.2f), new Vector3(0.6f, 0.38f, 0.2f), 4, 0.5f);
            // utensili: martello, tenaglia, cuoio, pentolino di colla
            kb.Box(new Vector3(-0.6f, 0.88f, -0.1f), new Vector3(-0.3f, 0.9f, -0.06f), 2, 0.3f);
            kb.Box(new Vector3(-0.62f, 0.88f, -0.15f), new Vector3(-0.55f, 0.95f, -0.01f), 1, 0.3f);
            kb.Box(new Vector3(-0.1f, 0.88f, -0.25f), new Vector3(0.2f, 0.89f, 0.0f), 3, 0.3f);
            kb.Box(new Vector3(0.4f, 0.88f, 0.05f), new Vector3(0.52f, 0.96f, 0.17f), 1, 0.3f);
            // pannello con attrezzi appesi sul retro
            kb.Box(new Vector3(-0.75f, 1.3f, 0.3f), new Vector3(0.75f, 2.0f, 0.33f), 2, TileWood);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.6f + i * 0.28f, drop = (i % 2) * 0.12f;
                kb.Box(new Vector3(x, 1.45f, 0.27f), new Vector3(x + 0.03f, 1.85f - drop, 0.3f), 1, 0.3f);
                kb.Box(new Vector3(x - 0.04f, 1.85f - drop, 0.27f), new Vector3(x + 0.07f, 1.92f - drop, 0.3f), 2, 0.3f);
            }
        }

        static void ScaffaleForme(KitBuilder kb)
        {
            // scaffale con le forme di legno (fronte a +z); sub 0 legno scuro, 1 legno chiaro
            const float t = 0.04f;
            kb.Box(new Vector3(-0.7f, 0f, -0.18f), new Vector3(-0.66f, 1.8f, 0.18f), 0, TileWood);
            kb.Box(new Vector3(0.66f, 0f, -0.18f), new Vector3(0.7f, 1.8f, 0.18f), 0, TileWood);
            kb.Box(new Vector3(-0.7f, 0f, -0.2f), new Vector3(0.7f, 1.8f, -0.17f), 0, TileWood);
            foreach (var y in new[] { 0.1f, 0.7f, 1.3f, 1.78f })
                kb.Box(new Vector3(-0.7f, y, -0.2f), new Vector3(0.7f, y + t, 0.18f), 0, TileWood);
            foreach (var y in new[] { 0.14f, 0.74f, 1.34f })
                for (int i = 0; i < 7; i++)
                {
                    float x = -0.6f + i * 0.18f;
                    kb.Box(new Vector3(x, y, -0.08f), new Vector3(x + 0.1f, y + 0.1f, 0.14f), 1, 0.3f);
                    kb.Box(new Vector3(x + 0.01f, y + 0.1f, 0.0f), new Vector3(x + 0.09f, y + 0.2f, 0.06f), 1, 0.3f);
                }
        }

        static void Manichino(KitBuilder kb)
        {
            // sub 0 stoffa, 1 legno
            kb.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 1.0f, 0f), 0.025f, 5, 1, 0.3f);
            kb.Box(new Vector3(-0.25f, 0f, -0.03f), new Vector3(0.25f, 0.04f, 0.03f), 1, 0.3f);
            kb.Box(new Vector3(-0.03f, 0f, -0.25f), new Vector3(0.03f, 0.04f, 0.25f), 1, 0.3f);
            kb.Box(new Vector3(-0.17f, 1.0f, -0.1f), new Vector3(0.17f, 1.5f, 0.1f), 0, 0.4f);
            kb.Box(new Vector3(-0.2f, 1.42f, -0.09f), new Vector3(0.2f, 1.5f, 0.09f), 0, 0.4f);
            kb.Box(new Vector3(-0.04f, 1.5f, -0.04f), new Vector3(0.04f, 1.6f, 0.04f), 1, 0.3f);
        }

        static void RotoliStoffa(KitBuilder kb)
        {
            // pila di rotoli (assi lungo x); sub 0..3 stoffe
            for (int i = 0; i < 3; i++) kb.Tube(new Vector3(-0.4f, 0.075f, -0.16f + i * 0.16f), new Vector3(0.4f, 0.075f, -0.16f + i * 0.16f), 0.075f, 8, i, 0.3f);
            kb.Tube(new Vector3(-0.4f, 0.2f, -0.08f), new Vector3(0.4f, 0.2f, -0.08f), 0.075f, 8, 3, 0.3f);
            kb.Tube(new Vector3(-0.4f, 0.2f, 0.08f), new Vector3(0.4f, 0.2f, 0.08f), 0.075f, 8, 1, 0.3f);
        }

        static void CassaFrutta(KitBuilder kb, int tipo)
        {
            // sub 0 legno, 1 foglia (verdura), 2 ocra (arance), 3 rosso (pomodori)
            kb.Box(new Vector3(-0.3f, 0f, -0.2f), new Vector3(0.3f, 0.22f, 0.2f), 0, TileWood, KitBuilder.NY | KitBuilder.PY);
            kb.Box(new Vector3(-0.28f, 0.2f, -0.18f), new Vector3(0.28f, 0.22f, 0.18f), 0, TileWood);
            var rnd = new System.Random(900 + tipo);
            for (int ix = 0; ix < 5; ix++)
                for (int iz = 0; iz < 3; iz++)
                {
                    float x = -0.25f + ix * 0.11f, z = -0.13f + iz * 0.13f, h = 0.08f + (float)rnd.NextDouble() * 0.05f;
                    kb.Box(new Vector3(x - 0.045f, 0.22f, z - 0.05f), new Vector3(x + 0.045f, 0.22f + h, z + 0.05f), 1 + tipo, 0.2f, KitBuilder.NY);
                }
        }

        static void PoltronaBarbiere(KitBuilder kb)
        {
            // fronte a +z; sub 0 ferro, 1 cuoio
            kb.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.4f, 0f), 0.06f, 6, 0, 0.3f);
            kb.Box(new Vector3(-0.25f, 0f, -0.25f), new Vector3(0.25f, 0.05f, 0.25f), 0, 0.5f);
            kb.Box(new Vector3(-0.28f, 0.4f, -0.28f), new Vector3(0.28f, 0.5f, 0.3f), 1, 0.5f);
            kb.Box(new Vector3(-0.28f, 0.5f, -0.3f), new Vector3(0.28f, 1.15f, -0.22f), 1, 0.5f);
            kb.Box(new Vector3(-0.15f, 1.15f, -0.3f), new Vector3(0.15f, 1.35f, -0.22f), 1, 0.5f);
            kb.Box(new Vector3(-0.34f, 0.55f, -0.28f), new Vector3(-0.28f, 0.7f, 0.25f), 1, 0.5f);
            kb.Box(new Vector3(0.28f, 0.55f, -0.28f), new Vector3(0.34f, 0.7f, 0.25f), 1, 0.5f);
            kb.Box(new Vector3(-0.2f, 0.05f, 0.25f), new Vector3(0.2f, 0.12f, 0.45f), 0, 0.5f);
        }

        static void Radio(KitBuilder kb)
        {
            // sub 0 legno, 1 stoffa, 2 ottone
            kb.Box(new Vector3(-0.2f, 0f, -0.1f), new Vector3(0.2f, 0.28f, 0.1f), 0, 0.5f);
            kb.Box(new Vector3(-0.15f, 0.04f, 0.1f), new Vector3(0.0f, 0.24f, 0.11f), 1, 0.3f);
            kb.Box(new Vector3(0.03f, 0.14f, 0.1f), new Vector3(0.17f, 0.24f, 0.11f), 2, 0.3f);
            kb.Box(new Vector3(0.05f, 0.05f, 0.1f), new Vector3(0.07f, 0.09f, 0.12f), 2, 0.3f);
            kb.Box(new Vector3(0.12f, 0.05f, 0.1f), new Vector3(0.14f, 0.09f, 0.12f), 2, 0.3f);
        }

        static void SvegliaRotta(KitBuilder kb)
        {
            // sveglia a campanelli ammaccata, ferma, con un piede rotto; sub 0 metallo, 1 quadrante chiaro
            kb.Box(new Vector3(-0.07f, 0f, -0.035f), new Vector3(0.07f, 0.14f, 0.035f), 0, 0.5f);          // cassa
            kb.Box(new Vector3(-0.055f, 0.03f, 0.035f), new Vector3(0.055f, 0.125f, 0.04f), 1, 0.3f);       // quadrante
            kb.Box(new Vector3(-0.003f, 0.075f, 0.04f), new Vector3(0.003f, 0.115f, 0.043f), 0, 0.2f);      // lancetta fissa
            kb.Box(new Vector3(-0.04f, 0.075f, 0.04f), new Vector3(-0.003f, 0.081f, 0.043f), 0, 0.2f);
            kb.Box(new Vector3(-0.065f, 0.14f, -0.02f), new Vector3(-0.015f, 0.165f, 0.02f), 0, 0.3f);      // campanello sinistro
            kb.Box(new Vector3(0.015f, 0.14f, -0.02f), new Vector3(0.065f, 0.158f, 0.02f), 0, 0.3f);        // campanello destro (ammaccato)
            kb.Box(new Vector3(-0.055f, -0.01f, -0.025f), new Vector3(-0.035f, 0f, 0.025f), 0, 0.3f);       // piede sinistro
        }

        // ------------------------------------------------------------------ santini e ricordi sul muro

        static void PreparaSantini()
        {
            PreparaTestiAmbiente("basso"); // santini e calendario: testi in testi_ambiente.json
            MaterialeDisegnoBambino("Ricordo", 160, 120, 25); // il biglietto sul muro e' un disegno di bambino, senza scritte
        }

        static GameObject Appeso(Transform p, string nome, Vector3 pos, Vector3 size, string mat, float yaw = 0f)
        {
            return Box(nome, p, pos, size, mat, new Vector3(0f, yaw, 0f), false);
        }

        /// <summary>Cornice di legno + santino sul muro; n = normale verso la stanza, pos = punto sul piano del muro.</summary>
        static void Santino(Transform p, string key, Vector3 pos, Vector3 n, float w = 0.26f, float h = 0.38f)
        {
            Vector3 euler = Quaternion.LookRotation(n, Vector3.up).eulerAngles;
            Box("Cartello_Cornice" + key, p, pos + n * 0.012f, new Vector3(w + 0.05f, h + 0.05f, 0.024f), "M_LegnoVernicePelata", euler, false);
            Manifesto(p, key, pos + n * 0.014f, n, w, h, "Manifesto_" + key);
        }

        // ------------------------------------------------------------------ il basso

        static GameObject Candela(Transform p, string nome, Vector3 pos, GameObject sostegno)
        {
            var c = Model("Candela", p, pos, 0f, 1.0f, false, "Candela" + nome);
            var f = Prim(PrimitiveType.Sphere, "FiammaCandela" + nome, p, pos + Vector3.up * 0.14f, new Vector3(0.04f, 0.07f, 0.04f), "Glow", null, false);
            if (sostegno != null) { Impila(c, sostegno, new Vector2(pos.x - sostegno.transform.position.x, pos.z - sostegno.transform.position.z)); f.transform.position = c.transform.position + Vector3.up * 0.14f; Cavaliere(f, sostegno); }
            return c;
        }

        static void AngoloBasso(Transform env, string nome, float x0, float x1, float z0, float z1, float h)
        {
            var kb = new KitBuilder("M_IntonacoMattoni");
            kb.Box(new Vector3(x0, 0f, z0), new Vector3(x1, h, z1), 0, TileWall, KitBuilder.NY);
            MeshObject(env, nome, kb.Build(KitName("Angolo")), new[] { "M_IntonacoMattoni" }, true);
        }

        static void BuildBasso()
        {
            var scene = NewScene();
            kitScene = "Basso"; kitIndex = 0; pre = "Bas";
            var r = new System.Random(1947);
            doorWallPoints.Clear();
            ResetSpazio();
            segreti.Clear();
            riservati.Clear();
            quotaTerreno = null;
            Ambient(new Color(0.62f, 0.52f, 0.40f), new Color(0.42f, 0.34f, 0.26f), new Color(0.24f, 0.19f, 0.14f));
            Sun(new Color(1f, 0.88f, 0.68f), 0.6f, new Vector3(55f, -25f, 0f));
            PreparaSantini();
            EnsureWaterMaterial();

            var env = new GameObject("Ambiente").transform;
            var props = new GameObject("Arredo").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            // pianta (x est, z nord):
            //  stanza principale x -4,5..4,5 z -2,5..3,5 | alcova x -4,5..-1,5 z 3,8..7,8 | cucina x 1,5..4,5 z 3,8..7,8
            //  blocco pieno (ripostiglio nascosto, R011) x -1,2..1,2 z 3,8..8,1 | cortile x 1,2..6,3 z 8,1..14,1 (pareti da 4,2 m)
            Ground("Pavimento", env, new Vector3(-4.8f, -0.3f, -2.8f), new Vector3(4.8f, 0f, 8.1f), "M_PavimentoCotto", 1.2f);
            Ground("PavimentoCortile", env, new Vector3(0.9f, -0.3f, 8.1f), new Vector3(6.6f, 0f, 14.4f), "M_PavimentoBattuto", 1.6f);
            const float H = 3.0f, HC = 4.2f, TH = 0.3f;
            const string PL = "M_IntonacoMattoni", DADO = "M_IntonacoRossoSbiadito", TRIM = "M_ArenariaCrepe";
            Func<List<WallOpening>> nessuna = () => new List<WallOpening>();

            // stanza principale
            InteriorWall(env, "MuroSud", new WallFrame(new Vector3(4.5f, 0f, -2.5f), Vector3.left, Vector3.forward), 9f, H, TH,
                new List<WallOpening> { new WallOpening(4.5f, 1.3f, 0f, 2.2f, true) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroOvest", new WallFrame(new Vector3(-4.5f, 0f, -2.5f), Vector3.forward, Vector3.right), 6f, H, TH,
                new List<WallOpening> { new WallOpening(3.0f, 0.9f, 1.1f, 2.1f) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroEst", new WallFrame(new Vector3(4.5f, 0f, 3.5f), Vector3.back, Vector3.left), 6f, H, TH,
                new List<WallOpening> { new WallOpening(2.5f, 0.9f, 1.1f, 2.1f) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroNord", new WallFrame(new Vector3(-4.5f, 0f, 3.5f), Vector3.right, Vector3.back), 9f, H, TH,
                new List<WallOpening> { new WallOpening(1.5f, 2.0f, 0f, 2.3f, true), new WallOpening(7.5f, 2.0f, 0f, 2.3f, true) }, PL, DADO, 1.0f, TRIM);
            // alcova
            InteriorWall(env, "MuroOvestAlcova", new WallFrame(new Vector3(-4.5f, 0f, 3.8f), Vector3.forward, Vector3.right), 4f, H, TH,
                new List<WallOpening> { new WallOpening(2.0f, 0.8f, 1.2f, 2.0f) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroNordAlcova", new WallFrame(new Vector3(-4.5f, 0f, 7.8f), Vector3.right, Vector3.back), 3f, H, TH, nessuna(), PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroEstAlcova", new WallFrame(new Vector3(-1.5f, 0f, 7.8f), Vector3.back, Vector3.left), 4f, H, TH, nessuna(), PL, DADO, 1.0f, TRIM);
            // cucina
            InteriorWall(env, "MuroOvestCucina", new WallFrame(new Vector3(1.5f, 0f, 3.8f), Vector3.forward, Vector3.right), 4f, H, TH, nessuna(), PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroEstCucina", new WallFrame(new Vector3(4.5f, 0f, 7.8f), Vector3.back, Vector3.left), 4f, H, TH,
                new List<WallOpening> { new WallOpening(2.0f, 0.8f, 1.3f, 2.1f) }, PL, DADO, 1.0f, TRIM);
            // cortile (e retro della cucina)
            InteriorWall(env, "MuroSudCortile", new WallFrame(new Vector3(6.3f, 0f, 8.1f), Vector3.left, Vector3.forward), 5.1f, HC, TH,
                new List<WallOpening> { new WallOpening(3.3f, 1.1f, 0f, 2.2f, true) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroOvestCortile", new WallFrame(new Vector3(1.2f, 0f, 8.1f), Vector3.forward, Vector3.right), 6f, HC, TH, nessuna(), PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroNordCortile", new WallFrame(new Vector3(1.2f, 0f, 14.1f), Vector3.right, Vector3.back), 5.1f, HC, TH,
                new List<WallOpening> { new WallOpening(2.5f, 1.1f, 0f, 2.2f, true) }, PL, DADO, 1.0f, TRIM);
            InteriorWall(env, "MuroEstCortile", new WallFrame(new Vector3(6.3f, 0f, 14.1f), Vector3.back, Vector3.left), 6f, HC, TH, nessuna(), PL, DADO, 1.0f, TRIM);
            // blocco pieno fra alcova e cucina
            AngoloBasso(env, "MuroBloccoRipostiglio", -1.2f, 1.2f, 3.8f, 8.1f, H);
            // spigoli esterni
            AngoloBasso(env, "MuroAngolo_SO", -4.8f, -4.5f, -2.8f, -2.5f, H);
            AngoloBasso(env, "MuroAngolo_SE", 4.5f, 4.8f, -2.8f, -2.5f, H);
            AngoloBasso(env, "MuroAngolo_OMezzo", -4.8f, -4.5f, 3.5f, 3.8f, H);
            AngoloBasso(env, "MuroAngolo_EMezzo", 4.5f, 4.8f, 3.5f, 3.8f, H);
            AngoloBasso(env, "MuroAngolo_NOAlcova", -4.8f, -4.5f, 7.8f, 8.1f, H);
            AngoloBasso(env, "MuroAngolo_NEAlcova", -1.5f, -1.2f, 7.8f, 8.1f, H);
            AngoloBasso(env, "MuroAngolo_NOCortile", 0.9f, 1.2f, 14.1f, 14.4f, HC);
            AngoloBasso(env, "MuroAngolo_NECortile", 6.3f, 6.6f, 14.1f, 14.4f, HC);
            AngoloBasso(env, "MuroAngolo_SECortile", 6.3f, 6.6f, 7.8f, 8.1f, HC);

            // ---------------------------------------------------------------- stanza principale
            var tavolo = Model("TavoloLegno01", props, new Vector3(2.3f, 0f, 1.2f), 0f, 1.2f, true, "Tavolo");
            Sedia(props, new Vector3(0.95f, 0f, 1.2f), 90f);
            Sedia(props, new Vector3(3.65f, 0f, 1.2f), -90f);
            Sedia(props, new Vector3(2.3f, 0f, 0.35f), 0f);
            Sedia(props, new Vector3(2.3f, 0f, 2.05f), 180f);
            var lampTavolo = Model("LampadaPetrolio_A", props, new Vector3(2.25f, 0.66f, 1.25f), 0f, 1.2f, false, "LampadaTavolo");
            Impila(lampTavolo, tavolo, new Vector2(-0.05f, 0.05f));
            var fiammaTavolo = Prim(PrimitiveType.Sphere, "FiammaLampada", props, lampTavolo.transform.position + Vector3.up * 0.18f, new Vector3(0.06f, 0.1f, 0.06f), "Glow", null, false);
            Cavaliere(fiammaTavolo, tavolo);
            var cesto = Model("CestaVimini", props, new Vector3(2.7f, 0.66f, 1.0f), 30f, 1.2f, false, "Cesto");
            Impila(cesto, tavolo, new Vector2(0.4f, -0.2f));

            // braciere e secchio del carbone
            var br = Empty("Braciere", props, new Vector3(-1.2f, 0f, 0.6f)).transform;
            Cyl("CiotolaBraciere", br, new Vector3(0f, 0.4f, 0f), 0.55f, 0.14f, "M_FerroRuggine02");
            Cyl("PiedeBraciere", br, new Vector3(0f, 0.2f, 0f), 0.1f, 0.4f, "M_FerroRuggine02", null, false);
            Cyl("BaseBraciere", br, new Vector3(0f, 0.03f, 0f), 0.4f, 0.06f, "M_FerroRuggine02", null, false);
            Cyl("Brace", br, new Vector3(0f, 0.46f, 0f), 0.45f, 0.03f, "Glow", null, false);
            Model("SecchioMetallo", props, new Vector3(-0.55f, 0f, 0.0f), 10f, 1.1f, true, "SecchioCarbone");
            Sedia(props, new Vector3(-2.0f, 0f, 0.7f), 80f);
            Sedia(props, new Vector3(-1.2f, 0f, -0.4f), 10f);

            // cassapanca con vaso, botte e casse, credenza, materassi arrotolati
            var cassapanca = Model("CassaLegno", props, new Vector3(4.15f, 0f, -0.6f), 90f, 1.45f, true, "Cassapanca");
            var vaso = Model("VasoTerracotta", props, new Vector3(4.15f, 0.52f, -0.4f), 0f, 1.0f, false, "Vaso");
            Impila(vaso, cassapanca, new Vector2(0f, 0.2f));
            Model("BotteVino", props, new Vector3(3.95f, 0f, -2.0f), 0f, 1.2f, true, "Botte");
            var cassaB = Model("CassaLegno", props, new Vector3(-4.1f, 0f, -0.6f), 90f, 1.2f, true, "Cassa");
            var cassaT = Model("CassaLegno", props, new Vector3(-4.1f, 0.43f, -0.6f), 78f, 1.0f, true, "Cassa");
            Impila(cassaT, cassaB, Vector2.zero);
            Pz(props, "PilaMaterassi", "B_PilaMaterassi", new[] { "M_TessutoLino", "PannoBeige", "PannoTerra" }, PilaMaterassi, new Vector3(-4.2f, 0f, -1.9f), 90f);
            var credenza = Pz(props, "Credenza", "B_Credenza", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02", "Cera" }, Credenza, new Vector3(-4.2f, 0f, 1.5f), 90f);
            var lampCredenza = Model("LampadaPetrolio_B", props, new Vector3(-4.2f, 0.96f, 1.2f), 0f, 1.3f, false, "LampadaCredenza");
            Impila(lampCredenza, credenza, new Vector2(0f, -0.3f));
            var fiammaCredenza = Prim(PrimitiveType.Sphere, "FiammaLampada", props, lampCredenza.transform.position + Vector3.up * 0.2f, new Vector3(0.06f, 0.1f, 0.06f), "Glow", null, false);
            Cavaliere(fiammaCredenza, credenza);
            var sveglia = Pz(props, "SvegliaRotta", "B_SvegliaRotta", new[] { "M_FerroRuggine02", "Cera" }, SvegliaRotta, new Vector3(0f, 0.96f, 0f), 90f, false);
            Impila(sveglia, credenza, new Vector2(0f, 0.2f));
            Cavaliere(sveglia, credenza);
            Sacco(props, new Vector3(3.9f, 0f, 2.9f), 20f);

            // altarino con lumino (muro nord)
            {
                var fr = new WallFrame(new Vector3(0.2f, 0f, 3.5f), Vector3.right, Vector3.back);
                var kb = new KitBuilder("M_ArenariaCrepe", "M_LegnoDipintoBlu", "M_LegnoVernicePelata");
                fr.Box(kb, -0.5f, -0.38f, -0.08f, 0f, 1.1f, 2.2f, 0, TileStone, 0);
                fr.Box(kb, 0.38f, 0.5f, -0.08f, 0f, 1.1f, 2.2f, 0, TileStone, 0);
                fr.Box(kb, -0.56f, 0.56f, -0.1f, 0f, 2.2f, 2.32f, 0, TileStone, 0);
                fr.Box(kb, -0.38f, 0.38f, -0.03f, -0.01f, 1.1f, 2.2f, 1, 1f, 0);
                fr.Box(kb, -0.55f, 0.55f, -0.3f, 0f, 1.0f, 1.07f, 2, TileWood, 0);
                fr.Box(kb, -0.4f, -0.34f, -0.26f, -0.04f, 0.4f, 1.0f, 2, TileWood, 0);
                fr.Box(kb, 0.34f, 0.4f, -0.26f, -0.04f, 0.4f, 1.0f, 2, TileWood, 0);
                MeshObject(props, "Altarino", kb.Build(KitName("Altarino")), kb.matNames, false);
                Prim(PrimitiveType.Capsule, "StatuettaAltarino", props, fr.W(-0.05f, -0.1f, 1.38f), new Vector3(0.1f, 0.2f, 0.1f), "Cera", null, false);
                Model("Candela", props, fr.W(0.22f, -0.15f, 1.07f), 0f, 1.0f, false, "CeroAltarino");
                Prim(PrimitiveType.Sphere, "LuminoAltarino", props, fr.W(0.22f, -0.15f, 1.2f), new Vector3(0.05f, 0.08f, 0.05f), "Glow", null, false);
            }

            // santini, calendario e crocifisso sui muri
            Santino(props, "SantinoGennaro", new Vector3(-1.8f, 1.75f, -2.5f), Vector3.forward);
            Santino(props, "SantinoMadonna", new Vector3(-2.4f, 1.7f, -2.5f), Vector3.forward);
            Santino(props, "Calendario", new Vector3(-4.5f, 1.65f, 2.5f), Vector3.right);
            Santino(props, "SantinoCuore", new Vector3(4.5f, 1.8f, 1.0f), Vector3.left);
            {
                var kc = new KitBuilder("M_LegnoVernicePelata");
                kc.Box(new Vector3(-4.5f, 1.82f, 1.475f), new Vector3(-4.46f, 2.27f, 1.525f), 0, TileWood);
                kc.Box(new Vector3(-4.5f, 2.12f, 1.36f), new Vector3(-4.46f, 2.17f, 1.64f), 0, TileWood);
                MeshObject(props, "Cartello_Crocifisso", kc.Build(KitName("Crocifisso")), new[] { "M_LegnoVernicePelata" }, false);
            }
            Appeso(props, "Cartello_MazzoErbe", new Vector3(4.46f, 2.1f, -1.4f), new Vector3(0.05f, 0.35f, 0.14f), "Paglia");
            Appeso(props, "Cartello_Specchio", new Vector3(4.46f, 1.6f, -0.2f), new Vector3(0.03f, 0.4f, 0.28f), "Finestra");

            // tenda dell'alcova (aperta a meta') e bastone
            Cyl("BastoneTenda", props, new Vector3(-3.0f, 2.45f, 3.4f), 0.04f, 2.3f, "M_FerroRuggine02", new Vector3(0f, 0f, 90f), false);
            Box("Tenda", props, new Vector3(-2.45f, 1.25f, 3.4f), new Vector3(1.0f, 2.3f, 0.04f), "Tessuto2", null, false);
            Box("Tenda", props, new Vector3(-3.85f, 1.3f, 3.4f), new Vector3(0.35f, 2.2f, 0.06f), "Tessuto2", null, false);

            // panni appesi
            Clothesline(props, r, 2.8f, 2.55f, 2.45f, -4.5f, 4.5f, 5);
            Clothesline(props, r, -1.0f, 2.5f, 2.45f, -4.5f, -1.0f, 3);

            // porta d'uscita nel varco del muro sud
            var doorFr = new WallFrame(new Vector3(0f, 0f, -2.5f), Vector3.left, Vector3.forward);
            var door = DoorLeaf(props, "PortaUscita", doorFr, 0f, 0.08f, 0.2f, 1.15f, 2.15f, 0.25f);
            var d = door.AddComponent<Door>();
            d.targetScene = "10_Hub_Vicolo";
            d.targetSpawn = "da_basso_antonietta";

            // ---------------------------------------------------------------- alcova
            var letto = Pz(props, "LettoFerro", "B_LettoFerro", new[] { "M_FerroRuggine02", "M_TessutoLino", "Tessuto1" }, LettoFerro, new Vector3(-3.85f, 0f, 6.8f), 0f);
            letto.transform.localScale = new Vector3(0.86f, 1f, 1f); // letto da una piazza e mezza: lascia un corridoio di 90 cm
            Pz(props, "MaterassoTerra", "B_MaterassoTerra", new[] { "M_TessutoLino", "PannoBeige", "PannoTerra" }, MaterassoTerra, new Vector3(-1.95f, 0f, 6.8f), 0f, false);
            var baule = Model("CassaLegno", props, new Vector3(-3.95f, 0f, 4.5f), 0f, 1.3f, true, "BauleAlcova");
            Candela(props, "Alcova", new Vector3(-3.95f, 0.45f, 4.5f), baule);
            Model("SecchioLegno", props, new Vector3(-1.8f, 0f, 4.4f), 0f, 1.1f, true, "SecchioNotte");
            Santino(props, "SantinoMadonna", new Vector3(-3.75f, 1.9f, 7.8f), Vector3.back, 0.3f, 0.44f);
            Santino(props, "Ricordo", new Vector3(-3.1f, 1.7f, 7.8f), Vector3.back, 0.3f, 0.22f);
            Appeso(props, "Cartello_CappelloAppeso", new Vector3(-2.2f, 1.75f, 7.76f), new Vector3(0.28f, 0.05f, 0.1f), "PannoGrigio");
            Appeso(props, "Cartello_Gancio", new Vector3(-2.2f, 1.85f, 7.78f), new Vector3(0.04f, 0.1f, 0.04f), "M_FerroRuggine02");

            // ---------------------------------------------------------------- cucina
            var fornello = Pz(props, "FornelloCarbone", "B_Fornello", new[] { "M_FerroRuggine02", "Glow" }, Fornello, new Vector3(4.1f, 0f, 7.45f), 180f);
            var tavCucina = Model("TavoloLegno02", props, new Vector3(2.2f, 0f, 5.6f), 90f, 1.1f, true, "TavoloCucina");
            var credCucina = Pz(props, "Credenza", "B_Credenza", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02", "Cera" }, Credenza, new Vector3(1.85f, 0f, 7.2f), 90f);
            Candela(props, "Cucina", new Vector3(1.85f, 0.96f, 6.9f), credCucina);
            // pentole appese a un'asse sulla parete est
            Appeso(props, "Appeso_AsseePentole", new Vector3(4.46f, 1.75f, 5.7f), new Vector3(0.05f, 0.05f, 1.8f), "M_LegnoVernicePelata");
            for (int i = 0; i < 5; i++)
                Cyl("Appeso_Pentola" + i, props, new Vector3(4.34f, 1.6f - (i % 2) * 0.08f, 4.95f + i * 0.35f), 0.2f + (i % 3) * 0.03f, 0.14f, "M_FerroRuggineGrossa", new Vector3(0f, 0f, 90f), false);
            Pz(props, "BacinellaCucina", "B_BacinellaTreppiede", new[] { "M_FerroRuggine02", "M_SecchioMetallo" }, kb =>
            {
                foreach (var a in new[] { 0f, 120f, 240f })
                {
                    float rad = a * Mathf.Deg2Rad;
                    kb.Tube(new Vector3(Mathf.Cos(rad) * 0.2f, 0f, Mathf.Sin(rad) * 0.2f), new Vector3(Mathf.Cos(rad) * 0.12f, 0.52f, Mathf.Sin(rad) * 0.12f), 0.015f, 4, 0, 0.3f);
                }
                kb.Tube(new Vector3(0f, 0.52f, 0f), new Vector3(0f, 0.66f, 0f), 0.28f, 12, 1, 0.5f);
            }, new Vector3(4.0f, 0f, 4.5f), 0f);
            Model("SecchioLegno", props, new Vector3(4.2f, 0f, 5.2f), 20f, 1.1f, true, "SecchioAcqua");
            Model("SecchioMetallo", props, new Vector3(4.15f, 0f, 6.0f), 0f, 1.1f, true, "SecchioAcqua");
            Model("CestaVimini", props, new Vector3(1.9f, 0f, 4.5f), 0f, 1.3f, true, "CestaPanni");
            Appeso(props, "Cartello_Strofinaccio", new Vector3(4.455f, 1.1f, 4.2f), new Vector3(0.03f, 0.4f, 0.28f), "PannoBianco");

            // ---------------------------------------------------------------- cortile sul retro
            Pezzo(props, "Lavatoio", "Lavatoio", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Lavatoio, new Vector3(1.7f, 0f, 11.5f), 90f, 0.02f);
            Pezzo(props, "Pozzo", "Pozzo", new[] { "M_ArenariaBlocchi", "M_LegnoVernicePelata", "M_FerroRuggine02" }, Pozzo, new Vector3(5.35f, 0f, 12.9f), 0f, 0.02f);
            Pezzo(props, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, new Vector3(5.6f, 0f, 9.0f), 10f, 0.03f);
            Pezzo(props, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, new Vector3(4.7f, 0f, 8.9f), -15f, 0.03f);
            Clothesline(props, r, 10.4f, 3.4f, 3.3f, 1.2f, 6.3f, 5);
            Clothesline(props, r, 12.0f, 3.25f, 3.35f, 1.2f, 6.3f, 4);
            Model("BotteVino", props, new Vector3(1.7f, 0f, 13.4f), 0f, 1.25f, true, "Botte");
            var cassaC1 = Model("CassaLegno", props, new Vector3(2.6f, 0f, 13.6f), 10f, 1.3f, true, "Cassa");
            var cassaC2 = Model("CassaLegno", props, new Vector3(2.6f, 0.55f, 13.6f), 30f, 1.1f, true, "Cassa");
            Impila(cassaC2, cassaC1, Vector2.zero);
            Basilico(props, new Vector3(2.7f, 0f, 12.6f), 0f);
            Basilico(props, new Vector3(6.0f, 0f, 11.9f), 0f);
            Bacinella(props, new Vector3(2.4f, 0f, 9.3f));
            Sedia(props, new Vector3(5.5f, 0f, 10.9f), 200f);
            Pezzo(props, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, new Vector3(4.4f, 0f, 11.3f), 0f, 0.03f);
            Model("ScalaLegno", props, new Vector3(1.62f, 0f, 9.4f), -90f, 1.5f, true, "ScalaLegno"); // R017: scala appoggiata al muro, e' un ostacolo (prima senza collider: Amerigo l'attraversava)
            Model("SecchioMetallo", props, new Vector3(4.0f, 0f, 10.0f), 0f, 1.1f, true, "Secchio");
            // porta chiusa sul retro del cortile (segreto)
            DoorLeaf(props, "PortaRetro", new WallFrame(new Vector3(1.2f, 0f, 14.1f), Vector3.right, Vector3.back), 2.5f, 0.08f, 0.2f, 1.0f, 2.15f, 0.25f);
            noBalcone.Add(new Vector3(3.7f, 0f, 14.1f));

            // soffitto del basso (R006 tappa 4): lastra sopra stanza, alcova, blocco e cucina (il cortile resta a cielo aperto) con travi a vista.
            // Il nome "MuroSoffitto" lo mette sul layer VistaLibera: WallFader lo nasconde solo quando sta fra la camera e Amerigo
            // (camera sopra il soffitto); con la camera sotto la volta il soffitto si vede.
            Box("MuroSoffittoStanza", env, new Vector3(0f, H + 0.075f, 2.63f), new Vector3(9.6f, 0.15f, 10.86f), PL, null, true);
            foreach (float zt in new[] { -2.0f, -0.5f, 1.0f, 2.5f })
                Box("AppesoTraveStanza", props, new Vector3(0f, H - 0.1f, zt), new Vector3(8.95f, 0.2f, 0.2f), "M_LegnoVernicePelata", null, false);
            foreach (float zt in new[] { 5.0f, 6.8f })
            {
                Box("AppesoTraveAlcova", props, new Vector3(-3.0f, H - 0.1f, zt), new Vector3(2.95f, 0.2f, 0.2f), "M_LegnoVernicePelata", null, false);
                Box("AppesoTraveCucina", props, new Vector3(3.0f, H - 0.1f, zt), new Vector3(2.95f, 0.2f, 0.2f), "M_LegnoVernicePelata", null, false);
            }

            RisolviSpazio("20_Int_BassoAntonietta");

            // le scarpe (due blocchi a forma di scarpa) vicino alla porta
            var shoes = Empty("Scarpe", props, new Vector3(1.3f, 0f, -2.0f));
            ShoeObject(shoes.transform, new Vector3(-0.1f, 0f, 0f), 8f, 1f);
            ShoeObject(shoes.transform, new Vector3(0.12f, 0f, 0.03f), -14f, 1f);
            var bc = shoes.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.08f, 0f);
            bc.size = new Vector3(0.5f, 0.18f, 0.42f);
            var shoeGlow = shoes.AddComponent<Glow>();
            shoeGlow.intensity = 0.35f; // alone discreto: la forma della scarpa deve restare leggibile
            var pick = shoes.AddComponent<ItemPickup>();
            pick.oggettoId = "scarpe";

            // luce calda sopra il tavolo (lume a petrolio): una sola Point Light
            var lgo = new GameObject("Luce Calda");
            lgo.transform.position = new Vector3(1.8f, 1.8f, 0.8f);
            var pl = lgo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.70f, 0.40f);
            pl.intensity = 4f;
            pl.range = 9f;
            pl.shadows = LightShadows.None;

            Spawn("ingresso", new Vector3(0f, 0f, -1.7f), 0f, spawns);
            Spawn("oggetto_scarpe", new Vector3(0.7f, 0f, -1.4f), 90f, spawns);

            SegretiBasso();
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, new Color(0.10f, 0.07f, 0.05f), true);
            BuildPlayer(new Vector3(0f, 0.05f, -1.7f), 0f, cam, CamProfilo.InternoPiccoloAdulto, true); // il basso e' nel presente: Amerigo adulto
            SaveScene(scene, "20_Int_BassoAntonietta");
        }

        static void SegretiBasso()
        {
            Segreto("Doppio fondo della cassapanca", new Vector3(4.15f, 0.2f, -0.6f), "La cassapanca contro la parete est ha il fondo piu' alto del dovuto: lettere o fotografie nascoste da Antonietta.");
            Segreto("Mattone sciolto dietro l'altarino", new Vector3(0.2f, 1.5f, 3.45f), "Nella parete nord, sotto la mensola dei lumini: un mattone che gira. Cavita' per un oggetto piccolo o una chiave.");
            Segreto("Ripostiglio chiuso fra alcova e cucina", new Vector3(0f, 1.0f, 5.9f), "Intercapedine di 2,4 x 4,3 m dietro l'altarino (blocco pieno oggi): si apre con un pannello nella parete nord, dietro la credenza o l'altarino.");
            Segreto("Sotto il letto di ferro", new Vector3(-3.75f, 0.1f, 6.4f), "Fra le reti e il muro: una scatola di latta con i ricordi di Amerigo bambino.");
            Segreto("Retro della tenda dell'alcova", new Vector3(-3.0f, 1.2f, 3.55f), "Spazio fra tenda e parete: un biglietto o un santino appuntato.");
            Segreto("Cassapanca ai piedi del letto", new Vector3(-3.75f, 0.45f, 4.6f), "Baule dell'alcova con la candela: dentro, la coperta buona e qualcosa di piu'.");
            Segreto("Mattonella sconnessa sotto il tavolo", new Vector3(2.3f, 0f, 1.2f), "Una mattonella del pavimento di cotto che si solleva: nascondiglio per un oggetto piccolo.");
            Segreto("Credenza della stanza principale", new Vector3(-4.2f, 1.0f, 1.5f), "Dietro i piatti in alto c'e' un fondo finto; la credenza si puo' spostare per rivelare il ripostiglio.");
            Segreto("Sotto la bacinella della cucina", new Vector3(4.0f, 0.05f, 4.5f), "Il treppiede della bacinella nasconde una rientranza nel muro.");
            Segreto("Pentole appese della cucina", new Vector3(4.35f, 1.6f, 5.6f), "Una pentola appesa e' piu' pesante delle altre: dentro un oggetto o una lettera.");
            Segreto("Pozzo del cortile", new Vector3(5.35f, 0.8f, 12.9f), "Il pozzo ha una secchia legata e un'asse sconnessa: il fondo puo' ospitare un oggetto o un indizio.");
            Segreto("Porta chiusa sul retro del cortile", new Vector3(3.7f, 0f, 13.8f), "Porta a nord del cortile (PortaRetro), oggi chiusa: potrebbe condurre a un vicolo segreto o a un altro luogo.");
            Segreto("Pila di casse del cortile", new Vector3(2.6f, 0.5f, 13.6f), "Casse accatastate sotto il filo del bucato: un doppio fondo o un ripiano nascosto.");
            Segreto("Lavatoio del cortile", new Vector3(1.7f, 0.5f, 11.5f), "Dietro il lavatoio, contro il muro ovest, una pietra staccata.");
            Segreto("Scala a pioli appoggiata alla parete", new Vector3(1.62f, 1.0f, 9.4f), "Scala di legno contro il muro ovest: sale oltre il bordo del cortile (accesso a un terrazzo, futuro).");
            ScriviDocSegreti(PuntiSegretiBassoPath, "Punti per i segreti del basso di Antonietta (R011)", "20_Int_BassoAntonietta",
                "Stanza principale x -4,5..4,5 z -2,5..3,5; alcova x -4,5..-1,5 z 3,8..7,8; cucina x 1,5..4,5 z 3,8..7,8; cortile x 1,2..6,3 z 8,1..14,1 (aperto, pareti da 4,2 m); blocco pieno x -1,2..1,2 z 3,8..8,1 (ripostiglio nascosto).",
                null);
        }

        // ------------------------------------------------------------------ rapporti dei punti segreti

        static void ScriviDocSegreti(string path, string titolo, string scena, string zoneTesto, string extra)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + titolo);
            sb.AppendLine();
            sb.AppendLine("Generato da `SceneBuilder` (R006, tappa 3). Coordinate mondo nella scena `" + scena + "` (x, y, z).");
            sb.AppendLine("Non toccare a mano: rigenerare la scena da **Treno > Costruisci tutte le scene** aggiorna questo file.");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(zoneTesto)) { sb.AppendLine("**Zone:** " + zoneTesto); sb.AppendLine(); }
            sb.AppendLine("| # | Punto | x | y | z | Idea |");
            sb.AppendLine("|---|-------|---|---|---|------|");
            int i = 1;
            foreach (var s in segreti)
                sb.AppendLine("| " + (i++) + " | " + s.nome + " | " + s.pos.x.ToString("0.0") + " | " + s.pos.y.ToString("0.0") + " | " + s.pos.z.ToString("0.0") + " | " + s.descr + " |");
            if (!string.IsNullOrEmpty(extra)) { sb.AppendLine(); sb.AppendLine(extra); }
            string full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
