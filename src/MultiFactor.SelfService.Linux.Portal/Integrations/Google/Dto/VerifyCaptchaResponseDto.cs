using System.Text.Json.Serialization;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Google.Dto
{
    public class VerifyCaptchaResponseDto
    {
        /// <summary>
        /// true|false.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; }

        /// <summary>
        /// Timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ).
        /// </summary>
        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; }

        /// <summary>
        /// The hostname of the site where the reCAPTCHA was solved.
        /// </summary>
        [JsonPropertyName("hostname")]
        public string HostName { get; }

        /// <summary>
        /// Optional.
        /// </summary>
        [JsonPropertyName("error-codes")]
        public IReadOnlyList<string>? ErrorCodes { get; }
    }

    public class VerifyCaptchaRequestDto
    {
        /// <summary>
        /// Required. The shared key between your site and reCAPTCHA.
        /// </summary>
        [JsonPropertyName("secret")]
        public string Secret { get; }

        /// <summary>
        /// Required. The user response token provided by the reCAPTCHA client-side integration on your site.
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; }

        /// <summary>
        /// Optional. The user's IP address.
        /// </summary>
        [JsonPropertyName("remoteip")]
        public string? RemoteIp { get; }

        public VerifyCaptchaRequestDto(string secret, string response)
        {
            Secret = secret ?? throw new ArgumentNullException(nameof(secret));
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }
    }
}
