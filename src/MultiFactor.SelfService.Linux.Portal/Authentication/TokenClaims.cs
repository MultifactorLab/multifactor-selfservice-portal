namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public record TokenClaims(
        string Id,
        string Identity,
        string RawUserName,
        bool MustChangePassword, 
        DateTime ValidTo,
        bool MustResetPassword,
        string SamlClaim,
        string OidcClaim,
        bool MustUnlockUser = false);
}
