using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapConnectionAdapterFactory
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<LdapConnectionAdapterFactory> _logger;
        private readonly ILdapBindDnFormatter _bindDnFormatter;

        public LdapConnectionAdapterFactory(PortalSettings settings, ILogger<LdapConnectionAdapterFactory> logger, ILdapBindDnFormatter bindDnFormatter)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bindDnFormatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
        }

        /// <summary>
        /// Returns instance of <see cref="LdapConnectionAdapter"/>.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Ldap connection adapter.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="LdapUserNotFoundException"></exception>
        public async Task<LdapConnectionAdapter> CreateAdapterAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException($"'{nameof(username)}' cannot be null or empty.", nameof(username));
            if (string.IsNullOrEmpty(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            var parsed = LdapIdentity.ParseUser(username);
            if (parsed.Type == IdentityType.UserPrincipalName)
            {
                return await LdapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, parsed, password,
                    config => config.SetFormatter(_bindDnFormatter));
            }

            using var technicalConn = await LdapConnectionAdapter.CreateAsync(
                    _settings.CompanySettings.Domain,
                    LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User),
                    _settings.TechnicalAccountSettings.Password,
                    config => config.SetFormatter(_bindDnFormatter).SetLogger(_logger));

            var domain = await technicalConn.WhereAmIAsync();
            var existedUser = await FindUserByUidAsync(username, domain, technicalConn);
            if (existedUser == null) throw new LdapUserNotFoundException($"User with '{username}' does not exist in domain '{_settings.CompanySettings.Domain}'");

            return await LdapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, existedUser, password, config => config.SetFormatter(_bindDnFormatter).SetLogger(_logger));
        }

        private static async Task<LdapIdentity?> FindUserByUidAsync(string username, LdapDomain domain, LdapConnectionAdapter connection)
        {
            var user = LdapIdentity.ParseUser(username);
            var filter = LdapFilter.Create("objectclass", "user").Or("objectclass", "person")
                    .And(LdapFilter.Create("uid", user.GetUid()).Or("sAMAccountName", user.GetUid()));

            var attrs = new[] { "uid", "sAMAccountName" };
            var result = await connection.SearchQueryAsync(domain.Name, filter.Build(), LdapForNet.Native.Native.LdapSearchScope.LDAP_SCOPE_SUBTREE, attrs);

            var entry = result.FirstOrDefault();
            if (entry == null) return null;

            var attrValue = GetAnyAttrValue(entry, attrs);
            if (attrValue == null) throw new Exception($"No attribute of ({string.Join(',', attrs)}) was found in the SEARCH response");

            return LdapIdentity.ParseUser(attrValue);
        }

        private static string? GetAnyAttrValue(LdapEntry entry, params string[] attributes)
        {
            foreach (var attr in attributes)
            {
                if (!entry.DirectoryAttributes.Contains(attr)) continue;

                if (entry.DirectoryAttributes.TryGetValue(attr, out var attrValue))
                {
                    return attrValue.GetValue<string>();
                }
            }

            return null;
        }
    }
}