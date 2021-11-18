using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
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
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();

            builder.Services.AddOptions<FunctionSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("PowerApps").Bind(settings);
                });
            builder.Services.AddSingleton<IOrganizationService, ServiceClient>(x =>
            {
                var cache = x.GetService<IMemoryCache>();
                var environment = Environment.GetEnvironmentVariable("PowerApps:EnvironmentUrl");
                var managedIdentity = new DefaultAzureCredential(); //This works locally and live as well. Locally, it uses the account on Visual Studio, VSCode, Az CLI
                return new ServiceClient(
                        tokenProviderFunction: f => GetToken(environment, managedIdentity, cache),
                        instanceUrl: new Uri(environment));
            });
        }

        private async Task<string> GetToken(string environment, DefaultAzureCredential credential, IMemoryCache cache)
        {
            if (!cache.TryGetValue(environment, out AccessToken token))
            {
                token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
                cache.Set(environment, token, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(50) });
            }
            return token.Token;
        }
    }
}
