param location string = resourceGroup().location
@description('Name of the VNet to be created')
param name string
param tags object = {}
param serviceEndpointStorageLocations string[]

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2019-11-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'snet-functionapp'
        properties: {
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
          addressPrefix: '10.0.0.0/24'
          serviceEndpoints: [
            {
              locations: serviceEndpointStorageLocations
              service: 'Microsoft.Storage'
            }
          ]
          delegations: [
            {
              name: 'Microsoft.Web/serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'snet-privatelink'
        properties: {
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
          addressPrefix: '10.0.1.0/24'
        }
      }
    ]
  }
}

output functionAppSubnet string = virtualNetwork.properties.subnets[0].id
output privateLinkSubnet string = virtualNetwork.properties.subnets[1].id
output name string = virtualNetwork.name
