# RedAnts GameClock (Matchuhr)

Kleine Blazor-Server-Webapp (.NET 8), die die UDP-Telegramme der Hallen-Matchuhr empfängt und Spielzeit, Spielstand und Drittel live im Browser anzeigt. Schwarzer Hintergrund, Zeit maximal gross, Logos Red Ants (immer Heim) und Gastteam.

## Starten

**Windows:** `Start-GameClock.cmd` doppelklicken, oder in PowerShell:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-GameClock/Start-GameClock.ps1)))
```

**Linux (x64 / ARM64, z.B. Raspberry Pi):**

```bash
curl -fsSL https://raw.githubusercontent.com/haugjan/RedAnts/main/RedAnts-GameClock/start-gameclock.sh | bash
```

Das Skript lädt das neueste Release (Tag `gameclock-v*`), installiert es (Windows: `%LOCALAPPDATA%\RedAnts-GameClock`, Linux: `~/.local/share/redants-gameclock`), startet die App und gibt die URL aus. Ist kein Internet verfügbar, startet es die bereits installierte Version (`-Offline` bzw. `--offline` überspringt die Suche). Port ändern: `-Port 8080` bzw. `PORT=8080`.

Die Anzeige läuft unter `http://localhost:5080/` und von anderen Geräten unter `http://<IP>:5080/`. Vollbild mit F11.

## Konfiguration

`appsettings.json` neben der Exe:

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `Urls` | `http://0.0.0.0:5080` | Adresse der Webapp |
| `GameClock:Port` | `50085` | UDP-Port, auf dem die Matchuhr sendet |
| `GameClock:Ip` | `172.20.1.143` | Absender-IP der Matchuhr, `any` für alle |
| `GameClock:LogDir` | `logs` | Rohdaten-Log, eine Zeile pro Änderung |
| `GameClock:Home` | Red Ants | Name und Logo des Heimteams |
| `GameClock:Guests` | BEO, UBO | Gastteams, Schlüssel = Kürzel, das die Uhr sendet; Logos unter `wwwroot/logos/` |

Ohne Eintrag in `Guests` zeigt die Anzeige das Kürzel als Text.

## Aufbau

- `UdpReceiver` (Hosted Service) liest die Telegramme, schreibt Änderungen ins Rohdaten-Log und reicht sie an `ClockHub` weiter (bei Änderung sofort, sonst 1x pro Sekunde).
- `Components/Pages/Clock.razor` (Interactive Server) abonniert `ClockHub.Changed` und zeigt nach 3 s ohne Daten einen Hinweis.
- `wwwroot/reconnect.js` startet Blazor mit unbegrenzten Reconnect-Versuchen und lädt die Seite neu, wenn der Server die Sitzung nicht mehr kennt (z.B. nach Neustart).
- `GET /api/state` liefert den aktuellen Stand als JSON.

## Telegramm der Matchuhr

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
| 10 / 11 | Kürzel Heim / Gast |
| 12 | Modus: `GAME TIME`, `INTERMISSION` (Pause), `TIME-OUT`; laut iCast ausserdem Warmup, Time to Warmup, Time to Face-Off, Local Time |
| 8–9, 13–19 | nicht entschlüsselt; siehe Rohdaten-Log |

Die Quelle ist die Scoreboard-Ausgabe von iCast (iCast Sweden AB). Die Belegung der Strafenfelder stammt aus dem bestehenden Node-RED-Flow für vMix und ist noch nicht an einer echten Strafe überprüft.

Zeigt die Hallenuhr die Tageszeit, sendet sie nichts; die Anzeige blendet dann nach 3 s ab.

## Release

`.github/workflows/gameclock-release.yml` baut bei jeder Änderung in diesem Ordner self-contained Pakete für `win-x64`, `linux-x64` und `linux-arm64`. Version = Major.Minor aus `<Version>` in der csproj plus Run-Nummer; auf `main` entsteht ein Release `gameclock-v<version>` mit den Paketen und den Start-Skripten.
