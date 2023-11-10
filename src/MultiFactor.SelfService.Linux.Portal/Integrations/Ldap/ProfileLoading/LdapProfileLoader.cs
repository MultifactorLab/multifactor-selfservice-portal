using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public class LdapProfileLoader
    {
        private readonly LdapProfileFilterProvider _profileFilterProvider;
        private readonly ILogger<LdapProfileLoader> _logger;
        private readonly AdditionalClaimsMetadata _additionalClaimsMetadata;

        const string _memberOfAttr = "memberOf";
        private readonly string[] _queryAttributes = new[]
        {
            "DistinguishedName",
            "displayName",
            "mail",
            "telephoneNumber",
            "mobile",
            _memberOfAttr
        };

        public LdapProfileLoader(LdapProfileFilterProvider profileFilterProvider, ILogger<LdapProfileLoader> logger,
            AdditionalClaimsMetadata additionalClaimsMetadata)
        {
            _profileFilterProvider = profileFilterProvider ?? throw new ArgumentNullException(nameof(profileFilterProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _additionalClaimsMetadata = additionalClaimsMetadata ?? throw new ArgumentNullException(nameof(additionalClaimsMetadata));
        }

        public async Task<LdapProfile?> LoadProfileAsync(LdapDomain domain, LdapIdentity user,
            LdapConnectionAdapter connection)
        {
            var searchFilter = _profileFilterProvider.GetProfileSearchFilter(user);

            _logger.LogDebug("Querying user '{user:l}' in '{domain:l}'", user, domain);
            var allAttrs = _queryAttributes
                .Concat(_additionalClaimsMetadata.RequiredAttributes)
                .Distinct(new OrdinalIgnoreCaseStringComparer())
                .ToArray();
            var response = await connection.SearchQueryAsync(domain.Name, searchFilter.Build(), LdapSearchScope.LDAP_SCOPE_SUB, allAttrs);

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.LogError("Unable to find user '{user:l}' in '{domain:l}'", user, domain);
                return null;
            }

            var builder = LdapProfile.CreateBuilder(LdapIdentity.BaseDn(entry.Dn), entry.Dn);
            var attributes = entry.DirectoryAttributes;


            foreach (var attr in allAttrs.Where(x => !x.Equals(_memberOfAttr, StringComparison.OrdinalIgnoreCase)))
            {
                if (attributes.TryGetValue(attr, out var attrValue))
                {
                    builder.AddAttribute(attr, attrValue.GetValues<string>());
                }
            }

            if (attributes.TryGetValue(_memberOfAttr, out var memberOfAttr))
            {
                var val = memberOfAttr.GetValues<string>().Select(entry => LdapIdentity.DnToCn(entry)).ToArray();
                builder.AddAttribute(_memberOfAttr, val);
            }
            else
            {
                var allGroups = await GetAllUserGroups(domain, entry.Dn, connection);
                var val = allGroups.Select(entry => LdapIdentity.DnToCn(entry.Dn)).ToArray();
                builder.AddAttribute(_memberOfAttr, val);
            }

            _logger.LogDebug("User '{user:l}' profile loaded. DN: '{DN:l}'", user, entry.Dn);

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