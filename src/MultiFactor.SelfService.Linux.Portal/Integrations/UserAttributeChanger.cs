using System.Text;
using LdapForNet;
using LdapForNet.Native;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Integrations;

public class UserAttributeChanger : IUserAttributeChanger
{
    public async Task ChangeAttributeValueAsync(
        string dn,
        string attributeName,
        object attributeValue,
        ILdapConnectionAdapter connection)
    {
        if (string.IsNullOrWhiteSpace(dn))
            throw new ArgumentNullException(nameof(dn));
        if (string.IsNullOrWhiteSpace(attributeName))
            throw new ArgumentNullException(nameof(attributeName));
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        var request = BuildModifyRequest(dn, attributeName, attributeValue);
        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode != Native.ResultCode.Success)
        {
            throw new Exception($"Failed to set attribute: {response.ResultCode}");
        }
    }

    private ModifyRequest BuildModifyRequest(
        string dn,
        string attributeName,
        object attributeValue)
    {
        var attrName = attributeName;

        var attribute = new DirectoryModificationAttribute
        {
            Name = attrName,
            LdapModOperation = Native.LdapModOperation.LDAP_MOD_REPLACE
        };

        var bytes = Encoding.UTF8.GetBytes(attributeValue?.ToString());

        attribute.Add(bytes);

        return new ModifyRequest(dn, attribute);
    }
}