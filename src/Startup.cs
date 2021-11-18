using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(PowerAppsManagedIdentityDemoFunctions.Startup))]
namespace PowerAppsManagedIdentityDemoFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddOptions<FunctionSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("PowerApps").Bind(settings);
                });
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IOrganizationService, ServiceClient>(x =>
            {
                var environment = Environment.GetEnvironmentVariable("PowerApps:EnvironmentUrl");
                var managedIdentity = new DefaultAzureCredential(); //This works locally and live as well. Locally, it uses the account on Visual Studio, VSCode, Az CLI
                return new ServiceClient(
                        tokenProviderFunction: f => GetToken(environment, managedIdentity),
                        instanceUrl: new Uri(environment));
            });
        }

        private async Task<string> GetToken(string environment, DefaultAzureCredential credential)
        {
            var token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
            return token.Token;
        }
    }
}
