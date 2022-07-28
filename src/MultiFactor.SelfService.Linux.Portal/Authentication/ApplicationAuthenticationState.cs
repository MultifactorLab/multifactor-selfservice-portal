using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public class ApplicationAuthenticationState
    {
        private readonly object _lock = new object();
        private TokenClaims? _tokenClaims;

        private readonly TokenVerifier _tokenVerifier;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly ILogger<ApplicationAuthenticationState> _logger;

        public ApplicationAuthenticationState(TokenVerifier tokenVerifier, SafeHttpContextAccessor contextAccessor, ILogger<ApplicationAuthenticationState> logger)
        {
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Authenticate(string accessToken)
        {
            if (accessToken is null) throw new ArgumentNullException(nameof(accessToken));
            _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);

            var verifiedToken = _tokenVerifier.Verify(accessToken);
            _logger.LogInformation("Second factor for user '{user:l}' verified successfully", verifiedToken.Identity);

            _contextAccessor.HttpContext.Response.Cookies.Append(Constants.COOKIE_NAME, accessToken, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Expires = verifiedToken.ValidTo
            });

            lock (_lock)
            {
                _tokenClaims = verifiedToken;
            }
        }

        public void SignOut()
        {
            // remove mfa cookie
            if (_contextAccessor.HttpContext.Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            }

            lock (_lock)
            {
                _tokenClaims = null;
            }
        }

        public TokenClaims GetTokenClaims()
        {
            lock (_lock)
            {
                return _tokenClaims ?? throw new UnauthorizedException();
            }
        }
    }
}
