namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging.ForgottenPassword
{
    public record ForgottenPasswordChangeRequest(string username, string newPassword);
}
