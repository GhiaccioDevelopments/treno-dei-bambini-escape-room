# R021 – Grafica realistica al massimo, con un'identità che non somiglia a nessun altro gioco

| | |
|---|---|
| **Scritta da** | Claude, PC casa, 2026-11-07 |
| **Per** | Claude con Unity 6000.6.7f1 + MCP for Unity |
| **Base** | `feature/upgrade-realistico` @ 1af2cc3. **Il codice attuale vince** |
| **Dipende da** | **R007** (in corso: tappa 1 luce/cielo/post fatta; tappe 2 modelli 3D veri, 3 materiali/decal, 4 pioggia, 5 FPS). R021 alza l'asticella **sopra** R007: si fa dopo R007 o si fonde nelle sue tappe 2-5. Dopo R017 (mappe pulite) |
| **Priorità** | Alta |

## 1. Cosa ha chiesto l'utente (parole sue)
> Migliora al 100% la grafica, deve essere realistica, diversa da tutti.

Due obiettivi: **realismo** (materiali, luce, dettaglio, modelli veri) e **identità** (riconoscibile a colpo d'occhio,
non "l'ennesimo gioco Unity").

## 2. Vincoli (CLAUDE.md)
Tono serio e cinematografico, neorealismo del dopoguerra, nessun oggetto moderno; luce baked; ≥ 60 FPS a Alta su PC normale,
fluido a Bassa e su WebGL; LFS sotto controllo (avvisare verso 850 MB); asset CC0/CC-BY in `CREDITS.md`.

## 3. Identità visiva proposta: "pellicola neorealista"
Il gioco deve sembrare **un film italiano del 1946 girato a colori che non esistevano**: realismo fotografico filtrato
dalla pellicola dell'epoca. Elementi firma (da proporre all'utente con screenshot prima/dopo):
1. **Emulazione di pellicola** (post-process URP personalizzato, leggero): grana fotochimica che varia con la luminanza,
   *halation* (alone caldo attorno alle luci forti), leggerissima aberrazione e vignettatura di un obiettivo d'epoca,
   curva tonale "stampa" (neri morbidi, alte luci compresse). Intensità per preset.
2. **Tre "pellicole" per tre tempi**: 1946 presente del gioco = colore caldo desaturato; **ricordi** = virato seppia con
   *gate weave* (micro-tremolio del fotogramma), polvere e graffi rari, bordi sfumati; **1994** = colore freddo e neutro, più
   nitido (TRAMA §7.1).
3. **Luce di Napoli**: sole basso che entra a lame nei vicoli, rimbalzo caldo del tufo giallo, ombre blu; bucato steso che
   filtra la luce; polvere sospesa nei fasci (particelle leggere, non volumetrica).
4. **Materia**: tufo giallo napoletano, piperno grigio, basoli di pietra lavica lucidati dal passaggio, intonaci scrostati a
   più strati di colore, legno consumato, latta arrugginita. Texture **PBR complete** fotogrammetriche (CC0: Poly Haven,
   ambientCG), decal di umidità/sporco/salnitro alla base dei muri, macchie d'acqua.
5. **Racconto nel set**: bucato, edicole votive, sedie fuori dai bassi, gabbie con uccellini, manifesti del referendum 1946
   (testi nostri in `testi_ambiente.json`), macerie e puntellature di guerra.
6. **Personaggi**: pelle con subsurface semplice, tessuti con microdettaglio (normal di trama), sporco e usura coerenti.

## 4. Realismo tecnico (senza perdere FPS)
- Lightmap ad alta qualità con AO baked, light probe densi nei vicoli, reflection probe box-projected negli interni.
- Ombre a contatto (SSAO da Media), screen-space shadows dove convengono; LOD e impostori per le facciate lontane.
- Cielo: HDRI d'epoca credibile (coperto/pomeriggio, scelte di R007) + nuvole che si muovono lentamente.
- Dettaglio vicino alla camera: parallax/height leggero sui basoli e sugli intonaci (solo Alta/Ultra).
- Tutto per preset (Bassa → Ultra) e misurato: tabella FPS e peso build per preset nel Rapporto.

## 5. Ordine di lavoro consigliato
1. Concept: 3 "frame di riferimento" (vicolo di giorno, interno del basso, ricordo seppia) come screenshot di prova
   → **ok dell'utente sull'identità** (chiedere con il pannello) prima di estenderla.
2. Post-process "pellicola" e tre look per i tempi. 3. Materiali fotogrammetrici e decal (con R007 tappa 3).
4. Set dressing narrativo. 5. Personaggi. 6. Misure FPS/peso, un solo bake finale, confronto prima/dopo.

## 6. Criteri di accettazione
- [ ] L'utente ha approvato l'identità visiva sui 3 frame di riferimento.
- [ ] Confronto prima/dopo (stessa inquadratura) per hub, basso e flashback in `Docs/richieste/media/R021/` (JPG ≤ 1280×720).
- [ ] Look distinti per 1946, ricordi e 1994 (quest'ultimo almeno impostato come profilo).
- [ ] ≥ 60 FPS a Alta su PC normale, fluido a Bassa e WebGL; peso LFS annotato; asset in `CREDITS.md`.
- [ ] Nessun elemento moderno; console pulita.

## Rapporto
_(da compilare a fine lavoro)_
