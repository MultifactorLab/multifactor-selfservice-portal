using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;

namespace MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory
{
    public class CheckExpiredPasswordSessionStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;

        public CheckExpiredPasswordSessionStory(SafeHttpContextAccessor contextAccessor, PortalSettings settings)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IActionResult Execute()
        {
            var userName = _contextAccessor.HttpContext.Session.GetString(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY);
            var encryptedPwd = _contextAccessor.HttpContext.Session.GetString(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY);

            if (userName == null || encryptedPwd == null)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            if (!_settings.EnablePasswordManagement)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            return new ViewResult();
        }
    }
}
