using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPasswordStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class PasswordController : ControllerBase
    {
        [HttpGet]
        public IActionResult Change([FromServices] PortalSettings settings)
        {
            if (settings.EnablePasswordManagement)
            {
                return View();
            }

            return SignOut();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(ChangePasswordViewModel model, 
            [FromServices] ChangeValidPasswordStory changeValidPassword,
            [FromServices] IStringLocalizer<SharedResource> localizer)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, localizer.GetString("WrongUserNameOrPassword"));
                return View(model);
            }

            try
            {
                return await changeValidPassword.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
    }
}
