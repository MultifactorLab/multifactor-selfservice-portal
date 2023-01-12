using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [VerifyCaptcha]
        [ConsumeSsoClaims]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, [FromServices] SignInStory signIn)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                return await signIn.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [ConsumeSsoClaims]
        public IActionResult Logout([FromServices] SignOutStory signOut) => signOut.Execute();

        [HttpPost]
        public IActionResult PostbackFromMfa(string accessToken, [FromServices] AuthenticateSessionStory authenticateSession) 
            => authenticateSession.Execute(accessToken); 
        
        [HttpPost]
        public async Task<IActionResult> ByPassSamlSession(string username, string samlSession, [FromServices] MultiFactorApi api)
        {
            var page = await api.CreateSamlBypassRequestAsync(username, samlSession);
            return View(page);
        }
    }
}
