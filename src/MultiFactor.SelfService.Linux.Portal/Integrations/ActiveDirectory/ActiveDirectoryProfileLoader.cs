using LdapForNet;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ActiveDirectoryProfileLoader
    {
        private readonly ActiveDirectoryConnection _connection;
        private readonly string[] _queryAttributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile" };
        private readonly LdapNames _names = new LdapNames(LdapServerType.ActiveDirectory);

        public ActiveDirectoryProfileLoader(ActiveDirectoryConnection connection)
        {
            _connection = connection;
        }

        public async Task<ActiveDirectoryProfile?> LoadProfileAsync(LdapIdentity domain, LdapIdentity user)
        {
            var searchFilter = $"(&(objectClass={_names.UserClass})({_names.Identity(user)}={user.Name}))";

            // _logger.Debug($"Querying user '{{user:l}}' in {domain.Name}", user.Name);

            var response = await _connection.QueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, _queryAttributes);

            var entry = response.SingleOrDefault();
            if (entry == null)
            {
                //_logger.Error($"Unable to find user '{{user:l}}' in {domain.Name}", user.Name);
                return null;
            }

            // base profile
            var builder = ActiveDirectoryProfile.CreateBuilder(LdapIdentity.BaseDn(entry.Dn), entry.Dn);
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

            //_logger.Debug($"User '{{user:l}}' profile loaded: {profile.DistinguishedName}", user.Name);

            return builder.Build();
        }

        private Task<IList<LdapEntry>> GetAllUserGroups(LdapIdentity domain, string distinguishedName)
        {
            var escaped = GetDistinguishedNameEscaped(distinguishedName);
            var searchFilter = $"(member:1.2.840.113556.1.4.1941:={escaped})";
            return _connection.QueryAsync(domain.Name, searchFilter, LdapSearchScope.LDAP_SCOPE_SUB, "DistinguishedName");
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