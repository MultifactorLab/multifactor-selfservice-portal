namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class BypassSamlResponseDto
{
    public bool Success { get; init; }
    public string? CallbackUrl { get; init; }
    public string? AccessToken { get; init; }
    public string? SamlResponseHtml { get; init; }
    public string? ErrorMessage { get; init; }

    public static BypassSamlResponseDto Failed(string? reason = null) => new()
    {
        Success = false,
        ErrorMessage = reason ?? "SAML bypass failed"
    };
}

