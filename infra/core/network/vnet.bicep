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
        name: 'functionApp'
        properties: {
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
    ]
  }
}

output functionAppSubnet string = virtualNetwork.properties.subnets[0].id
output name string = virtualNetwork.name
