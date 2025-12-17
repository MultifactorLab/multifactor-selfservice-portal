namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LoginCompletedResponseDto
{
    public string Action { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public string? SessionId { get; init; }
    public bool MustChangePassword { get; init; }
    public DateTime? TokenExpirationDate { get; init; }
    public string? Identity { get; init; }
    public string? SamlSessionId { get; init; }
    public string? OidcSessionId { get; init; }

    public bool IsBypassSaml => Action == "BypassSaml";
    public bool IsBypassOidc => Action == "BypassOidc";
    public bool IsChangePassword => Action == "ChangePassword";
    public bool IsAuthenticated => Action == "Authenticated";
}

