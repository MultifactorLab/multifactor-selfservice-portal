using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface ILdapBindDnFormatter
    {
        bool BindDnIsDefined { get; }
        string BindDn { get; }
        string FormatBindDn(LdapIdentity user, string ldapUri);
    }

    public class DefaultLdapBindDnFormatter : ILdapBindDnFormatter
    {
        public bool BindDnIsDefined => false;

        public string BindDn => string.Empty;

        public string FormatBindDn(LdapIdentity user, string ldapUri) => user.Name;
    }
}
