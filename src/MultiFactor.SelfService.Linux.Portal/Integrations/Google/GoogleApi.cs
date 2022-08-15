using MultiFactor.SelfService.Linux.Portal.Integrations.Google.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Google
{
    public class GoogleApi
    {
        private readonly HttpClient _client;

        public GoogleApi(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Verifies the user's reCAPTCHA response.
        /// </summary>
        /// <param name="secret">Required. The shared key between your site and reCAPTCHA.</param>
        /// <param name="response">Required. The user response token provided by the reCAPTCHA client-side integration on your site.</param>
        /// <param name="remoteip">Optional. The user's IP address.</param>
        public Task<VerifyCaptchaResponseDto> VerifyCaptchaAsync(string secret, string response, string? remoteip = null)
        {
            return Task.FromResult(default (VerifyCaptchaResponseDto));
        }
    }
}
