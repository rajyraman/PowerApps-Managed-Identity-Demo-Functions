using './main.bicep'

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dataverse-functions-dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiasoutheast')
param serviceEndpointStorageLocations = readEnvironmentVariable('SERVICE_ENDPOINT_STORAGE_LOCATIONS', 'australiasoutheast,australiaeast')
param dataverseUrl = readEnvironmentVariable('DATAVERSE_URL', '')
param createVNet = readEnvironmentVariable('CREATE_VNET', 'false')
param createPrivateLink = readEnvironmentVariable('CREATE_PRIVATE_LINK', 'false')
