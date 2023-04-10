namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.ExpiredPasswordReset
{
    public record ExpiredPasswordChangeRequest(string username, string currentPassword, string newPassword);
}
