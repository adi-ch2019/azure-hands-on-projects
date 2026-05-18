terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Random suffix for unique names
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

# Resource Group
resource "azurerm_resource_group" "jarvis" {
  name     = "rg-jarvis-${random_string.suffix.result}"
  location = "centralindia"
}

# ===== BACKING SERVICE 1: Azure SQL =====
resource "azurerm_mssql_server" "jarvis" {
  name                         = "sql-jarvis-${random_string.suffix.result}"
  resource_group_name          = azurerm_resource_group.jarvis.name
  location                     = azurerm_resource_group.jarvis.location
  version                      = "12.0"
  administrator_login          = "jarvisadmin"
  administrator_login_password = random_password.sql_password.result
  
  tags = {
    Environment = "Production"
    System      = "JARVIS"
  }
}

resource "random_password" "sql_password" {
  length  = 20
  special = false
}

resource "azurerm_mssql_database" "suit_db" {
  name      = "SuitTelemetryDB"
  server_id = azurerm_mssql_server.jarvis.id
  sku_name  = "Basic"  # Low cost for portfolio
}

# Firewall rule for Azure services
resource "azurerm_mssql_firewall_rule" "azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.jarvis.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# ===== BACKING SERVICE 2: Redis Cache =====
resource "azurerm_redis_cache" "jarvis" {
  name                = "redis-jarvis-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.jarvis.name
  location            = azurerm_resource_group.jarvis.location
  capacity            = 1
  family              = "C"
  sku_name            = "Basic"
  
  redis_configuration {
    maxmemory_reserved = 2
    maxmemory_delta    = 2
  }
}

# ===== BACKING SERVICE 3: Service Bus =====
resource "azurerm_servicebus_namespace" "jarvis" {
  name                = "sb-jarvis-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.jarvis.name
  location            = azurerm_resource_group.jarvis.location
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "suit_events" {
  name         = "suit-events"
  namespace_id = azurerm_servicebus_namespace.jarvis.id
  
  # Exactly-once delivery for battle events
  requires_duplicate_detection = true
  duplicate_detection_history_time_window = "PT10M"
  
  # Session support for suit-specific ordering
  requires_session = false
  
  lock_duration = "PT30S"
  max_size_in_megabytes = 1024
}

# ===== App Service Plan =====
resource "azurerm_service_plan" "jarvis" {
  name                = "asp-jarvis-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.jarvis.name
  location            = azurerm_resource_group.jarvis.location
  os_type             = "Linux"
  sku_name            = "B1"  # Free tier for portfolio
}

# ===== App Service for API =====
resource "azurerm_linux_web_app" "api" {
  name                = "app-jarvis-api-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.jarvis.name
  location            = azurerm_resource_group.jarvis.location
  service_plan_id     = azurerm_service_plan.jarvis.id
  
  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }
  
  app_settings = {
    "ConnectionStrings__SuitDatabase" = "Server=tcp:${azurerm_mssql_server.jarvis.fully_qualified_domain_name},1433;Initial Catalog=SuitTelemetryDB;Persist Security Info=False;User ID=${azurerm_mssql_server.jarvis.administrator_login};Password=${random_password.sql_password.result};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    "ConnectionStrings__Redis"         = "${azurerm_redis_cache.jarvis.hostname}:${azurerm_redis_cache.jarvis.ssl_port},password=${azurerm_redis_cache.jarvis.primary_access_key},ssl=True,abortConnect=False"
    "ConnectionStrings__ServiceBus"    = azurerm_servicebus_namespace.jarvis.default_primary_connection_string
    "ASPNETCORE_ENVIRONMENT"           = "Production"
  }
}

# ===== Outputs for GitHub Actions =====
output "resource_group_name" {
  value = azurerm_resource_group.jarvis.name
}

output "sql_server_name" {
  value = azurerm_mssql_server.jarvis.name
}

output "redis_hostname" {
  value = azurerm_redis_cache.jarvis.hostname
}

output "servicebus_namespace" {
  value = azurerm_servicebus_namespace.jarvis.name
}

output "app_service_name" {
  value = azurerm_linux_web_app.api.name
}

output "connection_strings" {
  value = {
    sql        = "Server=tcp:${azurerm_mssql_server.jarvis.fully_qualified_domain_name},1433;Database=SuitTelemetryDB;User ID=${azurerm_mssql_server.jarvis.administrator_login};Password=${random_password.sql_password.result};Encrypt=True;"
    redis      = "${azurerm_redis_cache.jarvis.hostname}:${azurerm_redis_cache.jarvis.ssl_port},password=${azurerm_redis_cache.jarvis.primary_access_key}"
    servicebus = azurerm_servicebus_namespace.jarvis.default_primary_connection_string
  }
  sensitive = true
}