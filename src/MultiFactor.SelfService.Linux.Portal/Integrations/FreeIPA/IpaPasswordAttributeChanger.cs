using LdapForNet;
using LdapForNet.Native;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA
{
    public class IpaPasswordAttributeChanger : IPasswordAttributeChanger
    {
        public async Task ExecuteChangeCommandAsync(string dn, string oldPassword, 
            string newPassword, LdapConnectionAdapter connection)
        {
            var oldPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "userpassword",
                LdapModOperation = LdapModOperation.LDAP_MOD_DELETE
            };
            oldPasswordAttribute.Add(oldPassword);

            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "userpassword",
                LdapModOperation = Native.LdapModOperation.LDAP_MOD_ADD
            };
            newPasswordAttribute.Add(newPassword);

            var response = await connection.SendRequestAsync(new ModifyRequest(dn, oldPasswordAttribute, newPasswordAttribute));
            if (response.ResultCode != ResultCode.Success)
            {
                throw new Exception($"Password change command error: {response.ErrorMessage}");
            }
        }
    }
}
