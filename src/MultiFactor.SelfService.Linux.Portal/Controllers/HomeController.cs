using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly LoadProfileStory _loadProfile;

        public HomeController(LoadProfileStory loadProfile, SignOutStory signOut) : base(signOut)
        {
            _loadProfile = loadProfile;
        }

        public async Task<IActionResult> Index(MultiFactorClaimsDto claims)
        {
            if (claims.HasSamlSession() || claims.HasOidcSession())
            {
                //re-login for saml or oidc authentication
                return SignOut(claims);
            }

            try
            {
                var userProfile = await _loadProfile.ExecuteAsync();
                return View(userProfile);
            }
            catch (UnauthorizedException)
            {
                return SignOut(claims);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> RemoveAuthenticator(RemoveAuthenticatorDto dto, [FromServices] RemoveAuthenticatorStory removeAuthenticator)
        {
            return removeAuthenticator.ExecuteAsync(dto);
        }
    }
}
