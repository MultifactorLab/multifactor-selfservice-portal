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
            if (_contextAccessor.HttpContext.Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            }

            var redirectUrl = new StringBuilder("/account/login");
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={claimsDto.SamlSession}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.OidcSessionId}={claimsDto.OidcSession}");
            }

            return new RedirectResult(redirectUrl.ToString(), false);
        }
    }
}
