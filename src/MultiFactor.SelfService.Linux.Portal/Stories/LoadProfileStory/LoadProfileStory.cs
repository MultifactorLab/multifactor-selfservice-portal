using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory
{
    public class LoadProfileStory
    {
        private readonly PortalSettings _settings;

        public LoadProfileStory(PortalSettings settings)
        {
            _settings = settings;
        }

        public async Task<UserProfileDto> ExecuteAsync(string token)
        {
            await Task.Delay(100);

            return new UserProfileDto(
                    Guid.NewGuid().ToString(),
                    "AlekseyIdentity",
                    "Aleksey",
                    "a.pashkov@multifactor.ru",
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new UserProfilePolicyDto(true, true, true, true),
                    _settings.EnablePasswordManagement,
                    _settings.EnableExchangeActiveSyncDevicesManagement);
        }
    }

    public class MultiFactorApi
    {
        private readonly ApplicationHttpClient _client;
        private readonly PortalSettings _settings;

        public MultiFactorApi(ApplicationHttpClient client, PortalSettings settings)
        {
            _client = client;
            _settings = settings;
        }

        public Task<UserProfileDto> LoadUserProfileAsync()
        {

        }
    }

    public class ApplicationHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ApplicationHttpClient> _logger;

        public ApplicationHttpClient(HttpClient client, ILogger<ApplicationHttpClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string uri)
        {
            var resp = await ExecuteHttpMethod(() => _client.GetAsync(uri));
            if (resp?.Content == null) return default;
            return JsonSerializer.Deserialize<T>(await resp.Content.ReadAsStringAsync(), SerializerOptions.JsonSerializerOptions);
        }

        public async Task<T?> PostAsync<T>(string uri, object data)
        {
            var content = ToJsonContent(data);
            var resp = await ExecuteHttpMethod(() => _client.PostAsync(uri, content));
            if (resp.Content == null)
            {
                return default;
            }

            return await FromJsonContent<T>(resp.Content);
        }

        public async Task<T?> PutAsync<T>(string uri, object data)
        {
            var content = ToJsonContent(data);
            var resp = await ExecuteHttpMethod(() => _client.PutAsync(uri, content));
            if (resp.Content == null)
            {
                return default;
            }

            return await FromJsonContent<T>(resp.Content);
        }

        public Task DeleteAsync(string uri) => ExecuteHttpMethod(() => _client.DeleteAsync(uri));
        

        public Task DeleteAsync(string uri, object data)
        {
            var request = new HttpRequestMessage
            {
                Content = ToJsonContent(data),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(uri)
            };
            return ExecuteHttpMethod(() => _client.SendAsync(request));
        }

        private static StringContent ToJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static async Task<T?> FromJsonContent<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            if (jsonResponse == null) return default;
            return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
        }

        private async Task<HttpResponseMessage> ExecuteHttpMethod(Func<Task<HttpResponseMessage>> method)
        {
            var response = await method();

            try
            {
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException ex)
            {
                var content = await TryGetContent(response);
                _logger.LogError(ex, "An error occurred while accessing the source. Content: {content:l}. Exception message: {message:l}", content, ex.Message);

                if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedException();
                throw;
            }
        }

        private static async Task<string?> TryGetContent(HttpResponseMessage response)
        {
            if (response.Content == null)
            {
                return null;
            }

            if (response.Content.Headers?.ContentType?.MediaType != "application/json")
            {
                return await response.Content.ReadAsStringAsync();
            }

            var parsed = await FromJsonContent<ErrorObject>(response.Content);
            return parsed?.Message;
        }

        private record ErrorObject(string Message);
        
    }
}
