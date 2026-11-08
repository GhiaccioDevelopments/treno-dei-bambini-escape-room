using System.Collections.Generic;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>
    /// R008: densita' degli oggetti sparsi (panni, macerie, secchi, ceste, casse, botti, vasi). A "Medi" e "Pochi" nasconde in modo
    /// deterministico un terzo / due terzi degli oggetti che si trovano per nome: si spengono solo i Renderer (collisioni e giocabilita'
    /// restano identiche) e gli oggetti con cui si puo' interagire non vengono mai toccati. Si applica a ogni cambio di scena e subito dal menu.
    /// </summary>
    public static class ScenaDettagli
    {
        static readonly string[] Prefissi =
        {
            "Macerie_Mattoni", "Panno_", "SecchioMetallo", "SecchioLegno", "CestaVimini", "BotteVino", "CassaLegno", "VasoTerracotta"
        };

        sealed class Elemento
        {
            public Renderer[] rend;
            public bool[] orig;
            public int cesto;   // 0..2: con "Medi" si nasconde il cesto 0, con "Pochi" i cesti 0 e 1
        }

        static List<Elemento> _lista;
        static int _applicato = 2;

        public static void NuovaScena()
        {
            _lista = null;
            _applicato = 2;
        }

        static bool Candidato(string nome)
        {
            for (int i = 0; i < Prefissi.Length; i++)
                if (nome.StartsWith(Prefissi[i], System.StringComparison.Ordinal)) return true;
            return false;
        }

        static void Costruisci()
        {
            _lista = new List<Elemento>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (!Candidato(t.name)) continue;
                // un pezzo che sta dentro un altro candidato lo gestisce il padre
                var p = t.parent;
                bool figlio = false;
                while (p != null) { if (Candidato(p.name)) { figlio = true; break; } p = p.parent; }
                if (figlio) continue;
                if (t.GetComponentInChildren<Interactable>(true) != null || t.GetComponentInParent<Interactable>() != null) continue;
                var rend = t.GetComponentsInChildren<Renderer>(true);
                if (rend.Length == 0) continue;
                int h = Mathf.Abs(Mathf.RoundToInt(t.position.x * 37f) * 31 + Mathf.RoundToInt(t.position.z * 53f) * 17 + t.name.GetHashCode() % 997);
                var orig = new bool[rend.Length];
                for (int i = 0; i < rend.Length; i++) orig[i] = rend[i].enabled;
                _lista.Add(new Elemento { rend = rend, orig = orig, cesto = h % 3 });
            }
        }

        /// <summary>livello: 0 = Pochi, 1 = Medi, 2 = Tutti.</summary>
        public static void Applica(int livello)
        {
            livello = Mathf.Clamp(livello, 0, 2);
            if (livello == 2 && _applicato == 2) return; // tutto visibile e nulla da ripristinare: nessun costo
            if (_lista == null) Costruisci();
            foreach (var v in _lista)
            {
                bool nascosto = livello == 0 ? v.cesto != 2 : livello == 1 ? v.cesto == 0 : false;
                for (int i = 0; i < v.rend.Length; i++)
                    if (v.rend[i] != null) v.rend[i].enabled = v.orig[i] && !nascosto;
            }
            _applicato = livello;
        }
    }
}
