using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TrenoBambini.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R011: strumenti dei segreti. I segreti si agganciano per NOME del punto nei rapporti R006 (Docs/richieste/media/R006/punti_segreti_*.md):
    /// "Verifica punti" controlla che le coordinate scritte in segreti.json siano ancora quelle del punto (altrimenti rilanciare
    /// ArtSource/Segreti/genera_segreti.py dopo aver rigenerato le scene). "Prova automatica" e "Foto di posizionamento" avviano la prova in Play.
    /// </summary>
    public static class SegretiTools
    {
        const string JsonPath = "Assets/_Game/Resources/Data/segreti.json";
        static readonly Dictionary<string, string> Rapporti = new Dictionary<string, string>
        {
            { "10_Hub_Vicolo", "Docs/richieste/media/R006/punti_segreti_hub.md" },
            { "20_Int_BassoAntonietta", "Docs/richieste/media/R006/punti_segreti_basso.md" },
            { "30_FB_Scarpe", "Docs/richieste/media/R006/punti_segreti_flashback.md" },
            { "40_FB_Stracci", "Docs/richieste/media/R012/punti_segreti_stracci.md" },
        };

        static Dictionary<string, Vector3> Punti(string scena)
        {
            var res = new Dictionary<string, Vector3>();
            var conta = new Dictionary<string, int>();
            string path = Path.GetFullPath(Rapporti[scena]);
            if (!File.Exists(path)) return res;
            var re = new Regex(@"^\|\s*(\d+)\s*\|\s*(.+?)\s*\|\s*(-?[\d,\.]+)\s*\|\s*(-?[\d,\.]+)\s*\|\s*(-?[\d,\.]+)\s*\|");
            foreach (var riga in File.ReadAllLines(path, Encoding.UTF8))
            {
                var m = re.Match(riga);
                if (!m.Success) continue;
                string nome = m.Groups[2].Value;
                int k; conta.TryGetValue(nome, out k); k++; conta[nome] = k;
                string chiave = k == 1 ? nome : nome + " #" + k;
                float F(string s) { return float.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture); }
                res[chiave] = new Vector3(F(m.Groups[3].Value), F(m.Groups[4].Value), F(m.Groups[5].Value));
            }
            return res;
        }

        [MenuItem("Treno/Segreti/Verifica punti")]
        public static void VerificaPunti()
        {
            var f = JsonUtility.FromJson<SegretiFile>(File.ReadAllText(JsonPath, Encoding.UTF8));
            var cache = new Dictionary<string, Dictionary<string, Vector3>>();
            int problemi = 0, controllati = 0;
            System.Action<string, string, string, Vector3, Vector3> ctrl = (tipo, id, scena, atteso, trovato) =>
            {
                controllati++;
                if ((atteso - trovato).sqrMagnitude > 0.0004f) { problemi++; Debug.LogWarning("[Segreti] " + tipo + " " + id + ": coordinate " + trovato.ToString("F2") + " ma il punto dice " + atteso.ToString("F2") + " (rilanciare genera_segreti.py)"); }
            };
            System.Func<string, string, Vector3?> punto = (scena, nome) =>
            {
                if (!cache.ContainsKey(scena)) cache[scena] = Punti(scena);
                Vector3 v; return cache[scena].TryGetValue(nome, out v) ? v : (Vector3?)null;
            };
            foreach (var p in f.pezzi)
            {
                var v = punto(p.scena, p.punto);
                if (v == null) { problemi++; Debug.LogWarning("[Segreti] pezzo " + p.id + ": punto '" + p.punto + "' non trovato nel rapporto di " + p.scena); continue; }
                ctrl("pezzo", p.id, p.scena, v.Value + new Vector3(p.dx, p.dy, p.dz), new Vector3(p.x, p.y, p.z));
            }
            foreach (var e in f.enigmi)
            {
                var v = punto(e.scena, e.punto);
                if (v == null) { problemi++; Debug.LogWarning("[Segreti] enigma " + e.id + ": punto '" + e.punto + "' non trovato"); continue; }
                ctrl("enigma", e.id, e.scena, v.Value + new Vector3(e.dx, e.dy, e.dz), new Vector3(e.x, e.y, e.z));
            }
            foreach (var c in f.campane)
            {
                var v = punto(c.scena, c.punto);
                if (v == null) { problemi++; Debug.LogWarning("[Segreti] campana " + c.id + ": punto non trovato"); continue; }
                ctrl("campana", c.id, c.scena, v.Value + new Vector3(c.dx, 0f, c.dz), new Vector3(c.x, c.y, c.z));
            }
            foreach (var u in f.uova)
            {
                var v = punto(u.scena, u.punto);
                if (v == null) { problemi++; Debug.LogWarning("[Segreti] uovo " + u.id + ": punto non trovato"); continue; }
                ctrl("uovo", u.id, u.scena, v.Value + new Vector3(u.dx, u.dy, u.dz), new Vector3(u.x, u.y, u.z));
            }
            Debug.Log("[Segreti] Verifica punti: " + controllati + " controllati, " + problemi + " problemi.");
        }

        static void AvviaProva(string chiave)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Segreti] Esci dal Play prima di avviare la prova."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(chiave, true);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/00_Boot.unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Treno/Segreti/Prova automatica (Play)")]
        public static void Prova() { AvviaProva("segretitest"); }

        [MenuItem("Treno/Segreti/Foto di posizionamento (Play)")]
        public static void Foto() { AvviaProva("segretifoto"); }
    }
}
