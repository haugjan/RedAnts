#requires -Version 7
# ============================================================================
#  Red Ants: Read-only Pruefung des O365-Mailversands (Shared Mailbox + SMTP).
#  Nur Exchange Online + DNS, kein Microsoft.Graph. Aendert NICHTS.
#  Zeigt pro Punkt [ OK ] / [FEHLT] / [WARN].
# ============================================================================

# ---------------------------- KONFIG (wie im Setup) -------------------------
$AdminUpn   = 'jan.haug@redants.ch'
$Domain     = 'redants.ch'
$SharedSmtp = 'tickets@redants.ch'
$GrantUser  = 'jan.haug@redants.ch'
$SvcUpn     = 'service-user-web@redants.ch'
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

# 3) Verteiler weg? -----------------------------------------------------------
Write-Host "`n== tickets@ Objekt ==" -ForegroundColor Cyan
$dl = Get-DistributionGroup -Identity $SharedSmtp -ErrorAction SilentlyContinue
Check "Alte Verteilergruppe auf $SharedSmtp entfernt" (-not $dl) $(if ($dl) { 'Verteiler existiert noch, blockiert die Shared-Mailbox-Adresse.' } else { '' })

# 4) Shared Mailbox -----------------------------------------------------------
$mb = Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue
Check "Shared Mailbox $SharedSmtp existiert" ([bool]$mb) ''
if ($mb) { Check "Typ ist SharedMailbox" ($mb.RecipientTypeDetails -eq 'SharedMailbox') $mb.RecipientTypeDetails }

# 5) Rechte von jan.haug ------------------------------------------------------
Write-Host "`n== Berechtigungen ==" -ForegroundColor Cyan
if ($mb) {
    $full = Get-MailboxPermission  -Identity $SharedSmtp | Where-Object { $_.User -like "*$GrantUser*" -and $_.AccessRights -contains 'FullAccess' }
    $saU  = Get-RecipientPermission -Identity $SharedSmtp | Where-Object { $_.Trustee -like "*$GrantUser*" -and $_.AccessRights -contains 'SendAs' }
    Check "$GrantUser hat FullAccess" ([bool]$full)
    Check "$GrantUser hat SendAs"     ([bool]$saU)
}

# 6) Dienstkonto (via Exchange, kein Graph) ----------------------------------
Write-Host "`n== Dienstkonto $SvcUpn ==" -ForegroundColor Cyan
$svcUser = Get-User -Identity $SvcUpn -ErrorAction SilentlyContinue
Check "Konto existiert" ([bool]$svcUser)
$svcMb = Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue
Check "Postfach bereitgestellt (= lizenziert)" ([bool]$svcMb) $(if (-not $svcMb) { 'Konto im Admin Center anlegen + Exchange-Lizenz zuweisen (kann Minuten dauern).' } else { '' })
if ($svcMb) {
    $saS = Get-RecipientPermission -Identity $SharedSmtp | Where-Object { $_.Trustee -like "*$SvcUpn*" -and $_.AccessRights -contains 'SendAs' }
    Check "Dienstkonto darf als $SharedSmtp senden (SendAs)" ([bool]$saS)
    $cas = Get-CASMailbox -Identity $SvcUpn
    Check "SMTP AUTH fuer Dienstkonto aktiviert" ($cas.SmtpClientAuthenticationDisabled -eq $false) "SmtpClientAuthenticationDisabled=$($cas.SmtpClientAuthenticationDisabled)"
}

# 7) Tenant-Rahmen ------------------------------------------------------------
Write-Host "`n== Tenant / Sicherheit ==" -ForegroundColor Cyan
$tc = Get-TransportConfig
Check "Tenant blockiert SMTP AUTH nicht global" ($tc.SmtpClientAuthenticationDisabled -ne $true) "TransportConfig.SmtpClientAuthenticationDisabled=$($tc.SmtpClientAuthenticationDisabled) (per-Postfach ueberschreibt)" 'warn'
Check "Security Defaults manuell pruefen" $false "Im Entra-Portal 'Sicherheitsstandards' pruefen: bei AKTIV braucht das Dienstkonto eine Conditional-Access-Ausnahme fuer SMTP AUTH." 'warn'
try {
    $dkim = Get-DkimSigningConfig -Identity $Domain -ErrorAction SilentlyContinue
    Check "DKIM-Signierung fuer $Domain aktiv" ([bool]($dkim.Enabled)) '' 'warn'
} catch { Check "DKIM-Signierung fuer $Domain aktiv" $false '' 'warn' }

# Zusammenfassung -------------------------------------------------------------
Write-Host ("`nErgebnis: {0} OK, {1} WARN, {2} FEHLT" -f $script:pass, $script:warn, $script:fail) -ForegroundColor Cyan
if ($script:fail -eq 0) { Write-Host "Alles Notwendige vorhanden. WARN-Punkte pruefen (v.a. Security Defaults)." -ForegroundColor Green }
else { Write-Host "Es fehlt noch etwas (siehe [FEHLT]). Setup-Skript o365-mail-setup.ps1 ausfuehren." -ForegroundColor Yellow }
