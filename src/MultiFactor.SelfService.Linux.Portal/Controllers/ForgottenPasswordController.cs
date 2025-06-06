﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories;
using MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [RequiredFeature(ApplicationFeature.PasswordRecovery)]
    public class ForgottenPasswordController : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly RecoverPasswordStory _recoverPasswordStory;
        private readonly UnlockUserStory _unlockUserStory;
        private readonly TokenVerifier _tokenVerifier;
        private readonly DataProtection _dataProtection;
        private readonly ILogger _logger;

        public ForgottenPasswordController(
            DataProtection dataProtection,
            RecoverPasswordStory recoverPasswordStory,
            UnlockUserStory unlockUserStory,
            IStringLocalizer<SharedResource> localizer,
            TokenVerifier tokenVerifier,
            ILogger<ForgottenPasswordController> logger)
        {
            _recoverPasswordStory = recoverPasswordStory;
            _logger = logger;
            _localizer = localizer;
            _tokenVerifier = tokenVerifier;
            _dataProtection = dataProtection;
            _unlockUserStory = unlockUserStory;
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
                if (form.UnlockUser)
                {
                    return await _unlockUserStory.CallSecondFactorAsync(form);
                }

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
                TempData["reset-password-error"] = _localizer.GetString("UnableToRecoverPassword");
                return RedirectToAction("Wrong");
            }

            Response.Cookies.Append(Constants.PWD_RECOVERY_COOKIE, _dataProtection.Protect(token.Identity, Constants.PWD_RECOVERY_COOKIE), new CookieOptions
            {
                Expires = DateTime.Now.AddMinutes(5),
                Path = "/",
                Secure = true,
                HttpOnly = true
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
                var sessionCookie = Request.Cookies[Constants.PWD_RECOVERY_COOKIE];
                if (sessionCookie == null || _dataProtection.Unprotect(sessionCookie, Constants.PWD_RECOVERY_COOKIE) != form.Identity)
                {
                    _logger.LogError("Invalid reset password session for user '{identity:l}': session not found", form.Identity);
                    throw new ModelStateErrorException(_localizer.GetString("UnableToRecoverPassword"));
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
        public ActionResult Done()
        {
            Response.Cookies.Delete(Constants.PWD_RECOVERY_COOKIE);
            return View();
        }
    }
}