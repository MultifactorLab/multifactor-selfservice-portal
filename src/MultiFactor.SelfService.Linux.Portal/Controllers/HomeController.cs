using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [IsAuthorized]
    public class HomeController : ControllerBase
    {
        public async Task<IActionResult> Index(SingleSignOnDto claims, [FromServices] LoadProfileStory loadProfile)
        {
            if (claims.HasSamlSession())
            {
                var user = await loadProfile.ExecuteAsync();
                return new RedirectToActionResult("ByPassSamlSession", "Account", new { username = user.Identity, samlSession = claims.SamlSessionId });
            }

            if (claims.HasOidcSession())
            {
                //re-login for saml or oidc authentication
                return RedirectToAction("logout", "account", claims);
            }

            var userProfile = await loadProfile.ExecuteAsync();
            return View(userProfile);
        }
    }
}
