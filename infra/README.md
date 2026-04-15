# Infrastructure — Terraform

Provisions the full Azure stack for the D&D Campaign Manager:

- **Resource Group** — container for all resources
- **Azure SQL Server + Database** — serverless Gen5 with auto-pause (free-tier friendly)
- **App Service Plan + Linux Web App** — hosts the .NET 8 API (F1 free tier by default)
- **Static Web App** — hosts the React SPA (free tier)
- **Entra ID App Registrations** — API and SPA apps with roles and scopes

## Prerequisites

1. [Terraform CLI](https://developer.hashicorp.com/terraform/downloads) >= 1.5
2. [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) >= 2.50
3. An Azure subscription (free trial works)

## Quick start

```bash
# 1. Log in to Azure
az login

# 2. Set your subscription (if you have multiple)
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# 3. Navigate to the infra directory
cd infra

# 4. Create your config
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# 5. Initialize Terraform
terraform init

# 6. Preview what will be created
terraform plan

# 7. Deploy
terraform apply

# 8. Note the outputs — you'll need them for the app config
terraform output
```

## After deployment

### Run EF migrations against Azure SQL

From the project root:

```bash
# Get the connection string from Terraform output
CONNECTION=$(terraform -chdir=infra output -raw sql_server_fqdn)

dotnet ef database update \
  -p src/Infrastructure \
  -s src/API \
  --connection "Server=tcp:${CONNECTION},1433;Initial Catalog=dndcm-db;User ID=sqladmin;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;"
```

### Deploy the API

```bash
cd src/API
dotnet publish -c Release -o ./publish

# Deploy to App Service
az webapp deploy \
  --resource-group dndcm-dev-rg \
  --name dndcm-dev-api \
  --src-path ./publish \
  --type zip
```

### Configure the SPA

Terraform outputs a ready-to-use `.env` file:

```bash
terraform output -raw spa_env_file > ../client-app/.env
```

Then build and deploy:

```bash
cd client-app
npm install
npm run build

# Deploy to Static Web App
az staticwebapp deploy \
  --app-location ./dist \
  --resource-group dndcm-dev-rg \
  --name dndcm-dev-spa
```

### Assign yourself the DM role

```bash
# Get your user's object ID
USER_OID=$(az ad signed-in-user show --query id -o tsv)

# Get the API service principal's object ID
SP_OID=$(az ad sp list --display-name "dndcm-dev-api" --query "[0].id" -o tsv)

# Assign the DM role (role ID matches what's in main.tf)
az rest --method POST \
  --uri "https://graph.microsoft.com/v1.0/servicePrincipals/${SP_OID}/appRoleAssignments" \
  --body "{\"principalId\":\"${USER_OID}\",\"resourceId\":\"${SP_OID}\",\"appRoleId\":\"00000000-0000-0000-0000-000000000010\"}"
```

## Cost on free trial

| Resource | SKU | Monthly cost |
|---|---|---|
| App Service | F1 (free) | $0 |
| SQL Database | Serverless Gen5 with auto-pause | ~$5/mo (pauses when idle) |
| Static Web App | Free | $0 |
| Entra ID | Free tier | $0 |

The SQL serverless tier auto-pauses after 60 minutes of inactivity, so for a D&D campaign app that's used a few times a week, actual cost will be very low. The $200 free trial credit covers this many times over.

## Tear down

```bash
terraform destroy
```

This removes all Azure resources. Your Terraform state tracks everything, so nothing is orphaned.
