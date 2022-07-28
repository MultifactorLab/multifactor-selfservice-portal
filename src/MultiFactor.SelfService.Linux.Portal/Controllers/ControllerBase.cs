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

        protected IActionResult SignOut(MultiFactorClaimsDto claims) => _signOutStory.Execute(claims);      
    }
}
