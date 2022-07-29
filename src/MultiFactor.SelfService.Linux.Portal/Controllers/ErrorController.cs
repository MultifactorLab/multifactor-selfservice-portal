using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class ErrorController : ControllerBase
    {
        public IActionResult Index()
        {
            return View();
        } 
    }
}
