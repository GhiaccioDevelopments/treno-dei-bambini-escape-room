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
                var go = MondoFab.Scarpa(new ParteDef { id = "Nascosto_" + n.id, testo = n.modello, x = n.x, y = n.y, z = n.z, rx = n.rx, ry = 20f * i + 15f, colore = n.colore, sx = 1.4f, sy = 1.4f, sz = 1.4f }, transform, false);
                go.transform.position = new Vector3(n.x, n.y, n.z);
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true; // non blocca il player
                bc.center = new Vector3(0f, 0.06f, 0f); bc.size = new Vector3(0.5f, 0.25f, 0.35f);
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
                h.prompt = "Raccogli: " + n.nome;
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
