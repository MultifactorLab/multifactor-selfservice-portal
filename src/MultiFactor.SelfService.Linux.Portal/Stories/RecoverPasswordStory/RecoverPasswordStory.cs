using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory
{
    public class RecoverPasswordStory
    {
        private readonly MultiFactorApi _apiClient;
        private readonly PortalSettings _portalSettings;
        private readonly ForgottenPasswordChanger _passwordChanger;
        private readonly CredentialVerifier _credentialVerifier;
        private readonly ILogger<RecoverPasswordStory> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RecoverPasswordStory(
            MultiFactorApi apiClient,
            PortalSettings portalSettings,
            ForgottenPasswordChanger passwordChanger,
            ILogger<RecoverPasswordStory> logger,
            IStringLocalizer<SharedResource> localizer, CredentialVerifier credentialVerifier)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer;
            _credentialVerifier = credentialVerifier;
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
        }

        public async Task<IActionResult> StartRecoverAsync(EnterIdentityForm form)
        {
            var identity = await GetIdentity(form);

            var callback = form.MyUrl.BuildRelativeUrl("Reset", 1);
            try
            {
                var response = await _apiClient.StartResetPassword(identity, form.Identity, callback);
                return new RedirectResult(response.Url);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                throw new ModelStateErrorException(_localizer.GetString("AD.UnableToChangePassword"));
            }
        }

        private async Task<string> GetIdentity(EnterIdentityForm form)
        {
            var attr = _portalSettings.ActiveDirectorySettings.UseAttributeAsIdentity;
            var username = form.Identity.Trim();
            var verificationResult = await _credentialVerifier.VerifyMembership(username);
            if (!string.IsNullOrWhiteSpace(attr))
            {
                if (string.IsNullOrWhiteSpace(verificationResult.CustomIdentity))
                {
                    throw new InvalidOperationException($"Missing overridden identity (attribute '{attr}') for user {username}");
                }

                return verificationResult.CustomIdentity;
            }

            if (!_portalSettings.ActiveDirectorySettings.UseUpnAsIdentity)
            {
                return username;
            }

            if (string.IsNullOrEmpty(verificationResult.UserPrincipalName))
            {
                throw new InvalidOperationException($"Null UPN for user {username}");
            }

            return verificationResult.UserPrincipalName;
        }

        public async Task ResetPasswordAsync(ResetPasswordForm form)
        {
            var result = await _passwordChanger.ChangePassword(form.Identity, form.NewPassword);
            if(!result.Success)
            {
                throw new ModelStateErrorException(result.ErrorReason);
            }
        }
    }
}
