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
# It prompts for the SQL admin password and the Umbraco unattended-install password;
# those are never written to any file. Names marked "globally unique" may need adjusting
# if already taken.
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

ADMIN_EMAIL="admin@redants.ch"  # Umbraco backoffice super-user created on first boot
ADMIN_NAME="RedAnts Admin"
# ─────────────────────────────────────────────────────────────────────────────

# Passwords come from env vars if set, otherwise an interactive prompt (needs a real terminal).
# Interactive form:   bash deploy/azure-setup.sh
# Non-interactive:    SQL_PASS='...' UMB_PASS='...' bash deploy/azure-setup.sh
: "${SQL_PASS:=}"
: "${UMB_PASS:=}"
if [ -z "$SQL_PASS" ]; then read -rs -p "SQL admin password for '$SQL_ADMIN': " SQL_PASS || true; echo; fi
if [ -z "$UMB_PASS" ]; then read -rs -p "Umbraco backoffice password for '$ADMIN_EMAIL': " UMB_PASS || true; echo; fi
if [ -z "$SQL_PASS" ] || [ -z "$UMB_PASS" ]; then
  echo "ERROR: passwords not provided. Run this in an interactive terminal, or pass them as" >&2
  echo "       environment variables: SQL_PASS='...' UMB_PASS='...' bash deploy/azure-setup.sh" >&2
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

echo "==> App settings: connection string + provider + unattended install"
# Set the DSN as a plain app setting (ConnectionStrings__...) so ASP.NET Core does NOT
# auto-inject the legacy System.Data.SqlClient provider that the SQLAzure type would add.
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" \
  --settings \
    "ConnectionStrings__umbracoDbDSN=${CONN}" \
    "ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient" \
    "Umbraco__CMS__Unattended__InstallUnattended=true" \
    "Umbraco__CMS__Unattended__UpgradeUnattended=true" \
    "Umbraco__CMS__Unattended__UnattendedUserName=${ADMIN_NAME}" \
    "Umbraco__CMS__Unattended__UnattendedUserEmail=${ADMIN_EMAIL}" \
    "Umbraco__CMS__Unattended__UnattendedUserPassword=${UMB_PASS}" \
  --output none

echo "==> Publish credentials (put these into GitHub secrets KUDU_USER / KUDU_PASS)"
az webapp deployment list-publishing-credentials \
  --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" \
  --query "{KUDU_USER:publishingUserName, KUDU_PASS:publishingPassword}" -o json

cat <<EOF

Done. Next steps:
  1. In GitHub (repo Settings -> Secrets and variables -> Actions):
       - Variable AZURE_WEBAPP_NAME = ${APP_NAME}
       - Secret   KUDU_USER         = <publishingUserName printed above>
       - Secret   KUDU_PASS         = <publishingPassword printed above>
  2. Push to main (or run the "Deploy to Azure" workflow manually).
  3. First boot installs the Umbraco schema into the empty Azure SQL DB and creates the
     backoffice user ${ADMIN_EMAIL}. The code-first seeders then create the content types
     and sample content. Log in at https://${APP_NAME}.azurewebsites.net/umbraco
  4. (Optional, recommended) once installed, remove the Umbraco__CMS__Unattended__Unattended*
     install app settings again.
EOF
