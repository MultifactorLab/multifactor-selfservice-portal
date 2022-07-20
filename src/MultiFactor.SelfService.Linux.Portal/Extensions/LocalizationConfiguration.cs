using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class LocalizationConfiguration
    {
        private static readonly CultureInfo[] _supportedCultures = new[] { new CultureInfo("ru"), new CultureInfo("en") };

        public static void AddControllersWithViewsAndLocalization(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            applicationBuilder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = GetCultures(applicationBuilder.Configuration);
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });
            applicationBuilder.Services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        private static CultureInfo[] GetCultures(IConfiguration config)
        {
            var culture = config.GetPortalSettingsValue(x => x.UICulture);
            if (culture == null) return _supportedCultures;

            var ci = new CultureInfo(culture);
            if (!_supportedCultures.Any(x => x.LCID == ci.LCID))
            {
                return new[] { _supportedCultures[0] };
            }

            return new[] { ci };
        }
    }
}
