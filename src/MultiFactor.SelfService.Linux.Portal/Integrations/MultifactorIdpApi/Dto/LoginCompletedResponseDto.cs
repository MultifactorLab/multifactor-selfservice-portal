using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LoginCompletedResponseDto
{
    public bool Success { get; set; }
    public LoginCompletedAction Action { get; init; }
    public string RedirectUrl { get; init; }
    public string SessionId { get; init; }
    public bool MustChangePassword { get; init; }
    public string ErrorMessage { get; init; }
    public DateTime? TokenExpirationDate { get; init; }
    public string Identity { get; init; }
    public string SamlSessionId { get; init; }
    public string OidcSessionId { get; init; }
    public string RawUserName { get; init; }
    
    public static LoginCompletedResponseDto Failed(string message) => new()
    {
        Success = false,
        Action = LoginCompletedAction.Error,
        ErrorMessage = message
    };
}

