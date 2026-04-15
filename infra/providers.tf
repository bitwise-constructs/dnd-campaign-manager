terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }

  # Uncomment and configure for remote state (recommended for team use).
  # For local dev/trial, local state is fine.
  # backend "azurerm" {
  #   resource_group_name  = "tfstate-rg"
  #   storage_account_name = "tfstatedndcm"
  #   container_name       = "tfstate"
  #   key                  = "dndcm.tfstate"
  # }
}

provider "azurerm" {
  features {}
}

provider "azuread" {}
