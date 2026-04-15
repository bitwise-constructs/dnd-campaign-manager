variable "project_name" {
  description = "Short name used as prefix for all resources"
  type        = string
  default     = "dndcm"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "centralus"
}

variable "sql_admin_username" {
  description = "SQL Server admin username"
  type        = string
  default     = "sqladmin"
}

variable "sql_admin_password" {
  description = "SQL Server admin password"
  type        = string
  sensitive   = true
}

variable "entra_tenant_id" {
  description = "Microsoft Entra ID (Azure AD) tenant ID"
  type        = string
}

variable "entra_api_client_id" {
  description = "Client ID of the API app registration in Entra"
  type        = string
}

variable "entra_spa_client_id" {
  description = "Client ID of the SPA app registration in Entra"
  type        = string
}

variable "app_service_sku" {
  description = "App Service plan SKU (F1 = free, B1 = basic)"
  type        = string
  default     = "F1"
}

variable "sql_database_sku" {
  description = "SQL Database SKU name"
  type        = string
  default     = "GP_S_Gen5_1"
}

variable "tags" {
  description = "Tags applied to all resources"
  type        = map(string)
  default = {
    project = "dnd-campaign-manager"
    managed = "terraform"
  }
}
