namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public class LoginRequestDto
{
    // Credential verification results (already verified by SSP)
    public required VerifiedCredentialsDto VerifiedCredentials { get; init; }
    public string? SamlSessionId { get; init; }
    public string? OidcSessionId { get; init; }
    public Dictionary<string, string>? AdditionalClaims { get; init; }
    public required string LoginCompletedCallbackUrl { get; init; }
    public required SspSettingsDto Settings { get; init; }
}

public class VerifiedCredentialsDto
{
    public required bool IsAuthenticated { get; init; }
    public required bool IsBypass { get; init; }
    public required bool UserMustChangePassword { get; init; }
    public DateTime? PasswordExpirationDate { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public required string Username { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? CustomIdentity { get; init; }
    public string? Reason { get; init; }
}