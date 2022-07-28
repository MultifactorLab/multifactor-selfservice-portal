using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public class MultiFactorApi
    {
        private readonly ApplicationHttpClient _client;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly PortalSettings _settings;

        public MultiFactorApi(ApplicationHttpClient client, HttpClientTokenProvider tokenProvider, PortalSettings settings)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task RemoveAuthenticatorAsync(string authenticator, string id)
        {
            if (authenticator is null)  throw new ArgumentNullException(nameof(authenticator));
            if (id is null) throw new ArgumentNullException(nameof(id));

            return ExecuteAsync(() => _client.DeleteAsync<ApiResponse>($"/self-service/{authenticator}/{id}", GetBearerAuthHeaders()));
        }

        public async Task<UserProfileDto> GetUserProfileAsync()
        {
            var response = await ExecuteAsync(() => _client.GetAsync<ApiResponse<UserProfileApiDto>>("self-service", GetBearerAuthHeaders()));
            return new UserProfileDto
            {
                Id = response.Id,
                Identity = response.Identity,
                Name = response.Name,
                Email = response.Email,

                TotpAuthenticators = response.TotpAuthenticators,
                TelegramAuthenticators = response.TelegramAuthenticators,
                MobileAppAuthenticators = response.MobileAppAuthenticators,
                PhoneAuthenticators = response.PhoneAuthenticators,

                Policy = response.Policy,

                EnablePasswordManagement = _settings.EnablePasswordManagement,
                EnableExchangeActiveSyncDevicesManagement = _settings.EnableExchangeActiveSyncDevicesManagement
            };
        }

        public Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email, 
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (claims is null) throw new ArgumentNullException(nameof(claims));        

            var payload = new
            {
                Identity = string.IsNullOrEmpty(_settings.NetBiosName) ? username : $"{_settings.NetBiosName}\\{username}",
                Callback = new
                {
                    Action = postbackUrl,
                    Target = "_self"
                },
                Name = displayName,
                Email = email,
                Phone = phone,
                Claims = claims,
                Language = Thread.CurrentThread.CurrentCulture?.TwoLetterISOLanguageName
            };

            return ExecuteAsync(() => _client.PostAsync<ApiResponse<AccessPageDto>>("access/requests", payload, GetBasicAuthHeaders()));
        }

        private static async Task ExecuteAsync(Func<Task<ApiResponse?>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new Exception($"Unsuccessful response: {response}");
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<ApiResponse<T>?>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new Exception($"Unsuccessful response: {response}");
            }
            if (response.Model == null)
            {
                throw new Exception("Response payload is null");
            }

            return response.Model;
        }

        private IReadOnlyDictionary<string, string> GetBearerAuthHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_tokenProvider.GetToken()}" }
            };
        }

        private IReadOnlyDictionary<string, string> GetBasicAuthHeaders()
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiKey + ":" + _settings.MultiFactorApiSecret));
            return new Dictionary<string, string>
            {
                { "Authorization", $"Basic {auth}" }
            };
        }
    }
}
