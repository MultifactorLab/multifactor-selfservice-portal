using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class TelegramController : Controller
    {
        public IActionResult Index([FromServices] TokenClaimsAccessor claimsAccessor) => View(claimsAccessor.GetTokenClaims());
    }
}
