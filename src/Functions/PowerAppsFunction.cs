using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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

        [Function("EntityMetadata")]
        [OpenApiOperation(operationId: "EntityMetadata", tags: new[] { "PowerApps" }, Description = "Get Details about an entity (Managed Identity)", Summary = "Get Details about an entity (Managed Identity)")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "entityName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The entity to retrieve metadata for")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Response with entity metadata")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Invalid entity")]
        public HttpResponseData EntityMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "entity/{entityName}")] HttpRequestData req,
            string entityName,
            FunctionContext context)
        {
            var log = context.GetLogger<PowerAppsFunction>();
            log.LogInformation($"C# HTTP trigger function processed entitymetadata request for {entityName}.");

            var entityMetaData = _cache.GetOrCreate(
                    entityName,
                    cacheEntry =>
                    {
                        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                        log.LogInformation($"About to get metadata for {entityName}.");
                        return _serviceClient.GetEntityMetadata(entityName);
                    });
            
            var response = req.CreateResponse();
            
            if (entityMetaData == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.WriteString($"{entityName} does not exist");
                return response;
            }
            
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonConvert.SerializeObject(entityMetaData));
            return response;
        }

        [Function("WhoAmI")]
        [OpenApiOperation(operationId: "WhoAmI", tags: new[] { "PowerApps" }, Description = "Get details about current Managed Identity user", Summary = "Get details about current Managed Identity user using raw HTTP")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Response with userId, organization and business unit")]
        public async Task<HttpResponseData> WhoAmI(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger<PowerAppsFunction>();
            log.LogInformation("C# HTTP trigger function processed WhoAmI request.");

            //Auth, Base Url is handled on the Program.cs during instantiation of HTTPClient
            var whoAmIResponse = await (await _client.GetAsync($"WhoAmI()")).Content.ReadAsStringAsync();
            
            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(whoAmIResponse);
            return response;
        }

        [Function("WebAPIRaw")]
        [OpenApiOperation(operationId: "WebAPIRaw", tags: new[] { "PowerApps" }, Description = "Do a raw GET WebAPI Request", Summary = "Do a raw GET WebAPI Request")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RawWebApiGetRequestModel), Required = true, Description = "GET Uri to do the WebAPI call", Example = typeof(WebAPIGetExample))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "WebAPI Response")]
        public async Task<HttpResponseData> WebAPIRaw(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger<PowerAppsFunction>();
            log.LogInformation("C# HTTP trigger function processed WebAPIRaw request.");
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<RawWebApiGetRequestModel>(requestBody);

            //Auth, Base Url is handled on the Program.cs during instantiation of HTTPClient
            var response = req.CreateResponse();
            
            try 
            {
                var apiResponse = await (await _client.GetAsync(request.Uri)).Content.ReadAsStringAsync();
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(apiResponse);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error executing WebAPI request");
                response.StatusCode = HttpStatusCode.BadRequest;
                response.WriteString($"Error: {ex.Message}");
            }
            
            return response;
        }

        [Function("ExecuteFetchXML")]
        [OpenApiOperation(operationId: "ExecuteFetchXML", tags: new[] { "PowerApps" }, Description = "Execute FetchXML query", Summary = "Execute FetchXML query")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FetchXMLRequest), Required = true, Description = "FetchXML query to execute")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "Response with entity records")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Invalid FetchXML")]
        public async Task<HttpResponseData> ExecuteFetchXML(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger<PowerAppsFunction>();
            log.LogInformation($"C# HTTP trigger function processed FetchXML request.");
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<FetchXMLRequest>(requestBody);
            
            var response = req.CreateResponse();
            
            if (string.IsNullOrEmpty(request.FetchXML)) 
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.WriteString(JsonConvert.SerializeObject(new InvalidRequestModel { Reason = "FetchXML is required" }));
                return response;
            }

            try
            {
                var entities = _serviceClient.GetEntityDataByFetchSearch(request.FetchXML);
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(JsonConvert.SerializeObject(entities));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error executing FetchXML");
                response.StatusCode = HttpStatusCode.BadRequest;
                response.WriteString($"Error: {ex.Message}");
            }
            
            return response;
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

