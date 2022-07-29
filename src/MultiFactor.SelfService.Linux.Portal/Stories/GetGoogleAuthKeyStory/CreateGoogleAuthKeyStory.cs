using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.GetGoogleAuthKeyStory
{
    public class CreateGoogleAuthKeyStory
    {
        private readonly MultiFactorApi _api;

        public CreateGoogleAuthKeyStory(MultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<GoogleAuthenticatorViewModel> ExecuteAsync()
        {
            var response = await _api.CreateTotpKey();
            return new GoogleAuthenticatorViewModel
            {
                Link = response.Link,
                Key = response.Key
            };
        }
    }
}
