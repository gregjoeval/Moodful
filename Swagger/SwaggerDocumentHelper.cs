using Aliencube.AzureFunctions.Extensions.OpenApi;
using Aliencube.AzureFunctions.Extensions.OpenApi.Abstractions;
using Aliencube.AzureFunctions.Extensions.OpenApi.Configurations;
using Microsoft.Azure.WebJobs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Moodful.Swagger
{
    public class SwaggerDocumentHelper : IDocumentHelper
    {
        private readonly DocumentHelper _documentHelper;

        public SwaggerDocumentHelper()
        {
            _documentHelper = new DocumentHelper(new RouteConstraintFilter());
        }

        public FunctionNameAttribute GetFunctionNameAttribute(MethodInfo element)
        {
            return _documentHelper.GetFunctionNameAttribute(element);
        }

        public string GetHttpEndpoint(FunctionNameAttribute function, HttpTriggerAttribute trigger)
        {
            return _documentHelper.GetHttpEndpoint(function, trigger);
        }

        public HttpTriggerAttribute GetHttpTriggerAttribute(MethodInfo element)
        {
            return _documentHelper.GetHttpTriggerAttribute(element);
        }

        public List<MethodInfo> GetHttpTriggerMethods(Assembly assembly)
        {
            // TODO perhaps more betterer is to just load the entry assembly since there is only one of functions
            var dealerPolicyAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var methodInfos = dealerPolicyAssemblies.SelectMany(_documentHelper.GetHttpTriggerMethods).ToList();
            return methodInfos;
        }

        public OperationType GetHttpVerb(HttpTriggerAttribute trigger)
        {
            return _documentHelper.GetHttpVerb(trigger);
        }

        public OpenApiOperation GetOpenApiOperation(MethodInfo element, FunctionNameAttribute function, OperationType verb)
        {
            return _documentHelper.GetOpenApiOperation(element, function, verb);
        }

        public virtual List<OpenApiParameter> GetOpenApiParameters(MethodInfo element, HttpTriggerAttribute trigger)
        {
            return _documentHelper.GetOpenApiParameters(element, trigger);
        }

        public OpenApiPathItem GetOpenApiPath(string path, OpenApiPaths paths)
        {
            return _documentHelper.GetOpenApiPath(path, paths);
        }

        public OpenApiRequestBody GetOpenApiRequestBody(MethodInfo element, NamingStrategy namingStrategy = null)
        {
            return _documentHelper.GetOpenApiRequestBody(element, namingStrategy);
        }

        public OpenApiResponses GetOpenApiResponseBody(MethodInfo element, NamingStrategy namingStrategy = null)
        {
            return _documentHelper.GetOpenApiResponseBody(element, namingStrategy);
        }

        // HACK virtual so you can override and fix how swagger generates strings for enums and arrays for IEnumerables
        public virtual Dictionary<string, OpenApiSchema> GetOpenApiSchemas(List<MethodInfo> elements, NamingStrategy namingStrategy)
        {
            // NOTE: this will cause a stack overflow if any of the models have recursive properties (e.g. SimpleError)
            return _documentHelper.GetOpenApiSchemas(elements, namingStrategy);
        }

        public Dictionary<string, OpenApiSecurityScheme> GetOpenApiSecuritySchemes()
        {
            ////return _documentHelper.GetOpenApiSecuritySchemes();
            return new Dictionary<string, OpenApiSecurityScheme>
            {
                {
                    "Bearer", 
                    new OpenApiSecurityScheme 
                    { 
                        Name = "Authorization", 
                        Type = SecuritySchemeType.ApiKey, 
                        In = ParameterLocation.Header, 
                        Description = "Add your bearer token authorization header (including the 'Bearer ' prefix).",
                        Scheme = "Bearer",
                        BearerFormat = "JWT"
                    } 
                }
            };
        }

        protected static void FixEnumParameter<TEnum>(HttpTriggerAttribute trigger, List<OpenApiParameter> parameters, HttpMethod httpMethod, string route, ParameterLocation location, string name)
          where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }

            FixParameter(trigger, parameters, httpMethod, route, location, name, (parameter) =>
            {
                parameter.Schema = new OpenApiSchema { Enum = GetEnumValues<TEnum>(), Type = "string", Format = "string" };
            });
        }

        protected static void MakeParameterString(HttpTriggerAttribute trigger, List<OpenApiParameter> parameters, HttpMethod httpMethod, string route, ParameterLocation location, string name)
        {
            FixParameter(trigger, parameters, httpMethod, route, location, name, (parameter) =>
            {
                parameter.Schema = new OpenApiSchema { Type = "string", Format = "string" };
            });
        }

        protected static void FixParameter(HttpTriggerAttribute trigger, List<OpenApiParameter> parameters, HttpMethod httpMethod, string route, ParameterLocation location, string name, Action<OpenApiParameter> fix)
        {
            if (trigger.Route != route || !trigger.Methods.Contains(httpMethod.ToString()))
            {
                return;
            }

            var parameter = parameters.First(p => p.In == location && p.Name == name);
            fix(parameter);
        }

        protected static void FixEnumProperty<T, TEnum>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName)
          where T : class
          where TEnum : struct, IConvertible
        {
            FixEnumProperty<T, TEnum>(schemas, namingStrategy, propertyName, SelectRootSchema());
        }

        protected static void FixEnumProperty<T, TEnum>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName, Func<OpenApiSchema, NamingStrategy, OpenApiSchema> schemaSelector)
          where T : class
          where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }

            FixProperty<T>(schemas, namingStrategy, propertyName, schemaSelector, (property) =>
            {
                property.Enum = GetEnumValues<TEnum>();
                property.Type = "string";
                property.Format = "string";
                return property;
            });
        }

        protected static void FixIEnumerableProperty<T, TProperty>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName)
        {
            FixIEnumerableProperty<T, TProperty>(schemas, namingStrategy, propertyName, SelectRootSchema());
        }

        protected static void FixIEnumerableProperty<T, TProperty>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName, Func<OpenApiSchema, NamingStrategy, OpenApiSchema> schemaSelector)
        {
            // TODO check property is IEnumerable<T>
            // TODO support enum, other types via TProperty
            FixProperty<T>(schemas, namingStrategy, propertyName, schemaSelector, (property) =>
            {
                property.Type = "array";
                property.Items = new OpenApiSchema { Type = "string", UniqueItems = false };
                return property;
            });
        }

        protected static void MakePropertyString<T>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName)
        {
            MakePropertyString<T>(schemas, namingStrategy, propertyName, SelectRootSchema());
        }

        protected static void MakePropertyString<T>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName, Func<OpenApiSchema, NamingStrategy, OpenApiSchema> schemaSelector)
        {
            FixProperty<T>(schemas, namingStrategy, propertyName, schemaSelector, (property) =>
            {
                return new OpenApiSchema { Type = "string", Format = "string" };
            });
        }

        protected static void FixProperty<T>(Dictionary<string, OpenApiSchema> schemas, NamingStrategy namingStrategy, string propertyName, Func<OpenApiSchema, NamingStrategy, OpenApiSchema> schemaSelector, Func<OpenApiSchema, OpenApiSchema> fix)
        {
            var t = typeof(T);
            var rootSchema = schemas[t.Name];
            var schema = schemaSelector(rootSchema, namingStrategy);
            var property = schema.Properties[namingStrategy.GetPropertyName(propertyName, false)];
            schema.Properties[namingStrategy.GetPropertyName(propertyName, false)] = fix(property);
        }

        private static IList<IOpenApiAny> GetEnumValues<T>()
        {
            return ((T[])Enum.GetValues(typeof(T)))
            .Select(t => new OpenApiString(t.ToString()))
            .Cast<IOpenApiAny>()
            .ToList();
        }

        private static Func<OpenApiSchema, NamingStrategy, OpenApiSchema> SelectRootSchema()
        {
            return (schema, namingStrategy) => schema;
        }
    }
}
