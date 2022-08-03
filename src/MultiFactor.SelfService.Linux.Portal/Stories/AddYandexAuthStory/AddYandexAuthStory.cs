using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AddYandexAuthStory
{
    public class AddYandexAuthStory
    {
        private readonly MultiFactorApi _api;
        private readonly IStringLocalizer _localizer;

        public AddYandexAuthStory(MultiFactorApi api, IStringLocalizer<SharedResource> localizer)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<IActionResult> ExecuteAsync(string key, string otp)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (otp is null) throw new ArgumentNullException(nameof(otp));

            try
            {
                await _api.AddTotpAuthenticatorAsync(key, otp);
                return new LocalRedirectResult("/");
            }
            catch (UnsuccessfulResponseException)
            {
                throw new ModelStateErrorException(_localizer.GetString("WrongOtp"));
            }
        }
    }
}
