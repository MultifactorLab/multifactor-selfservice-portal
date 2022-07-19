using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Services.Api;
using MultiFactor.SelfService.Linux.Portal.Services.Api.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly MultiFactorSelfServiceApiClient _apiClient;

        public HomeController(MultiFactorSelfServiceApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View(_apiClient.LoadProfile());

            //if (Request.QueryString[MultiFactorClaims.SamlSessionId] != null)
            //{
            //    //re-login for saml authentication
            //    return SignOut();
            //}
            //if (Request.QueryString[MultiFactorClaims.OidcSessionId] != null)
            //{
            //    //re-login for oidc authentication
            //    return SignOut();
            //}

            //var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            //if (tokenCookie == null)
            //{
            //    return SignOut();
            //}

            //try
            //{
            //    var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);
            //    var userProfile = api.LoadProfile();
            //    userProfile.EnablePasswordManagement = Configuration.Current.EnablePasswordManagement;
            //    userProfile.EnableExchangeActiveSyncDevicesManagement = Configuration.Current.EnableExchangeActiveSyncDevicesManagement;

            //    return View(userProfile);
            //}
            //catch (UnauthorizedException)
            //{
            //    return SignOut();
            //}
        }
    }
}
