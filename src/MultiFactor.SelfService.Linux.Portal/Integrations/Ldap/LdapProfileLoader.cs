using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapProfileLoader
    {
        private readonly LdapConnectionAdapter _connection;
        private readonly ILdapBindDnFormatter _bindDnFormatter;
        private readonly ILogger _logger;
        private readonly string[] _queryAttributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile", "memberOf" };

        public LdapProfileLoader(LdapConnectionAdapter connection, ILdapBindDnFormatter bindDnFormatter, ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _bindDnFormatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LdapProfile?> LoadProfileAsync(LdapDomain domain, LdapIdentity user)
        {
            var searchFilter = LdapFilter.Create("objectClass", "user", "person");
            if (user.Type == IdentityType.UserPrincipalName && !_bindDnFormatter.BindDnIsDefined)
            {
                searchFilter = searchFilter.And(LdapFilter.Create("userPrincipalName", user.Name));
            }
            else if (_bindDnFormatter.BindDnIsDefined)
            {
                searchFilter = searchFilter.And(LdapFilter.Create("uid", $"{user.GetUid()}"));
            } 
            else if (user.Type == IdentityType.Uid)
            {
                searchFilter = searchFilter.And(LdapFilter.Create("uid", user.Name).Or("sAMAccountName", user.Name));
            }
            else
            {
                throw new NotImplementedException($"Unexpected user identity type: {user.Type}");
            }

            _logger.LogDebug("Querying user '{user:l}' in {domain:l}", user, domain);
            var response = await _connection.SearchQueryAsync(domain.Name, searchFilter.Build(), LdapSearchScope.LDAP_SCOPE_SUB, _queryAttributes);

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
                var allGroups = await GetAllUserGroups(domain, entry.Dn);
                builder.AddMemberOfValues(allGroups.Select(entry => LdapIdentity.DnToCn(entry.Dn)).ToArray());
            }

            _logger.LogDebug("User '{user:l}' profile loaded: {DN:l}", user, entry.Dn);

            return builder.Build();
        }

        private Task<IList<LdapEntry>> GetAllUserGroups(LdapDomain domain, string distinguishedName)
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