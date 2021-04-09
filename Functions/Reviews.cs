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
    public class Reviews
    {
        private const string BasePath = nameof(Reviews);
        private readonly AuthenticationOptions SecurityOptions;

        public Reviews(IOptions<AuthenticationOptions> securityOptions)
        {
            SecurityOptions = securityOptions.Value;
        }

        [FunctionName(nameof(Reviews) + nameof(GetReviews))]
        [ProducesResponseType(typeof(IEnumerable<Review>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetReviews), nameof(Reviews))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<Review>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            var models = cloudTable.RetrieveEntities<ReviewTableEntity>(userId).Select(o => o.MapTo());
            if (models.Count() == 0)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(models);
        }

        [FunctionName(nameof(Reviews) + nameof(GetReviewsById))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetReviewsById), nameof(Reviews))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetReviewsById(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = "{userId}/" + BasePath + "/{id}")] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
            string userId,
            Guid id,
            ILogger logger)
        {
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            var model = cloudTable.RetrieveEntity<ReviewTableEntity>(userId, id.ToString()).MapTo();
            if (model == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(PostReviews))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationResponse>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        [OpenApiOperation(nameof(PostReviews), nameof(Reviews))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(IEnumerable<ValidationResponse>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.Conflict, "application/json", typeof(JObject))]
        public async Task<IActionResult> PostReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var result = await httpRequest.GetValidatedJsonBody<Review, ReviewValidator>();

                if (!result.IsValid)
                {
                    return result.ToBadRequest();
                }

                var model = cloudTable.CreateEntity(new ReviewTableEntity(userId, result.Value.Id.ToString(), result.Value));

                return new OkObjectResult(model);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
            {
                return new ConflictResult();
            }
        }

        [FunctionName(nameof(Reviews) + nameof(UpdateReviews))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationResponse>), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(UpdateReviews), nameof(Reviews))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(IEnumerable<ValidationResponse>))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(Review))]
        public async Task<IActionResult> UpdateReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
            string userId,
            ILogger logger)
        {
            if (await httpRequest.Authenticate(SecurityOptions, userId, logger) == AuthenticationStatus.Unauthenticated)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var result = await httpRequest.GetValidatedJsonBody<Review, ReviewValidator>();

                if (!result.IsValid)
                {
                    return result.ToBadRequest();
                }

                var model = cloudTable.UpdateEntity(new ReviewTableEntity(userId, result.Value.Id.ToString(), result.Value));

                return new OkObjectResult(model);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
        }

        [FunctionName(nameof(Reviews) + nameof(DeleteReviews))]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(DeleteReviews), nameof(Reviews))]
        [OpenApiParameter("userId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.BadRequest, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> DeleteReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Delete), Route = "{userId}/" + BasePath + "/{id}")] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
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
                var model = cloudTable.DeleteEntity<ReviewTableEntity>(userId, id.ToString()).MapTo();
                return new OkResult();
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
        }
    }
}
