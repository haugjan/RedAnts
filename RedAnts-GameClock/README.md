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

Unterstützt werden ausschliesslich Matchuhren mit Netzwerkausgabe. Typ, Port, Absender-IP und Zeichensatz stehen in
der Oberfläche:

| Typ | Transport | Port | Herkunft |
|---|---|---|---|
| **iCast Scoreboard** | UDP | 50085 | iCast Sweden AB, wie in der Win4 Stratos-Halle |
| **Bodet ScorePad** | TCP | 4001 | Bodet Sport, die Uhr verbindet sich auf diesen Port |
| **Allgemeine Zeile mit Trennzeichen** | UDP | frei | jede andere Uhr, die eine Zeile ins Netz schickt |

Der allgemeine Typ deckt alles ab, was eine Zeile mit Trennzeichen sendet: Trennzeichen und Feldnummern werden in der
Oberfläche gesetzt, ohne Codeänderung. Ein neuer fester Typ ist eine Klasse mit `IClockProtocol` in `Clocks/`, in
`Program.cs` registriert; `ClockProtocols` und der Suchlauf nehmen ihn dann von selbst auf. Rein serielle Tafeln
(RS232, RS485, Clock-and-Data) sind bewusst nicht unterstützt.

### Suchlauf

Matchuhren senden von sich aus, sie antworten nicht auf Anfragen. Der Suchlauf hört darum 8 Sekunden gleichzeitig auf
20 gebräuchlichen UDP-Ports und 5 TCP-Ports mit, erkennt den Zeichensatz am Bytemuster, lässt jedes Protokoll den
Mitschnitt bewerten und zeigt Absender, Port, Transport, Typ und eine gelesene Vorschau. Ein Klick übernimmt alles in
die Konfiguration. Der laufende Empfang pausiert für die Dauer des Suchlaufs, danach bindet er sich neu.

`Netz absuchen` pingt zusätzlich die lokalen Subnetze ab und listet die erreichbaren Geräte. Das hilft, wenn nichts
ankommt, weil die Uhr per UDP an eine einzelne Adresse sendet statt an die Broadcast-Adresse, oder weil bei TCP in der
Uhr noch die falsche Zieladresse steht.

### Teams und Logos

Namen und Logos aller L-UPL-Vereine kommen aus der swissunihockey-API
(`/api/teams?season=…&league=24&game_class=11|21`). Die Logos werden einmal heruntergeladen und liegen danach unter
`data/logos/`, die Anzeige braucht im Spiel also kein Internet.

Sendet die Uhr ein Kürzel (`RED`, `UBO`), vergleicht `Automatisch zuordnen` es mit den Teamnamen (Initialen,
Wortanfänge, Umlaute aufgelöst) und übernimmt den Treffer. Jeder Name und jedes Logo lässt sich überschreiben, eigene
Logos lassen sich hochladen. Ohne Zuordnung zeigt die Anzeige das Kürzel als Text. Der Bodet ScorePad sendet keine
Kürzel; dort zählen die festen Einträge für Heim und Gast.

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

- `Clocks/ClockFeed` (Hosted Service) nimmt UDP-Telegramme oder eine TCP-Verbindung entgegen, schreibt Änderungen ins
  Rohdaten-Log und reicht sie an `ClockHub` weiter (bei Änderung sofort, sonst 1x pro Sekunde). Eine gespeicherte
  Konfiguration bindet den Empfang neu. Jede TCP-Verbindung bekommt einen frischen Parser.
- `Clocks/ClockScanner` übernimmt für den Suchlauf exklusiv die Sockets, UDP und TCP.
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

## Telegramm der Matchuhr (Bodet ScorePad)

Der ScorePad baut über seinen RJ45-Port eine TCP-Verbindung zu dieser Anzeige auf. Einzurichten im
`Menu technicien` (Code 4934) unter `communication Protocols`: Protokoll **TV** anlegen, IP-Adresse und Port dieses
Rechners eintragen, Sportart starten. Der Ausgang gibt es nur auf der MAIN-Tastatur.

Jede Nachricht ist ein Rahmen:

```
01      7f       02     47      31 31 ...        03     2d
SOH   Adresse   STX   CTRL    Nachricht         ETX    LRC
```

Die LRC ist das XOR aller Bytes ab SOH (ausgeschlossen) bis und mit ETX, danach `LRC = LRC and 0x7f`, und
`IF LRC < 32 THEN LRC = LRC + 32`. Die beiden ersten Nachrichtenbytes sind die Nummer als ASCII-Ziffern. Ein
Leerzeichen (`0x20`) steht in Zahlenfeldern für eine führende Null, `'7'` ist die Kennung für Floorball.

Byteangaben hier als Index im ganzen Rahmen ab 0:

| Nachricht | Index | Bedeutung |
|---|---|---|
| **11** Spielstand | 6 / 7 | Statuswort (Bit 1: 1 = Uhr steht) / Sportart |
| | 8–9 / 10–11 | Minuten / Sekunden |
| | 12–14 / 15–17 | Tore Heim / Gast, dreistellig |
| | 18 | Nummer des laufenden Drittels |
| **12** Strafen Heim | 7 / 8 / 9–10 | Anzeiger / Minuten / Sekunden der 1. Strafe |
| | 11 / 12 / 13–14 | dasselbe für die 2. Strafe |
| **13** Strafen Gast | wie 12 | |
| **14** dritte Strafe | 7–10 / 11–14 | dritte Strafe Heim / Gast |
| **15** Spielernummern | 7–12 / 13–18 | Zehner und Einer je Strafe, Heim / Gast; `20H 20H` = keine Nummer |

In der letzten Minute läuft die Uhr in Zehnteln. Dann steht an Index 10 statt einer Ziffer das Trennzeichen `'D'`
(`0x44`), und die Zeit liest sich als `ss.z`, zum Beispiel `56.4`.

Der Anzeiger einer Strafe ist ein Siebensegment-Code, der zwischen zwei Werten blinkt (etwa `0x81` und `0x80`). Die
Anzeige stützt sich darum nicht darauf, sondern zeigt eine Strafe, solange ihre Zeit nicht `00:00` ist.

Anders als iCast sendet der ScorePad in diesen Nachrichten weder Teamkürzel noch einen Modus; Heim und Gast werden in
der Konfiguration fest gesetzt. Die Belegung stammt aus Bodets eigener Beschreibung (Dokument 608264B, "Scorepad
Network output and protocols"); weil `static.bodet-sport.com` nicht mehr auflöst, über das Internet Archive. Das
dokumentierte Beispiel `01 7f 02 47 31 31 80 37 20 34 30 37 20 30 31 20 30 30 31 03 2d` liest sich damit als `04:07`,
Stand `1:0`, erstes Drittel, und die LRC `0x2d` rechnet sich nach; beides prüft ein Test.

## Release

`.github/workflows/gameclock-release.yml` führt bei jeder Änderung in diesem Ordner die Tests aus und baut
self-contained Pakete für `win-x64`, `linux-x64` und `linux-arm64`. Version = Major.Minor aus `<Version>` in der csproj
plus Run-Nummer; auf `main` entsteht ein Release `gameclock-v<version>` mit den Paketen und den Start-Skripten.
