using System.Collections.Generic;
using TrenoBambini.Data;
using TrenoBambini.UI.Fx;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R016 (il finale, 1994): meccanismo "ricomponi". Un documento strappato (la polizza del Monte dei pegni): i pezzi (azione "pezzo", valore = posto giusto)
    /// sono sparsi sul tavolo, i posti (azione "slot", valore = indice) sul sottomano. Si tocca un pezzo e poi un posto: se combacia il pezzo si posa dritto,
    /// altrimenti un messaggio gentile e il pezzo resta in mano. Quando tutti i pezzi sono a posto compaiono i pezzi di EnigmaDef.dopoRicomposto (timbro, croce).
    /// Testi in EnigmaDef.strumenti (0 scegli il posto, 1 prima un pezzo, 2 non combacia, 3 ancora {0}, 4 gia' a posto).
    /// </summary>
    public partial class EsaminaView
    {
        string pezzoSel;
        readonly HashSet<int> pezziPosati = new HashSet<int>();

        public int PezziPosati { get { return pezziPosati.Count; } }

        int NPezzi { get { int n = 0; foreach (var kv in run) if ((kv.Value.d.azione ?? "").ToLowerInvariant() == "pezzo") n++; return n; } }

        /// <summary>Azioni dei pezzi del meccanismo R016. true = gestita.</summary>
        bool UsaFinale(Run r)
        {
            if (Meccanismo(def) != "ricomponi") return false;
            string az = (r.d.azione ?? "").ToLowerInvariant();
            if (az == "pezzo") { PrendiPezzo(r.d.id); return true; }
            if (az == "slot") { Posa(pezzoSel, Mathf.RoundToInt(r.d.valore)); return true; }
            return false;
        }

        /// <summary>Prende in mano un pezzo del documento. Pubblico per la prova.</summary>
        public void PrendiPezzo(string id)
        {
            Run r;
            if (solved || string.IsNullOrEmpty(id) || !run.TryGetValue(id, out r)) return;
            if (pezziPosati.Contains(Mathf.RoundToInt(r.d.valore))) { Info(S(4)); return; }
            Run prima;
            if (pezzoSel != null && run.TryGetValue(pezzoSel, out prima)) Evidenzia(prima, false);
            pezzoSel = id;
            Evidenzia(r, true);
            UISfx.Play(Sfx.ClickPaper, 0.45f);
            Info((!string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(0));
        }

        /// <summary>Posa il pezzo (id) sul posto k. Pubblico per la prova.</summary>
        public bool Posa(string id, int k)
        {
            if (solved) return false;
            Run p;
            if (string.IsNullOrEmpty(id) || !run.TryGetValue(id, out p)) { Info(S(1)); return false; }
            int giusto = Mathf.RoundToInt(p.d.valore);
            if (pezziPosati.Contains(giusto)) { Info(S(4)); return false; }
            if (giusto != k) { Wrong(S(2)); return false; }
            var slot = CercaRun("slot", k);
            if (slot != null && slot.go != null && p.go != null)
            {
                p.go.transform.localPosition = slot.go.transform.localPosition + new Vector3(0f, 0.004f, 0f);
                p.go.transform.localRotation = Quaternion.identity;
            }
            Evidenzia(p, false);
            pezzoSel = null;
            pezziPosati.Add(giusto);
            UISfx.Play(Sfx.TagDrop, 0.5f);
            int resto = NPezzi - pezziPosati.Count;
            if (resto > 0) { Info(string.Format(S(3), resto)); return true; }
            Rivela(def.dopoRicomposto, true);
            Solved();
            return true;
        }
    }
}
