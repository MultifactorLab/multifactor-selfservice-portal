using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;

namespace MultiFactor.SelfService.Linux.Portal.Tests;

public class LdapProfileFilterProviderTests
{
    [Theory]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.Samba)]
    public void GetProfileSearchFilter_ForAdUid_UsesNarrowUserFilter(LdapImplementation implementation)
    {
        var provider = new LdapProfileFilterProvider(new LdapServerInfo(implementation));

        var filter = provider.GetProfileSearchFilter(new LdapIdentity("alice", IdentityType.Uid)).Build();

        Assert.Equal("(&(objectClass=user)(sAMAccountName=alice))", filter);
    }

    [Theory]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.Samba)]
    public void GetProfileSearchFilter_ForAdUpn_UsesNarrowUserFilter(LdapImplementation implementation)
    {
        var provider = new LdapProfileFilterProvider(new LdapServerInfo(implementation));

        var filter = provider.GetProfileSearchFilter(
            new LdapIdentity("alice@example.com", IdentityType.UserPrincipalName)).Build();

        Assert.Equal("(&(objectClass=user)(userPrincipalName=alice@example.com))", filter);
    }

    [Theory]
    [InlineData(LdapImplementation.FreeIPA)]
    [InlineData(LdapImplementation.OpenLdap)]
    public void GetProfileSearchFilter_ForLdapUser_PreservesCompatibleFilter(LdapImplementation implementation)
    {
        var provider = new LdapProfileFilterProvider(new LdapServerInfo(implementation));

        var filter = provider.GetProfileSearchFilter(new LdapIdentity("alice", IdentityType.Uid)).Build();

        Assert.Equal("(&(|(objectClass=user)(objectClass=person)(objectClass=memberof))(uid=alice))", filter);
    }
}