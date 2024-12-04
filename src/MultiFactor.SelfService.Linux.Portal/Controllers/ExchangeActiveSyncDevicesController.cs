using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeActiveSyncDeviceStateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SearchExchangeActiveSyncDevicesStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [IsAuthorized]
    [RequiredFeature(ApplicationFeature.ExchangeActiveSyncDevicesManagement)]
    public class ExchangeActiveSyncDevicesController : Controller
    {
        public async Task<IActionResult> Index([FromServices] SearchExchangeActiveSyncDevicesStory searchExchangeActiveSyncDevices)
        {
            var devices = await searchExchangeActiveSyncDevices.ExecuteAsync();
            return View(devices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string deviceId, [FromServices] ChangeActiveSyncDeviceStateStory changeActiveSyncDeviceState)
        {
            if (!ModelState.IsValid)
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
            if (!ModelState.IsValid)
            {
                return RedirectToAction("logout", "account");
            }

            await changeActiveSyncDeviceState.RejectAsync(deviceId);
            return LocalRedirect("/");
        }
    }
}
