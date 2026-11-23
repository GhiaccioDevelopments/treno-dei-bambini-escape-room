using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>
        /// R025: aggiunge nel ricordo (30_FB_Scarpe) il manifesto "TRENI PER IL NORD" (testo in testi_ambiente.json, gruppo ricordo) al centro del muro dei manifesti
        /// e sposta "Scuola" di lato, senza ricostruire la scena (una ricostruzione perderebbe la luce baked). Lo stesso posto e' nel costruttore del ricordo
        /// (SceneBuilderRicordo.cs): le ricostruzioni future lo contengono gia'. Si puo' lanciare piu' volte.
        /// </summary>
        [MenuItem("Treno/Filmato/Aggiungi manifesto Treni al ricordo")]
        public static void AggiungiManifestoTreni()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scena = EditorSceneManager.OpenScene("Assets/_Game/Scenes/30_FB_Scarpe.unity", OpenSceneMode.Single);
            matCache.Clear();
            PreparaTestiAmbiente("ricordo"); // crea MP_Treni (e rinfresca le altre insegne del ricordo)
            GameObject scuola = null, treni = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.gameObject.scene != scena) continue;
                if (t.name == "Manifesto_Scuola") scuola = t.gameObject;
                if (t.name == "Manifesto_Treni") treni = t.gameObject;
            }
            if (scuola == null) { Debug.LogWarning("[Filmato] Manifesto_Scuola non trovato nel ricordo."); return; }
            if (treni != null) { Debug.Log("[Filmato] Manifesto_Treni c'e' gia'."); return; }
            var n = scuola.transform.forward;                 // i manifesti guardano verso la strada
            var al = Vector3.Cross(n, Vector3.up);
            var centro = scuola.transform.position;
            // Manifesto() posa la tavola 1,15 cm davanti a pos: qui si parte dal centro attuale di "Scuola"
            var env = scuola.transform.parent;
            Manifesto(env, "Treni", centro - n * 0.0115f + Vector3.up * -0.05f, n, 0.62f, 0.93f, "Manifesto_Treni");
            scuola.transform.position = centro + al * 2.4f;
            EditorSceneManager.MarkSceneDirty(scena);
            EditorSceneManager.SaveScene(scena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Filmato] Manifesto 'TRENI PER IL NORD' aggiunto al muro dei manifesti del ricordo.");
        }
    }
}
