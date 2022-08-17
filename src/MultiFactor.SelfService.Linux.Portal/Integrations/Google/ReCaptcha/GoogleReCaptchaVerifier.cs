using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Google.ReCaptcha
{
    public class GoogleReCaptchaVerifier : ICaptchaVerifier
    {
        private readonly PortalSettings _settings;
        private readonly GoogleReCaptcha2Api _captcha2Api;
        private readonly ILogger<GoogleReCaptchaVerifier> _logger;

        public GoogleReCaptchaVerifier(PortalSettings settings, GoogleReCaptcha2Api captcha2Api, ILogger<GoogleReCaptchaVerifier> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _captcha2Api = captcha2Api ?? throw new ArgumentNullException(nameof(captcha2Api));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CaptchaVerificationResult> VerifyCaptchaAsync(HttpRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));     

            try
            {
                var token = request.Form[Constants.CAPTCHA_TOKEN].FirstOrDefault();
                if (token == null)
                {
                    return new CaptchaVerificationResult(false, "Response token is null");
                }

                var response = await _captcha2Api.SiteverifyAsync(_settings.GoogleReCaptchaSettings.Secret, token);
                if (response.Success)
                {
                    return new CaptchaVerificationResult(true);
                }

                var aggregatedError = response.ErrorCodes == null ? "Something went wrong" : AggregateErrors(response.ErrorCodes);
                return new CaptchaVerificationResult(false, aggregatedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Captcha verification failed");
                return new CaptchaVerificationResult(false, ex.Message);
            }
        }

        private static string AggregateErrors(IReadOnlyList<string> errorCodes)
        {
            var mapped = errorCodes.Select(GoogleSiteverifyErrorCode.GetDescription);
            return string.Join(Environment.NewLine, mapped);
        }
    }
}
