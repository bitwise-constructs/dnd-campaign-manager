locals {
  prefix   = "${var.project_name}-${var.environment}"
  api_name = "${local.prefix}-api"
}

# =============================================================================
# Resource Group
# =============================================================================

resource "azurerm_resource_group" "main" {
  name     = "${local.prefix}-rg"
  location = var.location
  tags     = var.tags
}

# =============================================================================
# SQL Server + Database
# =============================================================================

resource "azurerm_mssql_server" "main" {
  name                         = "${local.prefix}-sql"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
  tags                         = var.tags
}

resource "azurerm_mssql_database" "main" {
  name      = "${var.project_name}-db"
  server_id = azurerm_mssql_server.main.id

  # Free tier: GP_S_Gen5_1 serverless with auto-pause (cheapest option)
  sku_name                    = var.sql_database_sku
  max_size_gb                 = 32
  auto_pause_delay_in_minutes = 60
  min_capacity                = 0.5
  zone_redundant              = false

  tags = var.tags
}

# Allow Azure services to reach the SQL server (App Service -> SQL)
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# =============================================================================
# App Service Plan + Web App (API)
# =============================================================================

resource "azurerm_service_plan" "main" {
  name                = "${local.prefix}-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"

  # F1 = free tier, B1 = basic (cheapest paid)
  sku_name = var.app_service_sku

  tags = var.tags
}

resource "azurerm_linux_web_app" "api" {
  name                = local.api_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  https_only = true

  site_config {
    always_on = var.app_service_sku != "F1" # F1 doesn't support always-on

    application_stack {
      dotnet_version = "8.0"
    }

    cors {
      allowed_origins = [
        "https://${azurerm_static_web_app.spa.default_host_name}",
        "http://localhost:5173"
      ]
      support_credentials = true
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.environment == "prod" ? "Production" : "Development"

    # Entra ID auth
    "AzureAd__Instance" = "https://login.microsoftonline.com/"
    "AzureAd__TenantId" = var.entra_tenant_id
    "AzureAd__ClientId" = var.entra_api_client_id
    "AzureAd__Audience" = "api://${var.entra_api_client_id}"

    # Client app
    "ClientApp__BaseUrl"     = "https://${azurerm_static_web_app.spa.default_host_name}"
    "ClientApp__ClientId"    = var.entra_spa_client_id

    # D&D Beyond config
    "DndBeyond__CharacterServiceBaseUrl" = "https://character-service.dndbeyond.com/character/v5/character"
    "DndBeyond__TimeoutSeconds"          = "15"
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  tags = var.tags
}

# =============================================================================
# Static Web App (React SPA)
# =============================================================================

resource "azurerm_static_web_app" "spa" {
  name                = "${local.prefix}-spa"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = var.tags
}

# =============================================================================
# Entra ID App Registrations
# =============================================================================

data "azuread_client_config" "current" {}

# --- API App Registration ---
resource "azuread_application" "api" {
  display_name = "${local.prefix}-api"
  owners       = [data.azuread_client_config.current.object_id]

  identifier_uris = ["api://${var.entra_api_client_id}"]

  api {
    oauth2_permission_scope {
      admin_consent_description  = "Access the D&D Campaign Manager API"
      admin_consent_display_name = "Access D&D Campaign Manager"
      id                         = "00000000-0000-0000-0000-000000000001"
      enabled                    = true
      type                       = "Admin"
      value                      = "access_as_user"
    }
  }

  app_role {
    allowed_member_types = ["User"]
    description          = "Dungeon Master with full campaign access"
    display_name         = "DM"
    enabled              = true
    id                   = "00000000-0000-0000-0000-000000000010"
    value                = "DM"
  }

  app_role {
    allowed_member_types = ["User"]
    description          = "Player with access to their own characters"
    display_name         = "Player"
    enabled              = true
    id                   = "00000000-0000-0000-0000-000000000020"
    value                = "Player"
  }
}

resource "azuread_service_principal" "api" {
  client_id = azuread_application.api.client_id
  owners    = [data.azuread_client_config.current.object_id]
}

# --- SPA App Registration ---
resource "azuread_application" "spa" {
  display_name = "${local.prefix}-spa"
  owners       = [data.azuread_client_config.current.object_id]

  single_page_application {
    redirect_uris = [
      "http://localhost:5173",
      "https://${azurerm_static_web_app.spa.default_host_name}"
    ]
  }

  required_resource_access {
    resource_app_id = azuread_application.api.client_id

    resource_access {
      id   = "00000000-0000-0000-0000-000000000001"
      type = "Scope"
    }
  }
}

resource "azuread_service_principal" "spa" {
  client_id = azuread_application.spa.client_id
  owners    = [data.azuread_client_config.current.object_id]
}
