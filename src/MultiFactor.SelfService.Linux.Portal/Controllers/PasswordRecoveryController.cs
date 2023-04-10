using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class PasswordRecoveryController : ControllerBase
    {
        private ILogger _logger;
        private PortalSettings _portalSettings;
        private RecoverPasswordStory _recoverPasswordStory;
        private TokenVerifier _tokenVerifier;
        public PasswordRecoveryController(
            RecoverPasswordStory recoverPasswordStory,
            PortalSettings portalSettings,
            TokenVerifier tokenVerifier,
            ILogger<PasswordRecoveryController> logger)
        {
            _recoverPasswordStory = recoverPasswordStory;
            _logger = logger;
            _portalSettings = portalSettings;
            _tokenVerifier = tokenVerifier;
        }

        [HttpGet]
        public IActionResult Change()
        {
            return View();
        }


        [HttpPost]
        [VerifyCaptcha(CaptchaPlace.PasswordRecovery)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EnterIdentityForm form)
        {
            return await _recoverPasswordStory.StartRecoverAsync(form);
        }

        [HttpPost]
        public ActionResult Reset(string accessToken)
        {
            var token = _tokenVerifier.Verify(accessToken);

            if (!token.MustResetPassword)
            {
                _logger.LogError("Invalid reset password session for user '{identity:l}': required claims not found", token.Identity);
                return RedirectToAction("Wrong");
            }

            return View(new ResetPasswordForm
            {
                Identity = token.Identity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmReset(ResetPasswordForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Identity))
            {
                _logger.LogError("Invalid reset password form state: empty identity");
            }

            if (!ModelState.IsValid)
            {
                return View("Reset", form);
            }
            var result = await _recoverPasswordStory.RecoverPasswordAsync(form);
            return RedirectToAction("Done");
        }

        public ActionResult Wrong() => View();
        public ActionResult Done() => View();
    }
}
