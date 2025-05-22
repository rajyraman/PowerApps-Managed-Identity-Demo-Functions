param name string
param location string = resourceGroup().location
param tags object = {}

module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.1.0' = {
  name: '${name}-managed-identity'
  params: {
    name: name
    location: location
    tags: tags
  }
}

output name string = managedIdentity.outputs.name
output id string = managedIdentity.outputs.resourceId
output principalId string = managedIdentity.outputs.principalId
output clientId string = managedIdentity.outputs.clientId