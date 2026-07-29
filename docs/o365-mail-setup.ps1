#requires -Version 7
# ============================================================================
#  Red Ants: Shared Mailbox tickets@ + Dienstkonto service-user-web + SMTP AUTH
#  Einmalig als M365-Admin ausfuehren. Interaktive Anmeldung (Browser/MFA).
#  Prueft den Zustand mit docs/o365-mail-check.ps1 gegen.
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$AdminUpn      = 'DEIN-ADMIN@redants.ch'            # dein Admin-Login
$SharedSmtp    = 'tickets@redants.ch'               # Shared Mailbox = Absender
$SharedName    = 'Red Ants Ticketing'
$SharedAlias   = 'tickets'
$GrantUser     = 'jan.haug@redants.ch'              # bekommt Vollzugriff + SendAs
$SvcUpn        = 'service-user-web@redants.ch'      # Dienstkonto (App-Login)
$SvcDisplay    = 'Red Ants Web App (Service)'
$SvcAlias      = 'service-user-web'
$UsageLocation = 'CH'
$LicensePref   = @('EXCHANGESTANDARD','O365_BUSINESS_ESSENTIALS','STANDARDPACK','SPB','SPE_E3')  # Wunsch-Reihenfolge
$BackupCsv     = "$HOME\tickets-verteiler-mitglieder.csv"
# ----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

# 0) Module + Verbindungen ----------------------------------------------------
foreach ($m in 'ExchangeOnlineManagement','Microsoft.Graph.Users','Microsoft.Graph.Identity.DirectoryManagement') {
    if (-not (Get-Module $m -ListAvailable)) { Install-Module $m -Scope CurrentUser -Force }
}
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false
Connect-MgGraph -Scopes 'User.ReadWrite.All','Organization.Read.All','Directory.ReadWrite.All','Policy.Read.All' -NoWelcome

# Sicherheits-Check: blockieren Security Defaults legacy SMTP AUTH?
$secDefaults = (Get-MgPolicyIdentitySecurityDefaultEnforcementPolicy).IsEnabled
if ($secDefaults) {
    Write-Warning "Security Defaults sind AKTIV. Damit blockiert der Tenant SMTP AUTH (Legacy-Auth)."
    Write-Warning "Das Dienstkonto braucht danach eine Conditional-Access-Ausnahme ODER Security Defaults muessen aus."
}

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
# Frueher Verteiler-Mitglieder bei Bedarf hier ebenso nachtragen:
#   Import-Csv $BackupCsv | ForEach-Object { Add-MailboxPermission -Identity $SharedSmtp -User $_.PrimarySmtpAddress -AccessRights FullAccess -AutoMapping $true }

# 4) Dienstkonto anlegen (idempotent) + Passwort generieren ------------------
$existing = Get-MgUser -Filter "userPrincipalName eq '$SvcUpn'" -ErrorAction SilentlyContinue
if (-not $existing) {
    $bytes = [byte[]]::new(18); [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $Password = ([Convert]::ToBase64String($bytes) -replace '[+/=]','') + 'A9!'
    $pwProfile = @{ Password = $Password; ForceChangePasswordNextSignIn = $false }
    New-MgUser -DisplayName $SvcDisplay -UserPrincipalName $SvcUpn -MailNickname $SvcAlias `
               -AccountEnabled -PasswordProfile $pwProfile -UsageLocation $UsageLocation | Out-Null
    Write-Host "Dienstkonto angelegt." -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Magenta
    Write-Host " PASSWORT fuer $SvcUpn (JETZT sicher notieren):" -ForegroundColor Magenta
    Write-Host "   $Password" -ForegroundColor Magenta
    Write-Host "==================================================================" -ForegroundColor Magenta
} else {
    Write-Host "Dienstkonto existiert bereits, Passwort unveraendert." -ForegroundColor Yellow
    Update-MgUser -UserId $SvcUpn -UsageLocation $UsageLocation
}

# 5) Lizenz zuweisen (erste passende mit freier Kapazitaet) ------------------
$mbEnabled = Get-MgUserLicenseDetail -UserId $SvcUpn -ErrorAction SilentlyContinue
if (-not $mbEnabled) {
    $skus = Get-MgSubscribedSku -All
    Write-Host "Verfuegbare Lizenzen:" -ForegroundColor Cyan
    $skus | Select-Object SkuPartNumber, @{n='Frei';e={$_.PrepaidUnits.Enabled - $_.ConsumedUnits}} | Format-Table -Auto
    $sku = foreach ($p in $LicensePref) { $skus | Where-Object { $_.SkuPartNumber -eq $p -and ($_.PrepaidUnits.Enabled - $_.ConsumedUnits) -gt 0 } | Select-Object -First 1 }
    $sku = $sku | Select-Object -First 1
    if (-not $sku) { throw "Keine passende Lizenz mit freier Kapazitaet gefunden. Lizenz im Admin Center zuweisen und ab Schritt 6 weiter." }
    Set-MgUserLicense -UserId $SvcUpn -AddLicenses @{ SkuId = $sku.SkuId } -RemoveLicenses @() | Out-Null
    Write-Host "Lizenz $($sku.SkuPartNumber) zugewiesen. Warte auf Postfach-Bereitstellung (kann Minuten dauern)..." -ForegroundColor Cyan
    for ($i=0; $i -lt 60 -and -not (Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue); $i++) { Start-Sleep 10 }
}

# 6) Dienstkonto darf als tickets@ senden + SMTP AUTH aktivieren -------------
if (-not (Get-Mailbox -Identity $SvcUpn -ErrorAction SilentlyContinue)) {
    Write-Warning "Postfach fuer $SvcUpn noch nicht bereit. Schritt 6 spaeter erneut ausfuehren:"
    Write-Warning "  Add-RecipientPermission -Identity $SharedSmtp -Trustee $SvcUpn -AccessRights SendAs -Confirm:`$false"
    Write-Warning "  Set-CASMailbox -Identity $SvcUpn -SmtpClientAuthenticationDisabled `$false"
} else {
    Add-RecipientPermission -Identity $SharedSmtp -Trustee $SvcUpn -AccessRights SendAs -Confirm:$false | Out-Null
    Set-CASMailbox -Identity $SvcUpn -SmtpClientAuthenticationDisabled $false
    Write-Host "SendAs auf $SharedSmtp + SMTP AUTH fuer $SvcUpn gesetzt." -ForegroundColor Green
}

# 7) Optional: DKIM-Signierung fuer die Domain aktivieren (DNS-CNAMEs sind da)
try {
    $dkim = Get-DkimSigningConfig -Identity redants.ch -ErrorAction SilentlyContinue
    if (-not $dkim) { New-DkimSigningConfig -DomainName redants.ch -Enabled $true | Out-Null }
    elseif (-not $dkim.Enabled) { Set-DkimSigningConfig -Identity redants.ch -Enabled $true }
    Write-Host "DKIM fuer redants.ch aktiv." -ForegroundColor Green
} catch { Write-Warning "DKIM konnte nicht automatisch aktiviert werden: $($_.Exception.Message)" }

# 8) Kontrolle ----------------------------------------------------------------
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
