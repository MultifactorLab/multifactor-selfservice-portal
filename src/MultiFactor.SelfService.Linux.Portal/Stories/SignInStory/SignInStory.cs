using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    public class SignInStory
    {
        private readonly CredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly IMultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer _localizer;
        private readonly ILogger<SignInStory> _logger;
        private readonly IApplicationCache _applicationCache;
        private readonly ClaimsProvider _claimsProvider;

        public SignInStory(CredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            IMultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            IApplicationCache applicationCache,
            IStringLocalizer<SharedResource> localizer,
            ILogger<SignInStory> logger,
            ClaimsProvider claimsProvider)
        {
            _credentialVerifier = credentialVerifier;
            _dataProtection = dataProtection;
            _api = api;
            _contextAccessor = contextAccessor;
            _settings = settings;
            _localizer = localizer;
            _logger = logger;
            _applicationCache = applicationCache;
            _claimsProvider = claimsProvider;
        }

        public async Task<IActionResult> ExecuteAsync(LoginViewModel model)
        {
            var userName = LdapIdentity.ParseUser(model.UserName);

            if (_settings.ActiveDirectorySettings.RequiresUserPrincipalName)
            {
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }

            var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
            if (userName.IsEquivalentTo(serviceUser)) return await WrongAsync();

            var adValidationResult =
                await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
            if (adValidationResult.IsAuthenticated)
            {
                _logger.LogInformation("User '{user}' credential verified successfully in {domain:l}", userName,
                    _settings.CompanySettings.Domain);
                var sso = _contextAccessor.SafeGetSsoClaims();
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    _logger.LogInformation("Bypass second factor for user '{@user:l}'", userName);
                    return new RedirectToActionResult("ByPassSamlSession", "Account",
                        new { username = model.UserName, samlSession = sso.SamlSessionId });
                }

                return await RedirectToMfa(adValidationResult, model.MyUrl);
            }

            if (adValidationResult.UserMustChangePassword && _settings.PasswordManagement.Enabled)
            {
                // because if we here - bind throw exception, so need verify
                if (_settings.NeedPrebindInfo())
                {
                    adValidationResult = await _credentialVerifier.VerifyMembership(model.UserName);
                }

                var encryptedPassword =
                    _dataProtection.Protect(model.Password.Trim(), Constants.PWD_RENEWAL_PURPOSE);
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                    model.UserName.Trim());
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                    encryptedPassword);

                return await RedirectToMfa(adValidationResult, model.MyUrl);
            }

            return await WrongAsync();

        }

        private async Task<IActionResult> WrongAsync()
        {
            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks.
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        private async Task<IActionResult> RedirectToMfa(CredentialVerificationResult verificationResult, string documentUrl)
        {
            var postbackUrl = documentUrl.BuildPostbackUrl();
            var claims = _claimsProvider.GetClaims();
            var username = GetIdentity(verificationResult);

            var personalData = new PersonalData(
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                _settings.MultiFactorApiSettings.PrivacyModeDescriptor);

            var accessPage = await _api.CreateAccessRequestAsync(username,
                personalData.Name,
                personalData.Email,
                personalData.Phone,
                postbackUrl,
                claims);
            
            if (string.IsNullOrWhiteSpace(accessPage.Url))
            {
                return new RedirectToActionResult("AccessDenied", "Error", null);
            }

            return new RedirectResult(accessPage.Url, true);
        }

        private string GetIdentity(CredentialVerificationResult verificationResult)
        {
            return !string.IsNullOrWhiteSpace(verificationResult.CustomIdentity)
                ? verificationResult.CustomIdentity
                : verificationResult.Username;
        }
    }
}
