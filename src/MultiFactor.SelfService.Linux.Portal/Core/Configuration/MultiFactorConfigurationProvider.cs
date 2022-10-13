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
            try
            {
                var value = _client.GetAsync(".well-known/jwks.json").Result ?? "{\"keys\":[]}";
                Data = new Dictionary<string, string>
                {
                    { Constants.TOKEN_VALIDATION, value }
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to retrieve configuration from server: Multifactor API is unreachable", ex);
            }
        }
    }
}
