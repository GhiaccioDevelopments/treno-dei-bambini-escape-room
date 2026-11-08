using System.IO;
using TrenoBambini.UI.Bozze;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R023 fase 1: menu Treno > R023 > Bozza ... Apre l'hub, entra in Play e crea la bozza (BozzaR023). Non tocca Titolo e Inventario esistenti.
    /// Le voci "+ screenshot" salvano un JPG 1280x720 in Docs/richieste/media/R023/bozze/ e poi escono dal Play.
    /// </summary>
    [InitializeOnLoad]
    public static class BozzeR023
    {
        const string KTipo = "R023_bozza_tipo", KShot = "R023_bozza_shot", KDopo = "R023_bozza_dopo";
        static BozzaR023 attiva;

        static BozzeR023()
        {
            EditorApplication.playModeStateChanged += OnPlay;
            EditorApplication.update += Poll;
        }

        [MenuItem("Treno/R023/Bozza menu A (locandina)")] static void MA() { Avvia(BozzaTipo.MenuA, ""); }
        [MenuItem("Treno/R023/Bozza menu B (biglietti)")] static void MB() { Avvia(BozzaTipo.MenuB, ""); }
        [MenuItem("Treno/R023/Bozza inventario A (latta e quaderno a fianco)")] static void IA() { Avvia(BozzaTipo.InventarioA, ""); }
        [MenuItem("Treno/R023/Bozza inventario B (latta dall'alto e quaderno sotto)")] static void IB() { Avvia(BozzaTipo.InventarioB, ""); }
        [MenuItem("Treno/R023/Bozza menu A + screenshot")] static void MAs() { Avvia(BozzaTipo.MenuA, "menu_A"); }
        [MenuItem("Treno/R023/Bozza menu B + screenshot")] static void MBs() { Avvia(BozzaTipo.MenuB, "menu_B"); }
        [MenuItem("Treno/R023/Bozza inventario A + screenshot")] static void IAs() { Avvia(BozzaTipo.InventarioA, "inventario_A"); }
        [MenuItem("Treno/R023/Bozza inventario B + screenshot")] static void IBs() { Avvia(BozzaTipo.InventarioB, "inventario_B"); }

        static void Avvia(BozzaTipo t, string shot)
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; }
            SessionState.SetInt(KTipo, (int)t);
            SessionState.SetString(KShot, shot);
            SessionState.SetFloat(KDopo, 7f);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/10_Hub_Vicolo.unity", OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        static void OnPlay(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredPlayMode) return;
            if (SessionState.GetInt(KTipo, -1) < 0) return;
            var go = new GameObject("BozzaR023");
            var b = go.AddComponent<BozzaR023>();
            b.tipo = (BozzaTipo)SessionState.GetInt(KTipo, 0);
            b.capturaNome = SessionState.GetString(KShot, "");
            b.capturaDopo = SessionState.GetFloat(KDopo, 7f);
            b.matLegno = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_TavoloLegno01.mat");
            b.matPanno = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Panno_Terra.mat");
            b.matCuoio = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Cuoio_Scarpe.mat");
            b.matSuola = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Suola_Scarpa.mat");
            b.matTerzo = b.matCuoio;
            b.meshScarpa = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Game/Generated/Kit/Scarpa_Logora.asset");
            attiva = b;
            SessionState.SetInt(KTipo, -1);
        }

        static void Poll()
        {
            if (attiva != null && attiva.Fatto && EditorApplication.isPlaying)
            {
                attiva = null;
                EditorApplication.isPlaying = false;
            }
        }
    }
}
