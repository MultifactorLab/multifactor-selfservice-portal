﻿using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;
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

        /// <summary>
        /// Removes specified authenticator from user profile.
        /// </summary>
        /// <param name="authenticator">Name</param>
        /// <param name="id">Id</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public Task RemoveAuthenticatorAsync(string authenticator, string id)
        {
            if (authenticator is null)  throw new ArgumentNullException(nameof(authenticator));
            if (id is null) throw new ArgumentNullException(nameof(id));

            return ExecuteAsync(() => _client.DeleteAsync<ApiResponse>($"/self-service/{authenticator}/{id}", GetBearerAuthHeaders()));
        }

        /// <summary>
        /// Returns user profile.
        /// </summary>
        /// <exception cref="UnsuccessfulResponseException"></exception>
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

        /// <summary>
        /// Returns new Time-based One Time Password.
        /// </summary>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public Task<TotpKeyDto> CreateTotpKey() => ExecuteAsync(() => _client.GetAsync<ApiResponse<TotpKeyDto>>("self-service/totp/new", GetBearerAuthHeaders()));

        /// <summary>
        /// Adds new Time-based One Time Password authenticator to the user profile.
        /// </summary>
        /// <param name="key">Totp identifier</param>
        /// <param name="otp">Password</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public Task AddTotpAuthenticatorAsync(string key, string otp)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (otp is null) throw new ArgumentNullException(nameof(otp));

            var payload = new { key, otp };

            return ExecuteAsync(() => _client.PostAsync<ApiResponse>("self-service/totp", payload, GetBearerAuthHeaders()));
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
                throw new UnsuccessfulResponseException(response.Message);
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
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiKey + ":" + _settings.MultiFactorApiSecret));
            return new Dictionary<string, string>
            {
                { "Authorization", $"Basic {auth}" }
            };
        }
    }
}