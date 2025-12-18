namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

/// <summary>
/// Response DTO from Identity endpoint.
/// </summary>
public sealed class IdentityResponseDto
{
    public bool Success { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public string? Username { get; init; }
    public string? ErrorMessage { get; init; }

    public static IdentityResponseDto Failed(string message) => new()
    {
        Success = false,
        Action = "Error",
        ErrorMessage = message
    };

    /// <summary>
    /// User must proceed to MFA page.
    /// </summary>
    public bool IsMfaRequired => Action == "MfaRequired";

    /// <summary>
    /// User should bypass MFA and enter password directly (show Authn view).
    /// Used when user is in bypass group and has active SSO session.
    /// </summary>
    public bool IsShowAuthn => Action == "ShowAuthn";

    /// <summary>
    /// Access denied.
    /// </summary>
    public bool IsAccessDenied => Action == "AccessDenied";
}

