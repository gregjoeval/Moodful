using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Moodful.Extensions
{
    /// <summary>
    /// Resource: https://www.tomfaltesek.com/azure-functions-input-validation/
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Returns the deserialized request body with validation information.
        /// </summary>
        /// <typeparam name="T">Type used for deserialization of the request body.</typeparam>
        /// <typeparam name="V">
        /// Validator used to validate the deserialized request body.
        /// </typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<ValidatableRequest<T>> GetValidatedJsonBody<T, V>(this HttpRequest request)
            where V : AbstractValidator<T>, new()
        {
            var requestObject = await request.TryGetJsonBody<T>();
            if (requestObject == null)
            {
                return new ValidatableRequest<T>
                {
                    Value = requestObject,
                    IsValid = false,
                    Errors = Enumerable.Empty<ValidationFailure>().ToList()
                };
            }

            var validator = new V();
            var validationResult = validator.Validate(requestObject);

            if (!validationResult.IsValid)
            {
                return new ValidatableRequest<T>
                {
                    Value = requestObject,
                    IsValid = false,
                    Errors = validationResult.Errors
                };
            }

            return new ValidatableRequest<T>
            {
                Value = requestObject,
                IsValid = true
            };
        }

        /// <summary>
        /// Returns the deserialized request body.
        /// </summary>
        /// <typeparam name="T">Type used for deserialization of the request body.</typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<T> TryGetJsonBody<T>(this HttpRequest request)
        {
            var requestBody = await request.ReadAsStringAsync();

            try
            {
                return JsonConvert.DeserializeObject<T>(requestBody);
            }
            catch
            {
                return default;
            }
        }
    }
}
