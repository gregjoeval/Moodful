using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moodful.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Moodful.Authorization
{
    /// <summary>
    /// Reference: https://liftcodeplay.com/2017/11/25/validating-auth0-jwt-tokens-in-azure-functions-aka-how-to-use-auth0-with-azure-functions/
    /// </summary>
    public class Security
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        private readonly AuthenticationOptions AuthenticationOptions;

        public Security(ILogger log, AuthenticationOptions authenticationOptions)
        {
            AuthenticationOptions = authenticationOptions;

            _logger = log;

            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{AuthenticationOptions.Issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = AuthenticationOptions.Issuer.StartsWith("https://") }
            );
        }

        private AuthenticationHeaderValue ParseAuthenticationHeaderFromHttpRequest(HttpRequest httpRequest)
        {
            var hasAuthorizationHeader = httpRequest.Headers.TryGetValue("Authorization", out var authorizationValue);
      
            _logger.LogDebug($"hasAuthorizationHeader:{hasAuthorizationHeader}");

            if (hasAuthorizationHeader)
            {
                var hasValidAuthenticationHeader = AuthenticationHeaderValue.TryParse(authorizationValue, out var authenticationHeader);

                _logger.LogDebug($"hasValidAuthenticationHeader:{hasAuthorizationHeader}");

                if (hasValidAuthenticationHeader)
                {
                    return authenticationHeader;
                }
            }

            return null;
        }

        public async Task<ClaimsPrincipal> ValidateTokenAsync(AuthenticationHeaderValue value)
        {
            if (value?.Scheme != "Bearer")
                return null;

            var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameter = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidAudience = AuthenticationOptions.Audience,
                ValidateAudience = true,
                ValidIssuer = $"{AuthenticationOptions.Issuer}/", // Auth0's issuer has a '/' on the end of its url
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
                    _logger.LogWarning($"{nameof(SecurityTokenSignatureKeyNotFoundException)}: {ex1.Message}");

                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();
                    tries++;
                }
                catch (SecurityTokenException ex2)
                {
                    _logger.LogWarning($"{nameof(SecurityTokenException)}: {ex2.Message}");

                    return null;
                }
            }

            return result;
        }

        public async Task<ClaimsPrincipal> AuthenticateHttpRequestAsync(HttpRequest httpRequest)
        {
            var authenticationHeader = ParseAuthenticationHeaderFromHttpRequest(httpRequest);

            ClaimsPrincipal claimsPrincipal;
            if ((claimsPrincipal = await ValidateTokenAsync(authenticationHeader)) != null)
            {
                return claimsPrincipal;
            }

            return null;
        }

        public JwtSecurityToken GetJWTSecurityTokenFromHttpRequestAsync(HttpRequest httpRequest)
        {
            var authenticationHeader = ParseAuthenticationHeaderFromHttpRequest(httpRequest);

            var tokenHandler = new JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(authenticationHeader.Parameter))
            {
                var token = tokenHandler.ReadJwtToken(authenticationHeader.Parameter);
                return token;
            }

            return null;
        }
    }
}
