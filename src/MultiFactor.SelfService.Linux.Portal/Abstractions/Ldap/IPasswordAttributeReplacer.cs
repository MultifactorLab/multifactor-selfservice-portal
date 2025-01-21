using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IPasswordAttributeReplacer
    {
        Task<PasswordChangingResult> ExecuteReplaceCommandAsync(string dn, string newPassword, ILdapConnectionAdapter connection);
    }
}
