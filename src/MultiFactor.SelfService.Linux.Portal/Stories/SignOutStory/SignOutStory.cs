using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory
{
    public class SignOutStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly CookieAuthenticationOptions _cookieOptions;

        public SignOutStory(SafeHttpContextAccessor contextAccessor, IOptions<CookieAuthenticationOptions> cookieOptions)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _cookieOptions = cookieOptions?.Value ?? throw new ArgumentNullException(nameof(cookieOptions));
        }

        public async Task<IActionResult> ExecuteAsync(MultiFactorClaimsDto claimsDto)
        {
            //await _contextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // remove mfa cookie
            if (_contextAccessor.HttpContext.Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            }

            var redirectUrl = new StringBuilder("/account/login");
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.SamlSessionId}={claimsDto.SamlSession}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.OidcSessionId}={claimsDto.OidcSession}");
            }

            return new RedirectResult(redirectUrl.ToString(), false);
        }
    }
}
