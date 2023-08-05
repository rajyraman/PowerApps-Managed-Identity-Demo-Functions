targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description(
  'Name of the the environment which is used to generate a short unique hash used in all resources.'
)
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description(
  'Locations for service endpoint in VNet e.g. australiaeast,australiasoutheast'
)
param serviceEndpointStorageLocations string

@description('Is VNet isolation needed?')
param createVNetString string = 'true'
var createVNet = bool(createVNetString)

@description('Create Private Endpoint and Private Link to Storage Account?')
param createPrivateLinkString string = 'true'
var createPrivateLink = bool(createPrivateLinkString)

// Optional parameters to override the default azd resource naming conventions. Update the main.parameters.json file to provide values. e.g.,:
// "resourceGroupName": {
//      "value": "myGroupName"
// }
param applicationInsightsDashboardName string = ''
param applicationInsightsName string = ''
param appServicePlanName string = ''
param logAnalyticsName string = ''
param resourceGroupName string = ''
param storageAccountName string = ''
param vNetName string = ''
param dataverseUrl string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(
  uniqueString(subscription().id, environmentName, location)
)
var tags = { 'azd-env-name': environmentName }
var functionName = '${abbrs.webSitesFunctions}${environmentName}-${resourceToken}'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

module vnet 'core/network/vnet.bicep' = if (createVNet || createPrivateLink) {
  name: 'vnet'
  scope: rg
  params: {
    name: !empty(vNetName) ? vNetName : '${abbrs.networkVirtualNetworks}${resourceToken}'
    location: location
    tags: tags
    serviceEndpointStorageLocations: split(serviceEndpointStorageLocations, ',')
  }
}

// Backing storage for Azure functions backend API
module storage './core/storage/storage-account.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
    location: location
    tags: tags
    subnet: createVNet ? vnet.outputs.functionAppSubnet : ''
    publicNetworkAccess: createVNet ? 'Disabled' : 'Enabled'
    shareName: functionName
  }
}

// Create an App Service Plan to group applications under the same payment plan and SKU
module appServicePlan './core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: rg
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    kind: createVNet ? 'elastic' : ''
    sku: createVNet ? {
      name: 'EP1'
      tier: 'ElasticPremium'
      family: 'EP'
    } : {
      name: 'Y1'
      tier: 'Dynamic'
      family: 'Y'
    }
  }
}

// Monitor application with Azure Monitor
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: !empty(applicationInsightsDashboardName) ? applicationInsightsDashboardName : '${abbrs.portalDashboards}${resourceToken}'
  }
}

module functions 'core/host/functions.bicep' = {
  name: 'functions-app'
  scope: rg
  params: {
    name: functionName
    location: location
    appServicePlanId: appServicePlan.outputs.id
    storageAccountName: storage.outputs.name
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    tags: union(tags, { 'azd-service-name': 'api' })
    subnetId: createVNet ? vnet.outputs.functionAppSubnet : ''
    appSettings: {
      DATAVERSE_URL: dataverseUrl
    }
  }
}

module privatelink 'core/network/privatelink.bicep' = if (createPrivateLink) {
  name: 'privatelink'
  scope: rg
  params: {
    storageAccountName: storage.outputs.name
    vnetName: createPrivateLink ? vnet.outputs.name : '' //Need this ternary condition. Otherwise ARM spits out error during compilation. See https://github.com/Azure/bicep/issues/3990#issue-967101559
    location: location
  }
}

// App outputs
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output FUNCTIONS_NAME string = functions.outputs.name
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = subscription().tenantId
