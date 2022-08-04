using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory
{
    public class LoadProfileStory
    {
        private readonly MultiFactorApi _api;

        public LoadProfileStory(MultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public Task<UserProfileDto> ExecuteAsync() => _api.GetUserProfileAsync();
    }
}
