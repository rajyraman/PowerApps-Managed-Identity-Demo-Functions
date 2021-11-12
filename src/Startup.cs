using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;

[assembly: FunctionsStartup(typeof(PowerAppsManagedIdentityDemoFunctions.Startup))]
namespace PowerAppsManagedIdentityDemoFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton(x => new DefaultAzureCredential());
            builder.Services.AddOptions<FunctionSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("PowerApps").Bind(settings);
                });
        }
    }
}
