#!/usr/bin/env bash
#
# One-time Azure provisioning for RedAnts, mirroring the Sporthalle Sulzerallee setup:
# an App Service (Linux, .NET 10) plus an Azure SQL database, in resource group RG_RedAnts.
#
# NOTE: Resources run in UK West (UK data residency, geographically Europe). This subscription's
# compute quota is heavily restricted: Switzerland North refuses Basic/Standard Linux (only pricey
# Premium v3), France Central had no capacity, West Europe is not accepting new customers, and
# North Europe / UK South have 0 VM quota. UK West was the one region that allowed cheap Linux B1.
# The resource group itself stays in Switzerland North (location is only metadata).
#
# Run this yourself after logging in to the Red Ants tenant:
#     az login --tenant redants.ch
#     bash deploy/azure-setup.sh
#
# It prompts only for the SQL admin password (never written to any file). Deployment auth uses
# Entra OIDC (no basic auth / no publish password): the script disables SCM basic auth and creates
# an app registration with a federated credential for the GitHub repo. Umbraco itself is installed
# by completing the browser installer at /umbraco on first visit. Names marked "globally unique"
# may need adjusting if already taken.
#
set -euo pipefail

# ── Configuration (adjust as needed) ─────────────────────────────────────────
SUBSCRIPTION="fdf0cdfa-61ef-409f-aa8b-bb0c6a306e3b"
RESOURCE_GROUP="RG_RedAnts"
LOCATION="ukwest"            # App Service region (cheap Linux B1 works here)
SQL_LOCATION="francecentral" # Azure SQL region — UK West/North Europe/West Europe refuse new SQL
                             # servers for this subscription; France Central accepts them.
                             # Cross-region app->DB within Europe is fine.

APP_PLAN="asp-redants"
APP_NAME="app-redants"          # globally unique -> https://<APP_NAME>.azurewebsites.net
PLAN_SKU="B1"                   # B1 = Basic (Linux); cheap. Available in UK West.

SQL_SERVER="sql-redants-fc"     # globally unique -> <SQL_SERVER>.database.windows.net
                                # (-fc: the plain 'sql-redants' name got locked to UK West by a
                                #  failed create; a fresh name avoids the region-lock conflict)
SQL_DB="sqldb-redants"
SQL_ADMIN="redantsadmin"
SQL_SKU="S0"                    # S0 = 10 DTU; adjust to taste (Basic/S1/GP_S_Gen5_1 ...)

STORAGE_ACCOUNT="stredants"     # globally unique, 3-24 lowercase alphanumerics. Holds Umbraco media.
MEDIA_CONTAINER="media"         # blob container Umbraco writes media into

GH_REPO="haugjan/RedAnts"       # GitHub repo the deploy workflow runs in
APP_REG_NAME="redants-github-deploy"  # Entra app registration used for OIDC deploy auth
# ─────────────────────────────────────────────────────────────────────────────

# The SQL admin password comes from the env var if set, otherwise an interactive prompt.
# Interactive form:   bash deploy/azure-setup.sh
# Non-interactive:    SQL_PASS='...' bash deploy/azure-setup.sh
: "${SQL_PASS:=}"
if [ -z "$SQL_PASS" ]; then read -rs -p "SQL admin password for '$SQL_ADMIN': " SQL_PASS || true; echo; fi
if [ -z "$SQL_PASS" ]; then
  echo "ERROR: SQL admin password not provided. Run this in an interactive terminal, or pass it" >&2
  echo "       as an environment variable: SQL_PASS='...' bash deploy/azure-setup.sh" >&2
  exit 1
fi

echo "==> Selecting subscription"
az account set --subscription "$SUBSCRIPTION"

echo "==> App Service plan ($APP_PLAN, Linux, $PLAN_SKU)"
az appservice plan create \
  --resource-group "$RESOURCE_GROUP" --name "$APP_PLAN" \
  --location "$LOCATION" --is-linux --sku "$PLAN_SKU"

echo "==> Web App ($APP_NAME, .NET 10 on Linux)"
az webapp create \
  --resource-group "$RESOURCE_GROUP" --plan "$APP_PLAN" --name "$APP_NAME" \
  --runtime "DOTNETCORE:10.0"

echo "==> Keep the app warm (Always On) so it does not cold-unload and serve the platform page"
az webapp config set \
  --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --always-on true --output none

echo "==> Azure SQL server ($SQL_SERVER in $SQL_LOCATION)"
az sql server create \
  --resource-group "$RESOURCE_GROUP" --name "$SQL_SERVER" \
  --location "$SQL_LOCATION" --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASS"

echo "==> Allow Azure services to reach the SQL server"
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

echo "==> Azure SQL database ($SQL_DB, $SQL_SKU)"
az sql db create \
  --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name "$SQL_DB" --service-objective "$SQL_SKU"

CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASS};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

echo "==> Storage account ($STORAGE_ACCOUNT in $LOCATION, for Umbraco media)"
az storage account create \
  --resource-group "$RESOURCE_GROUP" --name "$STORAGE_ACCOUNT" \
  --location "$LOCATION" --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access true --min-tls-version TLS1_2

echo "==> Media blob container ($MEDIA_CONTAINER, public blob read for media URLs)"
STORAGE_CONN="$(az storage account show-connection-string \
  --resource-group "$RESOURCE_GROUP" --name "$STORAGE_ACCOUNT" \
  --query connectionString -o tsv)"
az storage container create \
  --name "$MEDIA_CONTAINER" --public-access blob \
  --connection-string "$STORAGE_CONN" --output none

echo "==> App settings: connection string + provider + blob media"
# Set the DSN as a plain app setting (ConnectionStrings__...) so ASP.NET Core does NOT
# auto-inject the legacy System.Data.SqlClient provider that the SQLAzure type would add.
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" \
  --settings \
    "ConnectionStrings__umbracoDbDSN=${CONN}" \
    "ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient" \
    "Umbraco__Storage__AzureBlob__Media__ConnectionString=${STORAGE_CONN}" \
    "Umbraco__Storage__AzureBlob__Media__ContainerName=${MEDIA_CONTAINER}" \
  --output none

echo "==> Deployment identity (Entra OIDC): disable SCM basic auth"
# Modern Azure default already disables this; keep it explicit so no basic-auth publish path exists.
az resource update \
  --resource-group "$RESOURCE_GROUP" --namespace Microsoft.Web \
  --parent "sites/${APP_NAME}" --resource-type basicPublishingCredentialsPolicies \
  --name scm --set properties.allow=false --output none

echo "==> App registration + service principal ($APP_REG_NAME)"
APP_ID=$(az ad app create --display-name "$APP_REG_NAME" --query appId -o tsv)
az ad sp create --id "$APP_ID" --query id -o tsv >/dev/null || true
SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

echo "==> Federated credential (trust GitHub Actions on refs/heads/main)"
az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\":\"github-redants-main\",
  \"issuer\":\"https://token.actions.githubusercontent.com\",
  \"subject\":\"repo:${GH_REPO}:ref:refs/heads/main\",
  \"audiences\":[\"api://AzureADTokenExchange\"]
}" --output none

echo "==> Role assignment: Website Contributor on the app"
APP_SCOPE=$(az webapp show -g "$RESOURCE_GROUP" -n "$APP_NAME" --query id -o tsv)
# de139f84-... = Website Contributor. (az role assignment can fail with a spurious
# MissingSubscription in some CLI/tenant combos; the ARM PUT via az rest is the reliable path.)
RA_GUID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || python -c "import uuid;print(uuid.uuid4())")
az rest --method put \
  --url "https://management.azure.com${APP_SCOPE}/providers/Microsoft.Authorization/roleAssignments/${RA_GUID}?api-version=2022-04-01" \
  --headers "Content-Type=application/json" \
  --body "{\"properties\":{\"roleDefinitionId\":\"/subscriptions/${SUBSCRIPTION}/providers/Microsoft.Authorization/roleDefinitions/de139f84-1756-47ae-9be6-808fbbe84772\",\"principalId\":\"${SP_OBJECT_ID}\",\"principalType\":\"ServicePrincipal\"}}" \
  --output none || echo "  (role assignment may already exist; continuing)"

TENANT_ID=$(az account show --query tenantId -o tsv)

cat <<EOF

Done. GitHub configuration (repo Settings -> Secrets and variables -> Actions):
  Variable AZURE_WEBAPP_NAME     = ${APP_NAME}
  Secret   AZURE_CLIENT_ID       = ${APP_ID}
  Secret   AZURE_TENANT_ID       = ${TENANT_ID}
  Secret   AZURE_SUBSCRIPTION_ID = ${SUBSCRIPTION}

  (set them non-interactively with:
     gh variable set AZURE_WEBAPP_NAME --repo ${GH_REPO} --body ${APP_NAME}
     printf '%s' ${APP_ID} | gh secret set AZURE_CLIENT_ID --repo ${GH_REPO}
     printf '%s' ${TENANT_ID} | gh secret set AZURE_TENANT_ID --repo ${GH_REPO}
     printf '%s' ${SUBSCRIPTION} | gh secret set AZURE_SUBSCRIPTION_ID --repo ${GH_REPO} )

Next:
  1. Push to main (or run the "Deploy to Azure" workflow manually).
  2. The Azure SQL DB starts empty, so Umbraco serves its installer. Open
     https://${APP_NAME}.azurewebsites.net/umbraco and complete the install to create your
     backoffice admin account and the Umbraco schema. On the next boot the code-first seeders
     create the content types and sample content. Media is stored in the '${MEDIA_CONTAINER}'
     blob container.
EOF
