using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ADBindIdentityFormatter : IBindIdentityFormatter
    {
        private readonly PortalSettings _settings;

        public ADBindIdentityFormatter(PortalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool BaseDnIsDefined => !string.IsNullOrEmpty(_settings.LdapBaseDn);

        public string BaseDn => _settings.LdapBaseDn;

        public string FormatIdentity(LdapIdentity user, string ldapUri)
        {
            if (user.Type == IdentityType.UserPrincipalName)
            {
                return user.Name;
            }

            //try create upn from domain name
            if (Uri.IsWellFormedUriString(ldapUri, UriKind.Absolute))
            {
                var uri = new Uri(ldapUri);
                if (uri.PathAndQuery != null && uri.PathAndQuery != "/")
                {
                    var fqdn = LdapIdentity.DnToFqdn(uri.PathAndQuery);
                    return $"{user.Name}@{fqdn}";
                }
            }

            return user.Name;
        }
    }
}
