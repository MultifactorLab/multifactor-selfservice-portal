using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex
{
    public class YandexCaptchaVerifier : BaseCaptchaVerifier
    {
        private readonly YandexCaptchaApi _captchaApi;
        private readonly PortalSettings _settings;
        private YandexVerifyCaptchaResponseDto? _captchaResponse;
        
        public YandexCaptchaVerifier(ILogger<YandexCaptchaVerifier> logger, YandexCaptchaApi captchaApi, PortalSettings settings) : base(logger)
        {
            _captchaApi = captchaApi ?? throw new ArgumentNullException(nameof(captchaApi));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override async Task<bool> VerifyTokenAsync(string token, string? ip = null)
        {
            _captchaResponse = await _captchaApi.VerifyAsync(_settings.CaptchaSettings.Secret, token, ip);
            return _captchaResponse?.Status.Equals("Ok", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        protected override string GetResponseAggregatedErrors()
        {
            return _captchaResponse?.Message ?? DEFAULT_ERROR_MESSAGE;
        }
    }
}