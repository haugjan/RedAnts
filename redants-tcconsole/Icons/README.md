# TCU Stream Deck Icons

24 Icons (17 Motive, 7 davon zusätzlich als Alternativ-Variante `-b`) für die
Stream-Deck-/Companion-Buttons — als SVG-Quelle und als transparentes PNG in
5 Farbvarianten.

## Format

| Eigenschaft | Wert |
|---|---|
| Grösse | 144 × 144 px (Stream Deck rendert Keys mit 72/96 px – 144 px skaliert sauber herunter) |
| Hintergrund | transparent (Button-Farbe scheint durch) |
| Motivbereich | y = 8 … 96 px, d. h. **oberes ⅔** |
| Freier Bereich | untere ~48 px (⅓) bleiben leer → Platz für 1–2 Zeilen Button-Text |

Der Button-Titel wird in Companion/Stream Deck auf **unten** ausgerichtet und
überdeckt das Icon dadurch nie.

## Farbvarianten

Jedes Icon hat zwei Farbslots: eine **Basisfarbe** und eine **Akzentfarbe** für
das semantisch wichtigste Element (Stock, erhobener Arm, Stern, Play-Dreieck …).

| Ordner | Basis | Akzent | gedacht für |
|---|---|---|---|
| `png/dark`  | `#1A1A1A` | `#1A1A1A` | einfarbig, helle Button-Farben |
| `png/light` | `#FFFFFF` | `#FFFFFF` | einfarbig, dunkle Button-Farben |
| `png/red`   | `#1A1A1A` | `#E4002B` | zweifarbig, helle Button-Farben |
| `png/gold`  | `#FFFFFF` | `#FFD700` | zweifarbig, dunkle Button-Farben |
| `png/team`  | `#E4002B` | `#1A1A1A` | zweifarbig, helle/neutrale Button-Farben |

Weitere Kombinationen: in `render.ps1` unter `$variants` eine Zeile ergänzen und
neu rendern — die SVGs müssen dafür nicht angefasst werden.

## Ordner

```
Icons/
├─ svg/            Quellen (viewBox 0 0 144 144)
├─ png/<variante>/ je 24 PNGs, transparent
├─ preview.html    alle Icons in allen Varianten auf echten Button-Farben
└─ render.ps1      Rendert svg/ → png/
```

## Icon-Liste

| Datei | Motiv | Akzent |
|---|---|---|
| `ball` | Unihockeyball mit Lochung | Löcher |
| `coach` | Spielboard mit Feld und Laufweg | Laufweg-Pfeil |
| `coach-b` | Spielboard mit Feld, Taktik X/O | X und O |
| `timeout` | zwei Hände formen ein T | obere Hand |
| `timeout-b` | schlichtes T-Zeichen | Querbalken |
| `aufstellung` | Liste mit Nummern-Badges | Badges |
| `strafe` | Schiri-Piktogramm, gestreiftes Trikot, Arm gerade hoch | erhobener Arm |
| `strafe-b` | erhobene Hand mit gestreiftem Schiri-Ärmel | Ärmel |
| `spieler` | Spieler-Silhouette in Spielhaltung mit Stock | Stock |
| `spieler-b` | Trikot | Brustringe |
| `bestplayer` | Medaille am Band | Stern |
| `bestplayer-b` | Pokal mit Stern | Stern |
| `starting6` | Feld mit 6 Positionen | Positionen |
| `meldungen` | Megafon | Schallwellen |
| `kommentar` | Kommentatoren-Headset | Mikrofonbügel |
| `kommentar-b` | zwei Kommentatoren mit Headset | Mikrofonbügel |
| `schiedsrichter` | Trillerpfeife | Schallstriche |
| `schiedsrichter-b` | Büste mit gestreiftem Trikot + Pfeife | Pfeife |
| `drittel` | Kreis mit gefülltem Drittel | gefülltes Drittel |
| `highlights` | Funkeln / Sparkles | kleine Sterne |
| `resultat` | Anzeigetafel „3:2“ | Ziffern |
| `opener` | Bildschirm mit Play | Play-Dreieck |
| `einblender-ein` | Bauchbinde sichtbar | Bauchbinde |
| `einblender-aus` | Bauchbinde durchgestrichen | Strich |

## Neu rendern

```powershell
cd Icons
.\render.ps1                            # 144x144 -> png\<variante>\
.\render.ps1 -Size 72 -OutDir png72     # Stream Deck Classic
.\render.ps1 -Size 96 -OutDir png96     # Stream Deck XL nativ
.\render.ps1 -Only strafe,spieler       # nur einzelne Icons
```

Das Skript nutzt Chrome oder Edge im Headless-Modus, es ist kein zusätzliches
Tool nötig.

## Technik der SVG-Quellen

* Basisfarbe = `currentColor`, Akzentfarbe = `var(--accent)`.
* Akzent-Elemente tragen die Klasse `.af` (Fläche) bzw. `.as` (Strich);
  die Regeln dazu stehen in jedem SVG im `<style>`-Block.
* Akzentteile liegen nie deckungsgleich auf Basisflächen — dadurch bleiben die
  einfarbigen Varianten (`dark`/`light`) vollständig lesbar.
* Details wie Trikotstreifen sind echte Aussparungen (`fill-rule="evenodd"`),
  damit sie auf jedem Hintergrund funktionieren.

## Einbinden

* **Bitfocus Companion**: Button → *Appearance* → PNG hochladen, Text-Position „bottom“.
* **Elgato Stream Deck**: PNG auf die Taste ziehen, Titel-Ausrichtung „unten“.
