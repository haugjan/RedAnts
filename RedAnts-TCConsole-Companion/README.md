# RedAnts TCConsole — Companion-Modul

Bitfocus-Companion-Modul für TCConsole. Es liegt im RedAnts-Repo, ist aber ein
eigenständiges Node-Paket und wird getrennt released
(`.github/workflows/tcconsole-module-release.yml`, Tag `tcconsole-module-v*`).

## Die Idee

Das Modul kennt ausschliesslich die **Tastennummern 1–32**. Es weiss nicht, was
auf einer Taste steht, und nicht, was ein Druck auslöst — das entscheidet allein
TcuConsole und kann sich jederzeit ändern.

Die Zuordnung wird deshalb **einmal** angelegt und nie wieder angefasst:

- Aktion `press_slot` mit der Tastennummer
- Feedback `slot` mit derselben Nummer (liefert Text, Farbe und Tastenbild)

Für jede der 32 Tasten gibt es ein fertiges Preset unter „Tasten 1–32" — einfach
auf das Deck ziehen. In der Preset-Liste tragen sie nur ihre Nummer; Bild, Farbe
und Beschriftung erscheinen erst auf der platzierten Taste, zur Laufzeit von
TcuConsole. (Companion führt Feedbacks auch in der Vorschau aus; das Modul
erkennt Vorschau-Tasten an ihrer Kennung `preset:…` und zeigt dort bewusst nur
die Nummer.) Das Raster ist 4×8, durchnummeriert zeilenweise von links
oben (1) nach rechts unten (32); Stream Deck XL passt genau.

## Einrichtung

1. `redants-tcconsole-*.tgz` von der
   [Release-Seite](https://github.com/haugjan/RedAnts/releases) laden.
2. In Companion unter Settings → Modules → Import importieren.
3. Instanz anlegen, als **TcuConsole-URL** `http://localhost:5150` eintragen
   (Voreinstellung).
4. Die 32 Presets auf eine Seite ziehen.

TcuConsole muss laufen. Steht es still, meldet das Modul
`ConnectionFailure` und versucht es alle drei Sekunden erneut.

## Wie die Anzeige live bleibt

Das Modul hält per Long-Poll eine Anfrage auf `/deck/view?since=<version>` offen;
TcuConsole antwortet, sobald sich am Deck etwas ändert (Stand, Drittel,
Spielerkader, Kontextwechsel, Zustandsknöpfe), spätestens nach 25 s.

Tastenbilder kommen nicht in der Ansicht mit — sie enthält nur den Schlüssel
`icon|label|textfarbe`. Das Modul holt jedes Bild einmal über `/deck/image` und
merkt es sich. Die Beschriftung ist ins PNG gerechnet, weil Companion die
Textfläche nicht begrenzen kann und der Text sonst über das Motiv liefe.

## Entwicklung

```bash
yarn install
yarn package
```

`@companion-module/base` erwartet Node 18 oder 22 (die CI nutzt 22). Unter einer
neueren Node-Version braucht `yarn install` ein `--ignore-engines`.
