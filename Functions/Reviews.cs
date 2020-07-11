using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.OpenApi.Models;
using Moodful.Authorization;
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
        private Security _security;
        private static readonly Dictionary<string, Dictionary<Guid, Review>> ReviewCollection = new Dictionary<string, Dictionary<Guid, Review>>();
        private const string BasePath = "reviews";

        public Reviews()
        {
            _security = new Security();
        }

        private static string GetUserIdFromHttpRequest(HttpRequest httpRequest)
        {
            var token = Security.GetJWTSecurityTokenFromHttpRequestAsync(httpRequest);
            var userId = token.Subject;
            return userId;
        }

        private static Dictionary<Guid, Review> GetReviewsByUserIdFromHttpRequest(HttpRequest httpRequest)
        {
            var userId = GetUserIdFromHttpRequest(httpRequest);
            if (ReviewCollection.TryGetValue(userId, out var reviews))
            {
                return reviews;
            }

            return null;
        }

        private static Dictionary<Guid, Review> UpsertReviewsByUserIdFromHttpRequest(HttpRequest httpRequest, IEnumerable<Review> reviews)
        {
            var userId = GetUserIdFromHttpRequest(httpRequest);
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

                        existingReview.LastEdited = DateTimeOffset.UtcNow;
                        existingReview.Rating = model.Rating;
                        existingReview.Tags = model.Tags;
                        existingReview.Description = model.Description;

                        existingModels[model.Id] = existingReview;
                    }
                }
            }

            ReviewCollection[userId] = existingModels;

            return existingModels;
        }

        [FunctionName(nameof(Reviews) + nameof(Get))]
        [ProducesResponseType(typeof(List<Review>), 200)]
        [OpenApiOperation(nameof(Get), nameof(Reviews))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(List<Review>))]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await _security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetReviewsByUserIdFromHttpRequest(httpRequest);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(reviews.ToList());
        }

        [FunctionName(nameof(Reviews) + nameof(GetById))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetById), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> GetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            var claimsPrincipal = await _security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetReviewsByUserIdFromHttpRequest(httpRequest);
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

        [FunctionName(nameof(Reviews) + nameof(Post))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [OpenApiOperation(nameof(Post), nameof(Reviews))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.Conflict, "application/json", typeof(JObject))]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await _security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Review>(requestBody);

            var reviews = GetReviewsByUserIdFromHttpRequest(httpRequest);            
            if (reviews != null && reviews.ContainsKey(model.Id))
            {
                return new ConflictResult();
            }

            UpsertReviewsByUserIdFromHttpRequest(httpRequest, new[] { model });
            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(Update))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(Update), nameof(Reviews))]
        [OpenApiRequestBody("application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await _security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetReviewsByUserIdFromHttpRequest(httpRequest);
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

            UpsertReviewsByUserIdFromHttpRequest(httpRequest, new[] { model });

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Reviews) + nameof(Delete))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(typeof(Review), 404)]
        [OpenApiOperation(nameof(Delete), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            var claimsPrincipal = await _security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetReviewsByUserIdFromHttpRequest(httpRequest);
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
