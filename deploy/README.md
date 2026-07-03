# Deployment (Azure)

RedAnts deploys to an Azure App Service in resource group **RG_RedAnts** via GitHub Actions
(`.github/workflows/deploy.yml`, Kudu ZipDeploy), backed by **Azure SQL** in production. This
mirrors the Sporthalle Sulzerallee setup.

## Database

- **Development:** SQLite (`appsettings.Development.json`, file under `umbraco/Data/`). Unchanged.
- **Production:** Azure SQL. `appsettings.json` sets `ConnectionStrings:umbracoDbDSN_ProviderName`
  to `Microsoft.Data.SqlClient` with an empty DSN; the real connection string is injected at
  runtime as an App Service app setting `ConnectionStrings__umbracoDbDSN`.

The SQL Server persistence provider ships transitively with `Umbraco.Cms` (no extra package).
`Program.cs` only runs its SQLite/WAL bootstrap when the provider is `Microsoft.Data.Sqlite`,
so production (SqlClient) skips it automatically.

## One-time provisioning

You must run this yourself: the Azure CLI in the dev environment is signed in to a different
tenant, and the SQL admin / backoffice passwords must be set by you.

```bash
az login --tenant redants.ch
bash deploy/azure-setup.sh
```

`azure-setup.sh` creates the App Service plan (Linux, .NET 10), the Web App, the Azure SQL
server + database, a firewall rule allowing Azure services, and the app settings (connection
string, provider name, unattended install). It prompts for the SQL admin password and the
Umbraco backoffice password (never written to disk) and prints the publish credentials.

## GitHub configuration

Repo → Settings → Secrets and variables → Actions:

| Kind     | Name                | Value                                             |
|----------|---------------------|---------------------------------------------------|
| Variable | `AZURE_WEBAPP_NAME` | the App Service name (e.g. `app-redants`)         |
| Secret   | `KUDU_USER`         | `publishingUserName` printed by the setup script  |
| Secret   | `KUDU_PASS`         | `publishingPassword` printed by the setup script  |

## Deploy

Push to `main` (or run the **Deploy to Azure** workflow manually). The workflow builds,
publishes, zips, ZipDeploys to Kudu, waits for completion, and warms up `/`.

On first deploy, Umbraco installs its schema into the empty Azure SQL database and creates the
backoffice user; the code-first seeders (`Infrastructure/**/…ContentTypeSeeder`) then create the
content types and sample content. After install you may remove the
`Umbraco__CMS__Unattended__Unattended*` app settings.

## Not included (optional follow-ups)

- **Media storage:** uploaded media currently lands on the App Service local disk, which is not
  durable across redeploys/scale-out. Sporthalle uses `Umbraco.StorageProviders.AzureBlob`
  (`AddAzureBlobMediaFileSystem()` guarded to non-Development in `Program.cs`) with a storage
  account. Say the word and I'll wire the same up for RedAnts.
- **Scale-out:** Azure SQL supports multiple App Service instances; keep a single plan instance
  only if you also keep media on local disk.
