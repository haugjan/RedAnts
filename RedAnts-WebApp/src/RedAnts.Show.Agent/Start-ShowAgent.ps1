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
    [switch]$NoBrowser,
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }
try { $Host.UI.RawUI.WindowTitle = "Red Ants Board" } catch { }

$repo = "haugjan/RedAnts"
$appName = "Red Ants Board"
$defaultCloud = "https://show.redants.ch"
$switchHallCode = 3
$root = if ($InstallDir) { $InstallDir } else { Join-Path $env:LOCALAPPDATA "RedAnts\ShowAgent" }
$dataDir = Join-Path $root "data"
$exe = Join-Path $root "RedAnts.Show.Agent.exe"
$icon = Join-Path $root "board.ico"
$releaseFile = Join-Path $root "release.txt"
$configFile = Join-Path $dataDir "agent.json"
$hallsFile = Join-Path $dataDir "hallen.json"
$selfFile = Join-Path $root "Start-ShowAgent.ps1"

function Write-Banner {
    Write-Host ""
    Write-Host "  ╔════════════════════════════════════════════╗" -ForegroundColor DarkRed
    Write-Host "  ║           RED ANTS  ·  BOARD               ║" -ForegroundColor Red
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

function Resolve-Cloud([string]$url) {
    $url = $url.Trim().TrimEnd("/")
    if (-not $url) { return $defaultCloud }
    if ($url -notmatch "^https?://") { $url = "https://$url" }
    $fixed = $url -replace "^(https?://)admin(-dev)?\.redants\.ch$", '$1show$2.redants.ch'
    if ($fixed -ne $url) { Write-Warn "$url ist das Backoffice, das Board verbindet sich mit $fixed." }
    return $fixed
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

function Update-Config {
    $config = Read-Json $configFile
    if (-not $config) { $config = [pscustomobject]@{} }
    $names = @($config.PSObject.Properties.Name)
    $values = @{}

    $storedCloud = if ($names -contains "cloudUrl") { [string]$config.cloudUrl } else { "" }
    $values.cloudUrl = Resolve-Cloud $(if ($Cloud) { $Cloud } else { $storedCloud })

    if (-not ($names -contains "cloudPassword") -or -not $config.cloudPassword) {
        $halls = Read-Json $hallsFile
        if ($halls -and ($halls.PSObject.Properties.Name -contains "bordPasswort") -and $halls.bordPasswort) {
            $values.cloudPassword = [string]$halls.bordPasswort
        }
    }

    if ($Hall -and $Key) {
        $values.hallId = $Hall.ToLowerInvariant()
        $values.key = $Key
    }
    if ($DeckPort -gt 0) { $values.deckPort = $DeckPort }
    elseif (-not ($names -contains "deckPort")) { $values.deckPort = 5150 }

    foreach ($name in $values.Keys) {
        $config | Add-Member -NotePropertyName $name -NotePropertyValue $values[$name] -Force
    }
    Write-Json $config $configFile
    return $config
}

function Get-LatestRelease {
    $headers = @{ "User-Agent" = "RedAnts-Board" }
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
        Stop-Here "Das Board ist nicht installiert und kein Download möglich. Internetverbindung prüfen."
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
        Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers @{ "User-Agent" = "RedAnts-Board" } -TimeoutSec 600
        if ((Get-Item -LiteralPath $zip).Length -lt 1MB) { throw "Die heruntergeladene Datei ist unvollständig." }

        Get-Process RedAnts.Show.Agent -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Warn "Ein laufendes Board wird beendet."
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

function New-Shortcut([string]$path, [switch]$Quiet) {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($path)
    $shortcut.TargetPath = "powershell.exe"
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$selfFile`""
    if ($Quiet) { $arguments += " -Silent" }
    $shortcut.Arguments = $arguments
    $shortcut.WorkingDirectory = $root
    $shortcut.Description = "$($appName): lädt die neuste Version, fragt nach der Halle und startet"
    if (Test-Path -LiteralPath $icon) { $shortcut.IconLocation = "$icon,0" }
    elseif (Test-Path -LiteralPath $exe) { $shortcut.IconLocation = "$exe,0" }
    $shortcut.Save()
}

function Remove-OldShortcuts([string]$folder) {
    foreach ($pattern in @("Show-Agent*.lnk", "Show-Bord.url")) {
        Get-ChildItem -LiteralPath $folder -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
            try { Remove-Item -LiteralPath $_.FullName -Force; Write-Note "Alte Verknüpfung '$($_.Name)' entfernt." } catch { }
        }
    }
}

function Set-Shortcuts {
    if ($NoShortcut) { return }
    $desktop = [Environment]::GetFolderPath("Desktop")
    Remove-OldShortcuts $desktop
    $path = Join-Path $desktop "$appName.lnk"
    $fresh = -not (Test-Path -LiteralPath $path)
    try {
        New-Shortcut $path
        if ($fresh) { Write-Ok "Verknüpfung '$appName' liegt auf dem Desktop." }
    }
    catch { Write-Warn "Verknüpfung konnte nicht angelegt werden: $($_.Exception.Message)" }

    $startup = [Environment]::GetFolderPath("Startup")
    $hadAutostart = Test-Path -LiteralPath (Join-Path $startup "Show-Agent.lnk")
    Remove-OldShortcuts $startup
    if ($Autostart -or $hadAutostart) {
        try {
            New-Shortcut (Join-Path $startup "$appName.lnk") -Quiet
            Write-Ok "Das Board startet mit Windows (letzte Halle, ohne Hallenwahl)."
        }
        catch { Write-Warn "Autostart konnte nicht angelegt werden: $($_.Exception.Message)" }
    }
}

function Open-BoardWhenReady([string]$url) {
    if ($NoBrowser) { return $null }
    $uri = [uri]$url
    return Start-Job -ArgumentList $uri.Host, $uri.Port, $url -ScriptBlock {
        param($hostName, $port, $url)
        for ($i = 0; $i -lt 120; $i++) {
            $client = New-Object Net.Sockets.TcpClient
            try { $client.Connect($hostName, $port); Start-Process $url; return }
            catch { Start-Sleep -Milliseconds 500 }
            finally { $client.Dispose() }
        }
    }
}

try {
    Write-Banner
    Install-Self
    Update-Agent
    $config = Update-Config
    Set-Shortcuts

    $port = if ($config.deckPort) { $config.deckPort } else { 5150 }
    $boardUrl = "http://localhost:$port/bord"
    $choose = -not $Silent -and -not ($Hall -and $Key)
    Write-Host ""
    if ($choose) { Write-Ok "Die Halle wählst du gleich im Browser: $boardUrl" }
    else { Write-Ok "Halle $($config.hallId) über $($config.cloudUrl)" }
    Write-Note "Bord, Uhr und Status: $boardUrl · /uhr · /status"
    Write-Host ""

    if ($CheckOnly) { Write-Ok "Prüflauf beendet, das Board wurde nicht gestartet."; exit 0 }
    if (-not (Test-Path -LiteralPath $exe)) { Stop-Here "Das Board fehlt unter $root." }

    Write-Step "Board läuft. Dieses Fenster offen lassen, mit Strg+C beenden."
    Write-Host ""
    $browser = Open-BoardWhenReady $boardUrl
    Push-Location -LiteralPath $root
    try {
        do {
            $agentArgs = @()
            if ($choose) { $agentArgs += "--choose-hall" }
            & $exe @agentArgs
            $code = $LASTEXITCODE
            if ($code -eq $switchHallCode) {
                $choose = $true
                Write-Host ""
                Write-Step "Halle wechseln: das Board startet neu."
            }
        } while ($code -eq $switchHallCode)
    }
    finally { Pop-Location }
    if ($browser) { Remove-Job $browser -Force -ErrorAction SilentlyContinue }
    Write-Host ""
    if ($code -ne 0) { Stop-Here "Das Board hat sich mit Fehlercode $code beendet." }
    Write-Ok "Das Board wurde beendet."
    if (-not $Silent) { Read-Host "  Enter schliesst das Fenster" }
}
catch {
    Write-Host ""
    Stop-Here "Unerwarteter Fehler: $($_.Exception.Message)"
}
