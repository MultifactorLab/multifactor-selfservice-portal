using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class PingController : Controller
    {
        [Route("api/ping")]
        public async Task<ActionResult<ApplicationInfoDto>> Index([FromServices] GetApplicationInfoStory getApplicationInfo)
        {
            return Ok(await getApplicationInfo.ExecuteAsync());
        }
    }
}
