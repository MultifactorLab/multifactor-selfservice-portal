using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [RequiredFeature(ApplicationFeature.PasswordRecovery)]
    public class ForgottenPasswordController : ControllerBase
    {
        private const string PASSWORD_COOKIE = "PSession";

        private ILogger _logger;
        private IStringLocalizer<SharedResource> _localizer;
        private RecoverPasswordStory _recoverPasswordStory;
        private TokenVerifier _tokenVerifier;
        private DataProtection _dataProtection;

        public ForgottenPasswordController(
            DataProtection dataProtection,
            RecoverPasswordStory recoverPasswordStory,
            IStringLocalizer<SharedResource> localizer,
            TokenVerifier tokenVerifier,
            ILogger<ForgottenPasswordController> logger)
        {
            _recoverPasswordStory = recoverPasswordStory;
            _logger = logger;
            _localizer = localizer;
            _tokenVerifier = tokenVerifier;
            _dataProtection = dataProtection;
        }

        [HttpGet]
        public IActionResult Change()
        {
            return View();
        }


        [HttpPost]
        [VerifyCaptcha(CaptchaRequired.PasswordRecovery)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(EnterIdentityForm form)
        {
            try
            {
                return await _recoverPasswordStory.StartRecoverAsync(form);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(form);
            }
        }

        [HttpPost]
        public ActionResult Reset(string accessToken)
        {
            var token = _tokenVerifier.Verify(accessToken);

            if (!token.MustResetPassword)
            {
                _logger.LogError("Invalid reset password session for user '{identity:l}': required claims not found", token.Identity);
                TempData["reset-password-error"] = string.Format(_localizer.GetString("UnableToRecoverPassword"), token.Identity);
                return RedirectToAction("Wrong");
            }

            Response.Cookies.Append(PASSWORD_COOKIE, _dataProtection.Protect(token.Identity, PASSWORD_COOKIE), new CookieOptions
            {
                Expires = DateTime.Now.AddHours(1),
                Path = "/",
                Secure = true
            });


            return View(new ResetPasswordForm
            {
                Identity = token.Identity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmReset(ResetPasswordForm form)
        {
            if (!ModelState.IsValid)
            {
                return View("Reset", form);
            }
           

            try
            {
                var sesionCookie = Request.HttpContext.Request.Cookies[PASSWORD_COOKIE];
                if (sesionCookie == null || _dataProtection.Unprotect(sesionCookie, PASSWORD_COOKIE) != form.Identity)
                {
                    _logger.LogError("Invalid reset password session for user '{identity:l}': required claims not found", form.Identity);
                    throw new ModelStateErrorException(string.Format(_localizer.GetString("UnableToRecoverPassword"), form.Identity));
                }
                await _recoverPasswordStory.ResetPasswordAsync(form);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Reset", form);
            }

            return RedirectToAction("Done");
        }

        public ActionResult Wrong()
        {
            var error = TempData["reset-password-error"];
            return View(error); 
        }
        public ActionResult Done() => View();
    }
}
