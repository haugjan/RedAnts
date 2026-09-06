#requires -Version 7
# ============================================================================
#  Red Ants: Read-only Pruefung des Graph-app-only Mailversands.
#  Nur Exchange Online + DNS, kein Microsoft.Graph-SDK. Aendert NICHTS.
#  Zeigt pro Punkt [ OK ] / [FEHLT] / [WARN].
# ============================================================================

# ---------------------------- KONFIG (wie im Setup) -------------------------
$AdminUpn   = 'jan.haug@redants.ch'
$Domain     = 'redants.ch'
$SharedSmtp = 'tickets@redants.ch'
$GrantUser  = 'jan.haug@redants.ch'
$GroupAlias = 'graph-mail-senders'
$GroupSmtp  = 'graph-mail-senders@redants.ch'
# ----------------------------------------------------------------------------

$script:pass = 0; $script:fail = 0; $script:warn = 0
function Check {
    param([string]$Label, [bool]$Ok, [string]$Detail = '', [ValidateSet('fail','warn')][string]$OnFail = 'fail')
    if ($Ok)                    { Write-Host ("[ OK ] {0}" -f $Label) -ForegroundColor Green; $script:pass++ }
    elseif ($OnFail -eq 'warn') { Write-Host ("[WARN] {0}" -f $Label) -ForegroundColor Yellow; $script:warn++ }
    else                        { Write-Host ("[FEHLT] {0}" -f $Label) -ForegroundColor Red; $script:fail++ }
    if ($Detail) { Write-Host ("       {0}" -f $Detail) -ForegroundColor DarkGray }
}

# 1) DNS (braucht keine Anmeldung) -------------------------------------------
Write-Host "`n== DNS ($Domain) ==" -ForegroundColor Cyan
try {
    $mx  = (Resolve-DnsName $Domain -Type MX  -ErrorAction Stop).NameExchange -join ', '
    Check "MX zeigt auf Microsoft" ($mx -match 'mail\.protection\.outlook\.com') $mx
} catch { Check "MX abfragbar" $false $_.Exception.Message }
try {
    $txt = (Resolve-DnsName $Domain -Type TXT -ErrorAction Stop | Where-Object {$_.Strings}).Strings
    $spf = $txt | Where-Object { $_ -like 'v=spf1*' }
    Check "SPF enthaelt spf.protection.outlook.com" ([bool]($spf -match 'include:spf\.protection\.outlook\.com')) ($spf -join ' ')
    Check "M365 Domain-Verifizierung (MS=...)" ([bool]($txt -match '^MS=ms')) ''
} catch { Check "TXT/SPF abfragbar" $false $_.Exception.Message }
foreach ($s in 'selector1','selector2') {
    try {
        $c = (Resolve-DnsName "$s._domainkey.$Domain" -Type CNAME -ErrorAction Stop).NameHost
        Check "DKIM-CNAME $s vorhanden" ([bool]($c -match 'onmicrosoft\.com')) $c
    } catch { Check "DKIM-CNAME $s vorhanden" $false $_.Exception.Message }
}
try {
    $dmarc = (Resolve-DnsName "_dmarc.$Domain" -Type TXT -ErrorAction Stop | Where-Object {$_.Strings}).Strings -join ' '
    Check "DMARC-Record vorhanden" ([bool]($dmarc -like 'v=DMARC1*')) $dmarc 'warn'
} catch { Check "DMARC-Record vorhanden" $false '' 'warn' }

# 2) Exchange-Verbindung ------------------------------------------------------
Write-Host "`n== Exchange Online ==" -ForegroundColor Cyan
if (-not (Get-Module ExchangeOnlineManagement -ListAvailable)) {
    Check "ExchangeOnlineManagement installiert" $false 'Install-PSResource ExchangeOnlineManagement'
    Write-Host "`nErgebnis: $script:pass OK, $script:warn WARN, $script:fail FEHLT" -ForegroundColor Cyan
    return
}
Import-Module ExchangeOnlineManagement
try { $null = Get-ConnectionInformation -ErrorAction Stop } catch { Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false }
Check "Exchange Online verbunden" ([bool](Get-ConnectionInformation))

# 3) tickets@ ist eine Shared Mailbox ----------------------------------------
Write-Host "`n== tickets@ ==" -ForegroundColor Cyan
$dl = Get-DistributionGroup -Identity $SharedSmtp -ErrorAction SilentlyContinue
Check "Alte Verteilergruppe auf $SharedSmtp entfernt" (-not $dl) $(if ($dl) { 'Verteiler blockiert die Shared-Mailbox-Adresse.' } else { '' })
$mb = Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue
Check "Shared Mailbox $SharedSmtp existiert" ([bool]$mb)
if ($mb) { Check "Typ ist SharedMailbox" ($mb.RecipientTypeDetails -eq 'SharedMailbox') $mb.RecipientTypeDetails }

# 4) Rechte von jan.haug ------------------------------------------------------
Write-Host "`n== Berechtigungen ==" -ForegroundColor Cyan
if ($mb) {
    $full = Get-MailboxPermission  -Identity $SharedSmtp | Where-Object { $_.User -like "*$GrantUser*" -and $_.AccessRights -contains 'FullAccess' }
    $saU  = Get-RecipientPermission -Identity $SharedSmtp | Where-Object { $_.Trustee -like "*$GrantUser*" -and $_.AccessRights -contains 'SendAs' }
    Check "$GrantUser hat FullAccess" ([bool]$full)
    Check "$GrantUser hat SendAs"     ([bool]$saU)
}

# 5) Graph-Versand-Rahmen -----------------------------------------------------
Write-Host "`n== Graph app-only ==" -ForegroundColor Cyan
$grp = Get-DistributionGroup -Identity $GroupSmtp -ErrorAction SilentlyContinue
Check "Security-Gruppe $GroupSmtp existiert" ([bool]$grp) 'Wird von o365-graph-appreg.ps1 angelegt.'
if ($grp) {
    $gm = @(Get-DistributionGroupMember -Identity $GroupSmtp -ErrorAction SilentlyContinue | ForEach-Object { $_.PrimarySmtpAddress.ToString().ToLower() })
    Check "$SharedSmtp ist in der Gruppe" ($gm -contains $SharedSmtp.ToLower())
}
$pol = Get-ApplicationAccessPolicy -ErrorAction SilentlyContinue | Where-Object { $_.AccessRight -eq 'RestrictAccess' -and ("$($_.ScopeName)$($_.ScopeIdentity)" -like "*$GroupAlias*") }
Check "Application Access Policy (RestrictAccess) gesetzt" ([bool]$pol) $(if ($pol) { "AppId $($pol.AppId -join ', ')" } else { 'Wird von o365-graph-appreg.ps1 angelegt.' })

# 6) DKIM ---------------------------------------------------------------------
Write-Host "`n== DKIM ==" -ForegroundColor Cyan
try {
    $dkim = Get-DkimSigningConfig -Identity $Domain -ErrorAction SilentlyContinue
    Check "DKIM-Signierung fuer $Domain aktiv" ([bool]($dkim.Enabled)) '' 'warn'
} catch { Check "DKIM-Signierung fuer $Domain aktiv" $false '' 'warn' }

# Zusammenfassung -------------------------------------------------------------
Write-Host ("`nErgebnis: {0} OK, {1} WARN, {2} FEHLT" -f $script:pass, $script:warn, $script:fail) -ForegroundColor Cyan
if ($script:fail -eq 0) { Write-Host "Alles Notwendige vorhanden. Client-Secret in user-secrets nicht vergessen." -ForegroundColor Green }
else { Write-Host "Es fehlt noch etwas (siehe [FEHLT]). Setup + o365-graph-appreg.ps1 ausfuehren." -ForegroundColor Yellow }
