namespace MultiFactor.SelfService.Linux.Portal.Services.Api.Dto
{
    /// <summary>
    /// User profile
    /// </summary>
    public record UserProfileDto(
        string Id,
        string Identity,
        string Name,
        string Email,

        IReadOnlyList<UserProfileAuthenticatorDto> TotpAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> TelegramAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> MobileAppAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> PhoneAuthenticators,

        UserProfilePolicyDto Policy,

        bool EnablePasswordManagement,
        bool EnableExchangeActiveSyncDevicesManagement)
    {
        public int Count => 
            TotpAuthenticators.Count +
            TelegramAuthenticators.Count +
            MobileAppAuthenticators.Count +
            PhoneAuthenticators.Count;
    }
}