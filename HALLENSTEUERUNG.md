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
- Hostet die **Matchuhr als Webseite** für einen TV-Screen.
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
3. Danach ist alles **überschreibbar**, insbesondere Kommentatoren, Schiedsrichter, Teamnamen, **Teamkürzel**, **Teamfarbe für Heim und Gast**, Kader, Aufstellung und Topscorer.

Teamkürzel und Teamfarbe gehen an vMix und an die LED-Wand, sie bestimmen also, wie Scoreboard und Einblender aussehen. Beide sind je Spiel einstellbar, die Halle gibt nur den Vorschlag für die Heimmannschaft vor.

Der Eintrag `00=` aus der Spielkonfigurationsdatei ist der **Topscorer** der Mannschaft: die Nummer 00 wird nicht vergeben, und der Name steht im selben Abschnitt bereits mit der echten Nummer. Er erscheint deshalb nicht als Rückennummer im Kader, sondern als eigene Taste in der Spielerliste. Steht `00` in einer Aufstellung oder Linie, wird daraus die Spielerin oder der Spieler mit der echten Nummer.

## Matchuhr

Die Matchuhr ist die Quelle der Wahrheit für **Zeit, Spielstand, Drittel und laufende Strafen**. Der Spielstand kommt also nicht mehr von Hand, sondern aus dem Telegramm.

- Die Uhr **zählt immer aufwärts**.
- Verlängerung und Penaltyschiessen sind Abschnitt `4` und `5` im Telegramm, Beschriftung „Verlängerung“ und „Penaltyschiessen“.
- Den **Scoreboard-Einblender in vMix schreibt der Agent** aus dem Uhrtelegramm.
- Die Uhr-Webseite zeigt Zeit, Stand, Drittel, Teams und Logos.

## Musik

Technisch **eins zu eins wie das heutige DJ-Bord**: der Song startet in der Klick-Geste, vorgeladen, ohne Netzrunde, ohne sichtbaren Player.

- **Einzelstücke oder Playlists**, sequenziell oder zufällig.
- Jedes Stück mit **Cue-Start und Cue-Ende**.
- Verwaltet unter **Show, Musik**: eine Liste mit Name, Stücken und Zufall, dazu Hochladen und Spotify. Welche Taste welchen Eintrag spielt, steht unter **Decks**.
- **Steuerung**: Stopp, Fade-out, nächstes Stück, vorheriges Stück, Pause und Weiter.
- **Laufanzeige** mit Titel, Interpret und Restzeit, die laufende Taste ist erkennbar.
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
| **Tor Heim** | Spielt Musik (Song konfigurierbar), blendet auf der LED-Wand ein (konfigurierbar), öffnet die Spielerliste der Heimmannschaft inklusive **Eigentor** und **Topscorer**. Nach der Wahl geht die Spielerin an vMix und an die LED-Wand, in der Farbe und mit dem Kürzel der Mannschaft. Unten zeigt ein toter Knopf mit schwarzem Hintergrund an, dass gerade ein Tor gewählt wird. |
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

### Weitere Einblender

Als frei platzierbare Tasten verfügbar, weil sie heute im Einsatz sind:

- **Coach**, **Timeout**, **Aufstellung** und **Spieler** (nur Name) je Mannschaft.
- **Kommentatoren**, **Schiedsrichter**, **Opener**, **Resultat**, **Highlight**.
- **Meldungen**: Störung Technik, Video, Audio, dazu die vordefinierten Kommentartexte.
- **Zuschauerzähler**: +100, +10, +1, −1, −10, −100 und „Anzeigen“.

### In vMix

Die echten L-UPL-Einblender mit vollständiger Feldzuordnung, das Scoreboard inklusive Strafboxen, Teamlogos und die Overlay-Kanäle. Dazu **Teamkürzel und Teamfarbe je Mannschaft** aus der Spielkonfiguration.

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
| Deck | Vollständiger Torablauf und Starting 6 |
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

## Stand vom 16.10.2026

Gebaut und geprüft (Tests grün, im Browser nachgesehen):

- **Hallenkonfiguration** im Backoffice unter „Show”: Angebot, Geräte, Einblender, LED-Vorlagen, Decks mit Seiten und Tasten.
- **Hallenkonfigurationen**: sechs Konfigurationen werden beim Start angelegt, jede mit eigenem Agent-Schlüssel, damit sich zwei Hallen nicht gegenseitig die Musik stoppen.
  - `win4` WIN4 Stratos: Sound, Matchuhr iCast auf UDP 50086, vMix, LED-Wand; Decks XL und Mk2.
  - `win4-sound` WIN4 nur Sound: nur Soundboard, auch im Netz unter /bord bedienbar; ein Bord-Deck.
  - `win4-livestream` WIN4 nur Livestream: vMix und Matchuhr iCast, kein Sound, keine LED, keine automatische Pausenmusik; nur das XL-Deck.
  - `oberseen` Oberseen iPad: nur Sound, im Netz bedienbar.
  - `oberseen-notebook` Oberseen Notebook: Sound und Matchuhr Bodet ScorePad über TCP 4001.
  - `rennweg-ipad` Rennweg iPad: nur Sound, im Netz bedienbar.
  
  Im Netz unter /bord erscheinen nur die Hallen, die ausschliesslich Musik anbieten: win4-sound, oberseen und rennweg-ipad.
- **Deck-Modell**: Seiten statt fester Kontexte, Tastenarten leer, Musik, Befehlsfolge, feste Funktion, Seite, Anzeige; PANIC und Zurück liegen je Deck fest; eine Taste kann nach dem Druck auf die Startseite zurückspringen.
- **Feste Funktionen**: Tor Heim/Gast, Strafe mit 2', 2'+2', 10' und Spieldauer, Best Player, Spielerin, Starting 6, Zuschauerzähler, die Zustandstasten (Einblender, Uhr, Musik, Automodus) und die Musiksteuerung. Ihre Musik und LED-Vorlage steht als Befehlsfolge auf derselben Taste und ist im Backoffice unter Show > Decks änderbar; sie läuft beim Druck, bevor die feste Funktion übernimmt.
- **Auf Sendung**: nach der Spielerwahl zeigt eine tote Taste, was gerade eingeblendet ist (z. B. „Tor / 7 Nina Muster"); als Tastenart „Anzeige" lässt sich das auf jedes Deck legen.
- **Musik**: bleibt im Soundboard. Die Hallensteuerung liest die Befehle über einen Port, das Bord spielt sie mit dem Player des Soundboards, ohne sichtbaren Player und ohne Netzrunde.
- **Scoreboard**: der Agent schreibt Zeit, Stand, Drittel und die Strafboxen aus dem Uhrtelegramm direkt nach vMix.
- **Agent**: meldet sich an der Cloud an, hält die Konfiguration lokal, lädt die Musikstücke in einen Zwischenspeicher und bedient sie offline unter `/dj/sound/...`, serviert Bord und Matchuhr, spricht vMix und die LED-Wand.
- **Bord**: identisch lokal und in der Cloud, zeigt alle Decks der Halle nebeneinander auf einer Seite, spiegelt jedes Deck Taste für Taste. Die Kacheln wachsen mit dem Fenster und haben alle dieselbe Grösse je Deck. Die Kachelgrösse ergibt sich aus Fensterhöhe und Gesamtbreite, je Deck haben alle Tasten dieselbe Schriftgrösse, nur die langen Beschriftungen werden kleiner, damit nichts abgeschnitten wird.
- **Nur ein Bord spielt**: die Halle merkt sich, welches Bord zuletzt bedient wurde. Startet ein zweites Bord ein Stück, verstummt das erste; Stopp, Fade, Pause und Vor wirken in jedem offenen Bord derselben Halle. Ohne Agenten übernimmt das Cloud-Bord die Steuerung selbst, mit Agent kommt sie von ihm.
- **Zurück-Taste nur auf Unterseiten**: auf der Startseite ist der Platz der Zurück-Taste (je Deck fest definiert) frei für eine normale Taste. Auf Unterseiten (eine Ebene tiefer) wird derselbe Platz automatisch zu Zurück, unabhängig davon, was die Hallenkonfiguration dort eingetragen hat. Das Mk2 nutzt das, um Zurück oben links (Taste 1) zu haben und PANIC auf Taste 11.
- **Mk2 der Halle WIN4**: neue, vom Benutzer vorgegebene Struktur. Startseite mit Taste 1 für „Vor/Nach Spiel / Pause“, Taste 2 „Special Situations“, Taste 5 Spielunterbruch, Taste 10 Spielunterbruch automatisch, Taste 11 PANIC, Tasten 12 bis 15 Stopp, Pause, Vor und Fade-out. Zweite Seite mit neun Unterseiten, je Liga eine Dreiergruppe (L-UPL auf Tasten 2 bis 4, U21 auf 7 bis 9, U17 auf 12 bis 14), jede mit Vor Spiel, Pause und Nach Spiel. Dazu Aufwärmmusik auf Taste 10, tote schwarze Taste mit Seitennamen auf Taste 5. Jede dieser neun Seiten hat oben links Zurück, oben rechts eine tote schwarze Taste mit Seitennamen, in der untersten Reihe PANIC und dieselbe Musiksteuerung wie die Startseite, dazwischen die Stücke des Soundboards in ihrer Reihenfolge. LIDL und Mobiliar nur bei L-UPL, Einlaufsong je Liga ein anderer. Die Seiten werden aus dem Soundboard-Baum gebaut (Infrastructure/Content/MusicDeck.cs). Eine bestehende Halle wird beim Start einmalig auf die neuen Seiten umgestellt.
- **Spielkonfiguration auf dem Bord**: die Taste „⚙ Spiel“ öffnet Laden (swissunihockey, Datei, von Hand) und das Bearbeiten von Titel, Schiedsrichtern, Kommentatoren sowie je Team Kürzel, Farbe und Topscorer. Im Cloud-Bord sitzt die Maske direkt auf der Seite, im Bord des Agenten führt die Taste auf dieselbe Maske unter `/matchday?halle=<halle>` in der Cloud.
- **Matchuhr**: iCast über UDP und Bodet ScorePad über TCP (der Agent hört auf dem Port, die Uhr verbindet sich); Rahmen mit SOH/STX/ETX und LRC, Zeit, Stand, Drittel, beide Strafboxen und die Nummern der bestraften Spielerinnen.
- **Spielerin an die LED-Wand**: die Auswahl im Tor-, Strafen-, Best-Player- und Spielerin-Ablauf geht nach vMix und mit der Vorlage „spielerin“ auch an sedna; LED-Vorlagen kennen dieselben Platzhalter wie die Einblender.
- **Companion**: ein universelles Modul, je Deck eine Instanz, Presets je Tastennummer, Musiksteuerung und PANIC.
- **Beispielhalle Win4 Stratos**: wird beim Start angelegt, XL-Deck für die Hallensteuerung, Mk2 mit den Stücken des Clubs, Pausenmusik gesetzt.

## Offene Punkte

1. **Bodet in der Halle messen**: das Protokoll ist nach dem Handbuch gebaut und mit den dokumentierten Rahmen getestet, aber noch an keiner echten ScorePad in Oberseen gehört. Beim ersten Spiel dort den Rohlog (`/status`) ansehen.
2. **Sofortige Automatik braucht ein Signal, das iCast nicht sendet**: das Handbuch (Scoreboard Handball 2.0) listet alle Felder, ein Flag „Uhr läuft" ist nicht dabei; die beiden letzten Flags melden nur das Ende der Spielzeit und des Timeouts. Die Uhr sendet zwar zehnmal pro Sekunde, die Zeit selbst wechselt aber nur im Sekundentakt. Die Pausenmusik startet deshalb im Mittel nach einer halben Sekunde, in der letzten Minute (Zehntel) nach etwa einer Viertelsekunde. Beim Bodet ScorePad ist noch offen, ob das Attribut-Byte der Nachricht den Stillstand meldet.
3. **Spielerliste im Cloud-Bord**: die Spielerwahl läuft über den Agenten, ohne Agent zeigt das Bord die Seite nicht.
4. Die Annahme zum Topscorer (`00=` ist der Topscorer, weil die Nummer 00 nicht vergeben wird) wird beim ersten echten Spiel überprüft.
5. **Passwörter**: das Cloud-Bord hängt am Passwort `Show:BoardPassword`, das heute im Klartext in `appsettings.json` steht (aus der Soundboard-Zeit). Für die Hallensteuerung gehört es in die App-Service-Einstellungen. Das lokale Bord des Agenten hört nur auf `localhost`; soll es im Hallennetz erreichbar sein, braucht `agent.json` `bind` (z. B. `0.0.0.0`) und `boardPassword`, sonst warnt der Agent beim Start.
6. **Spielkonfiguration offline**: das Bearbeiten läuft über die Cloud, der Agent reicht sie nur weiter. Ohne Internet bleibt das zuletzt geladene Spiel gültig, ändern lässt es sich dann nicht.
7. **Zuschauerzähler**: die Seite zählt im Agenten mit und schreibt die Zahl in den Einblender, eine Auswertung über mehrere Spiele gibt es nicht.
8. **Musik nach dem Umbau der Mk2-Decks**: die Stücke „Vor dem Spiel 1. Liga“, Time Out und Penalty liegen nach dem Umbau nicht mehr auf dem Mk2, weil der Benutzer die Plätze anders vergeben hat. Sie sind weiterhin im Soundboard vorhanden und können im Backoffice auf freie Tasten gelegt werden.
