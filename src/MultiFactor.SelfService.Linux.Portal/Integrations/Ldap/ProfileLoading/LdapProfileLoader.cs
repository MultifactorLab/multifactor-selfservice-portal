using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public class LdapProfileLoader
    {
        private readonly LdapProfileFilterProvider _profileFilterProvider;
        private readonly ILogger<LdapProfileLoader> _logger;
        private readonly string[] _queryAttributes = new[]
        {
            "DistinguishedName",
            "displayName",
            "mail",
            "telephoneNumber",
            "mobile",
            "memberOf"
        };

        public LdapProfileLoader(LdapProfileFilterProvider profileFilterProvider, ILogger<LdapProfileLoader> logger)
        {
            _profileFilterProvider = profileFilterProvider ?? throw new ArgumentNullException(nameof(profileFilterProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LdapProfile?> LoadProfileAsync(LdapDomain domain, LdapIdentity user,
            LdapConnectionAdapter connection)
        {
            var searchFilter = _profileFilterProvider.GetProfileSearchFilter(user);

            _logger.LogDebug("Querying user '{user:l}' in {domain:l}", user, domain);
            var response = await connection.SearchQueryAsync(domain.Name, searchFilter.Build(), LdapSearchScope.LDAP_SCOPE_SUB, _queryAttributes);

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

            if (attributes.TryGetValue("memberOf", out var memberOfAttr))
            {
                builder.AddMemberOfValues(memberOfAttr.GetValues<string>()
                    .Select(entry => LdapIdentity.DnToCn(entry)).ToArray());
            }
            else
            {
                var allGroups = await GetAllUserGroups(domain, entry.Dn, connection);
                builder.AddMemberOfValues(allGroups.Select(entry => LdapIdentity.DnToCn(entry.Dn)).ToArray());
            }

            _logger.LogDebug("User '{user:l}' profile loaded: {DN:l}", user, entry.Dn);

            return builder.Build();
        }

        private Task<IList<LdapEntry>> GetAllUserGroups(LdapDomain domain, string distinguishedName, LdapConnectionAdapter connection)
        {
            var escaped = GetDistinguishedNameEscaped(distinguishedName);
            var searchFilter = $"(member:1.2.840.113556.1.4.1941:={escaped})";
            return connection.SearchQueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
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