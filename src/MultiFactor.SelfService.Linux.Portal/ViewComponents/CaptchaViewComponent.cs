using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.ViewComponents
{
    public class CaptchaViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(CaptchaRequired mode)
        {
            return View(mode);
        }
    }
}
