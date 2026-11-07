using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using TrenoBambini.Puzzles;
using TrenoBambini.World;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// R006 tappa 3: il flashback "Le scarpe" (30_FB_Scarpe) come tratto di via dei Quartieri Spagnoli nel 1946 con botteghe aperte,
    /// piazzetta, traversa, vicolo cieco e cortile. Riusa il sistema a zone rasterizzate del quartiere (suolo, facciate a moduli, tetti)
    /// con il prefisso "Ric" per le mesh condivise. Postazioni degli enigmi e oggetti nascosti: coordinate in flashback_scarpe.json
    /// (devono coincidere con le costanti qui sotto).
    /// </summary>
    public static partial class SceneBuilder
    {
        const string PuntiSegretiRicordoPath = "Docs/richieste/media/R006/punti_segreti_flashback.md";

        sealed class Passante { public string nome, nota; public Vector3 pos; public float yaw; }
        static readonly List<Passante> passanti = new List<Passante>();

        // ------------------------------------------------------------------ layout

        static void DefinisciZoneRicordo()
        {
            zone.Clear();
            Z("Inizio", "piazza", -6, 6, -32, -22, 0f);
            Z("Via", "strada", -3, 3, -22, 22, 0f);
            Z("SlargoCalzolaio", "piazza", -9, -3, -8, 0, 0f);
            Z("TraversaEst", "strada", 3, 16, 6, 10, 0f);
            Z("ViccoloOvest", "strada", -16, -3, 16, 20, 0f);
            Z("Passaggio", "strada", 3, 9, -16, -12, 0f);
            Z("CortileEst", "cortile", 9, 19, -20, -9, 0f);
            Rampa("ScalinataNord", -3, 3, 22, 25, 0f, 0.9f, 6);
            Z("Piazzetta", "piazza", -8, 8, 25, 37, 0.9f);
        }

        static void DefinisciFeatureRicordo()
        {
            feats.Clear();
            zonePonte.Clear();
            // botteghe: sign = chiave dell'insegna (MP_<sign>), interno = variante del basso aperto
            Bott("Calzolaio", -9, -3, 1, 0, 0);
            Bott("Sarto", -3, -15, 1, 0, 0);
            Bott("Salumeria", 3, -6, -1, 0, 2);
            Bott("Barbiere", 3, 0, -1, 0, 0);
            Bott("Trattoria", -3, 8, 1, 0, 0);
            Bott("Fruttivendolo", 3, 16, -1, 0, 2);
            Bott("Latteria", -6, -27, 1, 0, 2);
            Bott("Ferramenta", 6, -27, -1, 0, 0);
            // chiusure dei vicoli ciechi
            Fe("portone", -16, 18, 1, 0, 4, null, null);
            Fe("cancello", 16, 8, -1, 0, 4, null, null);
            // muri ciechi: uscita, edicole, graffiti, manifesti
            Fe("cieco", 0, 37, 0, -1, 6, null, null).piani = 3;     // uscita dal ricordo
            Fe("cieco", 0, -32, 0, 1, 6, null, null);               // bacheca d'ingresso
            Fe("cieco", 19, -14.5f, -1, 0, 6, null, null);          // graffito nel cortile
            Fe("cieco", -9, 20, 0, -1, 4, null, null);              // edicola, vicolo ovest
            Fe("cieco", 10, 10, 0, -1, 4, null, null);              // edicola, traversa est
            Fe("cieco", -8, 33, 1, 0, 6, null, null).piani = 3;     // muro ovest della piazzetta
        }

        static void Bott(string sign, float x, float z, float nx, float nz, int interno)
        {
            var f = Fe("basso", x, z, nx, nz, 6, null, sign);
            f.interno = interno;
        }

        // postazioni degli enigmi e oggetti nascosti (= coordinate in flashback_scarpe.json)
        static readonly Vector3 PosQuiz = new Vector3(-5.5f, 0f, -4.5f);
        static readonly Vector3 PosNascosto = new Vector3(0f, 0f, -18f);
        static readonly Vector3 PosOrdina = new Vector3(4.5f, 0.9f, 29f);
        static readonly Vector3 PosLucchetto = new Vector3(14f, 0f, -14f);
        static readonly Vector3 PosScarpaCartone = new Vector3(-12.5f, 0f, 18.6f);
        static readonly Vector3 PosZoccolo = new Vector3(-7.0f, 0f, -7.2f);
        static readonly Vector3 PosScarpone = new Vector3(12f, 0f, 8.6f);
        static readonly Vector3 PosScarpaSignora = new Vector3(6.2f, 0.9f, 34.3f);
        static readonly Vector3 PosPezze = new Vector3(14.2f, 0f, -17.6f);

        // ------------------------------------------------------------------ helper

        static Vector3 Fronte(Feat f, float dist, float lat)
        {
            var al = Vector3.Cross(f.n, Vector3.up);
            var p = f.wp + f.n * dist + al * lat;
            p.y = f.wp.y;
            return p;
        }
        static float YawMuro(Feat f) { return Mathf.Atan2(-f.n.x, -f.n.z) * Mathf.Rad2Deg; }
        static float YawStrada(Feat f) { return YawMuro(f) + 180f; }

        /// <summary>Scatola orientata con la profondita' lungo la normale n (size.x laterale, size.z lungo n).</summary>
        static GameObject BoxN(string nome, Transform p, Vector3 pos, Vector3 n, Vector3 size, string mat)
        {
            return Box(nome, p, pos, size, mat, Quaternion.LookRotation(n, Vector3.up).eulerAngles, false);
        }

        static GameObject Panchina(Transform env, Vector3 pos, float yaw)
        {
            return Pezzo(env, "PanchinaTufo", "Panchina", new[] { "M_ArenariaBlocchi" },
                kb => kb.Box(new Vector3(-1.0f, 0f, -0.28f), new Vector3(1.0f, 0.5f, 0.28f), 0, TileStone), pos, yaw, 0.03f);
        }

        static void Passa(string nome, float x, float z, float yaw, string nota)
        {
            passanti.Add(new Passante { nome = nome, pos = PT(x, z), yaw = yaw, nota = nota });
        }

        static void PreparaMaterialiRicordo()
        {
            PreparaMaterialiTesti();
            PreparaTestiAmbiente("ricordo"); // graffito e campana: testi in testi_ambiente.json
        }

        // ------------------------------------------------------------------ scena

        static void BuildFlashback()
        {
            var scene = NewScene();
            kitScene = "Ricordo"; kitIndex = 0; pre = "Ric";
            var r = new System.Random(1946);
            doorWallPoints.Clear();
            ResetSpazio();
            moduli.Clear();
            hubAvvisi = 0;
            segreti.Clear();
            passanti.Clear();
            riservati.Clear();
            Color sepiaFog = new Color(0.62f, 0.50f, 0.34f);
            Ambient(new Color(0.72f, 0.60f, 0.42f), new Color(0.52f, 0.42f, 0.30f), new Color(0.28f, 0.22f, 0.15f));
            Sun(new Color(1f, 0.86f, 0.62f), 0.9f, new Vector3(55f, -40f, 0f));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sepiaFog;
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 60f;

            EnsureLightMaterials();
            DefinisciZoneRicordo();
            RasterizzaZone();
            quotaTerreno = GroundY;
            DefinisciFeatureRicordo();
            foreach (var p in new[] { PosQuiz, PosNascosto, PosOrdina, PosLucchetto, PosScarpaCartone, PosZoccolo, PosScarpone, PosScarpaSignora, PosPezze })
                riservati.Add(new Vector2(p.x, p.z));

            var env = new GameObject("Ambiente").transform;
            var porte = new GameObject("Porte").transform;
            var spawns = new GameObject("SpawnPoints").transform;

            PreparaMaterialiRicordo();
            CostruisciSuolo(env);
            CostruisciTetti(env);
            CostruisciFacciate(env, porte, r);
            ArredaRicordo(env, r);
            RisolviSpazio("30_FB_Scarpe");
            AssestaMacerie();
            SetStatic(env.gameObject);
            PredisponiSicurezza(true); // R017

            // uscita: bagliore nel muro nord della piazzetta
            var exitRoot = new GameObject("Uscita").transform;
            Feat fu = null;
            foreach (var f in feats) if (f.tipo == "cieco" && Mathf.Abs(f.wp.z - 37f) < 0.1f) fu = f;
            var exit = Box("EscapeExit", exitRoot, fu.wp + fu.n * 0.12f + Vector3.up * 1.2f, new Vector3(1.4f, 2.4f, 0.12f), "M_LuceUscita");
            exit.AddComponent<Glow>();
            exit.AddComponent<EscapeExit>();

            var ctrl = new GameObject("EscapeRoom");
            ctrl.AddComponent<EscapeRoomController>().flashbackId = "flashback_scarpe";

            // punti di posizionamento dei passanti (R010): vuoti nominati
            var pr = new GameObject("PassantiRicordo").transform;
            foreach (var p in passanti)
            {
                var g = new GameObject(p.nome);
                g.transform.SetParent(pr, false);
                g.transform.SetPositionAndRotation(p.pos, Quaternion.Euler(0f, p.yaw, 0f));
            }

            Vector3 start = PT(0f, -29f);
            Spawn("inizio", start, 0f, spawns);

            SegretiRicordo();

            var cam = MakeCamera("Main Camera", Vector3.zero, Quaternion.identity, sepiaFog, true);
            BuildPlayer(start + Vector3.up * 0.05f, 0f, cam, CamProfilo.Esterno);
            SaveScene(scene, "30_FB_Scarpe");
            Debug.Log("[Ricordo] Flashback costruito: " + AreaCalpestabile().ToString("0") + " m2 calpestabili (" + zone.Count + " zone), " + moduli.Count + " moduli di facciata, " + hubAvvisi + " avvisi.");
            quotaTerreno = null;
            pre = "Hub";
        }

        // ------------------------------------------------------------------ arredo

        static void ArredaRicordo(Transform env, System.Random r)
        {
            propMesh.Clear();
            EnsureWaterMaterial();

            // ---- insegne e botteghe
            foreach (var f in feats)
            {
                if (f.tipo != "basso") continue;
                Box("Insegna_Bottega_" + f.sign, env, f.wp + f.n * 0.05f + Vector3.up * 2.77f, new Vector3(1.7f, 0.25f, 0.04f), "MP_" + f.sign,
                    Quaternion.LookRotation(f.n, Vector3.up).eulerAngles, false);
                WallLamp(env, f.wp, f.n, Vector3.Cross(f.n, Vector3.up), 1.25f);
                Negozio(env, f, r);
            }

            // ---- punti di riferimento: lampioni, fontana, pozzo, panchine, bancarelle, carretti
            Lamp(env, PT(-2.4f, -20f)); Lamp(env, PT(2.4f, -9.5f)); Lamp(env, PT(-2.4f, 5f)); Lamp(env, PT(2.4f, 20f));
            Lamp(env, PT(-5f, -30f)); Lamp(env, PT(5f, -30f)); Lamp(env, PT(-7f, 35.5f)); Lamp(env, PT(7f, 35.5f));
            Pezzo(env, "Fontanella", "Fontana", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Fontana, PT(-1f, 32f), 0f, 0.01f);
            Panchina(env, PT(-5.2f, 28.5f), 0f); Panchina(env, PT(5.8f, 36f), 0f); Panchina(env, PT(-6.5f, 32f), 90f);
            string[] m = { "M_LegnoVernicePelata", "Tessuto1", "M_CassaLegno", "PannoOcra" };
            string[] m2 = { "M_LegnoVernicePelata", "Tessuto2", "M_CassaLegno", "PannoBeige" };
            Pezzo(env, "Bancarella", "Bancarella1", m, Bancarella, PT(5.2f, 27.5f), 0f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella2", m2, Bancarella, PT(-5.0f, 35.2f), 180f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella1", m, Bancarella, PT(0f, -24.5f), 180f, 0.03f);
            Pezzo(env, "Bancarella", "Bancarella2", m2, Bancarella, PT(10f, 8.0f), 0f, 0.03f);
            Cart(env, PT(-2.0f, 10f)); Cart(env, PT(12.5f, -19.0f));
            Pezzo(env, "Pozzo", "Pozzo", new[] { "M_ArenariaBlocchi", "M_LegnoVernicePelata", "M_FerroRuggine02" }, Pozzo, PT(17.4f, -10.9f), 0f, 0.02f);
            Pezzo(env, "Lavatoio", "Lavatoio", new[] { "M_ArenariaBlocchi", "Acqua", "M_FerroRuggine02" }, Lavatoio, PT(18.4f, -17.5f), 90f, 0.02f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(10.3f, -19.2f), 20f, 0.03f);
            Pezzo(env, "GabbiaGalline", "Gabbia", new[] { "M_LegnoVernicePelata", "M_FerroRuggine02" }, GabbiaGalline, PT(11.3f, -19.4f), -10f, 0.03f);
            Sedia(env, PT(10.5f, -10.6f), 30f); Sedia(env, PT(11.4f, -10.3f), -40f);
            Model("BotteVino", env, PT(10.4f, -12.0f), 0f, 1.25f, true, "Botte");
            Model("CestaVimini", env, PT(13.2f, -17.2f), 25f, 1.5f, true, "CestaPanni");
            Basilico(env, PT(9.6f, -14.6f), 0f);

            // ---- slargo del calzolaio: cassa dietro cui si nasconde lo zoccolo, panche, vasi
            Model("CassaLegno", env, PT(-8.2f, -7.3f), 10f, 1.4f, true, "Cassa");
            Sedia(env, PT(-4.9f, -0.9f), 180f); Sedia(env, PT(-5.7f, -1.0f), 170f);
            Bacinella(env, PT(-4.0f, -6.8f));
            Basilico(env, PT(-8.3f, -0.8f), 0f);

            // ---- vicoli: casse, botti, scatola di ricordi
            Model("CassaLegno", env, PT(-12.6f, 16.8f), 15f, 1.4f, true, "Cassa");
            Model("CassaLegno", env, PT(-14.0f, 19.3f), -10f, 1.3f, true, "Cassa");
            Model("BotteVino", env, PT(-14.8f, 17.2f), 0f, 1.25f, true, "Botte");
            Sacco(env, PT(-14.2f, 19.2f), 30f);
            Sacco(env, PT(14.8f, 9.0f), 10f); Sacco(env, PT(14.2f, 6.8f), -20f);
            Model("CassaLegno", env, PT(15.2f, 7.8f), 0f, 1.3f, true, "Cassa");
            Valigia(env, PT(13.6f, 9.1f), 40f);

            // ---- oggetti sparsi lungo le strade (poche file di casse, ceste, secchi, sedie, ...)
            LungoLeStrade(env, r);

            // ---- easter egg
            Campana(env, PT(-3.2f, -26.2f));
            Pallone(env, PT(-2.4f, -24.2f));
            Trottola(env, PT(2.6f, -3.2f));
            Edicole(env);
            ScarpeSulFilo(env, -1f);
            ScarpeSulFilo(env, 12.5f);
            int muriManifesti = 0; // i manifesti stanno su muri ciechi a coordinate fisse: se il muro non c'e' lo si segnala
            foreach (var f in feats)
            {
                if (f.tipo != "cieco") continue;
                var al = Vector3.Cross(f.n, Vector3.up);
                if (Mathf.Abs(f.wp.x - 19f) < 0.1f)
                {
                    muriManifesti |= 1;
                    Manifesto(env, "Graffito", f.wp + al * -0.8f + Vector3.up * 1.3f, f.n, 1.1f, 0.42f, "Manifesto_Graffito");
                    Manifesto(env, "Crollo", f.wp + al * 1.6f + Vector3.up * 1.7f, f.n, 0.5f, 0.75f, "Manifesto_Crollo");
                }
                else if (Mathf.Abs(f.wp.z + 32f) < 0.1f)
                {
                    muriManifesti |= 2;
                    Manifesto(env, "Acqua", f.wp + al * -1.2f + Vector3.up * 1.7f, f.n, 0.62f, 0.93f, "Manifesto_Acqua");
                    Manifesto(env, "Viveri", f.wp + al * 1.2f + Vector3.up * 1.7f, f.n, 0.62f, 0.93f, "Manifesto_Viveri");
                    Manifesto(env, "Scuola", f.wp + al * 0.0f + Vector3.up * 1.8f, f.n, 0.5f, 0.78f, "Manifesto_Scuola");
                }
                else if (Mathf.Abs(f.wp.x + 8f) < 0.1f && Mathf.Abs(f.wp.z - 33f) < 0.1f)
                {
                    muriManifesti |= 4;
                    Manifesto(env, "Festa", f.wp + al * 0.5f + Vector3.up * 1.8f, f.n, 0.62f, 0.93f, "Manifesto_Festa");
                }
            }
            if ((muriManifesti & 1) == 0) Debug.LogWarning("[Ricordo] Muro cieco x=19 non trovato: niente graffito e cartello di pericolo.");
            if ((muriManifesti & 2) == 0) Debug.LogWarning("[Ricordo] Muro cieco z=-32 non trovato: niente manifesti Acqua/Viveri/Scuola.");
            if ((muriManifesti & 4) == 0) Debug.LogWarning("[Ricordo] Muro cieco (-8, 33) non trovato: niente manifesto Festa.");
            // stendipanni sopra le vie
            var linee = new List<Vector3>();
            linee.Add(new Vector3(0, -18, 'x')); linee.Add(new Vector3(0, -9, 'x')); linee.Add(new Vector3(0, 4, 'x')); linee.Add(new Vector3(0, 12, 'x'));
            linee.Add(new Vector3(-9, 18, 'z')); linee.Add(new Vector3(8, 8, 'z')); linee.Add(new Vector3(14, -10.5f, 'x'));
            
            PanniDaLista(env, r, linee);

            // ---- passanti fermi (punti di posizionamento per R010: personaggi "come in un ricordo")
            Passa("Passante_01_Ingresso", 0.0f, -27.5f, 0f, "Di spalle, all'imbocco della via: figura di riferimento che guarda verso il calzolaio.");
            Passa("Passante_02_DavantiSarto", -0.8f, -15.0f, 90f, "Fermo davanti alla vetrina del sarto.");
            Passa("Passante_03_Corridoio", 1.2f, -12.0f, 180f, "Di passaggio, con un fagotto sulla spalla.");
            Passa("Passante_04_ClienteCalzolaio", -6.0f, -2.0f, -90f, "Cliente che aspetta le scarpe riparate, sullo slargo.");
            Passa("Passante_05_BambinoCampana", -3.2f, -24.0f, 0f, "Bambino ferito a meta' di un salto sulla campana.");
            Passa("Passante_06_FilaSalumiere", 0.6f, -6.0f, 90f, "In fila davanti al salumiere.");
            Passa("Passante_07_DalBarbiere", 0.5f, 0.0f, 90f, "Uomo in attesa davanti al barbiere.");
            Passa("Passante_08_Trattoria", -0.5f, 8.0f, -90f, "Avventore che esce dalla trattoria.");
            Passa("Passante_09_Fruttivendolo", 0.6f, 15.5f, 90f, "Donna con la borsa della spesa davanti al fruttivendolo.");
            Passa("Passante_10_Traversa", 8.0f, 8.0f, 180f, "Passante nella traversa est, di profilo.");
            Passa("Passante_11_ViccoloOvest", -8.0f, 18.0f, 90f, "Ragazzo appoggiato al muro del vicolo cieco.");
            Passa("Passante_12_Cortile", 13.5f, -11.0f, 0f, "Lavandaia nel cortile.");
            Passa("Passante_13_Fontana", -3.5f, 29.5f, 45f, "Sulla piazzetta, vicino alla fontanella.");
            Passa("Passante_14_Panchina", 5.5f, 35.0f, 180f, "Anziano seduto sulla panchina, in fondo alla piazzetta.");
        }

        // ------------------------------------------------------------------ botteghe

        static void Negozio(Transform env, Feat f, System.Random r)
        {
            float yS = YawStrada(f), yM = YawMuro(f);
            Vector3 up = Vector3.up, al = Vector3.Cross(f.n, up);
            switch (f.sign)
            {
                case "Calzolaio":
                    {
                        Pz(env, "BancoCalzolaio", "R_BancoCalzolaio", new[] { "M_AssiStagionate", "M_FerroRuggine02", "M_LegnoVernicePelata", "PannoTerra", "Cuoio" }, BancoCalzolaio, Fronte(f, 0.95f, -0.9f), yM);
                        Sedia(env, Fronte(f, 1.7f, -0.9f), yM);
                        Pz(env, "ScaffaleForme", "R_ScaffaleForme", new[] { "M_AssiStagionate", "M_LegnoVernicePelata" }, ScaffaleForme, Fronte(f, 0.3f, 1.7f), yS);
                        Segreto("Sotto il banco del calzolaio", Fronte(f, 0.95f, -0.9f) + Vector3.up * 0.3f, "Sul ripiano basso del banco: ritagli di cuoio e una scatola di chiodini. Un posto per un indizio sul mestiere.");
                        Segreto("Forme di legno del calzolaio", Fronte(f, 0.3f, 1.7f) + Vector3.up * 1.0f, "Scaffale con le forme di legno: una ha il nome 'Amerigo' scritto a matita.");
                        var lantern = Model("LanternaPH", env, Fronte(f, 0.95f, -0.5f), 0f, 1.2f, false, "LanternaBanco");
                        // scarpe appese a un binario davanti alla bottega
                        BoxN("Appeso_BinarioScarpe", env, f.wp + f.n * 0.2f + al * -0.55f + up * 2.15f, f.n, new Vector3(2.2f, 0.04f, 0.04f), "M_FerroRuggine02");
                        for (int i = 0; i < 8; i++)
                        {
                            float lat = -1.55f + i * 0.28f;
                            Vector3 p = f.wp + f.n * 0.2f + al * lat;
                            BoxN("Appeso_Cordino" + i, env, p + up * 1.88f, f.n, new Vector3(0.012f, 0.5f, 0.012f), "M_FerroRuggine02");
                            ShoeObject(env, p + up * 1.49f, yS + ((i % 3) - 1) * 10f, 1.2f).name = "Appeso_Scarpa";
                        }
                        Segreto("Cesta di scarpe da riparare", Fronte(f, 0.9f, 2.9f) + Vector3.up * 0.3f, "Fra le scarpe da riparare ce n'e' una dal numero piccolo, con un biglietto.");
                        // cesta di scarpe da riparare
                        var cesta = Model("CestaVimini", env, Fronte(f, 0.9f, 2.9f), 20f, 1.5f, true, "CestaScarpe");
                        for (int i = 0; i < 2; i++)
                        {
                            var s = ShoeObject(env, Vector3.zero, 0f, 1.0f);
                            s.transform.position = new Vector3(cesta.transform.position.x + (i - 0.5f) * 0.22f, TopY(cesta) - 0.003f, cesta.transform.position.z);
                            Cavaliere(s, cesta);
                        }
                        break;
                    }
                case "Sarto":
                    {
                        Segreto("Manichino del sarto", Fronte(f, 1.0f, -1.3f) + Vector3.up * 1.2f, "Nella tasca del cappotto sul manichino: uno spillo d'oro e un bottone.");
                        Pz(env, "Manichino", "R_Manichino", new[] { "PannoBeige", "M_LegnoVernicePelata" }, Manichino, Fronte(f, 1.0f, -1.3f), yS);
                        Pz(env, "Manichino", "R_Manichino2", new[] { "PannoGrigio", "M_LegnoVernicePelata" }, Manichino, Fronte(f, 1.1f, -0.5f), yS + 25f);
                        var tav = Pz(env, "Tavolino", "Tavolino", new[] { "M_LegnoVernicePelata" }, kb =>
                        {
                            kb.Box(new Vector3(-0.4f, 0.72f, -0.4f), new Vector3(0.4f, 0.77f, 0.4f), 0, TileWood);
                            foreach (var sx in new[] { -0.36f, 0.31f }) foreach (var sz in new[] { -0.36f, 0.31f })
                                kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.05f, 0.72f, sz + 0.05f), 0, TileWood, KitBuilder.NY);
                        }, Fronte(f, 0.7f, 1.3f), 0f);
                        var rot = Pz(env, "RotoliStoffa", "R_RotoliStoffa", new[] { "PannoBlu", "PannoTerra", "PannoOcra", "PannoGrigio" }, RotoliStoffa, tav.transform.position, yM, false);
                        Impila(rot, tav, Vector2.zero);
                        // stoffe appese alla facciata
                        for (int i = 0; i < 3; i++)
                            ClothObject(env, i == 1 ? "Camicia" : "Pantaloni", i % 2, i == 0 ? "PannoBlu" : (i == 1 ? "PannoBeige" : "PannoGrigio"),
                                f.wp + f.n * 0.16f + al * (-2.0f + i * 0.85f + (i == 2 ? 0.2f : 0f)) + up * 2.3f, Mathf.Atan2(f.n.x, f.n.z) * Mathf.Rad2Deg, Vector3.one);
                        break;
                    }
                case "Salumeria":
                    {
                        Pezzo(env, "Bancarella", "Bancarella1", new[] { "M_LegnoVernicePelata", "Tessuto1", "M_CassaLegno", "PannoOcra" }, Bancarella, Fronte(f, 1.2f, 0f), YawStrada(f), 0.03f); // R007 t2: banco parallelo alla facciata, fronte verso la strada (2,3 m non sta di punta)
                        Pz(env, "Appeso_Salumi", "R_Salumi", new[] { "M_FerroRuggine02", "Tessuto1", "PannoOcra" }, kb =>
                        {
                            kb.Box(new Vector3(-1.1f, 2.1f, -0.03f), new Vector3(1.1f, 2.14f, 0.03f), 0, 0.3f);
                            for (int i = 0; i < 9; i++)
                            {
                                float x = -0.95f + i * 0.24f;
                                kb.Tube(new Vector3(x, 2.1f, 0f), new Vector3(x, 1.72f - (i % 3) * 0.08f, 0f), 0.035f, 5, i % 4 == 3 ? 2 : 1, 0.3f);
                            }
                        }, Fronte(f, 0.35f, 0f), yS, false);
                        Segreto("Salumi appesi", Fronte(f, 0.35f, 0f) + Vector3.up * 1.8f, "Fra i salami ce n'e' uno di carta: dentro un messaggio (gioco di mestieri).");
                        var cas = Model("CassaLegno", env, Fronte(f, 1.0f, 2.2f), 90f, 1.2f, true, "CassaFormaggi");
                        var f1 = Cyl("Formaggio1", env, Vector3.zero, 0.36f, 0.14f, "PannoOcra", null, false);
                        f1.transform.position = new Vector3(cas.transform.position.x, TopY(cas) + 0.07f, cas.transform.position.z);
                        Cavaliere(f1, cas);
                        break;
                    }
                case "Barbiere":
                    {
                        Segreto("Soglia del barbiere", Fronte(f, 0.4f, -1.4f), "Fra le assi della soglia, sotto la tendina: una moneta da dieci lire incastrata.");
                        Segreto("Radio del barbiere", Fronte(f, 0.7f, 1.7f) + Vector3.up * 0.9f, "Radio accesa sul tavolino del barbiere: potrebbe trasmettere un giornale radio dell'epoca (audio futuro).");
                        // tendina all'ingresso: bastone, mantellina in alto e un lembo raccolto da un lato (l'insegna dipinta "BARBIERE" e' sulla facciata)
                        BoxN("BastoneTenda", env, Fronte(f, 0.12f, 0f) + Vector3.up * 2.45f, f.n, new Vector3(1.5f, 0.04f, 0.04f), "M_FerroRuggine02");
                        BoxN("Tenda_Mantellina", env, Fronte(f, 0.12f, 0f) + Vector3.up * 2.28f, f.n, new Vector3(1.4f, 0.32f, 0.03f), "Tessuto2");
                        BoxN("Tenda_Lembo", env, Fronte(f, 0.16f, 0.45f) + Vector3.up * 1.2f, f.n, new Vector3(0.28f, 1.95f, 0.04f), "Tessuto2");
                        Pz(env, "PoltronaBarbiere", "R_Poltrona", new[] { "M_FerroRuggine02", "Cuoio" }, PoltronaBarbiere, Fronte(f, 1.2f, 0.3f), yS);
                        var t = Pz(env, "Tavolino", "Tavolino", new[] { "M_LegnoVernicePelata" }, kb =>
                        {
                            kb.Box(new Vector3(-0.4f, 0.72f, -0.4f), new Vector3(0.4f, 0.77f, 0.4f), 0, TileWood);
                            foreach (var sx in new[] { -0.36f, 0.31f }) foreach (var sz in new[] { -0.36f, 0.31f })
                                kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.05f, 0.72f, sz + 0.05f), 0, TileWood, KitBuilder.NY);
                        }, Fronte(f, 0.7f, 1.7f), 0f);
                        var rad = Pz(env, "Radio", "B_Radio", new[] { "M_LegnoVernicePelata", "Tessuto3", "M_FerroRuggine02" }, Radio, t.transform.position, yS, false);
                        Impila(rad, t, Vector2.zero);
                        Sedia(env, Fronte(f, 1.4f, -0.5f), yM);
                        break;
                    }
                case "Fruttivendolo":
                    {
                        Pezzo(env, "Bancarella", "Bancarella2", new[] { "M_LegnoVernicePelata", "Tessuto2", "M_CassaLegno", "PannoBeige" }, Bancarella, Fronte(f, 1.2f, 0f), YawStrada(f), 0.03f); // R007 t2: banco parallelo alla facciata, fronte verso la strada (2,3 m non sta di punta)
                        for (int i = 0; i < 4; i++)
                        {
                            int tipo = i % 3;
                            Pz(env, "CassaFrutta", "R_Frutta" + tipo, new[] { "M_CassaLegno", "Foglia", "PannoOcra", "Tessuto1" }, kb => CassaFrutta(kb, tipo), Fronte(f, 0.6f + (i % 2) * 0.5f, -2.4f + i * 0.35f), yS + i * 7f);
                        }
                        Segreto("Cassa di frutta del fruttivendolo", Fronte(f, 0.6f, -2.4f) + Vector3.up * 0.2f, "Sotto la verdura di una cassa: un biglietto della spesa con una lista di scarpe.");
                        Sacco(env, Fronte(f, 0.7f, 2.4f), 20f);
                        Sacco(env, Fronte(f, 0.9f, 2.9f), -15f);
                        break;
                    }
                case "Trattoria":
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            float lat = -1.2f + i * 2.4f;
                            var t = Pz(env, "Tavolino", "Tavolino", new[] { "M_LegnoVernicePelata" }, kb =>
                            {
                                kb.Box(new Vector3(-0.4f, 0.72f, -0.4f), new Vector3(0.4f, 0.77f, 0.4f), 0, TileWood);
                                foreach (var sx in new[] { -0.36f, 0.31f }) foreach (var sz in new[] { -0.36f, 0.31f })
                                    kb.Box(new Vector3(sx, 0f, sz), new Vector3(sx + 0.05f, 0.72f, sz + 0.05f), 0, TileWood, KitBuilder.NY);
                            }, Fronte(f, 1.3f, lat), 0f);
                            Sedia(env, Fronte(f, 0.6f, lat), yS + 180f);
                            Sedia(env, Fronte(f, 2.0f, lat), yS);
                            var lampT = Model("LampadaPetrolio_A", env, Vector3.zero, 0f, 1.0f, false, "LampadaTavolo");
                            Impila(lampT, t, Vector2.zero);
                        }
                        break;
                    }
                case "Latteria":
                    {
                        Model("BotteVino", env, Fronte(f, 0.8f, -1.8f), 0f, 1.25f, true, "BotteLatte");
                        Model("SecchioMetallo", env, Fronte(f, 0.8f, 1.4f), 0f, 1.1f, true, "SecchioLatte");
                        Model("SecchioMetallo", env, Fronte(f, 0.7f, 2.0f), 40f, 1.1f, true, "SecchioLatte");
                        Sedia(env, Fronte(f, 1.0f, 2.8f), yS);
                        break;
                    }
                default: // Ferramenta
                    {
                        Model("CassaLegno", env, Fronte(f, 0.7f, -1.6f), 0f, 1.3f, true, "CassaChiodi");
                        Model("SecchioMetallo", env, Fronte(f, 0.8f, -0.6f), 0f, 1.1f, true, "SecchioFerro");
                        Model("SecchioLegno", env, Fronte(f, 0.8f, 0.9f), 0f, 1.1f, true, "Secchio");
                        Pezzo(env, "Bicicletta", "Bicicletta", new[] { "M_FerroRuggine02", "Cuoio", "SuolaScarpa" }, Bicicletta, Fronte(f, 0.8f, 2.4f), yM + 90f, 0.03f);
                        break;
                    }
            }
        }

        // ------------------------------------------------------------------ easter egg

        static void Campana(Transform env, Vector3 p)
        {
            // la "campana" disegnata col gesso: dieci riquadri e il cielo
            var kb = new KitBuilder("PannoBeige");
            float y0 = 0.003f, y1 = 0.007f;
            for (int i = 0; i < 5; i++)
            {
                float z = i * 0.5f;
                if (i == 2 || i == 4) { kb.Box(new Vector3(-0.5f, y0, z), new Vector3(-0.03f, y1, z + 0.46f), 0, 1f, KitBuilder.NY); kb.Box(new Vector3(0.03f, y0, z), new Vector3(0.5f, y1, z + 0.46f), 0, 1f, KitBuilder.NY); }
                else kb.Box(new Vector3(-0.23f, y0, z), new Vector3(0.23f, y1, z + 0.46f), 0, 1f, KitBuilder.NY);
            }
            var go = MeshObject(env, "Decal_Campana", kb.Build(KitName("Campana")), new[] { "PannoBeige" }, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Segreto("Campana disegnata col gesso", p, "Gioco da bambini sull'acciottolato dello slargo d'ingresso: sulla casella 'cielo' un sassolino segnato con una A.");
        }

        static void Pallone(Transform env, Vector3 p)
        {
            var go = Prim(PrimitiveType.Sphere, "PalloneStracci", env, p + Vector3.up * 0.13f, new Vector3(0.26f, 0.26f, 0.26f), "PannoBeige", null, true);
            Mob(go);
            Segreto("Pallone di stracci", p, "Pallone cucito con gli stracci, lasciato vicino alla bancarella d'ingresso: i bambini giocano ancora.");
        }

        static void Trottola(Transform env, Vector3 p)
        {
            var kb = new KitBuilder("M_LegnoDipintoBlu", "M_FerroRuggine02");
            kb.Tube(p + Vector3.zero, p + new Vector3(0f, 0.04f, 0f), 0.005f, 4, 1, 0.1f);
            kb.Tube(p + new Vector3(0f, 0.04f, 0f), p + new Vector3(0f, 0.1f, 0f), 0.04f, 8, 0, 0.1f);
            var go = MeshObject(env, "Trottola", kb.Build(KitName("Trottola")), new[] { "M_LegnoDipintoBlu", "M_FerroRuggine02" }, false);
            Segreto("Trottola di legno", p, "Trottola dipinta di blu in mezzo alla via, vicino al barbiere.");
        }

        static void Edicole(Transform env)
        {
            foreach (var f in feats)
                if (f.tipo == "cieco" && (Mathf.Abs(f.wp.x + 9f) < 0.1f && Mathf.Abs(f.wp.z - 20f) < 0.1f || Mathf.Abs(f.wp.x - 10f) < 0.1f && Mathf.Abs(f.wp.z - 10f) < 0.1f))
                {
                    Shrine(env, f.wp, f.n);
                    Segreto("Edicola votiva", f.wp + f.n * 0.8f, "Edicola votiva con lumino e fiori: un biglietto sotto i fiori, ex-voto di bambini in partenza.");
                }
        }

        static void ScarpeSulFilo(Transform env, float z)
        {
            // classica coppia di scarpe legate per i lacci e lanciate sul filo della luce che attraversa la via (y 6,0)
            float y = 6.0f;
            Box("Appeso_FiloLuce", env, new Vector3(0f, y, z), new Vector3(5.8f, 0.02f, 0.02f), "M_FerroRuggine02", null, false);
            for (int i = 0; i < 2; i++)
            {
                Box("Appeso_LaccioScarpe" + i, env, new Vector3(0.4f + i * 0.35f, y - 0.2f, z), new Vector3(0.012f, 0.4f, 0.012f), "PannoBeige", null, false);
                var s = ShoeObject(env, new Vector3(0.4f + i * 0.35f, y - 0.55f, z), 90f, 1.2f);
                s.name = "Appeso_ScarpaFilo";
            }
            Segreto("Scarpe appese al filo della luce", new Vector3(0.5f, y - 0.5f, z), "Un paio di scarpe lanciate sul filo, a 6 m di altezza: nessuno sa di chi siano. Visibili dalla via.");
        }

        // ------------------------------------------------------------------ segreti

        static void SegretiRicordo()
        {
            foreach (var f in feats)
            {
                if (f.tipo == "portone") Segreto("Portone sbarrato del vicolo cieco ovest", Fronte(f, 0.5f, 0f), "Portone con le assi inchiodate: graffiti dei ragazzi e, sotto la soglia, una fessura per un biglietto.");
                if (f.tipo == "cancello") Segreto("Cancello della traversa est", Fronte(f, 0.5f, 0f), "Cancello di ferro: dietro le sbarre si intravede un cortile buio (luce o voce, in futuro).");
            }
            Segreto("Cassa dello zoccolo nascosto", PosZoccolo, "Fra la cassa e il muro del calzolaio: l'oggetto nascosto 'zoccolo di legno' (JSON).");
            Segreto("Vicolo cieco ovest, dietro le casse", PosScarpaCartone, "Oggetto nascosto 'scarpa con suola di cartone' (JSON): un nascondiglio per i ragazzi che non hanno scarpe.");
            Segreto("Traversa est, sacchi e cancello", PosScarpone, "Oggetto nascosto 'scarpone del soldato' (JSON), sotto i sacchi della traversa.");
            Segreto("Piazzetta, dietro la panchina", PosScarpaSignora, "Oggetto nascosto 'scarpa da signora' (JSON), dietro la panchina in fondo alla piazzetta.");
            Segreto("Cortile, cesta dei panni", PosPezze, "Oggetto nascosto 'pezze per i piedi' (JSON), nella cesta dei panni vicino al lavatoio.");
            Segreto("Pozzo del cortile", PT(17.4f, -10.9f, 0.8f), "Il pozzo ha una secchia legata e un'asse sconnessa: il fondo puo' ospitare un oggetto o un indizio.");
            Segreto("Graffito 'C'ero anch'io'", PT(18.9f, -15.3f, 1.3f), "Sul muro est del cortile: sotto la scritta di gesso, un'incisione con una data e delle iniziali.");
            Segreto("Gabbie delle galline", PT(10.8f, -19.3f, 0.4f), "Nel cortile: l'uovo di una gallina e un biglietto sotto la paglia.");
            Segreto("Bacheca d'ingresso", PT(0f, -31.5f, 1.7f), "Manifesti fitti sul muro sud: dietro il piu' vecchio, un foglio ripiegato.");
            Segreto("Fontanella della piazzetta", PT(-1f, 32f, 0.9f), "Il bordo della fontanella: una moneta sul fondo e una pietra mobile.");
            Segreto("Uscita dal ricordo", PT(0f, 36.6f, 0.0f), "La porta luminosa al fondo della piazzetta, che riporta al presente.");
            var sb = new StringBuilder();
            sb.AppendLine("## Punti per i passanti (R010)");
            sb.AppendLine();
            sb.AppendLine("Vuoti nominati sotto `PassantiRicordo` nella scena `30_FB_Scarpe`: posizione e orientamento (yaw) per \"passanti fermi come in un ricordo\".");
            sb.AppendLine("Tutti su suolo libero (nessun oggetto di scena sopra).");
            sb.AppendLine();
            sb.AppendLine("| Nome | x | y | z | yaw | Nota |");
            sb.AppendLine("|------|---|---|---|-----|------|");
            foreach (var p in passanti)
                sb.AppendLine("| " + p.nome + " | " + p.pos.x.ToString("0.0") + " | " + p.pos.y.ToString("0.0") + " | " + p.pos.z.ToString("0.0") + " | " + p.yaw.ToString("0") + " | " + p.nota + " |");
            sb.AppendLine();
            sb.AppendLine("## Postazioni degli enigmi e oggetti nascosti (flashback_scarpe.json)");
            sb.AppendLine();
            sb.AppendLine("| Elemento | x | y | z |");
            sb.AppendLine("|----------|---|---|---|");
            Action<string, Vector3> riga = (n, v) => sb.AppendLine("| " + n + " | " + v.x.ToString("0.0") + " | " + v.y.ToString("0.0") + " | " + v.z.ToString("0.0") + " |");
            riga("enigma_quiz", PosQuiz); riga("enigma_nascosto", PosNascosto); riga("enigma_ordina", PosOrdina); riga("enigma_lucchetto", PosLucchetto);
            riga("scarpa_cartone", PosScarpaCartone); riga("zoccolo_legno", PosZoccolo); riga("scarpone_soldato", PosScarpone);
            riga("scarpa_signora", PosScarpaSignora); riga("pezze_piedi", PosPezze);
            sb.AppendLine();
            sb.AppendLine("## Zone");
            sb.AppendLine();
            sb.AppendLine("| Zona | x0..x1 | z0..z1 | Quota |");
            sb.AppendLine("|------|--------|--------|-------|");
            foreach (var z in zone)
                sb.AppendLine("| " + z.nome + " | " + z.x0 + ".." + z.x1 + " | " + z.z0 + ".." + z.z1 + " | " + (z.rampa ? z.y + " -> " + z.y1 + " (scalinata)" : z.y.ToString()) + " |");
            ScriviDocSegreti(PuntiSegretiRicordoPath, "Punti per i segreti del flashback \"Le scarpe\" (R011) e passanti (R010)", "30_FB_Scarpe", null, sb.ToString());
        }
    }
}
