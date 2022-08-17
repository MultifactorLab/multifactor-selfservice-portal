using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeActiveSyncDeviceStateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SearchExchangeActiveSyncDevicesStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [Authorize]
    public class ExchangeActiveSyncDevicesController : Controller
    {
        private readonly PortalSettings _settings;

        public ExchangeActiveSyncDevicesController(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<IActionResult> Index([FromServices] SearchExchangeActiveSyncDevicesStory searchExchangeActiveSyncDevices)
        {
            if (!_settings.EnableExchangeActiveSyncDevicesManagement)
            {
                return RedirectToAction("logout", "account");
            }

            var devices = await searchExchangeActiveSyncDevices.ExecuteAsync();
            return View(devices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string deviceId, [FromServices] ChangeActiveSyncDeviceStateStory changeActiveSyncDeviceState)
        {
            if (!_settings.EnableExchangeActiveSyncDevicesManagement || !ModelState.IsValid)
            {
                return RedirectToAction("logout", "account");
            }

            await changeActiveSyncDeviceState.ApproveAsync(deviceId);
            return LocalRedirect("/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string deviceId, [FromServices] ChangeActiveSyncDeviceStateStory changeActiveSyncDeviceState)
        {
            if (!_settings.EnableExchangeActiveSyncDevicesManagement || !ModelState.IsValid)
            {
                return RedirectToAction("logout", "account");
            }

            await changeActiveSyncDeviceState.RejectAsync(deviceId);
            return LocalRedirect("/");
        }
    }
}
