using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IUserCanChangePassword
    {
        Task<bool> CheckAsync(LdapIdentity userName);
    }

    public class DefaultUserCanChangePassword : IUserCanChangePassword
    {
        public Task<bool> CheckAsync(LdapIdentity userName) => Task.FromResult(true);
    }
}
