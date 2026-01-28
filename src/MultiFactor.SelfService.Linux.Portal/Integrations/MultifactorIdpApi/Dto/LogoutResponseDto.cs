namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LogoutResponseDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static LogoutResponseDto Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

