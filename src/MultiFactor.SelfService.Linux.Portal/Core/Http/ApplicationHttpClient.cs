using MultiFactor.SelfService.Linux.Portal.Exceptions;
using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public class ApplicationHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ApplicationHttpClient> _logger;

        public ApplicationHttpClient(HttpClient client, ILogger<ApplicationHttpClient> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetAsync(string uri, IReadOnlyDictionary<string, string>? headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<T?> GetAsync<T>(string uri, IReadOnlyDictionary<string, string>? headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await HttpClientUtils.FromJsonContent<T>(resp.Content);
        }

        public async Task<T?> PostAsync<T>(string uri, object data, IReadOnlyDictionary<string, string>? headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);
            message.Content = HttpClientUtils.ToJsonContent(data);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await HttpClientUtils.FromJsonContent<T>(resp.Content);
        }

        public Task DeleteAsync(string uri, IReadOnlyDictionary<string, string>? headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            return ExecuteHttpMethod(() => _client.SendAsync(message));
        }

        private async Task<HttpResponseMessage> ExecuteHttpMethod(Func<Task<HttpResponseMessage>> method)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var response = await method();

            try
            {
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException ex)
            {
                var content = await HttpClientUtils.TryGetContent(response);
                _logger.LogError(ex, "An error occurred while accessing the source. Content: {content:l}. Exception message: {message:l}", content, ex.Message);

                if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedException();
                throw;
            }
        }
    }
}
