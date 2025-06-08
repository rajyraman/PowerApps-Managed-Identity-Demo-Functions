param name string
param location string = resourceGroup().location
param tags object = {}

param kind string
param sku object

// Convert from custom SKU object to AVM format
var skuName = sku.name

module appServicePlan 'br/public:avm/res/web/serverfarm:0.4.1' = {
  name: 'appServicePlan-${name}'
  params: {
    name: name
    location: location
    tags: tags
    skuName: skuName
    kind: kind
    reserved: false
    maximumElasticWorkerCount: 20
  }
}

output id string = appServicePlan.outputs.resourceId
output name string = appServicePlan.outputs.name
