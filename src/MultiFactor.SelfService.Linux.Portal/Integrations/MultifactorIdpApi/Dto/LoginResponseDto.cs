using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LoginResponseDto
{
    public bool Success { get; init; }
    public LoginAction Action { get; init; }
    public string RedirectUrl { get; init; }
    public string SessionId { get; init; }
    public string AccessToken { get; init; }
    public string ErrorMessage { get; init; }
    public bool MustChangePassword { get; init; }
    public DateTime? PasswordExpirationDate { get; init; }
    public string Username { get; init; }

    public static LoginResponseDto Failed(string message) => new()
    {
        Success = false,
        Action = LoginAction.Error,
        ErrorMessage = message
    };
}