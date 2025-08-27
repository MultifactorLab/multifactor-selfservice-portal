using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public interface IMultiFactorApi
    {
        Task PingAsync();
        Task<BypassPageDto> CreateSamlBypassRequestAsync(string login, string samlSessionId);
        Task<BypassPageDto> CreateOidcBypassRequestAsync(string login, string oidcSessionId);
        Task<ResetPasswordDto> StartResetPassword(string twoFaIdentity, string ldapIdentity, string callbackUrl);
        Task<UnlockUserDto> StartUnlockingUser(string identity, string callbackUrl);

        Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email,
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims);
        Task<UserProfileDto> GetUserProfileAsync();
        Task<ScopeSupportInfoDto> GetScopeSupportInfo();
    }
}
