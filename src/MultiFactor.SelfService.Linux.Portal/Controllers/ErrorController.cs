using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Services;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class ErrorController : ControllerBase
    {
        private readonly ScopeInfoService _scopeInfoService;
        public ErrorController(ScopeInfoService scopeInfoService)
        {
            _scopeInfoService = scopeInfoService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SessionExpired()
        {
            return View();
        }
        
        public async Task<IActionResult> AccessDenied()
        {
            var adminInfo = await _scopeInfoService.GetSupportInfo();
            return View(adminInfo);
        }
    }
}
