using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory
{
    public class SignOutStory
    {
        private readonly IOptions<CookieAuthenticationOptions> _cookieOptions;

        public SignOutStory(IOptions<CookieAuthenticationOptions> cookieOptions)
        {
            _cookieOptions = cookieOptions;
        }

        public async Task<IActionResult> ExecuteAsync(HttpContext context, MultiFactorClaimsDto claimsDto)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // remove mfa cookie
            if (context.Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                context.Response.Cookies.Delete(Constants.COOKIE_NAME);
            }

            var redirectUrl = new StringBuilder(_cookieOptions.Value.LoginPath);
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.SamlSessionId}={claimsDto.SamlSession}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.OidcSessionId}={claimsDto.OidcSession}");
            }

            return new RedirectResult(redirectUrl.ToString(), true);
        }
    }
}
