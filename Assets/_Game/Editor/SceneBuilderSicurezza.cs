using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R017: rete di sicurezza di ogni scena. Un piano scuro molto sotto la mappa (non visibile in condizioni normali) fa si' che un
    /// eventuale buco residuo non mostri mai il cielo o lo sfondo; il KillPlane (Scripts/World) riporta allo SpawnPoint il giocatore che
    /// cadesse sotto il piano, con la dissolvenza di gioco. Chiamata dai costruttori di scena dopo la costruzione dell'ambiente.
    /// </summary>
    public static partial class SceneBuilder
    {
        /// <summary>Distanza (m) tra il punto piu' basso della mappa e il piano di sicurezza.</summary>
        const float QuotaPianoSicurezza = 2.5f;
        /// <summary>Distanza (m) tra il piano di sicurezza e la quota di recupero del KillPlane.</summary>
        const float QuotaRecupero = 3f;

        static Material MaterialePianoSicurezza()
        {
            string path = ArtGenerator.MaterialPath("M_PianoSicurezza");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.025f, 0.02f, 0.015f));
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Piano scuro sotto la mappa e, con 'recupero', KillPlane che riporta il giocatore allo SpawnPoint piu' vicino.</summary>
        static void PredisponiSicurezza(bool recupero)
        {
            bool any = false;
            Bounds b = new Bounds();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return;
            float y = b.min.y - QuotaPianoSicurezza;
            var root = new GameObject("Sicurezza").transform;
            var piano = Box("PianoSicurezza", root, new Vector3(b.center.x, y - 0.1f, b.center.z), new Vector3(b.size.x + 300f, 0.2f, b.size.z + 300f), null, null, false);
            var rend = piano.GetComponent<Renderer>();
            rend.sharedMaterial = MaterialePianoSicurezza();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            if (recupero)
            {
                var kp = new GameObject("KillPlane").AddComponent<KillPlane>();
                kp.transform.SetParent(root, false);
                kp.sogliaY = y - QuotaRecupero;
            }
        }
    }
}
