using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class UserPasswordChanger
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly IPasswordAttributeChanger _passwordAttributeChanger;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly LdapProfileLoader _profileLoader;
        private readonly PasswordRequirementsService _passwordRequirementsService;

        public UserPasswordChanger(LdapConnectionAdapterFactory connectionFactory,
            ILogger<UserPasswordChanger> logger,
            IPasswordAttributeChanger passwordAttributeChanger,
            IStringLocalizer<SharedResource> localizer,
            LdapProfileLoader profileLoader,
            PasswordRequirementsService passwordRequirementsService)
        {
            _connectionFactory = connectionFactory;
            _passwordAttributeChanger = passwordAttributeChanger;
            _logger = logger;
            _localizer = localizer;
            _profileLoader = profileLoader;
            _passwordRequirementsService = passwordRequirementsService;
        }

        public async Task<PasswordChangingResult> ChangePassword(
            string username, 
            string currentPassword, 
            string newPassword, 
            ChangePasswordMode passwordChangeMode)
        {
            var validationResult = _passwordRequirementsService.ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}", username, validationResult);
                return new PasswordChangingResult(false, validationResult.ToString());
            }
            try
            {
                using var connection = passwordChangeMode == ChangePasswordMode.AsTechnicalAccount
                 ? await _connectionFactory.CreateAdapterAsTechnicalAccAsync()
                 : await _connectionFactory.CreateAdapterAsync(username, currentPassword);

                var profile = await _profileLoader.GetLdapProfile(connection, username);

                await _passwordAttributeChanger.ExecuteChangeCommandAsync(profile.DistinguishedName, currentPassword, newPassword, connection);
                
                _logger.LogInformation("Password changed/reset for user '{username}'", username);
                return new PasswordChangingResult(true, string.Empty);
            }
            catch (LdapUnwillingToPerformException ex)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}, {result:l}",
                    username, ex.Message, ex.HResult);
                return new PasswordChangingResult(false, _localizer.GetString("AD.PasswordDoesNotMeetRequirements"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}",
                    username, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }
    }
}
