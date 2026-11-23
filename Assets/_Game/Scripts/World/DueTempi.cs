using System;
using System.Collections;
using System.Collections.Generic;
using TrenoBambini.Puzzles;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>
    /// R016: la stanza a due tempi del finale (80_Basso_1994). Ogni angolo ha una versione 1946 (radice spenta: lume a petrolio, una figura di spalle,
    /// luce calda dinamica e locale, nessun nuovo bake). Quando l'enigma dell'angolo viene risolto (chiave in MondoStato) e la vista ravvicinata e' chiusa,
    /// l'angolo torna al 1946 per qualche secondo mentre la lampadina del 1994 si abbassa, poi sfuma di nuovo nel presente.
    /// chiaveTutti: quando quell'enigma e' risolto (la custodia sotto il letto) tutta la stanza torna al 1946 per un attimo.
    /// Gli angoli gia' risolti all'avvio (salvataggio) non si ripetono. Pubblico Ricordati() per la prova.
    /// </summary>
    public class DueTempi : MonoBehaviour
    {
        [Serializable]
        public class Angolo
        {
            public string chiave;
            public GameObject radice;
            public Light luce;
        }

        public List<Angolo> angoli = new List<Angolo>();
        public string chiaveTutti;
        public Light luce1994;
        public float durata = 6f, intensitaRicordo = 3f;

        readonly HashSet<string> visti = new HashSet<string>();
        readonly Queue<List<Angolo>> coda = new Queue<List<Angolo>>();
        bool inCorso;
        float luce1994Base;

        /// <summary>Quanti passaggi al 1946 sono partiti in questa scena (per la prova).</summary>
        public int Ricordati { get; private set; }

        void Start()
        {
            luce1994Base = luce1994 != null ? luce1994.intensity : 1f;
            foreach (var a in angoli)
            {
                if (a.radice != null) a.radice.SetActive(false);
                if (MondoStato.Risolto(a.chiave)) visti.Add(a.chiave);
            }
            if (!string.IsNullOrEmpty(chiaveTutti) && MondoStato.Risolto(chiaveTutti)) visti.Add(chiaveTutti);
        }

        void Update()
        {
            foreach (var a in angoli)
                if (!visti.Contains(a.chiave) && MondoStato.Risolto(a.chiave)) { visti.Add(a.chiave); coda.Enqueue(new List<Angolo> { a }); }
            if (!string.IsNullOrEmpty(chiaveTutti) && !visti.Contains(chiaveTutti) && MondoStato.Risolto(chiaveTutti))
            {
                visti.Add(chiaveTutti);
                coda.Enqueue(new List<Angolo>(angoli));
            }
            if (!inCorso && coda.Count > 0 && EsaminaView.Current == null) StartCoroutine(Ricorda(coda.Dequeue()));
        }

        IEnumerator Ricorda(List<Angolo> lista)
        {
            inCorso = true;
            Ricordati++;
            foreach (var a in lista)
            {
                if (a.radice != null) a.radice.SetActive(true);
                if (a.luce != null) a.luce.intensity = 0f;
            }
            yield return Sfuma(lista, 0f, 1f, 1.2f);
            float t = 0f;
            while (t < durata) { t += Time.deltaTime; yield return null; }
            yield return Sfuma(lista, 1f, 0f, 1.6f);
            foreach (var a in lista) if (a.radice != null) a.radice.SetActive(false);
            inCorso = false;
        }

        IEnumerator Sfuma(List<Angolo> lista, float da, float a, float dur)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                float k = Mathf.SmoothStep(da, a, t);
                foreach (var x in lista) if (x.luce != null) x.luce.intensity = intensitaRicordo * k;
                if (luce1994 != null) luce1994.intensity = luce1994Base * Mathf.Lerp(1f, 0.35f, k);
                yield return null;
            }
            foreach (var x in lista) if (x.luce != null) x.luce.intensity = intensitaRicordo * a;
            if (luce1994 != null) luce1994.intensity = luce1994Base * Mathf.Lerp(1f, 0.35f, a);
        }
    }
}
