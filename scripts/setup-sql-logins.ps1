#Requires -Version 7.0
<#
.SYNOPSIS
    RedAnts: Dedizierte SQL-Logins einführen (einmalige Durchführung).

.DESCRIPTION
    Führt das vollständige Runbook in einem Schritt durch:

    Phase 0  – Az CLI Login (isolierte Session, kein Konflikt mit laufenden Sessions)
    Phase 1  – Passwörter generieren (nur im RAM, nie auf Disk)
    Phase 2  – Key Vault Secrets anlegen
    Phase 3  – Admin-Creds aus KV lesen
    Phase 4  – Azure SQL Firewall temporär öffnen, Contained Users anlegen, wieder schliessen
    Phase 5  – DEV Connection String umstellen, Health Check
    Phase 6  – PROD Connection String umstellen, Health Check
    Phase 7  – Branch pushen, Backup Workflow auslösen und auf Ergebnis warten
    Phase 8  – Merge nach main, Worktree und Branch aufräumen
    Phase 9  – Server-Admin Passwort rotieren

    Sicherheitshinweise:
    - Admin-Passwort: via SQLCMDPASSWORD Env-Var (nie als CLI-Argument), wird via
      [System.Environment]::SetEnvironmentVariable(..., $null) vollständig entfernt.
    - User-Passwörter (CREATE USER ... WITH PASSWORD): landen kurzfristig in einer
      Temp-SQL-Datei; diese wird nach Verwendung mit Nullbytes überschrieben und
      gelöscht — auch bei Fehler (finally-Block).
    - KV-Secrets / App-Settings / Admin-PW-Rotation: Passwörter als CLI-Argumente
      (az). Az-Fehlerausgabe wird bei sensitiven Aufrufen maskiert (-RedactOnError).
    - Azure CLI Tokens: isolierter Temp-Dir, wird am Skript-Ende (auch bei Fehler)
      vollständig gelöscht.
    - .NET-Strings sind nicht sicher löschbar; SecureString minimiert die Lebenszeit
      von Klartext im Prozessspeicher.

.PARAMETER SkipSqlUsers
    SQL-User-Erstellung überspringen (bereits manuell angelegt).

.PARAMETER SkipHealthChecks
    Health Checks nach Connection-String-Umstellung überspringen.

.PARAMETER SkipBackupTest
    Backup Workflow Test überspringen. Branch wird trotzdem gepusht (Phase 8 braucht ihn).

.PARAMETER SkipMerge
    Merge nach main überspringen.

.PARAMETER SkipAdminRotation
    Admin-Passwort-Rotation überspringen.

.NOTES
    Voraussetzungen:
    - PowerShell 7+
    - Azure CLI (az), eingeloggt mit ausreichenden Rechten auf RG_RedAnts
    - GitHub CLI (gh), eingeloggt: gh auth login
    - Git
    - sqlcmd ODER SqlServer PS-Modul: Install-Module SqlServer -Scope CurrentUser

    ACHTUNG nach Phase 9: Das Admin-Passwort ist rotiert. Ein erneuter Lauf ohne
    -SkipAdminRotation generiert ein neues Passwort und überschreibt KV + App-Settings.
#>

param(
    [switch]$SkipSqlUsers,
    [switch]$SkipHealthChecks,
    [switch]$SkipBackupTest,
    [switch]$SkipMerge,
    [switch]$SkipAdminRotation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Konstanten ─────────────────────────────────────────────────────────────
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
$BRANCH          = "ops/sql-dedicated-logins"
$WORKTREE        = "C:\development\RedAnts-s6"
$MAIN_REPO       = "C:\development\RedAnts"
$FW_RULE_NAME    = "temp-setup-sql-logins"

# ── Hilfsfunktionen ────────────────────────────────────────────────────────
function New-SqlPw {
    # 32 alphanumerische Zeichen (Base64 von 24 Zufallsbytes, Sonderzeichen durch x).
    # Azure SQL Password Policy: mind. 8 Zeichen, mind. 3 von 4 Zeichenklassen
    # (Grossbuchstaben + Kleinbuchstaben + Ziffern = 3 Klassen => erfüllt).
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
                      -Query $Query -Encrypt Mandatory -TrustServerCertificate $false `
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
        throw @"
Weder SqlServer-Modul noch sqlcmd CLI gefunden.
  SqlServer-Modul: Install-Module SqlServer -Scope CurrentUser
  sqlcmd CLI:      https://learn.microsoft.com/sql/tools/sqlcmd-utility
"@
    }
}

function Assert-SqlToolAvailable {
    $hasSqlModule = $null -ne (Get-Module -ListAvailable -Name SqlServer -ErrorAction SilentlyContinue)
    $hasSqlCmd    = $null -ne (Get-Command sqlcmd -ErrorAction SilentlyContinue)
    if (-not $hasSqlModule -and -not $hasSqlCmd) {
        throw @"
Weder SqlServer-Modul noch sqlcmd CLI gefunden.
  SqlServer-Modul: Install-Module SqlServer -Scope CurrentUser
  sqlcmd CLI:      https://learn.microsoft.com/sql/tools/sqlcmd-utility
"@
    }
    if ($hasSqlModule) { Ok "SqlServer-Modul verfügbar." } else { Ok "sqlcmd CLI verfügbar." }
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

# Gibt idempotentes T-SQL zurück, das die Rollenzuweisung nur macht wenn nötig
function Get-RoleGrantSql([string]$Role, [string]$User) {
    @"
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = '$Role' AND m.name = '$User'
)
    ALTER ROLE [$Role] ADD MEMBER [$User];
"@
}

# ── Globaler Cleanup-Scope (Az Config-Dir) ─────────────────────────────────
$azConfigDir = Join-Path $env:TEMP "ra-sql-setup-$(Get-Random)"

try {

# ── Phase 0: Az Login ──────────────────────────────────────────────────────
Step "Phase 0: Az Login (isolierte Session)"
$env:AZURE_CONFIG_DIR = $azConfigDir
Invoke-AzCmd @("login", "--output", "none")
Invoke-AzCmd @("account", "set", "--subscription", $SUBSCRIPTION)
Ok "Eingeloggt auf Subscription $SUBSCRIPTION."

# ── Frühe Voraussetzungsprüfungen ──────────────────────────────────────────
Step "Voraussetzungen prüfen"
if (-not $SkipSqlUsers) { Assert-SqlToolAvailable }

gh auth status --hostname github.com 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI nicht authentifiziert. Bitte 'gh auth login' ausführen." }
Ok "gh CLI authentifiziert."

# ── Phase 1: Passwörter generieren ────────────────────────────────────────
Step "Phase 1: Passwörter generieren (nur RAM)"
$PW_APP_PROD  = New-SqlPw | ConvertTo-SecureString -AsPlainText -Force
$PW_APP_DEV   = New-SqlPw | ConvertTo-SecureString -AsPlainText -Force
$PW_BACKUP    = New-SqlPw | ConvertTo-SecureString -AsPlainText -Force
$PW_NEW_ADMIN = New-SqlPw | ConvertTo-SecureString -AsPlainText -Force
Ok "4 Passwörter als SecureString generiert (je 32 Zeichen)."

# ── Phase 2: Key Vault Secrets setzen ────────────────────────────────────
Step "Phase 2: KV Secrets setzen ($KV)"
$kvEntries = @(
    @{ Name = "app-sql-prod-user";        Value = "redants_app" }
    @{ Name = "app-sql-prod-password";    Value = Plain $PW_APP_PROD }
    @{ Name = "app-sql-dev-user";         Value = "redants_app" }
    @{ Name = "app-sql-dev-password";     Value = Plain $PW_APP_DEV }
    @{ Name = "backup-sql-prod-user";     Value = "redants_backup" }
    @{ Name = "backup-sql-prod-password"; Value = Plain $PW_BACKUP }
)
foreach ($entry in $kvEntries) {
    Invoke-AzCmd @("keyvault", "secret", "set", "--vault-name", $KV,
                   "--name", $entry.Name, "--value", $entry.Value) | Out-Null
    Ok $entry.Name
}

# ── Phase 3: Admin-Creds aus KV lesen ─────────────────────────────────────
Step "Phase 3: Admin-Creds aus KV"
$ADMIN_USER = (Invoke-AzCmd @("keyvault", "secret", "show",
    "--vault-name", $KV, "--name", "sql-admin-user",
    "--query", "value", "-o", "tsv")).Trim()
$ADMIN_PW_SECURE = (Invoke-AzCmd @("keyvault", "secret", "show",
    "--vault-name", $KV, "--name", "sql-admin-password",
    "--query", "value", "-o", "tsv")).Trim() |
    ConvertTo-SecureString -AsPlainText -Force
Ok "Admin-User: $ADMIN_USER"

# ── Phase 4: SQL Contained Users anlegen ─────────────────────────────────
if (-not $SkipSqlUsers) {
    Step "Phase 4a: Temporäre Firewall-Regel anlegen"
    $myIp = (Invoke-RestMethod -Uri "https://api.ipify.org?format=text" -TimeoutSec 15).Trim()
    Write-Host "  Lokale öffentliche IP: $myIp"

    # Upsert: löschen falls vorhanden, dann neu erstellen (idempotent bei Neustart)
    az sql server firewall-rule delete `
        --resource-group $RG --server $SQL_SERVER_NAME `
        --name $FW_RULE_NAME --yes 2>$null
    Invoke-AzCmd @("sql", "server", "firewall-rule", "create",
        "--resource-group", $RG, "--server", $SQL_SERVER_NAME,
        "--name", $FW_RULE_NAME,
        "--start-ip-address", $myIp, "--end-ip-address", $myIp) | Out-Null
    Ok "Firewall-Regel '$FW_RULE_NAME' ($myIp) angelegt."

    try {
        Step "Phase 4b: SQL Users anlegen"

        $pwAppProd = Plain $PW_APP_PROD
        $pwAppDev  = Plain $PW_APP_DEV
        $pwBackup  = Plain $PW_BACKUP

        $sqlAppProd = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'redants_app' AND type_desc = 'SQL_USER')
BEGIN
    CREATE USER [redants_app] WITH PASSWORD = '$pwAppProd';
    PRINT 'redants_app: erstellt.';
END
ELSE BEGIN
    ALTER USER [redants_app] WITH PASSWORD = '$pwAppProd';
    PRINT 'redants_app: Passwort aktualisiert.';
END
$(Get-RoleGrantSql 'db_datareader' 'redants_app')
$(Get-RoleGrantSql 'db_datawriter' 'redants_app')
$(Get-RoleGrantSql 'db_ddladmin'   'redants_app')
GRANT EXECUTE ON SCHEMA::dbo TO [redants_app];
PRINT 'redants_app: Rollen und EXECUTE auf dbo gesetzt.';
"@

        $sqlAppDev = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'redants_app' AND type_desc = 'SQL_USER')
BEGIN
    CREATE USER [redants_app] WITH PASSWORD = '$pwAppDev';
    PRINT 'redants_app: erstellt.';
END
ELSE BEGIN
    ALTER USER [redants_app] WITH PASSWORD = '$pwAppDev';
    PRINT 'redants_app: Passwort aktualisiert.';
END
$(Get-RoleGrantSql 'db_datareader' 'redants_app')
$(Get-RoleGrantSql 'db_datawriter' 'redants_app')
$(Get-RoleGrantSql 'db_ddladmin'   'redants_app')
GRANT EXECUTE ON SCHEMA::dbo TO [redants_app];
PRINT 'redants_app: Rollen und EXECUTE auf dbo gesetzt.';
"@

        $sqlBackup = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'redants_backup' AND type_desc = 'SQL_USER')
BEGIN
    CREATE USER [redants_backup] WITH PASSWORD = '$pwBackup';
    PRINT 'redants_backup: erstellt.';
END
ELSE BEGIN
    ALTER USER [redants_backup] WITH PASSWORD = '$pwBackup';
    PRINT 'redants_backup: Passwort aktualisiert.';
END
$(Get-RoleGrantSql 'db_datareader' 'redants_backup')
GRANT VIEW DEFINITION     TO [redants_backup];
GRANT VIEW DATABASE STATE TO [redants_backup];
PRINT 'redants_backup: Rollen gesetzt.';
"@

        Write-Host "  $DB_DEV : redants_app ..."
        Invoke-SqlQuery -Database $DB_DEV -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query $sqlAppDev

        Write-Host "  $DB_PROD: redants_app ..."
        Invoke-SqlQuery -Database $DB_PROD -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query $sqlAppProd

        Write-Host "  $DB_PROD: redants_backup ..."
        Invoke-SqlQuery -Database $DB_PROD -AdminUser $ADMIN_USER `
                        -AdminPwSecure $ADMIN_PW_SECURE -Query $sqlBackup

        Ok "SQL Users angelegt."

    } finally {
        Step "Phase 4c: Firewall-Regel entfernen"
        try {
            Invoke-AzCmd @("sql", "server", "firewall-rule", "delete",
                "--resource-group", $RG, "--server", $SQL_SERVER_NAME,
                "--name", $FW_RULE_NAME, "--yes") | Out-Null
            Ok "Firewall-Regel '$FW_RULE_NAME' entfernt."
        } catch {
            Warn "Firewall-Regel konnte nicht automatisch entfernt werden: $_"
            Warn "Bitte manuell entfernen:"
            Warn "  az sql server firewall-rule delete --resource-group $RG --server $SQL_SERVER_NAME --name $FW_RULE_NAME --yes"
        }
    }
}

# ── Phase 5: DEV Connection String + Health Check ─────────────────────────
Step "Phase 5: DEV Connection String umstellen"
$dsnDev = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$DB_DEV;" +
          "User ID=redants_app;Password=$(Plain $PW_APP_DEV);" +
          "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Invoke-AzCmd @("webapp", "config", "appsettings", "set",
    "--resource-group", $RG, "--name", $APP_DEV,
    "--settings", "ConnectionStrings__umbracoDbDSN=$dsnDev",
    "--output", "none") -RedactOnError | Out-Null
Ok "DEV App Setting gesetzt (App startet neu)."

if (-not $SkipHealthChecks) { Wait-Health -Url $HEALTH_DEV }

# ── Phase 6: PROD Connection String + Health Check ────────────────────────
Step "Phase 6: PROD Connection String umstellen"
$dsnProd = "Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=$DB_PROD;" +
           "User ID=redants_app;Password=$(Plain $PW_APP_PROD);" +
           "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Invoke-AzCmd @("webapp", "config", "appsettings", "set",
    "--resource-group", $RG, "--name", $APP_PROD,
    "--settings", "ConnectionStrings__umbracoDbDSN=$dsnProd",
    "--output", "none") -RedactOnError | Out-Null
Ok "PROD App Setting gesetzt (App startet neu)."

if (-not $SkipHealthChecks) { Wait-Health -Url $HEALTH_PROD }

# ── Phase 7: Branch pushen (immer) + Backup Workflow Test ─────────────────
Step "Phase 7: Branch pushen"
git -C $WORKTREE push -u origin $BRANCH
if ($LASTEXITCODE -ne 0) { throw "git push fehlgeschlagen." }
Ok "Branch '$BRANCH' gepusht."

if (-not $SkipBackupTest) {
    $triggerTime = [System.DateTimeOffset]::UtcNow
    gh workflow run backup.yml --repo haugjan/RedAnts --ref $BRANCH
    if ($LASTEXITCODE -ne 0) { throw "gh workflow run fehlgeschlagen." }
    Ok "Backup Workflow ausgelöst."

    Write-Host "  Warte 20s auf GitHub-seitige Run-Erstellung..."
    Start-Sleep -Seconds 20

    # Run anhand des Trigger-Zeitstempels identifizieren (kein Race mit anderen Runs)
    $ts = $triggerTime.ToString('yyyy-MM-ddTHH:mm:ss') + 'Z'
    $runId = gh run list --repo haugjan/RedAnts --workflow backup.yml `
                 --limit 5 --json databaseId,createdAt `
                 -q "[.[] | select(.createdAt >= `"$ts`")] | first | .databaseId"
    if ($LASTEXITCODE -ne 0 -or -not $runId) {
        throw "Konnte Run-ID nicht ermitteln. Manuell prüfen: gh run list --repo haugjan/RedAnts --workflow backup.yml"
    }
    Write-Host "  Run ID: $runId — warte auf Abschluss..."

    gh run watch $runId --repo haugjan/RedAnts --exit-status
    if ($LASTEXITCODE -ne 0) {
        throw "Backup Workflow fehlgeschlagen (Run $runId). Merge abgebrochen. Details: gh run view $runId --log"
    }
    Ok "Backup Workflow erfolgreich abgeschlossen."
}

# ── Phase 8: Merge nach main + Cleanup ────────────────────────────────────
if (-not $SkipMerge) {
    Step "Phase 8: Merge nach main"
    $commitHash = git -C $WORKTREE rev-parse HEAD
    git -C $MAIN_REPO fetch origin main
    git -C $MAIN_REPO checkout main
    git -C $MAIN_REPO merge --ff-only $commitHash
    if ($LASTEXITCODE -ne 0) {
        throw "git merge --ff-only fehlgeschlagen — remote main hat neue Commits. Bitte manuell mergen."
    }
    git -C $MAIN_REPO push origin main
    if ($LASTEXITCODE -ne 0) { throw "git push origin main fehlgeschlagen." }
    Ok "Auf main gepusht (CI deployed DEV + PROD)."

    git -C $MAIN_REPO worktree remove $WORKTREE --force
    git -C $MAIN_REPO branch -d $BRANCH
    git -C $MAIN_REPO push origin --delete $BRANCH
    Ok "Worktree + Branch aufgeräumt."
}

# ── Phase 9: Admin-Passwort rotieren ──────────────────────────────────────
if (-not $SkipAdminRotation) {
    Step "Phase 9: Server-Admin Passwort rotieren (Break-Glass)"
    Invoke-AzCmd @("sql", "server", "update",
        "--resource-group", $RG, "--name", $SQL_SERVER_NAME,
        "--admin-password", (Plain $PW_NEW_ADMIN)) -RedactOnError | Out-Null
    Invoke-AzCmd @("keyvault", "secret", "set",
        "--vault-name", $KV, "--name", "sql-admin-password",
        "--value", (Plain $PW_NEW_ADMIN)) | Out-Null
    Ok "Admin-Passwort rotiert und nur in KV gespeichert."
    Warn "Das alte sql-admin-password ist ungültig. Neues steht nur in kv-redants-prod."
    Warn "Bei erneutem Lauf alle -Skip*-Flags prüfen — neue Passwörter würden sonst generiert."
}

Step "FERTIG"
Write-Host "Alle Phasen erfolgreich durchgefuehrt." -ForegroundColor Green

} finally {
    if (Test-Path $azConfigDir) {
        Remove-Item $azConfigDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "`n  (Az-Config-Dir mit Tokens bereinigt.)"
    }
    [System.Environment]::SetEnvironmentVariable("AZURE_CONFIG_DIR", $null)
}
