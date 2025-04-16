using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Integrations.OpenLdap;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;

public class LockAttributeChangerFactory
{
    private readonly LdapServerInfo _serverInfo;
    private readonly IUserAttributeChanger _userAttributeChanger;
    private readonly LdapConnectionAdapterFactory _ldapConnectionAdapterFactory;
    private readonly LdapProfileLoader _ldapProfileLoader;
    private readonly ILogger<ILockAttributeChanger> _logger;

    public LockAttributeChangerFactory(
        LdapServerInfo serverInfo,
        IUserAttributeChanger userAttributeChanger,
        LdapConnectionAdapterFactory ldapConnectionAdapterFactory,
        LdapProfileLoader ldapProfileLoader,
        ILogger<ILockAttributeChanger> logger)
    {
        _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        _userAttributeChanger = userAttributeChanger;
        _ldapProfileLoader = ldapProfileLoader;
        _ldapConnectionAdapterFactory = ldapConnectionAdapterFactory;
        _logger = logger;
    }

    public ILockAttributeChanger CreateChanger()
    {
        switch (_serverInfo.Implementation)
        {
            case LdapImplementation.FreeIPA:
                return new IpaLockAttributeChanger(
                    _userAttributeChanger,
                    _ldapConnectionAdapterFactory,
                    _ldapProfileLoader,
                    _logger);
            case LdapImplementation.OpenLdap:
                return new OpenLdapLockAttributeChanger(
                    _userAttributeChanger,
                    _ldapConnectionAdapterFactory,
                    _ldapProfileLoader,
                    _logger);
            case LdapImplementation.Samba:
            case LdapImplementation.ActiveDirectory:
                return new AdLockAttributeChanger(
                    _userAttributeChanger,
                    _ldapConnectionAdapterFactory,
                    _ldapProfileLoader,
                    _logger);
            default:
                return new AdLockAttributeChanger(
                    _userAttributeChanger,
                    _ldapConnectionAdapterFactory,
                    _ldapProfileLoader,
                    _logger);
        }
    }
}