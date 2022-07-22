using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Services.Api;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _apiClient;

        public HomeController(MultiFactorSelfServiceApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [AllowAnonymous]
        public IActionResult Index(MultiFactorClaimsDto claims)
        {
            if (claims.SamlSession != null)
            {
                //re-login for saml authentication
                return SignOut();
            }

            if (claims.OidcSession != null)
            {
                  //re-login for oidc authentication
                  return SignOut();
            }

            //var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            //if (tokenCookie == null)
            //{
            //    return SignOut();
            //}

            //try
            //{
            //    var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);
            var userProfile = _apiClient.LoadProfile();

            return View(userProfile);
            //}
            //catch (UnauthorizedException)
            //{
            //    return SignOut();
            //}
        }
    }
}
