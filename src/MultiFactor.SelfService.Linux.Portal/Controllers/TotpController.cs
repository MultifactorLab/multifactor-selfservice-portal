using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Stories.AddYandexAuthStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CreateYandexAuthKeyStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class TotpController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromServices] CreateYandexAuthKeyStory createYandexAuthKey)
        {
            var model = await createYandexAuthKey.ExecuteAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(YandexKeyViewModel model, [FromServices] AddYandexAuthStory addYandexAuth)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                return await addYandexAuth.ExecuteAsync(model.Key, model.Otp);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Index", model);
            }   
        }
    }
}
