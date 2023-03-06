using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class PasswordAttributeChangerFactory
    {
        private readonly LdapServerInfo _serverInfo;

        public PasswordAttributeChangerFactory(LdapServerInfo serverInfo)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public IPasswordAttributeChanger CreateChanger()
        {
            if (_serverInfo.Implementation == LdapImplementation.FreeIPA)
            {
                return new IpaPasswordAttributeChanger();
            }

            return new ADPasswordAttributeChanger();
        }
    }
}
