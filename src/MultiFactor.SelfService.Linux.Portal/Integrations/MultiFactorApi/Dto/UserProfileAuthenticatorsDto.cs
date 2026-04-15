namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserAuthenticatorsDto
    {
        public UserProfileAuthenticatorDto[] TotpAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] TelegramAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] MobileAppAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] PhoneAuthenticators { get; set; }

        public UserProfileAuthenticatorDto[] GetAuthenticators()
        {
            return Enumerable.Empty<UserProfileAuthenticatorDto>()
                .Concat(TotpAuthenticators ?? [])
                .Concat(TelegramAuthenticators ?? [])
                .Concat(MobileAppAuthenticators ?? [])
                .Concat(PhoneAuthenticators ?? [])
                .ToArray();
        }
    }
}