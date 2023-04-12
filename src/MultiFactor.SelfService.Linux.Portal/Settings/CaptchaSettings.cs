namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public enum CaptchaType
    {
        Google = 0,
        Yandex = 1,
    }

    public enum CaptchaRequired
    {
        Always = 0,
        PasswordRecovery = 1
    }

    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; init; }
        public bool Enabled { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Secret { get; init; } = string.Empty;
        public CaptchaRequired CaptchaRequired { get; init; } = CaptchaRequired.Always;

        public bool IsCaptchaEnabled(CaptchaType type, CaptchaRequired mode) =>
            Enabled && CaptchaType == type && IsCaptchaEnabled(mode);

        public bool IsCaptchaEnabled(CaptchaRequired mode)
        {
            var placeEnabled = CaptchaRequired == CaptchaRequired.Always
               || (CaptchaRequired == CaptchaRequired.PasswordRecovery && mode == CaptchaRequired.PasswordRecovery);
            return Enabled && placeEnabled;
        }

        public bool IsCaptchaEnabled(CaptchaType type) =>
            Enabled && CaptchaType == type;
    }
}