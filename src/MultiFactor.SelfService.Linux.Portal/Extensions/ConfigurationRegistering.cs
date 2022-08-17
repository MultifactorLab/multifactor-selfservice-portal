using MultiFactor.SelfService.Linux.Portal.Settings;
using Serilog;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static partial class ConfigurationRegistering
    {
        /// <summary>
        /// Loads configuration and adds it as a singletone in the DI container.
        /// Must be called after the logger is configered (ConfigureLogging()).
        /// </summary>
        public static WebApplicationBuilder RegisterConfiguration(this WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder.Configuration) ?? throw new Exception("Can't find PortalSettings section in appsettings");

                Validate(settings);

                applicationBuilder.Services.AddSingleton(settings);
                return applicationBuilder;
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

        private static PortalSettings GetSettings(IConfigurationRoot config)
        {
            var section = config.GetSection(nameof(PortalSettings));
            return section.Get<PortalSettings>(o =>
            {
                o.BindNonPublicProperties = true;
            });
        }

        private static void Validate(PortalSettings settings)
        {
            var result = new PortalSettingsValidator().Validate(settings);
            if (result.IsValid) return;

            var aggregatedMsg = result.Errors.Select(x => x.ErrorMessage).Aggregate((acc, cur) => $"{acc}{Environment.NewLine}{cur}");
            throw new Exception($"Configuration errors: {Environment.NewLine}{aggregatedMsg}");
        }
    }
}
