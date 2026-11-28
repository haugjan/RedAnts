param([string]$Dir = "dump-sqldb-redants-prod")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$dir = Join-Path $PSScriptRoot $Dir

function Load($name) { Get-Content (Join-Path $dir $name) -Raw -Encoding utf8 | ConvertFrom-Json }

# ---- music library: id -> path label
$music = @{}
function Walk($nodes, $path) {
  foreach ($n in $nodes) {
    $label = if ($path) { "$path · $($n.Label)" } else { $n.Label }
    if ($n.Children -and $n.Children.Count -gt 0) { Walk $n.Children $label }
    else { $music[$n.Id] = $label }
  }
}
$profile = Load "profile-bibliothek.json"
Walk $profile.Root ""
"=== MUSIC ($($music.Count) entries) ==="
$music.GetEnumerator() | Sort-Object Value | ForEach-Object { "  $($_.Key) => $($_.Value)" }

function KeyLine($k) {
  $kind = $k.Kind
  $what = switch ($kind) {
    "musik"   { "MUSIK " + $(if ($music.ContainsKey($k.MusicId)) { $music[$k.MusicId] } else { "?" + $k.MusicId }) }
    "spezial" { "FEST " + $k.Special }
    "seite"   { "SEITE> " + $k.PageId }
    "anzeige" { "ANZEIGE " + $k.Live }
    "befehle" { "BEFEHLE" }
    default   { $kind }
  }
  $steps = ""
  if ($k.Steps -and $k.Steps.Count -gt 0) {
    $steps = " steps[" + (($k.Steps | ForEach-Object {
      $t = $_.Target
      if ($_.Action -eq "musik.start" -and $music.ContainsKey($t)) { $t = $music[$t] }
      "$($_.Action):$t" + $(if ($_.Ms) { "($($_.Ms)ms)" } else { "" }) + $(if ($_.Side) { "[$($_.Side)]" } else { "" })
    }) -join " ; ") + "]"
  }
  $flags = @()
  if ($k.HomeAfter) { $flags += "home" }
  if ($k.PSObject.Properties["Livestream"] -and -not $k.Livestream) { $flags += "noLive" }
  if ($k.PSObject.Properties["Led"] -and -not $k.Led) { $flags += "noLed" }
  $f = if ($flags) { " {" + ($flags -join ",") + "}" } else { "" }
  "    {0,2}: [{1}] {2}{3}{4}" -f $k.Number, $k.Label.Replace("`n", "/"), $what, $steps, $f
}

function PageDump($p, $cols) {
  "  PAGE $($p.Id) '$($p.Name)' parent=$($p.ParentId)"
  foreach ($k in ($p.Keys | Sort-Object Number)) { if ($k.Kind -ne "leer") { KeyLine $k } }
}

"`n=== DECKS (Show:Decks) ==="
$decks = Load "setting-Show_Decks.json"
foreach ($d in $decks) {
  "DECK $($d.Id) '$($d.Name)' $($d.Columns)x$($d.Rows) panic=$($d.PanicKey) back=$($d.BackKey) pages=$($d.Pages.Count)"
  foreach ($p in $d.Pages) { PageDump $p $d.Columns }
}

"`n=== SHEETS (Show:Sheets) ==="
$sheets = Load "setting-Show_Sheets.json"
foreach ($s in $sheets) {
  "SET $($s.Columns)x$($s.Rows) pages=$($s.Pages.Count)"
  foreach ($p in $s.Pages) { PageDump $p $s.Columns }
}

"`n=== HALLS ==="
Get-ChildItem $dir -Filter "hall-*.json" | ForEach-Object {
  $h = Get-Content $_.FullName -Raw -Encoding utf8 | ConvertFrom-Json
  $o = $h.Offered
  $off = if ($o) { "sound=$($o.Sound) clock=$($o.Clock) vmix=$($o.Vmix) led=$($o.Led) light=$($o.Light) players=$($o.Players)" } else { "default" }
  $dv = $h.Devices
  $bm = if ($h.BreakMusicId -and $music.ContainsKey($h.BreakMusicId)) { $music[$h.BreakMusicId] } else { $h.BreakMusicId }
  "HALL $($h.Id) '$($h.Name)' hidden=$($h.Hidden) screen='$($h.DefaultScreen)' deckIds=[$($h.DeckIds -join ',')] ownDecks=$(if ($h.Decks) { $h.Decks.Count } else { 0 }) activeGame=$($h.ActiveGameId)"
  "     offers: $off"
  "     devices: clock=$($dv.ClockType):$($dv.ClockPort) vmix=$($dv.VmixHost):$($dv.VmixPort) sedna=$($dv.SednaHost):$($dv.SednaPort) img=$($dv.ImageFolder) period=$($dv.PeriodMinutes)"
  "     breakMusic: $bm  ledTemplates=$($h.LedTemplates.Count) titles=$($h.OverlayTitles.Count)"
  if ($h.Decks) { foreach ($d in $h.Decks) { "     ownDeck $($d.Id) '$($d.Name)' $($d.Columns)x$($d.Rows) pages=$($d.Pages.Count)" } }
}
