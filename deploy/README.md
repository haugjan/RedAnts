# Deployment (Azure)

RedAnts deploys to an Azure App Service in resource group **RG_RedAnts** via GitHub Actions
(`.github/workflows/deploy.yml`), backed by **Azure SQL** in production. Deployment authenticates
with **Microsoft Entra / OIDC** (workload identity federation): no basic auth, no long-lived
publish password. SCM basic-auth publishing stays disabled on the app.

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
media connection string + container). It also sets up the deployment identity: it disables SCM
basic auth, creates the `redants-github-deploy` Entra app registration with a federated
credential for `repo:haugjan/RedAnts:ref:refs/heads/main`, grants it the **Website Contributor**
role on the app, and prints the three OIDC ids for the GitHub secrets below. It prompts only for
the SQL admin password (never written to disk).

## Media on Azure Blob Storage

Uploaded media is stored in Azure Blob Storage (durable across redeploys and scale-out) via
`Umbraco.StorageProviders.AzureBlob`. `Program.cs` calls `AddAzureBlobMediaFileSystem()` only
when `Umbraco:Storage:AzureBlob:Media:ConnectionString` is configured, so local development
(no blob config) keeps media on disk untouched. In Azure the connection string and container
name are injected as app settings `Umbraco__Storage__AzureBlob__Media__ConnectionString` and
`__ContainerName` (container `media`, public blob read access so media URLs resolve directly).

## GitHub configuration

Repo → Settings → Secrets and variables → Actions:

| Kind     | Name                    | Value                                                    |
|----------|-------------------------|----------------------------------------------------------|
| Variable | `AZURE_WEBAPP_NAME`     | the App Service name (e.g. `app-redants`)                |
| Secret   | `AZURE_CLIENT_ID`       | appId of the `redants-github-deploy` app registration    |
| Secret   | `AZURE_TENANT_ID`       | Entra tenant id                                          |
| Secret   | `AZURE_SUBSCRIPTION_ID` | Azure subscription id                                    |

No secret holds a password: the runner requests a short-lived OIDC token that `azure/login`
exchanges via the federated credential. To rotate trust, edit the app registration's federated
credential (issuer `token.actions.githubusercontent.com`, subject
`repo:haugjan/RedAnts:ref:refs/heads/main`).

## Deploy

Push to `main` (or run the **Deploy to Azure** workflow manually). The workflow builds,
publishes, zips, logs in to Azure via OIDC, deploys the zip with `az webapp deploy` over ARM,
and warms up `/`.

On first deploy the Azure SQL database is empty, so Umbraco serves its one-time installer. Open
`https://<app>.azurewebsites.net/umbraco`, complete the install (this creates your backoffice
admin account and writes the Umbraco schema), then reload the site. On the next boot the
code-first seeders (`Infrastructure/**/…ContentTypeSeeder`) create the content types and sample
content. Because the site is not installed until you finish that step, the workflow's warm-up is
best-effort and does not fail the deploy while `/` still returns the installer.

If you prefer a fully unattended install instead, set the `Umbraco__CMS__Unattended__Unattended*`
app settings yourself (user name, email, password) so the admin user is created automatically on
first boot; remove them again afterwards.

## Scale-out

Azure SQL and blob media both support multiple App Service instances, so the plan can be scaled
out without losing media or session-independent state.

## Backups

Databases (server `sql-redants-ch`):
- Automatic point-in-time restore (PITR): `sqldb-redants-prod` retains 14 days, `sqldb-redants-dev`
  retains 7 days (Basic tier maximum).
- Long-term retention (LTR): prod keeps a weekly backup for 4 weeks and a monthly backup for
  12 months; dev keeps a weekly backup for 2 weeks.
- Restore from PITR: `az sql db restore`; list/restore LTR backups: `az sql db ltr-backup list` /
  `az sql db ltr-backup restore`.

Media (storage accounts `stredantsprod`, `stredantsdev`, `stredants`):
- Blob soft delete and container soft delete both retain 14 days; blob versioning and change feed
  are enabled. Deleted or overwritten media can be restored within the retention window
  (`az storage blob undelete` / restore a prior version).
