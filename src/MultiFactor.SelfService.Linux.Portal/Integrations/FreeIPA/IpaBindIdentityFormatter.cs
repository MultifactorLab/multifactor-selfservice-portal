using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA
{
    public class IpaBindIdentityFormatter : IBindIdentityFormatter
    {
        private readonly PortalSettings _settings;

        public IpaBindIdentityFormatter(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool BaseDnIsDefined => !string.IsNullOrEmpty(_settings.LdapBaseDn);

        public string BaseDn => _settings.LdapBaseDn;

        public string FormatIdentity(LdapIdentity user, string ldapUri)
        {
            var bindDn = $"uid={user.GetUid()}";
            if (BaseDnIsDefined)
            {
                bindDn += "," + _settings.LdapBaseDn;
            }

            return bindDn;
        }
    }
}
