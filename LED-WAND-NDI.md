# LED-Wand und NDI – Erkenntnisse aus dem Hallentest (27.11.2026, Halle win4)

Getestet am Streaming-PC `Grafik` (172.20.1.166) mit einem eigenen NDI-Testsender. Die LED-Wand läuft über sedna (`172.20.1.49:27182`), angesteuert vom Show-Agent.

## Kurzfassung

| Thema | Ergebnis |
|---|---|
| Live-Taste | `start playlist -n "STR_RA_Live" -o` – zeigt die NDI-Quelle **`REGIE (vMix - Output 1)`** |
| Replay-Taste | `start playlist -n "STR_RA_Replay" -o` – zeigt die NDI-Quelle **`REGIE (vMix - Output 2)`** (dorthin gesendet, auf der Wand bestätigt) |
| Zurück zum Spiel | `stop playlist -o -fo 0.5` |
| NDI-Signal | 1920×1080, 16:9 |
| **Sichtbare Fläche** | **1920 × 996 px** – volle Breite, oben vollständig, **unten fehlen 84 Zeilen** (996–1079) |
| Verzerrung | keine (Testkreis bleibt rund) |
| Schwarz im Signal | LEDs aus – schwarze Ränder sehen aus, als wäre die Wand kleiner |

## Welche Playlist welche Quelle zeigt

Die Zuordnung steht nur in den sedna-Playlists, nicht im Code. Der Code (`Hall.DefaultLedTemplates` in `RedAnts.Show/Domain/Hall.cs`) kennt nur die Playlist-Namen. Beobachtet:

- `STR_RA_Live` → `REGIE (vMix - Output 1)` – ein Testbild unter diesem Namen erschien bei Live.
- `STR_RA_Replay` → vermutlich `REGIE (vMix - Output 2)`; im Test mit zwei unterschiedlichen Bildern auf Output 1/2 geprüft.

NDI-Quellnamen haben die Form `RECHNERNAME (Quellname)`. Die Playlists erwarten den Rechnernamen `REGIE`. Ein anderer Rechner kann diese Quellen nur vortäuschen, wenn er den NDI-Rechnernamen überschreibt (siehe unten). Läuft gleichzeitig der echte Regie-Rechner mit vMix, gibt es die Namen doppelt.

## Sichtbare Fläche der Wand

Mit einem Rasterbild (16×9 Felder à 120 px) und einem Messbild (Säulen mit rotem Endbalken in 2-px-Schritten) ausgemessen:

- Links, rechts, oben: alles sichtbar (auch die Balken mit 0 px Randabstand).
- Unten: Säule bis Zeile 996 sichtbar, Säule bis 998 nicht. **Letzte sichtbare Zeile: 995.**
- Das gilt genauso für das echte vMix-Signal: Auch dort sieht man unten 84 Zeilen nicht. Wenn das stört, muss die Abbildung des NDI-Signals auf die Wand in sedna korrigiert werden.

### Vorgaben für Grafiken auf der Wand

| Variante | Grösse | Hinweis |
|---|---|---|
| A – nur sichtbare Fläche | **1920 × 996 px** | füllt die Wand exakt |
| B – volles Signal (empfohlen) | **1920 × 1080 px** | unterste 84 px nur Hintergrund, kein Text; passt auch, falls sedna später korrigiert wird |

- Text etwa 20 px vom Rand weg halten.
- Schrift eher gross (ab ca. 40 px Höhe): Die Wand hat weniger physische Pixel als 1920×1080, kleine Schrift (z.B. 17 px) war auf Fotos nicht lesbar.
- 4:3-Grafiken bekommen schwarze Seitenränder, die auf der Wand wie „keine Wand“ aussehen. Besser direkt in 16:9 gestalten.
- Gemini liefert keine exakten Pixelmasse: „16:9 Querformat, unten ca. 8 % Rand ohne Text“ verlangen.

## Umschalten Live / Replay / Spiel

Wechseltest am 27.11.2026, 18:47–18:48: 5 Runden Live (6 s) → Replay (6 s) → Spiel (4 s), Befehle wie der Agent sie schickt (eine TCP-Verbindung pro Befehl, Zeile mit `\n`). Alle 16 Befehle wurden von sedna angenommen. Wie sauber die Übergänge auf der Wand aussahen (Schwarz, falsches Bild, Verzögerung beim NDI-Aufbau), ist noch nicht ausgewertet.

## Offen: Taste, die ein bestimmtes Bild zeigt

Ziel: Ein Tastendruck soll ein bestimmtes Bild auf NDI legen **und** im selben Moment die Wand umschalten. Der Agent kennt dafür heute keinen Schritt – Tasten können nur `musik.*`, `vmix.ein/text/aus` und `led.start/stopp` (`RedAnts.Show/Domain/Deck.cs`). Mögliche Wege, noch nicht entschieden:

1. **Neuer Agent-Schritt** (z.B. `ndi.bild`): Der Agent sendet selbst eine NDI-Quelle, wechselt das Bild und schickt gleichzeitig den sedna-Befehl. Unabhängig von vMix; braucht eine eigene sedna-Playlist für diese Quelle.
2. **Über vMix**: `vmix.ein` blendet einen Titel mit dem Bild ein, `led.start live` schaltet die Wand. Keine Code-Änderung, aber nur mit verbundenem vMix, und das Bild liegt auf dem vMix-Output (vermutlich auch im Livestream).
3. **Lokaler Steuerdienst** neben dem Agent – nur für Tests sinnvoll.

## NDI-Testsender (lokal auf `Grafik`)

Liegt in `C:\Users\Livestream\NdiTestbild` (nicht im Repo), .NET 8, nutzt die installierte NDI-6-Runtime und Windows Media Foundation (kein ffmpeg).

| Aufruf | Wirkung |
|---|---|
| `NdiTestbild.exe --list` | alle sichtbaren NDI-Quellen auflisten |
| `--machine REGIE --name "vMix - Output 1"` | Quellname; Standard ist genau dieser |
| (ohne Bildoption) `--title "X" --bg "#1E50C8"` | bewegtes Testbild mit Uhr und Bildzähler |
| `--image <datei>` | Standbild, eingepasst |
| `--video <datei>` | Video in Endlosschleife, ohne Ton |
| `--fill` | randlos füllen statt einpassen |
| `--visible-height 996` | Inhalt in die sichtbaren 996 Zeilen setzen, darunter schwarz |
| `--stretch-edges 280` | schwarze Seitenränder füllen, indem nur die äussersten 280 Quellpixel (Hintergrund) gestreckt werden |
| `--calib` | Rasterbild A1–P9 mit Pixelkoordinaten |
| `--ruler` | Messbild für die Ränder (Säulen 976–1010, Randbalken 0–16 px) |

Startdateien: `Bild-und-Video-starten.cmd`, `Kalibrierung-starten.cmd`, `Messung-unterer-Rand-starten.cmd`, `Wechseltest.ps1`.

Technische Erkenntnisse:

- **Rechnername überschreiben:** NDI liest `ndi-config.v1.json` aus dem Ordner in `NDI_CONFIG_DIR`; `{ "ndi": { "machinename": "REGIE" } }` lässt den Sender als `REGIE (…)` erscheinen, obwohl der PC `Grafik` heisst. Funktioniert mit NDI 6.
- **Video-Cache:** Das Video wird einmal decodiert, skaliert und als UYVY-Rohbilder (BT.709) in `out\cache\` gespeichert (≈ 4 MB pro Bild, ≈ 95 MB pro Sekunde bei 24 fps; 10 s Clip = 949 MB) und danach per Memory-Mapping gesendet. CPU-Last sank von 8.7 % (Decodieren zur Laufzeit) auf 0.8 % (16 Kerne). Die NDI-Kompression selbst lässt sich nicht vorberechnen.
- **Standbild** in BGRA kostet ca. 2.5 % CPU, weil NDI jedes Bild umrechnet.
- `Bitmap.Clone` kopiert die Pixel nicht wirklich – bei fremdem Speicher (Media-Foundation-Puffer) gibt das eine AccessViolation. Zeilen selbst kopieren.
