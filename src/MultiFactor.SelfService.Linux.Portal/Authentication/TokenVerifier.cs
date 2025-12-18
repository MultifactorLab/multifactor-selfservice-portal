using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using System.IdentityModel.Tokens.Jwt;

namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public class TokenVerifier
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TokenVerifier> _logger;

        public TokenVerifier(IConfiguration config, ILogger<TokenVerifier> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TokenClaims Verify(string accessToken)
        {
            try
            {
                var validationParameters = TokenValidationParametersFactory.GetParameters(_config);
                var handler = new JwtSecurityTokenHandler();
                var claimsPrincipal = handler.ValidateToken(accessToken, validationParameters, out var securityToken);
                var jwtSecurityToken = (JwtSecurityToken)securityToken;

                var rawUserName = claimsPrincipal.Claims
                    .SingleOrDefault(claim => claim.Type == Constants.MultiFactorClaims.RawUserName)?.Value;
                var identity = jwtSecurityToken.Subject;
                var unlockUser = claimsPrincipal.Claims
                                     .FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.UnlockUser)
                                     ?.Value?.ToLower() == "true";
                var mustChangePassword =
                    claimsPrincipal.Claims.Any(claim => claim.Type == Constants.MultiFactorClaims.ChangePassword);
                var mustResetPassword =
                    claimsPrincipal.Claims.Any(claim => claim.Type == Constants.MultiFactorClaims.ResetPassword);
                var samlClaim = claimsPrincipal.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.SamlSessionId)?.Value;
                var oidcClaim = claimsPrincipal.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.OidcSessionId)?.Value;
                // use raw user name when possible couse multifactor may transform identity depend by settings
                return new TokenClaims(
                    Id: jwtSecurityToken.Id,
                    Identity: identity,
                    RawUserName: rawUserName,
                    MustChangePassword: mustChangePassword,
                    ValidTo: jwtSecurityToken.ValidTo,
                    MustResetPassword: mustResetPassword,
                    SamlClaim: samlClaim,
                    OidcClaim: oidcClaim,
                    MustUnlockUser: unlockUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying token");
                throw new UnauthorizedException("Error verifying token", ex);
            }
        }
    }
}