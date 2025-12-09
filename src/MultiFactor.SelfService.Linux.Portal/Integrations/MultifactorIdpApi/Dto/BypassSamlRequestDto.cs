namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class BypassSamlRequestDto
{
    public required string SamlSessionId { get; init; }
}

