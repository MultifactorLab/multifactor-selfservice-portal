namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

/// <summary>
/// Identity request DTO with optionally pre-verified membership data.
/// Used for pre-authentication flow (MFA first, then password).
/// </summary>
public class IdentityRequestDto
{
    public required string Username { get; init; }
    public VerifiedMembershipDto? VerifiedMembership { get; init; }
    public string? SamlSessionId { get; init; }
    public string? OidcSessionId { get; init; }
    public Dictionary<string, string>? AdditionalClaims { get; init; }
    public required string LoginCompletedCallbackUrl { get; init; }
    public required IdentitySspSettingsDto Settings { get; init; }
}

/// <summary>
/// Pre-verified membership from SSP's local AD verification (without password).
/// Used when NeedPrebindInfo is true.
/// </summary>
public class VerifiedMembershipDto
{
    public required bool IsBypass { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? CustomIdentity { get; init; }
}

public class IdentitySspSettingsDto
{
    public bool PreAuthenticationMethod { get; init; }
    public bool RequiresUserPrincipalName { get; init; }
    public bool NeedPrebindInfo { get; init; }
    public bool UseUpnAsIdentity { get; init; }
    public string PrivacyMode { get; init; } = "None";
    public string? NetBiosName { get; init; }
    public string? SignUpGroups { get; init; }
}

