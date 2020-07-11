﻿using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;
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
using System.Text;
using System.Threading.Tasks;

namespace Moodful.Functions
{
    public class Tags
    {
        private static readonly Dictionary<string, Dictionary<Guid, Tag>> TagCollection = new Dictionary<string, Dictionary<Guid, Tag>>();
        private const string BasePath = "tags";

        private static string GetUserIdFromHttpRequest(HttpRequest httpRequest)
        {
            var token = Security.GetJWTSecurityTokenFromHttpRequestAsync(httpRequest);
            var userId = token.Subject;
            return userId;
        }

        private static Dictionary<Guid, Tag> GetTagsByUserIdFromHttpRequest(HttpRequest httpRequest)
        {
            var userId = GetUserIdFromHttpRequest(httpRequest);
            if (TagCollection.TryGetValue(userId, out var tags))
            {
                return tags;
            }

            return null;
        }

        private static Dictionary<Guid, Tag> UpsertTagsByUserIdFromHttpRequest(HttpRequest httpRequest, IEnumerable<Tag> tags)
        {
            var userId = GetUserIdFromHttpRequest(httpRequest);
            TagCollection.TryGetValue(userId, out var existingModels);

            foreach (var model in tags)
            {
                if (existingModels == null)
                {
                    existingModels = tags.ToDictionary(o => o.Id);
                }
                else
                {
                    var wasAdded = existingModels.TryAdd(model.Id, model);
                    if (wasAdded == false)
                    {
                        var existingTag = existingModels[model.Id];

                        existingTag.LastEdited = DateTimeOffset.UtcNow;
                        existingTag.Title = model.Title;

                        existingModels[model.Id] = existingTag;
                    }
                }
            }

            TagCollection[userId] = existingModels;

            return existingModels;
        }

        [FunctionName(nameof(Tags) + nameof(Get))]
        [ProducesResponseType(typeof(List<Tag>), 200)]
        [OpenApiOperation(nameof(Get), nameof(Tags))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(List<Tag>))]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var tags = GetTagsByUserIdFromHttpRequest(httpRequest);
            if (tags == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(tags.ToList());
        }

        [FunctionName(nameof(Tags) + nameof(GetById))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(GetById), nameof(Tags))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public static async Task<IActionResult> GetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Get), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetTagsByUserIdFromHttpRequest(httpRequest);
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

        [FunctionName(nameof(Tags) + nameof(Post))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [OpenApiOperation(nameof(Post), nameof(Tags))]
        [OpenApiRequestBody("application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.Conflict, "application/json", typeof(JObject))]
        public static async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Post), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Tag>(requestBody);

            var reviews = GetTagsByUserIdFromHttpRequest(httpRequest);
            if (reviews != null && reviews.ContainsKey(model.Id))
            {
                return new ConflictResult();
            }

            UpsertTagsByUserIdFromHttpRequest(httpRequest, new[] { model });
            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Tags) + nameof(Update))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [OpenApiOperation(nameof(Update), nameof(Tags))]
        [OpenApiRequestBody("application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(Tag))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethods.Put), Route = BasePath)] HttpRequest httpRequest)
        {
            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetTagsByUserIdFromHttpRequest(httpRequest);
            if (reviews == null)
            {
                return new NotFoundResult();
            }

            string requestBody = await new StreamReader(httpRequest.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<Tag>(requestBody);
            if (reviews?.FirstOrDefault(o => o.Key == model.Id) == null)
            {
                return new NotFoundResult();
            }

            UpsertTagsByUserIdFromHttpRequest(httpRequest, new[] { model });

            return new OkObjectResult(model);
        }

        [FunctionName(nameof(Tags) + nameof(Delete))]
        [ProducesResponseType(typeof(Tag), 200)]
        [ProducesResponseType(typeof(Tag), 404)]
        [OpenApiOperation(nameof(Delete), nameof(Tags))]
        [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseBody(HttpStatusCode.OK, "application/json", typeof(JObject))]
        [OpenApiResponseBody(HttpStatusCode.NotFound, "application/json", typeof(JObject))]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Delete), Route = BasePath + "/{id}")] HttpRequest httpRequest,
            Guid id)
        {
            var claimsPrincipal = await Security.AuthenticateHttpRequestAsync(httpRequest);
            if (claimsPrincipal == null)
            {
                return new UnauthorizedResult();
            }

            var reviews = GetTagsByUserIdFromHttpRequest(httpRequest);
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
