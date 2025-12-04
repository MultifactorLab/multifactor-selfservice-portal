namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LoginResponseDto
{
    public bool Success { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public string? SessionId { get; init; }
    public string? AccessToken { get; init; }
    public string? ErrorMessage { get; init; }
    public bool MustChangePassword { get; init; }
    public DateTime? PasswordExpirationDate { get; init; }
    public string? Username { get; init; }

    public static LoginResponseDto Failed(string message) => new()
    {
        Success = false,
        Action = "Error",
        ErrorMessage = message
    };

    public bool IsMfaRequired => Action == "MfaRequired";
    public bool IsBypassSaml => Action == "BypassSaml";
    public bool IsBypassOidc => Action == "BypassOidc";
    public bool IsChangePassword => Action == "ChangePassword";
    public bool IsAccessDenied => Action == "AccessDenied";
}