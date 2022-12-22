using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapConnectionAdapterConfigBuilder
    {
        private readonly LdapConnectionAdapterConfig _config;

        public LdapConnectionAdapterConfigBuilder(LdapConnectionAdapterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public LdapConnectionAdapterConfigBuilder SetBindIdentityFormatter(IBindIdentityFormatter bindDnFormatter)
        {
            _config.BindIdentityFormatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
            return this;
        }

        public LdapConnectionAdapterConfigBuilder SetBindPasswordFormatter(IBindPasswordFormatter pwdFormatter)
        {
            _config.BindPasswordFormatter = pwdFormatter ?? throw new ArgumentNullException(nameof(pwdFormatter));
            return this;
        }

        public LdapConnectionAdapterConfigBuilder SetLogger(ILogger logger)
        {
            _config.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }
    }
}