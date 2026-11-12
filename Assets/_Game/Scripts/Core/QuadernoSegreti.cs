using System;
using System.Collections.Generic;

namespace TrenoBambini.Core
{
    [Serializable]
    public class QuadernoVoce
    {
        public string id, titolo, testo;
    }

    /// <summary>
    /// R023: aggancio per i segreti di R011. Il quaderno dell'inventario (pagina "Quaderno", sezione "Segreti") chiede l'elenco a <see cref="Provider"/>:
    /// R011 lo imposta una volta (per esempio all'avvio del gioco) con una funzione che legge dal salvataggio i segreti trovati.
    /// Finche' non e' impostato, la sezione mostra il testo "nessun segreto" preso da ui_menu.json.
    /// </summary>
    public static class QuadernoSegreti
    {
        public static Func<List<QuadernoVoce>> Provider;

        public static List<QuadernoVoce> Trovati()
        {
            var l = Provider != null ? Provider() : null;
            return l ?? new List<QuadernoVoce>();
        }
    }
}
