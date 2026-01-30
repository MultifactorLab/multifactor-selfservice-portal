namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public record UserProfileAuthenticatorsApiDto(
        UserProfileAuthenticatorDto[] TotpAuthenticators,
        UserProfileAuthenticatorDto[] TelegramAuthenticators,
        UserProfileAuthenticatorDto[] MobileAppAuthenticators,
        UserProfileAuthenticatorDto[] PhoneAuthenticators);

    /// <summary>
    /// MFA authenticator
    /// </summary>
    public record UserProfileAuthenticatorDto(string Id, string Label);
}