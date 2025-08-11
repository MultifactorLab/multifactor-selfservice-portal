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
            var userProfile = await loadProfile.ExecuteAsync();

            if (claims.HasSamlSession())
            {
                return new RedirectToActionResult("ByPassSamlSession", "Account", new { username = userProfile.Identity, samlSession = claims.SamlSessionId });
            }

            if (claims.HasOidcSession())
            {
                return new RedirectToActionResult("ByPassOidcSession", "Account", new { username = userProfile.Identity, oidcSession = claims.OidcSessionId });
            }

            return View(userProfile);
        }
    }
}
