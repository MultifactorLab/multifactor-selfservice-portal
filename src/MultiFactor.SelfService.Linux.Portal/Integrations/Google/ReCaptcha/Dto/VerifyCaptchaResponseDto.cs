using System.Text.Json.Serialization;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Google.ReCaptcha.Dto
{
    public class VerifyCaptchaResponseDto
    {
        /// <summary>
        /// true|false.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ).
        /// </summary>
        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; set; }

        /// <summary>
        /// The hostname of the site where the reCAPTCHA was solved.
        /// </summary>
        [JsonPropertyName("hostname")]
        public string HostName { get; set; }

        /// <summary>
        /// Optional.
        /// </summary>
        [JsonPropertyName("error-codes")]
        public IReadOnlyList<string>? ErrorCodes { get; set; }
    }
}
