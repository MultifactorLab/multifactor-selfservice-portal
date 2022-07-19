namespace MultiFactor.SelfService.Linux.Portal.Services.Api.Dto
{
    /// <summary>
    /// User group policy 
    /// </summary>
    public record UserProfilePolicyDto (bool Totp, bool Telegram, bool MobileApp, bool Phone);
}