using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapConnectionAdapterConfig
    {
        public ILdapBindDnFormatter Formatter { get; set; } = new DefaultLdapBindDnFormatter();
        public ILogger? Logger { get; set; }
    }
}