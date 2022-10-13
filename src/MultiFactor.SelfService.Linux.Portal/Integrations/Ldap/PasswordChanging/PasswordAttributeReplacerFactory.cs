using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class PasswordAttributeReplacerFactory
    {
        private readonly PortalSettings _settings;

        public PasswordAttributeReplacerFactory(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IPasswordAttributeReplacer CreateReplacer()
        {
            if (!string.IsNullOrWhiteSpace(_settings.LdapBaseDn))
            {
                return new IpaPasswordAttributeReplacer();
            }

            return new ADPasswordAttributeReplacer();
        }
    }
}
