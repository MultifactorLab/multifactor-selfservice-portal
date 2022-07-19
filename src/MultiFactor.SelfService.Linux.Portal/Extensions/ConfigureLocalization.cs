using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class LocalizationConfiguration
    {
        public static void ConfigureLocalization(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            applicationBuilder.Services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            applicationBuilder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = new[] {
                    new CultureInfo("ru"),
                    new CultureInfo("en")
                };
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;

                var defCulture = applicationBuilder.Configuration.GetValue<string>($"{nameof(PortalSettings)}:{nameof(PortalSettings.DefaultCulture)}");
                options.DefaultRequestCulture = new RequestCulture(defCulture);
            });
        }
    }
}
