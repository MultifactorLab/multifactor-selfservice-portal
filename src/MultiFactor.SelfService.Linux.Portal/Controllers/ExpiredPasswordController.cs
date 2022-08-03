using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class ExpiredPasswordController : ControllerBase
    {
        [HttpGet]
        public IActionResult Change([FromServices] CheckExpiredPasswordSessionStory checkExpiredPasswordSession)
        {
            var result = checkExpiredPasswordSession.Execute();
            return result;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(ChangeExpiredPasswordViewModel model, 
            [FromServices] ChangeExpiredPasswordStory changeExpiredPassword,
            [FromServices] IStringLocalizer<SharedResource> localizer)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, localizer.GetString("WrongUserNameOrPassword"));
                return View(model);
            }

            try
            {
                return await changeExpiredPassword.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        public ActionResult Done()
        {
            return View();
        }
    }
}
