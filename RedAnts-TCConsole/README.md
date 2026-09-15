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

Jedes weitere Update danach:

```powershell
C:\TCUnihockey\TcuConsole\Update-TcuConsole.ps1
```

Das Skript nimmt das neueste Release mit dem Tag `tcconsole-v*`, überspringt den
Download, wenn `release.txt` diese Version bereits nennt, und verweigert die
Installation, solange TcuConsole läuft (`-Force` beendet es vorher). Es
überschreibt, statt zu spiegeln — ausgetauschte Icons bleiben also erhalten.

## Wo TCunihockey gesucht wird

Beim Start sucht TcuConsole aufwärts nach dem Ordner mit `TCunihockey.exe` bzw.
`Configurations` und meldet ihn im Log. Aus `C:\TCUnihockey\TcuConsole` findet
es damit `C:\TCUnihockey`. Liegt es woanders, hilft `--tcu <pfad>`.

## Das Deck

TcuConsole führt 32 Tasten (4×8) und navigiert zwischen den Kontexten selbst.
Das Companion-Modul in `../RedAnts-TCConsole-Companion` überträgt nur die
Tastennummern. Einzelheiten stehen in `CLAUDE.md` unter „The TCConsole deck".
