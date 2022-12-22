using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.FreeIPA
{
    public class IpaPasswordAttributeReplacer : IPasswordAttributeReplacer
    {
        public async Task ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection)
        {
            var newPasswordAttribute = new DirectoryModificationAttribute
            {
                Name = "userpassword",
                LdapModOperation = LdapModOperation.LDAP_MOD_REPLACE
            };
            newPasswordAttribute.Add(newPassword);

            var response = await connection.SendRequestAsync(new ModifyRequest(dn, newPasswordAttribute));
            if (response.ResultCode != ResultCode.Success)
            {
                throw new Exception($"Password change command error: {response.ErrorMessage}");
            }
        }
    }
}
