using MultiFactor.SelfService.Linux.Portal.Options;
using MultiFactor.SelfService.Linux.Portal.Services;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class CloudConfigurationConfiguring
    {
        public static WebApplicationBuilder ConfigureCloudConfiguration(this WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<ShowcaseSettings>()
                .BindConfiguration("ShowcaseSettings")
                .ValidateDataAnnotations();

            builder.Services.AddTransient<IShowcaseSettingsOptions, ShowcaseSettingsOptions>();
            builder.Services.AddHostedService<ShowcaseSettingsUpdaterService>();

            return builder;
        }
    }
}
