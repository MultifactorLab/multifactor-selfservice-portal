using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Stories.AddGoogleAuthStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetGoogleAuthKeyStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class TotpController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromServices] CreateGoogleAuthKeyStory createGoogleAuthKey)
        {
            var model = await createGoogleAuthKey.ExecuteAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(GoogleAuthenticatorViewModel model, [FromServices] AddGoogleAuthStory addGoogleAuth)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                return await addGoogleAuth.ExecuteAsync(model.Key, model.Otp);
            }
            catch (ModelStateErrorException ex)
            {
                // TODO: move this handling logic to the some custom FILTER and register GLOBALLY.
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
    }
}
