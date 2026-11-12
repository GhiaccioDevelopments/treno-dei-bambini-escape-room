using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TrenoBambini.Core;
using TrenoBambini.Player;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R018: prova automatica della camera. Menu: Treno/Verifica/Camera.
    /// Passo 1 (editor): per ogni scena di gioco calcola l'area calpestabile (come VerificaSuperfici) e ne estrae una griglia di punti
    /// (al massimo 'MaxPunti' per scena), scrive il lavoro in Library/R018_lavoro.json.
    /// Passo 2 (Play): CameraProva (Scripts/Player) teletrasporta il giocatore sui punti, ruota la camera di 360 gradi a 3 inclinazioni e
    /// scrive Docs/richieste/media/R018/rapporto_&lt;etichetta&gt;.txt. Al termine esce dal Play.
    /// In batch: TrenoBambini.EditorTools.VerificaCamera.Avvia("prima"|"dopo").
    /// </summary>
    public static class VerificaCamera
    {
        const string ScenesFolder = "Assets/_Game/Scenes";
        static readonly string[] Scene = { "10_Hub_Vicolo", "20_Int_BassoAntonietta", "30_FB_Scarpe", "40_FB_Stracci", "50_FB_Volantino" };
        public const int MaxPunti = 90;

        [MenuItem("Treno/Verifica/Camera")]
        public static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Avvia("ultimo");
        }

        [MenuItem("Treno/Verifica/Camera - renderer senza collider")]
        public static void MenuColliderMancanti()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log("[Verifica camera] " + ColliderMancanti("ultimo"));
        }

        /// <summary>
        /// R018: renderer visibili e grandi (dimensioni che contano per la camera, vedi CameraGuard.ContaDimensioni) senza alcun collider solido su di loro, sui genitori
        /// o sui figli: la camera li attraverserebbe. Nel Player, nei piani di sicurezza e nelle decorazioni 'morbide' (PrefissiMorbidi) non si controlla. Rapporto: collider_mancanti_&lt;etichetta&gt;.txt.
        /// </summary>
        // Decorazioni sottili, appese o addossate ai muri: la camera puo' attraversarle senza che si veda alcun difetto (fili, panni stesi,
        // manifesti, insegne, tende, decalcomanie, lampioni a muro, travi appese sotto il soffitto, macerie piatte a terra, il fondo del selciato sotto il suolo).
        static readonly string[] PrefissiMorbidi = { "Filo", "Panno_", "Manifesto_", "Insegna_", "Cartello_", "Tenda", "BastoneTenda", "Decal_", "LampioneMuro", "Appeso", "TerraMacerie", "EdicolaStatuetta", "Selciato_Fondo" };
        static bool Morbido(string nome)
        {
            foreach (var p in PrefissiMorbidi) if (nome.StartsWith(p)) return true;
            return false;
        }

        public static string ColliderMancanti(string etichetta)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Renderer senza collider (lato >= " + CameraGuard.MinLatoCollider.ToString("0.0") + " m) - " + etichetta + " - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            int tot = 0; var riepilogo = new StringBuilder();
            foreach (var n in Scene)
            {
                string path = ScenesFolder + "/" + n + ".unity";
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var gruppi = new SortedDictionary<string, int>();
                var esempi = new Dictionary<string, string>();
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                {
                    if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                    if (!r.enabled || r.GetComponentInParent<UnityEngine.Rendering.Volume>() != null) continue;
                    if (r.CompareTag("Player") || (r.transform.root != null && r.transform.root.CompareTag("Player"))) continue;
                    if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                    string nome = r.name;
                    if (Morbido(nome)) continue;
                    if (nome.StartsWith("PianoSicurezza") || nome.StartsWith("KillPlane") || nome.StartsWith("Cielo") || nome.StartsWith("Sky")) continue;
                    var b = r.bounds.size;
                    if (!CameraGuard.ContaDimensioni(b)) continue;
                    bool ha = false;
                    foreach (var c in r.GetComponentsInParent<Collider>(true)) if (!c.isTrigger && c.enabled) { ha = true; break; }
                    if (!ha) foreach (var c in r.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger && c.enabled) { ha = true; break; }
                    if (ha) continue;
                    string chiave = nome;
                    int u = chiave.LastIndexOf('_'); if (u > 0 && int.TryParse(chiave.Substring(u + 1), out _)) chiave = chiave.Substring(0, u);
                    gruppi.TryGetValue(chiave, out int k); gruppi[chiave] = k + 1;
                    if (!esempi.ContainsKey(chiave)) esempi[chiave] = r.transform.position.ToString("0.0") + " dimensioni " + b.ToString("0.0");
                }
                int sc = 0; foreach (var kv in gruppi) sc += kv.Value;
                tot += sc;
                sb.AppendLine("==== " + n + ": " + sc + " renderer senza collider ====");
                foreach (var kv in gruppi) sb.AppendLine("  " + kv.Key + " x" + kv.Value + "   (es. " + esempi[kv.Key] + ")");
                riepilogo.Append(n + " " + sc + "; ");
            }
            sb.AppendLine("TOTALE: " + tot);
            string full = Path.GetFullPath("Docs/richieste/media/R018/collider_mancanti_" + etichetta + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString(), new UTF8Encoding(false));
            return "Renderer senza collider: " + tot + " | " + riepilogo;
        }

        public static void Avvia(string etichetta, bool foto = false, bool emulaPrima = false)
        {
            var lavoro = new CameraProva.Lavoro { etichetta = etichetta, foto = foto, emulaPrima = emulaPrima };
            foreach (var n in Scene)
            {
                if (foto && n.StartsWith("30_")) continue;
                string path = ScenesFolder + "/" + n + ".unity";
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var pl in GameObject.FindGameObjectsWithTag("Player")) pl.SetActive(false);
                Physics.SyncTransforms();
                SpawnPoint sp = null;
                foreach (var s in Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include)) { if (sp == null || s.id == "inizio" || s.id == "ingresso") sp = s; }
                var sc = new CameraProva.ScenaLavoro { scena = n };
                if (sp != null)
                {
                    bool fb = n.StartsWith("30_");
                    var area = VerificaSuperfici.AreaCalpestabile(sp.transform.position, fb ? 0.2f : 0.28f, fb ? 1.0f : 1.75f);
                    int passo = Mathf.Max(1, area.celle.Count / MaxPunti);
                    for (int i = 0; i < area.celle.Count; i += passo) sc.punti.Add(area.celle[i]);
                }
                lavoro.scene.Add(sc);
            }
            File.WriteAllText("Library/R018_lavoro.json", JsonUtility.ToJson(lavoro));
            // si parte dalla prima scena: gli altri caricamenti li fa CameraProva in Play
            EditorSceneManager.OpenScene(ScenesFolder + "/" + Scene[0] + ".unity", OpenSceneMode.Single);
            Debug.Log("[Verifica camera] Lavoro scritto (" + lavoro.scene.Count + " scene). Entro in Play...");
            EditorApplication.EnterPlaymode();
        }
    }
}
