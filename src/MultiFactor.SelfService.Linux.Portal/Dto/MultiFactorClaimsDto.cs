namespace MultiFactor.SelfService.Linux.Portal.Dto
{
    public record MultiFactorClaimsDto (string SamlSessionId, string OidcSessionId)
    {
        public bool HasSamlSession() => !string.IsNullOrEmpty(SamlSessionId);
        public bool HasOidcSession() => !string.IsNullOrEmpty(OidcSessionId);
    }
}
