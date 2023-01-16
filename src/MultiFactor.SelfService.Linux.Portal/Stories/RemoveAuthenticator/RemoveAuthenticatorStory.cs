using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator
{
    public class RemoveAuthenticatorStory
    {
        private readonly MultiFactorApi _api;
        private readonly PortalSettings _settings;

        public RemoveAuthenticatorStory(MultiFactorApi api, PortalSettings settings)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<IActionResult> ExecuteAsync(RemoveAuthenticatorDto dto)
        {
            var response = await _api.GetUserProfileAsync();
            var userProfile = response.ToUserProfileDto(_settings);
            // do not remove last
            if (userProfile.Count > 1)
            {
                await _api.RemoveAuthenticatorAsync(dto.Authenticator, dto.Id);
            }

            return new LocalRedirectResult("/");
        }
    }
}
