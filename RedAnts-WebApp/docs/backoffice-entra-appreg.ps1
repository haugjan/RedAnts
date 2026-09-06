#requires -Version 7
# ============================================================================
#  Red Ants: Microsoft-Login (Entra ID) fuer das Umbraco-Backoffice einrichten.
#  Eigene App-Registrierung mit Redirect-URIs (interaktives Web-Login), NICHT
#  die Graph-Mail-App (die nutzt reine App-Permissions ohne Redirect).
#  Nur bestehende Umbraco-User mit @redants.ch werden verknuepft; lokales
#  Passwort-Login ist im Code gesperrt (DenyLocalLogin).
#  Als Entra-Admin (fuer Single-Tenant-App + Client-Secret) ausfuehren.
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$TenantId   = ''   # redants.ch Tenant-ID (gleicher Tenant wie Graph:TenantId)
$AppName    = 'Red Ants Backoffice Login'
$CallbackPath  = '/umbraco-entra-signin'
$SignoutPath   = '/umbraco-entra-signout'
$AdminHosts = @(
    'https://admin.redants.ch',        # PROD
    'https://admin-dev.redants.ch',    # DEV
    'http://localhost:5601'            # lokal (Port an den lokalen Start anpassen)
)
# ----------------------------------------------------------------------------

$RedirectUris = $AdminHosts | ForEach-Object { "$_$CallbackPath" }
$LogoutUris   = $AdminHosts | ForEach-Object { "$_$SignoutPath" }

Write-Host "`n=================================================================" -ForegroundColor Magenta
Write-Host " App-Registrierung im Entra-Portal (portal.azure.com > Entra > App registrations):" -ForegroundColor Magenta
Write-Host "   1) New registration: Name '$AppName', 'Accounts in this organizational directory only' (Single tenant)." -ForegroundColor Magenta
Write-Host "      Platform 'Web'. Redirect-URIs (alle drei eintragen):" -ForegroundColor Magenta
$RedirectUris | ForEach-Object { Write-Host "        - $_" -ForegroundColor Magenta }
Write-Host "      Front-channel logout URL (eine reicht, z.B. PROD): $($LogoutUris[0])" -ForegroundColor Magenta
Write-Host "      -> Application (client) ID + Directory (tenant) ID notieren." -ForegroundColor Magenta
Write-Host "   2) Authentication: 'ID tokens' NICHT noetig (Authorization Code Flow). Keine impliziten Grants." -ForegroundColor Magenta
Write-Host "   3) Token configuration > Add optional claim > ID > 'email' (und 'upn'), Haken 'Turn on the Microsoft Graph email permission'." -ForegroundColor Magenta
Write-Host "   4) API permissions: Microsoft Graph > DELEGATED > openid, profile, email (User.Read reicht). Admin consent erteilen." -ForegroundColor Magenta
Write-Host "   5) Certificates & secrets > New client secret > VALUE (Geheimnis) sofort kopieren." -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta

if ([string]::IsNullOrWhiteSpace($TenantId)) { $TenantId = Read-Host 'Directory (tenant) ID' }
$AppId  = Read-Host 'Application (client) ID der neuen App-Registrierung'
if ([string]::IsNullOrWhiteSpace($AppId)) { throw 'Ohne App-ID kann nichts gesetzt werden.' }

Write-Host "`n--- Lokal (user-secrets) ---" -ForegroundColor Green
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:TenantId`" `"$TenantId`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:ClientId`" `"$AppId`" --project C:\development\RedAnts-s1\RedAnts.csproj"
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:ClientSecret`" `"<CLIENT-SECRET-VALUE>`" --project C:\development\RedAnts-s1\RedAnts.csproj"

Write-Host "`n--- DEV App Service (app-redants-dev) ---" -ForegroundColor Green
Write-Host "  az webapp config appsettings set -g RG_RedAnts -n app-redants-dev --settings ``"
Write-Host "    BackOfficeAuth__TenantId=$TenantId BackOfficeAuth__ClientId=$AppId BackOfficeAuth__ClientSecret=<SECRET>"

Write-Host "`n--- PROD App Service (app-redants-prod) ---" -ForegroundColor Green
Write-Host "  az webapp config appsettings set -g RG_RedAnts -n app-redants-prod --settings ``"
Write-Host "    BackOfficeAuth__TenantId=$TenantId BackOfficeAuth__ClientId=$AppId BackOfficeAuth__ClientSecret=<SECRET>"

Write-Host "`nHinweis: Backoffice-User muessen ihre @redants.ch-E-Mail als Umbraco-User-E-Mail hinterlegt haben," -ForegroundColor Yellow
Write-Host "sonst greift die Verknuepfung nicht (und mangels Auto-Anlage kein Zugang)." -ForegroundColor Yellow
Write-Host "Break-glass: BackOfficeAuth-Settings leeren -> klassisches Umbraco-Login ist wieder aktiv." -ForegroundColor DarkGray
