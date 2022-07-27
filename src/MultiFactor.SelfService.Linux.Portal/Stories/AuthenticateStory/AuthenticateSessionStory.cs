using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using System.Security.Claims;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory
{
    public class AuthenticateSessionStory
    {
        private readonly TokenParser _tokenParser;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly ILogger<AuthenticateSessionStory> _logger;

        public AuthenticateSessionStory(TokenParser tokenParser, SafeHttpContextAccessor contextAccessor, ILogger<AuthenticateSessionStory> logger)
        {
            _tokenParser = tokenParser ?? throw new ArgumentNullException(nameof(tokenParser));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> ExecuteAsync(string accessToken)
        {
            if (accessToken is null) throw new ArgumentNullException(nameof(accessToken));      
            _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);

            var parsedToken = await _tokenParser.ParseAsync(accessToken);
          
            _logger.LogInformation("Second factor for user '{user:l}' verified successfully", parsedToken.Identity);

            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, parsedToken.Identity)
            };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await _contextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

            //var cookie = new HttpCookie(Constants.COOKIE_NAME)
            //{
            //    Value = accessToken,
            //    Expires = token.ValidTo
            //};

            //Response.Cookies.Add(cookie);

            //FormsAuthentication.SetAuthCookie(token.Identity, false);

            if (parsedToken.MustChangePassword)
            {
                return new RedirectToActionResult("ChangePassword", "Home", new { });
            }

            return new RedirectToActionResult("Index", "Home", new { });
        }
    }
}
