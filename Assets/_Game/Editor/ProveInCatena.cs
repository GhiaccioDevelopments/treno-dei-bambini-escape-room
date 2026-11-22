// Esegue le prove automatiche (self-test in Play) una dopo l'altra; gli esiti in <progetto>/<nome>test/OK.txt o FALLITA.txt
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
public static class ProveInCatena
{
    const string Chiave = "prove_coda";

    [MenuItem("Treno/Prove/Tutte le prove automatiche (Play)")]
    public static void Tutte() { Avvia("enigmitest,segretitest,mappatest,introtest,straccitest,volantinotest,cappottotest,violinotest,finaletest"); }

    public static void Avvia(string elenco)
    {
        SessionState.SetString(Chiave, elenco);
        Prossima();
    }

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetString(Chiave, "") != "")
                EditorApplication.delayCall += Prossima;
        };
    }

    static void Prossima()
    {
        string coda = SessionState.GetString(Chiave, "");
        if (coda == "" || EditorApplication.isPlaying) return;
        string[] p = coda.Split(',');
        string n = p[0];
        SessionState.SetString(Chiave, string.Join(",", p, 1, p.Length - 1));
        var d = System.IO.Path.Combine(Application.dataPath, "..", n);
        foreach (var f in new[] { "OK.txt", "FALLITA.txt" }) { var q = System.IO.Path.Combine(d, f); if (System.IO.File.Exists(q)) System.IO.File.Delete(q); }
        SessionState.SetBool(n, true);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Boot.unity");
        EditorApplication.isPlaying = true;
        Debug.Log("[ProveInCatena] avviata " + n + ", restano: " + SessionState.GetString(Chiave, ""));
    }
}
}
