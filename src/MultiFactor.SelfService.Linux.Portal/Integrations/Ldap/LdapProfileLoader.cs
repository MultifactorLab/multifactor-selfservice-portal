using LdapForNet;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapProfileLoader
    {
        private readonly LdapConnectionAdapter _connection;
        private readonly LdapNames _names;
        private readonly ILogger _logger;
        private readonly string[] _queryAttributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile" };

        public LdapProfileLoader(LdapConnectionAdapter connection, LdapNames names, ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LdapProfile?> LoadProfileAsync(LdapIdentity domain, LdapIdentity user)
        {
            var searchFilter = $"(&(objectClass={_names.UserClass})({_names.Identity(user)}={user.Name}))";

            _logger.LogDebug("Querying user '{user:l}' in {domain:l}", user, domain);
            var response = await _connection.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, _queryAttributes);

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.LogError("Unable to find user '{user:l}' in {domain:l}", user, domain);
                return null;
            }

            // base profile
            var builder = LdapProfile.CreateBuilder(LdapIdentity.BaseDn(entry.Dn), entry.Dn);
            var attributes = entry.DirectoryAttributes;

            if (attributes.TryGetValue("displayName", out var displayNameAttr))
            {
                builder.SetDisplayName(displayNameAttr.GetValue<string>());
            }

            if (attributes.TryGetValue("mail", out var mailAttr))
            {
                builder.SetEmail(mailAttr.GetValue<string>());
            }

            if (attributes.TryGetValue("telephoneNumber", out var phoneAttr))
            {
                builder.SetPhone(phoneAttr.GetValue<string>());
            }

            if (attributes.TryGetValue("mobile", out var mobileAttr))
            {
                builder.SetMobile(mobileAttr.GetValue<string>());
            }

            var allGroups = await GetAllUserGroups(domain, entry.Dn);
            builder.AddMemberOfValues(allGroups.Select(entry => LdapIdentity.DnToCn(entry.Dn)).ToArray());

            _logger.LogDebug("User '{user:l}' profile loaded: {DN:l}", user, entry.Dn);

            return builder.Build();
        }

        private Task<IList<LdapEntry>> GetAllUserGroups(LdapIdentity domain, string distinguishedName)
        {
            var escaped = GetDistinguishedNameEscaped(distinguishedName);
            var searchFilter = $"(member:1.2.840.113556.1.4.1941:={escaped})";
            return _connection.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
        }

        private string GetDistinguishedNameEscaped(string distinguishedName)
        {
            var ret = distinguishedName
                    .Replace("(", @"\28")
                    .Replace(")", @"\29");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ret = ret.Replace("\"", "\\\""); //quotes
                ret = ret.Replace("\\,", "\\5C,"); //comma
            }

            return ret;
        }
    }
}