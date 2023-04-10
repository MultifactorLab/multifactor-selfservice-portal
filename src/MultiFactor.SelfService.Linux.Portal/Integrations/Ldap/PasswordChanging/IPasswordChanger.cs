namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.PasswordRecovery
{
    public interface IPasswordChanger<T>
    {
        public Task<PasswordChangingResult> ChangePassword(T changeRequest);
    }
}
