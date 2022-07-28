namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public record TokenClaims(string Id, string Identity, bool MustChangePassword, DateTime ValidTo);
}
