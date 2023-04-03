using Microsoft.AspNetCore.Mvc;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    public abstract class ControllerBase : Controller
    {
    }
}
