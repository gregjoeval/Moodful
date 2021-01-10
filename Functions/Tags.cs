using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moodful.Services.Security;
using Moodful.Configuration;
using Moodful.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Moodful.Services.Storage;
using Moodful.Services.Storage.TableEntities;
using AutoMapper;
using Microsoft.Azure.Cosmos.Table;
using Moodful.Extensions;

namespace Moodful.Functions
{
    public class Tags
    {
        private const string BasePath = nameof(Tags);
        private Security Security;
        private readonly SecurityOptions SecurityOptions;
        private StorageService<TagTableEntity, Tag> StorageService;

        public Tags(IOptions<SecurityOptions> securityOptions, IMapper mapper)
        {
            SecurityOptions = securityOptions.Value;
            StorageService = new StorageService<TagTableEntity, Tag>(mapper);
        }

        [FunctionName(nameof(Tags) + nameof(GetTags))]
        [ProducesResponseType(typeof(IEnumerable<Tag>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetTags), nameof(Tags))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<Tag>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Tag)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
            {
                return new UnauthorizedResult();
            }

            var models = StorageService.Retrieve(cloudTable, userId);
            if (models.Count() == 0)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(models);
        }

        [FunctionName(nameof(Tags) + nameof(GetTagsById))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetTagsById), nameof(Tags))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetTagsById(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "{userId}/" + BasePath + "/{id}")] HttpRequest httpRequest,
            [Table(TableNames.Tag)] CloudTable cloudTable,
            string userId,
            Guid id,
            ILogger logger)
        {
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
            {
                return new UnauthorizedResult();
            }

            var model = StorageService.RetrieveById(cloudTable, userId, id.ToString());
            if (model == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Tags) + nameof(PostTags))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationResponse>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        [OpenApiOperation(nameof(PostTags), nameof(Tags))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(IEnumerable<ValidationResponse>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.Conflict, "application/json", typeof(JObject))]
        public async Task<IActionResult> PostTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Tag)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var result = await httpRequest.GetValidatedJsonBody<Tag, TagValidator>();

                if (!result.IsValid)
                {
                    return result.ToBadRequest();
                }

                var model = StorageService.Create(cloudTable, userId, result.Value);

                return new OkObjectResult(model);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 409)
                {
                    return new ConflictResult();
                }

                return new BadRequestResult(); // TODO: lets default to bad request for now but will probably need to change this
            }
        }

        [FunctionName(nameof(Tags) + nameof(UpdateTags))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationResponse>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(UpdateTags), nameof(Tags))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(IEnumerable<ValidationResponse>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> UpdateTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Tag)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var result = await httpRequest.GetValidatedJsonBody<Tag, TagValidator>();

                if (!result.IsValid)
                {
                    return result.ToBadRequest();
                }

                var model = StorageService.Update(cloudTable, userId, result.Value);

                return new OkObjectResult(model);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return new NotFoundResult();
                }

                return new BadRequestResult(); // TODO: lets default to bad request for now but will probably need to change this
            }
        }

        [FunctionName(nameof(Tags) + nameof(DeleteTags))]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(DeleteTags), nameof(Tags))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> DeleteTags(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = "{userId}/" + BasePath + "/{id}")] HttpRequest httpRequest,
            [Table(TableNames.Tag)] CloudTable cloudTable,
            string userId,
            Guid id,
            ILogger logger)
        {
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var model = StorageService.Delete(cloudTable, userId, id.ToString());
                return new OkResult();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return new NotFoundResult();
                }

                return new BadRequestResult(); // TODO: lets default to bad request for now but will probably need to change this
            }
        }
    }
}
