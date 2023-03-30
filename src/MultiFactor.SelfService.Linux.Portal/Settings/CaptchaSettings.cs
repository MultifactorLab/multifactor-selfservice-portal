namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public enum CaptchaType
    {
        Google = 0,
        Yandex = 1,
    }
    
    [Flags]
    public enum CaptchaPlace
    {
        Everywhere = 0,
        Login = 1,
        PasswordRecovery = 2
    }
    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; init; }
        public bool Enabled { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Secret { get; init; } = string.Empty;

        public bool IsCaptchaEnabled(CaptchaType type, CaptchaPlace place = CaptchaPlace.Everywhere) =>
            Enabled && CaptchaType == type && CaptchaPlace.HasFlag(place);
        public bool IsCaptchaEnabled(CaptchaPlace place = CaptchaPlace.Everywhere) =>
            Enabled && CaptchaPlace.HasFlag(place);

        public CaptchaPlace CaptchaPlace { get; init; } = CaptchaPlace.Everywhere;
    }
}
