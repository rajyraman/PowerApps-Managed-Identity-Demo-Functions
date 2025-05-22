using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;

namespace PowerAppsManagedIdentityDemoFunctions.Functions.Models
{
    public class WebAPIGetExample : OpenApiExample<RawWebApiGetRequestModel>
    {
        public override IOpenApiExample<RawWebApiGetRequestModel> Build(NamingStrategy namingStrategy = null)
        {
            this.Examples.Add(
                OpenApiExampleResolver.Resolve(
                    "Top 1 Contact",
                    new RawWebApiGetRequestModel() { Uri = "contacts?$top=1" },
                    namingStrategy
                ));
            this.Examples.Add(
                OpenApiExampleResolver.Resolve(
                    "Top 10 Account with only name and accountid",
                    new RawWebApiGetRequestModel() { Uri = "accounts?$top=1&$select=name,accountid" },
                    namingStrategy
                ));

            return this;
        }
    }
}

