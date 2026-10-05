targetScope = 'resourceGroup'

@description('Região em que o App Service e os recursos de observabilidade serão criados.')
param location string = resourceGroup().location

@description('Prefixo globalmente único, com letras minúsculas e números.')
param prefix string

@allowed([
  'F1'
  'B1'
])
@description('Plano do App Service. Verifique disponibilidade e preço antes de implantar.')
param appServiceSku string = 'B1'

var appName = 'app-${prefix}-producthub'
var planName = 'asp-${prefix}-producthub'
var workspaceName = 'law-${prefix}-producthub'
var insightsName = 'appi-${prefix}-producthub'
var keyVaultName = 'kv-${prefix}-producthub'

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: appServiceSku
    tier: appServiceSku == 'F1' ? 'Free' : 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false
      appSettings: [
        {
          name: 'KeyVaultUri'
          value: 'https://${keyVault.name}.vault.azure.net/'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
    httpsOnly: true
  }
}

output webAppName string = app.name
output webAppUrl string = 'https://${app.properties.defaultHostName}'
output keyVaultUri string = 'https://${keyVault.name}.vault.azure.net/'
