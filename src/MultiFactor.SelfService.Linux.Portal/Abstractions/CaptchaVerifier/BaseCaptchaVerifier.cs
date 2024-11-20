using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier
{
    public abstract class BaseCaptchaVerifier : ICaptchaVerifier
    {
        protected const string DEFAULT_ERROR_MESSAGE = "Something went wrong";
        private readonly ILogger<BaseCaptchaVerifier> _logger;

        protected BaseCaptchaVerifier(ILogger<BaseCaptchaVerifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract Task<bool> VerifyTokenAsync(string token, string ip = null);

        protected virtual string GetResponseAggregatedErrors()
        {
            return DEFAULT_ERROR_MESSAGE;
        }
        public async Task<CaptchaVerificationResult> VerifyCaptchaAsync(HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            try
            {
                var token = request.Form[Constants.CAPTCHA_TOKEN].FirstOrDefault();
                if (token == null)
                {
                    return new CaptchaVerificationResult(false, "Response token is null");
                }

                var headerAccessor = new HttpHeaderAccessor(request.HttpContext);
                var ipAddress = HttpClientUtils.GetRequestIp(request.HttpContext, headerAccessor);
                
                var response = await VerifyTokenAsync(token, ipAddress);
                if (response)
                {
                    return new CaptchaVerificationResult(true);
                }
                
                var aggregatedError = GetResponseAggregatedErrors();
                return new CaptchaVerificationResult(false, aggregatedError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Captcha verification failed");
                return new CaptchaVerificationResult(false, ex.Message);
            }
        }
    }    
}
