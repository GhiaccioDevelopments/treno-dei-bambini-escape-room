using System.Collections.Generic;
using TMPro;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.Mappa
{
    /// <summary>Un nome di strada scritto sulla pianta: cartellino di carta con il testo da mappe.json.</summary>
    public sealed class EtichettaStrada
    {
        public RectTransform rt;
        public CanvasGroup gruppo;
        public float x, z, w, h;
        public int priorita;     // 0 = si scrive per prima
        public string strada;    // id della strada
        public string nome;
        public TextMeshProUGUI testo;
        public float corpo;
        public bool misurata;
    }

    /// <summary>Pezzi comuni a minimappa e mappa grande: etichette dei vicoli, scelta di quelle che non si sovrappongono, ricerca del giocatore.</summary>
    public static class MappaUi
    {
        public static int Priorita(string tipo)
        {
            switch (tipo)
            {
                case "principale": return 0;
                case "largo": return 1;
                case "stanza": return 1;
                case "cortile": return 2;
                case "vicolo": return 3;
                default: return 4;   // rampa
            }
        }

        /// <summary>Crea un cartellino per ogni ancora di ogni strada della scena, figli di parent (centrati sull'ancora).</summary>
        public static List<EtichettaStrada> CreaEtichette(RectTransform parent, MappaScenaDef def, float corpo)
        {
            var l = new List<EtichettaStrada>();
            if (def == null) return l;
            foreach (var s in def.strade)
            {
                if (string.IsNullOrEmpty(s.nome) || s.ancore == null) continue;
                foreach (var a in s.ancore)
                {
                    var rt = UIFactory.NewRect("Via_" + s.id, parent);
                    var bg = rt.gameObject.AddComponent<Image>();
                    bg.sprite = UIArt.Tag(); bg.type = Image.Type.Sliced; bg.raycastTarget = false;
                    bg.color = new Color(1f, 0.97f, 0.88f, 0.86f);
                    var t = UIFactory.MakeText(rt, s.nome, corpo, UITheme.Inchiostro, TextAlignmentOptions.Center, FontStyles.Italic);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    t.overflowMode = TextOverflowModes.Overflow;
                    // il testo non si puo' misurare finche' il pannello e' spento: stima ora, misura vera alla prima comparsa (vedi Misura)
                    float w = s.nome.Length * corpo * 0.46f + corpo * 1.1f, h = corpo * 1.6f;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(w, h);
                    UIFactory.Stretch(t.rectTransform);
                    var cg = rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
                    rt.gameObject.SetActive(false);
                    l.Add(new EtichettaStrada { rt = rt, gruppo = cg, x = a.x, z = a.z, w = w, h = h, priorita = Priorita(s.tipo), strada = s.id, nome = s.nome, testo = t, corpo = corpo });
                }
            }
            return l;
        }

        /// <summary>Misura vera del cartellino, appena il testo e' attivo.</summary>
        public static void Misura(EtichettaStrada e)
        {
            if (e.misurata || e.testo == null || !e.rt.gameObject.activeInHierarchy) return;
            e.misurata = true;
            e.w = e.testo.GetPreferredValues(e.nome).x + e.corpo * 1.1f;
            e.rt.sizeDelta = new Vector2(e.w, e.h);
        }

        static int[] ordine = new int[0];

        /// <summary>
        /// Sceglie le etichette da mostrare: tra i candidati, per priorita' (poi chiave crescente), tiene quelle che non si sovrappongono
        /// a una gia' scelta. pos = centro in pixel dello schermo/mappa, margine = respiro tra i cartellini.
        /// </summary>
        public static void Seleziona(List<EtichettaStrada> l, Vector2[] pos, float[] chiave, bool[] candidato, bool[] scelto, float margine)
        {
            int n = l.Count;
            if (ordine.Length < n) ordine = new int[n];
            int m = 0;
            for (int i = 0; i < n; i++) { scelto[i] = false; if (candidato[i]) ordine[m++] = i; }
            for (int a = 1; a < m; a++)
            {
                int v = ordine[a]; int b = a - 1;
                while (b >= 0 && Prima(l, chiave, v, ordine[b])) { ordine[b + 1] = ordine[b]; b--; }
                ordine[b + 1] = v;
            }
            for (int k = 0; k < m; k++)
            {
                int i = ordine[k];
                bool libero = true;
                for (int j = 0; j < k && libero; j++)
                {
                    int o = ordine[j];
                    if (!scelto[o]) continue;
                    if (Mathf.Abs(pos[i].x - pos[o].x) < (l[i].w + l[o].w) * 0.5f + margine && Mathf.Abs(pos[i].y - pos[o].y) < (l[i].h + l[o].h) * 0.5f + margine) libero = false;
                    else if (l[i].strada == l[o].strada && Mathf.Abs(pos[i].x - pos[o].x) + Mathf.Abs(pos[i].y - pos[o].y) < 90f) libero = false; // lo stesso nome non si ripete a due passi
                }
                scelto[i] = libero;
            }
        }

        static bool Prima(List<EtichettaStrada> l, float[] chiave, int a, int b)
        {
            if (l[a].priorita != l[b].priorita) return l[a].priorita < l[b].priorita;
            return chiave[a] < chiave[b];
        }

        /// <summary>La strada (o stanza) dove si trova il punto: l'ultima della lista con una zona che lo contiene (le piu' specifiche stanno in fondo).</summary>
        public static StradaDef StradaIn(MappaScenaDef def, float x, float z)
        {
            if (def == null) return null;
            StradaDef trovata = null;
            for (int i = 0; i < def.strade.Count; i++)
            {
                var s = def.strade[i];
                for (int k = 0; k < s.zone.Count; k++)
                {
                    var r = s.zone[k];
                    if (x >= r.x0 - 0.3f && x <= r.x1 + 0.3f && z >= r.z0 - 0.3f && z <= r.z1 + 0.3f) { trovata = s; break; }
                }
            }
            return trovata;
        }

        public static Vector2 Ruota(Vector2 v, float gradi)
        {
            float a = gradi * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
