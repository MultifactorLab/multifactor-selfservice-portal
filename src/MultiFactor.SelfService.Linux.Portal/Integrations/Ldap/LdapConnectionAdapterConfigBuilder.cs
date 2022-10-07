using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapConnectionAdapterConfigBuilder
    {
        private readonly LdapConnectionAdapterConfig _config;

        public LdapConnectionAdapterConfigBuilder(LdapConnectionAdapterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public LdapConnectionAdapterConfigBuilder SetFormatter(ILdapBindDnFormatter bindDnFormatter)
        {
            _config.Formatter = bindDnFormatter ?? throw new ArgumentNullException(nameof(bindDnFormatter));
            return this;
        }

        public LdapConnectionAdapterConfigBuilder SetLogger(ILogger logger)
        {
            _config.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }
    }
}