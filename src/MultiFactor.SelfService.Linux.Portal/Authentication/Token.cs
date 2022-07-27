namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public record Token(string Id, string Identity, bool MustChangePassword, DateTime ValidTo);
}
