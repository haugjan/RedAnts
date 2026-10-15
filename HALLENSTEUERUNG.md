# Hallensteuerung, Neustart vom 15.10.2026

Dieses Dokument legt fest, was gebaut wird, bevor gebaut wird. Es ist die Abnahmegrundlage.
Der erste Anlauf liegt als Archiv auf dem Tag `show-archiv-2026-10-15` (Branch `feature/s6-show-module`, 50 Commits, nie auf `main`, nie auf Prod).

## Warum neu

Der erste Anlauf hat die Musikbedienung im neuen Deck-Bord nachgebaut, statt das bestehende Soundboard zu verwenden. Daraus folgten die Fehler, die im Betrieb aufgefallen sind: eine erfundene dritte Registerkarte, Musik, die Sekunden nach dem Klick startet (Umweg Browser, Cloud, Agent, zurück, dann erst Download), und ein sichtbarer Audio-Player, den das Soundboard nie hatte. Ursache waren zwei Festlegungen, die nie hinterfragt wurden: kein JavaScript im neuen Modul, und das Bord bildet alles ab, auch die Musik.

## Schnitt

| Bereich | Wer | Status |
|---|---|---|
| Musik | das bestehende Soundboard (`/dj`) | bleibt, wird nicht angefasst |
| Matchuhr, vMix-Einblender, LED, Streamdeck, Szenen | neue Hallensteuerung | wird gebaut |
| Konfiguration (Hallen, Geräte, Einblender, Decks) | Backoffice in der Cloud | wird gebaut |

Die Hallensteuerung baut keine Musikoberfläche. Wenn sie Musik auslösen soll (Torjingle), geschieht das über das Soundboard, nicht daneben.

## Zwei Hallentypen

**Nur Musik, Beispiel Oberseen.** Bedient wird am iPad über die Cloud, also das heutige Soundboard mit einem eigenen Profil. Kein Agent, kein Rechner in der Halle, keine Uhr, kein vMix. Hier ist nichts Neues nötig ausser der Profilpflege.

**Volle Halle, Beispiel Stratos.** Auf dem Hallen-PC läuft ein Agent: er hört die Matchuhr, steuert vMix und die LED-Wand und bedient das Streamdeck. Die Cloud hält die Konfiguration und bietet eine Fernbedienung für dieselben Funktionen, ohne Musik. Die Musik läuft daneben wie heute im Soundboard.

## Soll-Verhalten

Alles hier ist aus dem Bestehenden abgeschrieben, nicht neu erfunden. Abweichungen sind einzeln markiert.

### Soundboard, unverändert (heutiges Verhalten, dient als Abnahme)

1. Ein Klick auf eine Kachel startet den Song sofort in der Klick-Geste, ohne Netzrunde.
2. Ein zweiter Klick auf die laufende Kachel stoppt alles.
3. Ordner öffnen sich als Unterseite, zurück über die Ordnerspur.
4. Eine Kachel mit mehreren Songs spielt zufällig (🔀) oder der Reihe nach (🔁) und läuft weiter, bis gestoppt wird.
5. Langer Druck auf eine Mehrsong-Kachel öffnet die Einzelauswahl.
6. Kein sichtbarer Player. Die laufende Kachel zeigt den Equalizer, unten steht Titel und Interpret.
7. PANIC stoppt alles.
8. Spotify wie heute.

### Matchuhr (aus der GameClock übernommen)

1. Protokolle: iCast über UDP, Bodet ScorePad über TCP 4001, dazu das einstellbare Trennzeichen-Format.
2. Die Uhr zählt aufwärts, 00:00 bis zur eingestellten Drittelsdauer.
3. Suchlauf erkennt Sender, Port, Transport und Codierung selbst.
4. Vollbildanzeige für einen TV-Screen in der Halle.
5. Teamnamen und Logos aus der swissunihockey-API.

### Hallensteuerung am Streamdeck (aus TCConsole übernommen)

1. Festes 32er-Deck, 4 mal 8, Nummern unverändert, Kontexte statt Companion-Seiten.
2. Zurück auf Taste 17 und PANIC auf Taste 25 an jedem Ort.
3. Torablauf: erst Spielerin wählen, dann Stand erhöhen, Einblender erst, wenn der neue Stand steht.
4. Starting 6 nur, wenn sechs Einträge vorhanden sind, und nie über die Liste hinaus.
5. Standkorrektur nur abwärts, gesperrt solange die Uhr läuft.
6. Keine Maus, keine Tastatur, kein Fokuswechsel.

### Fernbedienung in der Cloud

1. Zeigt dasselbe wie das Streamdeck, Taste für Taste, plus Uhr und Zustände.
2. Keine Musikfunktionen.
3. Ohne angemeldeten Agenten: die Seite sagt das und sperrt die Tasten nicht lautlos.

## Abnahmekriterien

| Kriterium | Messung |
|---|---|
| Musik startet ohne spürbare Verzögerung | Soundboard unverändert, Start in der Klick-Geste |
| Keine neue Musikoberfläche | Die Hallensteuerung hat keine Musikseite |
| Uhr | Zeit auf der Fernbedienung höchstens eine Sekunde hinter der Hallenuhr |
| Einblender | Die echten L-UPL-Vorlagen, Felder stimmen mit vMix überein |
| Deck | Gleiche Tastenbelegung und gleiche Abläufe wie TCConsole heute |
| Nichts geht verloren | Soundboard und Ticketing in Prod bleiben unberührt |

## Etappen

Jede Etappe wird lokal vorgeführt, von dir abgenommen, erst dann folgt die nächste. Keine Etappe geht auf Prod, bevor die davor abgenommen ist.

1. **Gerüst**: Agent auf dem Hallen-PC, Anmeldung an der Cloud, Hallenkonfiguration im Backoffice (welche Halle bietet was).
2. **Matchuhr**: Empfang, Suchlauf, Vollbildanzeige.
3. **vMix**: Verbindung, die echten Einblender, Scoreboard.
4. **Deck**: die TCConsole-Abläufe am Streamdeck.
5. **LED**: sedna-Anbindung.
6. **Fernbedienung** in der Cloud, ohne Musik.

## Aus dem Archiv übernehmbar

Geprüft und brauchbar, wird etappenweise herausgepickt, nicht am Stück übernommen:

- iCast- und Bodet-Empfang samt Plausibilitätsprüfungen
- vMix-Anbindung, Scoreboard-Feldzuordnung, die aus `Einblender.zip` abgeleiteten L-UPL-Vorlagen
- LED-Anbindung (sedna)
- Simulator mit echten Einblendern, Uhr und vMix-Stellvertreter
- Agent-Grundgerüst mit Hub-Anbindung und Streamdeck-Bildern
- Backoffice-Konfiguration für Hallen, Szenen, Einblender, Decks
- Importskripte für die Prod-Musik, falls die Musik später doch umziehen soll

Nicht übernommen: Musikreiter, Musikwiedergabe im Bord, Musiktasten auf dem Mk2-Deck.

## Offene Fragen

1. **Oberseen**: reicht das heutige Soundboard mit einem eigenen Profil, oder soll dort zusätzlich etwas zu sehen sein, zum Beispiel die Matchuhr als Anzeige ohne Steuerung?
2. **Torjingle**: soll die Hallensteuerung Musik im Soundboard auslösen dürfen, und wenn ja, läuft das Soundboard dann auf demselben Rechner?
3. **GameClock**: ablösen, sobald die Uhr in der Hallensteuerung läuft, oder parallel weiterbetreiben?
4. **Streamdecks**: welche Halle hat welche Decks, und bleibt es beim XL in der Halle und dem Mk2 am Pult?
