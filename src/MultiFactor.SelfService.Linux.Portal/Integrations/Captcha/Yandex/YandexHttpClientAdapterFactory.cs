using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex
{
    public class YandexHttpClientAdapterFactory
    {
        private readonly HttpClient _client;
        private readonly JsonDataSerializer _jsonDataSerializer;
        private readonly ILogger<HttpClientAdapter> _logger;
        
        public YandexHttpClientAdapterFactory(IHttpClientFactory httpClientFactory, JsonDataSerializer jsonDataSerializer, ILogger<HttpClientAdapter> logger)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.YandexCaptcha);
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer, _logger);
        }
    }    
}
