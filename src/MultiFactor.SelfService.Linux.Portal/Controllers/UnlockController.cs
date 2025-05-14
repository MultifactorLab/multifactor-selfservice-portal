using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Stories;

namespace MultiFactor.SelfService.Linux.Portal.Controllers;

[RequiredFeature(ApplicationFeature.PasswordRecovery)]
public class UnlockController : ControllerBase
{
    private readonly TokenVerifier _tokenVerifier;
    private readonly UnlockUserStory _unlockUserStory;
    private readonly ILogger _logger;

    public UnlockController(
        UnlockUserStory unlockUserStory,
        TokenVerifier tokenVerifier,
        ILogger<UnlockController> logger)
    {
        _logger = logger;
        _tokenVerifier = tokenVerifier;
        _unlockUserStory = unlockUserStory;
    }

    [HttpPost]
    public async Task<IActionResult> Complete(string accessToken)
    {
        var token = _tokenVerifier.Verify(accessToken);
        if (!token.MustUnlockUser)
        {
            _logger.LogError("Invalid unlocking session for user '{identity:l}': required claims not found",
                token.Identity);
            return RedirectToAction("Wrong");
        }

        if (!await _unlockUserStory.UnlockUserAsync(token.Identity))
        {
            return RedirectToAction("Wrong");
        }

        return RedirectToAction("Success");
    }

    public ActionResult Wrong()
    {
        return View();
    }

    public ActionResult Success()
    {
        return View();
    }
}