using UnityEngine;

namespace TrenoBambini.Player
{
    /// <summary>
    /// R018: muro, parete o porta di un interno che si dissolve quando sta tra la camera e Amerigo (regola in CameraGuard).
    /// Usa il cross-fade dei LODGroup di Unity: lo shader URP Lit ha gia' il dithering (LOD_FADE_CROSSFADE) in tutti i passi, quindi
    /// serve un solo LODGroup per muro, nessuna copia di materiali, nessuna modifica alle lightmap e funziona anche in WebGL.
    /// Un solo livello LOD con soglia 0,01 (altezza relativa sullo schermo): per nascondere il muro si porta la 'size' del LODGroup quasi a zero,
    /// cosi' l'altezza relativa scende sotto la soglia (anche con la camera a pochi centimetri: le soglie sopra 1 non funzionano) e parte la
    /// dissolvenza animata (durata 'DurataFade'); per mostrarlo si ripristina la size.
    /// </summary>
    public class MuroDissolvibile : MonoBehaviour
    {
        public const float DurataFade = 0.25f;
        public const float Soglia = 0.01f;
        const float SizeNascosto = 0.0001f;
        LODGroup lod;
        float sizeNormale;
        // LODGroup.crossFadeAnimationDuration e' globale: si imposta al primo muro attivo e si ripristina quando non ne resta nessuno
        static int usi;
        static float durataOriginale;
        public bool Nascosto { get; private set; }

        /// <summary>Configura il LODGroup (usato dal generatore, in editor).</summary>
        public static MuroDissolvibile Prepara(GameObject go)
        {
            var m = go.GetComponent<MuroDissolvibile>();
            if (m == null) m = go.AddComponent<MuroDissolvibile>();
            var lg = go.GetComponent<LODGroup>();
            if (lg == null) lg = go.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.CrossFade;
            lg.animateCrossFading = true;
            var rs = go.GetComponentsInChildren<Renderer>(true);
            lg.SetLODs(new[] { new LOD(Soglia, rs) });
            lg.RecalculateBounds();
            return m;
        }

        void Awake()
        {
            lod = GetComponent<LODGroup>();
            if (lod != null) sizeNormale = lod.size;
            if (usi++ == 0) { durataOriginale = LODGroup.crossFadeAnimationDuration; LODGroup.crossFadeAnimationDuration = DurataFade; }
        }

        void OnDestroy()
        {
            if (--usi <= 0) { usi = 0; LODGroup.crossFadeAnimationDuration = durataOriginale; }
        }

        public void Dissolvi(bool nascondi)
        {
            if (nascondi == Nascosto || lod == null) return;
            Nascosto = nascondi;
            lod.size = nascondi ? SizeNascosto : sizeNormale;
        }

        void OnDisable() { if (Nascosto && lod != null) { Nascosto = false; lod.size = sizeNormale; } }
    }
}
