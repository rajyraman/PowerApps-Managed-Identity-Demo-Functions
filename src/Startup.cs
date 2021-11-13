using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System;

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
            builder.Services.AddSingleton<IOrganizationService, ServiceClient>(x =>
            {
                var managedIdentity = new DefaultAzureCredential();
                var environment = Environment.GetEnvironmentVariable("PowerApps:EnvironmentUrl");
                return new ServiceClient(tokenProviderFunction: async u =>
                    (await managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" }))).Token, instanceUrl: new Uri(environment));
            });
        }
    }
}
