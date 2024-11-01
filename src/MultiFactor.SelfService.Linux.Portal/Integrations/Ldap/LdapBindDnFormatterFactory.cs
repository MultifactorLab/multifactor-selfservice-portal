using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapBindDnFormatterFactory
    {
        private readonly PortalSettings _settings;
        private readonly LdapServerInfo _serverInfo;

        public LdapBindDnFormatterFactory(PortalSettings settings, LdapServerInfo serverInfo)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public IBindIdentityFormatter CreateFormatter()
        {
            if (_serverInfo.Implementation == LdapImplementation.FreeIPA || _serverInfo.Implementation == LdapImplementation.OpenLdap)
            {
                return new IpaBindIdentityFormatter(_settings);
            }

            return new ADBindIdentityFormatter(_settings);
        }
    }
}
