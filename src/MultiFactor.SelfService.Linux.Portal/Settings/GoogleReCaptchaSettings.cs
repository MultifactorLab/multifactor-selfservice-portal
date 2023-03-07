namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    [Obsolete("To be replaced with CaptchaSettings class")]
    public class GoogleReCaptchaSettings
    {
        public bool Enabled { get; private set; } = false;
        public string Key { get; private set; } = string.Empty;
        public string Secret { get; private set; } = string.Empty;
    }
}