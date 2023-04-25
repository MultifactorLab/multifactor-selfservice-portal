using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
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
        private readonly UserPasswordChanger _passwordChanger;
        private readonly ApplicationCache _applicationCache;
        public ChangeExpiredPasswordStory(PortalSettings settings, 
            SafeHttpContextAccessor contextAccessor, 
            DataProtection dataProtection,
            UserPasswordChanger passwordChanger,
            ApplicationCache applicationCache)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(passwordChanger));
        }

        public async Task<IActionResult> ExecuteAsync(ChangeExpiredPasswordViewModel model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            if (!_settings.PasswordManagement!.Enabled)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }
            var rawUserName = _contextAccessor.HttpContext.User.Claims.SingleOrDefault(
                claim => claim.Type == Constants.MultiFactorClaims.RawUserName)?.Value;
            if (rawUserName is null)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }
            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            if (userName?.Value is null || encryptedPwd?.Value is null)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            var currentPassword = _dataProtection.Unprotect(encryptedPwd.Value);
            var pwdChangeResult = await _passwordChanger.ChangePassword(
                userName.Value,
                currentPassword,
                model.NewPassword,
                _settings.PasswordManagement.ChangeExpiredPasswordMode
            );

            if (!pwdChangeResult.Success)
            {
                throw new ModelStateErrorException(pwdChangeResult.ErrorReason);
            }
            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            return new RedirectToActionResult("Done", "ExpiredPassword", new { });
        }
    }
}
