#if UNITY_EDITOR || DEBUG
// R008: prova di robustezza delle impostazioni grafiche. Solo editor e build di sviluppo.
using System.Collections;
using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>
    /// Cambia preset e voci a raffica (un cambio ogni <see cref="intervallo"/> secondi) per provare che non ci siano crash ne' errori in console,
    /// in particolare dei decal di URP. Si avvia da codice (<c>new GameObject("GraphicsStress").AddComponent&lt;GraphicsStress&gt;()</c>)
    /// Al termine ripristina il preset di partenza e scrive "[GraphicsStress] finito" in console.
    /// </summary>
    public class GraphicsStress : MonoBehaviour
    {
        public int cambi = 60;
        public float intervallo = 0.25f;
        public bool salva = true;
        public bool autonomo;       // da riga di comando: carica la scena, fa la prova e chiude il gioco
        public string scena = "10_Hub_Vicolo";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Avvia()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-graphicsstress")
                {
                    var go = new GameObject("GraphicsStress");
                    DontDestroyOnLoad(go);
                    var st = go.AddComponent<GraphicsStress>();
                    st.autonomo = true; st.cambi = 300; st.intervallo = 0.1f;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) st.scena = args[i + 1];
                    return;
                }
        }

        IEnumerator Start()
        {
            var rnd = new System.Random(8);
            if (autonomo)
            {
                yield return new WaitForSecondsRealtime(4f);
                UnityEngine.SceneManagement.SceneManager.LoadScene(scena);
                yield return new WaitForSecondsRealtime(4f);
            }
            var stato = GraphicsPresets.EsportaStato(); // le impostazioni dell'utente, personalizzate comprese, tornano com'erano
            var voci = (Voce[])System.Enum.GetValues(typeof(Voce));
            int errori = 0;
            Application.LogCallback cb = (testo, stack, tipo) => { if (tipo == LogType.Error || tipo == LogType.Exception) errori++; };
            Application.logMessageReceived += cb;
            for (int i = 0; i < cambi; i++)
            {
                if (i % 3 == 2)
                {
                    var v = voci[rnd.Next(voci.Length)];
                    if (v == Voce.Risoluzione || v == Voce.SchermoIntero) continue;
                    GraphicsPresets.Set(v, rnd.Next(GraphicsPresets.NumOpzioni(v)));
                }
                else GraphicsPresets.Apply(rnd.Next(4), false);
                yield return new WaitForSecondsRealtime(intervallo);
            }
            GraphicsPresets.ImportaStato(stato, salva);
            Application.logMessageReceived -= cb;
            Debug.Log("[GraphicsStress] finito: " + cambi + " cambi, errori in console: " + errori);
            if (autonomo) Application.Quit();
            Destroy(gameObject);
        }
    }
}
#endif
