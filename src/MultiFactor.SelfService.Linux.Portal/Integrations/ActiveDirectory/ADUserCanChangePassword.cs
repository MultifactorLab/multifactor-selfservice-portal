using LdapForNet.Adsddl;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ADUserCanChangePassword : IUserCanChangePassword
    {
        private const string _securityDescrAttr = "nTSecurityDescriptor";

        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly ILogger _logger;

        public ADUserCanChangePassword(LdapConnectionAdapterFactory connectionAdapterFactory, ILogger<ADUserCanChangePassword> logger)
        {
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CheckAsync(LdapIdentity userName)
        {
            using var connection = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync();

            var domain = await connection.WhereAmIAsync();
            var filter = LdapFilter.Create("objectclass", "user").Or("objectclass", "person")
                .And(LdapFilter.Create("uid", userName.GetUid()).Or("sAMAccountName", userName.GetUid()));
            var result = await connection.SearchQueryAsync(domain.Name, filter.Build(), 
                LdapForNet.Native.Native.LdapSearchScope.LDAP_SCOPE_SUBTREE, _securityDescrAttr);

            var entry = result.FirstOrDefault();
            if (entry == null)
            {
                _logger.LogWarning("User cannot change password: unable to check permissions. User '{user}' not found at domain '{domain}'",
                    userName, connection.Uri);
                return false;
            }

            if (!entry.DirectoryAttributes.TryGetValue(_securityDescrAttr, out var attr))
            {
                _logger.LogWarning("User cannot change password: unable to check permissions for user '{user}'. Required attribute not found", userName);
                return false;
            }

            var bytes = attr.GetValue<byte[]>();
            var sddl = new Sddl(bytes);

            if (sddl.UserCannotChangePassword())
            {
                _logger.LogWarning("User cannot change password: User '{user}' is not allowed to change password by policy", userName);
                return false;
            }

            return true;
        }
    }
}
