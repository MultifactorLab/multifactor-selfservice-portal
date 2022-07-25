namespace MultiFactor.SelfService.Linux.Portal.Dto
{
    public record MultiFactorClaimsDto (string SamlSession, string OidcSession)
    {
        public bool HasSamlSession() => !string.IsNullOrEmpty(SamlSession);
        public bool HasOidcSession() => !string.IsNullOrEmpty(OidcSession);
    }
}
