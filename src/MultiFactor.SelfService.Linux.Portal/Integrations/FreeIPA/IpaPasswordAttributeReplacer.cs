using LdapForNet;
using LdapForNet.Native;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA
{
    public class IpaPasswordAttributeReplacer : IPasswordAttributeReplacer
    {
        public async Task<PasswordChangingResult> ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection)
        {
            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "userpassword",
                LdapModOperation = Native.LdapModOperation.LDAP_MOD_REPLACE
            };

            newPasswordAttribute.Add(newPassword);

            var result = await connection.SendRequestAsync(new ModifyRequest(dn, newPasswordAttribute));
            return new PasswordChangingResult(
                result.ResultCode == Native.ResultCode.Success,
                result.ErrorMessage
            );
        }

    }
}
