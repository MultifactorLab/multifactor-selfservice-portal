using LdapForNet.Adsddl;
using LdapForNet.Adsddl.data;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public static class SddlExtensions
    {
        /// <summary>
        /// Returns TRUE if user is allowed to change password by AD policy.
        /// </summary>
        /// <param name="sddl">Security Descriptor Definition Languag object.</param>
        /// <returns>True or False</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool UserCannotChangePassword(this Sddl sddl)
        {
            if (sddl is null) throw new ArgumentNullException(nameof(sddl));           

            const string everyone_lookup = "S-1-1-0";
            const string self_lookup = "S-1-5-10";

            var dacl = sddl.GetDacl();
            var aces = dacl?.GetAces();
            if (aces == null || !aces.Any()) return false;

            var everyone = aces.FirstOrDefault(x => x.GetSid().ToString() == everyone_lookup);
            var self = aces.FirstOrDefault(x => x.GetSid().ToString() == self_lookup);

            if (everyone == null || self == null) return false;

            return everyone.GetAceType() == AceType.AccessDeniedObjectAceType && self.GetAceType() == AceType.AccessDeniedObjectAceType;
        }
    }
}
