using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IPasswordAttributeReplacer
    {
        Task ExecuteReplaceCommandAsync(string dn, string newPassword, LdapConnectionAdapter connection);
    }
}
