using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Core;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace PowerAppsManagedIdentityDemoFunctions.Functions
{
    public class WhoAmIFunction
    {
        private readonly DefaultAzureCredential _managedIdentity;
        private readonly HttpClient _client;
        private readonly FunctionSettings _settings;
        public WhoAmIFunction(DefaultAzureCredential managedIdentity, IHttpClientFactory httpClientFactory, IOptions<FunctionSettings> options)
        {
            _managedIdentity = managedIdentity;
            _client = httpClientFactory.CreateClient();
            _settings = options.Value;
        }

        [FunctionName("whoami")]
        [OpenApiOperation(operationId: "whoami", tags: "PowerApps", Description = "Get Details about current user (Managed Identity)", Summary = "Get Details about current user (Managed Identity)")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "Response with userId, environmentId")]
        public async Task<ActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "entity/{entityName}")] HttpRequest req,
            string entityName,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //Raw Token Request
            //string accessToken = (await _managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{_settings.EnvironmentUrl}/.default" }))).Token;
            //_client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            //var response = await _client.GetAsync($"{_settings.EnvironmentUrl}/api/data/v9.2/WhoAmI()");
            
            //Use Service Client, but use AZ Identity to get token
            var serviceClient = new ServiceClient(tokenProviderFunction: async u => 
                    (await _managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{_settings.EnvironmentUrl}/.default" }))).Token, instanceUrl: new Uri(_settings.EnvironmentUrl));
            var entityMetaData = serviceClient.GetEntityMetadata(entityName);
            return new OkObjectResult(entityMetaData);

        }
    }
}
