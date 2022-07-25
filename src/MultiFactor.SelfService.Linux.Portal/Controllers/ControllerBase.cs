using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [ResponseCache(NoStore = true, Duration = 0)]
    public abstract class ControllerBase : Controller
    {
        private readonly SignOutStory _signOutStory;

        protected ControllerBase(SignOutStory signOutStory)
        {
            _signOutStory = signOutStory;
        }

        protected Task<IActionResult> SignOutAsync(MultiFactorClaimsDto claims) => _signOutStory.ExecuteAsync(HttpContext, claims);      
    }
}
