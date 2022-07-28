using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Models;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoginStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    public class AccountController : ControllerBase
    {
        public AccountController(SignOutStory signOutStory) : base(signOutStory)
        {
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, [FromServices] LoginStory loginStory)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await loginStory.ExecuteAsync(model, new Dto.MultiFactorClaimsDto("", ""));
                return result;
            }
            catch (ModelStateErrorException ex)
            {
                // TODO: move this handling logic to the some custom FILTER and register GLOBALLY.
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult PostbackFromMfa(string accessToken, [FromServices] AuthenticateSessionStory authenticateStory) => authenticateStory.Execute(accessToken); 
    }
}
