using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System;
using System.Threading.Tasks;
using PowerAppsManagedIdentityDemoFunctions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.AddSingleton(new DefaultAzureCredential());

        services.AddOptions<FunctionSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("PowerApps").Bind(settings);
            });

        // HTTP Client for raw API calls
        services.AddHttpClient("PowerAppsClient", async (provider, httpClient) =>
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

        // Dataverse ServiceClient
        services.AddSingleton<IOrganizationService, ServiceClient>(provider =>
        {
            var managedIdentity = provider.GetRequiredService<DefaultAzureCredential>();
            var environment = Environment.GetEnvironmentVariable("DATAVERSE_URL");
            var cache = provider.GetService<IMemoryCache>();
            return new ServiceClient(
                    tokenProviderFunction: f => GetToken(environment, managedIdentity, cache),
                    instanceUrl: new Uri(environment),
                    useUniqueInstance: true);
        });
    })
    .Build();

host.Run();

async Task<string> GetToken(string environment, DefaultAzureCredential credential, IMemoryCache cache)
{
    var accessToken = await cache.GetOrCreateAsync(environment, async (cacheEntry) =>
    {
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(50);
        var token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
        return token;
    });
    return accessToken.Token;
}
