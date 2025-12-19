namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public class IdentityRequestDto
{
    public required string Username { get; init; }
    public string? SamlSessionId { get; init; }
    public string? OidcSessionId { get; init; }
    public Dictionary<string, string>? AdditionalClaims { get; init; }
    public required string LoginCompletedCallbackUrl { get; init; }
    public required string AdConnectorCallbackBaseUrl { get; init; }
    public required IdentitySspSettingsDto Settings { get; init; }
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

