# RedAnts Hallensteuerung — Companion-Modul

Bitfocus-Companion-Modul für die Hallensteuerung. Es liegt im RedAnts-Repo, ist
aber ein eigenständiges Node-Paket und wird getrennt released
(`.github/workflows/halle-companion-release.yml`, Tag `halle-companion-v*`).

## Die Idee

Das Modul kennt ausschliesslich **Tastennummern**. Was auf einer Taste steht und
was ein Druck auslöst, entscheidet allein der Agent in der Halle; die Halle wird
im Backoffice unter „Show" eingerichtet und kann sich jederzeit ändern, ohne dass
in Companion etwas angefasst werden muss.

Die Zuordnung wird deshalb **einmal** angelegt und nie wieder geändert:

- Aktion `press_slot` mit der Tastennummer
- Feedback `slot` mit derselben Nummer (liefert Text, Farbe und Tastenbild)

Für jede Taste gibt es ein fertiges Preset „Taste 1“, „Taste 2“, … — einfach auf
das Deck ziehen. Die Vorschau zeigt bewusst nur die Nummer; den Inhalt bekommt die
Taste erst auf dem Deck, live vom Agenten. Das
Raster ergibt sich aus dem Deck in der Hallenkonfiguration (Spalten × Zeilen,
zeilenweise durchnummeriert von links oben).

## Mehrere Decks

Eine Halle kann mehrere Streamdecks haben (zum Beispiel ein XL in der Halle und
ein Mk2 am DJ-Pult). **Je Streamdeck eine Instanz** anlegen und im Feld „Deck"
die Kennung eintragen (`xl`, `mk2`, …, wie in der Hallenkonfiguration). Jede
Instanz holt nur ihr eigenes Deck und schickt Tastendrücke mit dieser Kennung
zurück.

## Einrichtung

1. `redants-halle-*.tgz` von der
   [Release-Seite](https://github.com/haugjan/RedAnts/releases) laden.
2. In Companion unter Settings → Modules → Import importieren.
3. Je Deck eine Instanz anlegen: **Agent-URL** `http://localhost:5150`
   (Voreinstellung) und die **Deck-Kennung**.
4. Die Presets auf eine Seite ziehen.

Der Agent muss laufen. Steht er still, meldet das Modul `ConnectionFailure` und
versucht es alle drei Sekunden erneut.

## Was sonst noch im Modul steckt

- **Musiksteuerung und PANIC**: Aktionen für Stopp, Fade-out, nächstes Stück,
  voriges Stück, Pause und PANIC, aber keine eigenen Presets: diese Tasten
  liefert der Agent im Deck wie jede andere. Die Aktionen bleiben nur, damit
  bestehende Belegungen weiter funktionieren.
- **Startseite**: eine Aktion, die das Deck zurück auf die Startseite schickt.
- **Variablen**: `$(halle:deck)`, `$(halle:seite)`, `$(halle:zeit)`,
  `$(halle:drittel)`, `$(halle:stand_heim)`, `$(halle:stand_gast)` und
  `$(halle:musik)`.

## Wie die Anzeige live bleibt

Das Modul hält per Long-Poll eine Anfrage auf `/deck/view?since=<version>` offen;
der Agent antwortet, sobald sich am Deck etwas ändert (Stand, Drittel, Kader,
Seitenwechsel, Zustandstasten), spätestens nach 25 s.

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
