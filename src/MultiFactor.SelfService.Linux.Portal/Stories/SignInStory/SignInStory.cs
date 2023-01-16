﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
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
        private readonly MultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer _localizer;
        private readonly ILogger<SignInStory> _logger;
        private readonly ClaimsProvider _claimsProvider;

        public SignInStory(CredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            MultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            IStringLocalizer<SharedResource> localizer,
            ILogger<SignInStory> logger,
            ClaimsProvider claimsProvider)
        {
            _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimsProvider = claimsProvider ?? throw new ArgumentNullException(nameof(claimsProvider));
        }

        public async Task<IActionResult> ExecuteAsync(LoginViewModel model)
        {
            var userName = LdapIdentity.ParseUser(model.UserName);
            if (_settings.RequiresUserPrincipalName)
            {
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }

            var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);
            if (userName.IsEquivalentTo(serviceUser)) return await WrongAsync();

            var validationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
            if (validationResult.IsAuthenticated)
            {
                _logger.LogInformation("User '{user}' credential verified successfully in {domain:l}", userName, _settings.CompanySettings.Domain);
                var sso = _contextAccessor.SafeGetSsoClaims();
                if (sso.HasSamlSession() && validationResult.IsBypass)
                {
                    return new RedirectToActionResult("ByPassSamlSession", "account", new { username = model.UserName, samlSession = sso.SamlSessionId });
                }

                return await RedirectToMfa(validationResult, model.MyUrl);
            }

            if (validationResult.UserMustChangePassword)
            {
                _logger.LogInformation("User '{user}' credential verified successfully in {domain:l}, but user must change password", 
                    userName, 
                    _settings.CompanySettings.Domain);

                if (!_settings.EnablePasswordManagement)
                {
                    _logger.LogWarning("User cannot change password: password management is disabled");
                    return await WrongAsync();
                }
                
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim());
                _contextAccessor.HttpContext.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY, model.UserName.Trim());
                _contextAccessor.HttpContext.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY, encryptedPassword);

                return new RedirectToActionResult("Change", "ExpiredPassword", new { });
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

            var accessPage = await _api.CreateAccessRequestAsync(verificationResult.Username,
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                postbackUrl,
                claims);

            return new RedirectResult(accessPage.Url, true);
        }
    }
}
