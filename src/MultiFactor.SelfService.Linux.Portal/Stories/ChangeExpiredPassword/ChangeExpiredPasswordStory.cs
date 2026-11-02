using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPassword
{
    public class ChangeExpiredPasswordStory
    {
        private readonly PortalSettings _settings;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly DataProtection _dataProtection;
        private readonly UserPasswordChanger _passwordChanger;
        private readonly IApplicationCache _applicationCache;
        private readonly IMultiFactorApi _multiFactorApi;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly TokenVerifier _tokenVerifier;
        private readonly ILogger<ChangeExpiredPasswordStory> _logger;
        private readonly AuthenticateSessionStory _authenticateSessionStory;

        public ChangeExpiredPasswordStory(PortalSettings settings,
            SafeHttpContextAccessor contextAccessor,
            DataProtection dataProtection,
            UserPasswordChanger passwordChanger,
            IApplicationCache applicationCache,
            IMultiFactorApi multiFactorApi,
            HttpClientTokenProvider tokenProvider,
            TokenVerifier tokenVerifier,
            ILogger<ChangeExpiredPasswordStory> logger,
            AuthenticateSessionStory authenticateSessionStory)
        {
            _settings = settings;
            _contextAccessor = contextAccessor;
            _dataProtection = dataProtection;
            _passwordChanger = passwordChanger;
            _applicationCache = applicationCache;
            _multiFactorApi = multiFactorApi;
            _tokenProvider = tokenProvider;
            _tokenVerifier = tokenVerifier;
            _logger = logger;
            _authenticateSessionStory = authenticateSessionStory;
        }

        public async Task<IActionResult> ExecuteAsync(ChangeExpiredPasswordViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!_settings.PasswordManagement.Enabled)
            {
                return new RedirectToActionResult("Login", "Account", new { });
            }

            var rawUserName = _contextAccessor.HttpContext.User.Claims
                .SingleOrDefault(claim => claim.Type == Constants.MultiFactorClaims.RawUserName)?.Value;
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

            var accessToken = _tokenProvider.GetToken();
            var tokenClaims = _tokenVerifier.Verify(accessToken);
            var currentPassword = _dataProtection.Unprotect(encryptedPwd.Value, Constants.PWD_RENEWAL_PURPOSE);
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

            if (string.IsNullOrEmpty(tokenClaims.SamlClaim) && string.IsNullOrEmpty(tokenClaims.OidcClaim))
            {
                return new RedirectToActionResult("Done", "ExpiredPassword", null);
            }

            try
            {
                var claims = _tokenVerifier.GetPasswordChangeClaims(accessToken);
                claims.Remove(Constants.MultiFactorClaims.PasswordExpirationDate);
                var refreshedToken = await _multiFactorApi.RefreshAccessTokenAsync(accessToken, claims);
                var refreshedClaims = _tokenVerifier.Verify(refreshedToken);
                if (refreshedClaims.MustChangePassword
                    || refreshedClaims.Identity != tokenClaims.Identity
                    || refreshedClaims.Id != tokenClaims.Id
                    || refreshedClaims.RawUserName != rawUserName
                    || refreshedClaims.SamlClaim != tokenClaims.SamlClaim
                    || refreshedClaims.OidcClaim != tokenClaims.OidcClaim)
                {
                    throw new InvalidOperationException("Refreshed token has unexpected claims.");
                }

                _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
                return await _authenticateSessionStory.ExecuteAfterPasswordChangeAsync(
                    refreshedToken, tokenClaims.SamlClaim, tokenClaims.OidcClaim);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to complete sign-in after password change for '{User}'", rawUserName);
                _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
                return new RedirectToActionResult("Login", "Account",
                    new { samlSessionId = tokenClaims.SamlClaim, oidcSessionId = tokenClaims.OidcClaim });
            }
        }
    }
}
