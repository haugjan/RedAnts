# RedAnts Show

Steuert das Red Ants Soundboard ueber die `/api/show`-HTTP-API. Die Kacheln, Ordner und Profile des Boards werden dynamisch als Actions und Presets geladen.

## Einrichten

1. **Server-URL**: `https://show.redants.ch` (prod) oder `https://show-dev.redants.ch` (dev).
2. **API-Key**: `Show:ApiKey`, ersatzweise das Board-Passwort.
3. **Board-Code (Room)**: optional. Fuer mehrere gleichzeitige Spiele je Board einen eigenen Code verwenden und das Board mit `?room=CODE` oeffnen. Leer = alle Boards.

Das Board (`/show`) muss im Browser offen sein, sonst kommt der Befehl nirgends an (`boards: 0`).

## Presets

- **Kacheln**: je ein Button pro spielbarer Kachel, Farbe/Text vom Board.
- **Transport**: Stopp, Pause, Weiter, Fade-out, Zurueck, Home.
