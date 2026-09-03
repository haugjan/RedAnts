# companion-module-redants-show

Bitfocus Companion module for the Red Ants Soundboard (the `Show` slice of the RedAnts app). It talks to the existing HTTP control API under `/api/show` and loads the board layout dynamically, so tiles, folders and profiles show up as Companion actions and drag-and-drop presets and stay in sync when the board changes.

This module lives in the RedAnts repository but is a standalone Node package: it is built and installed into Companion separately from the .NET app.

## What it does

- Polls `GET /api/show/state` and builds, dynamically:
  - **Actions**: `Kachel abspielen`, `Einzelnen Song abspielen`, `Ordner oeffnen`, `Zurueck`, `Home`, `Profil wechseln`, `Stopp`, `Pause`, `Weiter`, `Fade-out`.
  - **Presets**: one draggable button per playable tile (label + colour from the board) plus a `Transport` set.
  - **Variables**: `profiles_count`, `tiles_count`, `folders_count`, `room`.
- Sends control commands to `GET /api/show/{play|song|folder|back|home|profile|stop|pause|resume|fade}`.

The commands are relayed by the server to a **connected board circuit**. A board (`/show`) must be open in a browser, otherwise the API answers `{ ok: true, boards: 0 }` and nothing plays.

## Configuration

| Field | Meaning |
|---|---|
| Server-URL | `https://show.redants.ch` (prod) or `https://show-dev.redants.ch` (dev) |
| API-Key | `Show:ApiKey`, falling back to the board password (`Show:BoardPassword`) |
| Board-Code (Room) | optional; scopes commands to one board (see below). Empty = all boards |
| Abfrage-Intervall | seconds between `/state` polls (default 15) |

## Multiple boards / multiple games at once (rooms)

The server API is otherwise a broadcast: without a room, a `play`/`stop` reaches **every** connected board. To run several games against the **same server instance** without cross-triggering, use a **Board-Code (room)**:

1. Open each board with a room in the URL, e.g. `https://show.redants.ch/show?room=halleA&key=...`. The board shows the code as a badge in the header.
2. Enter the same code in this module's `Board-Code (Room)` field.
3. The server then delivers each command only to boards registered with that code (`ShowRemote` matches the room; `Register(room, handler)` / `DispatchAsync` filter). An empty room still broadcasts, which keeps the single-board setup working unchanged.

So: one server, N boards, each `board <-> Companion` pair isolated by its room. The catalog (`/state`) stays shared and read-only, which is fine because it only feeds the action/preset dropdowns.

## Build & install into Companion

Requires Node 18+ and yarn.

```
cd companion-module-redants-show
yarn install
yarn build        # produces pkg/ via @companion-module/tools
```

Then in Companion: `Settings -> Developer modules path` -> point it at this folder (dev), or import the built package. See the Bitfocus docs on developer modules for your Companion version. Depending on the Companion version you may need to change `companion/manifest.json` -> `runtime.type` to `node22`.

## Limitations / next steps

- Live "which tile is currently playing" feedback needs a new server endpoint that reports playback state (the board knows it client-side; `/api/show/state` only returns the layout). Once such an endpoint exists, add a boolean feedback and highlight the active tile.
- Playing a tile assumes the target board is on the profile/folder that contains it; use `Profil wechseln` / `Ordner oeffnen` first, or place presets per profile.
