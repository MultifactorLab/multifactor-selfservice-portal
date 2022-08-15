using MultiFactor.SelfService.Linux.Portal.Integrations.Google;

namespace MultiFactor.SelfService.Linux.Portal.Stories.VerifyCaptchaStory
{
    public class VerifyCaptchaStory
    {
        private readonly GoogleApi _googleApi;

        public VerifyCaptchaStory(GoogleApi googleApi)
        {
            _googleApi = googleApi ?? throw new ArgumentNullException(nameof(googleApi));
        }

        public async Task ExecuteAsync()
        {

        }
    }
}
