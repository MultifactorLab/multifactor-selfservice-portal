﻿using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory
{
    public class AuthenticateSessionStory
    {
        private readonly TokenVerifier _tokenVerifier;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _portalSettings;
        private readonly ApplicationCache _applicationCache;
        private readonly ILogger<AuthenticateSessionStory> _logger;

        public AuthenticateSessionStory(TokenVerifier tokenVerifier, SafeHttpContextAccessor contextAccessor, ILogger<AuthenticateSessionStory> logger, PortalSettings portalSettings, ApplicationCache applicationCache)
        {
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _logger = logger;
            _portalSettings = portalSettings;
            _applicationCache = applicationCache;
        }

        public IActionResult Execute(string accessToken)
        {
            ArgumentNullException.ThrowIfNull(accessToken);
            _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);

            var verifiedToken = _tokenVerifier.Verify(accessToken);
            _logger.LogInformation("Second factor for user '{user:l}' verified successfully", verifiedToken.Identity);
            // 2fa before authn enable
            
            if (_portalSettings.PreAuthenticationMethod)
            {
                var identity = verifiedToken.Identity;
                var requestId = verifiedToken.Id;
                _applicationCache.SetIdentity(requestId,
                    new IdentityViewModel
                        { UserName = identity, AccessToken = accessToken });
                var cachedUser = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(identity));
                
                _contextAccessor.HttpContext.Response.Cookies.Append(Constants.COOKIE_NAME, accessToken, new CookieOptions
                {
                    Secure = true,
                    HttpOnly = true,
                    Expires = verifiedToken.ValidTo
                });
                return verifiedToken.MustChangePassword || !cachedUser.IsEmpty
                    ? new RedirectToActionResult("Change", "ExpiredPassword", default) 
                    : new RedirectToActionResult("Identity", "Account", new { requestId = requestId });
            }
            
            _contextAccessor.HttpContext.Response.Cookies.Append(Constants.COOKIE_NAME, accessToken, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Expires = verifiedToken.ValidTo
            });

            return verifiedToken.MustChangePassword 
                ? new RedirectToActionResult("Change", "ExpiredPassword", default) 
                : new RedirectToActionResult("Index", "Home", default);
        }
    }
}
