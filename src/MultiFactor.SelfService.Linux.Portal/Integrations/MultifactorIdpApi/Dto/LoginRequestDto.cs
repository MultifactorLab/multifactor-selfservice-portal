namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public class LoginRequestDto
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string CallbackUrl { get; init; }
    public string? SamlSessionId { get; init; }
    public string? OidcSessionId { get; init; }
    public Dictionary<string, string>? AdditionalClaims { get; init; }
    public required SspSettingsDto Settings { get; init; }
}