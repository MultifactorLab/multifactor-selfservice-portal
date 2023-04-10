using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.PasswordRecovery;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public abstract class PasswordChangerBase<T> : IPasswordChanger<T>
    {
        protected readonly ILogger _logger;
        protected readonly IStringLocalizer<SharedResource> _localizer;
        protected readonly LdapProfileLoader _profileLoader;

        public PasswordChangerBase(
            ILogger logger, 
            IStringLocalizer<SharedResource> localizer, 
            LdapProfileLoader profileLoader)
        { 
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        public abstract Task<PasswordChangingResult> ChangePassword(T request);
        public abstract Task<PasswordChangingResult> ChangeLdapPasswordAttribute(LdapProfile profile, LdapConnectionAdapter connection, T changeRequest);
        
        protected async Task<PasswordChangingResult> TryExecuteAsync(Func<Task<PasswordChangingResult>> action, LdapIdentity identity)
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

        protected async Task<PasswordChangingResult> ChangePasswordAsync(LdapIdentity user, LdapConnectionAdapter connection, T changeRequest)
        {
            var domain = await connection.WhereAmIAsync();
            var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
            if (profile == null)
            {
                _logger.LogWarning("Unable to change password: profile not loaded");
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }

            return await ChangeLdapPasswordAttribute(profile, connection, changeRequest);
        }
    }
}
