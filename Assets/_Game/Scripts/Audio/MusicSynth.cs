using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// "Tema di Amerigo": melodia ORIGINALE per violino (La minore, 12 battute, ♩ = 72) e le sue varianti.
    /// La partitura è qui sotto in forma testuale (stessa melodia di Assets/Audio/Musica/sorgenti/tema_amerigo.abc e .mid)
    /// e viene resa con un violino sintetico, mandolino, pianoforte e campanelle, con riverbero e patina d'epoca applicati al file.
    /// Se in Assets/Audio/Musica c'è una registrazione vera con lo stesso id (violino dal vivo), vince quella (vedi AudioLibrary).
    /// Nessun legame con le musiche del film o del libro: melodia, armonie e orchestrazione sono nuove.
    /// </summary>
    public static class MusicSynth
    {
        public const string Solo = "tema_amerigo_solo";
        public const string Ricordo = "tema_amerigo_ricordo";
        public const string Scoperta = "tema_amerigo_scoperta";
        public const string Vicolo = "vicolo_mandolino";
        public const string StingerRisolto = "stinger_risolto";

        public const float Bpm = 72f;
        static float Eighth { get { return 60f / Bpm / 2f; } }

        /// <summary>Melodia: "nota:durata in ottavi", le battute da 8 ottavi sono separate da '|'. 'z' = pausa.</summary>
        public const string MelodyScore =
            "A4:2 C5:3 B4:1 A4:2 | G#4:2 B4:3 A4:1 G4:2 | E4:4 A4:2 C5:2 | B4:6 z:2 | " +
            "E5:3 D5:1 C5:2 B4:2 | C5:3 B4:1 A4:2 Bb4:2 | A4:4 G#4:2 B4:2 | A4:6 z:2 | " +
            "E5:4 A5:2 G5:2 | F5:3 E5:1 D5:2 C5:2 | B4:3 C5:1 D5:2 G#4:2 | A4:8";

        // accordi per battuta (MIDI): Am E Am E | Am Bb(napoletana) E Am | Am F E Am
        static readonly int[] Am = { 45, 57, 60, 64 };
        static readonly int[] E = { 40, 52, 56, 59 };
        static readonly int[] Bb = { 46, 58, 62, 65 };
        static readonly int[] F = { 41, 53, 57, 60 };
        static readonly int[][] Chords = { Am, E, Am, E, Am, Bb, E, Am, Am, F, E, Am };

        public static bool IsMusicId(string id)
        {
            return id == Solo || id == Ricordo || id == Scoperta || id == Vicolo || id == StingerRisolto;
        }

        // ------------------------------------------------------------ partitura
        static int NoteToMidi(string name)
        {
            int semi;
            switch (name[0])
            {
                case 'C': semi = 0; break;
                case 'D': semi = 2; break;
                case 'E': semi = 4; break;
                case 'F': semi = 5; break;
                case 'G': semi = 7; break;
                case 'A': semi = 9; break;
                default: semi = 11; break;
            }
            int i = 1;
            if (i < name.Length && name[i] == '#') { semi++; i++; }
            else if (i < name.Length && name[i] == 'b') { semi--; i++; }
            int oct = name[i] - '0';
            return 12 * (oct + 1) + semi;
        }

        /// <summary>Melodia come note (tempo in secondi) a partire da offset secondi.</summary>
        public static List<Synth.Note> Melody(float offset, int transpose, float vel)
        {
            var list = new List<Synth.Note>();
            float t = offset;
            float prev = 0f;
            var toks = MelodyScore.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int k = 0; k < toks.Length; k++)
            {
                var parts = toks[k].Split(':');
                float dur = int.Parse(parts[1]) * Eighth;
                if (parts[0] != "z")
                {
                    int midi = NoteToMidi(parts[0]) + transpose;
                    var n = new Synth.Note { midi = midi, start = t, dur = dur * 0.98f, vel = vel };
                    // portamento leggero sui salti ascendenti di terza o più (inflessione da canzone napoletana)
                    if (prev > 0f && midi - prev >= 3f && midi - prev <= 8f) n.glideFrom = prev + (midi - prev) * 0.6f;
                    list.Add(n);
                    prev = midi;
                }
                t += dur;
            }
            return list;
        }

        public static float PassSeconds { get { return 12 * 8 * Eighth; } }

        /// <summary>Testo ABC della melodia (per il file di partitura nel progetto).</summary>
        public static string AbcText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("X:1");
            sb.AppendLine("T:Tema di Amerigo");
            sb.AppendLine("C:originale per il gioco \"Il treno dei bambini\" (Ghiaccio Developments)");
            sb.AppendLine("M:4/4");
            sb.AppendLine("L:1/8");
            sb.AppendLine("Q:1/4=72");
            sb.AppendLine("K:Am");
            sb.AppendLine("%%MIDI program 40");
            string abc = "A2 c3 B A2 | ^G2 B3 A G2 | E4 A2 c2 | B6 z2 |\n" +
                         "e3 d c2 B2 | c3 B A2 _B2 | A4 ^G2 B2 | A6 z2 |\n" +
                         "e4 a2 g2 | f3 e d2 c2 | B3 c d2 ^G2 | A8 |]";
            sb.AppendLine(abc);
            return sb.ToString();
        }

        // ------------------------------------------------------------ resa
        static bool Over(Stopwatch sw)
        {
            if (sw.ElapsedMilliseconds < 4) return false;
            sw.Reset(); sw.Start();
            return true;
        }

        /// <summary>Rende una variante in modo asincrono (cede il controllo per non bloccare il frame). Alla fine chiama done(clip).</summary>
        public static IEnumerator Render(string id, Action<AudioClip> done)
        {
            float[] samples = null;
            var e = RenderBuffer(id, b => samples = b);
            while (e.MoveNext()) yield return null;
            var clip = Synth.ToClip(id, samples);
            if (done != null) done(clip);
        }

        /// <summary>Rende i campioni (mono, Synth.Rate) a blocchi: cede il controllo di continuo (note, riverbero, patina). Usata anche dallo strumento che pre-genera i file.</summary>
        public static IEnumerator RenderBuffer(string id, Action<float[]> done)
        {
            var rng = new Synth.Rng(0xA11CEu);
            var sw = Stopwatch.StartNew();
            float[] buf;

            if (id == Scoperta || id == StingerRisolto)
            {
                buf = Synth.Buffer(id == Scoperta ? 8.5f : 3.2f);
                if (id == Scoperta)
                {
                    // violino che sale per grado (La-Do-Mi-La) e si apre su un accordo, con celesta sopra
                    int[] seq = { 57, 60, 64, 69, 72 };
                    for (int i = 0; i < seq.Length; i++)
                    {
                        var n = new Synth.Note { midi = seq[i], start = 0.15f + i * 0.5f, dur = i == seq.Length - 1 ? 4.2f : 0.62f, vel = 0.8f };
                        Synth.Violin(buf, n, 1f, rng);
                        Synth.Bell(buf, n.start + 0.02f, seq[i] + 12, 0.35f, 2.4f);
                        if (Over(sw)) yield return null;
                    }
                    Synth.Violin(buf, new Synth.Note { midi = 64, start = 2.5f, dur = 4f, vel = 0.5f }, 1f, rng);
                    Synth.Violin(buf, new Synth.Note { midi = 76, start = 2.5f, dur = 4f, vel = 0.4f }, 1f, rng);
                    var rv = Synth.ReverbSteps(buf, 0.5f, 2.4f); while (rv.MoveNext()) yield return null;
                }
                else
                {
                    // pizzicato + campanella: piccola gioia
                    int[] seq = { 69, 72, 76, 81 };
                    for (int i = 0; i < seq.Length; i++)
                    {
                        Synth.Pluck(buf, new Synth.Note { midi = seq[i], start = i * 0.13f, dur = 0.5f, vel = 0.9f }, 1f, 0.7f, 0f, rng);
                        Synth.Bell(buf, i * 0.13f + 0.02f, seq[i] + 12, 0.28f, 1.6f);
                    }
                    var rv = Synth.ReverbSteps(buf, 0.3f, 1.4f); while (rv.MoveNext()) yield return null;
                }
                Synth.Normalize(buf, 0.75f);
                Synth.FadeEdges(buf, 0.002f, 0.25f);
            }
            else
            {
                int passes = id == Ricordo ? 2 : 1;
                float lead = 1.2f, tail = 3.5f;
                buf = Synth.Buffer(lead + PassSeconds * passes + tail);

                for (int pass = 0; pass < passes; pass++)
                {
                    float off = lead + pass * PassSeconds;
                    var mel = Melody(off, 0, 1f);
                    if (id == Solo)
                    {
                        for (int i = 0; i < mel.Count; i++) { var n = mel[i]; n.vel = 0.9f; Synth.Violin(buf, n, 1f, rng); if (Over(sw)) yield return null; }
                    }
                    else if (id == Ricordo)
                    {
                        // violino lontano e morbido; pianoforte in arpeggi; nel secondo giro il violino entra più piano e il piano cresce
                        float vg = pass == 0 ? 0.8f : 0.6f;
                        for (int i = 0; i < mel.Count; i++) { var n = mel[i]; n.vel = 0.75f; Synth.Violin(buf, n, vg, rng); if (Over(sw)) yield return null; }
                        int[] pattern = { 0, 2, 3, 2, 1, 2, 3, 2 };
                        for (int b = 0; b < 12; b++)
                        {
                            var ch = Chords[b];
                            for (int k = 0; k < 8; k++)
                            {
                                var pn = new Synth.Note { midi = ch[pattern[k]] + (pattern[k] == 0 ? 0 : 0), start = off + (b * 8 + k) * Eighth, dur = Eighth * 1.6f, vel = (k == 0 ? 0.55f : 0.32f) * (pass == 0 ? 0.8f : 1f) };
                                Synth.Piano(buf, pn, 0.8f, rng);
                                if (Over(sw)) yield return null;
                            }
                        }
                    }
                    else // Vicolo: mandolino (tremolo) sulla melodia, chitarra sui tempi forti, il violino risponde nelle ultime 4 battute
                    {
                        for (int i = 0; i < mel.Count; i++)
                        {
                            var n = mel[i]; n.vel = 0.55f;
                            Synth.Pluck(buf, n, 1f, 0.78f, 11f, rng);
                            if (Over(sw)) yield return null;
                        }
                        for (int b = 0; b < 12; b++)
                        {
                            var ch = Chords[b];
                            for (int beat = 0; beat < 4; beat += 2)
                                for (int c = 0; c < 4; c++)
                                    Synth.Pluck(buf, new Synth.Note { midi = ch[c], start = off + (b * 8 + beat * 2) * Eighth + c * 0.018f, dur = 0.9f, vel = 0.3f }, 0.8f, 0.6f, 0f, rng);
                            if (Over(sw)) yield return null;
                        }
                        for (int i = 0; i < mel.Count; i++)
                        {
                            if (mel[i].start - off < 8 * 8 * Eighth) continue;
                            var n = mel[i]; n.vel = 0.38f; n.midi -= 12f; if (n.glideFrom > 0f) n.glideFrom -= 12f;
                            Synth.Violin(buf, n, 1f, rng);
                            if (Over(sw)) yield return null;
                        }
                    }
                }
                if (Over(sw)) yield return null;
                float wet = id == Solo ? 0.5f : (id == Ricordo ? 0.38f : 0.2f);
                float room = id == Solo ? 2.4f : 1.6f;
                var rvb = Synth.ReverbSteps(buf, wet, room);
                while (rvb.MoveNext()) yield return null;
                if (id == Ricordo || id == Solo)
                {
                    // grammofono: è un ricordo (1), appena un velo di fruscio nel solo (0.25)
                    var pat = Synth.PatinaSteps(buf, rng, id == Ricordo ? 1f : 0.25f);
                    while (pat.MoveNext()) yield return null;
                }
                Synth.Normalize(buf, id == Vicolo ? 0.5f : 0.7f);
                // coda e inizio si fondono: loop senza clic
                buf = Synth.MakeLoop(buf, 2.5f);
            }

            if (done != null) done(buf);
        }

        /// <summary>Versione sincrona per gli strumenti dell'editor (esporta WAV).</summary>
        public static AudioClip RenderSync(string id)
        {
            AudioClip result = null;
            var e = Render(id, c => result = c);
            while (e.MoveNext()) { }
            return result;
        }
    }
}
