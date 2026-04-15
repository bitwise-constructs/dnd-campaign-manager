output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "api_url" {
  description = "URL of the deployed API"
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "spa_url" {
  description = "URL of the deployed React SPA"
  value       = "https://${azurerm_static_web_app.spa.default_host_name}"
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  value = azurerm_mssql_database.main.name
}

output "api_app_client_id" {
  description = "Entra API app registration client ID"
  value       = azuread_application.api.client_id
}

output "spa_app_client_id" {
  description = "Entra SPA app registration client ID"
  value       = azuread_application.spa.client_id
}

output "entra_tenant_id" {
  value = var.entra_tenant_id
}

output "spa_env_file" {
  description = "Contents for client-app/.env file"
  value       = <<-EOT
    VITE_AZURE_TENANT_ID=${var.entra_tenant_id}
    VITE_AZURE_CLIENT_ID=${azuread_application.spa.client_id}
    VITE_API_CLIENT_ID=${azuread_application.api.client_id}
    VITE_API_BASE_URL=https://${azurerm_linux_web_app.api.default_hostname}/api
  EOT
}
