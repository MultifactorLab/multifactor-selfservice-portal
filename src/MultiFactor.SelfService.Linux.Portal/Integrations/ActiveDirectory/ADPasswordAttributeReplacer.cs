using LdapForNet;
using LdapForNet.Native;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class ADPasswordAttributeReplacer : IPasswordAttributeReplacer
    {
        public async Task<PasswordChangingResult> ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection)
        {
            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "unicodePwd",
                LdapModOperation = Native.LdapModOperation.LDAP_MOD_REPLACE
            };

            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));

            var result = await connection.SendRequestAsync(new ModifyRequest(dn, newPasswordAttribute));
            return new PasswordChangingResult(
                result.ResultCode == Native.ResultCode.Success,
                result.ErrorMessage
            );
        }

    }
}
