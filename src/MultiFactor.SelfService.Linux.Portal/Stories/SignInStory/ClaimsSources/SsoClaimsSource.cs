using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources
{
    public class SsoClaimsSource : IClaimsSource
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;

        public SsoClaimsSource(SafeHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var sso = _httpContextAccessor.SafeGetSsoClaims();
            var claims = new Dictionary<string, string>();

            if (sso.HasSamlSession())
            {
                claims.Add(Constants.MultiFactorClaims.SamlSessionId, sso.SamlSessionId);
            }

            if (sso.HasOidcSession())
            {
                claims.Add(Constants.MultiFactorClaims.OidcSessionId, sso.OidcSessionId);
            }

            return claims;
        }
    }
}
