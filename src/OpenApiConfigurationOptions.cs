using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace PowerAppsManagedIdentityDemoFunctions
{
    public class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
    {
        public override OpenApiInfo Info { get; set; } = new OpenApiInfo()
        {
            Version = "3.0.0",
            Title = "OpenAPI Sample for using with Functions with Dataverse",
            TermsOfService = new Uri("https://github.com/rajyraman/PowerApps-Managed-Identity-Demo-Functions"),
            Contact = new OpenApiContact()
            {
                Name = "Natraj",
                Url = new Uri("https://github.com/rajyraman/PowerApps-Managed-Identity-Demo-Functions/issues"),
            }
        };

        public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
    }
}