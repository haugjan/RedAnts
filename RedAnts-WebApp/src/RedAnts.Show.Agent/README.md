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

## Installation in a hall

The agent brings its own .NET runtime, so nothing has to be installed first. It needs a notebook in the hall network that reaches the internet, and the devices of the hall (match clock, vMix, LED wall) on that same network.

1. **Set the hall up in the cloud**: backoffice `admin.redants.ch`, section Show > Hallen. Create the hall, switch on what it offers (sound, match clock plus its type, vMix, LED wall), pick its decks, then copy its **id** and its **agent key**.
2. **Fetch the package**: the newest release under <https://github.com/haugjan/RedAnts/releases> carries the tag `show-agent-v<version>`. Windows takes `show-agent-<version>-win-x64.zip`, Linux `show-agent-<version>-linux-x64.tar.gz` or `-linux-arm64.tar.gz`. Unpack it anywhere, the installer copies what it needs.
3. **Install once**:
   - Windows, in PowerShell inside the unpacked folder:
     ```powershell
     .\Install-ShowAgent.ps1 -Cloud https://show.redants.ch -Hall <id> -Key <key> -Autostart
     ```
     It writes `%LOCALAPPDATA%\RedAnts\ShowAgent\data\agent.json`, puts a "Show-Agent" shortcut on the desktop (with `-Autostart` also into the user's autostart folder) and starts the agent right away.
   - Linux:
     ```bash
     ./install-show-agent.sh --cloud https://show.redants.ch --hall <id> --key <key> --service
     ```
     `--service` registers the systemd user service `redants-show-agent` (`systemctl --user status redants-show-agent`, log with `journalctl --user -u redants-show-agent -f`).
4. **Check**: `http://localhost:5150/status` lists what the agent sees (hub, clock, vMix, LED, current game). In the backoffice the hall shows as connected, and `show.redants.ch/bord` mirrors the deck of that hall.

Day to day the desktop shortcut is enough: `Start-ShowAgent.ps1` (`start-show-agent.sh`) loads the newest release into `%LOCALAPPDATA%\RedAnts\ShowAgent` (Linux `~/.local/share/redants/show-agent`) and starts it, so **an update is nothing but a restart through the shortcut**. `-NoUpdate` keeps the installed version, `--cloud`, `--hall`, `--key` and `--deck-port` override the configuration for one run.

### data/agent.json

| Key | Meaning |
|---|---|
| `cloudUrl` | `https://show.redants.ch` (dev: `https://show-dev.redants.ch`); the hub is `<cloudUrl>/hubs/halle` |
| `hallId` | the id of the hall in the backoffice |
| `key` | its agent key; it may be renewed in the backoffice, then the file needs the new one |
| `deckPort` | port of the local endpoints, `5150` by default |
| `bind` | `localhost` by default; `0.0.0.0` offers board, clock and game mask to the hall network |
| `boardPassword` | required as soon as `bind` leaves localhost, otherwise the agent says so and stays local |

Without a configuration the agent asks for cloud URL, hall id and key on the console. `data/payload.json` keeps the last synced configuration, so it also starts when the cloud is out of reach, and `data/musik` caches the music.

### Ports

| Direction | Port |
|---|---|
| Companion, board, match clock, game mask, status | TCP 5150 on the agent (local, or the hall network with `bind` plus `boardPassword`) |
| iCast match clock → agent | UDP 50085 and 50086 |
| Bodet ScorePad → agent | TCP 4001, the agent listens and the clock connects |
| agent → vMix | TCP 8099 |
| agent → sedna LED wall | TCP 27182 |
| agent → cloud | HTTPS to `show.redants.ch` (WebSocket for the hub) |

### When something is missing

- The status page names every link and its last error; the deck keys of a device the hall does not offer stay empty instead of failing.
- A key that needs vMix or the match clock is hidden in a hall without it, so the same deck runs in every hall.
- The clock is read only: the agent listens to the hall clock and never starts or stops it. Without a telegram the score may be corrected on the deck.
- A board on the hall network answers under `http://<notebook>:5150/bord` and asks for `boardPassword` once.

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
