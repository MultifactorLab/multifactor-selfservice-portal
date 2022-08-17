using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Google
{
    public class GoogleHttpClientAdapterFactory
    {
        private readonly HttpClient _client;
        private readonly JsonDataSerializer _jsonDataSerializer;
        private readonly ILogger<HttpClientAdapter> _logger;

        public GoogleHttpClientAdapterFactory(HttpClient client, JsonDataSerializer jsonDataSerializer, ILogger<HttpClientAdapter> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer, _logger);
        }
    }
}
