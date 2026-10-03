[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'RedAnts-GameClock'),
    [string]$Repository = 'haugjan/RedAnts',
    [int]$Port = 5080,
    [switch]$Offline
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$tagPrefix = 'gameclock-v'
$assetPattern = 'gameclock-*-win-x64.zip'
$exe = Join-Path $Destination 'RedAnts.GameClock.exe'
$installedFile = Join-Path $Destination 'release.txt'

function Install-Latest {
    Write-Host "Suche neuestes Matchuhr-Release in $Repository ..."
    $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases?per_page=100" -Headers @{ 'User-Agent' = 'Start-GameClock' }
    $release = $releases |
        Where-Object { -not $_.draft -and $_.tag_name.StartsWith($tagPrefix) } |
        Sort-Object { [datetime]$_.published_at } -Descending |
        Select-Object -First 1
    if (-not $release) { throw "Kein Release mit dem Tag-Praefix '$tagPrefix' gefunden." }

    $latest = $release.tag_name.Substring($tagPrefix.Length)
    $installed = if (Test-Path $installedFile) { (Get-Content $installedFile -Raw).Trim() } else { $null }
    if ($installed -eq $latest -and (Test-Path $exe)) {
        Write-Host "Matchuhr $installed ist aktuell."
        return
    }

    $asset = $release.assets | Where-Object { $_.name -like $assetPattern } | Select-Object -First 1
    if (-not $asset) { throw "Das Release $($release.tag_name) enthaelt kein Paket '$assetPattern'." }

    Get-Process -Name 'RedAnts.GameClock' -ErrorAction SilentlyContinue | Stop-Process -Force

    $staging = Join-Path ([IO.Path]::GetTempPath()) ('gameclock-' + [guid]::NewGuid().ToString('n'))
    New-Item -ItemType Directory -Path $staging | Out-Null
    try {
        $zip = Join-Path $staging $asset.name
        Write-Host "Lade $($asset.name) ..."
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing
        Unblock-File -Path $zip
        if (-not (Test-Path $Destination)) { New-Item -ItemType Directory -Path $Destination | Out-Null }
        Expand-Archive -Path $zip -DestinationPath $Destination -Force
        Write-Host "Matchuhr $latest installiert nach $Destination."
    }
    finally {
        Remove-Item -Path $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Grant-Firewall {
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    $rule = 'RedAnts Matchuhr'
    if (Get-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue) { return }
    if ($isAdmin) {
        New-NetFirewallRule -DisplayName $rule -Direction Inbound -Program $exe -Action Allow -Profile Any | Out-Null
        Write-Host "Firewall-Regel '$rule' angelegt."
    }
    else {
        Write-Host "Hinweis: Falls Windows nach der Firewall fragt, 'Zugriff zulassen' waehlen." -ForegroundColor Yellow
    }
}

if ($Offline) {
    if (-not (Test-Path $exe)) { throw "Keine installierte Matchuhr in $Destination gefunden." }
}
else {
    try { Install-Latest }
    catch {
        if (Test-Path $exe) { Write-Warning "Update nicht moeglich ($($_.Exception.Message)). Starte installierte Version." }
        else { throw }
    }
}

Grant-Firewall

$url = "http://localhost:$Port/"
Write-Host ''
Write-Host "  Matchuhr: $url" -ForegroundColor Green
Write-Host ''
Start-Process $url
& $exe --urls "http://0.0.0.0:$Port"
