param vnetName string
param storageAccountName string
param location string = resourceGroup().location
var privateLinkSubnetName = vnet.properties.subnets[1].id
var storageSuffix = environment().suffixes.storage

resource vnet 'Microsoft.Network/virtualNetworks@2019-11-01' existing = {
  name: vnetName
}

resource storage 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageAccountName
}

var privateEndpointResources = [ 'file', 'blob', 'table', 'queue' ]

resource privateStorageDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = [for privateEndpointResource in privateEndpointResources: {
  name: 'privatelink.${privateEndpointResource}.${storageSuffix}'
  location: 'global'
}]

resource privateStorageDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = [for privateEndpointResource in privateEndpointResources: {
  name: 'privatelink.${privateEndpointResource}.${storageSuffix}/${privateEndpointResource}${storageSuffix}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
  dependsOn: privateStorageDnsZone
}]

resource privateEndpointStorage 'Microsoft.Network/privateEndpoints@2022-05-01' = [for privateEndpointResource in privateEndpointResources: {
  name: '${storage.name}-${privateEndpointResource}-private-endpoint'
  location: location
  properties: {
    subnet: {
      id: privateLinkSubnetName
    }
    privateLinkServiceConnections: [
      {
        name: 'storage${privateEndpointResource}privateLinkconnection'
        properties: {
          privateLinkServiceId: storage.id
          groupIds: [
            privateEndpointResource
          ]
        }
      }
    ]
  }
}]

resource privateEndpointStoragePrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2022-05-01' = [for privateEndpointResource in privateEndpointResources: {
  name: '${storage.name}-${privateEndpointResource}-private-endpoint/${privateEndpointResource}privatednszonegroup'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config'
        properties: {
          privateDnsZoneId: resourceId('Microsoft.Network/privateDnsZones', 'privatelink.${privateEndpointResource}.${storageSuffix}')
        }
      }
    ]
  }
  dependsOn: [ privateEndpointStorage, privateStorageDnsZoneLink ]
}]
