using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public class LdapProfileLoader
    {
        private readonly ILdapProfileFilterProvider _profileFilterProvider;
        private readonly ILogger<LdapProfileLoader> _logger;
        private readonly AdditionalClaimsMetadata _additionalClaimsMetadata;
        private readonly PortalSettings _portalSettings;

        const string _memberOfAttr = "memberOf";
        private readonly string[] _queryAttributes = {
            "DistinguishedName",
            "displayName",
            "mail",
            "email",
            "telephoneNumber",
            "mobile",
            "userPrincipalName",
            "pwdLastSet",
            "msDS-UserPasswordExpiryTimeComputed",
            _memberOfAttr
        };

        public LdapProfileLoader(
            ILdapProfileFilterProvider profileFilterProvider,
            ILogger<LdapProfileLoader> logger,
            AdditionalClaimsMetadata additionalClaimsMetadata,
            PortalSettings settings)
        {
            _profileFilterProvider = profileFilterProvider ?? throw new ArgumentNullException(nameof(profileFilterProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _additionalClaimsMetadata = additionalClaimsMetadata ?? throw new ArgumentNullException(nameof(additionalClaimsMetadata));
            _portalSettings = settings;
        }

        public async Task<LdapProfile> LoadProfileAsync(LdapDomain domain, LdapIdentity user, ILdapConnectionAdapter connection)
        {
            var searchFilter = _profileFilterProvider.GetProfileSearchFilter(user);

            _logger.LogDebug("Querying user '{user:l}' in '{domain:l}'", user, domain);
            var allAttrs = _queryAttributes
                .Concat(_additionalClaimsMetadata.RequiredAttributes)
                .Distinct(new OrdinalIgnoreCaseStringComparer())
                .ToList();

            if (_portalSettings.ActiveDirectorySettings.UseUpnAsIdentity)
            {
                allAttrs.Add("userPrincipalName");
            }

            var response = await connection.SearchQueryAsync(domain.Name, searchFilter.Build(), LdapSearchScope.LDAP_SCOPE_SUB, allAttrs.ToArray());

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                _logger.LogError("Unable to find user '{user:l}' in '{domain:l}'", user, domain);
                return null;
            }

            var builder = LdapProfile.Create(LdapIdentity.BaseDn(entry.Dn), entry.Dn);
            var attributes = entry.DirectoryAttributes;

            foreach (var attr in allAttrs.Where(x => !x.Equals(_memberOfAttr, StringComparison.OrdinalIgnoreCase)))
            {
                if (attributes.TryGetValue(attr, out var attrValue))
                {
                    builder.AddAttribute(attr, attrValue.GetValues<string>());
                }
            }

            var userGroups = new List<string>();
            if (attributes.TryGetValue(_memberOfAttr, out var memberOfAttr))
            {
                var val = memberOfAttr.GetValues<string>().Select(LdapIdentity.DnToCn).ToArray();
                userGroups.AddRange(val);
            }
            else
            {
                _logger.LogWarning("The MemberOf attribute is empty.");
            }

            if(_portalSettings.CompanySettings.LoadActiveDirectoryNestedGroups)
            {
                _logger.LogDebug("The LoadActiveDirectoryNestedGroups setting is set to true. Loading nested groups...");
                var allGroups = await GetAllUserGroups(domain, entry.Dn, connection, _portalSettings.CompanySettings.SplittedNestedGroupsDomain);
                var val = allGroups.Select(group => LdapIdentity.DnToCn(group.Dn)).ToArray();
                userGroups.AddRange(val);

            }
            builder.AddAttribute(_memberOfAttr, userGroups.Distinct(StringComparer.OrdinalIgnoreCase));
            _logger.LogDebug("User '{user:l}' profile loaded. DN: '{DN:l}'", user, entry.Dn);

            return builder.Build();
        }

        private async Task<IList<LdapEntry>> GetAllUserGroups(LdapDomain domain, string distinguishedName, ILdapConnectionAdapter connection, string[] nestedGroups = null)
        {
            var allUserGroupsNames = new List<LdapEntry>();
            var escaped = GetDistinguishedNameEscaped(distinguishedName);
            var searchFilter = $"(&(objectCategory=group)(member:1.2.840.113556.1.4.1941:={escaped}))";
            _logger.LogDebug("GetAllUserGroups. {searchFilter}", searchFilter);
            var baseDnsForSearch = nestedGroups?.Length > 0 ? nestedGroups : new[] { domain.Name };
            foreach (var baseDn in baseDnsForSearch)
            {
                var searchResult = await connection.SearchQueryAsync(
                    baseDn,
                    searchFilter,
                    LdapSearchScope.LDAP_SCOPE_SUB,
                    "DistinguishedName");

                allUserGroupsNames.AddRange(searchResult.Select(x => x));
            }
            return allUserGroupsNames;
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