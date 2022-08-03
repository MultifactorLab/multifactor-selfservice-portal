using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.CreateYandexAuthKeyStory
{
    public class CreateYandexAuthKeyStory
    {
        private readonly MultiFactorApi _api;

        public CreateYandexAuthKeyStory(MultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<YandexKeyViewModel> ExecuteAsync()
        {
            var response = await _api.CreateTotpKey();
            return new YandexKeyViewModel
            {
                Link = response.Link,
                Key = response.Key
            };
        }
    }
}
