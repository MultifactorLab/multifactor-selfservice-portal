using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class LdapExtensions
    {
        public static async Task<LdapProfile> GetLdapProfile(this LdapProfileLoader profileLoader, LdapConnectionAdapter connection, string username)
        {
            var user = LdapIdentity.ParseUser(username);
            var domain = await connection.WhereAmIAsync();
            var profile = await profileLoader.LoadProfileAsync(domain, user, connection);
            if (profile == null)
            {
                throw new Exception("Unable to change password: profile not loaded");
            }
            return profile;
        }
    }
}
