using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class PasswordAttributeReplacerFactory
    {
        private readonly LdapServerInfo _serverInfo;

        public PasswordAttributeReplacerFactory(LdapServerInfo serverInfo)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public IPasswordAttributeReplacer CreateReplacer()
        {
            if (_serverInfo.Implementation == LdapImplementation.FreeIPA)
            {
                return new IpaPasswordAttributeReplacer();
            }

            return new ADPasswordAttributeReplacer();
        }
    }
}
