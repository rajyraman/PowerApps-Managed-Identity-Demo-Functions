# Azure Functions - Managed Identity

This is a sample repo to demonstrate how to use Azure Functions System Assigned Managed Identity to connect to Power Apps WebAPI - without any password, secret or certificate.

Blog Post: https://dreamingincrm.com/2021/11/16/connecting-to-dataverse-from-function-app-using-managed-identity/

## Deploying Azure Resources
Click this button to deploy Storage Account, App Insights and Function App resources to your Azure tenant.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Frajyraman%2FPowerApps-Managed-Identity-Demo-Functions%2Fmain%2Fdeploy%2Fmain.json" target="_blank">
  <img src="https://aka.ms/deploytoazurebutton"/>
</a>

## Function App Deployment
The Function App assembly/code has to be deployed from your local machine by running [deploy-function.ps1](./deploy/deploy-function.ps1) and providing the following paramaters.

|Name|Mandatory|Description|Default|
|-|-|-|-|
|functionAppName|No|Name of the currently deployed Function App||
|environmentUrl|Yes|URL of your Power Apps Environment e.g. https://org.crm.dynamics.com||
|createApplicationUser|No|Create the Managed Identity of the Function App as an Application User in the Power Apps Environment|$false|

You also do it all in once (create Azure resources and deploy the Function App code) by running [run.ps1](./deploy/run.ps1) and providing the following parameters

|Name|Mandatory|Description|Default|
|-|-|-|-|
|resourceGroup|No|Name of the new Resource Group where you would like the resources deployed to|rgazfunc|
|location|No|Azure location where you would like the Resource Group to be created in|australiasoutheast|
|environmentUrl|Yes|URL of your Power Apps Environment e.g. https://org.crm.dynamics.com||
|createApplicationUser|No|Create the Managed Identity of the Function App as an Application User in the Power Apps Environment|$false|

The created Application User needs to have the right security roles assigned, so that operations in the Function App that leverage Power Apps WebAPI can be performed.