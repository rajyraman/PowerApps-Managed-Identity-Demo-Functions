param name string
param dashboardName string
param location string = resourceGroup().location
param tags object = {}
param includeDashboard bool = true
param logAnalyticsWorkspaceId string

module applicationInsights 'br/public:avm/res/insights/component:0.2.0' = {
  name: 'applicationInsights-${name}'
  params: {
    name: name
    location: location
    tags: tags
    kind: 'web'
    workspaceResourceId: logAnalyticsWorkspaceId
  }
}

module applicationInsightsDashboard 'applicationinsights-dashboard.bicep' = if (includeDashboard) {
  name: 'application-insights-dashboard'
  params: {
    name: dashboardName
    location: location
    applicationInsightsName: applicationInsights.outputs.name
  }
}

output connectionString string = applicationInsights.outputs.connectionString
output instrumentationKey string = applicationInsights.outputs.instrumentationKey
output name string = applicationInsights.outputs.name
