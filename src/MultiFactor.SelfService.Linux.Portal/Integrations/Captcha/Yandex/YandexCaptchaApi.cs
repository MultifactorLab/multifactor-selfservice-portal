using Microsoft.AspNetCore.WebUtilities;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex
{
    public class YandexCaptchaApi
    {
        private readonly HttpClientAdapter _client;

        public YandexCaptchaApi(YandexHttpClientAdapterFactory factory)
        {
            _client = factory?.CreateClientAdapter() ?? throw new ArgumentNullException(nameof(factory));
        }
        
        public async Task<YandexVerifyCaptchaResponseDto> VerifyAsync(string secret, string responseToken, string? remoteIp = null)
        {
            if (secret is null) throw new ArgumentNullException(nameof(secret));
            if (responseToken is null) throw new ArgumentNullException(nameof(responseToken));

            var param = new Dictionary<string, string?>
            { 
                { "secret", secret },
                { "token", responseToken }
            };
            if (!string.IsNullOrEmpty(remoteIp))
            {
                param.Add("ip", remoteIp);
            }

            var newAction = QueryHelpers.AddQueryString("validate", param);

            var resp = await _client.GetAsync<YandexVerifyCaptchaResponseDto>(newAction) ?? throw new Exception("Response is null");
            return resp;
        }
    }    
}
