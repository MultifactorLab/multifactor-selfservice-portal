namespace MultiFactor.SelfService.Linux.Portal.Authentication
{
    public record TokenClaims(
        string Id, 
        string Identity, 
        bool MustChangePassword, 
        DateTime ValidTo,
        bool MustResetPassword,
        string SamlClaims,
        string OidcClaims,
        bool MustUnlockUser = false);
}
