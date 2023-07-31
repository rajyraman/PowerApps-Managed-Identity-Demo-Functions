param name string
param location string = resourceGroup().location
param tags object = {}

// Reference Properties
param applicationInsightsName string = ''
param appServicePlanId string

param kind string

// Microsoft.Web/sites/config
param allowedOrigins array = []
param alwaysOn bool = false
param appSettings object = {}
param clientAffinityEnabled bool = false
param functionAppScaleLimit int = -1
param minimumElasticInstanceCount int = -1
param numberOfWorkers int = -1
param use32BitWorkerProcess bool = false
param ftpsState string = 'FtpsOnly'
param healthCheckPath string = ''
param subnetId string

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      netFrameworkVersion: 'v6.0'
      functionsRuntimeScaleMonitoringEnabled: false //if this is not set to false, deployment will fail.
      alwaysOn: alwaysOn
      ftpsState: ftpsState
      minTlsVersion: '1.2'
      numberOfWorkers: numberOfWorkers != -1 ? numberOfWorkers : null
      minimumElasticInstanceCount: minimumElasticInstanceCount != -1 ? minimumElasticInstanceCount : null
      use32BitWorkerProcess: use32BitWorkerProcess
      functionAppScaleLimit: functionAppScaleLimit != -1 ? functionAppScaleLimit : null
      healthCheckPath: healthCheckPath
      cors: {
        allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
      }
    }
    clientAffinityEnabled: clientAffinityEnabled
    httpsOnly: true
    vnetRouteAllEnabled: true
    vnetContentShareEnabled: true
    virtualNetworkSubnetId: subnetId
  }

  identity: { type: 'SystemAssigned' }

  //If this is not commented out, deployment will fail with "Storage access failed. Storage volume is currently in R/O mode". Leaving this as a comment for people searching this issue.
  // resource configLogs 'config' = {
  //   name: 'logs'
  //   properties: {
  //     applicationLogs: { fileSystem: { level: 'Verbose' } }
  //     detailedErrorMessages: { enabled: true }
  //     failedRequestsTracing: { enabled: true }
  //     httpLogs: { fileSystem: { enabled: true, retentionInDays: 1, retentionInMb: 35 } }
  //   }
  // }

  resource networkConfig 'networkConfig@2022-03-01' = {
    name: 'virtualNetwork'
    properties: {
      subnetResourceId: subnetId
      swiftSupported: true
    }
  }
}

module config 'appservice-appsettings.bicep' = if (!empty(appSettings)) {
  name: '${name}-appSettings'
  params: {
    name: appService.name
    appSettings: union(appSettings,
      !empty(applicationInsightsName) ? { APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString } : {})
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(applicationInsightsName)) {
  name: applicationInsightsName
}

output identityPrincipalId string = appService.identity.principalId
output name string = appService.name
output uri string = 'https://${appService.properties.defaultHostName}'
