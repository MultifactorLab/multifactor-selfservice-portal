using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory
{
    public class LoadUserProfileStory
    {
        private readonly MultiFactorApi _api;
        private readonly PortalSettings _settings;

        public LoadUserProfileStory(MultiFactorApi api, PortalSettings settings)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<UserProfileDto> ExecuteAsync()
        {
            var response = await _api.GetUserProfileAsync();
            return response.ToUserProfileDto(_settings);
        }
    }
}
