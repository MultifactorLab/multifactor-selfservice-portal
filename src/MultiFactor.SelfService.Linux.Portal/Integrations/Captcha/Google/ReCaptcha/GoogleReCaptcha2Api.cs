﻿using Microsoft.AspNetCore.WebUtilities;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha
{
    public class GoogleReCaptcha2Api
    {
        private readonly HttpClientAdapter _client;

        public GoogleReCaptcha2Api(GoogleHttpClientAdapterFactory factory)
        {
            _client = factory?.CreateClientAdapter() ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Verifies the user's reCAPTCHA response.
        /// </summary>
        /// <param name="secret">Required. The shared key between your site and reCAPTCHA.</param>
        /// <param name="responseToken">Required. The user response token provided by the reCAPTCHA client-side integration on your site.</param>
        /// <param name="remoteIp">Optional. The user's IP address.</param>
        public async Task<GoogleVerifyCaptchaResponseDto> SiteverifyAsync(string secret, string responseToken, string remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentNullException(nameof(secret));
            if (string.IsNullOrWhiteSpace(responseToken)) throw new ArgumentNullException(nameof(responseToken));

            var param = new Dictionary<string, string>
            { 
                { "secret", secret },
                { "response", responseToken }
            };
            if (!string.IsNullOrEmpty(remoteIp))
            {
                param.Add("remoteip", remoteIp);
            }

            var newAction = QueryHelpers.AddQueryString("siteverify", param);

            var resp = await _client.PostAsync<GoogleVerifyCaptchaResponseDto>(newAction) ?? throw new Exception("Response is null");
            return resp;
        }
    }
}
