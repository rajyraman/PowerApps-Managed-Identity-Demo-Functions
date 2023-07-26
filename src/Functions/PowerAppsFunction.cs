using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using PowerAppsManagedIdentityDemoFunctions.Functions.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
            _client = httpClientFactory.CreateClient("PowerAppsClient");
            _settings = options.Value;
            _cache = cache;
        }

        [FunctionName("EntityMetadata")]
        [OpenApiOperation(operationId: "EntityMetadata", tags: "PowerApps", Description = "Get Details about an entity (Managed Identity)", Summary = "Get Details about an entity (Managed Identity)")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "entityName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The entity to retrieve metadata for")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Response with entity metadata")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Invalid entity")]
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
                        return _serviceClient.GetEntityMetadata(entityName);
                    });
            if (entityMetaData == null)
                return new BadRequestObjectResult($"{entityName} does not exist");

            return new OkObjectResult(entityMetaData);

        }

        [FunctionName("WhoAmI")]
        [OpenApiOperation(operationId: "WhoAmI", tags: "PowerApps", Description = "Get details about current Managed Identity user", Summary = "Get details about current Managed Identity user using raw HTTP")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Response with userId, organization and business unit")]
        public async Task<ActionResult> WhoAmI(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed WhoAmI request.");

            //Auth, Base Url is handled on the Startup during instantiation of HTTPClient
            var whoAmIResponse = await (await _client.GetAsync($"WhoAmI()")).Content.ReadAsStringAsync();
            return new OkObjectResult(JsonConvert.DeserializeObject(whoAmIResponse));
        }

        [FunctionName("WebAPIRaw")]
        [OpenApiOperation(operationId: "WebAPIRaw", tags: "PowerApps", Description = "Do a raw GET WebAPI Request", Summary = "Do a raw GET WebAPI Request")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RawWebApiGetRequestModel), Required = true, Description = "GET Uri to do the WebAPI call", Example = typeof(WebAPIGetExample))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "WebAPI Response")]
        public async Task<ActionResult> WebAPIRaw(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed WhoAmI request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<RawWebApiGetRequestModel>(requestBody);

            //Auth, Base Url is handled on the Startup during instantiation of HTTPClient
            var whoAmIResponse = await (await _client.GetAsync(request.Uri)).Content.ReadAsStringAsync();
            return new OkObjectResult(JsonConvert.DeserializeObject(whoAmIResponse));
        }

        [FunctionName("ExecuteFetchXML")]
        [OpenApiOperation(operationId: "ExecuteFetchXML", tags: "PowerApps", Description = "Execute FetchXML query", Summary = "Execute FetchXML query")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FetchXMLRequest), Required = true, Description = "FetchXML query to execute")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Response with entity records")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Invalid FetchXML")]
        public async Task<ActionResult> ExecuteFetchXML(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed FetchXML request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<FetchXMLRequest>(requestBody);
            if (string.IsNullOrEmpty(request.FetchXML)) return new BadRequestObjectResult(new InvalidRequestModel { Reason = "FetchXML is required" });

            var entities = _serviceClient.GetEntityDataByFetchSearch(request.FetchXML);
            return new OkObjectResult(entities);
        }
    }

    public class FetchXMLRequest
    {
        public string FetchXML { get; set; }
    }
    public class RawWebApiGetRequestModel
    {
        public string Uri { get; set; }
    }
    public class InvalidRequestModel
    {
        public string Reason { get; set; }
    }
}

