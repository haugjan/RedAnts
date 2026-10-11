param(
    [string]$Cloud = "",
    [string]$Hall = "",
    [string]$Key = "",
    [switch]$NoUpdate
)

$ErrorActionPreference = "Stop"
$repo = "haugjan/RedAnts"
$installDir = Join-Path $env:LOCALAPPDATA "RedAnts\ShowAgent"
$exe = Join-Path $installDir "RedAnts.Show.Agent.exe"
$releaseFile = Join-Path $installDir "release.txt"

New-Item -ItemType Directory -Force $installDir | Out-Null

function Get-LatestRelease {
    $releases = Invoke-RestMethod "https://api.github.com/repos/$repo/releases?per_page=30" -Headers @{ "User-Agent" = "RedAnts-ShowAgent" }
    $releases | Where-Object { $_.tag_name -like "show-agent-v*" } | Select-Object -First 1
}

if (-not $NoUpdate) {
    try {
        $latest = Get-LatestRelease
        $current = if (Test-Path $releaseFile) { (Get-Content $releaseFile -Raw).Trim() } else { "" }
        $version = $latest.tag_name -replace "^show-agent-v", ""
        if ($latest -and $version -ne $current) {
            $asset = $latest.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1
            Write-Host "Lade Show-Agent $version ..."
            $zip = Join-Path $env:TEMP $asset.name
            Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers @{ "User-Agent" = "RedAnts-ShowAgent" }
            Get-Process RedAnts.Show.Agent -ErrorAction SilentlyContinue | Stop-Process -Force
            Expand-Archive $zip -DestinationPath $installDir -Force
            Remove-Item $zip
        }
    }
    catch {
        Write-Warning "Update nicht möglich ($($_.Exception.Message)), starte die vorhandene Version."
    }
}

if (-not (Test-Path $exe)) {
    Write-Error "Kein Show-Agent unter $installDir gefunden und kein Download möglich."
}

$arguments = @()
if ($Cloud) { $arguments += @("--cloud", $Cloud) }
if ($Hall) { $arguments += @("--hall", $Hall) }
if ($Key) { $arguments += @("--key", $Key) }

Write-Host "Starte $exe"
& $exe @arguments
