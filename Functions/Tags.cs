using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moodful.Configuration;
using Moodful.Extensions;
using Moodful.Models;
using Moodful.TableEntities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Moodful.Functions
{
    public class Tags
    {
        private const string BasePath = nameof(Tags);
        private readonly AuthenticationOptions SecurityOptions;

        public Tags(IOptions<AuthenticationOptions> securityOptions)
        {
            SecurityOptions = securityOptions.Value;
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
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            var models = cloudTable.RetrieveEntities<TagTableEntity>(userId).Select(o => o.MapTo());
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
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            var model = cloudTable.RetrieveEntity<TagTableEntity>(userId, id.ToString()).MapTo();
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
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
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

                var model = cloudTable.CreateEntity(new TagTableEntity(userId, result.Value.Id.ToString(), result.Value)).MapTo();

                return new OkObjectResult(model);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
            {
                return new ConflictResult();
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
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
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

                var model = cloudTable.UpdateEntity(new TagTableEntity(userId, result.Value.Id.ToString(), result.Value));

                return new OkObjectResult(model);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
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
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var model = cloudTable.DeleteEntity<TagTableEntity>(userId, id.ToString()).MapTo();
                return new OkResult();
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
        }
    }
}
