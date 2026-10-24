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

## Setup

1. In the backoffice (`admin.redants.ch`, section Show) create the hall and copy its id and agent key.
2. Install it once:
   - Windows: `Install-ShowAgent.ps1 -Cloud https://show.redants.ch -Hall <id> -Key <key> [-Autostart]` writes `data\agent.json`, puts a shortcut on the desktop (and into Autostart) and starts the agent.
   - Linux: `./install-show-agent.sh --cloud https://show.redants.ch --hall <id> --key <key> [--service]`; `--service` registers a systemd user service (`systemctl --user status redants-show-agent`).
3. Afterwards `Start-ShowAgent.ps1` (or `start-show-agent.sh`) is enough: it fetches the newest GitHub release into `%LOCALAPPDATA%\RedAnts\ShowAgent` (Linux: `~/.local/share/redants/show-agent`) and starts it. Without any configuration the agent asks for cloud URL, hall id and key on the console; `--cloud`, `--hall`, `--key` and `--deck-port` override them.
4. `data\payload.json` keeps the last synced configuration, so the agent also starts without the cloud.

Without a hall in reach, `RedAnts.Show.Simulator` next to it stands in for the match clock, vMix and the LED wall.

`http://localhost:5150/status` shows what the agent sees (connections, clock, scene).

## Board and match clock

The agent serves two pages of its own, both reachable from any browser in the hall network:

- `/bord` is the deck as a web page and replaces a Streamdeck: one tab per configured layout, the same keys with the same icons, plus score, time, period and the state pills above them. A press runs exactly what the Streamdeck key would run. The cloud shows the same board at `show.redants.ch/bord`.
- `/uhr` is a full screen match clock for a TV next to the court, Teko digits in fixed cells, the penalties of both sides with their player numbers, team logos or abbreviations around the score, the period label, amber during a break, red in the last minute and a lamp that says how long the clock has been silent.

Both pages listen on `localhost` only. `agent.json` moves them into the hall network with `bind` (for example `0.0.0.0`), but only together with `boardPassword`: without a password the agent says so on the console and stays on `localhost`.

A hall only starts what it offers. Sound, match clock (with its type), vMix, LED wall and light show are switched per hall in the backoffice; the agent skips the links it does not offer, refuses scene steps that need them and leaves the matching deck keys empty.

## Scenes

A scene is an ordered list of steps. The agent knows these actions:

| Action | Target | Behaviour |
|---|---|---|
| `sound.play`, `sound.stop`, `sound.fade` | `tile:<id>` | reported to the cloud, the browser board plays |
| `led.start` | `led:<event>` | looks up the hall's LED template for the event, expands placeholders, appends `-o`/`-fi` from the step and sends it to sedna |
| `led.stop` | `led:<event>` (default `stopp`) | same, falls back to `stop playlist` |
| `overlay.show` | `title:<kind>` | fills the fields of the hall's vMix title of that kind with the expanded templates and brings its overlay channel in |
| `overlay.text` | `title:<kind>`, default `message` | shows that title with the text typed on the key, for a note like "Empty Net" |
| `overlay.hide` | `title:<kind>` or `title:*` | takes the channel out, or every configured channel |
| `light.scene` | `light:<id>` | reserved, logs only |
| `deck.context` | `Main`, `GoalHome`, `PlayerAway`, … | switches the Streamdeck page |
| `player.pick` | | waits until a player key is pressed on the deck (skipped when the run already carries a `player` argument) |
| `wait` | `ms` | pause |
| `wait.score` | `timeoutMs` | waits until the hall clock reports a new score, continues with a warning after the timeout |
| `scene.run` | scene id | runs another scene inline |

Placeholders in LED commands and title fields: `{player.number}`, `{player.name}`, `{player.position}`, `{player.sponsor}`, `{player.photo}`, `{team.short}`, `{team.long}`, `{team.coach}`, `{team.logo}`, `{team.starting6}`, `{home.*}`, `{away.*}`, `{score}`, `{score.home}`, `{score.away}`, `{clock}`, `{period}`, `{period.label}`, `{penalty}`, `{text}`, `{event}`, `{side}`, `{game.title}`, `{game.league}`, `{game.location}`, `{referee1}`, `{referee2}`, `{commentator1}`, `{commentator2}`.

The scoreboard is a title of kind `scoreboard`: its fields map the clock slots (`time`, `period`, `homeScore`, `awayScore`, `homeName`, `awayName`, `homePen1`…`awayPen2`, `homePen1Fill`…) to vMix field names, `{0}` in a name expands to one field per digit (`M10`, `M01`, `T`, `S10`, `S01`). Without a configured scoreboard title the swiss unihockey `LUPL-SU_scoreboard_clock.gtzip` layout is used.

## Deck

The field deck has four rows of eight keys, numbered 1 to 32. Key 17 is always "Zurück", key 25 always PANIC. Its start page carries: goal, coach, timeout and lineup per team in the first row; penalty, player, best player and Starting 6 in the second; message, period, both scores, highlight, result and opener in the third; PANIC, commentators, referees, the clock and the sponsor/overlay state keys in the fourth. The music deck is a Streamdeck Mk2 of fifteen keys: key 1 is "Zurück", key 11 PANIC, keys 12 to 15 are stop, pause, next and fade out, and the pages below it hold the songs of one league. A key whose scene is not configured in the cloud stays grey and does nothing. The clock itself is read-only: the agent only listens to the hall clock, it never starts or stops it.

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
