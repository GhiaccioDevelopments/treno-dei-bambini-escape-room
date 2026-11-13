using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Puzzles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrenoBambini.World
{
    /// <summary>
    /// R012: oggetti-ricordo il cui punto d'ingresso sta in una scena gia' costruita (oggetti.json, campo "ingresso"): il fagotto di stracci nel cortile del basso.
    /// Compare solo dopo il capitolo precedente e finche' l'oggetto non e' nella latta. Pezzi di primitive come gli enigmi del mondo; nessuna scena da ricostruire.
    /// </summary>
    public static class IngressoRicordo
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aggancia()
        {
            SceneManager.sceneLoaded -= OnScena;
            SceneManager.sceneLoaded += OnScena;
        }

        static void OnScena(Scene sc, LoadSceneMode mode)
        {
            if (GameManager.IsMenuScene(sc.name)) return;
            if (mode == LoadSceneMode.Additive && GameManager.MenuBackdrop) return;
            Crea(sc);
        }

        /// <summary>Crea gli ingressi della scena (richiamabile anche dalla prova dopo aver cambiato lo stato).</summary>
        public static void Crea(Scene sc)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            foreach (var o in GameData.Oggetti)
            {
                var ing = o.ingresso;
                if (ing == null || ing.scena != sc.name) continue;
                if (gm.Save.inventario.Contains(o.id)) continue;
                if (!string.IsNullOrEmpty(ing.richiedeFlashback) && !gm.Save.flashbackCompletati.Contains(ing.richiedeFlashback)) continue;
                if (Esiste(sc, o.id)) continue;
                Costruisci(sc, o, ing);
            }
        }

        static bool Esiste(Scene sc, string id)
        {
            foreach (var p in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include)) if (p.oggettoId == id && p.gameObject.scene == sc) return true;
            return false;
        }

        static void Costruisci(Scene sc, OggettoDef o, IngressoDef ing)
        {
            var root = new GameObject("Ingresso_" + o.id);
            SceneManager.MoveGameObjectToScene(root, sc);
            root.transform.position = new Vector3(ing.x, ing.y, ing.z);
            root.transform.rotation = Quaternion.Euler(0f, ing.yaw, 0f);
            var d = new EnigmaDef { id = "ingresso_" + o.id, parti = ing.parti };
            MondoFab.Oggetto(d, root.transform, false);
            var b = MondoFab.Limiti(root.transform);
            var bc = root.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = root.transform.InverseTransformPoint(b.center);
            bc.size = new Vector3(Mathf.Max(0.5f, b.size.x), Mathf.Max(0.4f, b.size.y), Mathf.Max(0.5f, b.size.z));
            var g = root.AddComponent<Glow>();
            g.intensity = 0.35f; g.bob = false;
            var pick = root.AddComponent<ItemPickup>();
            pick.oggettoId = o.id;
            pick.prompt = ing.prompt;
        }
    }
}
