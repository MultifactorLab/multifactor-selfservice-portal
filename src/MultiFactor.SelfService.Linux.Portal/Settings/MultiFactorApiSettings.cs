using MultiFactor.SelfService.Linux.Portal.Settings.PrivacyMode;

namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class MultiFactorApiSettings
    {
        public string ApiUrl { get; private set; }
        public string ApiProxy { get; private set; }
        public string ApiKey { get; private set; }
        public string ApiSecret { get; private set; }
        public string PrivacyMode { get; private set; }
        public PrivacyModeDescriptor PrivacyModeDescriptor { get; set; }
    }
}
