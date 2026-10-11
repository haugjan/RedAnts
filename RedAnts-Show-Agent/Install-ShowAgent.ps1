param(
    [Parameter(Mandatory = $true)][string]$Cloud,
    [Parameter(Mandatory = $true)][string]$Hall,
    [Parameter(Mandatory = $true)][string]$Key,
    [int]$DeckPort = 5150,
    [switch]$Autostart
)

$ErrorActionPreference = "Stop"
$home = Join-Path $env:LOCALAPPDATA "RedAnts\ShowAgent"
New-Item -ItemType Directory -Force $home | Out-Null
New-Item -ItemType Directory -Force (Join-Path $home "data") | Out-Null

$config = @{ cloudUrl = $Cloud.TrimEnd("/"); hallId = $Hall.ToLowerInvariant(); key = $Key; deckPort = $DeckPort }
$config | ConvertTo-Json | Set-Content -Path (Join-Path $home "data\agent.json") -Encoding UTF8
Write-Host "Konfiguration geschrieben: $home\data\agent.json (Halle $($config.hallId), Cloud $($config.cloudUrl))"

$starter = Join-Path $home "Start-ShowAgent.ps1"
if (-not (Test-Path $starter)) {
    Copy-Item (Join-Path $PSScriptRoot "Start-ShowAgent.ps1") $starter -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $PSScriptRoot "Start-ShowAgent.cmd") (Join-Path $home "Start-ShowAgent.cmd") -ErrorAction SilentlyContinue
}

$shell = New-Object -ComObject WScript.Shell
function New-Shortcut([string]$Path) {
    $shortcut = $shell.CreateShortcut($Path)
    $shortcut.TargetPath = "powershell.exe"
    $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$starter`""
    $shortcut.WorkingDirectory = $home
    $shortcut.Description = "RedAnts Show-Agent (lädt die neuste Version und startet)"
    $shortcut.Save()
}

New-Shortcut (Join-Path ([Environment]::GetFolderPath("Desktop")) "Show-Agent.lnk")
Write-Host "Desktop-Verknüpfung angelegt."

if ($Autostart) {
    New-Shortcut (Join-Path ([Environment]::GetFolderPath("Startup")) "Show-Agent.lnk")
    Write-Host "Autostart angelegt (Ordner Autostart des Benutzers)."
}

Write-Host "Erster Start (lädt die neuste Version von GitHub) ..."
& $starter
