#requires -Version 7
# ============================================================================
#  Red Ants: Kopie eingehender Mails an tickets@ an ein Team weiterleiten.
#  Variante A: Verteilergruppe tickets-team@ + DeliverToMailboxAndForward.
#  Original bleibt in der Shared Mailbox, jedes Gruppenmitglied bekommt eine
#  Kopie ins eigene Postfach. Idempotent, aendert nur Gruppe + Weiterleitung.
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$AdminUpn   = 'jan.haug@redants.ch'
$SharedSmtp = 'tickets@redants.ch'
$GroupName  = 'Tickets Team'
$GroupAlias = 'tickets-team'
$GroupSmtp  = 'tickets-team@redants.ch'
$Members    = @('jan.haug@redants.ch')                       # Basis-Empfaenger
$ImportCsv  = $true                                          # alte Verteiler-Mitglieder uebernehmen?
$BackupCsv  = "$HOME\tickets-verteiler-mitglieder.csv"       # vom Setup-Skript erzeugt
# ----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

if (-not (Get-Module ExchangeOnlineManagement -ListAvailable)) {
    if (Get-Command Install-PSResource -ErrorAction SilentlyContinue) { Install-PSResource ExchangeOnlineManagement -Scope CurrentUser -TrustRepository }
    else { Install-Module ExchangeOnlineManagement -Scope CurrentUser -Force -AllowClobber }
}
Import-Module ExchangeOnlineManagement
try { $null = Get-ConnectionInformation -ErrorAction Stop } catch { Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false }

# 1) Gruppe sicherstellen -----------------------------------------------------
if (-not (Get-DistributionGroup -Identity $GroupSmtp -ErrorAction SilentlyContinue)) {
    New-DistributionGroup -Name $GroupName -Alias $GroupAlias -PrimarySmtpAddress $GroupSmtp -Type Distribution | Out-Null
    Write-Host "Gruppe $GroupSmtp angelegt." -ForegroundColor Green
    for ($i=0; $i -lt 20 -and -not (Get-DistributionGroup -Identity $GroupSmtp -ErrorAction SilentlyContinue); $i++) { Start-Sleep 3 }
} else {
    Write-Host "Gruppe $GroupSmtp existiert bereits." -ForegroundColor Yellow
}

# 2) Empfaengerliste zusammenstellen (Basis + optional CSV) ------------------
$wanted = [System.Collections.Generic.List[string]]::new()
$Members | Where-Object { $_ } | ForEach-Object { $wanted.Add($_.Trim().ToLower()) }
if ($ImportCsv -and (Test-Path $BackupCsv)) {
    Import-Csv $BackupCsv | ForEach-Object {
        if ($_.PrimarySmtpAddress) { $wanted.Add($_.PrimarySmtpAddress.Trim().ToLower()) }
    }
    Write-Host "Verteiler-Mitglieder aus $BackupCsv uebernommen." -ForegroundColor Cyan
}
$wanted = $wanted | Sort-Object -Unique

# 3) Mitglieder hinzufuegen (nur fehlende) -----------------------------------
$current = @(Get-DistributionGroupMember -Identity $GroupSmtp -ErrorAction SilentlyContinue |
             ForEach-Object { $_.PrimarySmtpAddress.ToString().ToLower() })
foreach ($m in $wanted) {
    if ($current -contains $m) { Write-Host "  bereits Mitglied: $m" -ForegroundColor DarkGray; continue }
    try {
        Add-DistributionGroupMember -Identity $GroupSmtp -Member $m -ErrorAction Stop
        Write-Host "  hinzugefuegt: $m" -ForegroundColor Green
    } catch {
        Write-Warning "  konnte $m nicht hinzufuegen: $($_.Exception.Message)"
    }
}

# 4) Shared Mailbox: lokal zustellen UND Kopie an die Gruppe ------------------
if (Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue) {
    Set-Mailbox -Identity $SharedSmtp -DeliverToMailboxAndForward $true -ForwardingAddress $GroupSmtp
    Write-Host "Weiterleitung gesetzt: $SharedSmtp -> $GroupSmtp (Original bleibt im Postfach)." -ForegroundColor Green
} else {
    Write-Warning "Shared Mailbox $SharedSmtp nicht gefunden. Zuerst o365-mail-setup.ps1 ausfuehren, dann dieses Skript."
}

# 5) Kontrolle ----------------------------------------------------------------
Write-Host "`n--- Kontrolle ---" -ForegroundColor Cyan
Write-Host "Gruppenmitglieder ($GroupSmtp):"
Get-DistributionGroupMember -Identity $GroupSmtp | Select-Object DisplayName,PrimarySmtpAddress | Format-Table -Auto
if (Get-Mailbox -Identity $SharedSmtp -ErrorAction SilentlyContinue) {
    Get-Mailbox -Identity $SharedSmtp | Select-Object DisplayName,DeliverToMailboxAndForward,ForwardingAddress | Format-List
}
Write-Host "Fertig. Kopie-Empfaenger aendern = einfach Mitglieder in $GroupSmtp anpassen." -ForegroundColor Green
