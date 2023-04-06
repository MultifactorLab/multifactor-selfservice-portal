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
        Login = 1,
        PasswordRecovery = 2
    }

    [Flags]
    public enum CaptchaRequired
    {
        Full = CaptchaPlace.Login | CaptchaPlace.PasswordRecovery,
        PasswordRecovery = CaptchaPlace.PasswordRecovery
    }

    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; init; }
        public bool Enabled { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Secret { get; init; } = string.Empty;

        public bool IsCaptchaEnabled(CaptchaType type, CaptchaPlace place) =>
            Enabled && CaptchaType == type && IsCaptchaEnabled(place);
        public bool IsCaptchaEnabled(CaptchaPlace place) =>
            Enabled && CaptchaRequired.HasFlag((CaptchaRequired)place);
        public bool IsCaptchaEnabled(CaptchaType type) =>
            Enabled && CaptchaType == type;

        public CaptchaRequired CaptchaRequired { get; init; } = CaptchaRequired.PasswordRecovery;
    }
}
