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
        
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBasicAuthHeaders();
                headers.TryAdd(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                
                var response = await _clientAdapter.PostAsync<IdpApiResponse<LoginResponseDto>>(
                    "api/v1/login",
                    request,
                    headers);

                if (response == null)
                {
                    return LoginResponseDto.Failed("Empty response from IdP");
                }

                if (!response.Success)
                {
                    return LoginResponseDto.Failed(response.Message ?? "Login failed");
                }

                return response.Data ?? LoginResponseDto.Failed("Empty data in response");
            }
            catch (Exception ex)
            {
                return LoginResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Completes login after MFA verification.
        /// </summary>
        public async Task<LoginCompletedResponseDto> LoginCompletedAsync(LoginCompletedRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBasicAuthHeaders();
                headers.TryAdd(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());

                var formData = new[]
                {
                    new KeyValuePair<string, string>("accessToken", request.AccessToken)
                };

                var response = await _clientAdapter.PostFormAsync<IdpApiResponse<LoginCompletedResponseDto>>(
                    "api/v1/login-completed",
                    formData,
                    headers);

                if (response == null)
                {
                    return LoginCompletedResponseDto.Failed("Empty response from IdP");
                }

                if (!response.Success)
                {
                    return LoginCompletedResponseDto.Failed(response.Message ?? "Login completion failed");
                }

                return response.Data ?? LoginCompletedResponseDto.Failed("Empty data in response");
            }
            catch (Exception ex)
            {
                return LoginCompletedResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Creates new SSO master session.
        /// </summary>
        public async Task<SsoMasterSessionDto> CreateSsoMasterSession(string userIdentity)
        {
            var payload = new
            {
                UserIdentity = userIdentity
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/create",
                payload,
                GetBasicAuthHeaders()));

            return new SsoMasterSessionDto(response.MasterSessionId);
        }

        /// <summary>
        /// Returns existing SSO master session.
        /// </summary>
        public async Task<SsoMasterSessionDto> GetSsoMasterSession()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<SsoMasterSessionDto>>("sso-master-session", GetBearerAuthHeaders()));
            return new SsoMasterSessionDto(response.MasterSessionId);
        }

        /// <summary>
        /// Adds SAML session to SSO master session.
        /// </summary>
        /// <param name="samlSessionId"></param>
        public async Task<SsoMasterSessionDto> AddSamlToSsoMasterSession(string samlSessionId)
        {
            var payload = new
            {
                ChildSessionId = samlSessionId,
                SessionType = SsoMasterSessionTypes.SamlSessionType
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/add-child-session",
                payload,
                GetBearerAuthHeaders()));

            return new SsoMasterSessionDto(response.MasterSessionId);
        }

        /// <summary>
        /// Adds OIDC session to SSO master session.
        /// </summary>
        /// <param name="oidcSessionId"></param>
        public async Task<SsoMasterSessionDto> AddOidcToSsoMasterSession(string oidcSessionId)
        {
            var payload = new
            {
                ChildSessionId = oidcSessionId,
                SessionType = SsoMasterSessionTypes.OidcSessionType
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/add-child-session",
                payload,
                GetBearerAuthHeaders()));

            return new SsoMasterSessionDto(response.MasterSessionId);
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

        /// <summary>
        /// Creates SAML bypass via IdP.
        /// </summary>
        public async Task<BypassSamlResponseDto> BypassSamlAsync(BypassSamlRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBearerAuthHeaders();
                headers.TryAdd(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                
                var response = await _clientAdapter.PostAsync<IdpApiResponse<BypassSamlResponseDto>>("api/v1/saml/bypass", request, headers);
                
                if (response == null)
                {
                    return BypassSamlResponseDto.Failed("Empty response from IdP");
                }

                if (!response.Success)
                {
                    return BypassSamlResponseDto.Failed(response.Message ?? "Bypass failed");
                }

                return new BypassSamlResponseDto
                {
                    Success = true,
                    SamlResponseHtml = response.Data.SamlResponseHtml
                };
            }
            catch (Exception ex)
            {
                return BypassSamlResponseDto.Failed(ex.Message);
            }
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
