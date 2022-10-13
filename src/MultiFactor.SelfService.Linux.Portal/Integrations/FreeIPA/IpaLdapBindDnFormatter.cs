using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA
{
    public class IpaLdapBindDnFormatter : ILdapBindDnFormatter
    {
        private readonly PortalSettings _settings;

        public IpaLdapBindDnFormatter(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool BindDnIsDefined => !string.IsNullOrEmpty(_settings.LdapBaseDn);

        public string BindDn => _settings.LdapBaseDn;

        public string FormatBindDn(LdapIdentity user, string ldapUri)
        {
            var bindDn = $"{Core.Constants.UID_ATTRIBUTE}{user.GetUid()}";
            if (!string.IsNullOrEmpty(_settings.LdapBaseDn))
            {
                bindDn += "," + _settings.LdapBaseDn;
            }

            return bindDn;
        }
    }
}
