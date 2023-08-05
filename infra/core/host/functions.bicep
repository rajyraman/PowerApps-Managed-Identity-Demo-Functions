param name string
param location string = resourceGroup().location
param tags object = {}

// Reference Properties
param applicationInsightsName string
param appServicePlanId string
param storageAccountName string

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

var appSettingsUnionised = empty(subnetId) ? union(appSettings, {
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
    WEBSITE_CONTENTSHARE: name
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    OpenApi__HideSwaggerUI: 'false'
    OpenApi__AuthLevel__UI: 'Anonymous'
    OpenApi__AuthLevel__Document: 'Anonymous'
  }) : union(appSettings, {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    WEBSITE_CONTENTOVERVNET: '1'
    OpenApi__HideSwaggerUI: 'false'
    OpenApi__AuthLevel__UI: 'Anonymous'
    OpenApi__AuthLevel__Document: 'Anonymous'
    AzureWebJobsStorage__accountName: storage.name //This connects to Storage as Function's System Assigned Managed Identity
  })
module functions 'appservice.bicep' = {
  name: '${name}-functions'
  params: {
    name: name
    location: location
    tags: tags
    applicationInsightsName: applicationInsightsName
    appServicePlanId: appServicePlanId
    appSettings: appSettingsUnionised
    clientAffinityEnabled: clientAffinityEnabled
    functionAppScaleLimit: functionAppScaleLimit
    kind: kind
    minimumElasticInstanceCount: minimumElasticInstanceCount
    numberOfWorkers: numberOfWorkers
    use32BitWorkerProcess: use32BitWorkerProcess
    subnetId: subnetId
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageAccountName
}

// See https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#all
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(resourceGroup().id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')) //Storage Blob Data Owner
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
    principalId: functions.outputs.identityPrincipalId
  }
}

output name string = functions.outputs.name
output uri string = functions.outputs.uri
output identityPrincipalId string = functions.outputs.identityPrincipalId
