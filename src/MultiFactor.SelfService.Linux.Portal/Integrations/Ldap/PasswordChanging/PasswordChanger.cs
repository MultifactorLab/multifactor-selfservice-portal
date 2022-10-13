using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
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
        private readonly ILdapBindDnFormatter _bindDnFormatter;
        private readonly IPasswordAttributeReplacer _passwordAttributeReplacer;

        public PasswordChanger(LdapConnectionAdapterFactory connectionFactory, 
            PortalSettings settings,
            ILogger<PasswordChanger> logger, 
            IStringLocalizer<SharedResource> localizer,
            ILdapBindDnFormatter bindDnFormatter,
            IPasswordAttributeReplacer passwordAttributeReplacer)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _bindDnFormatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
            _passwordAttributeReplacer = passwordAttributeReplacer ?? throw new ArgumentNullException(nameof(passwordAttributeReplacer));
        }

        public Task<PasswordChangingResult> ChangeValidPasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (currentPassword is null) throw new ArgumentNullException(nameof(currentPassword));
            if (newPassword is null) throw new ArgumentNullException(nameof(newPassword));

            var user = LdapIdentity.ParseUser(username);
            var techUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);

            return TryExecuteAsync(async () =>
            {
                using var connection = await _connectionFactory.CreateAdapterAsync(username, currentPassword);
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
                    config => config.SetFormatter(_bindDnFormatter).SetLogger(_logger));
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
            catch (LdapUserNotFoundException ex)
            {
                _logger.LogError(ex, "Verification technical account user at {Domain:l} failed", _settings.CompanySettings.Domain);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Changing password for user '{identity:l}' failed: {message:l}", identity.Name, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }

        private async Task<PasswordChangingResult> ChangePasswordAsync(LdapIdentity user, string newPassword, LdapConnectionAdapter connection)
        {
            var domain = await connection.WhereAmIAsync();
            var profileLoader = new LdapProfileLoader(connection, _bindDnFormatter, _logger);
            var profile = await profileLoader.LoadProfileAsync(domain, user);
            if (profile == null)
            {
                return new PasswordChangingResult(false, "AD.UnableToChangePassword");
            }

            await _passwordAttributeReplacer.ExecuteReplaceCommandAsync(profile.DistinguishedName, newPassword, connection);

            return new PasswordChangingResult(true, string.Empty);
        }
    }
}
