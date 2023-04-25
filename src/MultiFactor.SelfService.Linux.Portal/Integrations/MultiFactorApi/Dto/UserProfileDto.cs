namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileDto
    {
        public string Id { get; }
        public string Identity { get; }
        public string? Name { get; init; }
        public string? Email { get; init; }

        public IReadOnlyList<UserProfileAuthenticatorDto> TotpAuthenticators { get; init; } = new List<UserProfileAuthenticatorDto>();
        public IReadOnlyList<UserProfileAuthenticatorDto> TelegramAuthenticators { get; init; } = new List<UserProfileAuthenticatorDto>();
        public IReadOnlyList<UserProfileAuthenticatorDto> MobileAppAuthenticators { get; init; } = new List<UserProfileAuthenticatorDto>();
        public IReadOnlyList<UserProfileAuthenticatorDto> PhoneAuthenticators { get; init; } = new List<UserProfileAuthenticatorDto>();

        public UserProfilePolicyDto Policy { get; init; } = new UserProfilePolicyDto(false, false, false, false);

        public bool EnablePasswordManagement { get; init; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; init; }

        public int Count =>
            TotpAuthenticators.Count +
            TelegramAuthenticators.Count +
            MobileAppAuthenticators.Count +
            PhoneAuthenticators.Count;

        public UserProfileDto(string id, string identity)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }
}