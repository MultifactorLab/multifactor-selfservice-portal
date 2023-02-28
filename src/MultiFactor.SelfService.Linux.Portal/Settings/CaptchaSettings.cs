namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public enum CaptchaType
    {
        Google = 0,
        Yandex = 1,
    }
    
    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; private set; } = CaptchaType.Yandex;
        public bool Enabled { get; private set; } = false;
        public string Key { get; private set; } = string.Empty;
        public string Secret { get; private set; } = string.Empty;

        public bool IsYandexEnabled => CaptchaType == CaptchaType.Yandex && Enabled;
        public bool IsGoogleEnabled => CaptchaType == CaptchaType.Google && Enabled;
    }
}
