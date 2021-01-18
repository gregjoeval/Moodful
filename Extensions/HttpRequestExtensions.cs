using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moodful.Configuration;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Moodful.Extensions
{
    /// <remarks>
    /// Reference: https://liftcodeplay.com/2017/11/25/validating-auth0-jwt-tokens-in-azure-functions-aka-how-to-use-auth0-with-azure-functions/
    /// Reference: https://www.tomfaltesek.com/azure-functions-input-validation/
    /// </remarks>
    public static class HttpRequestExtensions
    {
        public static async Task<AuthenticationStatus> Authenticate(this HttpRequest httpRequest, AuthenticationOptions securityOptions, string userId, ILogger logger = null)
        {
            if (securityOptions.Debug == true)
            {
                return AuthenticationStatus.Authenticated; // Doing this for now because swagger isnt using jwt token
            }

            var authenticationHeader = httpRequest.ParseAuthenticationHeader(logger);

            if ((await authenticationHeader?.ValidateTokenAsync(securityOptions, logger)) == null)
            {
                return AuthenticationStatus.Unauthenticated;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(authenticationHeader.Parameter))
            {
                return AuthenticationStatus.Unauthenticated;
            }

            var token = tokenHandler.ReadJwtToken(authenticationHeader.Parameter);
            if (userId != token.Subject)
            {
                return AuthenticationStatus.Unauthenticated;
            }

            return AuthenticationStatus.Authenticated;
        }

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

        private static async Task<ClaimsPrincipal> ValidateTokenAsync(this AuthenticationHeaderValue value, AuthenticationOptions securityOptions, ILogger logger = null)
        {
            if (value?.Scheme != "Bearer")
                return null;

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{securityOptions.Issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = securityOptions.Issuer.StartsWith("https://") }
            );
            var config = await configurationManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameter = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidAudience = securityOptions.Audience,
                ValidateAudience = true,
                ValidIssuer = $"{securityOptions.Issuer}/", // Auth0's issuer has a '/' on the end of its url
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateActor = true
            };

            ClaimsPrincipal result = null;
            var tries = 0;

            while (result == null && tries <= 1)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    result = handler.ValidateToken(value.Parameter, validationParameter, out var token);
                }
                catch (SecurityTokenSignatureKeyNotFoundException ex1)
                {
                    logger?.LogWarning($"{nameof(SecurityTokenSignatureKeyNotFoundException)}: {ex1.Message}");

                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    configurationManager.RequestRefresh();
                    tries++;
                }
                catch (SecurityTokenException ex2)
                {
                    logger?.LogWarning($"{nameof(SecurityTokenException)}: {ex2.Message}");

                    return null;
                }
            }

            return result;
        }

        private static AuthenticationHeaderValue ParseAuthenticationHeader(this HttpRequest httpRequest, ILogger logger = null)
        {
            var hasAuthorizationHeader = httpRequest.Headers.TryGetValue("Authorization", out var authorizationValue);

            logger?.LogDebug($"hasAuthorizationHeader:{hasAuthorizationHeader}");

            if (hasAuthorizationHeader)
            {
                var hasValidAuthenticationHeader = AuthenticationHeaderValue.TryParse(authorizationValue, out var authenticationHeader);

                logger?.LogDebug($"hasValidAuthenticationHeader:{hasAuthorizationHeader}");

                if (hasValidAuthenticationHeader)
                {
                    return authenticationHeader;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the deserialized request body.
        /// </summary>
        /// <typeparam name="T">Type used for deserialization of the request body.</typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        private static async Task<T> TryGetJsonBody<T>(this HttpRequest request)
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
