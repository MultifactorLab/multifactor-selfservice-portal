namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class GoogleReCaptchaSettings
    {
        public bool Enabled { get; private set; }
        public string Key { get; private set; }
        public string Secret { get; private set; }
    }
}
