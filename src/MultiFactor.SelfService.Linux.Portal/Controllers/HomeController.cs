using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfile;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [IsAuthorized]
    public class HomeController : ControllerBase
    {
        public async Task<IActionResult> Index(SingleSignOnDto claims, [FromServices] LoadProfileStory loadProfile,
            [FromServices] FilterShowcaseLinksStory filterShowcaseLinks)
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

            var showcaseLinks = filterShowcaseLinks.Execute(userProfile.Policy);

            var model = new ShowcaseViewModel()
            {
                Profile = userProfile,
                ShowcaseLinks = showcaseLinks
            };
            return View(model);
        }
    }
}
