using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TrenoBambini.Audio;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Abs(float a) { return Math.Abs(a); }
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Cos(float a) { return (float)Math.Cos(a); }
        public static float Exp(float a) { return (float)Math.Exp(a); }
        public static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }
        public static int Clamp(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return Clamp(v, 0f, 1f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float InverseLerp(float a, float b, float v) { return a != b ? Clamp01((v - a) / (b - a)) : 0f; }
        public static float MoveTowards(float c, float t, float d) { return Math.Abs(t - c) <= d ? t : c + Math.Sign(t - c) * d; }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
    }
    public class AudioClip
    {
        public static AudioClip Create(string n, int len, int ch, int rate, bool stream) { return new AudioClip(); }
        public void SetData(float[] d, int off) { }
    }
}

/// <summary>
/// Pre-generatore dei file audio (R002): compila gli stessi Synth.cs / SfxSynth.cs / MusicSynth.cs del gioco (con un Mathf minimo)
/// e scrive WAV mono 22050 Hz, identici alla sintesi a runtime (stessi seed). Uso: vedi genera.sh (poi encode_ogg.py -> .ogg).
/// Senza argomenti genera i brani e gli effetti pesanti che il gioco carica da file; con id come argomenti, solo quelli
/// ("tutti" = tutti gli id sintetici, per misure).
/// </summary>
static class Program
{
    static readonly string[] Pregenerati =
    {
        "tema_amerigo_solo", "tema_amerigo_ricordo", "tema_amerigo_scoperta", "vicolo_mandolino", "stinger_risolto",
        "vicolo_brusio", "vento_mare", "ricordo_tono", "stazione_vapore", "interno_silenzio",
        "violino_sale", "porta_apri", "campane", "venditore"
    };

    static void WriteWav(string path, float[] data)
    {
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int bytes = data.Length * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Synth.Rate); w.Write(Synth.Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            for (int i = 0; i < data.Length; i++) w.Write((short)Math.Round(Math.Clamp(data[i], -1f, 1f) * 32767f));
        }
    }

    static void Report(string id, float[] d, long ms)
    {
        double peak = 0, sq = 0;
        for (int i = 0; i < d.Length; i++) { double a = Math.Abs(d[i]); if (a > peak) peak = a; sq += d[i] * d[i]; }
        Console.WriteLine($"{id}\t{d.Length / (double)Synth.Rate:F2}s\tpeak={peak:F3}\trms={Math.Sqrt(sq / d.Length):F3}\t{ms}ms");
    }

    static int Main(string[] args)
    {
        string dir = args[0];
        Directory.CreateDirectory(dir);
        var ids = new List<string>();
        for (int i = 1; i < args.Length; i++) ids.Add(args[i]);
        if (ids.Count == 0) ids.AddRange(Pregenerati);
        else if (ids.Count == 1 && ids[0] == "tutti")
        {
            ids.Clear();
            ids.AddRange(SfxSynth.AllIds);
            ids.AddRange(new[] { MusicSynth.Solo, MusicSynth.Ricordo, MusicSynth.Scoperta, MusicSynth.Vicolo, MusicSynth.StingerRisolto });
        }
        foreach (var id in ids)
        {
            var sw = Stopwatch.StartNew();
            float[] buf = null;
            if (MusicSynth.IsMusicId(id)) { var e = MusicSynth.RenderBuffer(id, b => buf = b); while (e.MoveNext()) { } }
            else buf = SfxSynth.BuildSamples(id);
            if (buf == null) { Console.WriteLine("?? " + id); continue; }
            Report(id, buf, sw.ElapsedMilliseconds);
            WriteWav(Path.Combine(dir, id + ".wav"), buf);
        }
        return 0;
    }
}
