using Aliencube.AzureFunctions.Extensions.OpenApi;
using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Aliencube.AzureFunctions.Extensions.OpenApi.Configurations;
using Aliencube.AzureFunctions.Extensions.OpenApi.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Moodful.Functions
{
    public class Swagger
    {
        private static readonly OpenApiInfo openApiInfo = new OpenApiInfo
        {
            Version = "2.0.0",
            Title = "Open API Sample on Azure Functions",
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("http://opensource.org/licenses/MIT")
            }
        };

        private static OpenApiFormat ParseOpenApiFormatFromString(string input)
        {
            switch (input.ToLowerInvariant())
            {
                case "yml":
                case "yaml":
                    return OpenApiFormat.Yaml;
                case "json":
                    return OpenApiFormat.Json;
            }

            return OpenApiFormat.Json;
        }

        [FunctionName(nameof(RenderOpenApiDocument))]
        [OpenApiIgnore]
        public static async Task<IActionResult> RenderOpenApiDocument(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = "openapi/{extension?}")] HttpRequest req,
            ILogger log,
            string extension)
        {
            var ext = ParseOpenApiFormatFromString(extension);
            var helper = new DocumentHelper(new RouteConstraintFilter());
            var document = new Document(helper);
            var result = await document.InitialiseDocument()
                                       .AddMetadata(openApiInfo)
                                       .AddServer(req, "api")
                                       .Build(Assembly.GetExecutingAssembly())
                                       .RenderAsync(OpenApiSpecVersion.OpenApi3_0, ext)
                                       .ConfigureAwait(false);
            var response = new ContentResult()
            {
                Content = result,
                ContentType = "application/json",
                StatusCode = (int)HttpStatusCode.OK
            };

            return response;
        }

        [FunctionName(nameof(RenderSwaggerUI))]
        [OpenApiIgnore]
        public static async Task<IActionResult> RenderSwaggerUI(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "swagger/ui")] HttpRequest req,
            ILogger log)
        {
            var ui = new SwaggerUI();
            var result = await ui.AddMetadata(openApiInfo)
                                 .AddServer(req, "api")
                                 .BuildAsync()
                                 .RenderAsync("openapi/json", null)
                                 .ConfigureAwait(false);
            var response = new ContentResult()
            {
                Content = result,
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK
            };

            return response;
        }
    }
}
