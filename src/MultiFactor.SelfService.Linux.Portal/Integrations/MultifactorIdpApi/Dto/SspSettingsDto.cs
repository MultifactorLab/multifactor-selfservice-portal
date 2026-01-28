namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public class SspSettingsDto
{
    public bool PreAuthenticationMethod { get; init; }
    public bool RequiresUserPrincipalName { get; init; }
    public bool PasswordManagementEnabled { get; init; }
    public bool NeedPrebindInfo { get; init; }
    public string PrivacyMode { get; init; } = "None";
    public string? NetBiosName { get; init; }
    public string SignUpGroups { get; init; }
}