using Serilog;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class SettingsLoading
    {
        public static WebApplicationBuilder LoadSettings(this WebApplicationBuilder applicationBuilder, string[] args)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                      .AddXmlFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.xml", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();

                if (args.Any())
                {
                    config.AddCommandLine(args);
                }
            });

            LoadPortalSettings(applicationBuilder);

            return applicationBuilder;
        }

        private static void LoadPortalSettings(WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder) ?? throw new Exception("Can't find PortalSettings section in appsettings");
                ValidateSettings(settings);
                applicationBuilder.Services.AddSingleton(settings);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static PortalSettings GetSettings(WebApplicationBuilder applicationBuilder)
        {
            var section = applicationBuilder.Configuration.GetSection(nameof(PortalSettings));
            return section.Get<PortalSettings>(o =>
            {
                o.BindNonPublicProperties = true;
            });
        }

        private static void ValidateSettings(PortalSettings settings)
        {
            if (string.IsNullOrEmpty(settings.CompanyName)) ThrowConfigError(nameof(PortalSettings.CompanyName));
            if (string.IsNullOrEmpty(settings.CompanyDomain)) ThrowConfigError(nameof(PortalSettings.CompanyDomain));
            if (string.IsNullOrEmpty(settings.CompanyLogoUrl)) ThrowConfigError(nameof(PortalSettings.CompanyLogoUrl));
            if (string.IsNullOrEmpty(settings.MultiFactorApiUrl)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiUrl));
            if (string.IsNullOrEmpty(settings.MultiFactorApiKey)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiKey));
            if (string.IsNullOrEmpty(settings.MultiFactorApiSecret)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiSecret));
        }

        private static void ThrowConfigError(string propertyName)
        {
            throw new Exception($"Configuration error: '{propertyName}' element not found");
        }
    }
}
