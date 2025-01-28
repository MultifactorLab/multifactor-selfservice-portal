using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public class MultifactorHttpClientAdapterFactory
    {
        private readonly HttpClient _client;
        private readonly JsonDataSerializer _jsonDataSerializer;
        private readonly ILogger<HttpClientAdapter> _logger;

        public MultifactorHttpClientAdapterFactory(IHttpClientFactory httpClientFactory, JsonDataSerializer jsonDataSerializer, ILogger<HttpClientAdapter> logger)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.MultifactorApi);
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer, _logger);
        }
    }
}
