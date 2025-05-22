param name string
param location string = resourceGroup().location
param tags object = {}
param shareName string

@allowed([
  'Cool'
  'Hot'
  'Premium' ])
param accessTier string = 'Hot'
param allowBlobPublicAccess bool = true
param allowCrossTenantReplication bool = true
param allowSharedKeyAccess bool = true
param defaultToOAuthAuthentication bool = false

@allowed([ 'AzureDnsZone', 'Standard' ])
param dnsEndpointType string = 'Standard'
param kind string = 'StorageV2'
param minimumTlsVersion string = 'TLS1_2'

@allowed([ 'Enabled', 'Disabled' ])
param publicNetworkAccess string = 'Enabled'
param sku object = { name: 'Standard_LRS' }

param subnet string

module storage 'br/public:avm/res/storage/storage-account:0.10.0' = {
  name: 'storage-${name}'
  params: {
    name: name
    location: location
    tags: tags
    skuName: sku.name
    kind: kind
    accessTier: accessTier
    allowBlobPublicAccess: allowBlobPublicAccess
    allowCrossTenantReplication: allowCrossTenantReplication
    allowSharedKeyAccess: allowSharedKeyAccess
    defaultToOAuthAuthentication: defaultToOAuthAuthentication
    dnsEndpointType: dnsEndpointType
    minimumTlsVersion: minimumTlsVersion
    publicNetworkAccess: publicNetworkAccess
    networkAcls: empty(subnet) ? {
      defaultAction: 'Allow'
    } : {
      bypass: ['AzureServices']
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: subnet
          action: 'Allow'
        }
      ]
    }
  }
}

module fileServices 'br/public:avm/res/storage/storage-account/file-service:0.10.0' = {
  name: 'fileServices-${name}'
  params: {
    storageAccountName: storage.outputs.name
    shareDeleteRetentionPolicy: {
      enabled: false
    }
  }
}

module fileShare 'br/public:avm/res/storage/storage-account/file-service/share:0.10.0' = {
  name: 'fileShare-${name}'
  params: {
    storageAccountName: storage.outputs.name
    name: shareName
    accessTier: 'TransactionOptimized'
    shareQuota: 5120
    enabledProtocols: 'SMB'
  }
  dependsOn: [
    fileServices
  ]
}

output name string = storage.outputs.name
output primaryEndpoints object = storage.outputs.primaryEndpoints
