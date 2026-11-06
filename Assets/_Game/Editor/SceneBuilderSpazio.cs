using System.Collections.Generic;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006: gestione dello spazio nei costruttori di scena. I modelli appoggiati a terra sono "mobili": a fine costruzione
    /// RisolviSpazio() li allontana da pareti e altri oggetti usando gli stessi ingombri della verifica
    /// (Treno > Verifica > Compenetrazioni). Gli oggetti posati sopra un mobile ("cavalieri") ne seguono lo spostamento.
    /// </summary>
    public static partial class SceneBuilder
    {
        static readonly List<GameObject> mobili = new List<GameObject>();
        static readonly Dictionary<GameObject, List<GameObject>> cavalieri = new Dictionary<GameObject, List<GameObject>>();
        // punti (x,z) del piano di facciata dove non vanno balconi: lampioni, edicole, ancoraggi dei fili da bucato
        static readonly List<Vector3> noBalcone = new List<Vector3>();

        /// <summary>Distanza libera davanti alle facciate: zoccolo e fascia in pietra sporgono fino a 10 cm.</summary>
        const float SpessoreZoccolo = 0.11f;

        static void ResetSpazio()
        {
            mobili.Clear();
            cavalieri.Clear();
            noBalcone.Clear();
        }

        static void RegistraMobile(GameObject go)
        {
            if (go != null && !mobili.Contains(go)) mobili.Add(go);
        }

        /// <summary>Registra 'sopra' come appoggiato su 'sotto': seguira' i suoi spostamenti.</summary>
        static void Cavaliere(GameObject sopra, GameObject sotto)
        {
            if (sopra == null || sotto == null) return;
            List<GameObject> l;
            if (!cavalieri.TryGetValue(sotto, out l)) cavalieri[sotto] = l = new List<GameObject>();
            l.Add(sopra);
        }

        /// <summary>Quota della sommita' dell'oggetto (ingombro reale dei renderer).</summary>
        static float TopY(GameObject go)
        {
            bool any = false;
            Bounds b = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b.max.y : go.transform.position.y;
        }

        /// <summary>Appoggia 'sopra' sulla sommita' di 'sotto' (stessa x,z + scostamento) e lo registra come cavaliere.</summary>
        static void Impila(GameObject sopra, GameObject sotto, Vector2 scostamentoXZ)
        {
            var p = sotto.transform.position;
            sopra.transform.position = new Vector3(p.x + scostamentoXZ.x, TopY(sotto) - 0.003f, p.z + scostamentoXZ.y);
            Cavaliere(sopra, sotto);
        }

        /// <summary>Alloggia i mobili dopo la costruzione: li sposta fuori da muri e oggetti (vedi VerificaCompenetrazioni.Risolvi).</summary>
        static void RisolviSpazio(string scena)
        {
            int n = VerificaCompenetrazioni.Risolvi(mobili, cavalieri);
            Debug.Log("[SceneBuilder] " + scena + ": " + n + " spostamenti automatici per liberare lo spazio (" + mobili.Count + " mobili).");
        }

        /// <summary>Posa un pezzo di cumulo sul pavimento o, se 'impila', sul pezzo a terra che lo sovrasta di piu' (un solo livello).</summary>
        static void PosaSuCumulo(GameObject pezzo, List<GameObject> posati, bool impila)
        {
            Bounds b = BoundsDi(pezzo);
            float suolo = GroundY(b.center.x, b.center.z); // quota del suolo reale (zone rialzate e rampe), non y=0
            float base_ = suolo;
            if (impila)
            {
                float migliore = 0f;
                foreach (var q in posati)
                {
                    var bq = BoundsDi(q);
                    if (bq.min.y > suolo + 0.05f) continue; // porta solo chi sta a terra
                    float ox = Mathf.Min(b.max.x, bq.max.x) - Mathf.Max(b.min.x, bq.min.x);
                    float oz = Mathf.Min(b.max.z, bq.max.z) - Mathf.Max(b.min.z, bq.min.z);
                    if (ox > 0.3f * Mathf.Min(b.size.x, bq.size.x) && oz > 0.3f * Mathf.Min(b.size.z, bq.size.z) && ox * oz > migliore)
                    {
                        migliore = ox * oz;
                        base_ = bq.max.y - 0.45f * bq.size.y; // si incastra a meta' nel pezzo sotto: il cumulo resta basso e compatto
                    }
                }
            }
            pezzo.transform.position += Vector3.up * (base_ - b.min.y);
        }

        static Bounds BoundsDi(GameObject go)
        {
            bool any = false;
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
