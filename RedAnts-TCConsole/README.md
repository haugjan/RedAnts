# TCConsole

Windows-Werkzeug (.NET 8, WPF) für den Livestream: es steuert TCunihockey und
liefert dem Companion-Modul die 32 Tasten des Decks.

TCunihockey selbst gehört nicht in dieses Repo — es ist Fremdsoftware und liegt
auf dem Streaming-Rechner unter `C:\TCUnihockey`.

## Installation auf dem Streaming-Rechner

Voraussetzung ist die **.NET 8 Desktop Runtime** (das ZIP ist framework-abhängig
gepackt):

```powershell
winget install Microsoft.DotNet.DesktopRuntime.8
```

Erstinstallation — ZIP von der
[Release-Seite](https://github.com/haugjan/RedAnts/releases) (Tag
`tcconsole-v*`) laden und nach `C:\TCUnihockey\TcuConsole` entpacken. Oder in
einem Rutsch:

```powershell
$zip = "$env:TEMP\tcconsole.zip"
$url = (Invoke-RestMethod https://api.github.com/repos/haugjan/RedAnts/releases `
        -Headers @{'User-Agent'='setup'} |
        Where-Object { $_.tag_name -like 'tcconsole-v*' } |
        Select-Object -First 1).assets[0].browser_download_url
Invoke-WebRequest $url -OutFile $zip -UseBasicParsing
Expand-Archive $zip -DestinationPath C:\TCUnihockey\TcuConsole -Force
```

**Nicht** direkt nach `C:\TCUnihockey` entpacken: dort liegen gleichnamige DLLs
von TCunihockey (`System.Security.AccessControl.dll`,
`Microsoft.Win32.Registry.dll`), die sonst überschrieben würden.

Jedes weitere Update danach: **Doppelklick auf
`C:\TCUnihockey\TcuConsole\Update-TcuConsole.cmd`**.

Der Starter ruft das PowerShell-Skript mit `-ExecutionPolicy Bypass` auf. Windows
lässt PowerShell-Skripte standardmässig nicht zu (Richtlinie `Restricted`) und
meldet dann "running scripts is disabled on this system". Der Starter umgeht das
nur für diesen einen Aufruf, ohne die Richtlinie des Rechners zu ändern. Das
Fenster bleibt am Ende offen, damit die Meldung lesbar ist.

Aus einer Konsole geht es gleichwertig so:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\TCUnihockey\TcuConsole\Update-TcuConsole.ps1
```

Die Batch-Datei steht bewusst auf einer einzigen Zeile: cmd.exe liest eine
laufende Batch-Datei zeilenweise nach, und das Update überschreibt sie gerade.
Eine Zeile ist vollständig eingelesen, bevor PowerShell startet.

Das Skript nimmt das neueste Release mit dem Tag `tcconsole-v*`, überspringt den
Download, wenn `release.txt` diese Version bereits nennt, und verweigert die
Installation, solange TcuConsole läuft (`-Force` beendet es vorher). Es
überschreibt, statt zu spiegeln — ausgetauschte Icons bleiben also erhalten.

## Wo TCunihockey gesucht wird

Beim Start sucht TcuConsole aufwärts nach dem Ordner mit `TCunihockey.exe` bzw.
`Configurations` und meldet ihn im Log. Aus `C:\TCUnihockey\TcuConsole` findet
es damit `C:\TCUnihockey`. Liegt es woanders, hilft `--tcu <pfad>`.

## Mit oder ohne Zeitsteuerung

Beim Start fragt TcuConsole:

```
Mit Zeitsteuerung (Uhr Start/Stop, −1 s, +1 s)? [J/n]
```

- **J** oder **Enter**: wie bisher, die Uhr wird über das Deck bedient.
- **N**: die Tasten **28** (−1 s), **29** (+1 s) und **30** (Uhr Start/Stop)
  bleiben leer. Sie rücken bewusst nicht nach — die Zuordnung in Companion ist
  fest, eine verschobene Taste läge sonst unter dem falschen Finger.
- Keine Eingabe innert 15 Sekunden gilt als **Ja**, damit ein unbeaufsichtigt
  gestartetes TcuConsole nicht an der Frage hängen bleibt.

Ohne Rückfrage geht es mit einem Parameter, etwa für zwei Verknüpfungen:

```
TcuConsole.exe --mit-uhr
TcuConsole.exe --ohne-uhr
```

Die Wahl gilt bis zum nächsten Start.

## Spielstand korrigieren

Ein Druck auf einen der beiden Stände der Startseite (Tasten **20** Heim,
**21** Gast) öffnet die Seite „Stand Heim" bzw. „Stand Gast" — ganz in der
Farbe der Mannschaft, nur Zurück und PANIC behalten ihre Farben. Darauf:
Mannschaftsname, aktueller Stand und **−1**.

- **−1** löst den Eintrag „-1" im Kontextmenü des Stands in TCunihockey aus.
  Das ist der Korrekturweg, den TCunihockey selbst vorsieht, ohne
  Nebenwirkungen. Wie beim Eigentor wird dafür kurz der Mauszeiger bewegt und
  danach zurückgesetzt; TCunihockey darf nicht verdeckt sein.
- **Nur bei stehender Uhr und ausgeblendeter Anzeige.** Sonst nimmt
  TCunihockey das Menü weg. Bei laufender Uhr ist die Taste gesperrt
  („−1 (Uhr läuft)"); bei eingeblendeter Anzeige meldet TcuConsole den Grund
  im Log.
- **Kein +1** — bewusst. TCunihockey kennt „+1" nur im Tor-Ablauf, und das
  hält mit der RedAnts-Konfiguration eine laufende Uhr an, blendet die Anzeige
  aus und legt eine Tor-Einblendung in die Vorschau. Tore zählen weiter über
  „Tor".
- Einen UDP-Befehl für den Stand gibt es nicht: `scoreboard_home_score` kennt
  TCunihockey nur als `TcuExternal=` und nur im Modus „Alle Daten" der externen
  Uhr.

## Das Deck

TcuConsole führt 32 Tasten (4×8) und navigiert zwischen den Kontexten selbst.
Das Companion-Modul in `../RedAnts-TCConsole-Companion` überträgt nur die
Tastennummern. Einzelheiten stehen in `CLAUDE.md` unter „The TCConsole deck".
