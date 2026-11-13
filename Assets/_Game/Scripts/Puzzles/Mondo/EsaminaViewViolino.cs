using System.Collections;
using System.Collections.Generic;
using TrenoBambini.Audio;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.UI;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrenoBambini.Puzzles
{
    /// <summary>
    /// R015 (capitolo "Il violino", Modena): i meccanismi nuovi della vista ravvicinata, quasi tutti sul suono. Ogni suono ha sempre un'alternativa
    /// visiva (onda disegnata, lancetta che trema, segni colorati che si accendono): ogni enigma si risolve anche senza sentire.
    ///  - forno: prova fisica e tempo. Il forno ha un calore che scende da solo; la legna lo alza, la serranda aperta lo fa scappare. Un pizzico di farina
    ///    sulla pietra dice se e' freddo (bianca), giusto (nocciola) o troppo caldo (nera). Si inforna solo dopo una lettura giusta, si sforna quando la
    ///    clessidra e' vuota (troppo presto = si rimette dentro, troppo tardi = si ricomincia, mai una penalita').
    ///  - suoni: abbinare suono e attrezzo. "Ascolta" fa sentire l'attrezzo nascosto e ne disegna l'onda sulla lavagnetta; toccando un attrezzo lo si sente e si vede la sua onda.
    ///  - accordatura: battimenti. Corda e riferimento insieme fanno "ua-ua" con tanti battiti al secondo quanti Hz di scarto; la lancetta trema allo stesso ritmo.
    ///    La chiave sposta lo scarto di mezzo passo: quando il battimento sparisce la corda e' accordata.
    ///  - impronte: tracce. Si confronta uno scarpone con un'impronta (numero, chiodi, tacco); riconosciute le orme che escono dal laboratorio, si seguono
    ///    scegliendo a ogni bivio la traccia giusta, fino al violino.
    ///  - melodia: ascolta e ripeti. Alcide suona una frase (le note si accendono sulla tastiera e sullo spartito); la si ripete toccando i segni colorati.
    ///    Una nota sbagliata = Alcide la rifa' piu' lenta.
    /// Tutti i testi stanno nel JSON (EnigmaDef.strumenti, forno, suoni, corde, impronte, frasi, parti.msg). Il codice non contiene testi narrativi.
    /// </summary>
    public partial class EsaminaView
    {
        static bool MecViolino(string mec) { return mec == "forno" || mec == "suoni" || mec == "accordatura" || mec == "impronte" || mec == "melodia"; }

        void BuildViolino(string mec)
        {
            if (!MecViolino(mec)) return;
            if (mec == "suoni" || mec == "accordatura" || mec == "melodia")
            {
                // per ascoltare: una sorgente 2D per gli effetti dell'enigma e la musica in pausa finche' la vista e' aperta
                voce = stage.gameObject.AddComponent<AudioSource>();
                voce.playOnAwake = false; voce.spatialBlend = 0f; voce.loop = false;
                stage.gameObject.AddComponent<MusicaSospesa>(); // la riaccende quando la vista si chiude (lo stage viene distrutto)
            }
            if (mec == "forno") BuildForno();
            else if (mec == "suoni") BuildSuoni();
            else if (mec == "accordatura") BuildAccordatura();
            else if (mec == "impronte") BuildImpronte();
            else if (mec == "melodia") BuildMelodia();
        }

        void BuildViolinoUi(string mec)
        {
            if (mec == "suoni" || mec == "melodia") BottoneViolino(S(0), "Ascolta", new Vector2(-28f, 24f), () => ConfermaStracci());
            else if (mec == "accordatura")
            {
                BottoneViolino(S(0), "Ascolta", new Vector2(-28f, 24f), () => ConfermaStracci());
                BottoneViolino(S(2), "GiraDestra", new Vector2(-28f, 96f), () => Giro(1));
                BottoneViolino(S(1), "GiraSinistra", new Vector2(-28f, 168f), () => Giro(-1));
            }
        }

        void BottoneViolino(string testo, string nome, Vector2 pos, UnityEngine.Events.UnityAction cb)
        {
            var b = UIFactory.MakeButton(ui, testo, cb, new Vector2(280f, 60f), ButtonStyle.Brass, 24f, true);
            UIFactory.Place((RectTransform)b.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), pos, new Vector2(280f, 60f));
            b.gameObject.name = nome;
        }

        /// <summary>Azioni dei pezzi dei meccanismi R015. true = gestita.</summary>
        bool UsaViolino(Run r)
        {
            string mec = Meccanismo(def);
            if (!MecViolino(mec)) return false;
            string az = (r.d.azione ?? "").ToLowerInvariant();
            int v = Mathf.RoundToInt(r.d.valore);
            switch (mec)
            {
                case "forno":
                    if (az == "legna") { Legna(); return true; }
                    if (az == "serranda") { Serranda(); return true; }
                    if (az == "farina") { Farina(); return true; }
                    if (az == "inforna") { Inforna(); return true; }
                    if (az == "sforna") { Sforna(); return true; }
                    break;
                case "suoni":
                    if (az == "attrezzo") { Attrezzo(r.d.id); return true; }
                    break;
                case "accordatura":
                    if (az == "corda") { ScegliCorda(v); return true; }
                    if (az == "giro") { Giro(v); return true; }
                    break;
                case "impronte":
                    if (az == "scarpone") { ToccaScarpone(v); return true; }
                    if (az == "orma") { Confronta(scarponeSel, r.d.id); return true; }
                    if (az == "ramo") { SeguiRamo(r.d.id); return true; }
                    break;
                case "melodia":
                    if (az == "nota") { SuonaNota(v); return true; }
                    break;
            }
            return false;
        }

        /// <summary>Tasto C, gamepad Nord e pulsante a schermo: Ascolta (suoni, accordatura, melodia). true = il meccanismo e' di R015.</summary>
        bool ConfermaViolino(string mec, out bool esito)
        {
            esito = false;
            if (!MecViolino(mec)) return false;
            if (mec == "suoni") esito = AscoltaSuono();
            else if (mec == "accordatura") esito = AscoltaCorda();
            else if (mec == "melodia") esito = AscoltaFrase();
            return true;
        }

        /// <summary>Tasti: Z / X girano la chiave (accordatura); 1-5 toccano gli attrezzi (suoni) o i segni colorati (melodia). Chiamato da Update.</summary>
        void TastiViolino()
        {
            if (solved || def == null) return;
            string mec = Meccanismo(def);
            if (!MecViolino(mec)) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (mec == "accordatura")
            {
                if (kb.zKey.wasPressedThisFrame) Giro(-1);
                else if (kb.xKey.wasPressedThisFrame) Giro(1);
                return;
            }
            int n = -1;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) n = 0;
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) n = 1;
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) n = 2;
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) n = 3;
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) n = 4;
            if (n < 0) return;
            if (mec == "melodia") SuonaNota(n);
            else if (mec == "suoni") { var r = CercaRun("attrezzo", n); if (r != null) Attrezzo(r.d.id); }
        }

        // ------------------------------------------------------------------ audio comune
        AudioSource voce;

        void Suona(AudioClip c, float vol = 1f, bool loop = false)
        {
            if (voce == null || c == null) return;
            if (AudioManager.I != null && AudioManager.I.WaitingForGesture) return;
            voce.Stop();
            voce.clip = c;
            voce.loop = loop;
            voce.volume = Mathf.Clamp01(vol * (AudioManager.I != null ? AudioManager.I.ChannelGain(AudioChannel.Effetti) : 0.9f));
            voce.Play();
        }

        static readonly Dictionary<string, AudioClip> clipViolino = new Dictionary<string, AudioClip>();

        /// <summary>Suono sintetizzato di un attrezzo dell'accordatore (offline, identico anche in WebGL).</summary>
        static AudioClip ClipAttrezzo(string tipo)
        {
            AudioClip c;
            if (clipViolino.TryGetValue("att_" + tipo, out c) && c != null) return c;
            var rng = new Synth.Rng(1946u + (uint)(tipo ?? "").Length * 7u);
            float[] buf;
            switch (tipo)
            {
                case "diapason": // sinusoide pura a 440 Hz, lunga, che si spegne piano
                    buf = Synth.Buffer(2.6f);
                    for (int i = 0; i < buf.Length; i++) { float t = i / (float)Synth.Rate; buf[i] = Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Exp(-t * 1.1f) * Mathf.Min(1f, t / 0.01f); }
                    break;
                case "chiave": // scatti regolari (cricchetto)
                    buf = Synth.Buffer(1.1f);
                    for (int k = 0; k < 9; k++)
                    {
                        int i0 = Mathf.RoundToInt((0.06f + k * 0.11f) * Synth.Rate);
                        for (int j = 0; j < 260 && i0 + j < buf.Length; j++) buf[i0 + j] += rng.Signed() * Mathf.Exp(-j / 40f);
                    }
                    Synth.HighPass(buf, 1800f);
                    break;
                case "feltro": // un tonfo sordo, basso e corto
                    buf = Synth.Noise(0.5f, rng, 1f);
                    Synth.LowPass(buf, 260f);
                    Synth.Envelope(buf, t => Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * 22f));
                    break;
                case "martelletto": // colpo secco su una corda di pianoforte
                    buf = Synth.Buffer(1.6f);
                    Synth.Piano(buf, new Synth.Note { midi = 60f, start = 0f, dur = 0.25f, vel = 1f }, 1f, rng);
                    break;
                default: // corda d'acciaio pizzicata: ronza a lungo
                    buf = Synth.Buffer(2.4f);
                    Synth.Pluck(buf, new Synth.Note { midi = 45f, start = 0f, dur = 2.0f, vel = 1f }, 1f, 0.75f, 0f, rng);
                    Synth.Pluck(buf, new Synth.Note { midi = 45.08f, start = 0f, dur = 2.0f, vel = 0.6f }, 1f, 0.75f, 0f, rng);
                    break;
            }
            Synth.Normalize(buf, 0.8f);
            c = Synth.ToClip("violino_" + tipo, buf);
            clipViolino["att_" + tipo] = c;
            return c;
        }

        /// <summary>Una nota di violino (melodia di Alcide): stesso strumento sintetico del tema del violino (R002).</summary>
        static AudioClip ClipNota(int midi, float dur)
        {
            string k = "nota_" + midi + "_" + Mathf.RoundToInt(dur * 100f);
            AudioClip c;
            if (clipViolino.TryGetValue(k, out c) && c != null) return c;
            var buf = Synth.Buffer(dur + 0.5f);
            Synth.Violin(buf, new Synth.Note { midi = midi, start = 0.01f, dur = dur, vel = 0.9f }, 1f, new Synth.Rng((uint)midi * 31u));
            Synth.Reverb(buf, 0.18f, 0.4f);
            Synth.Normalize(buf, 0.75f);
            c = Synth.ToClip(k, buf);
            clipViolino[k] = c;
            return c;
        }

        // ------------------------------------------------------------------ forno
        float temperatura, cottura;
        bool serrandaAperta, fornoLetto;
        int faseForno;                 // 0 da scaldare / infornare, 1 il pane cuoce, 2 fatto
        Vector3 braceScala, fiammaScala, sopraScala, sottoScala;

        public float Temperatura { get { return temperatura; } }
        public int FaseForno { get { return faseForno; } }
        public bool FornoLetto { get { return fornoLetto; } }
        public bool SerrandaAperta { get { return serrandaAperta; } }

        FornoDef F { get { return def.forno ?? (def.forno = new FornoDef()); } }

        void BuildForno()
        {
            temperatura = F.inizio; cottura = 0f; serrandaAperta = false; fornoLetto = false; faseForno = 0;
            Run r;
            if (run.TryGetValue("brace", out r) && r.go != null) braceScala = r.go.transform.localScale;
            if (run.TryGetValue("fiamma", out r) && r.go != null) fiammaScala = r.go.transform.localScale;
            if (run.TryGetValue("clessidra_sopra", out r) && r.go != null) sopraScala = r.go.transform.localScale;
            if (run.TryGetValue("clessidra_sotto", out r) && r.go != null) sottoScala = r.go.transform.localScale;
            StartCoroutine(VitaForno());
        }

        IEnumerator VitaForno()
        {
            while (!closed)
            {
                AvanzaForno(Time.unscaledDeltaTime);
                yield return null;
            }
        }

        /// <summary>Il tempo del forno: il calore scende da solo (di piu' con la serranda aperta), il pane cuoce. Pubblico per la prova (avanti veloce).</summary>
        public void AvanzaForno(float dt)
        {
            if (solved) return;
            temperatura = Mathf.Clamp(temperatura - (F.raffredda + (serrandaAperta ? F.serranda : 0f)) * dt, 20f, F.massimo);
            if (faseForno == 1)
            {
                cottura += dt;
                if (cottura > F.cottura + F.tardi) Bruciato();
            }
            AggiornaForno();
        }

        void AggiornaForno()
        {
            float k = Mathf.InverseLerp(20f, F.massimo, temperatura);
            Run r;
            if (run.TryGetValue("brace", out r) && r.go != null) r.go.transform.localScale = new Vector3(braceScala.x, braceScala.y * (0.4f + 2.2f * k), braceScala.z);
            if (run.TryGetValue("fiamma", out r) && r.go != null) r.go.transform.localScale = fiammaScala * (0.2f + 1.6f * k);
            float s = faseForno == 1 ? Mathf.Clamp01(cottura / Mathf.Max(1f, F.cottura)) : faseForno == 2 ? 1f : 0f;
            if (run.TryGetValue("clessidra_sopra", out r) && r.go != null) r.go.transform.localScale = new Vector3(sopraScala.x, Mathf.Max(0.0005f, sopraScala.y * (1f - s)), sopraScala.z);
            if (run.TryGetValue("clessidra_sotto", out r) && r.go != null) r.go.transform.localScale = new Vector3(sottoScala.x, Mathf.Max(0.0005f, sopraScala.y * s + sottoScala.y), sottoScala.z);
        }

        /// <summary>Un ciocco nel fuoco. Pubblico per la prova.</summary>
        public void Legna()
        {
            if (solved) return;
            temperatura = Mathf.Min(F.massimo, temperatura + F.legna);
            fornoLetto = false; // il forno e' cambiato: va riletto
            UISfx.Play(Sfx.Punch, 0.4f);
            Info(S(3));
            AggiornaForno();
        }

        /// <summary>Apre o chiude la serranda della bocca. Pubblico per la prova.</summary>
        public void Serranda()
        {
            if (solved) return;
            serrandaAperta = !serrandaAperta;
            Run r;
            if (run.TryGetValue("serranda", out r) && r.go != null)
                r.go.transform.localRotation = r.rot0 * Quaternion.Euler(serrandaAperta ? -80f : 0f, 0f, 0f);
            UISfx.Play(Sfx.ClickBrass, 0.4f);
            Info(serrandaAperta ? S(4) : S(5));
        }

        /// <summary>Un pizzico di farina sulla pietra: il colore dice com'e' il forno. Pubblico per la prova.</summary>
        public void Farina()
        {
            if (solved) return;
            int esito = temperatura < F.giusto0 ? 0 : temperatura > F.giusto1 ? 2 : 1;
            Rivela("pizzico_bianco", esito == 0);
            Rivela("pizzico_nocciola", esito == 1);
            Rivela("pizzico_nero", esito == 2);
            fornoLetto = esito == 1;
            UISfx.Play(Sfx.ClickPaper, 0.4f);
            Info(S(esito));
        }

        /// <summary>Inforna con la pala: solo dopo una lettura giusta e con il forno ancora giusto. Pubblico per la prova.</summary>
        public bool Inforna()
        {
            if (solved || faseForno != 0) return false;
            if (!fornoLetto) { Info(S(6)); return false; }
            if (temperatura < F.giusto0 || temperatura > F.giusto1) { fornoLetto = false; Wrong(S(7)); return false; }
            faseForno = 1; cottura = 0f;
            Rivela("pane_crudo", false);
            Rivela("pane_forno", true);
            string extra = "";
            if (serrandaAperta) { Serranda(); extra = "\n" + S(11); }
            UISfx.Play(Sfx.Whoosh, 0.4f);
            Info(S(8) + extra);
            return true;
        }

        /// <summary>Sforna: troppo presto = si rimette dentro; nella finestra giusta = risolto. Pubblico per la prova.</summary>
        public bool Sforna()
        {
            if (solved || faseForno != 1) return false;
            if (cottura < F.cottura - F.presto) { Info(S(9)); return false; }
            if (cottura > F.cottura + F.tardi) { Bruciato(); return false; }
            faseForno = 2;
            Rivela("pane_forno", false);
            Rivela("pane_cotto,pane_cotto_b", true);
            AggiornaForno();
            Solved();
            return true;
        }

        void Bruciato()
        {
            faseForno = 0; cottura = 0f; fornoLetto = false;
            Rivela("pane_forno", false);
            Rivela("pane_crudo", true);
            Info(S(10));
        }

        // ------------------------------------------------------------------ suoni
        int turnoSuono;
        bool suonoSentito;
        readonly List<GameObject> barre = new List<GameObject>();

        public int TurnoSuono { get { return turnoSuono; } }
        public bool SuonoSentito { get { return suonoSentito; } }

        void BuildSuoni() { turnoSuono = 0; suonoSentito = false; }

        SuonoDef SuonoDi(string attrezzo)
        {
            if (def.suoni != null) foreach (var s in def.suoni) if (s.attrezzo == attrezzo) return s;
            return null;
        }

        /// <summary>Alcide fa suonare l'attrezzo nascosto: si sente e la sua onda compare sulla lavagnetta. Pubblico per la prova.</summary>
        public bool AscoltaSuono()
        {
            if (solved || def.suoni == null || turnoSuono >= def.suoni.Count) return false;
            var s = def.suoni[turnoSuono];
            suonoSentito = true;
            Suona(ClipAttrezzo(s.tipo));
            DisegnaOnda(s.tipo, "#e8e4d8");
            Info(string.Format(S(1), s.onda));
            return true;
        }

        /// <summary>Tocca un attrezzo del banco: suona e disegna la sua onda; se e' quello giusto si passa al suono dopo. Pubblico per la prova.</summary>
        public bool Attrezzo(string id)
        {
            if (solved || def.suoni == null || turnoSuono >= def.suoni.Count) return false;
            var mio = SuonoDi(id);
            if (mio == null) return false;
            Suona(ClipAttrezzo(mio.tipo));
            Run r;
            if (!suonoSentito)
            {
                DisegnaOnda(mio.tipo, "#c9a45a");
                Info((run.TryGetValue(id, out r) && !string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + S(3));
                return false;
            }
            var giusto = def.suoni[turnoSuono];
            if (giusto.attrezzo != id)
            {
                DisegnaOnda(mio.tipo, "#c9a45a");
                Wrong(string.Format(S(2), mio.nome, mio.onda));
                return false;
            }
            turnoSuono++;
            suonoSentito = false;
            if (run.TryGetValue(id, out r)) { r.segnata = true; Evidenzia(r, true); }
            UISfx.Play(Sfx.Chime, 0.5f);
            if (turnoSuono >= def.suoni.Count) { Info(giusto.ok); Solved(); return true; }
            Info(giusto.ok + "\n" + string.Format(S(4), def.suoni.Count - turnoSuono));
            return true;
        }

        /// <summary>L'onda di un suono disegnata col gesso sulla lavagnetta (parte "onde": centro e misura): alternativa visiva per chi sente poco.</summary>
        void DisegnaOnda(string tipo, string colore)
        {
            foreach (var g in barre) if (g != null) Destroy(g);
            barre.Clear();
            Run area;
            if (!run.TryGetValue("onde", out area)) return;
            const int N = 40;
            float w = area.d.sx, h = area.d.sy;
            for (int k = 0; k < N; k++)
            {
                float y = Onda(tipo, k);
                float alt = Mathf.Max(0.004f, Mathf.Abs(y) * h * 0.5f);
                var p = new Vector3(area.d.x - w * 0.5f + (k + 0.5f) * w / N, area.d.y + Mathf.Sign(y) * alt * 0.5f, area.d.z);
                barre.Add(Pezzetto("onda_" + k, "cubo", "gesso", colore, p, new Vector3(w / N * 0.55f, alt, 0.004f), objRoot));
            }
        }

        /// <summary>Forma stilizzata di ogni suono (-1..1), una colonna alla volta: ogni attrezzo ha un disegno diverso.</summary>
        static float Onda(string tipo, int k)
        {
            switch (tipo)
            {
                case "diapason": return 0.85f * Mathf.Sin(k * 0.9f) * Mathf.Exp(-k / 90f);                       // lunga e liscia
                case "chiave": return k % 5 == 0 ? 0.75f : 0.04f;                                                  // scatti in fila
                case "feltro": return 0.32f * Mathf.Exp(-((k - 6f) * (k - 6f)) / 10f);                           // una gobba bassa, corta
                case "martelletto": return (k % 2 == 0 ? 1f : -1f) * (k < 2 ? 1f : Mathf.Exp(-(k - 2f) / 3.5f)); // picco secco
                default: return 0.85f * Mathf.Sin(k * 1.4f) * (0.55f + 0.45f * Mathf.Sin(k * 0.3f)) * Mathf.Exp(-k / 45f); // trema e ronza
            }
        }

        // ------------------------------------------------------------------ accordatura (battimenti)
        float[] scarti;
        bool[] accordate;
        int cordaSel = -1;
        float tBattito;

        public int CordaScelta { get { return cordaSel; } }
        public int CordeAccordate { get { int c = 0; if (accordate != null) foreach (var b in accordate) if (b) c++; return c; } }
        public float Scarto(int i) { return scarti != null && i >= 0 && i < scarti.Length ? scarti[i] : 0f; }

        void BuildAccordatura()
        {
            int n = def.corde != null ? def.corde.Count : 0;
            scarti = new float[n];
            accordate = new bool[n];
            for (int i = 0; i < n; i++) scarti[i] = def.corde[i].scarto;
            cordaSel = -1;
            StartCoroutine(Lancetta());
        }

        /// <summary>La lancetta sul telaio trema con il battimento della corda scelta (tanti battiti al secondo quanti Hz di scarto); ferma = accordata.</summary>
        IEnumerator Lancetta()
        {
            Run r;
            if (!run.TryGetValue("lancetta", out r) || r.go == null) yield break;
            Vector3 perno = r.pos0 - r.rot0 * new Vector3(0f, 0.04f, 0f);
            while (!closed)
            {
                tBattito += Time.unscaledDeltaTime;
                float s = cordaSel >= 0 ? Mathf.Abs(scarti[cordaSel]) : 0f;
                float ang = s > 0.01f ? 32f * Mathf.Sin(2f * Mathf.PI * s * tBattito) : 0f;
                var rot = r.rot0 * Quaternion.Euler(0f, 0f, ang);
                r.go.transform.localRotation = rot;
                r.go.transform.localPosition = perno + rot * new Vector3(0f, 0.04f, 0f);
                yield return null;
            }
        }

        /// <summary>Sceglie la corda da accordare. Pubblico per la prova.</summary>
        public void ScegliCorda(int i)
        {
            if (solved || scarti == null || i < 0 || i >= scarti.Length) return;
            if (accordate[i]) { Info(S(7)); return; }
            var prima = cordaSel >= 0 ? CercaRun("corda", cordaSel) : null;
            if (prima != null) { prima.segnata = false; Evidenzia(prima, false); }
            cordaSel = i;
            var r = CercaRun("corda", i);
            if (r != null) { r.segnata = true; Evidenzia(r, true); }
            UISfx.Play(Sfx.Tick, 0.5f);
            Info(string.Format(S(8), NomeBreve(def.corde[i].nome)));
        }

        static string NomeBreve(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return "";
            int sp = nome.IndexOf(' ');
            return sp >= 0 ? nome.Substring(sp + 1) : nome;
        }

        /// <summary>Corda e riferimento insieme: due sinusoidi a f e f + scarto (il battimento e' la loro differenza).</summary>
        AudioClip ClipBattimento(int i)
        {
            float f = Synth.Hz(def.corde[i].nota), s = scarti[i];
            var buf = Synth.Buffer(2.4f);
            for (int k = 0; k < buf.Length; k++)
            {
                float t = k / (float)Synth.Rate;
                float env = Mathf.Min(1f, t / 0.02f) * Mathf.Min(1f, (2.4f - t) / 0.3f);
                buf[k] = 0.4f * (Mathf.Sin(2f * Mathf.PI * f * t) + Mathf.Sin(2f * Mathf.PI * (f + s) * t)) * env;
            }
            return Synth.ToClip("battimento", buf);
        }

        /// <summary>Fa sentire corda e diapason insieme. Pubblico per la prova.</summary>
        public bool AscoltaCorda()
        {
            if (solved) return false;
            if (cordaSel < 0) { Info(S(6)); return false; }
            Suona(ClipBattimento(cordaSel));
            return true;
        }

        /// <summary>Mezzo scatto della chiave (dir -1 sinistra, +1 destra): lo scarto cambia di un passo. Pubblico per la prova.</summary>
        public bool Giro(int dir)
        {
            if (solved || scarti == null) return false;
            if (cordaSel < 0) { Info(S(6)); return false; }
            var c = def.corde[cordaSel];
            float passo = c.passo > 0.01f ? c.passo : 0.5f;
            float prima = Mathf.Abs(scarti[cordaSel]);
            scarti[cordaSel] = Mathf.Round((scarti[cordaSel] + Mathf.Sign(dir) * passo) / passo) * passo;
            float dopo = Mathf.Abs(scarti[cordaSel]);
            UISfx.Play(Sfx.Tick, 0.35f);
            if (dopo < 0.01f)
            {
                accordate[cordaSel] = true;
                scarti[cordaSel] = 0f;
                Rivela(c.dopo, true);
                var r = CercaRun("corda", cordaSel);
                if (r != null) { r.segnata = true; Evidenzia(r, true); }
                Suona(ClipBattimento(cordaSel));
                UISfx.Play(Sfx.Chime, 0.5f);
                Info(string.Format(S(5), c.nome));
                cordaSel = -1;
                if (CordeAccordate >= accordate.Length) Solved();
                return true;
            }
            Suona(ClipBattimento(cordaSel));
            Info(dopo < prima ? S(3) : S(4));
            return true;
        }

        // ------------------------------------------------------------------ impronte nella neve
        int scarponeSel = -1, colpevole = -1, passoPista, passiPista;

        public int Colpevole { get { return colpevole; } }
        public int PassoPista { get { return passoPista; } }

        void BuildImpronte()
        {
            scarponeSel = -1; colpevole = -1; passoPista = 0; passiPista = 0;
            foreach (var kv in run) if ((kv.Value.d.azione ?? "").ToLowerInvariant() == "ramo") passiPista = Mathf.Max(passiPista, Mathf.RoundToInt(kv.Value.d.valore) + 1);
        }

        ImprontaDef Impronta(int i) { return def.impronte != null && i >= 0 && i < def.impronte.Count ? def.impronte[i] : null; }

        /// <summary>Prende in mano lo scarpone del ragazzo j. Pubblico per la prova.</summary>
        public void ToccaScarpone(int j)
        {
            if (solved || Impronta(j) == null) return;
            var prima = scarponeSel >= 0 ? CercaRun("scarpone", scarponeSel) : null;
            if (prima != null) { prima.segnata = false; Evidenzia(prima, false); }
            scarponeSel = j;
            var r = CercaRun("scarpone", j);
            if (r != null) { r.segnata = true; Evidenzia(r, true); }
            UISfx.Play(Sfx.Tick, 0.5f);
            Info((r != null && !string.IsNullOrEmpty(r.d.msg) ? r.d.msg + "\n" : "") + (colpevole < 0 ? S(0) : ""));
        }

        /// <summary>Confronta lo scarpone j con l'impronta (id della parte "orma"): dice che cosa combacia o la prima cosa che non combacia. Pubblico per la prova.</summary>
        public bool Confronta(int j, string ormaId)
        {
            Run o;
            if (solved || string.IsNullOrEmpty(ormaId) || !run.TryGetValue(ormaId, out o)) return false;
            if (j < 0 || Impronta(j) == null) { Info((!string.IsNullOrEmpty(o.d.msg) ? o.d.msg + "\n" : "") + S(1)); return false; }
            int k = Mathf.RoundToInt(o.d.valore);
            var a = Impronta(j); var b = Impronta(k);
            if (b == null) return false;
            if (j == k)
            {
                string msg = string.Format(S(2), a.nome, a.misura, a.chiodi, a.tacco);
                UISfx.Play(Sfx.Chime, 0.45f);
                if (o.d.evento == "porta" && colpevole < 0)
                {
                    colpevole = k;
                    Rivela("g:pista_1", true);
                    msg += "\n" + string.Format(S(6), a.nome);
                }
                Info(msg);
                return true;
            }
            if (a.misura != b.misura) Wrong(string.Format(S(3), a.misura, b.misura > a.misura ? S(7) : S(8)));
            else if (a.chiodi != b.chiodi) Wrong(string.Format(S(4), a.chiodi, b.chiodi));
            else Wrong(string.Format(S(5), a.tacco, b.tacco));
            return false;
        }

        /// <summary>Segue un ramo della pista: quello giusto rivela il passo dopo (all'ultimo, la legnaia e il violino). Pubblico per la prova.</summary>
        public bool SeguiRamo(string id)
        {
            Run r;
            if (solved || string.IsNullOrEmpty(id) || !run.TryGetValue(id, out r)) return false;
            if (colpevole < 0) { Info(S(9)); return false; }
            if (Mathf.RoundToInt(r.d.valore) != passoPista) return false;
            if (r.d.evento != "giusto") { Wrong(r.d.msg); return false; }
            Rivela(r.d.dopo, true);
            r.segnata = true; Evidenzia(r, true);
            passoPista++;
            UISfx.Play(Sfx.Chime, 0.45f);
            if (passoPista >= passiPista) { Solved(); return true; }
            Info(S(10));
            return true;
        }

        // ------------------------------------------------------------------ melodia (ascolta e ripeti)
        int frase, posNota;
        bool fraseSentita, suonando;
        float lentezza = 1f;

        public int Frase { get { return frase; } }
        public int PosizioneNota { get { return posNota; } }
        public bool StaSuonando { get { return suonando; } }

        void BuildMelodia()
        {
            frase = 0; posNota = 0; fraseSentita = false; suonando = false; lentezza = 1f;
            StartCoroutine(TempoDiRivo());
        }

        FraseDef FraseCorrente { get { return def.frasi != null && frase < def.frasi.Count ? def.frasi[frase] : null; } }

        /// <summary>Rivo, dal banco, batte il tempo con la mano (solo un movimento: nessun testo).</summary>
        IEnumerator TempoDiRivo()
        {
            Run r;
            if (!run.TryGetValue("rivo_mano", out r) || r.go == null) yield break;
            float t = 0f;
            while (!closed)
            {
                t += Time.unscaledDeltaTime;
                var f = FraseCorrente;
                float per = (f != null ? f.tempo : 0.55f) * lentezza;
                float fase = Mathf.Repeat(t / Mathf.Max(0.2f, per), 1f);
                r.go.transform.localPosition = r.pos0 + new Vector3(0f, fase < 0.2f ? 0f : 0.03f * Mathf.Sin(Mathf.PI * (fase - 0.2f) / 0.8f), 0f);
                yield return null;
            }
        }

        /// <summary>Alcide suona la frase corrente (piu' lenta dopo un errore). Pubblico per la prova.</summary>
        public bool AscoltaFrase()
        {
            if (solved || FraseCorrente == null) return false;
            if (suonando) { Info(S(5)); return false; }
            StartCoroutine(SuonaFrase());
            return true;
        }

        IEnumerator SuonaFrase()
        {
            var f = FraseCorrente;
            if (f == null) yield break;
            suonando = true;
            posNota = 0;
            float per = Mathf.Max(0.25f, f.tempo * lentezza);
            for (int k = 0; k < f.note.Count && !closed; k++)
            {
                int n = f.note[k];
                AccendiNota(n, per * 0.85f);
                // il pallino dello spartito della frase si accende insieme
                Run p;
                if (run.TryGetValue("frase_" + frase + "_" + k, out p)) StartCoroutine(Lampo(p, per * 0.85f));
                yield return new WaitForSecondsRealtime(per);
            }
            suonando = false;
            fraseSentita = true;
            if (!closed && !solved) Info(S(1));
        }

        void AccendiNota(int i, float dur)
        {
            if (def.note != null && i >= 0 && i < def.note.Count) Suona(ClipNota(def.note[i], Mathf.Clamp(dur, 0.25f, 1.2f)));
            var r = CercaRun("nota", i);
            if (r != null) StartCoroutine(Lampo(r, dur));
        }

        readonly Dictionary<Run, Vector3> scalaLampo = new Dictionary<Run, Vector3>();

        IEnumerator Lampo(Run r, float dur)
        {
            if (r == null || r.go == null) yield break;
            Vector3 s0;
            if (!scalaLampo.TryGetValue(r, out s0)) { s0 = r.go.transform.localScale; scalaLampo[r] = s0; }
            Evidenzia(r, true);
            r.go.transform.localScale = Vector3.Scale(s0, new Vector3(1.35f, 1f, 1.35f));
            yield return new WaitForSecondsRealtime(dur);
            if (r.go == null) yield break;
            r.go.transform.localScale = s0;
            Evidenzia(r, false);
        }

        /// <summary>Amerigo tocca il segno colorato i. Pubblico per la prova.</summary>
        public bool SuonaNota(int i)
        {
            var f = FraseCorrente;
            if (solved || f == null || def.note == null || i < 0 || i >= def.note.Count) return false;
            if (suonando) { Info(S(5)); return false; }
            AccendiNota(i, 0.45f);
            if (!fraseSentita) { Info(S(4)); return false; }
            if (f.note[posNota] != i)
            {
                posNota = 0;
                fraseSentita = false;
                lentezza = Mathf.Min(2.2f, lentezza * 1.5f);
                Wrong(S(2));
                StartCoroutine(RifaiLenta());
                return false;
            }
            posNota++;
            if (posNota < f.note.Count) { Info(string.Format(S(3), f.note.Count - posNota)); return true; }
            Rivela("frase_" + frase + "_ok", true);
            UISfx.Play(Sfx.Chime, 0.5f);
            string ok = f.ok;
            frase++; posNota = 0; fraseSentita = false; lentezza = 1f;
            if (FraseCorrente == null) { Info(ok); Solved(); return true; }
            Rivela("g:frase_" + frase, true);
            Info(ok);
            return true;
        }

        IEnumerator RifaiLenta()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            if (!closed && !solved && !suonando) StartCoroutine(SuonaFrase());
        }
    }

    /// <summary>R015: mette in pausa la musica di scena finche' la vista ravvicinata e' aperta (si sente bene quello che si deve ascoltare).</summary>
    public sealed class MusicaSospesa : MonoBehaviour
    {
        void OnEnable() { if (AudioManager.I != null && AudioManager.I.Music != null) AudioManager.I.Music.SetPaused(true); }
        void OnDestroy() { if (AudioManager.I != null && AudioManager.I.Music != null) AudioManager.I.Music.SetPaused(false); }
    }
}
