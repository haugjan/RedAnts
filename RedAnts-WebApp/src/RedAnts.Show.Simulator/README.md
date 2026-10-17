# RedAnts Show-Simulator

Stand-ins for the three hall devices the Show agent talks to, so the agent, the scenes and the decks can be tested on any notebook without a hall. One Blazor Server app (`RedAnts.Show.Simulator.csproj`, .NET 10) with four panels on `http://localhost:5090`:

| Panel | What it does |
|---|---|
| Matchuhr | Sends iCast telegrams (13 `;`-separated fields, UTF-32 BE, 10 per second) to UDP `127.0.0.1:50085`. The operator drives it: start/stop, +1 min, goals, corrections, period, mode (GAME TIME, INTERMISSION, TIME-OUT, …), penalties, team names, count up or down. |
| vMix | A TCP API stand-in on port 8099: records every `FUNCTION` (`SetText`, `SetColor`, `SetImage`, `OverlayInput<n>In/Out`), answers `XML` with the real swissunihockey GT titles and their fields, and draws the live program output. Every overlay that is on air is rendered from the title's own geometry, colours, Hurme fonts and team logos (`wwwroot/titles`, converted from the LUPL title package), so an insert looks the way it does on the stream. Text shrinks to its box like GT does, and a field set to an empty value hides its box. |
| LED-Wand | A sedna presenter stand-in on port 27182: shows the running main and overlay playlist from `start playlist -n "…" [-o]` and `stop playlist`, plus every received command. |
| Agent | Polls `http://localhost:5150/status`, shows the lamps and the deck page, presses deck keys (PANIC is 25) and keeps the merged log of all panels. |

## Run

Windows: `Start-ShowSimulator.ps1` (or `.cmd`) downloads the newest release into `%LOCALAPPDATA%\RedAnts\ShowSimulator`, starts it and opens the browser. Linux: `start-show-simulator.sh [port]`. From the repository: `dotnet run --project RedAnts-WebApp/src/RedAnts.Show.Simulator`.

For an end-to-end test set the hall's vMix host and sedna host to `127.0.0.1` in the Show admin, start the agent for that hall and the simulator on the same machine, then press keys in the Agent panel or run scenes from the match-day page: the clock values appear in the scoreboard input, overlays come and go on the vMix panel, playlists on the LED panel.

## Release

`.github/workflows/show-simulator-release.yml` publishes self-contained single-file packages for `win-x64`, `linux-x64` and `linux-arm64` under the tag `show-simulator-v<major.minor.run>`.

## Einblender

`wwwroot/titles` holds one JSON layout per GT title (position, size, colour, font and text of every element), the images extracted from the title files, the two Hurme fonts and the league team logos. The names match the vMix input titles (`LUPL-SU_scoreboard_clock.gtzip` and so on), which is how the stage picks the layout for an overlay. A new title package is converted with the same script that produced these files: flatten the GT compositions to absolute coordinates, write one JSON per title and copy the embedded bitmaps next to it.
