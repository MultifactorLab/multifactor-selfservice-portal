using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

public interface IUserAttributeChanger
{
    Task ChangeAttributeValueAsync(string dn, string attributeName, object attributeValue, ILdapConnectionAdapter connection);
}