using MultiFactor.SelfService.Linux.Portal.Settings;
using Serilog;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static partial class ConfigurationRegistering
    {
        /// <summary>
        /// Loads configuration and adds it as a singletone in the DI container.
        /// Must be called after the logger is configered (<see cref="Logging.ConfigureLogging(WebApplicationBuilder)"/>).
        /// </summary>
        public static WebApplicationBuilder RegisterConfiguration(this WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder.Configuration) ?? throw new Exception("Can't find PortalSettings section in appsettings");
                MapObsoleteSections(settings);
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

        private static void MapObsoleteSections(PortalSettings settings)
        {
#pragma warning disable CS0612
            if (settings.GoogleReCaptchaSettings.Enabled && !settings.CaptchaSettings.Enabled)
            {
                settings.CaptchaSettings = new CaptchaSettings()
                {
                    Enabled = settings.GoogleReCaptchaSettings.Enabled,
                    Key = settings.GoogleReCaptchaSettings.Key,
                    Secret = settings.GoogleReCaptchaSettings.Secret,
                    CaptchaType = CaptchaType.Google
                };
            }

            if (settings.EnablePasswordManagement && !settings.PasswordManagement.PasswordManagementEnabled && 
                !settings.PasswordManagement.PasswordRecoveryEnabled) {

                settings.PasswordManagement = new PasswordManagementSettings(
                    settings.EnablePasswordManagement,
                    false,
                    settings.ChangeValidPasswordMode,
                    settings.ChangeExpiredPasswordMode,
                    settings.PasswordChangingSessionSettings?.PwdChangingSessionLifetime ??
                        settings.PasswordManagement.PwdChangingSessionLifetime,
                    settings.PasswordChangingSessionSettings?.PwdChangingSessionCacheSize ??
                        settings.PasswordManagement.PwdChangingSessionCacheSize
                    );
            }
#pragma warning restore CS0612
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
