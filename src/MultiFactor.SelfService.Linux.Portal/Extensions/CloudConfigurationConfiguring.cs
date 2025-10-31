using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Options;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class CloudConfigurationConfiguring
    {
        public static WebApplicationBuilder ConfigureCloudConfiguration(this WebApplicationBuilder builder)
        {
            var provider = builder.Services.BuildServiceProvider();
            var configProvider = new CloudConfigurationProvider(
                provider.GetRequiredService<IMultiFactorApi>(),
                provider.GetRequiredService<IMemoryCache>(),
                provider.GetRequiredService<IWebHostEnvironment>(),
                provider.GetRequiredService<ILogger<CloudConfigurationProvider>>());

            builder.Host.ConfigureAppConfiguration((hostingContext, configurationBuilder) =>
            {
                configurationBuilder.Add(configProvider);
            });

            builder.Services.AddOptions<ShowcaseSettings>()
                .BindConfiguration("ShowcaseSettings")
                .ValidateDataAnnotations();

            builder.Services.AddSingleton(configProvider);
            builder.Services.AddSingleton<ICloudConfigurationRefresher, CloudConfigurationRefresher>();
            builder.Services.AddTransient<IShowcaseSettingsOptions, ShowcaseSettingsOptions>();

            return builder;
        }
    }
}
