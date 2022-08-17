using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Text;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.PasswordChanging
{
    public class ActiveDirectoryPasswordChanger
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<ActiveDirectoryPasswordChanger> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ActiveDirectoryPasswordChanger(PortalSettings settings, ILogger<ActiveDirectoryPasswordChanger> logger, IStringLocalizer<SharedResource> localizer)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public Task<PasswordChangingResult> ChangeValidPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);

            return TryExecuteAsync(async () =>
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, user, currentPassword, _logger);
                return await ChangePasswordAsync(user, newPassword, connection);
            }, user);
        }

        public Task<PasswordChangingResult> ChangeExpiredPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);
            var techUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);

            return TryExecuteAsync(async () =>
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(
                    _settings.CompanySettings.Domain, 
                    techUser, 
                    _settings.TechnicalAccountSettings.Password, 
                    _logger);
                return await ChangePasswordAsync(user, newPassword, connection);
            }, user);
        }

        private async Task<PasswordChangingResult> TryExecuteAsync(Func<Task<PasswordChangingResult>> action, LdapIdentity identity)
        {
            try
            {
                var res = await action();
                _logger.LogInformation("Password changed for user '{user:l}'", identity.Name);
                return res;
            }
            catch (LdapUnwillingToPerformException ex)
            {
                _logger.LogWarning("Changing password for user '{identity:l}' failed: {message:l}, {result:l}", identity.Name, ex.Message, ex.HResult);
                return new PasswordChangingResult(false, _localizer.GetString("AD.PasswordDoesNotMeetRequirements"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Changing password for user '{identity:l}' failed: {message:l}", identity.Name, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }

        private async Task<PasswordChangingResult> ChangePasswordAsync(LdapIdentity user, string newPassword, LdapConnectionAdapter connection)
        {
            var domain = await connection.WhereAmI();
            var names = new LdapNames(LdapServerType.ActiveDirectory);
            var profileLoader = new LdapProfileLoader(connection, names, _logger);
            var profile = await profileLoader.LoadProfileAsync(domain, user);
            if (profile == null)
            {
                return new PasswordChangingResult(false, "AD.UnableToChangePassword");
            }

            await ExecuteReplaceCommandAsync(profile.DistinguishedName, newPassword, connection);

            return new PasswordChangingResult(true, string.Empty);
        }

        private static async Task ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection)
        {
            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "unicodePwd",
                LdapModOperation = LdapModOperation.LDAP_MOD_REPLACE
            };
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

            var response = await connection.SendRequestAsync(new ModifyRequest(dn, newPasswordAttribute));
            if (response.ResultCode != ResultCode.Success)
            {
                throw new Exception($"Password change command error: {response.ErrorMessage}");
            }
        }
    }
}
