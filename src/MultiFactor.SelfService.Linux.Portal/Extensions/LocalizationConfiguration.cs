using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class LocalizationConfiguration
    {
        public static void AddLocalization(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            applicationBuilder.Services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        public static void UseLocalization(this WebApplication app)
        {
            app.UseRequestLocalization(o =>
            {
                var cultures = new[] {
                    new CultureInfo("ru"),
                    new CultureInfo("en")
                };
                o.SupportedCultures = cultures;
                o.SupportedUICultures = cultures;

                var defCulture = app.GetSettingsValue(x => x.DefaultCulture);
                o.DefaultRequestCulture = new RequestCulture(defCulture);
            });
        }
    }
}
