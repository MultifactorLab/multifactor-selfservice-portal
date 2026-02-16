using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

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

        public async Task<IActionResult> Applications([FromServices] LoadProfileStory loadProfile)
        {
            var userProfile = await loadProfile.ExecuteAsync();

            var applications = new List<ApplicationItemViewModel>();

            // Load applications from settings if configured
            if (Settings.LinksShowcase != null)
            {
                var links = Settings.LinksShowcase.GetLinks();
                applications = links.Select(link => new ApplicationItemViewModel
                {
                    Title = link.Title,
                    Logo = $"~/content/images/{link.Image}",
                    Url = link.Url,
                    OpenInNewTab = link.OpenInNewTab
                }).ToList();
            }

            var model = new ApplicationsPageViewModel
            {
                UserProfile = userProfile,
                Applications = applications
            };

            return View(model);
        }
    }
}
