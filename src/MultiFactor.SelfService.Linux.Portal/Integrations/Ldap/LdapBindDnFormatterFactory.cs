using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapBindDnFormatterFactory
    {
        private readonly PortalSettings _settings;

        public LdapBindDnFormatterFactory(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ILdapBindDnFormatter CreateFormatter()
        {
            if (!string.IsNullOrWhiteSpace(_settings.LdapBindDn))
            {
                return new GenericLdapBindDnFormatter(_settings);
            }

            return new ADLdapBindDnFormatter(_settings);
        }
    }
}
