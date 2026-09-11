# companion-module-redants-show

Bitfocus Companion module for the Red Ants Soundboard (the `Show` slice of the RedAnts app). Companion buttons are only **slots 1–15**; their text, icon, colour and what a press triggers come live from the board that is open in a browser.

This module lives in the RedAnts repository but is a standalone Node package: it is built and released separately from the .NET app (`.github/workflows/companion-release.yml` publishes a `.tgz` on every change on `main`).

## How it works

- The board (`/show`) publishes its current view (profile, open folder, the 15 cells of its 5×3 grid, what is playing) to the server whenever it re-renders.
- The module long-polls `GET /api/show/view?since=<version>`; the server answers as soon as the view changes (or after 25 s), so the Streamdeck follows the board without delay.
- Slot `n` is the board cell in row `(n-1) / 5`, column `(n-1) % 5`. `GET /api/show/press/{n}` makes the board do what a tap on that cell does: play or stop a tile, open a folder, go back, pause/resume or fade.
- Profiles: the slots follow the board's active profile and folder. `profile-next`, `profile-prev` and `profile/{id}` switch it remotely.

## Actions, feedbacks, variables, presets

- **Actions**: `Slot drücken`, `Profil: nächstes`, `Profil: vorheriges`, `Profil wählen`, `Zurück`, `Home`, `Stopp`, `Pause`, `Weiter`, `Fade-out`.
- **Feedback**: `Slot-Anzeige` sets text (icon + label), text colour and background of a slot; a playing tile is shown inverted, disabled cells dimmed.
- **Variables**: `connected`, `profile`, `path`, `now_playing`, `unlocked`, `room`, `slot_1` … `slot_15`.
- **Presets**: `Slots` only (Slot 1–15, no board-specific values); back, pause and fade are board cells and arrive as slots. Without a connection a slot shows its number.

## Configuration

| Field | Meaning |
|---|---|
| Server-URL | `https://show.redants.ch` (prod) or `https://show-dev.redants.ch` (dev) |
| API-Key | `Show:ApiKey`, falling back to the board password (`Show:BoardPassword`) |
| Board-Code (Room) | optional; binds the module to the board opened with `?room=CODE`. Empty = the most recently active board, and commands reach every board |

## Build

Requires Node 22 and yarn.

```
cd RedAnts-Show-Companion
yarn install
yarn package      # produces pkg.tgz via @companion-module/tools
```

Import the `.tgz` in Companion under Modules → Import module package, or download it from the GitHub release.
