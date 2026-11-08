using System.Collections.Generic;
using TrenoBambini.Core;
using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>
    /// R007 tappa 4: meteo della scena, guidato dal campo "meteo" di luoghi.json ("pioggia" = pioggia costante, "pioggia_fuori" = si sente da fuori e piove
    /// solo nel cortile a cielo aperto, "sereno" = niente). Particelle leggere scalate per preset (Bassa poche e senza spruzzi), spruzzi a terra con
    /// raycast (nessuna collisione delle particelle), gocce dai fili del bucato, superfici bagnate (basoli bagnati) e pozzanghere con riflessi economici.
    /// Il componente sta in scena (aggiunto dal generatore) e riceve i materiali per riferimento: niente Resources, niente Shader.Find.
    /// </summary>
    public class MeteoController : MonoBehaviour
    {
        public Material materialePioggia;
        public Material materialeSpruzzo;
        public Material materialePozzanghera;
        public Material suoloBagnato;          // sostituisce M_BasoliLavica nelle scene con pioggia
        public string suoloOriginale = "M_BasoliLavica";
        [Tooltip("Solo per meteo pioggia_fuori: z oltre la quale si e' nel cortile scoperto")]
        public float cortileDaZ = 9.3f;
        [Tooltip("Solo pioggia_fuori: centro (x,z) e semi-dimensioni del cortile scoperto: la pioggia cade solo li' (box fisso), mai dentro le stanze")]
        public Vector2 centroCortile = new Vector2(3.7f, 11.8f);
        public Vector2 mezzoCortile = new Vector2(3.0f, 2.8f);
        bool anteprima;
        readonly List<KeyValuePair<MeshRenderer, Material[]>> originaliAnteprima = new List<KeyValuePair<MeshRenderer, Material[]>>();
        readonly List<GameObject> creatiAnteprima = new List<GameObject>();

        string meteo = "sereno";
        Transform cam;
        ParticleSystem pioggia, spruzzi;
        readonly List<ParticleSystem> gocce = new List<ParticleSystem>();
        int versione = -1;           // R008: ultima versione delle impostazioni grafiche applicata
        float kPioggia = 1f;
        bool spruzziOn, bagnatoOn = true;
        float resto;
        bool pozzanghereFatte;
        GameObject padrePozzanghere;
        readonly List<KeyValuePair<MeshRenderer, Material[]>> suoliBagnati = new List<KeyValuePair<MeshRenderer, Material[]>>(); // (renderer, materiali asciutti)
        readonly List<Material[]> suoliBagnatiMat = new List<Material[]>();                                                      // materiali bagnati, stesso ordine

        static string MeteoDi(string scena)
        {
            var l = GameData.Luoghi;
            for (int i = 0; i < l.Count; i++)
                if (l[i].scena == scena && !string.IsNullOrEmpty(l[i].meteo)) return l[i].meteo;
            return "sereno";
        }

        void Start()
        {
            meteo = MeteoDi(gameObject.scene.name);
            if (meteo == "sereno") { enabled = false; return; }
            if (meteo == "pioggia") { BagnaSuolo(); Pozzanghere(); Gocce(); }
            CreaPioggia();
        }

        /// <summary>R023: spegne la pioggia (particelle, spruzzi, gocce) e rimette il suolo asciutto; serve allo sfondo del menu iniziale.</summary>
        public void Sereno()
        {
            enabled = false;
            if (pioggia != null) pioggia.gameObject.SetActive(false);
            if (spruzzi != null) spruzzi.gameObject.SetActive(false);
            foreach (var d in gocce) if (d != null) d.gameObject.SetActive(false);
            ImpostaBagnato(false);
        }

        /// <summary>Solo per le foto di documentazione nell'editor (fuori da Play): costruisce la pioggia, la fa cadere per 1,5 s e posa spruzzi attorno alla camera.</summary>
        public void Anteprima(Vector3 camPos)
        {
            meteo = MeteoDi(gameObject.scene.name);
            if (meteo == "sereno") return;
            anteprima = !Application.isPlaying;
            if (pioggia == null)
            {
                if (meteo == "pioggia") { BagnaSuolo(); Pozzanghere(); Gocce(); }
                CreaPioggia();
            }
            Applica();
            if (meteo == "pioggia_fuori") pioggia.transform.position = new Vector3(centroCortile.x, 8f, centroCortile.y);
            else pioggia.transform.position = camPos + Vector3.up * 9f;
            bool attiva = meteo == "pioggia" || ((camPos.x - centroCortile.x) * (camPos.x - centroCortile.x) + (camPos.z - centroCortile.y) * (camPos.z - centroCortile.y) < 144f);
            var em = pioggia.emission; em.enabled = attiva;
            if (!attiva) return;
            pioggia.Simulate(1.5f, true, true);
            if (!spruzziOn) return;
            Physics.SyncTransforms();
            var ep = new ParticleSystem.EmitParams();
            int n = kPioggia < 0.5f ? 40 : 90;
            for (int i = 0; i < n; i++)
            {
                var o = camPos + new Vector3(Random.Range(-8f, 8f), 4f, Random.Range(-2f, 12f));
                RaycastHit h;
                if (Physics.Raycast(o, Vector3.down, out h, 14f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.7f)
                { ep.position = h.point + Vector3.up * 0.02f; ep.startLifetime = Random.Range(0.1f, 0.22f); spruzzi.Emit(ep, 1); }
            }
            spruzzi.Simulate(0.05f, true, false);
            foreach (var d in gocce) if (d != null) d.Simulate(1.2f, true, true);
        }

        /// <summary>Anteprima nell'editor: gli oggetti creati non vengono salvati nella scena (HideFlags.DontSave) e si rimuovono con RipristinaAnteprima.</summary>
        void Proteggi(GameObject g)
        {
            if (!anteprima) return;
            g.hideFlags = HideFlags.DontSave;
            creatiAnteprima.Add(g);
        }

        /// <summary>Annulla l'anteprima: rimette i materiali originali del suolo e distrugge particelle, pozzanghere e gocce create.</summary>
        public void RipristinaAnteprima()
        {
            foreach (var kv in originaliAnteprima) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            originaliAnteprima.Clear();
            foreach (var g in creatiAnteprima) if (g != null) DestroyImmediate(g);
            creatiAnteprima.Clear();
            pioggia = null; spruzzi = null; gocce.Clear(); pozzanghereFatte = false;
            suoliBagnati.Clear(); suoliBagnatiMat.Clear(); padrePozzanghere = null;
        }

        // ------------------------------------------------------------------ particelle

        void CreaPioggia()
        {
            var go = new GameObject("Pioggia");
            Proteggi(go);
            go.transform.SetParent(transform, false);
            pioggia = go.AddComponent<ParticleSystem>();
            pioggia.Stop();
            var main = pioggia.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.8f; main.startSpeed = 17f;
            main.startSize = 0.012f; main.maxParticles = 3000;
            main.startColor = new Color(0.82f, 0.88f, 0.95f, 0.30f);
            var sh = pioggia.shape;
            sh.shapeType = ParticleSystemShapeType.Box; sh.rotation = new Vector3(90f, 0f, 0f);
            sh.scale = meteo == "pioggia_fuori" ? new Vector3(mezzoCortile.x * 2f, 0.1f, mezzoCortile.y * 2f) : new Vector3(22f, 0.1f, 22f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2f; r.velocityScale = 0.035f;
            r.sharedMaterial = materialePioggia; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;

            var sg = new GameObject("Spruzzi");
            Proteggi(sg);
            sg.transform.SetParent(transform, false);
            spruzzi = sg.AddComponent<ParticleSystem>();
            spruzzi.Stop();
            var sm = spruzzi.main;
            sm.loop = false; sm.playOnAwake = false; sm.simulationSpace = ParticleSystemSimulationSpace.World;
            sm.startLifetime = 0.22f; sm.startSpeed = 0f; sm.startSize = 0.09f; sm.maxParticles = 600;
            sm.startColor = new Color(0.85f, 0.9f, 0.95f, 0.35f);
            var em = spruzzi.emission; em.enabled = false;
            var ssh = spruzzi.shape; ssh.enabled = false;
            var sz = spruzzi.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1.6f)));
            var col = spruzzi.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sr = sg.GetComponent<ParticleSystemRenderer>();
            sr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; sr.sharedMaterial = materialeSpruzzo;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
            pioggia.Play();
            Applica();
        }

        // R008: tutto guidato dalle voci Pioggia, Schizzi e gocce, Selciato bagnato (menu Impostazioni); i preset le riscrivono
        void Applica()
        {
            versione = GraphicsPresets.Version;
            kPioggia = GraphicsOptions.Pioggia[Mathf.Clamp(GraphicsPresets.Get(Voce.Pioggia), 0, GraphicsOptions.Pioggia.Length - 1)];
            spruzziOn = GraphicsPresets.Get(Voce.Spruzzi) != 0 && kPioggia > 0f;
            bagnatoOn = GraphicsPresets.Get(Voce.Bagnato) != 0;
            float k = kPioggia;
            if (meteo == "pioggia_fuori") k *= 0.25f; // il cortile e' piccolo: poche gocce
            var em = pioggia.emission; em.rateOverTime = 2200f * k; em.enabled = k > 0f;
            foreach (var d in gocce) if (d != null) { var e = d.emission; e.enabled = spruzziOn; }
            ImpostaBagnato(bagnatoOn);
        }

        /// <summary>Basoli bagnati e pozzanghere accesi/spenti al volo.</summary>
        void ImpostaBagnato(bool on)
        {
            if (padrePozzanghere != null && padrePozzanghere.activeSelf != on) padrePozzanghere.SetActive(on);
            for (int i = 0; i < suoliBagnati.Count; i++)
            {
                var r = suoliBagnati[i].Key;
                if (r == null) continue;
                var voluti = on ? suoliBagnatiMat[i] : suoliBagnati[i].Value;
                if (r.sharedMaterials.Length == voluti.Length && ReferenceEquals(r.sharedMaterials[0], voluti[0])) continue;
                r.sharedMaterials = voluti;
            }
        }

        void LateUpdate()
        {
            if (pioggia == null) return;
            if (versione != GraphicsPresets.Version) Applica();
            var cp = Camera.main;                     // ricontrollata a ogni frame: puo' cambiare (cutscene, benchmark, camera disattivata)
            if (cp != null) cam = cp.transform;
            if (cam == null) return;
            bool fuori = meteo == "pioggia_fuori";
            pioggia.transform.position = fuori ? new Vector3(centroCortile.x, 8f, centroCortile.y) : cam.position + Vector3.up * 9f;
            float dx = cam.position.x - centroCortile.x, dz = cam.position.z - centroCortile.y;
            bool attiva = !fuori || dx * dx + dz * dz < 144f; // pioggia_fuori: solo se il giocatore e' entro 12 m dal cortile
            var em = pioggia.emission;
            if (em.enabled != attiva) em.enabled = attiva;
            if (!attiva || !spruzziOn) return;

            // spruzzi a terra: raycast verso il basso attorno al giocatore (30/60/90 al secondo, per intensita' della pioggia)
            float rate = kPioggia < 0.5f ? 30f : kPioggia < 0.9f ? 60f : 90f;
            if (meteo == "pioggia_fuori") rate *= 0.5f;
            resto += rate * Time.deltaTime;
            int n = (int)resto; resto -= n;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                var o = fuori ? new Vector3(centroCortile.x + Random.Range(-mezzoCortile.x, mezzoCortile.x), 6f, centroCortile.y + Random.Range(-mezzoCortile.y, mezzoCortile.y))
                              : cam.position + new Vector3(Random.Range(-9f, 9f), 4f, Random.Range(-9f, 9f));
                RaycastHit h;
                if (Physics.Raycast(o, Vector3.down, out h, 14f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.7f)
                {
                    ep.position = h.point + Vector3.up * 0.02f;
                    spruzzi.Emit(ep, 1);
                }
            }
        }

        // ------------------------------------------------------------------ superfici bagnate, pozzanghere, gocce

        void BagnaSuolo()
        {
            if (suoloBagnato == null) return;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                var sm = r.sharedMaterials;
                bool cambia = false;
                for (int i = 0; i < sm.Length; i++)
                    if (sm[i] != null && sm[i].name == suoloOriginale) { sm[i] = suoloBagnato; cambia = true; }
                if (cambia)
                {
                    var asciutti = r.sharedMaterials;
                    if (anteprima) originaliAnteprima.Add(new KeyValuePair<MeshRenderer, Material[]>(r, asciutti));
                    suoliBagnati.Add(new KeyValuePair<MeshRenderer, Material[]>(r, asciutti));
                    suoliBagnatiMat.Add(sm);
                    r.sharedMaterials = sm;
                }
            }
        }

        /// <summary>Pozzanghere: dischi appena sopra il selciato con materiale molto liscio (riflettono cielo e reflection probe), in punti casuali ma fissi (seme costante).</summary>
        void Pozzanghere()
        {
            if (pozzanghereFatte || materialePozzanghera == null) return;
            pozzanghereFatte = true;
            var rnd = new System.Random(1946);
            var padreGo = new GameObject("Pozzanghere");
            Proteggi(padreGo);
            padrePozzanghere = padreGo;
            var padre = padreGo.transform;
            padre.SetParent(transform, false);
            var mesh = DiscoMesh(18);
            int fatte = 0;
            for (int t = 0; t < 600 && fatte < 45; t++)
            {
                var o = new Vector3((float)(rnd.NextDouble() * 6.0 - 3.0), 80f, (float)(rnd.NextDouble() * 96.0 - 44.0));
                RaycastHit h;
                if (!Physics.Raycast(o, Vector3.down, out h, 120f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (h.normal.y < 0.985f || !h.collider.name.StartsWith("Selciato")) continue;
                var p = new GameObject("Pozzanghera");
                p.transform.SetParent(padre, false);
                p.transform.position = h.point + Vector3.up * 0.006f;
                float s = 0.5f + (float)rnd.NextDouble() * 1.1f;
                p.transform.localScale = new Vector3(s * (1f + (float)rnd.NextDouble() * 0.6f), 1f, s);
                p.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                p.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = materialePozzanghera;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fatte++;
            }
        }

        static Mesh DiscoMesh(int seg)
        {
            var v = new Vector3[seg + 1]; var uv = new Vector2[seg + 1]; var tri = new int[seg * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float rr = 0.5f * (1f + 0.12f * Mathf.Sin(a * 3f + 1f));
                v[i + 1] = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
                uv[i + 1] = new Vector2(v[i + 1].x + 0.5f, v[i + 1].z + 0.5f);
                tri[i * 3] = 0; tri[i * 3 + 1] = (i + 1) % seg + 1; tri[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { name = "DiscoPozzanghera", vertices = v, uv = uv, triangles = tri };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Gocce dai fili del bucato (fino a 12): piccoli emettitori che gocciolano, solo da Media in su.</summary>
        void Gocce()
        {
            int n = 0;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t.name != "Filo") continue;
                if (((int)(t.position.x * 7f) + (int)(t.position.z * 3f)) % 3 != 0) continue;
                var go = new GameObject("Gocciolio");
                Proteggi(go);
                go.transform.SetParent(t, false);
                go.transform.localPosition = new Vector3(Random.Range(-0.4f, 0.4f) * t.lossyScale.x, -0.05f, 0f);
                go.transform.rotation = Quaternion.identity;
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop();
                var main = ps.main;
                main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = 1.4f; main.startSpeed = 0f; main.gravityModifier = 1f; main.startSize = 0.02f; main.maxParticles = 12;
                main.startColor = new Color(0.8f, 0.88f, 0.95f, 0.5f);
                var e = ps.emission; e.rateOverTime = 2.5f;
                var sh = ps.shape; sh.enabled = false;
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2f; r.velocityScale = 0.05f; r.sharedMaterial = materialePioggia;
                ps.Play();
                gocce.Add(ps);
                if (++n >= 12) break;
            }
        }
    }
}
