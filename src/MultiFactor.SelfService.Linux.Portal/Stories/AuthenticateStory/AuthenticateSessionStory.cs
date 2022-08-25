using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory
{
    public class AuthenticateSessionStory
    {
        private readonly TokenVerifier _tokenVerifier;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly ILogger<AuthenticateSessionStory> _logger;

        public AuthenticateSessionStory(TokenVerifier tokenVerifier, SafeHttpContextAccessor contextAccessor, ILogger<AuthenticateSessionStory> logger)
        {
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger;
        }

        public IActionResult Execute(string accessToken)
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

            if (verifiedToken.MustChangePassword)
            {
                return new RedirectToActionResult("ChangePassword", "Home", new { });
            }

            return new RedirectToActionResult("Index", "Home", new { });
        }
    }
}
