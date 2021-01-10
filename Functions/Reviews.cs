using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moodful.Services.Security;
using Moodful.Configuration;
using Moodful.Models;
using Moodful.Services.Storage;
using Moodful.Services.Storage.TableEntities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Moodful.Extensions;

namespace Moodful.Functions
{
    public class Reviews
    {
        private const string BasePath = nameof(Reviews);
        private Security Security;
        private readonly SecurityOptions SecurityOptions;
        private StorageService<ReviewTableEntity, Review> StorageService;

        public Reviews(IOptions<SecurityOptions> securityOptions, IMapper mapper)
        {
            SecurityOptions = securityOptions.Value;
            StorageService = new StorageService<ReviewTableEntity, Review>(mapper);
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
            Security = new Security(logger, SecurityOptions);

            if (await Security.AuthenticateHttpRequestAsync(httpRequest, userId) == SecurityStatus.UnAuthenticated)
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
        [OpenApiResponseBody(HttpStatusCode.Unauthorized, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> UpdateReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = "{userId}/" + BasePath)] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
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
                var result = await httpRequest.GetValidatedJsonBody<Review, ReviewValidator>();

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
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = "{userId}/" + BasePath + "/{id}")] HttpRequest httpRequest,
            [Table(TableNames.Review)] CloudTable cloudTable,
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
