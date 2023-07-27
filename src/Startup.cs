using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
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
            builder.Services.AddSingleton(new DefaultAzureCredential()); //DefaultAzureCredential locally and live as well. Locally, it uses the account on Visual Studio, VSCode, Az CLI
            builder.Services.AddOptions<FunctionSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("PowerApps").Bind(settings);
                });
            #region You need raw HTTP Client only if you are not going to use Dataverse Client. This is shown only as example.

            builder.Services.AddHttpClient("PowerAppsClient", async (provider, httpClient) =>
            {
                var managedIdentity = provider.GetRequiredService<DefaultAzureCredential>();
                var environment = Environment.GetEnvironmentVariable("DATAVERSE_URL");
                var cache = provider.GetService<IMemoryCache>();
                httpClient.BaseAddress = new Uri($"{environment}/api/data/v9.2/");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "azurefunction-powerapps");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, (await GetToken(environment, managedIdentity, cache)));

            });

            #endregion

            builder.Services.AddSingleton<IOrganizationService, ServiceClient>(provider =>
            {
                var managedIdentity = provider.GetRequiredService<DefaultAzureCredential>();
                var environment = Environment.GetEnvironmentVariable("DATAVERSE_URL");
                var cache = provider.GetService<IMemoryCache>();
                return new ServiceClient(
                        tokenProviderFunction: f => GetToken(environment, managedIdentity, cache),
                        instanceUrl: new Uri(environment),
                        useUniqueInstance: true);
            });
        }

        private async Task<string> GetToken(string environment, DefaultAzureCredential credential, IMemoryCache cache)
        {
            var accessToken = await cache.GetOrCreateAsync(environment, async (cacheEntry) =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(50);
                var token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
                return token;
            });
            return accessToken.Token;
        }
    }
}
