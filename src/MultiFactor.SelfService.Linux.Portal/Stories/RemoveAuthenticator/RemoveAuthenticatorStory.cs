using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator
{
    public class RemoveAuthenticatorStory
    {
        private readonly MultiFactorApi _api;

        public RemoveAuthenticatorStory(MultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public Task<IActionResult> ExecuteAsync(RemoveAuthenticatorDto dto)
        {
            throw new NotImplementedException();

            //var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);


            //    var userProfile = api.LoadProfile();
            //    if (userProfile.Count > 1) //do not remove last
            //    {
            //        api.RemoveAuthenticator(authenticator, id);
            //    }

            //    return RedirectToAction("Index");

        }
    }
}
