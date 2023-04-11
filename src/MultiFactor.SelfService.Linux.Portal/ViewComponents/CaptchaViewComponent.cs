using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.ViewComponents
{
    public class CaptchaViewComponent : ViewComponent
    {
        public CaptchaViewComponent()
        {
        }
        public IViewComponentResult Invoke(CaptchaPlace captchaPlace)
        {
            return View(captchaPlace);
        }
    }
}
