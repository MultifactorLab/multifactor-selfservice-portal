using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory
{
    public class SignOutStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly MultifactorIdpApi _idpApi;
        private readonly ILogger<SignOutStory> _logger;

        public SignOutStory(SafeHttpContextAccessor contextAccessor, MultifactorIdpApi idpApi, ILogger<SignOutStory> logger)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _idpApi = idpApi;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Execute()
        {
            // remove mfa cookie
             _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            _contextAccessor.HttpContext.Request.Headers.Remove("Authorization");
            _idpApi.LogoutSsoMasterSession();

            var redirectUrl = new StringBuilder("/account/login");
            var claimsDto = MultiFactorClaimsDtoBinder.FromRequest(_contextAccessor.HttpContext.Request);
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={claimsDto.SamlSessionId}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{Constants.MultiFactorClaims.OidcSessionId}={claimsDto.OidcSessionId}");
            }

            var res = redirectUrl.ToString();

            _logger.LogDebug("[SignOutStory]: Result redirectUrl: {redirectUrl:l}", res);

            return new RedirectResult(res, false);
        }
    }
}
