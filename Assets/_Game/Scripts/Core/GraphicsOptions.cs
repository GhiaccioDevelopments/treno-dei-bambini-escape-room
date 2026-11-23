namespace TrenoBambini.Core
{
    /// <summary>R008: tutte le voci grafiche regolabili dal menu Impostazioni. L'ordine dell'enum e' quello di <see cref="GraphicsOptions.Defs"/>.</summary>
    public enum Voce
    {
        // Generale
        Risoluzione, SchermoIntero, VSync, LimiteFps, ScalaRender, ContatoreFps,
        // Ombre
        OmbreQualita, OmbreDistanza, OmbreCascate, OmbreMorbide, OmbreLuci,
        // Luci
        LuciSecondarie, LuciMax, Riflessi,
        // Effetti
        Antialiasing, Ssao, Bloom, Grana, Vignetta, ProfonditaCampo, SfocaturaMovimento, Decal, Nebbia,
        // Meteo
        Pioggia, Spruzzi, Bagnato,
        // Dettaglio
        Texture, Anisotropico, Lod, Dettagli,
        // Accessibilita'
        RiduciMovimento
    }

    /// <summary>Descrizione di una voce: testi del menu, opzioni, valori dei quattro preset e disponibilita' per piattaforma.</summary>
    public sealed class VoceDef
    {
        public Voce id;
        public string chiave;       // suffisso della chiave PlayerPrefs: treno_g_<chiave>
        public string gruppo;
        public string nome;
        public string spiega;       // una riga di spiegazione
        public string[] opzioni;
        public int[] preset;        // indice di opzione per Bassa, Media, Alta, Ultra
        public bool legataAlPreset = true; // false: schermo, contatore FPS, accessibilita' (non fanno scattare "Personalizzata", non cambiano con i preset)
        public bool soloDesktop;    // nascosta su WebGL
        public int maxWeb = -1;     // su WebGL solo le prime N opzioni (-1 = tutte)
        public string nota;         // es. "Si applica ricaricando la scena" (vuota = si applica subito)
    }

    /// <summary>Tabella delle voci grafiche (R008).</summary>
    public static class GraphicsOptions
    {
        public static readonly string[] Gruppi = { "Generale", "Ombre", "Luci", "Effetti", "Meteo", "Dettaglio", "Gioco", "Accessibilità" };

        static readonly string[] NoSi = { "No", "Sì" };

        static VoceDef D(Voce id, string chiave, string gruppo, string nome, string spiega, string[] opz, int[] preset,
                         bool legata = true, bool desktop = false, int maxWeb = -1, string nota = null)
        {
            return new VoceDef { id = id, chiave = chiave, gruppo = gruppo, nome = nome, spiega = spiega, opzioni = opz, preset = preset,
                                 legataAlPreset = legata, soloDesktop = desktop, maxWeb = maxWeb, nota = nota };
        }

        /// <summary>Una voce per ogni valore di <see cref="Voce"/>, nello stesso ordine.</summary>
        public static readonly VoceDef[] Defs =
        {
            D(Voce.Risoluzione, "risoluzione", "Generale", "Risoluzione", "Dimensione della finestra o dello schermo. In finestra puoi anche trascinare il bordo.",
              new[] { "—" }, new[] { 0, 0, 0, 0 }, false, true),
            D(Voce.SchermoIntero, "schermo", "Generale", "Schermo", "Finestra o schermo intero (Alt+Invio fa lo stesso).",
              new[] { "Finestra", "Schermo intero" }, new[] { 0, 0, 0, 0 }, false, true),
            D(Voce.VSync, "vsync", "Generale", "Sincronia verticale", "Evita gli strappi dell'immagine. Se il gioco va a scatti, prova a spegnerla.",
              NoSi, new[] { 0, 1, 1, 1 }, true, true),
            D(Voce.LimiteFps, "limitefps", "Generale", "Limite di fotogrammi", "Tetto ai fotogrammi al secondo: meno calore e batteria. Con la sincronia attiva conta lo schermo.",
              new[] { "30", "60", "120", "Libero" }, new[] { 1, 1, 2, 2 }, true, true),
            D(Voce.ScalaRender, "scala", "Generale", "Scala di rendering", "Disegna il gioco a una risoluzione più bassa e lo ingrandisce: più veloce, un po' più sfocato.",
              new[] { "50%", "60%", "70%", "80%", "90%", "100%" }, new[] { 3, 5, 5, 5 }),
            D(Voce.ContatoreFps, "contafps", "Generale", "Contatore FPS", "Mostra in alto a sinistra quanti fotogrammi al secondo fa il gioco.",
              NoSi, new[] { 0, 0, 0, 0 }, false),

            D(Voce.OmbreQualita, "ombre", "Ombre", "Qualità delle ombre", "Quanto sono definite le ombre. Abbassale se il gioco va a scatti.",
              new[] { "Spente", "Basse", "Medie", "Alte", "Ultra" }, new[] { 2, 2, 3, 4 }),
            D(Voce.OmbreDistanza, "ombredist", "Ombre", "Distanza delle ombre", "Fino a dove si vedono le ombre. Più corta = meno lavoro per il computer.",
              new[] { "15 m", "25 m", "40 m", "60 m", "90 m" }, new[] { 1, 2, 3, 4 }),
            D(Voce.OmbreCascate, "cascate", "Ombre", "Cascate d'ombra", "In quanti livelli sono divise le ombre: più livelli = ombre nitide anche in lontananza.",
              new[] { "1", "2", "3", "4" }, new[] { 0, 1, 2, 3 }),
            D(Voce.OmbreMorbide, "morbide", "Ombre", "Ombre morbide", "Bordi sfumati invece che netti. Costa un po' di più.",
              NoSi, new[] { 0, 1, 1, 1 }),
            D(Voce.OmbreLuci, "ombreluci", "Ombre", "Ombre delle luci secondarie", "Lampade e candele proiettano ombre (solo dove la luce è impostata per farlo). Costa molto.",
              NoSi, new[] { 0, 0, 0, 0 }, true, true),

            D(Voce.LuciSecondarie, "luci", "Luci", "Luci secondarie", "Lampade a petrolio, candele e lampioni. Spente, gli interni restano più bui.",
              new[] { "Spente", "Per pixel" }, new[] { 1, 1, 1, 1 }),
            D(Voce.LuciMax, "lucimax", "Luci", "Luci per oggetto", "Quante luci secondarie illuminano lo stesso oggetto. Vale per Bassa e Media; Alta e Ultra le gestiscono da sole.",
              new[] { "1", "2", "4", "8" }, new[] { 2, 2, 2, 3 }),
            D(Voce.Riflessi, "riflessi", "Luci", "Qualità dei riflessi", "Come si fondono i riflessi dell'ambiente su pozzanghere e superfici lucide.",
              new[] { "Semplici", "Con fusione", "Completi" }, new[] { 2, 2, 2, 2 }),

            D(Voce.Antialiasing, "aa", "Effetti", "Bordi lisci (anti-aliasing)", "Toglie i gradini dai bordi degli oggetti. MSAA è il più bello ma il più pesante.",
              new[] { "Nessuno", "FXAA", "SMAA", "MSAA 2x", "MSAA 4x", "MSAA 8x" }, new[] { 0, 1, 3, 4 }, true, false, 3),
            D(Voce.Ssao, "ssao", "Effetti", "Occlusione ambientale", "Ombre sottili negli angoli e dove gli oggetti si toccano. Costa un po' di più.",
              NoSi, new[] { 0, 1, 1, 1 }, true, true),
            D(Voce.Bloom, "bloom", "Effetti", "Alone luminoso (bloom)", "Le luci forti sfumano leggermente nell'immagine.",
              NoSi, new[] { 0, 1, 1, 1 }),
            D(Voce.Grana, "grana", "Effetti", "Grana di pellicola", "Un velo di grana come nei vecchi film. Per un'immagine più pulita, spegnila.",
              NoSi, new[] { 1, 1, 1, 1 }),
            D(Voce.Vignetta, "vignetta", "Effetti", "Vignettatura e bordi", "Scurisce e sfuma leggermente i bordi dello schermo.",
              NoSi, new[] { 0, 1, 1, 1 }),
            D(Voce.ProfonditaCampo, "dof", "Effetti", "Profondità di campo", "Nelle scene filmate sfoca ciò che è fuori fuoco. Non si usa durante il gioco.",
              NoSi, new[] { 0, 1, 1, 1 }),
            D(Voce.SfocaturaMovimento, "motionblur", "Effetti", "Sfocatura di movimento", "Striscia l'immagine quando la camera si muove veloce. Spenta di base.",
              NoSi, new[] { 0, 0, 0, 0 }, true, true),
            D(Voce.Decal, "decal", "Effetti", "Segni sui muri (decal)", "Umidità e salnitro sulle pareti.",
              new[] { "Spenti", "Accesi" }, new[] { 0, 1, 1, 1 }, true, true),
            D(Voce.Nebbia, "nebbia", "Effetti", "Nebbia", "Foschia leggera in lontananza. Spenta, la vista è più nitida.",
              NoSi, new[] { 1, 1, 1, 1 }),

            D(Voce.Pioggia, "pioggia", "Meteo", "Pioggia", "Quanta pioggia cade nel vicolo e nel cortile (quando in scena piove).",
              new[] { "Spenta", "Leggera", "Normale", "Forte", "Intensa" }, new[] { 1, 2, 3, 4 }),
            D(Voce.Spruzzi, "spruzzi", "Meteo", "Schizzi e gocce", "Spruzzi a terra e gocce dai fili del bucato.",
              NoSi, new[] { 0, 1, 1, 1 }),
            D(Voce.Bagnato, "bagnato", "Meteo", "Selciato bagnato e pozzanghere", "Basoli scuri e pozzanghere che riflettono il cielo.",
              NoSi, new[] { 1, 1, 1, 1 }),

            D(Voce.Texture, "texture", "Dettaglio", "Qualità delle texture", "Risoluzione delle superfici. Metà o un quarto risparmiano memoria sui PC lenti.",
              new[] { "Piena", "Metà", "Un quarto" }, new[] { 0, 0, 0, 0 }),
            D(Voce.Anisotropico, "aniso", "Dettaglio", "Filtro anisotropico", "Tiene nitide le superfici viste di sbieco, come il selciato.",
              new[] { "Spento", "Normale", "Massimo" }, new[] { 1, 1, 2, 2 }),
            D(Voce.Lod, "lod", "Dettaglio", "Distanza di dettaglio (LOD)", "Fino a dove gli oggetti restano dettagliati prima di semplificarsi.",
              new[] { "Corta", "Normale", "Lunga", "Massima" }, new[] { 0, 1, 2, 3 }),
            D(Voce.Dettagli, "dettagli", "Dettaglio", "Oggetti sparsi", "Quanti panni stesi (e altri oggetti sparsi senza ostacolo) compaiono. Meno oggetti = meno lavoro.",
              new[] { "Pochi", "Medi", "Tutti" }, new[] { 2, 2, 2, 2 }),

            D(Voce.RiduciMovimento, "ridmov", "Accessibilità", "Riduci effetti di movimento", "Spegne la sfocatura di movimento e il tremolio e lo sfarfallio della pellicola dei ricordi.",
              NoSi, new[] { 0, 0, 0, 0 }, false),
        };

        /// <summary>Opzioni di scala di rendering, in percentuale.</summary>
        public static readonly float[] Scale = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
        public static readonly float[] DistanzaOmbre = { 15f, 25f, 40f, 60f, 90f };
        public static readonly int[] RisOmbre = { 0, 512, 1024, 2048, 4096 };
        public static readonly int[] MaxLuci = { 1, 2, 4, 8 };
        public static readonly float[] LodBias = { 0.8f, 1f, 1.5f, 2f };
        public static readonly int[] Limiti = { 30, 60, 120, -1 };
        /// <summary>Moltiplicatore della pioggia: Spenta, Leggera (0,12 come Bassa), Normale (0,45 come Media), Forte (0,8 come Alta), Intensa (1 come Ultra).</summary>
        public static readonly float[] Pioggia = { 0f, 0.12f, 0.45f, 0.8f, 1f };
    }
}
