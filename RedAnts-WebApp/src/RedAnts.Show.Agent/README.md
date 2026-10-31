# RedAnts Show-Agent

The hall agent is the local half of the Show module: a headless .NET service on the streaming notebook that executes what the cloud (`show.redants.ch`, module `RedAnts.Show`) has configured for its hall. It talks to the devices that are only reachable in the hall and reports back.

| Direction | What |
|---|---|
| Cloud → agent | `Sync` (hall settings, scenes, active game), `Run` (scene id plus arguments), `Panic`, `Press` (deck and key number) and `View` (the live deck of one layout, answered by the agent) over the SignalR hub `/hubs/halle` |
| Agent → cloud | `Report(kind, json)`: `status` (lamps for vMix, Uhr, LED plus details), `score`, `clock`, `deck`, `overlay`, `sound`, `log` |
| Agent → vMix | scoreboard fields from the hall clock, overlay titles for scenes (TCP 8099, `SetText`/`SetColor`/`SetImage`, `OverlayInput<n>In/Out`) |
| Agent → LED wall | sedna presenter commands (TCP 27182), templates per event from the hall settings |
| Hall clock → agent | iCast telegrams on UDP 50085/50086 (UTF-32 BE) or a Bodet ScorePad on TCP 4001, both documented below; manual score fallback on the deck when no telegram arrives |
| Companion → agent | the fixed 32-key deck: `GET /deck/view?since=`, `GET /deck/image?key=`, `POST /deck/press/{1..32}` on `http://localhost:5150` (Companion module `companion-module-redants-halle`) |

Music never runs on the agent: `sound.*` steps are reported to the cloud, the DJ board in the browser plays them.

## Installation in der Halle auf einen Blick

Das Programm heisst **Red Ants Board** (intern weiterhin Show-Agent). Einmal pro Notebook installieren, danach reicht ein Doppelklick.

1. **Installieren:** PowerShell öffnen, die Zeile unter [Board starten](#board-starten) einfügen und Enter drücken. Danach liegt auf dem Desktop genau eine Verknüpfung **„Red Ants Board"** mit dem Ameisen-Icon.
2. **Halle wählen:** Der Browser öffnet `http://localhost:5150/bord` und fragt als Erstes **„Welche Halle?"**. Beim allerersten Mal will er einmal das **Bord-Passwort** (dasselbe wie für `show.redants.ch/bord`); danach zeigt er alle Hallen aus dem Backoffice als grosse Buttons. **Ein Klick** verbindet das Board mit der Halle, den Agent-Schlüssel holt es selbst aus der Cloud. Die zuletzt benutzte Halle ist rot umrandet.
3. **Bord benutzen:** Nach dem Klick erscheint das Bord. Das PowerShell-Fenster offen lassen, solange die Show läuft.
   Die **Matchuhr** läuft zusätzlich auf Port 80: `http://localhost/` (oder `/uhr`), zum Beispiel als Browser-Input in vMix. Ist Port 80 belegt, meldet das Board das im Fenster und die Uhr bleibt unter `http://localhost:5150/uhr`. Anderer Port: `clockPort` in `data\agent.json`, `0` schaltet ihn ab.
4. **Companion einrichten** (nur mit Streamdeck):
   - Im Bord ganz unten (kleine graue Zeile unter den Tasten) auf **„Companion-Modul"** klicken: Das lädt das neuste Modul `redants-halle-*.tgz` (sonst von den Releases [`halle-companion-v…`](https://github.com/haugjan/RedAnts/releases)). In Companion unter **Modules → Import** importieren.
   - Unter **Connections** je Streamdeck eine Instanz „RedAnts Halle" anlegen: **Agent** `http://localhost:5150`, **Deck** `mk2` für das Streamdeck mit 15 Tasten, `xl` für das mit 32 Tasten.
   - Unter **Buttons → Presets → RedAnts Halle** die Presets „Taste 1", „Taste 2", … der Reihe nach auf die Tasten ziehen, zeilenweise von links oben (Mk2: Taste 1–5 obere Reihe, 6–10 Mitte, 11–15 unten). Das geschieht einmal; was auf den Tasten steht, schickt das Board.
   - Nach einem Board-Update mit neuen Tastenbildern die Connection einmal aus- und wieder einschalten, weil Companion die Bilder zwischenspeichert.
5. **Nächstes Mal:** Doppelklick auf **„Red Ants Board"**. Es holt automatisch die neuste Version und fragt wieder nach der Halle.

**Halle wechseln, während das Board läuft:** oben links auf den Hallennamen (▾) klicken und bestätigen. Das Board startet neu und zeigt wieder „Welche Halle?".

**Ohne Internet:** Erreicht das Board die Cloud nicht, bietet die Hallenwahl „Ohne Internet in <letzte Halle> starten" an.

## Bedienung für Helfer (Streamdeck)

**Merksatz Tor: „Tor drücken – Schützin und Assist antippen – grünen Knopf drücken, wenn der Speaker den Namen sagt.“**

| Taste | Was sofort passiert | Danach auf dem Streamdeck |
|---|---|---|
| **Tor Red Ants** | Leinwand „Tor“ + Tor-Sound | 1. Torschützin antippen → sofort auf den Livestream · 2. Assist antippen (oder „Kein Assist“) · 3. grüner Knopf „LED Torschützin“ drücken, wenn der Speaker sie nennt · 4. grüner Knopf „LED Assist“ · 5. „✓ Fertig“ |
| **Tor Red Ants, Halle ohne LED** | Tor-Sound | Torschützin antippen → Livestream, fertig (kein Assist, kein Speaker-Knopf) |
| **Tor Gast** | Leinwand „Tor Gast“ + Sound | Torschützin antippen → nur Livestream, fertig |
| **Strafe** | Strafe-Sound + Leinwand „Strafe“ | Dauer wählen (2', 2'+2', 10', Spieldauer) → Spielerin antippen → Livestream; bei den Red Ants zusätzlich die Spielerin auf der Leinwand |
| **Starting 6 (Red Ants)** | Sound + Leinwand + Livestream „Starting 6“ | „▶▶ Weiter“ oder direkt auf einen Namen → diese Spielerin auf Livestream und Leinwand; nach der sechsten „✓ Fertig“ |
| **Starting 6 Gast** | nur was auf der Taste eingestellt ist (z.B. Sound) | nichts weiter |

- **Wohin die Spielerin kommt:** Im Backoffice unter Show → Decks hat jede Spielerinnen-Taste (Tor, Strafe, Best Player, Spielerin, Starting 6) die Häkchen "Livestream" und "LED-Wand". Nur LED = nur Leinwand. Ohne beides öffnet die Taste keine Auswahl und spielt nur ihre Befehle (z.B. Sound). Beim Tor gibt es Assist und Speaker-Knopf nur mit LED.
- **Kleines Streamdeck (Mk2):** Statt Namen kommt ein Ziffernblock. Rückennummer tippen, das Feld zeigt Nummer und Name, darauf drücken. ⌫ löscht eine Ziffer.
- **Vertippt:** „◀ Zurück“ bricht ab, nichts wird angezeigt. Bleibt der Speaker-Knopf 3 Minuten liegen, geht das Deck von selbst zur Startseite.
- **„Starting 6 fehlt“:** Im Bord unter ⚙ Spiel bei sechs Spielerinnen die Position 1–6 eintragen.
- Was beim ersten Druck sofort passiert (Sound, Leinwand, Livestream), stellt man im Backoffice bei der Taste ein. Die Spielerinnen kommen auf die Leinwand mit der LED-Vorlage `spielerin`, sonst `tor` (`start playlist -n "STR_RA_{player.number00}"`), der Assist mit `assist`, sonst derselben. Spielerinnen gibt es auf der Leinwand nur für die Red Ants.
## Board starten

**Auf dem Notebook in der Halle die Desktop-Verknüpfung „Red Ants Board" doppelklicken.** Sie lädt vor jedem Start die neuste Version, dann folgt die Hallenwahl im Browser.

Liegt noch keine Verknüpfung da, einmal installieren: PowerShell öffnen und diese Zeile einfügen (das `&` gehört dazu, sonst meldet PowerShell „is not recognized as the name of a cmdlet"):

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force; irm https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-WebApp/src/RedAnts.Show.Agent/Start-ShowAgent.ps1 -OutFile "$env:TEMP\Start-ShowAgent.ps1"; & "$env:TEMP\Start-ShowAgent.ps1"
```

Das Skript selbst: [Start-ShowAgent.ps1](Start-ShowAgent.ps1) ([direkt herunterladen](https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-WebApp/src/RedAnts.Show.Agent/Start-ShowAgent.ps1)). Es

1. lädt die neuste Version nach `%LOCALAPPDATA%\RedAnts\ShowAgent`,
2. legt die Desktop-Verknüpfung „Red Ants Board" an und räumt die alten Verknüpfungen „Show-Agent …" und „Show-Bord" weg,
3. startet das Board mit der Hallenwahl und öffnet sie im Browser.

Ein Update braucht nichts weiter als einen Neustart über die Verknüpfung: das Skript vergleicht die installierte Version mit dem neusten Release und holt sie bei Bedarf. Ist GitHub nicht erreichbar, startet die installierte Version weiter.

## Welche Halle?

- Die Hallen kommen aus dem Backoffice (Show → Hallen) über `GET /bord/hallen`, der Agent-Schlüssel der gewählten Halle über `GET /bord/hallen/{id}/agent`. Beide liegen hinter dem Bord-Passwort der Cloud.
- Das Bord-Passwort, die gewählte Halle und ihr Schlüssel stehen in `data\agent.json` (`cloudPassword`, `hallId`, `key`, `hallName`). Ein Passwort aus der früheren `data\hallen.json` übernimmt das Skript beim ersten Start.
- Die Cloud ist `https://show.redants.ch`; `-Cloud https://show-dev.redants.ch` für Tests. Wer `admin[-dev].redants.ch` angibt, landet automatisch auf der passenden `show`-Adresse.
- Das Programm startet mit `--choose-hall` in der Hallenwahl; ohne den Schalter (Linux-Dienst, `-Silent`) nimmt es die gespeicherte Halle. Beendet es sich mit Code 3, startet das Skript es neu in der Hallenwahl.

### Schalter des Skripts

| Schalter | Wirkung |
|---|---|
| `-Hall <kennung>` | startet ohne Menü in dieser Halle (so laufen die Hallen-Verknüpfungen) |
| `-Key <schlüssel>` | nimmt eine neue Halle gleich auf, zusammen mit `-Hall` |
| `-Cloud <url>` | andere Cloud, etwa `https://show-dev.redants.ch` |
| `-Silent` | keine Hallenwahl, keine Rückfrage: nimmt die zuletzt benutzte Halle |
| `-Autostart` | legt zusätzlich „Red Ants Board" in den Autostart (mit `-Silent`, also ohne Hallenwahl) |
| `-NoUpdate` | startet die installierte Version ohne Suche nach einer neuen |
| `-NoShortcut` | legt keine Verknüpfungen an |
| `-NoBrowser` | öffnet das Bord nicht automatisch im Browser |
| `-CheckOnly` | prüft Update und Konfiguration, startet den Agenten nicht |
| `-InstallDir <pfad>` | installiert woanders hin als `%LOCALAPPDATA%\RedAnts\ShowAgent` |

### Wenn PowerShell streikt

| Meldung | Grund |
|---|---|
| `... is not recognized as the name of a cmdlet` | der Pfad wurde ohne `&` aufgerufen: `& "C:\...\Start-ShowAgent.ps1"` |
| `running scripts is disabled on this system` | `Set-ExecutionPolicy -Scope Process Bypass -Force` voranstellen (gilt nur für dieses Fenster) |
| Das Fenster schliesst sofort | die Verknüpfung benutzen; sie bleibt bei einem Fehler offen stehen |

## Was wo liegt

| Pfad | Inhalt |
|---|---|
| `%LOCALAPPDATA%\RedAnts\ShowAgent` | `RedAnts.Show.Agent.exe`, `Start-ShowAgent.ps1`, `release.txt` |
| `…\data\agent.json` | aktive Halle: `cloudUrl`, `hallId`, `key`, `deckPort`, dazu `bind` und `boardPassword` fürs Hallennetz |
| `…\data\hallen.json` | nur noch von früheren Versionen: daraus übernimmt das Skript einmal das Bord-Passwort |
| `…\data\payload.json` | letzte Konfiguration aus der Cloud, damit der Agent auch offline startet |
| `…\data\musik` | zwischengespeicherte Stücke |

## Ports

| Richtung | Port |
|---|---|
| Companion, Bord, Uhr, Spielmaske, Status | TCP 5150 auf dem Agenten (lokal; mit `bind` plus `boardPassword` im Hallennetz) |
| Matchuhr iCast → Agent | UDP 50085 und 50086 |
| Bodet ScorePad → Agent | TCP 4001, der Agent hört, die Uhr verbindet sich |
| Agent → vMix | TCP 8099 |
| Agent → sedna LED-Wand | TCP 27182 |
| Agent → Cloud | HTTPS zu `show.redants.ch` (WebSocket für den Hub) |

`http://localhost:5150/status` zeigt, was der Agent sieht: Hub, Uhr, vMix, LED und das laufende Spiel. Eine Taste, deren Gerät die Halle nicht anbietet, bleibt leer statt zu scheitern; die Uhr wird nur gelesen, nie gestellt.

## Linux

```bash
./install-show-agent.sh --cloud https://show.redants.ch --hall <kennung> --key <schlüssel> --service
```

`--service` registriert den systemd-Benutzerdienst `redants-show-agent` (`systemctl --user status redants-show-agent`, Log mit `journalctl --user -u redants-show-agent -f`). Danach startet `start-show-agent.sh` den Agenten und lädt vorher die neuste Version.

## Board and match clock

The agent serves two pages of its own, both reachable from any browser in the hall network:

- `/bord` is the deck as a web page and replaces a Streamdeck: one tab per configured layout, the same keys with the same icons, plus score, time, period and the state pills above them. A press runs exactly what the Streamdeck key would run. The cloud shows the same board at `show.redants.ch/bord`.
- `/uhr` is a full screen match clock for a TV next to the court, Teko digits in fixed cells, the penalties of both sides with their player numbers, team logos or abbreviations around the score, the period label, amber during a break, red in the last minute and a lamp that says how long the clock has been silent.
- `/spiel` edits the game of the hall on the agent itself: title, referees, commentators and per team name, abbreviation, colour, coach, top scorer, starting six and roster. It works without the internet; the change goes to the cloud as soon as it is reachable, and a newer game from the cloud wins. Loading another game (swissunihockey, a game config file, by hand) stays in the cloud.

The three pages listen on `localhost` only. `agent.json` moves them into the hall network with `bind` (for example `0.0.0.0`), but only together with `boardPassword`: without a password the agent says so on the console and stays on `localhost`.

A hall only starts what it offers. Sound, match clock (with its type), vMix, LED wall and light show are switched per hall in the backoffice; the agent skips the links it does not offer, refuses scene steps that need them and leaves the matching deck keys empty.

## What a key can do

A key has a kind (`leer`, `Musik`, `Befehlsfolge`, `Feste Funktion`, `Seite öffnen`, `Anzeige`), set in the backoffice under Show > Decks. A command sequence runs step by step; a fixed function runs its sequence first and then takes over.

| Command | Target | Behaviour |
|---|---|---|
| `musik.start` | a music entry | reported to the cloud, the board in the browser plays it |
| `musik.stopp`, `musik.fade` | | the same way, stop and fade out |
| `vmix.ein` | `title:<kind>` | fills the fields of the hall's vMix title of that kind with the expanded templates and brings its overlay channel in |
| `vmix.text` | `title:<kind>`, default `message` | shows that title with the text typed on the key, for a note like "Empty Net" |
| `vmix.aus` | `title:<kind>` or empty | takes that channel out, empty takes out every configured channel |
| `led.start` | `led:<event>` | looks up the hall's LED template for the event, expands placeholders and sends it to sedna |
| `led.stopp` | `led:<event>` (default `stopp`) | the same, falls back to `stop playlist` |
| `warten` | milliseconds | pause |

The fixed functions carry their own flow: goal, penalty (2', 2'+2', 10', Spieldauer), best player, player, Starting 6 and the spectator counter open the player list on the deck or count on, the state keys switch overlay, clock, music and the automatic break music, and the music keys are stop, fade out, next, previous and pause.

Placeholders in LED commands and title fields: `{player.number}`, `{player.name}`, `{player.position}`, `{player.sponsor}`, `{player.photo}`, `{team.short}`, `{team.long}`, `{team.coach}`, `{team.logo}`, `{team.starting6}`, `{home.*}`, `{away.*}`, `{score}`, `{score.home}`, `{score.away}`, `{clock}`, `{period}`, `{period.label}`, `{penalty}`, `{text}`, `{event}`, `{side}`, `{game.title}`, `{game.league}`, `{game.location}`, `{referee1}`, `{referee2}`, `{commentator1}`, `{commentator2}`.

The scoreboard is a title of kind `scoreboard`: its fields map the clock slots (`time`, `period`, `homeScore`, `awayScore`, `homeName`, `awayName`, `homePen1`…`awayPen2`, `homePen1Fill`…) to vMix field names, `{0}` in a name expands to one field per digit (`M10`, `M01`, `T`, `S10`, `S01`). Without a configured scoreboard title the swiss unihockey `LUPL-SU_scoreboard_clock.gtzip` layout is used.

## Deck

A deck is a Streamdeck Mk2 (5 × 3 keys) or a Streamdeck XL (8 × 4). Its size is chosen when it is created and stays fixed. Every deck has its own start page; its sub-pages belong to the size, so every deck with the same number of keys offers the same pages. PANIC and Zurück sit on a fixed place per deck, Zurück only on sub-pages, where it is split: the name of the key that opened the page above, "Zurück" below. A key whose device the hall does not offer stays empty, and the keys are the same on the Streamdeck, on the agent's board and on the cloud board.

## Match clock telegrams

### iCast

UDP, UTF-32 big endian, every 100 ms, fields separated by `;`:

```
19:38;2;6;3;;;;;0;0;RED;UBO;GAME TIME;0;-6;0;-2;0;0;1
```

| Index | Meaning |
|---|---|
| 0 | Time: `mm:ss`, in the last minute `ss.z` (tenths, shown red); game time counts up, break and timeout count down |
| 1 / 2 | Goals home / away |
| 3 | Period: 1-3 thirds, 4 overtime, 5 penalty shootout (5 is an assumption) |
| 4 / 5 | Penalty home 1 / 2: `<number> <mm:ss>`, empty without a penalty |
| 6 / 7 | Penalty away 1 / 2 |
| 8 / 9 | Penalty lamp home / away |
| 10 / 11 | Abbreviation home / away |
| 12 | Mode: `GAME TIME`, `INTERMISSION`, `TIME-OUT`; per iCast also Warmup, Time to Warmup, Time to Face-Off, Local Time |
| 13 / 15 | Shots home / away (in handball mode the number of timeouts) |
| 14 / 16 | Saves home / away |
| 17 / 18 | Third penalty home / away: `<number> <mm:ss>` |
| 19 | Flag "end of game time", active for one second |
| 20 | Flag "end of timeout", active for one second |

Fields 0-12 and 13-20 are documented in the manual of iCast Sweden AB (Scoreboard Handball, version 2.0, in the
livestream folder under "11 Scoreboard Synchronisierung"); the penalty lamps 8 / 9 come from the existing Node-RED
flow for vMix and have not been verified against a real penalty.

**There is no "clock is running" signal.** The clock sends ten telegrams per second, standing still as well, and no
field changes on start or stop. The two flags only report the end of the game time and of the timeout. Whether the
clock runs can therefore only be seen from the time moving on: once per second during play, in tenths in the last
minute. When the hall clock shows the time of day it sends nothing.

### Bodet ScorePad

The ScorePad opens a TCP connection from its RJ45 port to the agent. It is set up in the `Menu technicien` (code 4934)
under `communication Protocols`: add the protocol **TV**, enter the IP address and port of this machine, start the
sport. The output exists on the MAIN keyboard only.

Every message is one frame:

```
01      7f        02     47      31 31 ...        03     2d
SOH   address    STX    CTRL    message          ETX    LRC
```

The LRC is the XOR of all bytes after SOH up to and including ETX, then `LRC = LRC and 0x7f`, and
`IF LRC < 32 THEN LRC = LRC + 32`. The first two message bytes are the message number as ASCII digits. A space
(`0x20`) stands for a leading zero in numeric fields, `'7'` is the identifier for floorball.

Byte positions are indices in the whole frame starting at 0:

| Message | Index | Meaning |
|---|---|---|
| **11** score | 6 / 7 | status word (bit 1: 1 = clock stopped) / sport |
| | 8-9 / 10-11 | minutes / seconds |
| | 12-14 / 15-17 | goals home / away, three digits |
| | 18 | number of the running period |
| **12** penalties home | 7 / 8 / 9-10 | indicator / minutes / seconds of the first penalty |
| | 11 / 12 / 13-14 | the same for the second penalty |
| **13** penalties away | like 12 | |
| **14** third penalty | 7-10 / 11-14 | third penalty home / away |
| **15** player numbers | 7-12 / 13-18 | tens and ones per penalty, home / away; `20H 20H` = no number |

In the last minute the clock runs in tenths. Index 10 then carries the separator `'D'` (`0x44`) instead of a digit,
and the time reads as `ss.z`, for example `56.4`.

The indicator of a penalty is a seven-segment code blinking between two values (such as `0x81` and `0x80`), so the
clock page does not rely on it and shows a penalty as long as its time is not `00:00`.

Unlike iCast the ScorePad sends neither team abbreviations nor a mode in these messages; home and away are set in the
hall configuration. The layout comes from Bodet's own description (document 608264B, "Scorepad Network output and
protocols"), reached through the Internet Archive because `static.bodet-sport.com` no longer resolves. The documented
example `01 7f 02 47 31 31 80 37 20 34 30 37 20 30 31 20 30 30 31 03 2d` reads as `04:07`, score `1:0`, first period,
and its LRC `0x2d` recomputes; a test covers both.

## Release

`.github/workflows/show-agent-release.yml` builds a self-contained `win-x64` single-file package on every change under `RedAnts-WebApp/src/RedAnts.Show.Agent/` and publishes it under the tag `show-agent-v<major.minor.run>`; the major and minor come from `<Version>` in the csproj.
