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
        private readonly PortalSettings _settings;
        private readonly ILogger<PasswordChanger> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IPasswordAttributeChanger _passwordAttributeChanger;
        private readonly LdapProfileLoader _profileLoader;

        public PasswordChanger(LdapConnectionAdapterFactory connectionFactory, 
            PortalSettings settings,
            ILogger<PasswordChanger> logger, 
            IStringLocalizer<SharedResource> localizer,
            IPasswordAttributeChanger passwordAttributeChanger,
            LdapProfileLoader profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _passwordAttributeChanger = passwordAttributeChanger ?? throw new ArgumentNullException(nameof(passwordAttributeChanger));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        public Task<PasswordChangingResult> ChangeValidPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);

            switch (_settings.ChangeValidPasswordMode)
            {
                case ChangePasswordMode.AsTechnicalAccount:
                    return TryExecuteAsync(async () =>
                    {
                        using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                        return await ChangePasswordAsync(user, currentPassword, newPassword, connection);
                    }, user);

                default:
                    return TryExecuteAsync(async () =>
                    {
                        using var connection = await _connectionFactory.CreateAdapterAsync(username, currentPassword);
                        return await ChangePasswordAsync(user, currentPassword, newPassword, connection);
                    }, user);
            }  
        }

        public Task<PasswordChangingResult> ResetExpiredPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);

            switch (_settings.ChangeExpiredPasswordMode)
            {
                case ChangePasswordMode.AsTechnicalAccount:
                    return TryExecuteAsync(async () =>
                    {
                        using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                        return await ChangePasswordAsync(user, currentPassword, newPassword, connection);
                    }, user);

                default:
                    return TryExecuteAsync(async () =>
                    {
                        using var connection = await _connectionFactory.CreateAdapterAsync(username, currentPassword);
                        return await ChangePasswordAsync(user, currentPassword, newPassword, connection);
                    }, user);
            }
        }

        private async Task<PasswordChangingResult> TryExecuteAsync(Func<Task<PasswordChangingResult>> action, LdapIdentity identity)
        {
            try
            {
                var res = await action();
                _logger.LogInformation("Password changed/reset for user '{user}'", identity);
                return res;
            }
            catch (LdapUnwillingToPerformException ex)
            {
                _logger.LogWarning("Change/reset password for user '{identity}' failed: {message:l}, {result:l}", 
                    identity, ex.Message, ex.HResult);
                return new PasswordChangingResult(false, _localizer.GetString("AD.PasswordDoesNotMeetRequirements"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Change/reset password for user '{identity}' failed: {message:l}", 
                    identity, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }
        
        private async Task<PasswordChangingResult> ChangePasswordAsync(LdapIdentity user, string oldPassword, string newPassword, LdapConnectionAdapter connection)
        {
            var domain = await connection.WhereAmIAsync();
            var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
            if (profile == null)
            {
                _logger.LogWarning("Unable to change password: profile not loaded");
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }

            await _passwordAttributeChanger.ExecuteChangeCommandAsync(profile.DistinguishedName, oldPassword, newPassword, connection);

            return new PasswordChangingResult(true, string.Empty);
        }
    }
}
