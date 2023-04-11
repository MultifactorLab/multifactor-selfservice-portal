using MultiFactor.SelfService.Linux.Portal.Extensions;
using System.Reflection;

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

    public enum CaptchaRequired
    {
        [CaptchaPlace(Place = CaptchaPlace.Login | CaptchaPlace.PasswordRecovery)]
        Full = 0,
        [CaptchaPlace(Place = CaptchaPlace.PasswordRecovery)]
        PasswordRecovery = 1
    }

    public class CaptchaSettings
    {
        public CaptchaType CaptchaType { get; init; }
        public bool Enabled { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Secret { get; init; } = string.Empty;
        public CaptchaRequired CaptchaRequired { get; init; } = CaptchaRequired.Full;

        public bool IsCaptchaEnabled(CaptchaType type, CaptchaPlace place) =>
            Enabled && CaptchaType == type && IsCaptchaEnabled(place);

        public bool IsCaptchaEnabled(CaptchaPlace place)
        {
            var captchaRequiredMask = CaptchaRequired.GetEnumAttribute<CaptchaPlaceAttribute>();
            return Enabled && captchaRequiredMask!.Place.HasFlag(place);
        }

        public bool IsCaptchaEnabled(CaptchaType type) =>
            Enabled && CaptchaType == type;

    }

    public class CaptchaPlaceAttribute : Attribute
    {
        public CaptchaPlace Place { get; init; }
    }
}
