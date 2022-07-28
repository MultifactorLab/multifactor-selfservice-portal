using MultiFactor.SelfService.Linux.Portal.Core;

namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public class TokenClaimsProvider
    {
        private readonly TokenVerifier _tokenVerifier;
        private readonly SafeHttpContextAccessor _contextAccessor;

        public TokenClaimsProvider(TokenVerifier tokenVerifier, SafeHttpContextAccessor contextAccessor)
        {
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public TokenClaims GetTokenClaims()
        {
            return _tokenVerifier.Verify(_contextAccessor.HttpContext.Request.Headers["Authorization"]);
        }
    }
}
