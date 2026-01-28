using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

/// <summary>
/// Response DTO from Identity endpoint.
/// </summary>
public sealed class IdentityResponseDto
{
    public bool Success { get; init; }
    public IdentityAction Action { get; init; }
    public string RedirectUrl { get; init; }
    public string Username { get; init; }
    public string ErrorMessage { get; init; }

    public static IdentityResponseDto Failed(string message) => new()
    {
        Success = false,
        Action = IdentityAction.Error,
        ErrorMessage = message
    };
}

