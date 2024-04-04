using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha
{
    public class GoogleReCaptchaVerifier : BaseCaptchaVerifier
    {
        private readonly PortalSettings _settings;
        private readonly GoogleReCaptcha2Api _captcha2Api;
        private GoogleVerifyCaptchaResponseDto _responseDto = new();
        public GoogleReCaptchaVerifier(PortalSettings settings, GoogleReCaptcha2Api captcha2Api, ILogger<GoogleReCaptchaVerifier> logger)
            : base(logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _captcha2Api = captcha2Api ?? throw new ArgumentNullException(nameof(captcha2Api));
        }

        protected override async Task<bool> VerifyTokenAsync(string token, string ip = null)
        { 
            _responseDto = await _captcha2Api.SiteverifyAsync(_settings.CaptchaSettings.Secret, token, ip);
            return _responseDto.Success;
        }

        protected override string GetResponseAggregatedErrors()
        {
            return _responseDto.ErrorCodes == null ? "Something went wrong" : AggregateErrors(_responseDto.ErrorCodes);
        }
        
        private static string AggregateErrors(IReadOnlyList<string> errorCodes)
        {
            var mapped = errorCodes.Select(x => x);
            return string.Join(Environment.NewLine, mapped);
        }
    }
}
