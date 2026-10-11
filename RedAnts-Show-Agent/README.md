# RedAnts Show-Agent

The hall agent is the local half of the Show module: a headless .NET service on the streaming notebook that executes what the cloud (`show.redants.ch`, module `RedAnts.Show`) has configured for its hall. It talks to the devices that are only reachable in the hall and reports back.

| Direction | What |
|---|---|
| Cloud → agent | `Sync` (hall settings, scenes, active game), `Run` (scene id plus arguments), `Panic`, `Press` (deck and key number) and `View` (the live deck of one layout, answered by the agent) over the SignalR hub `/hubs/halle` |
| Agent → cloud | `Report(kind, json)`: `status` (lamps for vMix, Uhr, LED plus details), `score`, `clock`, `deck`, `overlay`, `sound`, `log` |
| Agent → vMix | scoreboard fields from the hall clock, overlay titles for scenes (TCP 8099, `SetText`/`SetColor`/`SetImage`, `OverlayInput<n>In/Out`) |
| Agent → LED wall | sedna presenter commands (TCP 27182), templates per event from the hall settings |
| Hall clock → agent | iCast telegrams on UDP 50085 (13 fields, UTF-32 BE); manual score fallback on the deck when no telegram arrives |
| Companion → agent | the fixed 32-key deck: `GET /deck/view?since=`, `GET /deck/image?key=`, `POST /deck/press/{1..32}` on `http://localhost:5150` (the `companion-module-redants-tcconsole` module works unchanged) |

Music never runs on the agent: `sound.*` steps are reported to the cloud, the DJ board in the browser plays them.

## Setup

1. In the backoffice (`admin.redants.ch`, section Show) create the hall and copy its id and agent key.
2. Install it once:
   - Windows: `Install-ShowAgent.ps1 -Cloud https://show.redants.ch -Hall <id> -Key <key> [-Autostart]` writes `data\agent.json`, puts a shortcut on the desktop (and into Autostart) and starts the agent.
   - Linux: `./install-show-agent.sh --cloud https://show.redants.ch --hall <id> --key <key> [--service]`; `--service` registers a systemd user service (`systemctl --user status redants-show-agent`).
3. Afterwards `Start-ShowAgent.ps1` (or `start-show-agent.sh`) is enough: it fetches the newest GitHub release into `%LOCALAPPDATA%\RedAnts\ShowAgent` (Linux: `~/.local/share/redants/show-agent`) and starts it. Without any configuration the agent asks for cloud URL, hall id and key on the console; `--cloud`, `--hall`, `--key` and `--deck-port` override them.
4. `data\payload.json` keeps the last synced configuration, so the agent also starts without the cloud.

Without a hall in reach, `RedAnts-Show-Simulator` stands in for the match clock, vMix and the LED wall.

`http://localhost:5150/status` shows what the agent sees (connections, clock, scene).

## Board and match clock

The agent serves two pages of its own, both reachable from any browser in the hall network:

- `/bord` is the deck as a web page and replaces a Streamdeck: one tab per configured layout, the same keys with the same icons, plus score, time, period and the state pills above them. A press runs exactly what the Streamdeck key would run. The cloud shows the same board at `show.redants.ch/bord`.
- `/uhr` is a full screen match clock for a TV next to the court: teams, score, running time, period and the current penalties.

A hall only starts what it offers. Sound, match clock (with its type), vMix, LED wall and light show are switched per hall in the backoffice; the agent skips the links it does not offer, refuses scene steps that need them and leaves the matching deck keys empty.

## Scenes

A scene is an ordered list of steps. The agent knows these actions:

| Action | Target | Behaviour |
|---|---|---|
| `sound.play`, `sound.stop`, `sound.fade` | `tile:<id>` | reported to the cloud, the browser board plays |
| `led.start` | `led:<event>` | looks up the hall's LED template for the event, expands placeholders, appends `-o`/`-fi` from the step and sends it to sedna |
| `led.stop` | `led:<event>` (default `stopp`) | same, falls back to `stop playlist` |
| `overlay.show` | `title:<kind>` | fills the fields of the hall's vMix title of that kind with the expanded templates and brings its overlay channel in |
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

Four rows of eight keys, numbered 1 to 32. Key 17 is always "Zurück", key 25 always PANIC. The start page mirrors the former TCConsole layout: goal, coach, timeout and lineup per team in the first row; penalty, player, best player and Starting 6 in the second; message, period, both scores, highlight, result and opener in the third; PANIC, commentators, referees, the clock and the sponsor/overlay state keys in the fourth. A key whose scene is not configured in the cloud stays grey and does nothing. The clock itself is read-only: the agent only listens to the hall clock, it never starts or stops it.

## Release

`.github/workflows/show-agent-release.yml` builds a self-contained `win-x64` single-file package on every change under `RedAnts-Show-Agent/` and publishes it under the tag `show-agent-v<major.minor.run>`; the major and minor come from `<Version>` in the csproj.
