namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class SettingsLoading
    {
        public static void LoadSettings(this WebApplicationBuilder applicationBuilder, string[] args)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                config.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                      .AddXmlFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.xml", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            var settings = GetSettings(applicationBuilder) ?? throw new Exception("Can't find PortalSettings section in appsettings");

            ValidateSettings(settings);

            applicationBuilder.Services.AddSingleton(settings);
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
            if (string.IsNullOrEmpty(settings.LoggingLevel)) ThrowConfigError(nameof(PortalSettings.LoggingLevel));
        }

        private static void ThrowConfigError(string propertyName)
        {
            throw new Exception($"Configuration error: '{propertyName}' element not found");
        }
    }
}
