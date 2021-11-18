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
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Extensions.Caching.Memory;

namespace PowerAppsManagedIdentityDemoFunctions.Functions
{
    public class PowerAppsFunction
    {
        private readonly ServiceClient _serviceClient;
        private readonly HttpClient _client;
        private readonly FunctionSettings _settings;
        private readonly IMemoryCache _cache;
        public PowerAppsFunction(IOrganizationService serviceClient, IHttpClientFactory httpClientFactory, IOptions<FunctionSettings> options, IMemoryCache cache)
        {
            _serviceClient = serviceClient as ServiceClient;
            _client = httpClientFactory.CreateClient();
            _settings = options.Value;
            _cache = cache;
        }

        [FunctionName("entitymetadata")]
        [OpenApiOperation(operationId: "entitymetadata", tags: "PowerApps", Description = "Get Details about an entity (Managed Identity)", Summary = "Get Details about an entity (Managed Identity)")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "entityName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The entity to retrieve metadata for")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "Response with entity metadata")]
        public ActionResult EntityMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "entity/{entityName}")] HttpRequest req,
            string entityName,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed entitymetadata request for {entityName}.");

            var entityMetaData = _cache.GetOrCreate(
                    entityName,
                    cacheEntry =>
                    {
                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                        log.LogInformation($"About to get metadata for {entityName}.");
                        return _serviceClient.GetEntityMetadata(entityName);  //Use Service Client, but use AZ Identity to get token
                    });
            return new OkObjectResult(entityMetaData);

        }

        [FunctionName("whoami")]
        [OpenApiOperation(operationId: "whoami", tags: "PowerApps", Description = "Get details about current Managed Identity user", Summary = "Get details about current Managed Identity user using raw HTTP")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "Response with userId, organization and business unit")]
        public async Task<ActionResult> WhoAmI(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed whoami request.");

            //Raw Token Request
            string accessToken = _serviceClient.CurrentAccessToken;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var whoAmIResponse = await (await _client.GetAsync($"{_settings.EnvironmentUrl}/api/data/v9.2/WhoAmI()")).Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(whoAmIResponse);
            return new OkObjectResult(new WebApiResponse { Response = data, Token = _serviceClient.CurrentAccessToken});
        }

        [FunctionName("fetchxml")]
        [OpenApiOperation(operationId: "fetchxml", tags: "PowerApps", Description = "Execute FetchXML query", Summary = "Execute FetchXML query")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FetchXMLRequest), Required = true, Description = "FetchXML query to execute")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Summary = "Response with entity records")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/json", bodyType: typeof(string), Summary = "Invalid FetchXML")]
        public async Task<ActionResult> FetchXML(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed FetchXML request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<FetchXMLRequest>(requestBody);
            if (string.IsNullOrEmpty(request.FetchXML)) return new BadRequestObjectResult(new InvalidRequest { Reason = "FetchXML is required" });

            var entities = _serviceClient.GetEntityDataByFetchSearch(request.FetchXML);
            return new OkObjectResult(entities);
        }
    }

    public class FetchXMLRequest
    {
        public string FetchXML { get; set; }
    }

    public class InvalidRequest 
    {
        public string Reason { get; set; }
    }

    public class WebApiResponse
    {
        public dynamic Response { get; set; }
        public string Token { get; set; }
    }
}
