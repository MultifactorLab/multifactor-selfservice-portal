namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    /// <summary>
    /// User group policy 
    /// </summary>
    public record UserProfilePolicyDto(bool Totp, bool Telegram, bool MobileApp, bool Phone);
}