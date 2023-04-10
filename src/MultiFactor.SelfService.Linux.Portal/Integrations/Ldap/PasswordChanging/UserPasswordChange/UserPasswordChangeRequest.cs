namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.UserPasswordChange
{
    public record UserPasswordChangeRequest(string username, string currentPassword, string newPassword);
}
