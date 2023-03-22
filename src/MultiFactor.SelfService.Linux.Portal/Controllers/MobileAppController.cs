using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Authentication;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [IsAuthorized]
    public class MobileAppController : Controller
    {
        public IActionResult Index([FromServices] TokenClaimsAccessor claimsAccessor) => View(claimsAccessor.GetTokenClaims());
    }
}
