#Requires -Version 7.0
#Requires -Modules SqlServer
[CmdletBinding()]
param(
    [ValidateSet('dev', 'prod')]
    [string]$Environment = 'dev',

    [string[]]$Databases,

    [switch]$SetEntraAdmin,

    [switch]$SkipStorage
)

$ErrorActionPreference = 'Stop'

$subscription = 'fdf0cdfa-61ef-409f-aa8b-bb0c6a306e3b'
$tenant = '64a8811c-a541-4b97-9571-5a8d280bd40b'
$resourceGroup = 'RG_RedAnts'
$sqlServer = 'sql-redants-ch'
$app = "app-redants-$Environment"
$storageAccount = "stredants$Environment"
$deployPrincipalName = 'redants-github-deploy'
$adminObjectId = '49e498c4-d43f-475a-8301-38b1c6057cc4'

if (-not $Databases) {
    $Databases = if ($Environment -eq 'dev') { @('sqldb-redants-dev', 'sqldb-redants-agent') } else { @('sqldb-redants-prod') }
}

function Invoke-Az {
    param([string[]]$Arguments)
    $output = & az @Arguments --subscription $subscription 2>&1
    if ($LASTEXITCODE -ne 0) { throw "az $($Arguments[0]) $($Arguments[1]) failed: $output" }
    return $output
}

Write-Host "== Managed identity on $app"
$appIdentity = (Invoke-Az @('webapp', 'identity', 'assign', '--resource-group', $resourceGroup, '--name', $app, '--query', 'principalId', '-o', 'tsv')).Trim()
Write-Host "   principalId $appIdentity"

if ($SetEntraAdmin) {
    Write-Host "== Entra administrator on $sqlServer"
    $graphToken = (& az account get-access-token --tenant $tenant --resource https://graph.microsoft.com --query accessToken -o tsv).Trim()
    $adminUser = Invoke-RestMethod -Headers @{ Authorization = "Bearer $graphToken" } -Uri "https://graph.microsoft.com/v1.0/users/$adminObjectId`?`$select=userPrincipalName"
    $adminUrl = "https://management.azure.com/subscriptions/$subscription/resourceGroups/$resourceGroup/providers/Microsoft.Sql/servers/$sqlServer/administrators/ActiveDirectory?api-version=2021-11-01"
    $adminBody = @{ properties = @{ administratorType = 'ActiveDirectory'; login = $adminUser.userPrincipalName; sid = $adminObjectId; tenantId = $tenant } } | ConvertTo-Json -Compress
    Invoke-Az @('rest', '--method', 'put', '--url', $adminUrl, '--body', $adminBody, '-o', 'none') | Out-Null
    Write-Host "   set to object $adminObjectId in tenant $tenant (az sql server ad-admin create would pick the CLI default tenant)"
}

$sequences = @('OrderNumberSeq', 'RefundNumberSeq', 'JournalSeq')
$sequenceGrants = ($sequences | ForEach-Object {
        "IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'$_') GRANT UPDATE ON OBJECT::dbo.[$_] TO [$app];"
    }) -join "`n"

$databaseScript = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$app')
    CREATE USER [$app] FROM EXTERNAL PROVIDER;
IF IS_ROLEMEMBER('db_datareader', '$app') = 0 ALTER ROLE db_datareader ADD MEMBER [$app];
IF IS_ROLEMEMBER('db_datawriter', '$app') = 0 ALTER ROLE db_datawriter ADD MEMBER [$app];
$sequenceGrants

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$deployPrincipalName')
    CREATE USER [$deployPrincipalName] FROM EXTERNAL PROVIDER;
IF IS_ROLEMEMBER('db_datareader', '$deployPrincipalName') = 0 ALTER ROLE db_datareader ADD MEMBER [$deployPrincipalName];
IF IS_ROLEMEMBER('db_datawriter', '$deployPrincipalName') = 0 ALTER ROLE db_datawriter ADD MEMBER [$deployPrincipalName];
IF IS_ROLEMEMBER('db_ddladmin', '$deployPrincipalName') = 0 ALTER ROLE db_ddladmin ADD MEMBER [$deployPrincipalName];

SELECT dp.name, dp.type_desc,
       STRING_AGG(r.name, ', ') AS roles
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members rm ON rm.member_principal_id = dp.principal_id
LEFT JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
WHERE dp.name IN (N'$app', N'$deployPrincipalName')
GROUP BY dp.name, dp.type_desc;
"@

$sqlToken = (& az account get-access-token --subscription $subscription --resource 'https://database.windows.net/' --query accessToken -o tsv).Trim()
foreach ($database in $Databases) {
    Write-Host "== Database users in $database"
    $rows = Invoke-Sqlcmd -ServerInstance "$sqlServer.database.windows.net" -Database $database -AccessToken $sqlToken -Query $databaseScript
    $rows | Format-Table -AutoSize | Out-String | Write-Host
}

if (-not $SkipStorage) {
    Write-Host "== Storage Blob Data Contributor on $storageAccount"
    $storageId = (Invoke-Az @('storage', 'account', 'show', '--resource-group', $resourceGroup, '--name', $storageAccount, '--query', 'id', '-o', 'tsv')).Trim()
    foreach ($assignee in @(@{ Id = $appIdentity; Type = 'ServicePrincipal' }, @{ Id = $adminObjectId; Type = 'User' })) {
        Invoke-Az @('role', 'assignment', 'create', '--assignee-object-id', $assignee.Id, '--assignee-principal-type', $assignee.Type, '--role', 'Storage Blob Data Contributor', '--scope', $storageId, '-o', 'none') | Out-Null
        Write-Host "   $($assignee.Type) $($assignee.Id)"
    }
}

Write-Host "Done. Next: set the app settings (passwordless DSN, AccountUrl keys) and the GitHub variable SQL_DSN for environment '$Environment'."
