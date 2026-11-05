using System;
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
                var go = PrimitiveFactory.Create(n.modello, "Nascosto_" + n.id, new Vector3(n.x, n.y, n.z), 0.28f,
                    new Color(0.55f, 0.38f, 0.24f), transform);
                var col = go.GetComponent<Collider>();
                if (col != null) col.isTrigger = true; // non blocca il player
                var h = go.AddComponent<HiddenItem>();
                h.item = n;
                h.owner = this;
                h.prompt = "Raccogli: " + n.nome;
            }

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
            if (Camera.main != null)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(h.transform.position);
                if (sp.z > 0f) UIParticles.BurstAtScreen(ParticleKind.Spark, sp, 10);
            }
            Destroy(h.gameObject);
            found++;
            if (found >= total) Complete();
            else Report();
        }

        /// <summary>Riletta della consegna con un indizio (richiamata dalla postazione).</summary>
        public void ShowHint()
        {
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
            if (onProgress != null) onProgress("");
            string t = def.spiegazione ?? "";
            if (!string.IsNullOrEmpty(def.temaCivico)) t += (t.Length > 0 ? "\n\n" : "") + "Educazione civica: " + def.temaCivico;
            if (t.Length == 0) t = "Hai trovato tutto!";
            var cb = onSolved;
            if (UIRoot.I != null) UIRoot.I.ShowMessage(def.titolo, t, () => { if (cb != null) cb(); });
            else if (cb != null) cb();
            Destroy(gameObject, 0.1f);
        }
    }
}
