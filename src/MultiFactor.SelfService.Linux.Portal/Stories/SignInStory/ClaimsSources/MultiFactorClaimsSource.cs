using System.Globalization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources
{
    public class MultiFactorClaimsSource : IClaimsSource
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;

        public MultiFactorClaimsSource(SafeHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var result = _httpContextAccessor.SafeGetGredVerificationResult();
            var claims = new Dictionary<string, string>
            {
                { Constants.MultiFactorClaims.RawUserName, result.Username }
            };

            if (result.UserMustChangePassword)
            {
                claims.Add(Constants.MultiFactorClaims.ChangePassword, "true");
                return claims;
            }

            claims.Add(Constants.MultiFactorClaims.PasswordExpirationDate,
                result.PasswordExpirationDate.ToString(CultureInfo.InvariantCulture));

            return claims;
        }
    }
}