using UnityEngine;

namespace TrenoBambini.UI.Fx
{
    /// <summary>Un solo punto per lo stile della UI: palette d'epoca, durate e interruttore "Effetti ridotti".</summary>
    public static class UITheme
    {
        // palette base (da UIFactory) + materiali della valigia d'archivio
        public static readonly Color Carta = new Color32(0xE8, 0xD9, 0xB5, 0xFF);
        public static readonly Color CartaChiara = new Color32(0xF1, 0xE6, 0xCC, 0xFF);
        public static readonly Color CartaScura = new Color32(0xD2, 0xBE, 0x93, 0xFF);
        public static readonly Color Inchiostro = new Color32(0x3B, 0x2A, 0x1A, 0xFF);
        public static readonly Color InchiostroBlu = new Color32(0x2E, 0x3A, 0x59, 0xFF);
        public static readonly Color Ottone = new Color32(0xB0, 0x8D, 0x57, 0xFF);
        public static readonly Color OttoneLuce = new Color32(0xE2, 0xC4, 0x8A, 0xFF);
        public static readonly Color Ceralacca = new Color32(0x7A, 0x1F, 0x1A, 0xFF);
        public static readonly Color Cartone = new Color32(0x8A, 0x6A, 0x45, 0xFF);
        public static readonly Color Oro = new Color32(0xF2, 0xC9, 0x6B, 0xFF);
        public static readonly Color Esatto = new Color32(0x3F, 0x6E, 0x3A, 0xFF);

        // durate (secondi, sempre in tempo "unscaled")
        public const float Apertura = 0.38f;
        public const float Chiusura = 0.2f;
        public const float Breve = 0.12f;

        /// <summary>"Effetti ridotti": niente tremolii, particelle e grandi movimenti; restano dissolvenze brevi.</summary>
        public static bool ReduceEffects { get; private set; }

        public static void SetReduceEffects(bool value)
        {
            ReduceEffects = value;
        }

        /// <summary>Durata effettiva: con effetti ridotti le animazioni diventano dissolvenze molto brevi.</summary>
        public static float Dur(float d)
        {
            return ReduceEffects ? Mathf.Min(d, 0.12f) : d;
        }
    }
}
