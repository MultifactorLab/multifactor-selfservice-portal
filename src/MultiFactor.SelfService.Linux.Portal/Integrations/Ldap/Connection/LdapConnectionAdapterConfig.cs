using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapConnectionAdapterConfig
    {
        public IBindIdentityFormatter BindIdentityFormatter { get; set; } = new DefaultBindIdentityFormatter();
        public IBindPasswordFormatter BindPasswordFormatter { get; set; } = new DefaultBindPasswordFormatter();
        public ILogger? Logger { get; set; }
    }
}