using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.ExpiredPasswordReset
{
    public class ExpiredPasswordChanger : PasswordChangerBase<ExpiredPasswordChangeRequest>
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly PortalSettings _settings;
        private readonly IPasswordAttributeChanger _passwordAttributeChanger;
        public ExpiredPasswordChanger(LdapConnectionAdapterFactory connectionFactory,
            PortalSettings settings,
            ILogger<ForgottenPasswordChanger> logger,
            IPasswordAttributeChanger passwordAttributeChanger,
            IStringLocalizer<SharedResource> localizer,
            LdapProfileLoader profileLoader)
            : base(logger, localizer, profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordAttributeChanger = passwordAttributeChanger ?? throw new ArgumentNullException(nameof(passwordAttributeChanger));
        }

        public override async Task<PasswordChangingResult> ChangeLdapPasswordAttribute(LdapProfile profile, LdapConnectionAdapter connection, ExpiredPasswordChangeRequest request)
        {
            await _passwordAttributeChanger.ExecuteChangeCommandAsync(profile.DistinguishedName, request.currentPassword, request.newPassword, connection);
            return new PasswordChangingResult(true, string.Empty);
        }

        public override async Task<PasswordChangingResult> ChangePassword(ExpiredPasswordChangeRequest request)
        {
            var user = LdapIdentity.ParseUser(request.username);
            // Get Connection
            return await TryExecuteAsync(async () =>
            {
                using var connection = (_settings.ChangeExpiredPasswordMode == ChangePasswordMode.AsTechnicalAccount)
                    ? await _connectionFactory.CreateAdapterAsTechnicalAccAsync()
                    : await _connectionFactory.CreateAdapterAsync(request.username, request.currentPassword);
                return await ChangePasswordAsync(user, connection, request);
            }, user);
        }
    }
}
