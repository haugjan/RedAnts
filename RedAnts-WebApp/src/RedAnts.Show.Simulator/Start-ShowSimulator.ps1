param(
    [int]$Port = 5090,
    [switch]$NoUpdate,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$repo = "haugjan/RedAnts"
$installDir = Join-Path $env:LOCALAPPDATA "RedAnts\ShowSimulator"
$exe = Join-Path $installDir "RedAnts.Show.Simulator.exe"
$releaseFile = Join-Path $installDir "release.txt"
New-Item -ItemType Directory -Force $installDir | Out-Null

function Test-Lesbar([string]$pfad) {
    try {
        $strom = [System.IO.File]::Open($pfad, "Open", "Read", "Read")
        $strom.Dispose()
        return $true
    }
    catch { return $false }
}

function Wait-Entpackt([string]$ordner) {
    $wwwroot = Join-Path $ordner "wwwroot"
    if (-not (Test-Path $wwwroot)) { return }
    for ($versuch = 1; $versuch -le 40; $versuch++) {
        $offen = @(Get-ChildItem $wwwroot -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Length -eq 0 -or -not (Test-Lesbar $_.FullName) })
        if ($offen.Count -eq 0) { return }
        Start-Sleep -Milliseconds 300
    }
    Write-Warning "Die entpackten Dateien waren nicht rechtzeitig vollstaendig lesbar. Falls die Seite ohne Gestaltung erscheint, das Fenster schliessen und neu starten."
}

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
            Expand-Archive $zip -DestinationPath $installDir -Force
            Remove-Item $zip
            Wait-Entpackt $installDir
        }
    }
    catch {
        Write-Warning "Update nicht möglich ($($_.Exception.Message)), starte die vorhandene Version."
    }
}

if (-not (Test-Path $exe)) {
    Write-Error "Kein Show-Simulator unter $installDir gefunden und kein Download möglich."
}

$url = "http://localhost:$Port"
Write-Host "Starte $exe auf $url"
if (-not $NoBrowser) {
    Start-Job -ScriptBlock {
        for ($versuch = 1; $versuch -le 60; $versuch++) {
            try {
                $css = Invoke-WebRequest "$using:url/app.css" -UseBasicParsing -TimeoutSec 2
                if ($css.RawContentLength -gt 0) { break }
            }
            catch { }
            Start-Sleep -Milliseconds 500
        }
        Start-Process $using:url
    } | Out-Null
}
& $exe --urls $url
