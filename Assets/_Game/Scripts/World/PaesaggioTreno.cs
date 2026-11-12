using System.Collections.Generic;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>
    /// R014: il paesaggio che scorre fuori dai finestrini della carrozza di terza classe (61_FB_Vagone). La carrozza sta ferma; fuori, su tutti e due i lati,
    /// scorrono strati a velocita' diverse (pali del telegrafo vicini e veloci, alberi, colline lontane e lente) fatti di primitive create a runtime
    /// (nessun asset, nessun testo). La luce del sole passa dal pomeriggio alla notte quando il viaggio e' quasi finito (gettone "quasi_arrivati", enigma del finestrino).
    /// Messo nella scena da SceneBuilderCappotto (Treno > Scene > Costruisci ricordo Cappotto).
    /// </summary>
    public class PaesaggioTreno : MonoBehaviour
    {
        public float zMin = -40f, zMax = 60f;   // tratto lungo il quale gli oggetti scorrono (verso -z: il treno va verso +z, cioe' verso nord)
        public float xCarrozza = 1.5f;          // meta' larghezza esterna della carrozza: gli strati stanno oltre
        public float quotaSuolo = -1.1f;        // quota della campagna (la carrozza e' alta sui binari)
        public float velocita = 11f;            // m/s dei pali (gli strati lontani sono piu' lenti)
        public string gettoneNotte = "quasi_arrivati";
        public Color luceGiorno = new Color(1f, 0.86f, 0.66f), luceNotte = new Color(0.42f, 0.5f, 0.72f);
        public float intensitaGiorno = 1.6f, intensitaNotte = 0.25f;

        sealed class Strato { public Transform t; public float v; }
        readonly List<Strato> strati = new List<Strato>();
        Light sole;
        float notte;

        void Start()
        {
            var rnd = new System.Random(1947);
            float lung = zMax - zMin;
            for (int lato = -1; lato <= 1; lato += 2)
            {
                // campagna (ferma): una striscia lunga di terra arata e prato
                Pezzo("Campagna", "cubo", "stoffa", "#5d5a3c", new Vector3(lato * (xCarrozza + 40f), quotaSuolo - 0.1f, (zMin + zMax) * 0.5f), new Vector3(80f, 0.2f, lung + 40f), 0f);
                Pezzo("Massicciata", "cubo", "pietra", "#5a5650", new Vector3(lato * (xCarrozza + 1.2f), quotaSuolo + 0.02f, (zMin + zMax) * 0.5f), new Vector3(2.4f, 0.12f, lung + 40f), 0f);
                // pali del telegrafo, ogni 25 m, vicini e veloci
                for (float z = zMin; z < zMax; z += 25f)
                {
                    var p = Pezzo("Palo", "cilindro", "legno", "#3a2c20", new Vector3(lato * (xCarrozza + 3.2f), quotaSuolo + 3.2f, z), new Vector3(0.16f, 3.2f, 0.16f), velocita);
                    Pezzo("Traversa", "cubo", "legno", "#3a2c20", new Vector3(0f, 0.92f, 0f), new Vector3(6f, 0.03f, 0.6f), -1f, p);
                }
                // alberi (filari di pioppi e gelsi): a 7-16 m, meno veloci
                for (int i = 0; i < 14; i++)
                {
                    float z = zMin + (float)rnd.NextDouble() * lung;
                    float x = lato * (xCarrozza + 7f + (float)rnd.NextDouble() * 9f);
                    float h = 3f + (float)rnd.NextDouble() * 4f;
                    var tronco = Pezzo("Albero", "cilindro", "legno", "#3b2f22", new Vector3(x, quotaSuolo + h * 0.5f, z), new Vector3(0.3f, h * 0.5f, 0.3f), velocita * 0.55f);
                    Pezzo("Chioma", "sfera", "stoffa", i % 3 == 0 ? "#44502f" : "#56603a", new Vector3(0f, 0.95f, 0f), new Vector3(9f, 0.7f, 9f), -1f, tronco);
                }
                // colline e casolari lontani: lentissimi
                for (int i = 0; i < 6; i++)
                {
                    float z = zMin + i * lung / 6f + (float)rnd.NextDouble() * 6f;
                    float x = lato * (xCarrozza + 45f + (float)rnd.NextDouble() * 20f);
                    Pezzo("Collina", "sfera", "stoffa", i % 2 == 0 ? "#545a40" : "#4b5239", new Vector3(x, quotaSuolo, z), new Vector3(40f, 14f + (float)rnd.NextDouble() * 10f, 30f), velocita * 0.07f);
                    if (i % 2 == 1)
                        Pezzo("Casolare", "cubo", "pietra", "#8a7a62", new Vector3(lato * (xCarrozza + 24f), quotaSuolo + 2.5f, z + 9f), new Vector3(7f, 5f, 5f), velocita * 0.2f);
                }
            }
            sole = RenderSettings.sun;
            notte = Notte() ? 1f : 0f;
            Luce();
        }

        /// <summary>Un pezzo di primitive (senza collider: fuori dai finestrini non si cammina). v &lt; 0 = figlio di 'padre', non scorre da solo.</summary>
        Transform Pezzo(string nome, string forma, string mat, string col, Vector3 p, Vector3 s, float v, Transform padre = null)
        {
            var d = new ParteDef { id = nome, forma = forma, materiale = mat, colore = col, x = p.x, y = p.y, z = p.z, sx = s.x, sy = s.y, sz = s.z };
            var g = MondoFab.Pezzo(d, padre != null ? padre : transform, false);
            var r = g.GetComponent<Renderer>();
            if (r != null) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            if (v > 0f) strati.Add(new Strato { t = g.transform, v = v });
            return g.transform;
        }

        bool Notte() { return !string.IsNullOrEmpty(gettoneNotte) && MondoStato.Ha(gettoneNotte); }

        void Update()
        {
            float dt = Time.deltaTime;
            float lung = zMax - zMin;
            for (int i = 0; i < strati.Count; i++)
            {
                var s = strati[i];
                if (s.t == null) continue;
                var p = s.t.localPosition;
                p.z -= s.v * dt;
                if (p.z < zMin) p.z += lung;
                s.t.localPosition = p;
            }
            notte = Mathf.MoveTowards(notte, Notte() ? 1f : 0f, dt * 0.08f);
            Luce();
        }

        void Luce()
        {
            if (sole == null) return;
            sole.color = Color.Lerp(luceGiorno, luceNotte, notte);
            sole.intensity = Mathf.Lerp(intensitaGiorno, intensitaNotte, notte);
        }
    }
}
