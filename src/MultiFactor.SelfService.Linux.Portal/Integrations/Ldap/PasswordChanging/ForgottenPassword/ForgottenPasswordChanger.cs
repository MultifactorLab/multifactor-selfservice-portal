using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.ForgottenPassword;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class ForgottenPasswordChanger : PasswordChangerBase<ForgottenPasswordChangeRequest>
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly PortalSettings _settings;
        private readonly ADPasswordAttributeReplacer _passwordAttributeReplacer;

        public ForgottenPasswordChanger(LdapConnectionAdapterFactory connectionFactory, 
            PortalSettings settings,
            ILogger<ForgottenPasswordChanger> logger,
            ADPasswordAttributeReplacer passwordAttributeReplacer,
            IStringLocalizer<SharedResource> localizer,
            LdapProfileLoader profileLoader) 
            : base(logger, localizer, profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordAttributeReplacer = passwordAttributeReplacer ?? throw new ArgumentNullException(nameof(passwordAttributeReplacer));
        }

        public override async Task<PasswordChangingResult> ChangeLdapPasswordAttribute(
            LdapProfile profile, LdapConnectionAdapter connection, ForgottenPasswordChangeRequest request)
        {
            return await _passwordAttributeReplacer.ExecuteReplaceCommandAsync(profile.DistinguishedName, request.newPassword, connection);
        }

        public override async Task<PasswordChangingResult> ChangePassword(ForgottenPasswordChangeRequest request)
        {
            var user = LdapIdentity.ParseUser(request.username);

            return await TryExecuteAsync(async () =>
            {
                using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
                return await ChangePasswordAsync(user, connection, request);
            }, user);
        }
    }
}
