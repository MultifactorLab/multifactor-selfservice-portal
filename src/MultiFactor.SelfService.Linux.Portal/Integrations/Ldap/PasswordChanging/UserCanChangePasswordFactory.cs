using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class UserCanChangePasswordFactory
    {
        private readonly LdapServerInfo _serverInfo;
        private readonly LdapConnectionAdapterFactory _adapterFactory;
        private readonly ILoggerFactory _loggerFactory;

        public UserCanChangePasswordFactory(LdapServerInfo serverInfo, LdapConnectionAdapterFactory adapterFactory, ILoggerFactory loggerFactory)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IUserCanChangePassword Create()
        {
            switch (_serverInfo.Implementation)
            {
                case LdapImplementation.ActiveDirectory: 
                    return new ADUserCanChangePassword(_adapterFactory, _loggerFactory.CreateLogger<ADUserCanChangePassword>());

                default: 
                    return new DefaultUserCanChangePassword();
            }
        }
    }
}
