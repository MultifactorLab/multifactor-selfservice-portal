using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IBindIdentityFormatter
    {
        bool BaseDnIsDefined { get; }
        string BaseDn { get; }
        string FormatIdentity(LdapIdentity user, string ldapUri);
    }

    public class DefaultBindIdentityFormatter : IBindIdentityFormatter
    {
        public bool BaseDnIsDefined => false;
        public string BaseDn => string.Empty;
        public string FormatIdentity(LdapIdentity user, string ldapUri) => user.Name;
    }
}
