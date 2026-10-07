# RedAnts GameClock (Matchuhr)

Blazor-Server-Webapp (.NET 8), die die Telegramme einer Hallen-Matchuhr empfängt, Spielzeit, Spielstand, Drittel und
Strafen live im Browser anzeigt und dieselben Werte an vMix schickt. Schwarzer Hintergrund, Zeit maximal gross,
Logos beider Teams. Die Einrichtung läuft über die Oberfläche, ein Verein braucht dafür keinen Zugriff auf den Code.

## Starten

**Windows:** `Start-GameClock.cmd` doppelklicken, oder in PowerShell:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-GameClock/Start-GameClock.ps1)))
```

**Linux (x64 / ARM64, z.B. Raspberry Pi):**

```bash
curl -fsSL https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-GameClock/start-gameclock.sh | bash
```

Das Skript lädt das neueste Release (Tag `gameclock-v*`), installiert es (Windows: `%LOCALAPPDATA%\RedAnts-GameClock`,
Linux: `~/.local/share/redants-gameclock`), startet die App und gibt die URL aus. Ist kein Internet verfügbar, startet
es die bereits installierte Version (`-Offline` bzw. `--offline` überspringt die Suche). Port ändern: `-Port 8080` bzw.
`PORT=8080`.

Die Anzeige läuft unter `http://localhost:5080/` und von anderen Geräten unter `http://<IP>:5080/`. Vollbild mit F11.

## Einrichten

Beim ersten Aufruf öffnet sich `/setup`. Danach führt das Zahnrad oben rechts dorthin; es wird erst sichtbar, wenn die
Maus in die obere rechte Ecke fährt, und stört so die Anzeige im Spiel nicht.

Gespeichert wird in `data/gameclock.json` neben der Exe. Der Ordner `data/` liegt nicht im Release-Paket, ein Update
überschreibt die Einstellungen also nicht. `appsettings.json` liefert nur noch die Vorgaben für den allerersten Start.

| Schlüssel in `appsettings.json` | Standard | Bedeutung |
|---|---|---|
| `Urls` | `http://0.0.0.0:5080` | Adresse der Webapp |
| `GameClock:Port` | `50085` | UDP-Port für den ersten Start |
| `GameClock:Ip` | `172.20.1.143` | Absender-IP für den ersten Start, `any` für alle |
| `GameClock:LogDir` | `logs` | Rohdaten-Log, eine Zeile pro Änderung |
| `GameClock:DataDir` | `data` | Konfiguration, Teamliste und heruntergeladene Logos |
| `GameClock:Home`, `GameClock:Guests` | Red Ants, BEO/UBO | Teams für den ersten Start |

### Matchuhr

Typ, UDP-Port, Absender-IP und Zeichensatz stehen in der Oberfläche. Unterstützt sind:

- **iCast Scoreboard** — die Scoreboard-Ausgabe von iCast Sweden AB, wie sie in der Win4 Stratos-Halle läuft.
- **Allgemeine Zeile mit Trennzeichen** — für jede andere Uhr, die eine Zeile per UDP schickt. Trennzeichen und
  Feldnummern werden selbst gesetzt, ohne Codeänderung. Praktisch alle Anzeigesysteme, die ihre Daten ins Netz geben,
  tun das in dieser Form, direkt oder über eine Brücke an der TV-Schnittstelle (RS485).

Ein neuer fester Typ ist eine Klasse mit `IClockProtocol` in `Clocks/`, in `Program.cs` registriert; `ClockProtocols`
und der Suchlauf nehmen ihn dann von selbst auf.

### Suchlauf

Matchuhren senden von sich aus, sie antworten nicht auf Anfragen. Der Suchlauf hört darum 8 Sekunden gleichzeitig auf
20 gebräuchliche Ports mit, erkennt den Zeichensatz am Bytemuster, lässt jedes Protokoll den Mitschnitt bewerten und
zeigt Absender, Port, Typ und eine gelesene Vorschau. Ein Klick übernimmt alles in die Konfiguration. Der laufende
Empfang pausiert für die Dauer des Suchlaufs, danach bindet er sich neu.

`Netz absuchen` pingt zusätzlich die lokalen Subnetze ab und listet die erreichbaren Geräte. Das hilft, wenn nichts
ankommt, weil die Uhr an eine einzelne Adresse sendet statt an die Broadcast-Adresse.

### Teams und Logos

Namen und Logos aller L-UPL-Vereine kommen aus der swissunihockey-API
(`/api/teams?season=…&league=24&game_class=11|21`). Die Logos werden einmal heruntergeladen und liegen danach unter
`data/logos/`, die Anzeige braucht im Spiel also kein Internet.

Die Uhr sendet nur ein Kürzel (`RED`, `UBO`). `Automatisch zuordnen` vergleicht es mit den Teamnamen (Initialen,
Wortanfänge, Umlaute aufgelöst) und übernimmt den Treffer. Jeder Name und jedes Logo lässt sich überschreiben, eigene
Logos lassen sich hochladen. Ohne Zuordnung zeigt die Anzeige das Kürzel als Text.

### vMix

GameClock spricht dieselbe TCP-API wie TCunihockey (Port 8099, `FUNCTION SetText`/`SetColor`/`SetImage`, Werte
URL-kodiert) und trifft mit der Vorgabe **L-UPL** denselben Titel und dieselben Felder:
`LUPL-SU_scoreboard_clock.gtzip` mit `TxtHomeScore.Text`, `TxtClockPeriod.Text`, `TxtClockTimeM10.Text` und so weiter.
Die bestehende vMix-Produktion muss dafür nicht angefasst werden.

- Steht `{0}` im Feldnamen, geht jede Ziffer einzeln an `M10`, `M01`, `T`, `S10`, `S01` — so arbeitet der L-UPL-Titel.
  Ohne `{0}` geht der ganze Wert in ein Feld, für Titel mit einem einzelnen Uhrenfeld.
- Gesendet wird nur, was sich geändert hat. Beim Verbinden geht einmal alles raus, eingeklammert in `PauseRender` /
  `ResumeRender`; danach läuft jede Änderung ohne Pause durch, wie bei TCunihockey.
- Das Drittel geht als `1`, `2`, `3`, `O` (Verlängerung) oder `P` (Penaltyschiessen) raus.
- Die Strafboxen werden über die Füllfarbe geschaltet: `…FF` bei laufender Strafe, `…00` sonst.
- `Titel aus vMix laden` liest den Zustand von vMix (`XML`) und füllt Titelliste und Feldnamen, damit auch ein eigener
  Titel ohne Tippen zugeordnet werden kann.
- Logos gehen per `SetImage` als Dateipfad raus. Das funktioniert nur, wenn die Anzeige auf dem vMix-Rechner läuft,
  darum ist es ausgeschaltet.

## Aufbau

- `Clocks/ClockFeed` (Hosted Service) liest die Telegramme, schreibt Änderungen ins Rohdaten-Log und reicht sie an
  `ClockHub` weiter (bei Änderung sofort, sonst 1x pro Sekunde). Eine gespeicherte Konfiguration bindet den Empfang neu.
- `Clocks/ClockScanner` übernimmt für den Suchlauf exklusiv die Sockets.
- `Vmix/VmixPublisher` (Hosted Service) hängt am `ClockHub` und hält die TCP-Verbindung zu vMix.
- `Components/Pages/Clock.razor` (Interactive Server) zeigt nach 3 s ohne Daten einen Hinweis.
- `Components/Pages/Setup.razor` mit den drei Abschnitten Matchuhr, Teams und vMix.
- `wwwroot/reconnect.js` startet Blazor mit unbegrenzten Reconnect-Versuchen und lädt die Seite neu, wenn der Server
  die Sitzung nicht mehr kennt (z.B. nach Neustart).
- `GET /api/state` liefert den aktuellen Stand als JSON.
- `tests/RedAnts.GameClock.Tests` deckt Protokolle, Zeichensatzerkennung, Kürzelzuordnung, vMix-Werte und das Speichern
  ab. Die Tests laufen in `gameclock-release.yml` vor dem Paketbau.

## Telegramm der Matchuhr (iCast)

UDP, UTF-32 Big-Endian, alle 100 ms, Felder mit `;` getrennt:

```
19:38;2;6;3;;;;;0;0;RED;UBO;GAME TIME;0;-6;0;-2;0;0;1
```

| Index | Bedeutung |
|---|---|
| 0 | Zeit: `mm:ss`, in der letzten Minute `ss.z` (Zehntel, rot angezeigt); Spielzeit zählt aufwärts, Pause und Timeout abwärts |
| 1 / 2 | Tore Heim / Gast |
| 3 | Periode: 1–3 Drittel, 4 Verlängerung, 5 Penaltyschiessen (5 ist eine Annahme) |
| 4 / 5 | Strafe Heim 1 / 2: `<Nr> <mm:ss>`, leer ohne Strafe |
| 6 / 7 | Strafe Gast 1 / 2 |
| 8 / 9 | Strafenlampe Heim / Gast |
| 10 / 11 | Kürzel Heim / Gast |
| 12 | Modus: `GAME TIME`, `INTERMISSION` (Pause), `TIME-OUT`; laut iCast ausserdem Warmup, Time to Warmup, Time to Face-Off, Local Time |
| 13–19 | nicht entschlüsselt; die Hallenuhr sendet mehr Felder als das Handbuch beschreibt, siehe Rohdaten-Log |

Die Felder 0–12 stehen so im Handbuch von iCast Sweden AB; die Belegung der Strafenfelder stammt zusätzlich aus dem
bestehenden Node-RED-Flow für vMix und ist noch nicht an einer echten Strafe überprüft.

Zeigt die Hallenuhr die Tageszeit, sendet sie nichts; die Anzeige blendet dann nach 3 s ab.

## Release

`.github/workflows/gameclock-release.yml` führt bei jeder Änderung in diesem Ordner die Tests aus und baut
self-contained Pakete für `win-x64`, `linux-x64` und `linux-arm64`. Version = Major.Minor aus `<Version>` in der csproj
plus Run-Nummer; auf `main` entsteht ein Release `gameclock-v<version>` mit den Paketen und den Start-Skripten.
