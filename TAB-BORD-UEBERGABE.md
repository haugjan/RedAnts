# Tab-Bord: Übergabe zum Fortsetzen der Session

Stand 28.11.2026. Diese Datei fasst zusammen, was in der Session "Tab-Based-Show" entschieden, gebaut und offen gelassen wurde, damit die Arbeit am neuen Bord in einer anderen Session oder auf einem anderen Rechner weitergeht. Sie ist die einzige Quelle, die dafür nötig ist; dazu kommen `CLAUDE.md`, `HALLENSTEUERUNG.md` und `ARCHITECTURE.md` im Repo.

## Was bereits auf main ist

| Commit | Inhalt |
|---|---|
| `af977a0c` | Persönliche Sponsoren nach TCunihockey-Muster: Halle mit Ordner Heim/Gast, Anzahl Logos je Nummer im INI-Format, vier Schalter, Rahmenfarbe; der Agent füllt `ImgSponsor.Source`, `RectSponsBackgr`, `RectSponsBorder`; `TitleSponsorTopUp` ergänzt die Felder einmalig. |
| `1b1eb8cc` | Topscorerin geht als `00` an vMix mit `SetImageVisibleOn ImgTopscorer.Source` (sonst Off); Titelfeld `<Bild>.Visible` ist ein Sichtbarkeitsschalter; feste Funktion `sponsor-vor` ("Nächster Sponsor") tauscht das Sponsorbild im laufenden Titel (`2/3` auf der Taste); `TitleTopScorerTopUp`. |
| `702359aa` | `show-agent-release.yml` veröffentlicht ein Release nur noch von main. |

Agent-Release von main: `show-agent-v1.0.100`. Beides ist auf PROD deployt (27.11.2026). Prod-Einrichtung, die noch aussteht: unter Show → Hallen → vMix → "Persönliche Sponsoren" die Ordner eintragen und `PersSponsorLUPL.txt` ins Textfeld einfügen; die Taste "Nächster Sponsor" unter Show → Decks platzieren; Hallen-PC auf Agent 1.0.100.

## Dokumente und Prototyp

- Konzeptdokument (Claude Doc, deutsch): https://claude.ai/artifact/WRepvzEEZwBE42w1wrL1fK ("Show-Modul Bedienkonzept Neu"). Enthält Ausgangslage, Ist-Analyse aus der Prod-Konfiguration, Leitideen, Vorschlag, Varianten A bis D, Ereignismodell, Nummerierte Reihen, Spielerinnen-Auswahl, Geräte, offene Fragen, Migrationspfad.
- Klickbarer Prototyp (Stand v7): https://claude.ai/artifact/9aDADwUc7BoaW3cBLPfKHE. Die Quelle liegt in diesem Branch unter `RedAnts-WebApp/docs/tab-bord-prototyp.html` (eine HTML-Datei, läuft ohne Server). Der Prototyp ist die Referenz für das Aussehen: Einstieg Halle, dann Liga; fünf Tabs über dem Raster; das Raster ist das Streamdeck (8 × 4 WIN 4, 5 × 3 Oberseen und Rennweg); Tasten mit `L` haben je Liga einen anderen Befehl oder fehlen; Tor als zwei Tasten "1. Sofort" und "2. Speaker"; Unterseiten im Raster mit Zurück oben links; "Reihen" unter Sonstiges schaltet zwischen Nachrücken und Platz halten; ⚙ Spiel simuliert Uhr und Geräte.

## Entscheidungen des Auftraggebers (verbindlich)

1. Bord und Streamdeck sehen gleich aus: das Bord zeigt das Deck-Raster mit denselben Farben und Plätzen (Zurück, Transport, PANIC wie heute).
2. Am bestehenden Bord (`/bord`, `Features/Board`, Decks unter Show → Decks) wird nichts geändert. Das neue Bord entsteht parallel. Was teilbar ist, wird geteilt: Musik (`IMusicLibrary`, Soundboard-Spieler), LED-Vorlagen, vMix-Titel, Hallen, Spiele.
3. Einstieg: zuerst Halle, dann Liga. Die Halle bestimmt, was zur Verfügung steht (LED nur WIN 4 usw.), wie heute über `HallCapabilities`.
4. Fünf fest codierte Tabs: Vor dem Spiel, Während dem Spiel, Pause, Nach dem Spiel, Sonstiges. Tabs sind nur auf dem Bord wählbar, nicht als Deck-Tasten; das Streamdeck zeigt den Tab des Bords.
5. Weniger Streamdeck-Seiten: keine Seiten je Liga mehr. Stattdessen kann eine Taste je Liga einen anderen Befehl haben (Einlaufmusik, LED-Standardbild) oder in einer Liga ausgeblendet sein (Mobiliar, LIDL nur L-UPL).
6. Nummerierte Reihen (heute "1. Mobiliar … 8. Bully") bleiben, die Nummer wird gezählt statt getippt, und die Reihe ist lückenlos, auch wenn Schritte in einer Liga fehlen. Zwei Darstellungsregeln sind im Prototyp gebaut: Nachrücken (neu zählen, Plätze rücken auf) und Platz halten (graue Taste "entfällt", Plätze und Nummern gleich).
7. Tor sind zwei Tasten: "Tor Heim · 1. Sofort" (heutiger Ablauf: Musik, LED-Animation, Schützin an vMix) und "Tor Heim · 2. Speaker" (zeigt das offene Tor auf der Taste, schickt die Schützin aus Schritt 1 auf die LED-Wand, fragt danach nur den Assist). Tor Gast hat eine Taste. Verworfen: Ereigniskarte, Ansage-Warteschlange, Arbeitsplätze, Startkarten, Ablauf-Listen mit Weiter-Taste, Erklärtexte auf dem Bord.
8. Die zusätzlichen vMix-Befehle müssen nicht mehr alle aufs Deck; eine Taste hat ein Flag "auf dem Deck", sonst nur auf dem Bord.

## Offene Entscheidungen (vor Etappe 1 klären)

Diese Fragen waren gestellt, aber noch nicht beantwortet:

- Reihen-Regel: Nachrücken (Empfehlung), Platz halten, oder je Reihe wählbar mit Vorgabe Nachrücken?
- Soll die Matchuhr den Tab mitführen (Vor Spiel, Spiel, Pause, Nach Spiel, Handübersteuerung bis zum nächsten Wechsel) oder nur von Hand? Empfehlung für den Start: nur von Hand.
- Strafe auch zweistufig (1. Sofort, 2. Speaker) oder eine Taste wie heute?
- Musik ohne eigenen Tab: Musiktasten in den Phasen, Transport fix unten, flache Liste mit Suche als Unterseite unter Sonstiges. Einverstanden?
- Adresse des neuen Bords (`/bord/neu` in der Cloud und beim Agenten) und Companion-Deck-IDs (`tabs-xl`, `tabs-mk2`).
- Ligenliste fest (L-UPL, U21, U17, 1. Liga) oder konfigurierbar, mit Vorbelegung aus dem geladenen Spiel.
- Umschaltung je Halle vom alten auf das neue Bord erst am Schluss.

## Vorgeschlagene Etappen

1. Modell und Admin: Datensatz `Show:TabBoard` (additiv, in `show.Settings`), Domain-Typen für Tabs, Tasten mit Befehl je Liga, Reihen, Deck-Flags je Format; Seeder mit der Startbelegung aus den heutigen Prod-Tasten (siehe Prototyp `PAGES`); Admin-Seite "Bord (Tabs)" neben "Decks".
2. Bord in der Cloud unter `/bord/neu`: Start mit Halle und Liga, Kontextleiste, fünf Tabs, Raster, Musik über den bestehenden Soundboard-Spieler. Damit sind Oberseen und Rennweg vollständig bedienbar.
3. Agent: dieselbe Seite lokal, Deck-Ansichten je Tab für XL und Mk2, Companion-Deck-IDs, Unterseiten Spielerliste, Strafenart, Zuschauer, Meldung.
4. Tor zweistufig: offene Tore im Agenten halten, "2. Speaker", Assist, Rückgängig.
5. Reihen-Regel umschaltbar, "Belegung zeigen", Playwright-Tests, Doku, Umschaltung je Halle.

Jede Etappe wird lokal vorgeführt (Agent-Profil gegen die Kopie-Datenbank, Simulator für vMix und LED) und einzeln abgenommen.

## Fakten aus der Prod-Konfiguration (25.11.2026)

Gelesen mit `RedAnts-WebApp/docs/show-config-dump.cs` und `show-config-analyze.ps1` (siehe unten).

- 9 Hallen, 6 davon für WIN 4 (`win4` "WIN4 Alles (L-UPL)", `win4-sound-led` "WIN4 Sound + LED (U17, U21)", `win4-livestream`, `win4-matchuhr`, `win4-sound`, `win4-sound-no-game-clock`), dazu `oberseen`, `oberseen-notebook`, `rennweg-ipad`. Vier sind sichtbar. Die Liga steht im Hallennamen.
- Drei Bibliotheksdecks: `xl` "Streamdeck XL (Full)", `mk2` "Streamdeck Mk2 (Full)", `deck-3` "Streamdeck Mk2 (Music Only)". Nur Music Only hat Tor Heim/Gast und Strafe Heim/Gast als Musiktasten.
- Seitensatz 5 × 3: 26 Seiten nach dem Muster Standort (Win4, Nicht Win4) × Liga (L-UPL, U21, U17, 1. Liga) × Phase (vor Spiel, Pause, nach Spiel); die Einlaufmusik liegt vier Tastendrücke tief. Die Vor-Spiel-Seiten sind nummerierte Reihen: L-UPL 1. Mobiliar, 2. Wartesong, 3. LIDL, 4. Einlaufmusik mit Intro, 5. Torbeschwörung, 6. Starting Six Gegner, 7. Starting Six Red Ants, 8. Bully; die anderen Ligen ohne Mobiliar und LIDL. Pause mit LED-Wand: Gastrowerbung, Gewinnspiel (+LED), Musik während Gewinnspiel, Pausenmusik; ohne LED-Wand neun Sponsoren-Ansagen (Gold, Mobiliar, LIDL, Silber, Nachwuchs, Bronze, Medical, Medien, Kooperation). Nach Spiel: Mobiliar, LIDL (L-UPL), Sieg oder Niederlage, Best Player.
- Die Ligen unterscheiden sich nur in der Einlaufmusik, in Mobiliar und LIDL (nur L-UPL) und im LED-Standardbild (`spiel-nla`, `spiel-u21`).
- 40 Musikeinträge in einem Profil `bibliothek` (Namen im Prototyp unter `MUSIC`).
- XL-Startseite: Tor, Coach, Timeout, Aufstellung je Seite; Strafe, Spieler (LED), Best Player, Starting 6 je Seite; Empty Net, Meldung, Red Ants Logo, Stand, Logo Gast, Replay, Vor Spiel/Pause, Kommentar, Schiri, LED Pause, LED Intermission, Uhr, Werbung, Einblender. Unterseiten Meldung, Vor Spiel/Pause, Teamlogos.

## TCunihockey als Vorbild (C:\TCUnihockey auf dem Arbeitsrechner)

Handbuch "unihockey.swiss LIVESTREAM - Anleitung TCunihockey V6.0.26.pdf", Kapitel 10.4 und 11. Mit `ilspycmd` dekompiliert (VB.NET, `FrmMain`), nur gelesen, nichts davon im Repo. Erkenntnisse, die der Agent heute umsetzt: Spielerinnen-Titel werden mit `SetText`, `SetColor`, `SetImage` gefüllt, Topscorerin als `00` mit `SetImageVisibleOn ImgTopscorer.Source`, Sponsorbild `ImgSponsor.Source` aus `<PersonalSponsorHomePath>\<Nummer>_<i>.png`, Anzahl je Nummer aus einer INI `[Home]` `7=2`, Box über `RectSponsBackgr`/`RectSponsBorder` mit Alpha `FF`/`00` ein- und ausgeblendet, Overlay-Kanal 2 für den Spielerinnen-Titel. Schemafarben: `luplw` `#FFF000`, `luplm` `#E60A14`, `su` `#C52F24`.

## Werkzeuge in diesem Branch

- `RedAnts-WebApp/docs/show-config-dump.cs`: datei-basiertes .NET-Skript, liest `show.Settings` (Show:*), `show.Halls`, `show.Profiles` und die Spiele einer Datenbank in einen Ordner `dump-<db>`. Aufruf aus einem Scratch-Ordner: `AZURE_CONFIG_DIR=C:\Users\Jan.Haug\.azure-redants DUMP_DIR=$(pwd) dotnet run show-config-dump.cs -- sqldb-redants-prod`. Die eigene IP muss in der SQL-Firewall stehen (Regel `local-jan-YYYYMMDD`). Die Hallen-JSON enthalten Agent-Schlüssel: nie committen.
- `RedAnts-WebApp/docs/show-config-analyze.ps1 -Dir dump-sqldb-redants-prod`: druckt Musikliste, Decks, Seitensatz und Hallen als Text.
- `RedAnts-WebApp/docs/tab-bord-prototyp.html`: der Prototyp.

## So geht es in einer neuen Session weiter

1. Repo klonen oder aktualisieren, eigenen Worktree auf einem neuen Branch ab `origin/main` anlegen (`feature/tab-board-<etappe>`), nie auf main arbeiten; dieser Übergabe-Branch wird nicht weitergeführt.
2. `CLAUDE.md`, `HALLENSTEUERUNG.md`, `ARCHITECTURE.md` (Abschnitte "Personal sponsors in the vMix titles", "The player on the LED wall") und diese Datei lesen; Prototyp öffnen.
3. Die offenen Entscheidungen oben klären, dann Etappe 1 bauen. Architekturregeln gelten (Slices, Handler, Ports, Architekturtests `dotnet test RedAnts-WebApp/tests/RedAnts.Architecture.Tests`).
4. Prüfen lokal: `dotnet run --project RedAnts-WebApp/src/RedAnts.Host --launch-profile Agent` gegen `sqldb-redants-agent` (Kopie von dev, bei Bedarf mit `az sql db copy` anlegen), Simulator `RedAnts.Show.Simulator` für vMix und LED, Browser-Tests in `RedAnts-WebApp/tests/RedAnts.BrowserTests`. URLs nie nur mit curl prüfen, immer im Browser.
5. Ausliefern wie gewohnt: Feature-Branch pushen (DEV), dann fragen "Auf prod deployen? Ja/Nein", dann `git push origin HEAD:main`; danach Branch und Worktree aufräumen. Agent-Releases entstehen nur noch von main.

Vorschlag für den ersten Satz an die neue Session: "Lies TAB-BORD-UEBERGABE.md und CLAUDE.md. Wir bauen das neue Tab-Bord parallel zum bestehenden Bord, Etappe 1 (Modell, Seeder, Admin-Seite). Meine Antworten auf die offenen Entscheidungen: …"
