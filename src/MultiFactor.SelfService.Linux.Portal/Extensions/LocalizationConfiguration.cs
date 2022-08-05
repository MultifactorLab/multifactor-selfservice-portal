using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class LocalizationConfiguration
    {
        private static readonly CultureInfo _ru = new ("ru");
        private static readonly CultureInfo _en = new ("en");
        private static readonly CultureInfo[] _supportedCultures = { _ru, _en};

        public static void AddControllersWithViewsAndLocalization(this WebApplicationBuilder applicationBuilder)
        {
            AddControllersWithViewsAndLocalization(applicationBuilder, null);
        }

        public static void AddControllersWithViewsAndLocalization(this WebApplicationBuilder applicationBuilder, Action<MvcOptions>? configure)
        {
            applicationBuilder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            applicationBuilder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                SetOptions(options, applicationBuilder.Configuration);
            });

            applicationBuilder.Services.AddControllersWithViews(configure)
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        private static void SetOptions(RequestLocalizationOptions options, IConfiguration config)
        {
            var culture = config.GetPortalSettingsValue(x => x.UICulture);
            if (culture == null)
            {
                SetDefault(options);
                return;
            }

            var ci = TryParse(culture);
            if (_supportedCultures.Any(x => x.LCID == ci.LCID))
            {
                options.SetDefaultCulture(ci.TwoLetterISOLanguageName);
                options.SupportedCultures = new List<CultureInfo> { ci };
                options.SupportedUICultures = new List<CultureInfo> { ci };
                return;
            }

            var r = new Regex("(?<auto>auto):(?<culture>\\w+)");
            var match = r.Match(culture);
            if (!match.Success) 
            {
                SetDefault(options);
                return;
            }

            var specCulture = new CultureInfo(match.Groups["culture"].Value);
            if (!_supportedCultures.Any(x => x.LCID == specCulture.LCID))
            {
                options.SetDefaultCulture(_en.TwoLetterISOLanguageName);
                options.SupportedCultures = new List<CultureInfo> { _en, _ru };
                options.SupportedUICultures = new List<CultureInfo> { _en, _ru };
                return;
            }
            
            options.SetDefaultCulture(specCulture.TwoLetterISOLanguageName);
            options.SupportedCultures = new List<CultureInfo> { _en, _ru };
            options.SupportedUICultures = new List<CultureInfo> { _en, _ru };
            return;       
        }

        private static void SetDefault(RequestLocalizationOptions options)
        {
            options.SetDefaultCulture(_en.TwoLetterISOLanguageName);
            options.SupportedCultures = new List<CultureInfo> { _en };
            options.SupportedUICultures = new List<CultureInfo> { _en };
        }

        private static CultureInfo TryParse(string name)
        {
            try
            {
                return new CultureInfo(name);
            }
            catch
            {
                return new CultureInfo("");
            }
        }
    }
}
