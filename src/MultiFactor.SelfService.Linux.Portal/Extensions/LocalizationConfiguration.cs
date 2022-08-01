﻿using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class LocalizationConfiguration
    {
        private static readonly CultureInfo[] _supportedCultures = new[] { new CultureInfo("ru"), new CultureInfo("en") };

        public static void AddControllersWithViewsAndLocalization(this WebApplicationBuilder applicationBuilder)
        {
            AddControllersWithViewsAndLocalization(applicationBuilder, null);
        }

        public static void AddControllersWithViewsAndLocalization(this WebApplicationBuilder applicationBuilder, Action<MvcOptions>? configure)
        {
            applicationBuilder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            applicationBuilder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = GetCultures(applicationBuilder.Configuration);
                options.SetDefaultCulture(_supportedCultures[0].TwoLetterISOLanguageName);
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });

            applicationBuilder.Services.AddControllersWithViews(configure)
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
