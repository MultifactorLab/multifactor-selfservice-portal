using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory
{
    public class AuthenticateSessionStory
    {
        private readonly TokenVerifier _tokenVerifier;
        private readonly MultifactorIdpApi _idpApi;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _portalSettings;
        private readonly IApplicationCache _applicationCache;
        private readonly ILogger<AuthenticateSessionStory> _logger;

        public AuthenticateSessionStory(TokenVerifier tokenVerifier, MultifactorIdpApi idpApi, SafeHttpContextAccessor contextAccessor, ILogger<AuthenticateSessionStory> logger, PortalSettings portalSettings, IApplicationCache applicationCache)
        {
            _tokenVerifier = tokenVerifier;
            _idpApi = idpApi;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _portalSettings = portalSettings;
            _applicationCache = applicationCache;
        }

        public async Task<IActionResult> Execute(string accessToken)
        {
            ArgumentNullException.ThrowIfNull(accessToken);
            _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);

            var verifiedToken = _tokenVerifier.Verify(accessToken);
            _logger.LogInformation("Second factor for user '{user:l}' verified successfully", verifiedToken.Identity);
            // 2fa before authn enable

            await _idpApi.CreateSsoMasterSession(verifiedToken.Identity);

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

            var sso = new SingleSignOnDto(verifiedToken.SamlClaims, verifiedToken.OidcClaims);
            if (sso.HasSamlSession())
            {
                return new RedirectToActionResult("ByPassSamlSession", "Account", new { samlSession = sso.SamlSessionId });
            }

            if (sso.HasOidcSession())
            {
                return new RedirectToActionResult("ByPassOidcSession", "Account", new { oidcSession = sso.OidcSessionId });
            }

            return verifiedToken.MustChangePassword 
                ? new RedirectToActionResult("Change", "ExpiredPassword", default) 
                : new RedirectToActionResult("Index", "Home", default);
        }
    }
}
