namespace MultiFactor.SelfService.Linux.Portal.Dto.AdConnector;

public sealed class VerifyCredentialsResponse
{
    public bool IsAuthenticated { get; init; }
    public bool IsBypass { get; init; }
    public bool UserMustChangePassword { get; init; }
    public DateTime PasswordExpirationDate { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Username { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? CustomIdentity { get; init; }
    public string? Reason { get; init; }
}