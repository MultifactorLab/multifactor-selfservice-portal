using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class ForgottenPasswordChanger 
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly IPasswordAttributeReplacer _passwordAttributeReplacer;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly LdapProfileLoader _profileLoader;
        private readonly PasswordRequirementsService _passwordRequirementsService;


        public ForgottenPasswordChanger(LdapConnectionAdapterFactory connectionFactory,
            ILogger<UserPasswordChanger> logger,
            IPasswordAttributeReplacer passwordAttributeReplacer,
            IStringLocalizer<SharedResource> localizer,
            LdapProfileLoader profileLoader,
            PasswordRequirementsService passwordRequirementsService)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _passwordAttributeReplacer = passwordAttributeReplacer ?? throw new ArgumentNullException(nameof(passwordAttributeReplacer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _passwordRequirementsService = passwordRequirementsService;
        }

        public async Task<PasswordChangingResult> ChangePassword(string username,string newPassword)
        {
            var validationResult = _passwordRequirementsService.ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}", username, validationResult);
                return new PasswordChangingResult(false, validationResult.ToString());
            }
            try
            {
                using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                var profile = await _profileLoader.GetLdapProfile(connection, username);
                await _passwordAttributeReplacer.ExecuteReplaceCommandAsync(profile.DistinguishedName, newPassword, connection);
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
