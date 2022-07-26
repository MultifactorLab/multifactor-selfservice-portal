namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public record UserProfileApiDto(
        string Id,
        string Identity,
        string Name,
        string Email,

        IReadOnlyList<UserProfileAuthenticatorDto> TotpAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> TelegramAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> MobileAppAuthenticators,
        IReadOnlyList<UserProfileAuthenticatorDto> PhoneAuthenticators,

        UserProfilePolicyDto Policy);
}