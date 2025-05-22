param name string
param location string = resourceGroup().location
param tags object = {}

// Reference Properties
param applicationInsightsName string
param appServicePlanId string
param storageAccountName string
param userAssignedIdentityId string

// Microsoft.Web/sites Properties
param kind string = 'functionapp'

// Microsoft.Web/sites/config
param appSettings object = {}
param clientAffinityEnabled bool = false
param functionAppScaleLimit int = -1
param minimumElasticInstanceCount int = -1
param numberOfWorkers int = -1
param use32BitWorkerProcess bool = false
param subnetId string

resource storage 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageAccountName
}

// Only dedicated AppService plans do not need WEBSITE_CONTENTAZUREFILECONNECTIONSTRING and WEBSITE_CONTENTSHARE keys according to docs.
var defaultAppSettings = {
  AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  WEBSITE_CONTENTSHARE: name
  FUNCTIONS_EXTENSION_VERSION: '~4'
  FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
  OpenApi__HideSwaggerUI: 'false'
  OpenApi__AuthLevel__UI: 'Anonymous'
  OpenApi__AuthLevel__Document: 'Anonymous'
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

module functions 'br/public:avm/res/web/site:0.3.0' = {
  name: '${name}-functions'
  params: {
    name: name
    location: location
    tags: tags
    kind: kind
    serverFarmId: appServicePlanId
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      functionsRuntimeScaleMonitoringEnabled: false
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      numberOfWorkers: numberOfWorkers != -1 ? numberOfWorkers : null
      minimumElasticInstanceCount: minimumElasticInstanceCount != -1 ? minimumElasticInstanceCount : null
      use32BitWorkerProcess: use32BitWorkerProcess
      functionAppScaleLimit: functionAppScaleLimit != -1 ? functionAppScaleLimit : null
      cors: {
        allowedOrigins: [ 'https://portal.azure.com', 'https://ms.portal.azure.com' ]
      }
    }
    clientAffinityEnabled: clientAffinityEnabled
    httpsOnly: true
    vnetRouteAllEnabled: empty(subnetId) ? false : true
    vnetContentShareEnabled: empty(subnetId) ? false : true
    virtualNetworkSubnetId: empty(subnetId) ? null : subnetId
    appSettings: union(
      empty(subnetId) ? defaultAppSettings : union(defaultAppSettings, { WEBSITE_CONTENTOVERVNET: '1' }),
      appSettings,
      { APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString }
    )
  }
}

// Virtual Network integration if subnet is provided
module networkConfig 'br/public:avm/res/web/site/config:0.3.0' = if (!empty(subnetId)) {
  name: '${name}-virtualNetwork'
  params: {
    name: 'virtualNetwork'
    kind: 'networkConfig'
    siteName: functions.outputs.name
    subnetResourceId: subnetId
    swiftSupported: true
  }
}

output name string = functions.outputs.name
output uri string = 'https://${functions.outputs.defaultHostName}'
// We can't use functions.outputs because user-assigned identities don't expose principal ID that way
// Use resource() function to get the principal ID of the managed identity
var managedIdentityResourceId = userAssignedIdentityId
output identityPrincipalId string = managedIdentityResourceId
