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
        static FilmatoFile filmato;
        static CreditiFile crediti;
        static List<EnigmaDef> enigmiMondo;
        static List<NascostoDef> oggettiEnigma;
        static EnigmiTesti testiEnigmi;
        static MappeFile mappe;
        static SegretiFile segreti;
        static readonly Dictionary<string, FlashbackDef> flashbacks = new Dictionary<string, FlashbackDef>();
        static readonly Dictionary<string, CutsceneDef> cutscenes = new Dictionary<string, CutsceneDef>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            luoghi = null; oggetti = null; personaggi = null; audio = null; uiMenu = null; crediti = null; enigmiMondo = null; oggettiEnigma = null; testiEnigmi = null; mappe = null; segreti = null;
            flashbacks.Clear(); cutscenes.Clear();
        }

        /// <summary>R019: minimappa, nomi dei vicoli e obiettivi (mappe.json). Mai null.</summary>
        public static MappeFile Mappe
        {
            get
            {
                if (mappe == null)
                {
                    mappe = LoadJson<MappeFile>("mappe") ?? new MappeFile();
                    if (mappe.scene == null) mappe.scene = new List<MappaScenaDef>();
                    if (mappe.obiettivi == null) mappe.obiettivi = new List<ObiettivoDef>();
                    if (mappe.parametri == null) mappe.parametri = new MappaParametri();
                    if (mappe.testi == null) mappe.testi = new MappaTesti();
                    if (mappe.testi.legenda == null) mappe.testi.legenda = new List<MappaLegendaVoce>();
                }
                return mappe;
            }
        }

        /// <summary>R011: segreti, collezionabili, enigmi segreti ed easter egg (segreti.json). Mai null.</summary>
        public static SegretiFile Segreti
        {
            get
            {
                if (segreti == null)
                {
                    segreti = LoadJson<SegretiFile>("segreti") ?? new SegretiFile();
                    if (segreti.testi == null) segreti.testi = new SegretiTesti();
                    if (segreti.stanze == null) segreti.stanze = new List<StanzaSegretiDef>();
                    if (segreti.segreti == null) segreti.segreti = new List<SegretoDef>();
                    if (segreti.pezzi == null) segreti.pezzi = new List<SegretoPezzoDef>();
                    if (segreti.enigmi == null) segreti.enigmi = new List<EnigmaDef>();
                    if (segreti.campane == null) segreti.campane = new List<CampanaDef>();
                    if (segreti.uova == null) segreti.uova = new List<UovoDef>();
                    if (segreti.gettoni == null) segreti.gettoni = new List<NascostoDef>();
                }
                return segreti;
            }
        }

        /// <summary>Descrizione della pianta di una scena (null se la scena non ne ha una).</summary>
        public static MappaScenaDef MappaScena(string scena)
        {
            if (string.IsNullOrEmpty(scena)) return null;
            var l = Mappe.scene;
            for (int i = 0; i < l.Count; i++) if (l[i].scena == scena) return l[i];
            return null;
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

        /// <summary>Testi dell'interfaccia degli enigmi (enigmi_mondo.json, blocco testi); mai null.</summary>
        public static EnigmiTesti TestiEnigmi
        {
            get
            {
                if (testiEnigmi == null)
                {
                    var f = LoadJson<EnigmiMondoFile>("enigmi_mondo");
                    testiEnigmi = (f != null && f.testi != null) ? f.testi : new EnigmiTesti();
                }
                return testiEnigmi;
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
                    oggettiEnigma.AddRange(Segreti.gettoni); // R011: biglietti e indizi dei segreti
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

        /// <summary>R025: regia del filmato iniziale (filmato_intro.json). Mai null.</summary>
        public static FilmatoFile Filmato
        {
            get { if (filmato == null) filmato = LoadJson<FilmatoFile>("filmato_intro") ?? new FilmatoFile(); return filmato; }
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
