using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Text;
using static MultiFactor.SelfService.Linux.Portal.Core.Constants;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public class MultiFactorApi
    {
        private readonly HttpClientAdapter _clientAdapter;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly PortalSettings _settings;

        public MultiFactorApi(MultifactorHttpClientAdapterFactory clientFactory, HttpClientTokenProvider tokenProvider, PortalSettings settings)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            _clientAdapter = clientFactory.CreateClientAdapter();

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task PingAsync()
        {
            return ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse>("ping"));
        }

        public Task<BypassPageDto> CreateSamlBypassRequestAsync(string login, string samlSessionId)
        {
            var payload = new
            {
                Identity = login,
                SamlSessionId = samlSessionId
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<BypassPageDto>>("access/bypass/saml", payload, GetBasicAuthHeaders()));
        }
        
        /// <summary>
        /// Sends a request to create an enrollment request for the self-service portal.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation, containing an <see cref="ApiResponse{EnrollmentPageDto}"/> object.
        /// </returns>
        public Task<ApiResponse<EnrollmentPageDto>> CreateEnrollmentRequest()
        {
             return _clientAdapter.PostAsync<ApiResponse<EnrollmentPageDto>>(
                 $"/self-service/create-enrollment-request?dcCode={_settings.CompanySettings.Domain}",
                 data: null,
                 GetBearerAuthHeaders());
        }

        /// <summary>
        /// Returns user profile.
        /// </summary>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public async Task<UserProfileDto> GetUserProfileAsync()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<UserProfileApiDto>>("self-service", GetBearerAuthHeaders()));
            return new UserProfileDto(response.Id, response.Identity)
            {
                Name = response.Name,
                Email = response.Email,

                TotpAuthenticators = response.TotpAuthenticators,
                TelegramAuthenticators = response.TelegramAuthenticators,
                MobileAppAuthenticators = response.MobileAppAuthenticators,
                PhoneAuthenticators = response.PhoneAuthenticators,

                Policy = response.Policy,

                EnablePasswordManagement = _settings.PasswordManagement.Enabled,
                EnableExchangeActiveSyncDevicesManagement = _settings.ExchangeActiveSyncDevicesManagement.Enabled
            };
        }

        /// <summary>
        /// Returns new access token.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="displayName"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <param name="postbackUrl"></param>
        /// <param name="claims"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email, 
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims)
        {
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(claims);

            var payload = new
            {
                Identity = string.IsNullOrEmpty(_settings.ActiveDirectorySettings.NetBiosName) 
                    ? username 
                    : $"{_settings.ActiveDirectorySettings.NetBiosName}\\{username}",
                Callback = new
                {
                    Action = postbackUrl,
                    Target = "_self"
                },
                Name = displayName,
                Email = email,
                Phone = phone,
                Claims = claims,
                Language = Thread.CurrentThread.CurrentCulture?.TwoLetterISOLanguageName,
                GroupPolicyPreset = new
                {
                    SignUpGroups = _settings.GroupPolicyPreset.SignUpGroups
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<AccessPageDto>>("access/requests", payload, GetBasicAuthHeaders()));
        }

        public Task<ResetPasswordDto> StartResetPassword(string identity, string callbackUrl)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(callbackUrl);

            // add netbios domain name to login if specified

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.ResetPassword, "true" }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<ResetPasswordDto>>("self-service/start-reset-password", payload, GetBasicAuthHeaders()));

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
