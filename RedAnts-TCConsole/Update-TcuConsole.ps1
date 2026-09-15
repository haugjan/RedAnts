[CmdletBinding()]
param(
    [string]$Destination,
    [string]$Repository = 'haugjan/RedAnts',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$tagPrefix = 'tcconsole-v'
$scriptName = Split-Path -Leaf $PSCommandPath

if (-not $Destination) {
    if (Test-Path (Join-Path $PSScriptRoot 'TcuConsole.csproj')) {
        throw "Dieses Skript liegt im Quellordner des Repos. Bitte ein Zielverzeichnis angeben, z.B. .\$scriptName -Destination C:\TCUnihockey\TcuConsole"
    }
    $Destination = $PSScriptRoot
}

Write-Host "Suche neuestes TCConsole-Release in $Repository ..."
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases?per_page=100" -Headers @{ 'User-Agent' = $scriptName } |
    Where-Object { -not $_.draft -and $_.tag_name.StartsWith($tagPrefix) } |
    Sort-Object { [datetime]$_.published_at } -Descending |
    Select-Object -First 1

if (-not $release) {
    throw "Kein Release mit dem Tag-Praefix '$tagPrefix' gefunden."
}

$latest = $release.tag_name.Substring($tagPrefix.Length)
$installedFile = Join-Path $Destination 'release.txt'
$installed = if (Test-Path $installedFile) { (Get-Content $installedFile -Raw).Trim() } else { $null }

if ($installed -eq $latest -and -not $Force) {
    Write-Host "TcuConsole $installed ist bereits aktuell."
    return
}

$asset = $release.assets | Where-Object { $_.name -like '*.zip' } | Select-Object -First 1
if (-not $asset) {
    throw "Das Release $($release.tag_name) enthaelt kein ZIP."
}

$running = Get-Process -Name 'TcuConsole' -ErrorAction SilentlyContinue
if ($running) {
    if (-not $Force) {
        throw "TcuConsole laeuft gerade. Bitte beenden, oder das Skript mit -Force starten."
    }
    Write-Host "Beende laufende TcuConsole ..."
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue
}

$staging = Join-Path ([IO.Path]::GetTempPath()) ('tcconsole-' + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    $zip = Join-Path $staging $asset.name
    Write-Host "Lade $($asset.name) ..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing
    Unblock-File -Path $zip

    $unpacked = Join-Path $staging 'unpacked'
    Expand-Archive -Path $zip -DestinationPath $unpacked -Force

    $pendingScript = $null
    $shippedScript = Join-Path $unpacked $scriptName
    if (Test-Path $shippedScript) {
        $pendingScript = Join-Path $staging $scriptName
        Move-Item -Path $shippedScript -Destination $pendingScript
    }

    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination | Out-Null
    }
    Copy-Item -Path (Join-Path $unpacked '*') -Destination $Destination -Recurse -Force

    if ($pendingScript) {
        try {
            Copy-Item -Path $pendingScript -Destination (Join-Path $Destination $scriptName) -Force
        }
        catch {
            Write-Warning "Das Update-Skript selbst konnte nicht ersetzt werden: $($_.Exception.Message)"
        }
    }

    Write-Host "TcuConsole $latest installiert nach $Destination."
    if ($installed) {
        Write-Host "Vorher installiert: $installed"
    }
}
finally {
    Remove-Item -Path $staging -Recurse -Force -ErrorAction SilentlyContinue
}
