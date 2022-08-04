using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator.Dto;
namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class HomeController : ControllerBase
    {
        public async Task<IActionResult> Index(SingleSignOnDto claims, [FromServices] LoadProfileStory loadProfile)
        {
            if (claims.HasSamlSession() || claims.HasOidcSession())
            {
                //re-login for saml or oidc authentication
                return RedirectToAction("logout", "account", claims);
            }

            var userProfile = await loadProfile.ExecuteAsync();
            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RemoveAuthenticator(RemoveAuthenticatorDto dto, [FromServices] RemoveAuthenticatorStory removeAuthenticator)
        {
            return removeAuthenticator.ExecuteAsync(dto);
        }
    }
}
