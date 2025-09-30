using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory
{
    public class CheckExpiredPasswordSessionStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;

        private readonly IApplicationCache _applicationCache;

        public CheckExpiredPasswordSessionStory(SafeHttpContextAccessor contextAccessor, PortalSettings settings, IApplicationCache applicationCache)
        {
            _contextAccessor = contextAccessor;
            _settings = settings;
            _applicationCache = applicationCache;
        }

        public IActionResult Execute()
        {
            if (!_settings.PasswordManagement.Enabled)
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

            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            return new ViewResult();
        }
    }
}
