using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoadProfile;

public class LoadIdpProfileStory
{
    private readonly IMultifactorIdpApi _api;

    public LoadIdpProfileStory(IMultifactorIdpApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<UserProfileDto> ExecuteAsync() => _api.GetUserProfileAsync();
}