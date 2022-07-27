using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using System.IdentityModel.Tokens.Jwt;

namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public class TokenParser
    {
        private JsonWebKeySet _jsonWebKeySet;

        private readonly IServiceProvider _serviceProvider;
        private readonly PortalSettings _settings;
        private readonly ILogger<TokenParser> _logger;

        public TokenParser(IServiceProvider serviceProvider, PortalSettings settings, ILogger<TokenParser> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Token> ParseAsync(string accessToken)
        {
            try
            {
                await EsureJsonWebKeySetLoadedAsync();

                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKeys = _jsonWebKeySet.Keys,
                    ValidAudience = _settings.MultiFactorApiKey,
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateTokenReplay = true,
                };

                var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(accessToken, validationParameters, out var securityToken);
                var jwtSecurityToken = (JwtSecurityToken)securityToken;

                var identity = jwtSecurityToken.Subject;
                var rawUserName = claimsPrincipal.Claims.SingleOrDefault(claim => claim.Type == MultiFactorClaims.RawUserName)?.Value;

                // use raw user name when possible couse multifactor may transform identity depend by settings
                return new Token(jwtSecurityToken.Id,
                    rawUserName ?? identity,
                    claimsPrincipal.Claims.Any(claim => claim.Type == MultiFactorClaims.ChangePassword),
                    jwtSecurityToken.ValidTo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                throw new UnauthorizedException("Error verifying token", ex);
            }
        }

        private async Task EsureJsonWebKeySetLoadedAsync()
        {
            if (_jsonWebKeySet != null) return;

            _logger.LogDebug("Fetching jwks");
            var api = _serviceProvider.GetRequiredService<MultiFactorApi>();
            var response = await api.FetchJsonWebKeySetAsync();
            _logger.LogDebug("Fetched jwks: \n{response:l}", response);

            _jsonWebKeySet = new JsonWebKeySet(response);
        }
    }
}
