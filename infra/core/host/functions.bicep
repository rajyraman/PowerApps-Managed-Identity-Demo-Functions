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
module functions 'appservice.bicep' = {
  name: '${name}-functions'
  params: {
    name: name
    location: location
    tags: tags
    applicationInsightsName: applicationInsightsName
    appServicePlanId: appServicePlanId
    appSettings: union(appSettings, {
        AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        WEBSITE_CONTENTSHARE: name
        FUNCTIONS_EXTENSION_VERSION: '~4'
        FUNCTIONS_WORKER_RUNTIME: 'dotnet'
        WEBSITE_CONTENTOVERVNET: '1'
        OpenApi__HideSwaggerUI: 'false'
        OpenApi__AuthLevel__UI: 'Anonymous'
        OpenApi__AuthLevel__Document: 'Anonymous'
      })
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

output name string = functions.outputs.name
output uri string = functions.outputs.uri
output identityPrincipalId string = functions.outputs.identityPrincipalId
