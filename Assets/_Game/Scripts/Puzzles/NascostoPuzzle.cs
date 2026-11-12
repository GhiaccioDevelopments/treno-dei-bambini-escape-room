using System;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using TrenoBambini.World;
using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>Oggetto nascosto nella stanza: discreto, senza glow. Interagendo viene raccolto.</summary>
    public class HiddenItem : Interactable
    {
        public NascostoDef item;
        public NascostoPuzzle owner;

        public override void Interact()
        {
            if (owner != null) owner.Found(this);
        }
    }

    /// <summary>Respiro lentissimo della luce che sfiora una calzatura da trovare (nessun alone, solo luce vera sul selciato).</summary>
    public class LuceCalzatura : MonoBehaviour
    {
        public Light luce;
        float fase;
        void Start() { fase = UnityEngine.Random.value * 6.28f; }
        void Update()
        {
            if (luce != null) luce.intensity = 7.4f + 1.2f * Mathf.Sin(Time.time * 1.1f + fase);
        }
    }

    /// <summary>
    /// R013 ("Il volantino del treno"): ascolto nello spazio. Una voce del vicolo non e' un oggetto: e' un punto a terra sotto un balcone, una finestra o
    /// accanto al lavatoio. Avvicinandosi (o premendo E) la voce si "sente": il sottotitolo (nome: descrizione) compare come per un ritrovamento e la
    /// diceria entra nel quaderno. Una luce calda e tremolante sulla finestra da cui viene la voce la fa notare; da lontano si sente un mormorio (evento audio "voce_vicolo").
    /// </summary>
    public class VoceVicolo : MonoBehaviour
    {
        public HiddenItem item;
        public Light luce;
        public Vector3 fonte;
        public float raggio = 2.0f;
        Transform player;
        float fase, prossimoMormorio;

        public static VoceVicolo Crea(NascostoDef n, NascostoPuzzle owner, Transform parent)
        {
            var go = new GameObject("Voce_" + n.id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(n.x, n.y, n.z);
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true; // il punto dove ci si ferma ad ascoltare: non blocca il player
            bc.center = new Vector3(0f, 0.9f, 0f); bc.size = new Vector3(1.6f, 1.8f, 1.6f);
            var v = go.AddComponent<VoceVicolo>();
            v.fonte = new Vector3(n.fx, n.fy, n.fz);
            if (v.fonte == Vector3.zero) v.fonte = go.transform.position + Vector3.up * 3f;
            var lg = new GameObject("LuceVoce");
            lg.transform.SetParent(go.transform, false);
            lg.transform.position = v.fonte;
            v.luce = lg.AddComponent<Light>();
            v.luce.type = LightType.Point; v.luce.range = 4.5f; v.luce.intensity = 2.2f; v.luce.shadows = LightShadows.None;
            v.luce.color = new Color(1f, 0.78f, 0.5f); // lume di una finestra a sera
            var h = go.AddComponent<HiddenItem>();
            h.item = n;
            h.owner = owner;
            h.prompt = !string.IsNullOrEmpty(n.prompt) ? n.prompt : n.nome;
            v.item = h;
            return v;
        }

        void Start() { fase = UnityEngine.Random.value * 6.28f; prossimoMormorio = Time.time + UnityEngine.Random.value * 3f; }

        void Update()
        {
            if (luce != null) luce.intensity = 2.2f + 0.5f * Mathf.Sin(Time.time * 1.7f + fase) + 0.2f * Mathf.Sin(Time.time * 5.3f + fase * 2f);
            if (item == null) return;
            if (player == null)
            {
                var p = FindAnyObjectByType<TrenoBambini.Player.ThirdPersonController>();
                if (p == null) return;
                player = p.transform;
            }
            Vector3 d = player.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < 10f && Time.time > prossimoMormorio)
            {
                AudioManager.FireAt("voce_vicolo", fonte);
                prossimoMormorio = Time.time + 6f + UnityEngine.Random.value * 5f;
            }
            // ci si ferma sotto la finestra: la voce si sente da sola (non durante dialoghi, viste ravvicinate o cutscene)
            if (dist < raggio && GameManager.I != null && !GameManager.I.InputBlocked) item.Interact();
        }
    }

    /// <summary>Enigma "nascosto": spawna gli oggetti da trovare, conta i ritrovamenti, mostra la descrizione di ciascuno.</summary>
    public class NascostoPuzzle : MonoBehaviour
    {
        EnigmaDef def;
        Action onSolved;
        Action<string> onProgress;
        int found, total, hintIndex;
        bool finished;

        public static NascostoPuzzle Begin(EnigmaDef def, Transform parent, Action<string> progress, Action solved)
        {
            var go = new GameObject("Nascosto_" + def.id);
            if (parent != null) go.transform.SetParent(parent, false);
            var p = go.AddComponent<NascostoPuzzle>();
            p.Setup(def, progress, solved);
            return p;
        }

        void Setup(EnigmaDef d, Action<string> progress, Action solved)
        {
            def = d;
            onProgress = progress;
            onSolved = solved;
            total = d.oggetti != null ? d.oggetti.Count : 0;

            if (total == 0)
            {
                Debug.LogWarning("Enigma 'nascosto' senza oggetti: " + d.id);
                Complete();
                return;
            }

            for (int i = 0; i < total; i++)
            {
                var n = d.oggetti[i];
                if (MondoStato.Ha(n.id)) { found++; continue; } // gia' nella borsa (partita ripresa)
                // R024: modello vero (calz_<stile>), un po' piu' grande del vero per riconoscerlo da lontano; il fascio di luce lo fa notare senza aloni colorati
                GameObject go;
                bool straccio = !string.IsNullOrEmpty(n.modello) && n.modello.StartsWith("straccio");
                if (n.modello == "voce")
                {
                    VoceVicolo.Crea(n, this, transform); // R013: una voce del vicolo (si ascolta avvicinandosi, niente da raccogliere)
                    continue;
                }
                if (straccio)
                {
                    // R012: pezzo di stoffa gettato a terra (due falde sovrapposte, colore del tessuto), piu' grande del vero per riconoscerlo da lontano
                    go = new GameObject("Nascosto_" + n.id);
                    go.transform.SetParent(transform, false);
                    go.transform.position = new Vector3(n.x, n.y, n.z);
                    go.transform.rotation = Quaternion.Euler(0f, 37f * i + 10f, 0f);
                    MondoFab.Pezzo(new ParteDef { id = "falda_a", forma = "cubo", materiale = "stoffa", colore = n.colore, x = 0f, y = 0.015f, z = 0f, ry = 0f, sx = 0.5f, sy = 0.03f, sz = 0.38f }, go.transform, false);
                    MondoFab.Pezzo(new ParteDef { id = "falda_b", forma = "cubo", materiale = "stoffa", colore = n.colore, x = 0.06f, y = 0.04f, z = 0.03f, ry = 24f, rz = 4f, sx = 0.36f, sy = 0.03f, sz = 0.3f }, go.transform, false);
                }
                else
                {
                    go = MondoFab.Scarpa(new ParteDef { id = "Nascosto_" + n.id, testo = n.modello, x = n.x, y = n.y, z = n.z, rx = n.rx, ry = 20f * i + 15f, colore = n.colore, sx = 1.4f, sy = 1.4f, sz = 1.4f }, transform, false);
                    go.transform.position = new Vector3(n.x, n.y, n.z);
                }
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true; // non blocca il player
                bc.center = new Vector3(0f, 0.06f, 0f); bc.size = straccio ? new Vector3(0.6f, 0.25f, 0.5f) : new Vector3(0.5f, 0.25f, 0.35f);
                var lg = new GameObject("Luce");
                lg.transform.SetParent(go.transform, false);
                lg.transform.position = go.transform.position + new Vector3(0.15f, 1.1f, -0.4f);
                var luce = lg.AddComponent<Light>();
                luce.type = LightType.Point; luce.range = 2.8f; luce.intensity = 8f; luce.shadows = LightShadows.None;
                luce.color = new Color(1f, 0.86f, 0.62f); // calda, come un raggio di sole tra i palazzi
                lg.AddComponent<LuceCalzatura>().luce = luce;
                var h = go.AddComponent<HiddenItem>();
                h.item = n;
                h.owner = this;
                h.prompt = !string.IsNullOrEmpty(n.prompt) ? n.prompt : "Raccogli: " + n.nome;
            }

            if (found >= total) { Complete(); return; }
            if (UIRoot.I != null) UIRoot.I.ShowMessage(d.titolo, d.consegna, null);
            Report();
        }

        void Report()
        {
            if (onProgress != null) onProgress("Trovati: " + found + "/" + total);
        }

        public void Found(HiddenItem h)
        {
            if (finished || h == null) return;
            if (h.item != null && MondoStato.Ha(h.item.id)) return; // R013: gia' trovato nello stesso fotogramma (voce sentita avvicinandosi e con E insieme)
            if (UIRoot.I != null && h.item != null)
                UIRoot.I.Toast(h.item.nome + ": " + h.item.descrizione, 4.5f);
            // scintille dorate dove l'oggetto è stato trovato
            UISfx.Play(Sfx.Sparkle, 0.8f);
            AudioManager.FireAt("trovato", h.transform.position);
            if (Camera.main != null)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(h.transform.position);
                if (sp.z > 0f) UIParticles.BurstAtScreen(ParticleKind.Spark, sp, 10);
            }
            MondoStato.Dai(h.item.id);
            if (GameManager.I != null) GameManager.I.SaveGame();
            Destroy(h.gameObject);
            found++;
            if (found >= total) Complete();
            else Report();
        }

        /// <summary>Riletta della consegna con un indizio (richiamata dalla postazione).</summary>
        public void ShowHint()
        {
            TrenoBambini.Mappa.Obiettivi.RivelaIndizio(); // R019
            if (UIRoot.I == null) return;
            string t = def.consegna ?? "";
            if (def.indizi != null && def.indizi.Count > 0)
            {
                int i = hintIndex % def.indizi.Count;
                hintIndex++;
                t += "\n\nIndizio " + (i + 1) + "/" + def.indizi.Count + ": " + def.indizi[i];
            }
            UIRoot.I.ShowMessage(def.titolo, t, null);
        }

        void Complete()
        {
            if (finished) return;
            finished = true;
            AudioManager.Fire("enigma_risolto");
            if (onProgress != null) onProgress("");
            string t = def.spiegazione ?? "";
            if (!string.IsNullOrEmpty(def.temaCivico)) t += (t.Length > 0 ? "\n\n" : "") + "Educazione civica: " + def.temaCivico;
            if (t.Length == 0) t = "Hai trovato tutto!";
            if (!string.IsNullOrEmpty(def.completoMsg)) t += "\n\n" + def.completoMsg;
            var cb = onSolved;
            if (UIRoot.I != null) UIRoot.I.ShowMessage(def.titolo, t, () => { if (cb != null) cb(); });
            else if (cb != null) cb();
            Destroy(gameObject, 0.1f);
        }
    }
}
