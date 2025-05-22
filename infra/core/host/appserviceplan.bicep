param name string
param location string = resourceGroup().location
param tags object = {}

param kind string
param sku object

// Convert from custom SKU object to AVM format
var skuName = sku.name
var capacity = contains(sku, 'capacity') ? sku.capacity : null

module appServicePlan 'br/public:avm/res/web/serverfarm:0.3.0' = {
  name: 'appServicePlan-${name}'
  params: {
    name: name
    location: location
    tags: tags
    skuName: skuName
    capacity: capacity
    kind: kind
    reserved: false
    maximumElasticWorkerCount: 20
  }
}

output id string = appServicePlan.outputs.resourceId
output name string = appServicePlan.outputs.name
