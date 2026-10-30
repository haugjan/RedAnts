[CmdletBinding()]
param(
    [string]$Hall = "",
    [string]$Key = "",
    [string]$Cloud = "",
    [string]$InstallDir = "",
    [int]$DeckPort = 0,
    [switch]$NoUpdate,
    [switch]$Silent,
    [switch]$NoShortcut,
    [switch]$Autostart,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }

$repo = "haugjan/RedAnts"
$defaultCloud = "https://show.redants.ch"
$root = if ($InstallDir) { $InstallDir } else { Join-Path $env:LOCALAPPDATA "RedAnts\ShowAgent" }
$dataDir = Join-Path $root "data"
$exe = Join-Path $root "RedAnts.Show.Agent.exe"
$releaseFile = Join-Path $root "release.txt"
$configFile = Join-Path $dataDir "agent.json"
$hallsFile = Join-Path $dataDir "hallen.json"
$selfFile = Join-Path $root "Start-ShowAgent.ps1"

function Write-Banner {
    Write-Host ""
    Write-Host "  ╔════════════════════════════════════════════╗" -ForegroundColor DarkRed
    Write-Host "  ║           RED ANTS  ·  SHOW-AGENT          ║" -ForegroundColor Red
    Write-Host "  ╚════════════════════════════════════════════╝" -ForegroundColor DarkRed
    Write-Host ""
}

function Write-Step([string]$text) { Write-Host "  → $text" -ForegroundColor Cyan }
function Write-Ok([string]$text) { Write-Host "  ✓ $text" -ForegroundColor Green }
function Write-Note([string]$text) { Write-Host "    $text" -ForegroundColor DarkGray }
function Write-Warn([string]$text) { Write-Host "  ! $text" -ForegroundColor Yellow }
function Write-Fail([string]$text) { Write-Host "  ✗ $text" -ForegroundColor Red }

function Stop-Here([string]$text) {
    Write-Fail $text
    if (-not $Silent) {
        Write-Host ""
        Read-Host "  Enter schliesst das Fenster"
    }
    exit 1
}

function Read-Json([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try { return Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch {
        Write-Warn "$path ist beschädigt und wird neu angelegt."
        Copy-Item -LiteralPath $path -Destination "$path.kaputt" -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Write-Json($value, [string]$path) {
    $folder = Split-Path $path -Parent
    if (-not (Test-Path -LiteralPath $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
    $value | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $path -Encoding UTF8
}

function Get-Halls {
    $stored = Read-Json $hallsFile
    $halls = @()
    $last = ""
    $script:boardPassword = ""
    if ($stored) {
        if ($stored.PSObject.Properties.Name -contains "hallen") { $halls = @($stored.hallen) } else { $halls = @($stored) }
        if ($stored.PSObject.Properties.Name -contains "zuletzt") { $last = [string]$stored.zuletzt }
        if ($stored.PSObject.Properties.Name -contains "bordPasswort") { $script:boardPassword = [string]$stored.bordPasswort }
    }
    $halls = @($halls | Where-Object { $_ -and $_.id })
    if ($halls.Count -eq 0 -and (Test-Path -LiteralPath $configFile)) {
        $config = Read-Json $configFile
        if ($config -and $config.hallId) {
            $halls = @([pscustomobject]@{ name = [string]$config.hallId; id = [string]$config.hallId; key = [string]$config.key; cloud = [string]$config.cloudUrl })
            $last = [string]$config.hallId
        }
    }
    return [pscustomobject]@{ Halls = $halls; Last = $last }
}

function Save-Halls($halls, [string]$last) {
    Write-Json ([pscustomobject]@{ zuletzt = $last; bordPasswort = $script:boardPassword; hallen = @($halls) }) $hallsFile
}

function Get-CloudHalls([string]$cloud, [string]$password) {
    try {
        $url = "$($cloud.TrimEnd('/'))/bord/hallen?key=$([uri]::EscapeDataString($password))"
        $answer = Invoke-RestMethod $url -Headers @{ "User-Agent" = "RedAnts-ShowAgent" } -TimeoutSec 30
        return @($answer | Where-Object { $_.id })
    }
    catch {
        Write-Warn "Die Hallenliste konnte nicht geladen werden ($($_.Exception.Message))."
        return @()
    }
}

function New-Hall {
    Write-Host ""
    Write-Host "  Neue Halle erfassen" -ForegroundColor White
    $cloud = (Read-Host "  Cloud [$defaultCloud]").Trim()
    if (-not $cloud) { $cloud = $defaultCloud }
    $cloud = $cloud.TrimEnd("/")

    if (-not $script:boardPassword) {
        $script:boardPassword = (Read-Host "  Bord-Passwort (dasselbe wie für $cloud/bord)").Trim()
    }

    $id = ""
    $name = ""
    $offered = @()
    if ($script:boardPassword) {
        Write-Step "Hole die Hallen von $cloud ..."
        $offered = Get-CloudHalls $cloud $script:boardPassword
    }

    if ($offered.Count -gt 0) {
        Write-Host ""
        for ($i = 0; $i -lt $offered.Count; $i++) {
            $hint = if ($offered[$i].agent) { "  (dort läuft schon ein Agent)" } else { "" }
            Write-Host ("   [{0}] {1,-24} {2}{3}" -f ($i + 1), $offered[$i].name, $offered[$i].id, $hint) -ForegroundColor Gray
        }
        Write-Host "   [x] Kennung von Hand eingeben" -ForegroundColor DarkGray
        $answer = (Read-Host "  Welche Halle").Trim()
        $number = 0
        if ([int]::TryParse($answer, [ref]$number) -and $number -ge 1 -and $number -le $offered.Count) {
            $id = [string]$offered[$number - 1].id
            $name = [string]$offered[$number - 1].name
        }
    }

    if (-not $id) {
        $id = (Read-Host "  Kennung der Halle").Trim()
        if (-not $id) { return $null }
    }

    Write-Note "Der Agent-Schlüssel steht im Backoffice unter Show > Hallen bei der Halle."
    $key = (Read-Host "  Agent-Schlüssel für $id").Trim()
    if (-not $key) { return $null }
    if (-not $name) { $name = $id }
    $own = (Read-Host "  Anzeigename [$name]").Trim()
    if ($own) { $name = $own }

    return [pscustomobject]@{ name = $name; id = $id.ToLowerInvariant(); key = $key; cloud = $cloud }
}

function Select-Hall {
    $known = Get-Halls
    $halls = @($known.Halls)
    $last = $known.Last

    if ($Hall) {
        $match = $halls | Where-Object { $_.id -eq $Hall.ToLowerInvariant() } | Select-Object -First 1
        if ($match) {
            if ($Key) { $match.key = $Key }
            if ($Cloud) { $match.cloud = $Cloud.TrimEnd("/") }
            return $match
        }
        if (-not $Key) { Stop-Here "Die Halle '$Hall' ist hier nicht erfasst. Einmal mit -Hall <kennung> -Key <schlüssel> starten." }
        $fresh = [pscustomobject]@{
            name  = $Hall
            id    = $Hall.ToLowerInvariant()
            key   = $Key
            cloud = $(if ($Cloud) { $Cloud.TrimEnd("/") } else { $defaultCloud })
        }
        $halls += $fresh
        Save-Halls $halls $fresh.id
        return $fresh
    }

    if ($halls.Count -eq 0) {
        if ($Silent) { Stop-Here "Keine Halle erfasst. Das Skript einmal ohne -Silent starten." }
        Write-Step "Noch keine Halle erfasst."
        $fresh = New-Hall
        if (-not $fresh) { Stop-Here "Ohne Halle kann der Agent nicht starten." }
        Save-Halls @($fresh) $fresh.id
        return $fresh
    }

    $standard = $halls | Where-Object { $_.id -eq $last } | Select-Object -First 1
    if (-not $standard) { $standard = $halls[0] }
    if ($Silent -or $halls.Count -eq 1) { return $standard }

    while ($true) {
        Write-Host ""
        Write-Host "  In welcher Halle bist du?" -ForegroundColor White
        for ($i = 0; $i -lt $halls.Count; $i++) {
            $hall = $halls[$i]
            $mark = if ($hall.id -eq $standard.id) { "  ← zuletzt" } else { "" }
            Write-Host ("   [{0}] {1,-24} {2}{3}" -f ($i + 1), $hall.name, $hall.id, $mark) -ForegroundColor Gray
        }
        Write-Host "   [n] neue Halle erfassen" -ForegroundColor DarkGray
        Write-Host "   [q] abbrechen" -ForegroundColor DarkGray
        $answer = (Read-Host ("  Auswahl [" + ($halls.IndexOf($standard) + 1) + "]")).Trim()

        if (-not $answer) { return $standard }
        if ($answer -eq "q") { exit 0 }
        if ($answer -eq "n") {
            $fresh = New-Hall
            if ($fresh) {
                $halls = @($halls | Where-Object { $_.id -ne $fresh.id }) + $fresh
                Save-Halls $halls $fresh.id
                return $fresh
            }
            continue
        }
        $number = 0
        if ([int]::TryParse($answer, [ref]$number) -and $number -ge 1 -and $number -le $halls.Count) {
            return $halls[$number - 1]
        }
        Write-Warn "Bitte eine Zahl von 1 bis $($halls.Count), n oder q."
    }
}

function Save-Choice($hall) {
    $halls = @(Get-Halls | Select-Object -ExpandProperty Halls)
    $halls = @($halls | Where-Object { $_.id -ne $hall.id }) + $hall
    Save-Halls $halls $hall.id

    $config = Read-Json $configFile
    if (-not $config) { $config = [pscustomobject]@{} }
    $values = @{
        cloudUrl = $(if ($hall.cloud) { $hall.cloud } else { $defaultCloud })
        hallId   = $hall.id
        key      = $hall.key
    }
    if ($DeckPort -gt 0) { $values.deckPort = $DeckPort }
    elseif (-not ($config.PSObject.Properties.Name -contains "deckPort")) { $values.deckPort = 5150 }
    foreach ($name in $values.Keys) {
        $config | Add-Member -NotePropertyName $name -NotePropertyValue $values[$name] -Force
    }
    Write-Json $config $configFile
}

function Get-LatestRelease {
    $headers = @{ "User-Agent" = "RedAnts-ShowAgent" }
    for ($try = 1; $try -le 3; $try++) {
        try {
            $releases = Invoke-RestMethod "https://api.github.com/repos/$repo/releases?per_page=30" -Headers $headers -TimeoutSec 30
            return $releases | Where-Object { $_.tag_name -like "show-agent-v*" -and -not $_.draft } | Select-Object -First 1
        }
        catch {
            if ($try -eq 3) { throw }
            Start-Sleep -Seconds (2 * $try)
        }
    }
}

function Update-Agent {
    if ($NoUpdate) { Write-Note "Update übersprungen (-NoUpdate)."; return }
    Write-Step "Suche die neuste Version ..."
    $latest = $null
    try { $latest = Get-LatestRelease }
    catch { Write-Warn "GitHub ist nicht erreichbar ($($_.Exception.Message))." }
    if (-not $latest) {
        if (Test-Path -LiteralPath $exe) { Write-Warn "Es läuft die installierte Version weiter."; return }
        Stop-Here "Kein Agent installiert und kein Download möglich. Internetverbindung prüfen."
    }

    $version = $latest.tag_name -replace "^show-agent-v", ""
    $current = if (Test-Path -LiteralPath $releaseFile) { (Get-Content -LiteralPath $releaseFile -Raw).Trim() } else { "" }
    if ($version -eq $current -and (Test-Path -LiteralPath $exe)) { Write-Ok "Version $version ist aktuell."; return }

    $asset = $latest.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1
    if (-not $asset) {
        if (Test-Path -LiteralPath $exe) { Write-Warn "Im Release fehlt das Windows-Paket, es läuft die installierte Version."; return }
        Stop-Here "Im Release $version fehlt das Windows-Paket."
    }

    $zip = Join-Path $env:TEMP $asset.name
    Write-Step "Lade Version $version ($([math]::Round($asset.size / 1MB, 1)) MB) ..."
    try {
        Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers @{ "User-Agent" = "RedAnts-ShowAgent" } -TimeoutSec 600
        if ((Get-Item -LiteralPath $zip).Length -lt 1MB) { throw "Die heruntergeladene Datei ist unvollständig." }

        Get-Process RedAnts.Show.Agent -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Warn "Ein laufender Agent wird beendet."
            $_ | Stop-Process -Force
            Start-Sleep -Milliseconds 800
        }
        if (-not (Test-Path -LiteralPath $root)) { New-Item -ItemType Directory -Force -Path $root | Out-Null }
        Expand-Archive -LiteralPath $zip -DestinationPath $root -Force
        Set-Content -LiteralPath $releaseFile -Value $version -Encoding UTF8
        Write-Ok "Version $version installiert."
    }
    catch {
        if (Test-Path -LiteralPath $exe) { Write-Warn "Update fehlgeschlagen ($($_.Exception.Message)), es läuft Version $current weiter." }
        else { Stop-Here "Download fehlgeschlagen: $($_.Exception.Message)" }
    }
    finally { try { Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue } catch { } }
}

function Install-Self {
    foreach ($folder in @($root, $dataDir)) {
        if (-not (Test-Path -LiteralPath $folder)) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
    }
    $source = $PSCommandPath
    if ($source -and $source -ne $selfFile) {
        try { Copy-Item -LiteralPath $source -Destination $selfFile -Force } catch { Write-Warn "Das Skript konnte nicht nach $root kopiert werden: $($_.Exception.Message)" }
    }
    try { Unblock-File -LiteralPath $selfFile -ErrorAction SilentlyContinue } catch { }
}

function New-Shortcut([string]$path, [string]$Hall = "", [switch]$Quiet) {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($path)
    $shortcut.TargetPath = "powershell.exe"
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$selfFile`""
    if ($Hall) { $arguments += " -Hall $Hall" }
    if ($Quiet) { $arguments += " -Silent" }
    $shortcut.Arguments = $arguments
    $shortcut.WorkingDirectory = $root
    $shortcut.Description = "RedAnts Show-Agent: lädt die neuste Version und startet sie"
    if (Test-Path -LiteralPath $exe) { $shortcut.IconLocation = $exe }
    $shortcut.Save()
}

function Set-Shortcuts {
    if ($NoShortcut) { return }
    $desktop = [Environment]::GetFolderPath("Desktop")
    $menu = Join-Path $desktop "Show-Agent.lnk"
    $fresh = -not (Test-Path -LiteralPath $menu)
    try {
        New-Shortcut $menu
        if ($fresh) { Write-Ok "Verknüpfung 'Show-Agent' liegt auf dem Desktop." }
    }
    catch { Write-Warn "Verknüpfung konnte nicht angelegt werden: $($_.Exception.Message)" }

    foreach ($hall in @(Get-Halls | Select-Object -ExpandProperty Halls)) {
        $label = "Show-Agent " + ($hall.name -replace '[\\/:*?"<>|]', " ")
        $path = Join-Path $desktop ($label.Trim() + ".lnk")
        $new = -not (Test-Path -LiteralPath $path)
        try {
            New-Shortcut $path -Hall $hall.id
            if ($new) { Write-Ok "Verknüpfung '$label' liegt auf dem Desktop." }
        }
        catch { Write-Warn "Verknüpfung für $($hall.name) konnte nicht angelegt werden: $($_.Exception.Message)" }
    }
    if ($Autostart) {
        try {
            New-Shortcut (Join-Path ([Environment]::GetFolderPath("Startup")) "Show-Agent.lnk") -Quiet
            Write-Ok "Der Agent startet ab jetzt mit Windows."
        }
        catch { Write-Warn "Autostart konnte nicht angelegt werden: $($_.Exception.Message)" }
    }
}

try {
    Write-Banner
    Install-Self
    Update-Agent

    $chosen = Select-Hall
    Save-Choice $chosen
    Set-Shortcuts

    $config = Read-Json $configFile
    $port = if ($config -and $config.deckPort) { $config.deckPort } else { 5150 }
    Write-Host ""
    Write-Ok "Halle $($chosen.name) ($($chosen.id)) über $($chosen.cloud)"
    Write-Note "Bord, Uhr und Status: http://localhost:$port/bord · /uhr · /status"
    Write-Note "Halle wechseln: dieses Fenster schliessen und die Verknüpfung neu starten."
    Write-Host ""

    if ($CheckOnly) { Write-Ok "Prüflauf beendet, der Agent wurde nicht gestartet."; exit 0 }
    if (-not (Test-Path -LiteralPath $exe)) { Stop-Here "Der Agent fehlt unter $root." }

    Write-Step "Agent läuft. Mit Strg+C beenden."
    Write-Host ""
    & $exe
    $code = $LASTEXITCODE
    Write-Host ""
    if ($code -ne 0) { Stop-Here "Der Agent hat sich mit Fehlercode $code beendet." }
    Write-Ok "Der Agent wurde beendet."
    if (-not $Silent) { Read-Host "  Enter schliesst das Fenster" }
}
catch {
    Write-Host ""
    Stop-Here "Unerwarteter Fehler: $($_.Exception.Message)"
}
