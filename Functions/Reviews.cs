using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.OpenApi.Models;
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
    public static class Reviews
    {
        private static readonly List<Review> ReviewList = new List<Review>();
        private const string BasePath = "reviews";

        [FunctionName(nameof(Reviews) + nameof(Get))]
        [ProducesResponseType(typeof(List<Review>), 200)]
        [OpenApiOperation(nameof(Get), nameof(Reviews))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(List<Review>))]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = BasePath)] HttpRequest httpRequest)
        {
            await Task.FromResult(0); // this is to disable the empty async warning
            return new OkObjectResult(ReviewList);
        }

        [FunctionName(nameof(Reviews) + nameof(GetById))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetById), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Review))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public static async Task<IActionResult> GetById(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            await Task.FromResult(0); // this is to disable the empty async warning
            var model = ReviewList.FirstOrDefault(o => o.Id == id);
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
        public static async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Post), Route = BasePath)] HttpRequest httpRequest)
        {
            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Review>(requestBody);
            if (ReviewList.FirstOrDefault(o => o.Id == model.Id) != null)
            {
                return new Microsoft.AspNetCore.Mvc.ConflictResult();
            }

            ReviewList.Add(model);
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
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Put), Route = BasePath)] HttpRequest httpRequest)
        {
            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Review>(requestBody);
            var index = ReviewList.FindIndex(o => o.Id == model.Id);
            if (index == -1)
            {
                return new NotFoundResult();
            }

            var review = ReviewList[index];
            review.LastEdited = DateTimeOffset.UtcNow;
            review.Rating = model.Rating;
            review.Tags = model.Tags;
            review.Description = model.Description;
            ReviewList[index] = review;

            return new OkObjectResult(review);
        }

        [FunctionName(nameof(Reviews) + nameof(Delete))]
        [ProducesResponseType(typeof(Review), 200)]
        [ProducesResponseType(typeof(Review), 404)]
        [OpenApiOperation(nameof(Delete), nameof(Reviews))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            await Task.FromResult(0); // this is to disable the empty async warning
            var count = ReviewList.RemoveAll(o => o.Id == id);
            if (count == 0)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
