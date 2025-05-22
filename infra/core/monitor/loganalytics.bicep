param name string
param location string = resourceGroup().location
param tags object = {}

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.4.0' = {
  name: 'logAnalytics-${name}'
  params: {
    name: name
    location: location
    tags: tags
    retentionInDays: 30
    sku: 'PerGB2018'
  }
}

output id string = logAnalytics.outputs.resourceId
output name string = logAnalytics.outputs.name
