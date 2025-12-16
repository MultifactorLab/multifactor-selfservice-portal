using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi
{
    public interface IMultifactorIdpApi
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, Dictionary<string, string> headers);
        Task<LoginCompletedResponseDto> LoginCompletedAsync(LoginCompletedRequestDto request, Dictionary<string, string> headers);
        
        Task<SsoMasterSessionDto> CreateSsoMasterSession(string userIdentity);
        Task<SsoMasterSessionDto> GetSsoMasterSession();
        
        Task<SsoMasterSessionDto> AddSamlToSsoMasterSession(string samlSessionId);
        Task<SsoMasterSessionDto> AddOidcToSsoMasterSession(string oidcSessionId);
        
        Task LogoutSsoMasterSession();

        Task<BypassSamlResponseDto> BypassSamlAsync(BypassSamlRequestDto request, Dictionary<string, string> headers);
        
        Task<UserProfileDto> GetUserProfileAsync();
    }
}
