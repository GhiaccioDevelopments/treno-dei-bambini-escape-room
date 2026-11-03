using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>Postazione di un enigma nella stanza-ricordo. Solo la prima non risolta è attiva (con Glow).</summary>
    public class PuzzleStation : Interactable
    {
        public EnigmaDef def;

        EscapeRoomController ctrl;
        Glow glow;
        NascostoPuzzle nascosto;
        public bool Active { get; private set; }
        public bool Solved { get; private set; }

        public void Setup(EnigmaDef d, EscapeRoomController c, Glow g)
        {
            def = d;
            ctrl = c;
            glow = g;
        }

        public void Refresh(bool active, bool solved)
        {
            Active = active;
            Solved = solved;
            if (glow != null) glow.on = active && !solved;
            string titolo = def != null ? def.titolo : "";
            if (solved) prompt = "Risolto: " + titolo;
            else if (!active) prompt = "Enigma non ancora accessibile";
            else prompt = "Esamina: " + titolo;
        }

        public override void Interact()
        {
            if (def == null || UIRoot.I == null) return;
            if (Solved) { UIRoot.I.Toast(def.titolo + ": già risolto.", 2.5f); return; }
            if (!Active) { UIRoot.I.Toast("Prima devi risolvere l'enigma precedente.", 3f); return; }

            if ((def.tipo ?? "").ToLowerInvariant() == "nascosto")
            {
                if (nascosto == null)
                    nascosto = NascostoPuzzle.Begin(def, null, ctrl != null ? (System.Action<string>)ctrl.SetExtraHud : null, OnSolved);
                else nascosto.ShowHint();
                return;
            }

            var p = PuzzlePanel.Open(def, OnSolved);
            if (p == null) UIRoot.I.Toast("Questo enigma non è ancora disponibile.");
        }

        void OnSolved()
        {
            nascosto = null;
            if (ctrl != null) ctrl.OnEnigmaSolved(this);
        }
    }
}
