@description('The name of the app service resource within the current resource group scope')
param name string

@description('The app settings to be applied to the app service')
param appSettings object

resource appService 'Microsoft.Web/sites@2022-03-01' existing = {
  name: name

  resource configAppSettings 'config' = {
    name: 'appsettings'
    properties: appSettings
  }
}
