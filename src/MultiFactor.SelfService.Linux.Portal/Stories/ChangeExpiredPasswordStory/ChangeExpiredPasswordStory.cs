using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPasswordStory
{
    public class ChangeExpiredPasswordStory
    {
        private readonly PortalSettings _settings;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly DataProtection _dataProtection;
        private readonly ActiveDirectoryPasswordChanger _passwordChanger;

        public ChangeExpiredPasswordStory(PortalSettings settings, 
            SafeHttpContextAccessor contextAccessor, 
            DataProtection dataProtection, 
            ActiveDirectoryPasswordChanger passwordChanger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
        }

        public async Task<IActionResult> ExecuteAsync(ChangeExpiredPasswordViewModel model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            if (!_settings.EnablePasswordManagement)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            var userName = _contextAccessor.HttpContext.Session.GetString(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY);
            var encryptedPwd = _contextAccessor.HttpContext.Session.GetString(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY);

            if (userName == null || encryptedPwd == null)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            var currentPassword = _dataProtection.Unprotect(encryptedPwd);
            var pwdChangeResult = await _passwordChanger.ChangeExpiredPasswordAsync(userName, currentPassword, model.NewPassword);
            if (!pwdChangeResult.Success)
            {
                throw new ModelStateErrorException(pwdChangeResult.ErrorReason);
            }

            _contextAccessor.HttpContext.Session.Remove(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY);
            _contextAccessor.HttpContext.Session.Remove(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY);

            return new RedirectToActionResult("Done", "ExpiredPassword", new { });
        }
    }
}
