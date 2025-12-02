using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public interface IMultiFactorApi
    {
        Task PingAsync();
        Task<ShowcaseSettings> GetShowcaseSettingsAsync();
        Task<byte[]> GetShowcaseLogoAsync(string fileName);
        Task<BypassPageDto> CreateSamlBypassRequestAsync(UserProfileDto user, string samlSessionId);
        Task<BypassPageDto> CreateOidcBypassRequestAsync(UserProfileDto user, string oidcSessionId);
        Task<ResetPasswordDto> StartResetPassword(string twoFaIdentity, string ldapIdentity, string callbackUrl);
        Task<UnlockUserDto> StartUnlockingUser(string identity, string callbackUrl);

        Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email,
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims);
        Task<UserProfileDto> GetUserProfileAsync();
        Task<ScopeSupportInfoDto> GetScopeSupportInfo();
        Task<ApiResponse<EnrollmentPageDto>> CreateEnrollmentRequest();
    }
}
