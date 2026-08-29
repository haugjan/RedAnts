#Requires -Version 7.0
<#
.SYNOPSIS
    RedAnts: SQL-Passwörter nahtlos rotieren.

.DESCRIPTION
    Rotiert die SQL-Passwörter für redants_app (DEV + PROD) und redants_backup
    ohne Unterbruch:

    Bestehende Datenbankverbindungen der laufenden App bleiben aktiv — ALTER USER
    trennt keine offenen Sessions. Erst neue Verbindungen nach dem App-Neustart
    verwenden das neue Passwort. Die App startet mit dem neuen DSN hoch.

    Phase 1  – Admin-Creds aus KV lesen
    Phase 2  – Neue Passwörter generieren
    Phase 3  – Aktuelle Secrets als «…-prev» in KV sichern (Rollback-Basis)
    Phase 4  – SQL-Passwörter ändern (Firewall kurz öffnen / schliessen)
    Phase 5  – Neue DSN in KV und App Settings eintragen (App-Neustart ausgelöst)
    Phase 6  – Health Checks
    Phase 7  – «…-prev»-Secrets aus KV löschen

    Im Fehlerfall bleiben die «prev»-Secrets erhalten. Rollback:
      1. Altes Passwort aus KV «…-prev» lesen
      2. SQL: ALTER USER / ALTER LOGIN mit altem Passwort wiederherstellen
      3. App Settings manuell zurücksetzen (KV-Secret-Name ohne «-prev»)
      4. «prev»-Secrets manuell löschen:
         az keyvault secret delete --vault-name kv-redants-prod --name <name>-prev

.PARAMETER SkipDev
    DEV-Rotation überspringen.

.PARAMETER SkipProd
    PROD-Rotation überspringen.

.PARAMETER SkipBackup
    Rotation des Backup-Logins überspringen.

.PARAMETER SkipHealthChecks
    Health Checks nach App-Restart überspringen.

.NOTES
    Voraussetzungen: PowerShell 7+, Azure CLI (az), SqlServer-Modul oder sqlcmd.
#>

param(
    [switch]$SkipDev,
    [switch]$SkipProd,
    [switch]$SkipBackup,
    [switch]$SkipHealthChecks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$KV              = "kv-redants-prod"
$SQL_SERVER_FQDN = "sql-redants-ch.database.windows.net"
$SQL_SERVER_NAME = "sql-redants-ch"
$SUBSCRIPTION    = "fdf0cdfa-61ef-409f-aa8b-bb0c6a306e3b"
$RG              = "RG_RedAnts"
$APP_DEV         = "app-redants-dev"
$APP_PROD        = "app-redants-prod"
$DB_DEV          = "sqldb-redants-dev"
$DB_PROD         = "sqldb-redants-prod"
$HEALTH_DEV      = "https://tickets-dev.redants.ch/health"
$HEALTH_PROD     = "https://tickets.redants.ch/health"
$FW_RULE_NAME    = "temp-rotate-passwords"

function New-SqlPw {
    [System.Convert]::ToBase64String(
        [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24)
    ) -replace '[^A-Za-z0-9]', 'x'
}

function Step { param($Msg) Write-Host "`n── $Msg" -ForegroundColor Cyan }
function Ok   { param($Msg) Write-Host "  v $Msg" -ForegroundColor Green }
function Warn { param($Msg) Write-Host "  ! $Msg" -ForegroundColor Yellow }

function Plain([securestring]$s) {
    [System.Net.NetworkCredential]::new("", $s).Password
}

function Invoke-AzCmd {
    param([string[]]$ArgList, [switch]$RedactOnError)
    $out = az @ArgList 2>&1
    if ($LASTEXITCODE -ne 0) {
        $msg = if ($RedactOnError) { "[Ausgabe wegen sensitiver Daten unterdrückt]" } else { "$out" }
        throw "az Fehler (exit $LASTEXITCODE):`n$msg"
    }
    $out
}

function Read-KvSecret([string]$Name) {
    (Invoke-AzCmd @("keyvault", "secret", "show",
        "--vault-name", $KV, "--name", $Name,
        "--query", "value", "-o", "tsv") -RedactOnError).Trim()
}

function Set-KvSecret([string]$Name, [string]$Value) {
    Invoke-AzCmd @("keyvault", "secret", "set",
        "--vault-name", $KV, "--name", $Name,
        "--value", $Value, "--output", "none") -RedactOnError | Out-Null
}

function Invoke-SqlQuery {
    param(
        [string]$Database,
        [string]$AdminUser,
        [securestring]$AdminPwSecure,
        [string]$Query
    )
    $plain = Plain $AdminPwSecure
    $hasSqlModule = $null -ne (Get-Module -ListAvailable -Name SqlServer -ErrorAction SilentlyContinue)

    if ($hasSqlModule) {
        Import-Module SqlServer -ErrorAction Stop
        Invoke-Sqlcmd -ServerInstance $SQL_SERVER_FQDN -Database $Database `
                      -Username $AdminUser -Password $plain `
                      -Query $Query -Encrypt Mandatory -TrustServerCertificate:$false `
                      -ErrorAction Stop
    } elseif ($null -ne (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        $tmpSql = New-TemporaryFile
        try {
            [System.IO.File]::WriteAllText($tmpSql.FullName, $Query, [System.Text.UTF8Encoding]::new($false))
            [System.Environment]::SetEnvironmentVariable("SQLCMDPASSWORD", $plain)
            $out = sqlcmd -S "tcp:$SQL_SERVER_FQDN,1433" -d $Database `
                          -U $AdminUser -i $tmpSql.FullName -b 2>&1
            if ($LASTEXITCODE -ne 0) { throw "sqlcmd Fehler (exit $LASTEXITCODE):`n$out" }
            Write-Host ($out -join "`n")
        } finally {
            [System.Environment]::SetEnvironmentVariable("SQLCMDPASSWORD", $null)
            if (Test-Path $tmpSql.FullName) {
                $len = (Get-Item $tmpSql.FullName -ErrorAction SilentlyContinue).Length
                if ($len -gt 0) {
                    [System.IO.File]::WriteAllBytes($tmpSql.FullName, [byte[]]::new($len))
                }
                Remove-Item $tmpSql.FullName -Force -ErrorAction SilentlyContinue
            }
        }
    } else {
        throw "Weder SqlServer-Modul noch sqlcmd CLI gefunden."
    }
}

function Wait-Health {
    param([string]$Url, [int]$MaxSeconds = 600)
    Write-Host "  Warte auf HTTP 200 von $Url (max. ${MaxSeconds}s)..."
    $deadline = (Get-Date).AddSeconds($MaxSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 15
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30 -SkipHttpErrorCheck
            Write-Host "    HTTP $($r.StatusCode)"
            if ($r.StatusCode -eq 200) { Ok "Gesund."; return }
        } catch {
            Warn "(Netzwerkfehler: $($_.Exception.Message))"
        }
    }
    throw "Health Check $Url nach ${MaxSeconds}s nicht erfolgreich."
}

$azConfigDir = Join-Path ([System.IO.Path]::GetTempPath()) "ra-rotate-$(Get-Random)"

# Welche Secrets erhalten ein -prev Backup?
$secretsToPrev = [System.Collections.Generic.List[string]]::new()

try {

Step "Phase 0: Az Login (isolierte Session)"
$env:AZURE_CONFIG_DIR = $azConfigDir
Invoke-AzCmd @("login", "--output", "none")
Invoke-AzCmd @("account", "set", "--subscription", $SUBSCRIPTION)
Ok "Eingeloggt auf Subscription $SUBSCRIPTION."

$hasSqlModule = $null -ne (Get-Module -ListAvailable -Name SqlServer -ErrorAction SilentlyContinue)
$hasSqlCmd    = $null -ne (Get-Command sqlcmd -ErrorAction SilentlyContinue)
if (-not $hasSqlModule -and -not $hasSqlCmd) {
    Write-Host "  SqlServer-Modul nicht gefunden — wird installiert..."
    Install-Module SqlServer -Scope CurrentUser -Force -AllowClobber
}

Step "Phase 1: Admin-Creds aus KV"
$ADMIN_USER = Read-KvSecret "sql-admin-user"
$ADMIN_PW_SECURE = (Read-KvSecret "sql-admin-password") |
    ConvertTo-SecureString -AsPlainText -Force
Ok "Admin-User: $ADMIN_USER"

Step "Phase 2: Neue Passwörter generieren"
$newPwDev    = if (-not $SkipDev)    { New-SqlPw | ConvertTo-SecureString -AsPlainText -Force } else { $null }
$newPwProd   = if (-not $SkipProd)   { New-SqlPw | ConvertTo-SecureString -AsPlainText -Force } else { $null }
$newPwBackup = if (-not $SkipBackup) { New-SqlPw | ConvertTo-SecureString -AsPlainText -Force } else { $null }
$count = @($newPwDev, $newPwProd, $newPwBackup) | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count
Ok "$count neue Passwörter generiert."

Step "Phase 3: Aktuelle Secrets als 'prev' sichern"

if (-not $SkipDev) {
    $secretsToPrev.Add("app-sql-dev-password")
    $secretsToPrev.Add("ConnectionStrings--umbracoDbDSN")
}
if (-not $SkipProd)   { $secretsToPrev.Add("app-sql-prod-password") }
if (-not $SkipBackup) { $secretsToPrev.Add("backup-sql-prod-password") }

foreach ($name in $secretsToPrev) {
    $current = Read-KvSecret $name
    Set-KvSecret "$name-prev" $current
    Ok "$name → ${name}-prev gesichert."
}

Step "Phase 4: SQL-Passwörter aktualisieren"
$myIp = (Invoke-RestMethod -Uri "https://api.ipify.org?format=text" -TimeoutSec 15).Trim()
Write-Host "  Lokale öffentliche IP: $myIp"

az sql server firewall-rule delete `
    --resource-group $RG --server $SQL_SERVER_NAME `
    --name $FW_RULE_NAME 2>$null
Invoke-AzCmd @("sql", "server", "firewall-rule", "create",
    "--resource-group", $RG, "--server", $SQL_SERVER_NAME,
    "--name", $FW_RULE_NAME,
    "--start-ip-address", $myIp, "--end-ip-address", $myIp) | Out-Null
Ok "Firewall-Regel '$FW_RULE_NAME' ($myIp) angelegt."

try {
    if (-not $SkipDev) {
        Write-Host "  ${DB_DEV}: redants_app ..."
        Invoke-SqlQuery -Database $DB_DEV -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query @"
ALTER USER [redants_app] WITH PASSWORD = '$(Plain $newPwDev)';
PRINT 'redants_app (DEV): Passwort aktualisiert.';
"@
        Ok "${DB_DEV}: redants_app rotiert."
    }

    if (-not $SkipProd) {
        Write-Host "  ${DB_PROD}: redants_app ..."
        Invoke-SqlQuery -Database $DB_PROD -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query @"
ALTER USER [redants_app] WITH PASSWORD = '$(Plain $newPwProd)';
PRINT 'redants_app (PROD): Passwort aktualisiert.';
"@
        Ok "${DB_PROD}: redants_app rotiert."
    }

    if (-not $SkipBackup) {
        Write-Host "  master: redants_backup ..."
        Invoke-SqlQuery -Database "master" -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query @"
ALTER LOGIN [redants_backup] WITH PASSWORD = '$(Plain $newPwBackup)';
PRINT 'redants_backup: Passwort aktualisiert.';
"@
        Ok "redants_backup rotiert."
    }
} finally {
    try {
        Invoke-AzCmd @("sql", "server", "firewall-rule", "delete",
            "--resource-group", $RG, "--server", $SQL_SERVER_NAME,
            "--name", $FW_RULE_NAME) | Out-Null
        Ok "Firewall-Regel '$FW_RULE_NAME' entfernt."
    } catch {
        Warn "Firewall-Regel konnte nicht automatisch entfernt werden."
        Warn "Manuell entfernen: az sql server firewall-rule delete --resource-group $RG --server $SQL_SERVER_NAME --name $FW_RULE_NAME"
    }
}

# Ab hier: SQL hat neue Passwörter. App Settings so schnell wie möglich
# nachziehen, damit das Zeitfenster mit alten App-Credentials minimal bleibt.
# Bestehende SQL-Verbindungen der laufenden App bleiben aktiv — nur neue
# Verbindungsversuche (z.B. nach Pool-Expiry) würden mit alten Credentials
# fehlschlagen. Der App-Neustart durch Settings-Update schliesst dieses Fenster.

Step "Phase 5: KV Secrets und App Settings aktualisieren"

if (-not $SkipDev) {
    $dsnDev = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$DB_DEV;" +
              "User ID=redants_app;Password=$(Plain $newPwDev);" +
              "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    Set-KvSecret "app-sql-dev-password" (Plain $newPwDev)
    Ok "KV: app-sql-dev-password aktualisiert."
    Set-KvSecret "ConnectionStrings--umbracoDbDSN" $dsnDev
    Ok "KV: ConnectionStrings--umbracoDbDSN (DEV, für lokales dotnet run) aktualisiert."
    Invoke-AzCmd @("webapp", "config", "appsettings", "set",
        "--resource-group", $RG, "--name", $APP_DEV,
        "--settings", "ConnectionStrings__umbracoDbDSN=$dsnDev",
        "--output", "none") -RedactOnError | Out-Null
    Ok "DEV App Setting gesetzt (App startet neu)."
}

if (-not $SkipProd) {
    $dsnProd = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$DB_PROD;" +
               "User ID=redants_app;Password=$(Plain $newPwProd);" +
               "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    Set-KvSecret "app-sql-prod-password" (Plain $newPwProd)
    Ok "KV: app-sql-prod-password aktualisiert."
    Invoke-AzCmd @("webapp", "config", "appsettings", "set",
        "--resource-group", $RG, "--name", $APP_PROD,
        "--settings", "ConnectionStrings__umbracoDbDSN=$dsnProd",
        "--output", "none") -RedactOnError | Out-Null
    Ok "PROD App Setting gesetzt (App startet neu)."
}

if (-not $SkipBackup) {
    Set-KvSecret "backup-sql-prod-password" (Plain $newPwBackup)
    Ok "KV: backup-sql-prod-password aktualisiert."
}

Step "Phase 6: Health Checks"
if (-not $SkipHealthChecks) {
    if (-not $SkipDev)  { Wait-Health -Url $HEALTH_DEV }
    if (-not $SkipProd) { Wait-Health -Url $HEALTH_PROD }
} else {
    Warn "Health Checks übersprungen (-SkipHealthChecks)."
}

Step "Phase 7: 'prev'-Secrets bereinigen"
foreach ($name in $secretsToPrev) {
    try {
        Invoke-AzCmd @("keyvault", "secret", "delete",
            "--vault-name", $KV, "--name", "$name-prev") | Out-Null
        Ok "${name}-prev gelöscht."
    } catch {
        Warn "${name}-prev konnte nicht gelöscht werden: $_"
    }
}

Step "FERTIG"
Write-Host "Alle Passwörter erfolgreich rotiert." -ForegroundColor Green

} catch {
    Write-Host "`nFEHLER: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Zeile $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim())" -ForegroundColor Red
    if ($secretsToPrev.Count -gt 0) {
        Write-Host "`nRollback-Anleitung:" -ForegroundColor Yellow
        Write-Host "  Die 'prev'-Secrets in KV enthalten die alten Passwörter:" -ForegroundColor Yellow
        foreach ($n in $secretsToPrev) { Write-Host "    kv-redants-prod: $n-prev" -ForegroundColor Yellow }
        Write-Host "  1. Altes Passwort aus KV lesen" -ForegroundColor Yellow
        Write-Host "  2. SQL: ALTER USER [redants_app] WITH PASSWORD = '...' (Admin-Creds aus sql-admin-password)" -ForegroundColor Yellow
        Write-Host "  3. App Settings manuell zurücksetzen (DSN mit altem Passwort)" -ForegroundColor Yellow
        Write-Host "  4. 'prev'-Secrets manuell löschen nach erfolgreichem Rollback" -ForegroundColor Yellow
    }
    throw
} finally {
    [System.Environment]::SetEnvironmentVariable("AZURE_CONFIG_DIR", $null)
    if ($azConfigDir -and (Test-Path $azConfigDir -ErrorAction SilentlyContinue)) {
        try { Remove-Item $azConfigDir -Recurse -Force }
        catch { Write-Host "  (Az-Config-Dir konnte nicht bereinigt werden: $azConfigDir)" -ForegroundColor DarkGray }
    }
}
