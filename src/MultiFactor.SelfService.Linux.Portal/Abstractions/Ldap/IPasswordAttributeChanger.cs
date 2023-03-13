using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IPasswordAttributeChanger
    {
        Task ExecuteChangeCommandAsync(string dn, string oldPassword, string newPassword, LdapConnectionAdapter connection);
    }
}
