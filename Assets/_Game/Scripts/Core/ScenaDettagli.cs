using System.Collections.Generic;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>
    /// R008: densita' degli oggetti sparsi (oggi nell'hub risultano idonei solo i panni stesi: gli altri hanno collider o luce cotta). A "Medi" e "Pochi" nasconde in modo
    /// deterministico un terzo / due terzi degli oggetti che si trovano per nome. Vengono toccati solo gli oggetti senza collider (non trigger),
    /// senza luce cotta (lightmap) e non interagibili: cosi' non restano ostacoli invisibili ne' ombre cotte. Si applica a ogni cambio di scena e subito dal menu.
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
                // solo oggetti sparsi "leggeri": con un collider (non trigger) sparirebbe l'oggetto ma non l'ostacolo; con la luce cotta (lightmap)
                // resterebbe la sua ombra. Questi si lasciano sempre visibili.
                bool escluso = false;
                foreach (var c in t.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger) { escluso = true; break; }
                if (escluso) continue;
                foreach (var r in rend) if (r.lightmapIndex >= 0 && r.lightmapIndex < 65534) { escluso = true; break; }
                if (escluso) continue;
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
