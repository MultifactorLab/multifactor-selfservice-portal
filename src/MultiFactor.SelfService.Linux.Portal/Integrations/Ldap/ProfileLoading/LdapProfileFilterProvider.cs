using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public interface ILdapProfileFilterProvider
    {
        ILdapFilter GetProfileSearchFilter(LdapIdentity user);
    }

    public class LdapProfileFilterProvider :ILdapProfileFilterProvider
    {
        private readonly LdapServerInfo _serverInfo;

        public LdapProfileFilterProvider(LdapServerInfo serverInfo)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public ILdapFilter GetProfileSearchFilter(LdapIdentity user)
        {
            var searchFilter = LdapFilter.Create("objectClass", "user", "person", "memberof");
            switch (_serverInfo.Implementation)
            {
                case LdapImplementation.FreeIPA:
                case LdapImplementation.OpenLdap:
                    {
                        return searchFilter.And(LdapFilter.Create("uid", $"{user.GetUid()}"));
                    }
                case LdapImplementation.ActiveDirectory:
                case LdapImplementation.Samba:
                    {
                        if (user.Type == IdentityType.UserPrincipalName)
                        {
                            return searchFilter.And(LdapFilter.Create("userPrincipalName", user.Name));
                        }

                        if (user.Type == IdentityType.Uid)
                        {
                            return searchFilter.And("sAMAccountName", user.Name);
                        }

                        throw new NotImplementedException($"Unexpected user identity type: {user.Type}");
                    }

                default: throw new NotImplementedException("Unknown LDAP server implementation");
            }
        }
    }
}
