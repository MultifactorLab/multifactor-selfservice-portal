using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class PasswordChanger
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly ILogger<PasswordChanger> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IPasswordAttributeReplacer _passwordAttributeReplacer;
        private readonly LdapProfileLoader _profileLoader;

        public PasswordChanger(LdapConnectionAdapterFactory connectionFactory, 
            ILogger<PasswordChanger> logger, 
            IStringLocalizer<SharedResource> localizer,
            IPasswordAttributeReplacer passwordAttributeReplacer,
            LdapProfileLoader profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _passwordAttributeReplacer = passwordAttributeReplacer ?? throw new ArgumentNullException(nameof(passwordAttributeReplacer));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        public Task<PasswordChangingResult> ChangeValidPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);

            return TryExecuteAsync(async () =>
            {
                using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                return await ChangePasswordAsync(user, newPassword, connection);
            }, user);
        }

        public Task<PasswordChangingResult> ChangeExpiredPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);

            return TryExecuteAsync(async () =>
            {
                using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                return await ChangePasswordAsync(user, newPassword, connection);
            }, user);
        }

        private async Task<PasswordChangingResult> TryExecuteAsync(Func<Task<PasswordChangingResult>> action, LdapIdentity identity)
        {
            try
            {
                var res = await action();
                _logger.LogInformation("Password changed for user '{user}'", identity);
                return res;
            }
            catch (LdapUnwillingToPerformException ex)
            {
                _logger.LogWarning("Changing password for user '{identity}' failed: {message:l}, {result:l}", 
                    identity, ex.Message, ex.HResult);
                return new PasswordChangingResult(false, _localizer.GetString("AD.PasswordDoesNotMeetRequirements"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Changing password for user '{identity}' failed: {message:l}", 
                    identity, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }

        private async Task<PasswordChangingResult> ChangePasswordAsync(LdapIdentity user, string newPassword, LdapConnectionAdapter connection)
        {
            var domain = await connection.WhereAmIAsync();
            var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
            if (profile == null)
            {
                return new PasswordChangingResult(false, "AD.UnableToChangePassword");
            }

            await _passwordAttributeReplacer.ExecuteReplaceCommandAsync(profile.DistinguishedName, newPassword, connection);

            return new PasswordChangingResult(true, string.Empty);
        }
    }
}
