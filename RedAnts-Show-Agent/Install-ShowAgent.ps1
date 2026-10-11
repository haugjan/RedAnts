param(
    [Parameter(Mandatory = $true)][string]$Cloud,
    [Parameter(Mandatory = $true)][string]$Hall,
    [Parameter(Mandatory = $true)][string]$Key,
    [int]$DeckPort = 5150,
    [switch]$Autostart
)

$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:LOCALAPPDATA "RedAnts\ShowAgent"
New-Item -ItemType Directory -Force $installDir | Out-Null
New-Item -ItemType Directory -Force (Join-Path $installDir "data") | Out-Null

$config = @{ cloudUrl = $Cloud.TrimEnd("/"); hallId = $Hall.ToLowerInvariant(); key = $Key; deckPort = $DeckPort }
$config | ConvertTo-Json | Set-Content -Path (Join-Path $installDir "data\agent.json") -Encoding UTF8
Write-Host "Konfiguration geschrieben: $installDir\data\agent.json (Halle $($config.hallId), Cloud $($config.cloudUrl))"

$starter = Join-Path $installDir "Start-ShowAgent.ps1"
if (-not (Test-Path $starter)) {
    Copy-Item (Join-Path $PSScriptRoot "Start-ShowAgent.ps1") $starter -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $PSScriptRoot "Start-ShowAgent.cmd") (Join-Path $installDir "Start-ShowAgent.cmd") -ErrorAction SilentlyContinue
}

$shell = New-Object -ComObject WScript.Shell
function New-Shortcut([string]$Path) {
    $shortcut = $shell.CreateShortcut($Path)
    $shortcut.TargetPath = "powershell.exe"
    $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$starter`""
    $shortcut.WorkingDirectory = $installDir
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
