#requires -Version 7
# ============================================================================
#  Red Ants: Shared Mailbox tickets@ als Absender vorbereiten.
#  Nur Exchange Online. Kein Dienstkonto/SMTP AUTH mehr, da der Versand ueber
#  Graph app-only laeuft (siehe docs/o365-graph-appreg.ps1).
#  Einmalig als M365-Admin ausfuehren. Interaktive Anmeldung (Browser/MFA).
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$AdminUpn   = 'jan.haug@redants.ch'                 # dein Admin-Login
$SharedSmtp = 'tickets@redants.ch'                  # Shared Mailbox = Absender
$SharedName = 'Red Ants Ticketing'
$SharedAlias= 'tickets'
$GrantUser  = 'jan.haug@redants.ch'                 # bekommt Vollzugriff + SendAs
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

# 2) Shared Mailbox anlegen (idempotent, mit Kollisionspruefung) -------------
$existingMb = Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue
if ($existingMb -and $existingMb.RecipientTypeDetails -eq 'SharedMailbox' -and "$($existingMb.PrimarySmtpAddress)" -ieq $SharedSmtp) {
    Write-Host "Shared Mailbox existiert bereits." -ForegroundColor Yellow
} elseif ($existingMb) {
    throw ("$SharedSmtp ist bereits als Adresse bei '$($existingMb.PrimarySmtpAddress)' ($($existingMb.RecipientTypeDetails)) belegt. " +
           "Zuerst dort entfernen: Set-Mailbox '$($existingMb.PrimarySmtpAddress)' -EmailAddresses @{remove='$SharedSmtp'} " +
           "und dieses Skript erneut ausfuehren.")
} else {
    New-Mailbox -Shared -Name $SharedName -DisplayName $SharedName -Alias $SharedAlias -PrimarySmtpAddress $SharedSmtp | Out-Null
    Write-Host "Shared Mailbox angelegt, warte auf Bereitstellung..." -ForegroundColor Cyan
    for ($i=0; $i -lt 30 -and -not (Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue); $i++) { Start-Sleep 5 }
}

# 3) jan.haug berechtigen (Vollzugriff + Senden als) -------------------------
Add-MailboxPermission  -Identity $SharedSmtp -User $GrantUser -AccessRights FullAccess -InheritanceType All -AutoMapping $true -ErrorAction SilentlyContinue | Out-Null
Add-RecipientPermission -Identity $SharedSmtp -Trustee $GrantUser -AccessRights SendAs -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
Write-Host "$GrantUser hat jetzt Vollzugriff + SendAs auf $SharedSmtp." -ForegroundColor Green

# 4) Optional: DKIM-Signierung fuer die Domain aktivieren (DNS-CNAMEs sind da)
try {
    $dkim = Get-DkimSigningConfig -Identity redants.ch -ErrorAction SilentlyContinue
    if (-not $dkim) { New-DkimSigningConfig -DomainName redants.ch -Enabled $true | Out-Null }
    elseif (-not $dkim.Enabled) { Set-DkimSigningConfig -Identity redants.ch -Enabled $true }
    Write-Host "DKIM fuer redants.ch aktiv." -ForegroundColor Green
} catch { Write-Warning "DKIM konnte nicht automatisch aktiviert werden: $($_.Exception.Message)" }

# 5) Kontrolle ----------------------------------------------------------------
Write-Host "`n--- Kontrolle ---" -ForegroundColor Cyan
Get-Mailbox $SharedSmtp | Select-Object DisplayName,PrimarySmtpAddress,RecipientTypeDetails | Format-List
Get-RecipientPermission $SharedSmtp | Where-Object {$_.AccessRights -contains 'SendAs'} | Select-Object Trustee,AccessRights | Format-Table -Auto

Write-Host "`nShared Mailbox bereit. Fuer den App-Versand jetzt ausfuehren:" -ForegroundColor Green
Write-Host "  C:\development\RedAnts-s1\docs\o365-graph-appreg.ps1   (App-Registrierung + Access Policy)"
Write-Host "  C:\development\RedAnts-s1\docs\o365-mail-forwarding.ps1 (optional: Kopie eingehender Mails ans Team)"
