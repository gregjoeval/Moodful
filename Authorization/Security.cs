﻿using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
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
    public static class Security
    {
        private static readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        private static readonly string ISSUER = "https://gjv-dev.auth0.com/"; // TODO: should pull from config
        private static readonly string AUDIENCE = "https://gjv-dev-api-water-boy.herokuapp.com"; // TODO: should pull from config, TODO: this is a temp value

        static Security()
        {
            var documentRetriever = new HttpDocumentRetriever { RequireHttps = ISSUER.StartsWith("https://") };

            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{ISSUER}.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever
            );
        }

        private static AuthenticationHeaderValue ParseAuthenticationHeaderFromHttpRequest(HttpRequest httpRequest)
        {
            var hasAuthorizationHeader = httpRequest.Headers.TryGetValue("Authorization", out var authorizationValue);

            if (hasAuthorizationHeader)
            {
                var hasValidAuthenticationHeader = AuthenticationHeaderValue.TryParse(authorizationValue, out var authenticationHeader);

                if (hasValidAuthenticationHeader)
                {
                    return authenticationHeader;
                }
            }

            return null;
        }

        public static async Task<ClaimsPrincipal> ValidateTokenAsync(AuthenticationHeaderValue value)
        {
            if (value?.Scheme != "Bearer")
                return null;

            var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameter = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidAudience = AUDIENCE,
                ValidateAudience = true,
                ValidIssuer = ISSUER,
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
                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();
                    tries++;
                }
                catch (SecurityTokenException ex2)
                {
                    return null;
                }
            }

            return result;
        }

        public static async Task<ClaimsPrincipal> AuthenticateHttpRequestAsync(HttpRequest httpRequest)
        {
            var authenticationHeader = ParseAuthenticationHeaderFromHttpRequest(httpRequest);

            ClaimsPrincipal claimsPrincipal;
            if ((claimsPrincipal = await ValidateTokenAsync(authenticationHeader)) != null)
            {
                return claimsPrincipal;
            }

            return null;
        }

        public static JwtSecurityToken GetJWTSecurityTokenFromHttpRequestAsync(HttpRequest httpRequest)
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
