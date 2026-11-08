using System;
using System.Collections.Generic;
using TrenoBambini.Data;
using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>Loader dei contenuti JSON (Resources/Data). Tollerante: file mancante = warning, nessuna eccezione.</summary>
    public static class GameData
    {
        static List<LuogoDef> luoghi;
        static List<OggettoDef> oggetti;
        static List<PersonaggioDef> personaggi;
        static AudioFile audio;
        static UiMenuFile uiMenu;
        static CreditiFile crediti;
        static List<EnigmaDef> enigmiMondo;
        static List<NascostoDef> oggettiEnigma;
        static readonly Dictionary<string, FlashbackDef> flashbacks = new Dictionary<string, FlashbackDef>();
        static readonly Dictionary<string, CutsceneDef> cutscenes = new Dictionary<string, CutsceneDef>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            luoghi = null; oggetti = null; personaggi = null; audio = null; uiMenu = null; crediti = null; enigmiMondo = null; oggettiEnigma = null;
            flashbacks.Clear(); cutscenes.Clear();
        }

        /// <summary>R023: testi di menu iniziale e inventario (ui_menu.json).</summary>
        public static UiMenuFile UiMenu { get { if (uiMenu == null) uiMenu = (LoadJson<UiMenuFile>("ui_menu") ?? new UiMenuFile()).Normalizza(); return uiMenu; } }

        /// <summary>R023: asset esterni e licenze, generati da CREDITS.md (Treno > Crediti > Genera crediti.json).</summary>
        public static CreditiFile Crediti { get { if (crediti == null) crediti = LoadJson<CreditiFile>("crediti") ?? new CreditiFile(); return crediti; } }

        /// <summary>R009/R020: enigmi degli oggetti nel mondo di hub e basso (enigmi_mondo.json). Ognuno ha il campo scena.</summary>
        public static List<EnigmaDef> EnigmiMondo
        {
            get
            {
                if (enigmiMondo == null)
                {
                    var f = LoadJson<EnigmiMondoFile>("enigmi_mondo");
                    enigmiMondo = (f != null && f.enigmi != null) ? f.enigmi : new List<EnigmaDef>();
                }
                return enigmiMondo;
            }
        }

        /// <summary>Oggetti/indizi che si ottengono negli enigmi (nome e descrizione), dal file del mondo e dagli oggetti nascosti dei ricordi.</summary>
        public static List<NascostoDef> OggettiEnigma
        {
            get
            {
                if (oggettiEnigma == null)
                {
                    oggettiEnigma = new List<NascostoDef>();
                    var f = LoadJson<EnigmiMondoFile>("enigmi_mondo");
                    if (f != null && f.oggetti != null) oggettiEnigma.AddRange(f.oggetti);
                    foreach (var o in Oggetti)
                    {
                        if (string.IsNullOrEmpty(o.flashback)) continue;
                        var fb = Flashback(o.flashback);
                        if (fb == null) continue;
                        foreach (var e in fb.enigmi) if (e.oggetti != null) oggettiEnigma.AddRange(e.oggetti);
                    }
                }
                return oggettiEnigma;
            }
        }

        public static List<LuogoDef> Luoghi
        {
            get
            {
                if (luoghi == null)
                {
                    var f = LoadJson<LuoghiFile>("luoghi");
                    luoghi = (f != null && f.luoghi != null) ? f.luoghi : new List<LuogoDef>();
                }
                return luoghi;
            }
        }

        public static List<OggettoDef> Oggetti
        {
            get
            {
                if (oggetti == null)
                {
                    var f = LoadJson<OggettiFile>("oggetti");
                    oggetti = (f != null && f.oggetti != null) ? f.oggetti : new List<OggettoDef>();
                }
                return oggetti;
            }
        }

        public static List<PersonaggioDef> Personaggi
        {
            get
            {
                if (personaggi == null)
                {
                    var f = LoadJson<PersonaggiFile>("personaggi");
                    personaggi = (f != null && f.personaggi != null) ? f.personaggi : new List<PersonaggioDef>();
                }
                return personaggi;
            }
        }

        /// <summary>Configurazione audio (audio.json). Mai null: se il file manca ritorna una configurazione vuota.</summary>
        public static AudioFile Audio
        {
            get
            {
                if (audio == null) audio = LoadJson<AudioFile>("audio") ?? new AudioFile();
                return audio;
            }
        }

        public static OggettoDef Oggetto(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var l = Oggetti;
            for (int i = 0; i < l.Count; i++) if (l[i].id == id) return l[i];
            return null;
        }

        public static LuogoDef Luogo(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var l = Luoghi;
            for (int i = 0; i < l.Count; i++) if (l[i].id == id) return l[i];
            return null;
        }

        public static PersonaggioDef Personaggio(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var l = Personaggi;
            for (int i = 0; i < l.Count; i++) if (l[i].id == id) return l[i];
            return null;
        }

        public static FlashbackDef Flashback(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            FlashbackDef d;
            if (flashbacks.TryGetValue(id, out d)) return d;
            d = LoadJson<FlashbackDef>(id);
            if (d == null && !id.StartsWith("flashback_")) d = LoadJson<FlashbackDef>("flashback_" + id);
            if (d == null) Debug.LogWarning("Flashback non trovato: " + id);
            flashbacks[id] = d;
            return d;
        }

        public static CutsceneDef Cutscene(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CutsceneDef d;
            if (cutscenes.TryGetValue(id, out d)) return d;
            d = LoadJson<CutsceneDef>(id);
            if (d == null && !id.StartsWith("cutscene_")) d = LoadJson<CutsceneDef>("cutscene_" + id);
            if (d == null) Debug.LogWarning("Cutscene non trovata: " + id);
            cutscenes[id] = d;
            return d;
        }

        static T LoadJson<T>(string name) where T : class
        {
            var ta = Resources.Load<TextAsset>("Data/" + name);
            if (ta == null) return null;
            try
            {
                return JsonUtility.FromJson<T>(ta.text.TrimStart('﻿'));
            }
            catch (Exception e)
            {
                Debug.LogWarning("JSON non valido (" + name + "): " + e.Message);
                return null;
            }
        }
    }
}
