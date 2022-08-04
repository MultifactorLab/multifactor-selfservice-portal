using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator
{
    public class RemoveAuthenticatorStory
    {
        private readonly MultiFactorApi _api;

        public RemoveAuthenticatorStory(MultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<IActionResult> ExecuteAsync(RemoveAuthenticatorDto dto)
        {
            var userProfile = await _api.GetUserProfileAsync();
            // do not remove last
            if (userProfile.Count > 1)
            {
                await _api.RemoveAuthenticatorAsync(dto.Authenticator, dto.Id);
            }

            return new LocalRedirectResult("/");
        }
    }
}
