namespace MultiFactor.SelfService.Linux.Portal.Dto
{
    public record MultiFactorClaimsDto (string? SamlSession, string? OidcSession);
}
