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

        IEnumerator Start()
        {
            var rnd = new System.Random(8);
            int partenza = Mathf.Max(0, GraphicsPresets.Current);
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
            GraphicsPresets.Apply(partenza, salva);
            Application.logMessageReceived -= cb;
            Debug.Log("[GraphicsStress] finito: " + cambi + " cambi, errori in console: " + errori);
            Destroy(gameObject);
        }
    }
}
#endif
