using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Text;
using static MultiFactor.SelfService.Linux.Portal.Core.Constants;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi
{
    public class MultifactorIdpApi
    {
        private readonly HttpClientAdapter _clientAdapter;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly PortalSettings _settings;

        public MultifactorIdpApi(MultifactorIdpHttpClientAdapterFactory clientFactory, HttpClientTokenProvider tokenProvider, PortalSettings settings)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            _clientAdapter = clientFactory.CreateClientAdapter();

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Returns SSO master session.
        /// </summary>
        public async Task<SsoMasterSessionDto> GetSsoMasterSession()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<SsoMasterSessionDto>>("sso-master-session", GetBearerAuthHeaders()));
            return new SsoMasterSessionDto(response.MasterSessionId, response.SamlSessionIds);
        }

        /// <summary>
        /// Adds SAML session to SSO master session.
        /// </summary>
        /// <param name="samlSessionId"></param>
        public async Task<SsoMasterSessionDto> AddToSsoMasterSession(string samlSessionId)
        {
            var payload = new
            {
                ChildSessionId = samlSessionId
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/add-child-session",
                payload,
                GetBearerAuthHeaders()));

            return new SsoMasterSessionDto(response.MasterSessionId, response.SamlSessionIds);
        }

        /// <summary>
        /// Logout from SSO master session.
        /// </summary>
        public Task LogoutSsoMasterSession()
        {
            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse>(
                "sso-master-session/logout",
                data: null,
                GetBearerAuthHeaders()));
        }

        private static async Task ExecuteAsync(Func<Task<ApiResponse>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new UnsuccessfulResponseException(response.Message);
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<ApiResponse<T>>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new UnsuccessfulResponseException(response.Message);
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
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiSettings.ApiKey + ":" + _settings.MultiFactorApiSettings.ApiSecret));
            return new Dictionary<string, string>
            {
                { "Authorization", $"Basic {auth}" }
            };
        }
    }
}
