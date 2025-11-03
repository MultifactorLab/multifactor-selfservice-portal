using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Text;
using MultiFactor.SelfService.Linux.Portal.Dto;
using static MultiFactor.SelfService.Linux.Portal.Core.Constants;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    internal class MultiFactorApi : IMultiFactorApi
    {
        private readonly HttpClientAdapter _clientAdapter;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly PortalSettings _settings;

        public MultiFactorApi(MultifactorHttpClientAdapterFactory clientFactory, HttpClientTokenProvider tokenProvider, PortalSettings settings)
        {
            _clientAdapter = clientFactory.CreateClientAdapter();
            _tokenProvider = tokenProvider;
            _settings = settings;
        }

        public Task PingAsync()
        {
            return ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse>("ping"));
        }

        public async Task<ShowcaseSettings> GetShowcaseSettingsAsync()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<ShowcaseSettingsDto>>("self-service/settings/showcase", GetBasicAuthHeaders()));
            return new ShowcaseSettings()
            {
                Enabled = response.Enabled,
                Links = response.ShowcaseLinks
                    .Select(x => new ShowcaseLink()
                    {
                        Url = x.Url,
                        Title = x.Title,
                        OpenInNewTab = x.OpenInNewTab,
                        Image = x.Image,
                    })
                    .ToArray(),
            };
        }

        public async Task<byte[]> GetShowcaseLogoAsync(string fileName)
        {
            var response = await _clientAdapter.GetByteArrayAsync(
                $"self-service/settings/showcase/logo/{fileName}",
                GetBasicAuthHeaders());
            return response;
        }

        public Task<BypassPageDto> CreateSamlBypassRequestAsync(UserProfileDto user, string samlSessionId)
        {
            var payload = new
            {
                Identity = user.Identity,
                SamlSessionId = samlSessionId,
                Claims = new Dictionary<string, string>()
                {
                    { "name", user.Name },
                    { "email", user.Email }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<BypassPageDto>>("access/bypass/saml", payload, GetBasicAuthHeaders()));
        }

        public Task<BypassPageDto> CreateOidcBypassRequestAsync(UserProfileDto user, string oidcSessionId)
        {
            var payload = new
            {
                Identity = user.Identity,
                OidcSessionId = oidcSessionId,
                Claims = new Dictionary<string, string>()
                {
                    { "name", user.Name },
                    { "email", user.Email }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<BypassPageDto>>("access/bypass/oidc", payload, GetBasicAuthHeaders()));
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
                "/self-service/create-enrollment-request",
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

        public Task<ResetPasswordDto> StartResetPassword(string twoFaIdentity, string ldapIdentity, string callbackUrl)
        {
            ArgumentNullException.ThrowIfNull(twoFaIdentity);
            ArgumentNullException.ThrowIfNull(callbackUrl);

            // add netbios domain name to login if specified

            var payload = new
            {
                Identity = twoFaIdentity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.ResetPassword, "true" },
                    { MultiFactorClaims.RawUserName, ldapIdentity }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<ResetPasswordDto>>("self-service/start-reset-password", payload, GetBasicAuthHeaders()));
        }

        public Task<UnlockUserDto> StartUnlockingUser(string identity, string callbackUrl)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ArgumentNullException.ThrowIfNull(callbackUrl);

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.UnlockUser, "true"}
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<UnlockUserDto>>("self-service/start-unlock-user", payload, GetBasicAuthHeaders()));
        }

        public Task<ScopeSupportInfoDto> GetScopeSupportInfo()
        {
            return ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<ScopeSupportInfoDto>>("/self-service/support-info", GetBasicAuthHeaders()));
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
