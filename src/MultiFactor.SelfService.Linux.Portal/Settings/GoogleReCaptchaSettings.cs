namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    [Obsolete]
    public class GoogleReCaptchaSettings
    {
        public bool Enabled { get; private set; } = false;
        public string Key { get; private set; } = string.Empty;
        public string Secret { get; private set; } = string.Empty;
    }
}