# Rendert alle SVG-Quellen aus .\svg nach transparenten PNGs.
#
# Jede SVG-Quelle kennt zwei Farbslots:
#   currentColor            -> Basisfarbe   (Grundmotiv)
#   var(--accent)           -> Akzentfarbe  (Klassen .af = Flaeche, .as = Strich)
#
# Farbvarianten (siehe $variants weiter unten):
#   dark        Basis #1A1A1A / Akzent #1A1A1A   einfarbig, fuer helle Button-Farben
#   light       Basis #FFFFFF / Akzent #FFFFFF   einfarbig, fuer dunkle Button-Farben
#   red         Basis #1A1A1A / Akzent #E4002B   zweifarbig, fuer helle Button-Farben
#   gold        Basis #FFFFFF / Akzent #FFD700   zweifarbig, fuer dunkle Button-Farben
#   team        Basis #E4002B / Akzent #1A1A1A   zweifarbig, fuer helle Button-Farben
#
# Benoetigt Google Chrome oder Microsoft Edge (headless Screenshot), sonst nichts.
#
#   .\render.ps1                          -> 144x144 nach png\<variante>\
#   .\render.ps1 -Size 72 -OutDir png72
#   .\render.ps1 -Only ball,coach         -> nur einzelne Icons neu rendern

param(
    [int]$Size = 144,
    [string]$OutDir = 'png',
    [string[]]$Only
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$svgDir = Join-Path $root 'svg'

$browser = @(
    'C:\Program Files\Google\Chrome\Application\chrome.exe',
    'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe',
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $browser) { throw 'Weder Chrome noch Edge gefunden.' }

# Hier weitere Farbvarianten ergaenzen: name = @{ base = '#RRGGBB'; accent = '#RRGGBB' }
$variants = [ordered]@{
    'dark'  = @{ base = '#1A1A1A'; accent = '#1A1A1A' }
    'light' = @{ base = '#FFFFFF'; accent = '#FFFFFF' }
    'red'   = @{ base = '#1A1A1A'; accent = '#E4002B' }
    'gold'  = @{ base = '#FFFFFF'; accent = '#FFD700' }
    'team'  = @{ base = '#E4002B'; accent = '#1A1A1A' }
}

$svgs = Get-ChildItem $svgDir -Filter *.svg
if ($Only) { $svgs = $svgs | Where-Object { $Only -contains $_.BaseName } }

$tmp = Join-Path $env:TEMP ('svgrender-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null
$count = 0

foreach ($variant in $variants.Keys) {
    $base = $variants[$variant].base
    $accent = $variants[$variant].accent
    $dest = Join-Path $root "$OutDir\$variant"
    New-Item -ItemType Directory -Path $dest -Force | Out-Null

    foreach ($svg in $svgs) {
        $markup = Get-Content $svg.FullName -Raw
        $html = @"
<!DOCTYPE html><html><head><meta charset="utf-8"><style>
html,body{margin:0;padding:0;background:transparent;width:${Size}px;height:${Size}px;overflow:hidden}
svg{display:block;width:${Size}px;height:${Size}px;color:$base;--accent:$accent}
</style></head><body>$markup</body></html>
"@
        $htmlPath = Join-Path $tmp ($svg.BaseName + '-' + $variant + '.html')
        Set-Content -Path $htmlPath -Value $html -Encoding UTF8
        $png = Join-Path $dest ($svg.BaseName + '.png')

        & $browser --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 `
            --default-background-color=00000000 --window-size="$Size,$Size" `
            --screenshot="$png" ("file:///" + $htmlPath.Replace('\', '/')) 2>$null | Out-Null

        if (Test-Path $png) { $count++ } else { Write-Host "FEHLER  $variant/$($svg.BaseName).png" }
    }
    Write-Host "$variant : $($svgs.Count) Icons"
}

Remove-Item $tmp -Recurse -Force
Write-Host "`n$count PNGs geschrieben ($Size x $Size)."
