namespace MultiFactor.SelfService.Linux.Portal.Dto.AdConnector;

public sealed class VerifyCredentialsRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}