using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class MultiFactorConfigurationProvider : ConfigurationProvider
    {
        private readonly HttpClientAdapter _client;

        public MultiFactorConfigurationProvider(MultifactorHttpClientAdapterFactory clientFactory)
        {
            _client = clientFactory?.CreateClientAdapter() ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public override void Load()
        {
            var value = _client.GetAsync(".well-known/jwks.json").Result ?? "{\"keys\":[]}";
            Data = new Dictionary<string, string>
            {
                { Constants.TOKEN_VALIDATION, value }
            };
        }
    }
}
