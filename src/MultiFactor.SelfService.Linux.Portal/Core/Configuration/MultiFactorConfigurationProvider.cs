using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class MultiFactorConfigurationProvider : ConfigurationProvider
    {
        private readonly HttpClientAdapter _client;
        private readonly ILogger _logger;

        public MultiFactorConfigurationProvider(MultifactorHttpClientAdapterFactory clientFactory, ILogger logger)
        {
            _client = clientFactory?.CreateClientAdapter() ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
        }

        public override void Load()
        {
            try
            {
                _logger.LogInformation("Trying to access Multifactor server...");
                var value = _client.GetAsync(".well-known/jwks.json").Result ?? "{\"keys\":[]}";
                _logger.LogInformation("Multifactor server acessibility is OK");
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
