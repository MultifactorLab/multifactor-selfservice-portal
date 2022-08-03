using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Dto;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory
{
    public class SignOutStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;

        public SignOutStory(SafeHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public IActionResult Execute(MultiFactorClaimsDto claimsDto)
        {
            // remove mfa cookie
             _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            _contextAccessor.HttpContext.Request.Headers.Remove("Authorization");

            var redirectUrl = new StringBuilder("/account/login");
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={claimsDto.SamlSessionId}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.OidcSessionId}={claimsDto.OidcSessionId}");
            }

            return new RedirectResult(redirectUrl.ToString(), false);
        }
    }
}
