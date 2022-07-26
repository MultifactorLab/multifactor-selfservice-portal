namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileDto
    {
        public string Id { get; init; }
        public string Identity { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }

        public IReadOnlyList<UserProfileAuthenticatorDto> TotpAuthenticators { get; init; }
        public IReadOnlyList<UserProfileAuthenticatorDto> TelegramAuthenticators { get; init; }
        public IReadOnlyList<UserProfileAuthenticatorDto> MobileAppAuthenticators { get; init; }
        public IReadOnlyList<UserProfileAuthenticatorDto> PhoneAuthenticators { get; init; }

        public UserProfilePolicyDto Policy { get; init; }

        public bool EnablePasswordManagement { get; init; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; init; }

        public int Count =>
            TotpAuthenticators.Count +
            TelegramAuthenticators.Count +
            MobileAppAuthenticators.Count +
            PhoneAuthenticators.Count;
    }
}