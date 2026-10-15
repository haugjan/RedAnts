# Hallensteuerung

Dieses Dokument legt fest, was gebaut wird, bevor gebaut wird. Es ist die Abnahmegrundlage.
Der erste Anlauf liegt als Archiv auf dem Tag `show-archiv-2026-10-15` (Branch `feature/s6-show-module`, nie auf `main`, nie auf Prod).

## Warum neu

Der erste Anlauf hat die Musikbedienung im neuen Bord nachgebaut, statt das bestehende Soundboard zu verwenden. Daraus folgten die Fehler im Betrieb: eine erfundene dritte Registerkarte, Musik, die Sekunden nach dem Klick startet, und ein sichtbarer Audio-Player, den das Soundboard nie hatte. Ursache waren zwei Festlegungen, die nie hinterfragt wurden: kein JavaScript im neuen Modul, und das Bord bildet alles ab, auch die Musik.

## Der Agent

Ein lokal laufendes Programm auf dem Rechner in der Halle. Er ist die Zentrale und läuft vollständig ohne Internet weiter.

- Sendet Befehle an die **LED-Wand** (sedna, TCP 27182) und an **vMix** (TCP-API).
- Empfängt das Signal der **Matchuhr**: iCast über UDP und Bodet ScorePad über TCP, dazu der Suchlauf, der Sender, Port, Transport und Codierung selbst erkennt.
- Nimmt Befehle eines **lokalen Companion-Moduls** entgegen und meldet ihm laufend Farben, Symbole und Beschriftungen der Tasten.
- Hostet die **Matchuhr als Webseite** für einen TV-Screen, Gestaltung wie die heutige GameClock.
- Hostet das **Bord**.
- **Zwischenspeicher**: die Musikstücke aus dem Blob Storage liegen lokal, damit sie ohne Netz und ohne Wartezeit starten.
- **Ohne Internet**: zuletzt geholte Hallenkonfiguration, Spielkonfiguration und Musik bleiben nutzbar, Bord, Uhr, vMix, LED und Streamdecks laufen weiter. Sobald die Verbindung zurück ist, gleicht er ab.

## Hallenkonfiguration

Es gibt n Hallen. Jede Halle legt fest:

- **Schnittstellen**: Musik (immer vorhanden), LED-Wand, vMix, Matchuhr inklusive Typ (iCast, Bodet ScorePad). Oberseen ist Bodet.
- **Streamdecks**: welche vorhanden sind und in welcher Grösse. Jedes Deck hat seine **eigene Seitenhierarchie**, zwei Decks zeigen also nicht zwangsläufig dasselbe.
- **Tasten** je Deck und Seite.
- **Seiten**, hierarchisch verschachtelt.

Regeln für jede Seite:

- **PANIC** liegt fix auf jeder Seite und beendet alles.
- Jede Unterseite hat einen **Zurück**-Knopf eine Ebene höher.
- Eine Aktion kann so definiert sein, dass sie nach dem Ausführen **automatisch auf die Hauptseite** zurückspringt.

Gepflegt wird das im **Umbraco-Backoffice unter dem Reiter „Show“**, mit einer Unterseite für die Musik.

## Das Bord

Das Bord bildet die Matchuhr ab und darunter alle Streamdecks, die für diese Halle konfiguriert sind, Taste für Taste, mit denselben Farben, Symbolen und Beschriftungen. Alles ist auch auf dem Bord bedienbar.

- **Lokal beim Agenten**: die volle Halle.
- **In der Cloud**: dasselbe Bord, dort aber nur für Hallenkonfigurationen, die ausschliesslich Musik abspielen (Beispiel Oberseen, bedient am iPad).
- **Zugang mit Passwort**, lokal wie in der Cloud.
- Eine Halle ohne physisches Streamdeck bekommt trotzdem ein Deck mit einer Grösse konfiguriert, damit das Bord weiss, wie viele Tasten es zeigt.
- **Kacheln passen sich der Bildschirmgrösse an**: das Raster behält Zeilen und Spalten des Decks, die Kacheln wachsen mit dem Fenster. Die Beschriftung wird so gross wie möglich gesetzt und nie abgeschnitten, sie verkleinert sich erst, wenn sie sonst nicht mehr in die Kachel passt. Auf dem Streamdeck selbst rechnet der Agent dasselbe beim Erzeugen des Tastenbildes.

### Spielkonfiguration auf dem Bord

Auf dem Bord, nicht im Admin:

1. Spiel von der **swissunihockey-API** laden.
2. Oder eine **Spielkonfigurationsdatei** einlesen.
3. Danach ist alles **überschreibbar**, insbesondere Kommentatoren, Schiedsrichter, Teamnamen, Kader und Aufstellung.

## Matchuhr

Die Matchuhr ist die Quelle der Wahrheit für **Zeit, Spielstand, Drittel und laufende Strafen**. Der Spielstand kommt also nicht mehr von Hand, sondern aus dem Telegramm.

- Die Uhr **zählt immer aufwärts**.
- Verlängerung und Penaltyschiessen verhalten sich wie in TCunihockey (Abschnitt `4` und `5` im Telegramm, Beschriftung „Verlängerung“ und „Penaltyschiessen“).
- Den **Scoreboard-Einblender in vMix schreibt künftig der Agent**, so wie es heute die GameClock tut. Die GameClock wird damit abgelöst, sobald die Uhr hier läuft.
- Die Uhr-Webseite zeigt Zeit, Stand, Drittel, Teams und Logos, Gestaltung wie die GameClock.

## Musik

Technisch **eins zu eins wie das heutige DJ-Bord**: der Song startet in der Klick-Geste, vorgeladen, ohne Netzrunde, ohne sichtbaren Player.

- **Einzelstücke oder Playlists**, sequenziell oder zufällig.
- Jedes Stück mit **Cue-Start und Cue-Ende**.
- Verwaltet im heutigen **DJ-Admin**, das Setzen von Anfang und Ende bleibt gleich komfortabel. Die Musikbefehle bekommen **Namen** und werden auf einer Unterseite im Admin definiert.
- **Steuerung**: Stopp, Fade-out, nächstes Stück, vorheriges Stück, Pause und Weiter.
- **Laufanzeige** mit Titel, Interpret und Restzeit, die laufende Kachel ist erkennbar.
- **Langer Druck** auf eine Playlist öffnet die Einzelauswahl der Songs.
- **Spotify** nur im Cloud-Bord, lokal beim Agenten nicht.
- Die Musik läuft auf dem Gerät, das das Bord anzeigt, beziehungsweise auf dem Agenten.

## Tasten

### Frei definierbare Tasten

Eine Taste führt eine **Abfolge vordefinierter Befehle** aus, zum Beispiel Musik auslösen, dann etwas in vMix einblenden, dann auf der LED-Wand.

### Feste Spezialtasten

Verhalten fest, Position frei wählbar.

| Taste | Verhalten |
|---|---|
| **Tor Heim** | Spielt Musik (Song konfigurierbar), blendet auf der LED-Wand ein (konfigurierbar), öffnet die Spielerliste der Heimmannschaft inklusive **Eigentor** und **Topscorer**. Nach der Wahl geht die Spielerin an vMix und an die LED-Wand. Unten zeigt ein toter Knopf mit schwarzem Hintergrund an, dass gerade ein Tor gewählt wird. |
| **Tor Gast** | Wie Tor Heim, mit der Gästemannschaft. |
| **Strafe Heim** | Wie Tor Heim, aber zwischen Taste und Spielerwahl kommt die Strafenseite: 2', 2'+2', 10', Spieldauer. |
| **Strafe Gast** | Wie Strafe Heim, mit der Gästemannschaft. |
| **Best Player Heim / Gast** | Wie Tor, ohne Musik und ohne Eigentor. |
| **Einblender an/aus** | Zeigt rot, wenn etwas eingeblendet ist, sonst grün, und schaltet auf Knopfdruck um. |
| **Uhr an/aus** | Zeigt, ob die Uhr eingeblendet ist, und schaltet um. |
| **Musik an/aus** | Zeigt, ob Musik läuft, und schaltet sie aus. |
| **Automodus Pausenmusik** | Zeigt, ob der Automodus an ist, und legt die Playlist fest. Automodus heisst: solange die Uhr zwischen 00:00 und der Drittelsdauer steht, läuft bei einem Spielunterbruch automatisch Musik. Vor dem Spiel und nach Ablauf des Drittels nicht. |
| **Starting 6** | Öffnet eine Unterseite mit den sechs Spielerinnen. Ein Weiter-Knopf oder ein Klick auf eine Spielerin blendet sie in vMix als Starting Six ein, wahlweise auch auf der LED-Wand, dort nur für die Heimmannschaft. Die gerade gezeigte Spielerin leuchtet. |
| **Musiksteuerung** | Je eine Taste für Stopp, Fade-out, nächstes Stück, vorheriges Stück und Pause. |

### Weitere Einblender, aus TCConsole übernommen

Als frei platzierbare Tasten verfügbar, weil sie heute im Einsatz sind:

- **Coach**, **Timeout**, **Aufstellung** und **Spieler** (nur Name) je Mannschaft.
- **Kommentatoren**, **Schiedsrichter**, **Opener**, **Resultat**, **Highlight**.
- **Meldungen**: Störung Technik, Video, Audio, dazu die vordefinierten Kommentartexte.
- **Zuschauerzähler**: +100, +10, +1, −1, −10, −100 und „Anzeigen“.

### In vMix

Alles, was heute mit TCConsole und TCunihockey möglich war: die echten L-UPL-Einblender mit vollständiger Feldzuordnung, das Scoreboard inklusive Strafboxen, Teamlogos und die Overlay-Kanäle.

## Was entfällt

- **Uhrsteuerung vom Deck** (−1 s, +1 s, Start/Stop): die Hallenuhr ist die Quelle, sie wird am Zeitnehmertisch bedient.
- **Standkorrektur auf dem Deck**: der Stand kommt aus dem Telegramm, korrigiert wird an der Uhr.
- **Drittel setzen und Penaltys zurücksetzen**: beides steht im Telegramm.

## Abnahmekriterien

| Kriterium | Messung |
|---|---|
| Musik startet ohne spürbare Verzögerung | Start in der Klick-Geste, vorgeladen, auch ohne Internet |
| Keine fremde Oberfläche | Das Bord zeigt die Decks eins zu eins, keine zusätzlichen Reiter |
| Uhr | Zeit auf dem Bord höchstens eine Sekunde hinter der Hallenuhr |
| Einblender | Die echten L-UPL-Vorlagen, Felder stimmen mit vMix überein |
| Deck | Gleiche Abläufe wie TCConsole heute, inklusive Torablauf und Starting 6 |
| Netzausfall | Agent, Bord, Uhr, vMix, LED und Musik laufen weiter |
| Nichts geht verloren | Soundboard und Ticketing in Prod bleiben unberührt |

## Etappen

Jede Etappe wird lokal vorgeführt, von dir abgenommen, erst dann folgt die nächste.

1. **Gerüst**: Agent auf dem Hallen-PC, Hallenkonfiguration im Backoffice, Konfiguration lokal zwischengespeichert.
2. **Matchuhr**: iCast und Bodet, Suchlauf, Uhr-Webseite, Scoreboard in vMix.
3. **Bord und Decks**: Seiten, Tasten, Spiegelung, Companion-Modul, Spielkonfiguration laden und bearbeiten.
4. **Spezialtasten**: Tor, Strafe, Best Player, Starting 6, Zustandstasten.
5. **Musik**: Technik des DJ-Bords, lokaler Zwischenspeicher, Automodus.
6. **LED**: sedna-Anbindung und Vorlagen.
7. **Cloud-Bord** für Hallen, die nur Musik spielen.

## Companion

Ein **universelles Modul** für alles: Hallensteuerung und Musik, Halle mit und ohne Agent. Das bisherige DJ-Modul wird davon abgelöst.

- Companion führt mehrere Streamdecks als eigene Oberflächen. Weil jedes Deck seine eigene Seitenhierarchie hat, wird **je Deck eine Modulinstanz** eingerichtet, die auf dieses Deck zeigt.
- Je Taste gibt es die Aktion „Taste drücken“ mit der Tastennummer und die Rückmeldung mit derselben Nummer, dazu ein fertiges Preset je Nummer. Die Zuordnung wird einmal gemacht und nie mehr angefasst.
- Was die Taste zeigt und was sie tut, entscheidet allein die Hallenkonfiguration. Das Modul kennt nur Nummern.
- Zusätzlich enthält das Modul die Musiksteuerung (Stopp, Fade-out, nächstes und vorheriges Stück, Pause) und die Seitennavigation.

## Offene Punkte

1. **Topscorer**: In der echten Spielkonfig (`game-config-v6-Example`, UHC Uster gegen Chur, 23.06.2023) steht in beiden Mannschaften ein Eintrag `00=`, und zwar mit dem Namen einer Spielerin oder eines Spielers, die oder der weiter oben schon mit der eigenen Nummer steht: Heim `70=Simon Suter` und `00=Simon Suter`, Gast `61=Joel Hirschi` und `00=Joel Hirschi`. `00` taucht zusätzlich in `Starting6=46,6,00,10,12,14` und in `Line2=17,-,00,9,22` auf. Offen bleibt, was TCunihockey damit anzeigt und was die Taste bei uns tun soll.
