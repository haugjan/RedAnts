# RedAnts Show-Simulator

Stand-ins for the three hall devices the Show agent talks to, so the agent, the scenes and the decks can be tested on any notebook without a hall. One Blazor Server app (`RedAnts.Show.Simulator.csproj`, .NET 10) with four panels on `http://localhost:5090`:

| Panel | What it does |
|---|---|
| Matchuhr | Sends iCast telegrams (13 `;`-separated fields, UTF-32 BE, 10 per second) to UDP `127.0.0.1:50085`. The operator drives it: start/stop, +1 min, goals, corrections, period, mode (GAME TIME, INTERMISSION, TIME-OUT, …), penalties, team names, count up or down. |
| vMix | A TCP API stand-in on port 8099: records every `FUNCTION` (`SetText`, `SetColor`, `SetImage`, `OverlayInput<n>In/Out`), answers `XML` with the configured inputs and their fields (LUPL scoreboard plus GT0.4 titles) and shows which overlay channel carries which input. |
| LED-Wand | A sedna presenter stand-in on port 27182: shows the running main and overlay playlist from `start playlist -n "…" [-o]` and `stop playlist`, plus every received command. |
| Agent | Polls `http://localhost:5150/status`, shows the lamps and the deck page, presses deck keys (PANIC is 25) and keeps the merged log of all panels. |

## Run

Windows: `Start-ShowSimulator.ps1` (or `.cmd`) downloads the newest release into `%LOCALAPPDATA%\RedAnts\ShowSimulator`, starts it and opens the browser. Linux: `start-show-simulator.sh [port]`. From the repository: `dotnet run --project RedAnts-Show-Simulator`.

For an end-to-end test set the hall's vMix host and sedna host to `127.0.0.1` in the Show admin, start the agent for that hall and the simulator on the same machine, then press keys in the Agent panel or run scenes from the match-day page: the clock values appear in the scoreboard input, overlays come and go on the vMix panel, playlists on the LED panel.

## Release

`.github/workflows/show-simulator-release.yml` publishes self-contained single-file packages for `win-x64`, `linux-x64` and `linux-arm64` under the tag `show-simulator-v<major.minor.run>`.
