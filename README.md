[![Open in GitHub Codespaces](https://img.shields.io/static/v1?style=for-the-badge&label=GitHub+Codespaces&message=Open&color=brightgreen&logo=github)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=427378322&machine=standardLinux32gb&devcontainer_path=.devcontainer%2Fdevcontainer.json&location=WestUs2)
[![Open in Dev Containers](https://img.shields.io/static/v1?style=for-the-badge&label=Dev%20%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/rajyraman/PowerApps-Managed-Identity-Demo-Functions)

# Connect to Dataverse from Azure Functions using System Assigned Managed Identity

This is a sample repo to demonstrate how to use Azure Functions System Assigned Managed Identity to connect to Power Apps WebAPI - without any password, secret or certificate.

Blog Post: https://dreamingincrm.com/2021/11/16/connecting-to-dataverse-from-function-app-using-managed-identity/

**This repo has been updated Aug-2023, so the content in the blog post does not exactly line up with what is currently in the repo.**

This is a sample repo that shows how to use Bicep to create Function App and how to use the Function App's System Assigned Managed Identity to connect to Dataverse. This application uses the Azure Developer CLI (azd) to deploy all the resources.

### Prerequisites

The following prerequisites are required to use this application. Please ensure that you have them all installed locally.

- [Azure Developer CLI](https://aka.ms/azd-install)
- [.NET SDK 6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Azure Functions Core Tools (4+)](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Node.js with npm (16.13.1+)](https://nodejs.org/)
- [Power Platform CLI](https://learn.microsoft.com/en-au/power-platform/developer/cli/introduction#install-microsoft-power-platform-cli)

If you don't want to install these tools locally you can always run the whole repo locally, using Dev Containers by clicking the Dev Containers button, or entirely in the browser by clicking the GitHub Codespaces button on the top.

### Deploying

The easiest option is to run this single command using Azure Developer CLI.

```powershell
azd up
```

This command will deploy the required resources and the Function App's application code as well.

You can also run provisioning first using

```powershell
azd provision
```

following by Function App's application code deployment using

```powershell
azd deploy
```

All the resources in Azure can be easily cleanup using

```powershell
azd down
```
For the full list of command refer to [azd docs](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/reference).

The Function App can be deployed in 1 of 3 possible configurations.

1. Azure Functions in Consumption Plan - This does not have any VNet or Storage level network isolation features. If you are just interesting in testing out how Functions connects to Dataverse as Managed Identity start here.
2. Azure Function in Elastic Premium with only Service Endpoints and VNet - Storage account is isolated to the VNet and Azure Functions traffic to Storage Account goes via the VNet using public Internet. This is the entry level security in terms of Network traffic. Azure Functions also connects to Storage Account using the Function App's System Assigned Managed Identity. This is controlled by the _createVNet_ parameter.
3. Azure Function in Elastic Premium with Private Endpoints - Storage Account is isolated to the VNet. Function App communicates with Storage Account using VNet over Private Link connection. Traffic in Private Link goes through Microsoft Backbone not via public internet. Traffic to the Function App still is over the public internet. This is controlled by the _createPrivateLink_ parameter.

This repo has azd [posthooks](hooks/postprovision.ps1) setup. So, the newly provisioned Function App will be automatically added as an Application User with System Administrator role using `pac admin assign-user`.

# Architecture

![Architecture](./images/architecture.png)