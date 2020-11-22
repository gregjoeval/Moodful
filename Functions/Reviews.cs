using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Moodful.Authorization;
using Moodful.Configuration;
using Moodful.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Moodful.Functions
{
    public class Reviews
    {
        private const string BasePath = nameof(Reviews);
        private Security Security;
        private readonly AuthenticationOptions AuthenticationOptions;
        private static readonly Dictionary<string, Dictionary<Guid, Review>> ReviewCollection = new Dictionary<string, Dictionary<Guid, Review>>();

        public Reviews(IOptions<AuthenticationOptions> authenticationOptions)
        {
            AuthenticationOptions = authenticationOptions.Value;
        }

        private string GetUserIdFromHttpRequest(HttpRequest httpRequest)
        {
            var token = Security.GetJWTSecurityTokenFromHttpRequestAsync(httpRequest);
            var userId = token.Subject;
            return userId;
        }

        private static Dictionary<Guid, Review> GetReviewsByUserId(string userId)
        {
            if (ReviewCollection.TryGetValue(userId, out var reviews))
            {
                return reviews;
            }

            return null;
        }

        private static Dictionary<Guid, Review> UpsertReviewsByUserId(string userId, IEnumerable<Review> reviews)
        {
            ReviewCollection.TryGetValue(userId, out var existingModels);

            foreach(var model in reviews)
            {
                if (existingModels == null)
                {
                    existingModels = reviews.ToDictionary(o => o.Id);
                }
                else
                {
                    var wasAdded = existingModels.TryAdd(model.Id, model);
                    if (wasAdded == false)
                    {
                        var existingReview = existingModels[model.Id];

                        existingReview.LastModified = DateTimeOffset.UtcNow;
                        existingReview.Rating = model.Rating;
                        existingReview.Secret = model.Secret;
                        existingReview.TagIds = model.TagIds;
                        existingReview.Description = model.Description;

                        existingModels[model.Id] = existingReview;
                    }
                }
            }

            ReviewCollection[userId] = existingModels;

            return existingModels;
        }

        [FunctionName(nameof(Reviews) + nameof(GetReviews))]
        [ProducesResponseType(typeof(List<Review>), 200)]
        [OpenApiOperation(nameof(GetReviews), nameof(Reviews))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(List<Review>))]
        public async Task<IActionResult> GetReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath)] HttpRequest httpRequest,
            ILogger logger)
        {
            Security = new Security(logger, AuthenticationOptions);

            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var userId = GetUserIdFromHttpRequest(httpRequest);
            var reviews = GetReviewsByUserId(userId);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(reviews.ToList().Select(o => o.Value));
        }

        [FunctionName(nameof(Reviews) + nameof(GetReviewsById))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetReviewsById), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetReviewsById(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id,
            ILogger logger)
        {
            Security = new Security(logger, AuthenticationOptions);

            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var userId = GetUserIdFromHttpRequest(httpRequest);
            var reviews = GetReviewsByUserId(userId);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            var model = reviews.FirstOrDefault(o => o.Key == id).Value;
            if (model == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(PostReviews))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [OpenApiOperation(nameof(PostReviews), nameof(Reviews))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.Conflict, "application/json", typeof(JObject))]
        public async Task<IActionResult> PostReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = BasePath)] HttpRequest httpRequest,
            ILogger logger)
        {
            Security = new Security(logger, AuthenticationOptions);

            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Review>(requestBody);

            var userId = GetUserIdFromHttpRequest(httpRequest);
            var reviews = GetReviewsByUserId(userId);            
            if (reviews != null && reviews.ContainsKey(model.Id))
            {
                return new ConflictResult();
            }

            UpsertReviewsByUserId(userId, new[] { model });
            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(UpdateReviews))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(UpdateReviews), nameof(Reviews))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> UpdateReviews(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = BasePath)] HttpRequest httpRequest,
            ILogger logger)
        {
            Security = new Security(logger, AuthenticationOptions);

            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var userId = GetUserIdFromHttpRequest(httpRequest);
            var reviews = GetReviewsByUserId(userId);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Review>(requestBody);
            if (reviews?.FirstOrDefault(o => o.Key == model.Id) == null)
            {
                return new NotFoundResult();
            }

            UpsertReviewsByUserId(userId, new[] { model });

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(DeleteReviews))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(typeof(Review), 404)]
        [OpenApiOperation(nameof(DeleteReviews), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> DeleteReviews(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id,
            ILogger logger)
        {
            Security = new Security(logger, AuthenticationOptions);

            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var userId = GetUserIdFromHttpRequest(httpRequest);
            var reviews = GetReviewsByUserId(userId);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            var removed = reviews.Remove(id);
            if (!removed)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
