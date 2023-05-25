using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory
{
    public class RecoverPasswordStory
    {
        private readonly MultiFactorApi _apiClient;
        private readonly PortalSettings _portalSettings;
        private readonly ForgottenPasswordChanger _passwordChanger;
        private readonly ILogger<RecoverPasswordStory> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly DataProtection _dataProtection;
        public RecoverPasswordStory(
            MultiFactorApi apiClient,
            PortalSettings portalSettings,
            ForgottenPasswordChanger passwordChanger,
            ILogger<RecoverPasswordStory> logger,
            IStringLocalizer<SharedResource> localizer,
            DataProtection dataProtection)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer;
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _dataProtection = dataProtection;
        }

        public async Task<IActionResult> StartRecoverAsync(EnterIdentityForm form)
        {
            if (_portalSettings.ActiveDirectorySettings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(form.Identity);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }
            var callback = form.MyUrl.BuildRelativeUrl("Reset", 1);
            try
            {
                var response = await _apiClient.StartResetPassword(form.Identity.Trim(), callback);
                return new RedirectResult(response.Url);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                throw new ModelStateErrorException(string.Format(_localizer.GetString("UnableToRecoverPassword"), form.Identity));
            }
        }

        public async Task<IActionResult> ResetPasswordAsync(ResetPasswordForm form)
        {
            var result = await _passwordChanger.ChangePassword(form.Identity, form.NewPassword);
            if(!result.Success)
            {
                throw new ModelStateErrorException(result.ErrorReason);
            }
            return new RedirectResult("Done");
        }
    }
}
