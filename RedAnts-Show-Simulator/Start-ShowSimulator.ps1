param(
    [int]$Port = 5090,
    [switch]$NoUpdate,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$repo = "haugjan/RedAnts"
$home = Join-Path $env:LOCALAPPDATA "RedAnts\ShowSimulator"
$exe = Join-Path $home "RedAnts.Show.Simulator.exe"
$releaseFile = Join-Path $home "release.txt"
New-Item -ItemType Directory -Force $home | Out-Null

if (-not $NoUpdate) {
    try {
        $releases = Invoke-RestMethod "https://api.github.com/repos/$repo/releases?per_page=30" -Headers @{ "User-Agent" = "RedAnts-ShowSimulator" }
        $latest = $releases | Where-Object { $_.tag_name -like "show-simulator-v*" } | Select-Object -First 1
        $current = if (Test-Path $releaseFile) { (Get-Content $releaseFile -Raw).Trim() } else { "" }
        $version = $latest.tag_name -replace "^show-simulator-v", ""
        if ($latest -and $version -ne $current) {
            $asset = $latest.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1
            Write-Host "Lade Show-Simulator $version ..."
            $zip = Join-Path $env:TEMP $asset.name
            Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers @{ "User-Agent" = "RedAnts-ShowSimulator" }
            Get-Process RedAnts.Show.Simulator -ErrorAction SilentlyContinue | Stop-Process -Force
            Expand-Archive $zip -DestinationPath $home -Force
            Remove-Item $zip
        }
    }
    catch {
        Write-Warning "Update nicht möglich ($($_.Exception.Message)), starte die vorhandene Version."
    }
}

if (-not (Test-Path $exe)) {
    Write-Error "Kein Show-Simulator unter $home gefunden und kein Download möglich."
}

$url = "http://localhost:$Port"
Write-Host "Starte $exe auf $url"
if (-not $NoBrowser) { Start-Job -ScriptBlock { Start-Sleep -Seconds 3; Start-Process $using:url } | Out-Null }
& $exe --urls $url
