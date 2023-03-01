namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public enum CaptchaType
    {
        Google = 0,
        Yandex = 1,
    }
    
    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; init; }
        public bool Enabled { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Secret { get; init; } = string.Empty;

        public bool IsCaptchaEnabled(CaptchaType type) => Enabled && CaptchaType == type;
    }
}
