using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using System.Text;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ADPasswordAttributeChanger : IPasswordAttributeChanger
    {
        public async Task ExecuteChangeCommandAsync(
            string dn,
            string oldPassword, 
            string newPassword,
            ILdapConnectionAdapter connection)
        {
            var oldPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "unicodePwd",
                LdapModOperation = LdapModOperation.LDAP_MOD_DELETE
            };
            oldPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{oldPassword}\""));

            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "unicodePwd",
                LdapModOperation = LdapModOperation.LDAP_MOD_ADD
            };
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

            var response = await connection.SendRequestAsync(new ModifyRequest(dn, oldPasswordAttribute, newPasswordAttribute));
            if (response.ResultCode != ResultCode.Success)
            {
                throw new Exception($"Password change command error: {response.ErrorMessage}");
            }
        }
    }
}
