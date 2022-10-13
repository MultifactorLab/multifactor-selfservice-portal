using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using System.Text;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ADPasswordAttributeReplacer : IPasswordAttributeReplacer
    {
        public async Task ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection)
        {
            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "unicodePwd",
                LdapModOperation = LdapModOperation.LDAP_MOD_REPLACE
            };
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

            var response = await connection.SendRequestAsync(new ModifyRequest(dn, newPasswordAttribute));
            if (response.ResultCode != ResultCode.Success)
            {
                throw new Exception($"Password change command error: {response.ErrorMessage}");
            }
        }
    }
}
