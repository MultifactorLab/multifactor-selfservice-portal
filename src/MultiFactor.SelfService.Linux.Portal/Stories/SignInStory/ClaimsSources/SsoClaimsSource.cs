using System.Configuration;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources
{
    public class SsoClaimsSource : IClaimsSource
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;
        private readonly PortalSettings _portalSettings;

        public SsoClaimsSource(SafeHttpContextAccessor httpContextAccessor, PortalSettings portalSettings)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _portalSettings = portalSettings;
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var sso = _httpContextAccessor.SafeGetSsoClaims();
            var claims = new Dictionary<string, string>();

            if (sso.HasSamlSession())
            {
                claims.Add(Constants.MultiFactorClaims.SamlSessionId, sso.SamlSessionId);
                claims.Add(Constants.MultiFactorClaims.AdditionSsoStep, "true");
            }

            if (sso.HasOidcSession())
            {
                claims.Add(Constants.MultiFactorClaims.OidcSessionId, sso.OidcSessionId);
                claims.Add(Constants.MultiFactorClaims.AdditionSsoStep, "true");
            }

            return claims;
        }
    }
}
