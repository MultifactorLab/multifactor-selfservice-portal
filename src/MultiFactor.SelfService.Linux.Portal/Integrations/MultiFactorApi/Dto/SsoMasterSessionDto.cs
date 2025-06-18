namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public record SsoMasterSessionDto(
        string MasterSessionId,
        List<string> SamlSessionIds
        );
}
