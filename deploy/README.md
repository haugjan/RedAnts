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
server + database, a firewall rule allowing Azure services, a Storage account + `media` blob
container for Umbraco media, and the app settings (SQL connection string, provider name, blob
media connection string + container, unattended install). It prompts for the SQL admin password
and the Umbraco backoffice password (never written to disk) and prints the publish credentials.

## Media on Azure Blob Storage

Uploaded media is stored in Azure Blob Storage (durable across redeploys and scale-out) via
`Umbraco.StorageProviders.AzureBlob`. `Program.cs` calls `AddAzureBlobMediaFileSystem()` only
when `Umbraco:Storage:AzureBlob:Media:ConnectionString` is configured, so local development
(no blob config) keeps media on disk untouched. In Azure the connection string and container
name are injected as app settings `Umbraco__Storage__AzureBlob__Media__ConnectionString` and
`__ContainerName` (container `media`, public blob read access so media URLs resolve directly).

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

On first deploy the Azure SQL database is empty, so Umbraco serves its one-time installer. Open
`https://<app>.azurewebsites.net/umbraco`, complete the install (this creates your backoffice
admin account and writes the Umbraco schema), then reload the site. On the next boot the
code-first seeders (`Infrastructure/**/…ContentTypeSeeder`) create the content types and sample
content. Because the site is not installed until you finish that step, the workflow's warm-up is
best-effort and does not fail the deploy while `/` still returns the installer.

If you prefer a fully unattended install instead, set the `Umbraco__CMS__Unattended__Unattended*`
app settings (as `azure-setup.sh` does when run) so the admin user is created automatically on
first boot; remove them again afterwards.

## Scale-out

Azure SQL and blob media both support multiple App Service instances, so the plan can be scaled
out without losing media or session-independent state.
