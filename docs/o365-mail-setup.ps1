#requires -Version 7
# ============================================================================
#  Red Ants: Shared Mailbox tickets@ + SMTP-AUTH-Freigabe fuer service-user-web
#  Nur Exchange Online (kein Microsoft.Graph, um SDK-Assembly-Konflikte zu
#  vermeiden). Das Dienstkonto wird im M365 Admin Center angelegt (Schritt 4).
#  Einmalig als M365-Admin ausfuehren. Interaktive Anmeldung (Browser/MFA).
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$AdminUpn   = 'jan.haug@redants.ch'                 # dein Admin-Login
$SharedSmtp = 'tickets@redants.ch'                  # Shared Mailbox = Absender
$SharedName = 'Red Ants Ticketing'
$SharedAlias= 'tickets'
$GrantUser  = 'jan.haug@redants.ch'                 # bekommt Vollzugriff + SendAs
$SvcUpn     = 'service-user-web@redants.ch'         # Dienstkonto (App-Login)
$BackupCsv  = "$HOME\tickets-verteiler-mitglieder.csv"
# ----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

# 0) Modul + Verbindung -------------------------------------------------------
if (-not (Get-Module ExchangeOnlineManagement -ListAvailable)) {
    if (Get-Command Install-PSResource -ErrorAction SilentlyContinue) { Install-PSResource ExchangeOnlineManagement -Scope CurrentUser -TrustRepository }
    else { Install-Module ExchangeOnlineManagement -Scope CurrentUser -Force -AllowClobber }
}
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false

Write-Warning "SMTP AUTH funktioniert nur, wenn 'Security Defaults' im Entra-Portal AUS sind (oder das"
Write-Warning "Dienstkonto eine Conditional-Access-Ausnahme hat). Bitte im Entra-Admin unter"
Write-Warning "'Identitaet > Uebersicht > Eigenschaften > Sicherheitsstandards verwalten' pruefen."

# 1) Verteiler pruefen + Mitglieder sichern ----------------------------------
$dl = Get-DistributionGroup -Identity $SharedSmtp -ErrorAction SilentlyContinue
if ($dl) {
    Write-Host "Verteilergruppe '$SharedSmtp' gefunden. Mitglieder werden gesichert nach $BackupCsv" -ForegroundColor Cyan
    Get-DistributionGroupMember -Identity $SharedSmtp |
        Select-Object DisplayName,PrimarySmtpAddress,RecipientType |
        Export-Csv -Path $BackupCsv -NoTypeInformation -Encoding UTF8
    Get-Content $BackupCsv | Write-Host

    $ok = Read-Host "Verteiler '$SharedSmtp' jetzt LOESCHEN, um die Adresse fuer die Shared Mailbox freizugeben? (ja/nein)"
    if ($ok -ne 'ja') { throw "Abgebrochen. Adresse bleibt beim Verteiler." }
    Remove-DistributionGroup -Identity $SharedSmtp -Confirm:$false
    Write-Host "Verteiler entfernt." -ForegroundColor Green
} else {
    Write-Host "Keine Verteilergruppe auf $SharedSmtp gefunden, ueberspringe." -ForegroundColor Yellow
}

# 2) Shared Mailbox anlegen (idempotent) -------------------------------------
if (-not (Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue)) {
    New-Mailbox -Shared -Name $SharedName -DisplayName $SharedName -Alias $SharedAlias -PrimarySmtpAddress $SharedSmtp | Out-Null
    Write-Host "Shared Mailbox angelegt, warte auf Bereitstellung..." -ForegroundColor Cyan
    for ($i=0; $i -lt 30 -and -not (Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue); $i++) { Start-Sleep 5 }
} else {
    Write-Host "Shared Mailbox existiert bereits." -ForegroundColor Yellow
}

# 3) jan.haug berechtigen (Vollzugriff + Senden als) -------------------------
Add-MailboxPermission  -Identity $SharedSmtp -User $GrantUser -AccessRights FullAccess -InheritanceType All -AutoMapping $true -ErrorAction SilentlyContinue | Out-Null
Add-RecipientPermission -Identity $SharedSmtp -Trustee $GrantUser -AccessRights SendAs -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
Write-Host "$GrantUser hat jetzt Vollzugriff + SendAs auf $SharedSmtp." -ForegroundColor Green
# Frueher Verteiler-Mitglieder bei Bedarf ebenso nachtragen (siehe o365-mail-forwarding.ps1
# fuer die saubere Variante ueber eine Gruppe).

# 4) Dienstkonto im Admin Center anlegen (manuell, kein Graph) ---------------
Write-Host "`n=================================================================" -ForegroundColor Magenta
Write-Host " Jetzt im M365 Admin Center anlegen: $SvcUpn" -ForegroundColor Magenta
Write-Host "   - Benutzer > Aktiver Benutzer > hinzufuegen"                     -ForegroundColor Magenta
Write-Host "   - Exchange-Online-Lizenz (Plan 1 genuegt), Nutzungsort Schweiz"  -ForegroundColor Magenta
Write-Host "   - starkes Passwort, 'Passwort bei erster Anmeldung aendern' AUS" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta
Read-Host "Wenn $SvcUpn angelegt UND lizenziert ist, Enter druecken (Skript wartet aufs Postfach)"
for ($i=0; $i -lt 60 -and -not (Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue); $i++) {
    Start-Sleep 10; Write-Host "  warte auf Postfach $SvcUpn (Bereitstellung dauert oft ein paar Minuten)..." -ForegroundColor DarkGray
}

# 5) Dienstkonto darf als tickets@ senden + SMTP AUTH aktivieren -------------
if (-not (Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue)) {
    Write-Warning "Postfach fuer $SvcUpn noch nicht bereit. Spaeter diese zwei Zeilen ausfuehren:"
    Write-Warning "  Add-RecipientPermission -Identity $SharedSmtp -Trustee $SvcUpn -AccessRights SendAs -Confirm:`$false"
    Write-Warning "  Set-CASMailbox -Identity $SvcUpn -SmtpClientAuthenticationDisabled `$false"
} else {
    Add-RecipientPermission -Identity $SharedSmtp -Trustee $SvcUpn -AccessRights SendAs -Confirm:$false | Out-Null
    Set-CASMailbox -Identity $SvcUpn -SmtpClientAuthenticationDisabled $false
    Write-Host "SendAs auf $SharedSmtp + SMTP AUTH fuer $SvcUpn gesetzt." -ForegroundColor Green
}

# 6) Optional: DKIM-Signierung fuer die Domain aktivieren (DNS-CNAMEs sind da)
try {
    $dkim = Get-DkimSigningConfig -Identity redants.ch -ErrorAction SilentlyContinue
    if (-not $dkim) { New-DkimSigningConfig -DomainName redants.ch -Enabled $true | Out-Null }
    elseif (-not $dkim.Enabled) { Set-DkimSigningConfig -Identity redants.ch -Enabled $true }
    Write-Host "DKIM fuer redants.ch aktiv." -ForegroundColor Green
} catch { Write-Warning "DKIM konnte nicht automatisch aktiviert werden: $($_.Exception.Message)" }

# 7) Kontrolle ----------------------------------------------------------------
Write-Host "`n--- Kontrolle ---" -ForegroundColor Cyan
Get-Mailbox $SharedSmtp | Select-Object DisplayName,PrimarySmtpAddress,RecipientTypeDetails | Format-List
Get-RecipientPermission $SharedSmtp | Where-Object {$_.AccessRights -contains 'SendAs'} | Select-Object Trustee,AccessRights | Format-Table -Auto
if (Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue) {
    Get-CASMailbox $SvcUpn | Select-Object Name, SmtpClientAuthenticationDisabled | Format-Table -Auto
}
Write-Host "Tenant-weit SMTP AUTH deaktiviert? (per-Postfach-`$false ueberschreibt dies):" -ForegroundColor Cyan
Get-TransportConfig | Select-Object SmtpClientAuthenticationDisabled | Format-List

Write-Host "`nFertig. Danach die App-Zugangsdaten setzen:" -ForegroundColor Green
Write-Host "  dotnet user-secrets set `"Office365:User`" `"$SvcUpn`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "  dotnet user-secrets set `"Office365:Password`" `"<PASSWORT>`" --project C:\development\RedAnts-s1\RedAnts.csproj"
