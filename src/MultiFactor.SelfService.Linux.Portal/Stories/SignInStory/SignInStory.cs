using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;
using System.Configuration;
using System.Reflection;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    public class SignInStory
    {
        private readonly CredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly MultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer _localizer;
        private readonly ILogger<SignInStory> _logger;
        private readonly ApplicationCache _applicationCache;
        private readonly ClaimsProvider _claimsProvider;
        private readonly PortalSettings _portalSettings;

        public SignInStory(CredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            MultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            ApplicationCache applicationCache,
            IStringLocalizer<SharedResource> localizer,
            ILogger<SignInStory> logger,
            ClaimsProvider claimsProvider,
            PortalSettings portalSettings)
        {
            _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _claimsProvider = claimsProvider ?? throw new ArgumentNullException(nameof(claimsProvider));
            _portalSettings = portalSettings;
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

            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
            if (adValidationResult.IsAuthenticated)
            {
                _logger.LogInformation("User '{user}' credential verified successfully in {domain:l}", userName, _settings.CompanySettings.Domain);
                var sso = _contextAccessor.SafeGetSsoClaims();
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return new RedirectToActionResult("ByPassSamlSession", "account", new { username = model.UserName, samlSession = sso.SamlSessionId });
                }

                return await RedirectToMfa(adValidationResult, model.MyUrl);
            }

            if (adValidationResult.UserMustChangePassword && _settings.PasswordManagement.Enabled)
            {
                // because if we here - bind throw exception, so need verify
                if (_settings.ActiveDirectorySettings.NeedPrebindInfo())
                {
                    adValidationResult = await _credentialVerifier.VerifyMembership(model.UserName, true);
                }
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim(), Constants.PWD_RENEWAL_PURPOSE);
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName), model.UserName.Trim());
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName), encryptedPassword);

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
            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = $"{currentUri.Scheme}://{currentUri.Authority}";

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing /
            noLastSegment = noLastSegment.Trim("/".ToCharArray());

            var postbackUrl = noLastSegment + "/PostbackFromMfa";
            var claims = _claimsProvider.GetClaims();
            var username = GetIdentity(verificationResult);

            var accessPage = await _api.CreateAccessRequestAsync(username,
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                postbackUrl,
                claims);

            return new RedirectResult(accessPage.Url, true);
        }

        private string GetIdentity(CredentialVerificationResult verificationResult)
        {
            var identity = verificationResult.Username;
            if (_portalSettings.ActiveDirectorySettings.UseUpnAsIdentity)
            {
                if (string.IsNullOrEmpty(verificationResult.UserPrincipalName))
                {
                    throw new InvalidOperationException($"Null UPN for user {verificationResult.Username}");
                }
                identity = verificationResult.UserPrincipalName;
            }
            if(identity == null)
            {
                throw new InvalidOperationException($"Null username, can't sign in");
            }
            return identity;
        }
    }
}
