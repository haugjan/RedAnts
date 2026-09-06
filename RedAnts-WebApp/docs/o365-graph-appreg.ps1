#requires -Version 7
# ============================================================================
#  Red Ants: Graph app-only Mailversand einrichten.
#  App-Registrierung (Portal) + Application Access Policy (Exchange Online),
#  die die App darauf beschraenkt, NUR als tickets@ zu senden.
#  Kein Konto, keine Lizenz, keine MFA, kein SMTP AUTH.
#  Als M365-Admin (Global Admin fuer Admin-Consent) ausfuehren.
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$AdminUpn   = 'jan.haug@redants.ch'
$SharedSmtp = 'tickets@redants.ch'                  # Absender, muss existieren (Shared Mailbox)
$GroupName  = 'Graph Mail Senders'
$GroupAlias = 'graph-mail-senders'
$GroupSmtp  = 'graph-mail-senders@redants.ch'       # mail-enabled Security Group fuer die Policy
# ----------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'

# 0) Modul + Exchange-Verbindung ---------------------------------------------
if (-not (Get-Module ExchangeOnlineManagement -ListAvailable)) {
    if (Get-Command Install-PSResource -ErrorAction SilentlyContinue) { Install-PSResource ExchangeOnlineManagement -Scope CurrentUser -TrustRepository }
    else { Install-Module ExchangeOnlineManagement -Scope CurrentUser -Force -AllowClobber }
}
Import-Module ExchangeOnlineManagement
Connect-ExchangeOnline -UserPrincipalName $AdminUpn -ShowBanner:$false

$TenantId = (Get-ConnectionInformation | Select-Object -First 1).TenantId

# 1) App-Registrierung im Portal (manuell, kein Graph-SDK) -------------------
Write-Host "`n=================================================================" -ForegroundColor Magenta
Write-Host " App-Registrierung im Entra-Portal (portal.azure.com > Entra > App registrations):" -ForegroundColor Magenta
Write-Host "   1) New registration: Name 'Red Ants Web App', Single tenant, keine Redirect-URI." -ForegroundColor Magenta
Write-Host "      -> Application (client) ID notieren." -ForegroundColor Magenta
Write-Host "   2) API permissions > Add > Microsoft Graph > APPLICATION permissions > 'Mail.Send' > Add." -ForegroundColor Magenta
Write-Host "      -> danach 'Grant admin consent for <Tenant>' klicken (muss gruen werden)." -ForegroundColor Magenta
Write-Host "   3) Certificates & secrets > New client secret > VALUE (Geheimnis) sofort kopieren." -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta
$AppId = Read-Host "Application (client) ID der neuen App-Registrierung"
if ([string]::IsNullOrWhiteSpace($AppId)) { throw "Ohne App-ID kann die Policy nicht gesetzt werden." }

# 2) Mail-enabled Security Group (Scope fuer die Policy) ---------------------
if (-not (Get-DistributionGroup -Identity $GroupSmtp -ErrorAction SilentlyContinue)) {
    New-DistributionGroup -Name $GroupName -Alias $GroupAlias -PrimarySmtpAddress $GroupSmtp -Type Security | Out-Null
    Write-Host "Security-Gruppe $GroupSmtp angelegt." -ForegroundColor Green
    for ($i=0; $i -lt 20 -and -not (Get-DistributionGroup -Identity $GroupSmtp -ErrorAction SilentlyContinue); $i++) { Start-Sleep 3 }
} else { Write-Host "Security-Gruppe $GroupSmtp existiert bereits." -ForegroundColor Yellow }

$members = @(Get-DistributionGroupMember -Identity $GroupSmtp -ErrorAction SilentlyContinue | ForEach-Object { $_.PrimarySmtpAddress.ToString().ToLower() })
if ($members -notcontains $SharedSmtp.ToLower()) {
    Add-DistributionGroupMember -Identity $GroupSmtp -Member $SharedSmtp
    Write-Host "$SharedSmtp zur Gruppe hinzugefuegt." -ForegroundColor Green
} else { Write-Host "$SharedSmtp ist bereits in der Gruppe." -ForegroundColor Yellow }

# 3) Application Access Policy: App darf NUR als Gruppenmitglieder senden -----
$existing = Get-ApplicationAccessPolicy -ErrorAction SilentlyContinue | Where-Object { $_.AppId -eq $AppId -and $_.ScopeIdentity -like "*$GroupAlias*" }
if (-not $existing) {
    New-ApplicationAccessPolicy -AppId $AppId -PolicyScopeGroupId $GroupSmtp -AccessRight RestrictAccess `
        -Description "Red Ants Web App darf nur als $SharedSmtp senden" | Out-Null
    Write-Host "Application Access Policy gesetzt (RestrictAccess auf $GroupSmtp)." -ForegroundColor Green
} else { Write-Host "Application Access Policy existiert bereits." -ForegroundColor Yellow }

Write-Host "Policy-Propagierung kann bis ~30 Min dauern. Danach Test:" -ForegroundColor DarkGray

# 4) Test: darf die App als tickets@ senden? ---------------------------------
try {
    $t = Test-ApplicationAccessPolicy -Identity $SharedSmtp -AppId $AppId
    Write-Host ("Test {0}: {1}" -f $SharedSmtp, $t.AccessCheckResult) -ForegroundColor Cyan
} catch { Write-Warning "Test noch nicht moeglich (Policy evtl. noch am Propagieren): $($_.Exception.Message)" }

# 5) Ausgabe: was in die App-Konfiguration muss ------------------------------
Write-Host "`n--- App-Konfiguration (user-secrets) ---" -ForegroundColor Green
Write-Host "  dotnet user-secrets set `"Graph:TenantId`" `"$TenantId`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "  dotnet user-secrets set `"Graph:ClientId`" `"$AppId`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "  dotnet user-secrets set `"Graph:ClientSecret`" `"<CLIENT-SECRET-VALUE>`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "`nSender (Graph:Sender) ist bereits $SharedSmtp. Fertig." -ForegroundColor Green
